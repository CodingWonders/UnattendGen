using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace UnattendGen.UserSettings
{
    public class SystemComponent
    {

        public string? Id;

        public List<SystemPass>? Passes = new List<SystemPass>();

        public string? Data;

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

        public SystemComponent(string? id, List<SystemPass>? passes, string? data)
        {
            Id = id;
            Passes = passes;
            Data = data;
        }

        public static List<SystemComponent>? LoadComponents()
        {
            List<SystemComponent> componentList = new List<SystemComponent>();

            try
            {
                string[] componentFiles = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Components"), 
                    "*.xml", SearchOption.TopDirectoryOnly);
                if (componentFiles.Length > 0)
                {
                    foreach (string filePath in componentFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (Regex.IsMatch(fileName, @"^.*_\w*$"))
                        {
                            string[] nameParts = fileName.Split("_");
                            string name = nameParts[0];
                            string pass = nameParts[1];
                            string[] knownPasses = ["windowspe", "offlineservicing", "generalize", "specialize", "audituser", "auditsystem", "oobesystem"];
                            if (!knownPasses.Contains(pass.ToLower()))
                                continue;
                            List<SystemPass> passes = [new SystemPass(pass)];
                            string contents = File.ReadAllText(filePath);
                            if (!String.IsNullOrEmpty(contents))
                            {
                                SystemComponent component = new SystemComponent(
                                    name,
                                    passes,
                                    contents);
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
