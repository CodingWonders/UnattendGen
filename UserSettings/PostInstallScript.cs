using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace UnattendGen.UserSettings
{

    /// <summary>
    /// Post-installation scripts to run after the Windows installation or at a specific stage during Windows installation
    /// </summary>
    public class PostInstallScript
    {

        public string? ScriptContent;

        public enum StageContext
        {
            /// <summary>
            /// The script will run in the system's Specialize pass, during Windows installation, and before users are created
            /// </summary>
            System,

            /// <summary>
            /// The script will run when the first user logs on
            /// </summary>
            FirstLogon,

            /// <summary>
            /// The script will run when a user logs on for the first time. If the target system were to be configured with multiple user accounts, the script will run on all of them when they log on for the first time
            /// </summary>
            FirstTimeUserLogon,

            /// <summary>
            /// The script will modify the default user's registry hive, stored in the NTUSER.DAT file, in \Users\Default. 
            /// 
            /// This is expected to be of REG type.
            /// </summary>
            NTUserHiveModify
        }

        public enum ScriptExtension
        {
            /// <summary>
            /// PowerShell script
            /// </summary>
            PowerShell,
            /// <summary>
            /// Batch script
            /// </summary>
            Batch,
            /// <summary>
            /// Exported Registry File
            /// </summary>
            Reg,
            /// <summary>
            /// Visual Basic Script file
            /// </summary>
            VBScript,
            /// <summary>
            /// JScript file
            /// </summary>
            JScript,
            /// <summary>
            /// Unknown script type
            /// </summary>
            Unknown,
            /// <summary>
            /// Independent command. The script did not come from a file
            /// </summary>
            NoFile
        }

        public ScriptExtension Extension;

        public StageContext Stage;

        public PostInstallScript()
        {
            ScriptContent = "Write-Host \"Hello World\"";
            Extension = ScriptExtension.PowerShell;
            Stage = StageContext.System;
        }

        public PostInstallScript(string? scriptContent, ScriptExtension extension, StageContext stage)
        {
            ScriptContent = scriptContent;
            Extension = extension;
            Stage = stage;
        }

        public static List<PostInstallScript>? LoadScripts(string? filePath)
        {
            List<PostInstallScript> scriptList = new List<PostInstallScript>();

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    XmlReaderSettings xs = new XmlReaderSettings();
                    xs.IgnoreWhitespace = true;

                    using (XmlReader reader = XmlReader.Create(fs, xs))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "PostInstallScript")
                            {
                                // Determine if script is of file or inline command

                                string scriptContent = reader.GetAttribute("ScriptContent");

                                string scriptPath = "";

                                ScriptExtension extension = ScriptExtension.Unknown;

                                // Let's see if this item is not bogus
                                if (!string.IsNullOrEmpty(scriptContent))
                                {
                                    // If it starts with file:, then we know that what we are referencing is a file, and not a file path.
                                    // In that case, grab file contents instead
                                    if (scriptContent.StartsWith("file:"))
                                    {
                                        // Load script from file
                                        scriptPath = scriptContent.Substring(5);
                                        if (!File.Exists(scriptPath))
                                        {
                                            Console.WriteLine($"WARNING: the specified file (\"{Path.GetFileName(scriptPath)}\") does not exist. Skipping...");
                                            continue;
                                        }
                                        Console.WriteLine($"INFO: Getting contents of file \"{Path.GetFileName(scriptPath)}\"...");
                                        scriptContent = File.ReadAllText(scriptPath);

                                        // Let's get to know our file better based on its extension. If we got an unknown file type,
                                        // then we discard the file and move on.
                                        extension = Path.GetExtension(scriptPath).ToLowerInvariant() switch
                                        {
                                            ".bat" or ".cmd" or ".nt" => ScriptExtension.Batch,
                                            ".ps1" => ScriptExtension.PowerShell,
                                            ".reg" => ScriptExtension.Reg,
                                            ".vbs" or ".vbe" or ".wsf" or ".wsc" => ScriptExtension.VBScript,
                                            ".js" or ".jse" => ScriptExtension.JScript,
                                            _ => ScriptExtension.Unknown
                                        };

                                        if (extension == ScriptExtension.Unknown)
                                        {
                                            Console.WriteLine($"WARNING: the specified file (\"{Path.GetFileName(scriptPath)}\") does not appear to have a valid extension. Skipping...");
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        extension = ScriptExtension.NoFile;
                                    }
                                }

                                StageContext stage = reader.GetAttribute("Stage") switch
                                {
                                    "System" => StageContext.System,
                                    "FirstLogon" => StageContext.FirstLogon,
                                    "FirstTimeUserLogon" => StageContext.FirstTimeUserLogon,
                                    "NTUserHiveModify" => StageContext.NTUserHiveModify,
                                    _ => throw new Exception("Invalid script stage")
                                };

                                if ((stage == StageContext.NTUserHiveModify) && (extension == ScriptExtension.Reg))
                                {
                                    // Check header
                                    if (!scriptContent.StartsWith("Windows Registry Editor Version 5.00"))
                                    {
                                        Console.WriteLine($"WARNING: the specified file is of REG type, but its header is not valid. Skipping...");
                                        continue;
                                    }
                                }
                                else if ((stage == StageContext.NTUserHiveModify) && (extension != ScriptExtension.Reg))
                                {
                                    Console.WriteLine($"WARNING: to modify the NTUSER.DAT hive, the file must be of REG type. Skipping...");
                                    continue;
                                }

                                scriptList.Add(new PostInstallScript(scriptContent, extension, stage));
                            }
                        }
                    }
                }
                return scriptList;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
