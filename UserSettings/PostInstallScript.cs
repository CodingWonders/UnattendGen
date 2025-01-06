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

        public StageContext Stage;

        public PostInstallScript()
        {
            ScriptContent = "Write-Host \"Hello World\"";
            Stage = StageContext.System;
        }

        public PostInstallScript(string? scriptContent, StageContext stage)
        {
            ScriptContent = scriptContent;
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

                                if (stage == StageContext.NTUserHiveModify)
                                {
                                    // Verify that what we are dealing with is a REG file. Check both extension and header
                                    if (scriptPath != null)
                                    {
                                        // Check extension
                                        if (Path.GetExtension(scriptPath) == ".reg")
                                        {
                                            // Check header
                                            if (!scriptContent.StartsWith("Windows Registry Editor Version 5.00"))
                                            {
                                                Console.WriteLine($"WARNING: the specified file is of REG type, but its header is not valid. Skipping...");
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"WARNING: the specified file is not of REG type. Skipping...");
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // Outright deny it
                                        Console.WriteLine($"WARNING: no file has been specified. Skipping...");
                                        continue;
                                    }
                                }

                                scriptList.Add(new PostInstallScript(scriptContent, stage));
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
