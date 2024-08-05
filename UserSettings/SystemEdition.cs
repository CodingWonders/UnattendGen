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
    public class SystemEdition
    {

        public string? Id;

        public string? DisplayName;

        public string? ProductKey;

        public static SystemEdition? LoadSettings(string filePath)
        {
            SystemEdition edition = new SystemEdition();

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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Edition")
                            {

                                edition.Id = reader.GetAttribute("Id");
                                edition.DisplayName = reader.GetAttribute("DisplayName");
                                edition.ProductKey = reader.GetAttribute("Key");

                            }
                        }
                    }
                }
                return edition;
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
