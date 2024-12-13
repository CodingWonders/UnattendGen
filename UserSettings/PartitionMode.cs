using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace UnattendGen.UserSettings
{
    /// <summary>
    /// Partition settings for Disk 0
    /// </summary>
    public class DiskZeroSettings
    {
        public enum PartitionStyle
        {
            /// <summary>
            /// MBR partition table, used by default on systems that use BIOS instead of UEFI
            /// </summary>
            MBR,
            /// <summary>
            /// GUID Partition Table, used by default on newer systems with UEFI firmware
            /// </summary>
            GPT
        }

        public enum RecoveryEnvironmentMode
        {
            /// <summary>
            /// The Recovery Environment will not be present
            /// </summary>
            None,
            /// <summary>
            /// The Recovery Environment will be installed on a separate disk partition
            /// </summary>
            Partition,
            /// <summary>
            /// The Recovery Environment will be installed on the Windows installation
            /// </summary>
            Windows
        }

        /// <summary>
        /// The style of the partition table
        /// </summary>
        public PartitionStyle partStyle;

        /// <summary>
        /// The size of the EFI System Partition, in MB
        /// </summary>
        public int ESPSize;

        /// <summary>
        /// The availability and location of the Recovery Environment (WinRE)
        /// </summary>
        public RecoveryEnvironmentMode recoveryEnvironment;

        /// <summary>
        /// The size of the Recovery Environment partition, in MB
        /// </summary>
        public int recEnvSize;

        /// <summary>
        /// Loads the disk settings specified by the user in a configuration file
        /// </summary>
        /// <param name="filePath">The path of the configuration file</param>
        /// <returns>The disk settings</returns>
        public static DiskZeroSettings? LoadDiskSettings(string filePath)
        {
            DiskZeroSettings diskZero = new DiskZeroSettings();

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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "DiskZero")
                            {
                                diskZero.partStyle = reader.GetAttribute("PartitionStyle") switch
                                {
                                    "MBR" => PartitionStyle.MBR,
                                    "GPT" => PartitionStyle.GPT,
                                    _ => PartitionStyle.GPT
                                };
                                diskZero.recoveryEnvironment = reader.GetAttribute("RecoveryEnvironment") switch
                                {
                                    "No" => RecoveryEnvironmentMode.None,
                                    "WinRE" => RecoveryEnvironmentMode.Partition,
                                    "Windows" => RecoveryEnvironmentMode.Windows,
                                    _ => RecoveryEnvironmentMode.Partition
                                };

                                diskZero.ESPSize = Convert.ToInt32(reader.GetAttribute("ESPSize"));
                                diskZero.recEnvSize = Convert.ToInt32(reader.GetAttribute("RESize"));

                            }
                        }
                    }
                }
                return diskZero;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }

    }

    /// <summary>
    /// Partition settings for DiskPart scripts
    /// </summary>
    public class DiskPartSettings
    {
        /// <summary>
        /// The path of the specified DiskPart script
        /// </summary>
        public string? scriptFile;

        /// <summary>
        /// Determine whether or not to install Windows to the first available partition that has enough space and does not already contain an installation of Windows, after DiskPart configuration has finished
        /// </summary>
        public bool automaticInstall;

        /// <summary>
        /// Disk number for OS installation. Only used when <see cref="automaticInstall"/> is set to false
        /// </summary>
        public int diskNum;

        /// <summary>
        /// Partition number for OS installation. Only used when <see cref="automaticInstall"/> is set to false
        /// </summary>
        public int partNum;

        /// <summary>
        /// Loads the DiskPart settings specified by the user in a configuration file
        /// </summary>
        /// <param name="filePath">The path of the configuration file</param>
        /// <returns>The DiskPart configuration</returns>
        public static DiskPartSettings? LoadDiskSettings(string filePath)
        {
            DiskPartSettings diskPart = new DiskPartSettings();

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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "DiskPart")
                            {
                                diskPart.scriptFile = reader.GetAttribute("ScriptFile");
                                diskPart.automaticInstall = (reader.GetAttribute("AutoInst") == "1");
                                diskPart.diskNum = Convert.ToInt32(reader.GetAttribute("Disk"));
                                diskPart.partNum = Convert.ToInt32(reader.GetAttribute("Partition"));
                            }
                        }
                    }
                }
                return diskPart;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }

    }
}
