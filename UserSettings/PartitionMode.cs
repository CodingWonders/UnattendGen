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
    public class DiskZeroSettings
    {
        public enum PartitionStyle
        {
            MBR,
            GPT
        }

        public enum RecoveryEnvironmentMode
        {
            None,
            Partition,
            Windows
        }

        public PartitionStyle partStyle;

        public int ESPSize;

        public RecoveryEnvironmentMode recoveryEnvironment;

        public int recEnvSize;

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
                                string? partStyle = reader.GetAttribute("PartitionStyle");
                                string? recEnv = reader.GetAttribute("RecoveryEnvironment");

                                switch (partStyle)
                                {
                                    case "MBR":
                                        diskZero.partStyle = PartitionStyle.MBR;
                                        break;
                                    case "GPT":
                                        diskZero.partStyle = PartitionStyle.GPT;
                                        break;
                                    default:
                                        diskZero.partStyle = PartitionStyle.GPT;
                                        break;
                                }

                                switch (recEnv)
                                {
                                    case "No":
                                        diskZero.recoveryEnvironment = RecoveryEnvironmentMode.None;
                                        break;
                                    case "WinRE":
                                        diskZero.recoveryEnvironment = RecoveryEnvironmentMode.Partition;
                                        break;
                                    case "Windows":
                                        diskZero.recoveryEnvironment = RecoveryEnvironmentMode.Windows;
                                        break;
                                    default:
                                        diskZero.recoveryEnvironment = RecoveryEnvironmentMode.Partition;
                                        break;
                                }

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

    public class DiskPartSettings
    {
        public string? scriptFile;

        public bool automaticInstall;

        public int diskNum;

        public int partNum;

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
