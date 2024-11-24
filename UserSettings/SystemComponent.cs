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
    public class SystemComponent
    {

        public string? Id;

        public List<SystemPass>? Passes = new List<SystemPass>();

        public SystemComponent() { }

        public SystemComponent(string? id)
        {
            this.Id = id;
        }

        public SystemComponent(string? id, List<SystemPass>? passes)
        {
            Id = id;
            Passes = passes;
        }

        public static List<SystemComponent>? LoadComponents(string? filePath)
        {
            List<SystemComponent> componentList = new List<SystemComponent>();

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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Component")
                            {
                                string passList = reader.GetAttribute("Passes");
                                List<String> passListTemp = new List<String>();
                                passListTemp = passList.Split(new char[] { ',' }).ToList();

                                List<SystemPass> passes = new List<SystemPass>();

                                List<string> knownPasses =
                                [
                                    .. new string[] { "offlineServicing", "windowsPE", "generalize", "specialize", "auditSystem", "auditUser", "oobeSystem" },
                                ];

                                foreach (string pass in passListTemp)
                                {
                                    if (!knownPasses.Contains(pass))
                                    {
                                        Debug.WriteLine($"Unknown pass \"{pass}\"");
                                        continue;
                                    }
                                    passes.Add(new SystemPass(pass));
                                }

                                SystemComponent component = new SystemComponent(reader.GetAttribute("Id"), passes);

                                componentList.Add(component);
                            }
                        }
                    }
                }
                return componentList;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }

    }

    public class SystemPass
    {

        public string? Name;

        public bool Enabled;

        public SystemPass(string name)
        {
            this.Name = name;
            this.Enabled = true;
            return;
        }

    }

}
