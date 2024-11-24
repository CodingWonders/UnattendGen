using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace UnattendGen.UserSettings
{
    public class RegionFile
    {

        public List<GeoIds> regionGeo = new List<GeoIds>();

        public List<ImageLanguages> regionLang = new List<ImageLanguages>();

        public List<KeyboardIdentifiers> regionKeys = new List<KeyboardIdentifiers>();

        public List<TimeOffsets> regionTimes = new List<TimeOffsets>();

        public List<UserLocales> regionLocales = new List<UserLocales>();

    }

    [Serializable(), XmlRoot("GeoId")]
    public class GeoIds
    {

        [XmlAttribute("Id")]
        public string? Id { get; set; }

        [XmlAttribute("DisplayName")]
        public string? DisplayName { get; set; }

        public GeoIds()
        {
        }

        public GeoIds(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public static List<GeoIds>? LoadItems(string filePath)
        {
            List<GeoIds> geoList = new List<GeoIds>();
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "GeoId")
                            {
                                GeoIds geo = new GeoIds(reader.GetAttribute("Id"), reader.GetAttribute("DisplayName"));
                                geoList.Add(geo);
                            }
                        }
                    }
                }
                return geoList;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }

    }

    [Serializable(), XmlRoot("ImageLanguage")]
    public class ImageLanguages
    {

        [XmlAttribute("Id")]
        public string? Id { get; set; }

        [XmlAttribute("DisplayName")]
        public string? DisplayName { get; set; }

        public ImageLanguages()
        {
        }

        public ImageLanguages(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public static List<ImageLanguages>? LoadItems(string filePath)
        {
            List<ImageLanguages> langList = new List<ImageLanguages>();
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "ImageLanguage")
                            {
                                ImageLanguages lang = new ImageLanguages(reader.GetAttribute("Id"), reader.GetAttribute("DisplayName"));
                                langList.Add(lang);
                            }
                        }
                    }
                }
                return langList;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }

    }

    [Serializable(), XmlRoot("KeyboardIdentifier")]
    public class KeyboardIdentifiers
    {

        [XmlAttribute("Id")]
        public string? Id { get; set; }

        [XmlAttribute("DisplayName")]
        public string? DisplayName { get; set; }

        [XmlAttribute("Type")]
        public string? Type { get; set; }

        public KeyboardIdentifiers()
        {
        }

        public KeyboardIdentifiers(string id, string displayName, string type)
        {
            Id = id;
            DisplayName = displayName;
            Type = type;
        }

        public static List<KeyboardIdentifiers>? LoadItems(string filePath)
        {
            List<KeyboardIdentifiers> keyboardList = new List<KeyboardIdentifiers>();
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "KeyboardIdentifier")
                            {
                                KeyboardIdentifiers keyboard = new KeyboardIdentifiers(reader.GetAttribute("Id"),
                                                                                       reader.GetAttribute("DisplayName"),
                                                                                       reader.GetAttribute("Type"));
                                keyboardList.Add(keyboard);
                            }
                        }
                    }
                }
                return keyboardList;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }


    }

    [Serializable(), XmlRoot("TimeOffset")]
    public class TimeOffsets
    {

        [XmlAttribute("Id")]
        public string? Id { get; set; }

        [XmlAttribute("DisplayName")]
        public string? DisplayName { get; set; }

        public TimeOffsets()
        {
        }

        public TimeOffsets(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }


        public static List<TimeOffsets>? LoadItems(string filePath)
        {
            List<TimeOffsets> offsetList = new List<TimeOffsets>();
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "TimeOffset")
                            {
                                TimeOffsets offset = new TimeOffsets(reader.GetAttribute("Id"), reader.GetAttribute("DisplayName"));
                                offsetList.Add(offset);
                            }
                        }
                    }
                }
                return offsetList;
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }

    }

    [Serializable(), XmlRoot("UserLocale")]
    public class UserLocales
    {

        [XmlAttribute("Id")]
        public string? Id { get; set; }

        [XmlAttribute("DisplayName")]
        public string? DisplayName { get; set; }

        [XmlAttribute("LCID")]
        public string? LCID { get; set; }

        [XmlAttribute("KeyboardLayout")]
        public string? KeybId { get; set; }

        [XmlAttribute("GeoLocation")]
        public string? GeoLoc { get; set; }

        public UserLocales()
        {
        }

        public UserLocales(string id, string displayName, string lcid, string keybId, string geoLoc)
        {
            Id = id;
            DisplayName = displayName;
            LCID = lcid;
            KeybId = keybId;
            GeoLoc = geoLoc;
        }

        public static List<UserLocales>? LoadItems(string filePath)
        {
            List<UserLocales> localeList = new List<UserLocales>();
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "UserLocale")
                            {
                                UserLocales locale = new UserLocales(reader.GetAttribute("Id"),
                                                                     reader.GetAttribute("DisplayName"),
                                                                     reader.GetAttribute("LCID"),
                                                                     reader.GetAttribute("KeyboardLayout"),
                                                                     reader.GetAttribute("GeoLocation"));
                                localeList.Add(locale);
                            }
                        }
                    }
                }
                return localeList;
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
