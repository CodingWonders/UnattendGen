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
    public class WirelessNetwork
    {
        public enum AuthenticationProtocol
        {
            Open,
            WPA2,
            WPA3
        }

        public string? SSID;

        public string? Password;

        public AuthenticationProtocol Authentication;

        public bool NonBroadcast;

        public WirelessNetwork()
        {
            SSID = "";
            Password = "";
            Authentication = AuthenticationProtocol.WPA2;
            NonBroadcast = false;
        }

        public WirelessNetwork(string? ssid, string? password, AuthenticationProtocol authProtocol, bool nonBroadcast)
        {
            SSID = ssid;
            Password = password;
            Authentication = authProtocol;
            // Disregard password on open networks
            if (authProtocol == AuthenticationProtocol.Open)
                Password = "";
            NonBroadcast = nonBroadcast;
        }

        public static WirelessNetwork? LoadSettings(string filePath)
        {
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "WirelessNetwork")
                            {
                                WirelessNetwork network = new WirelessNetwork(reader.GetAttribute("Name"),
                                                                              reader.GetAttribute("Password"),
                                                                              reader.GetAttribute("AuthMode") switch
                                                                              {
                                                                                  "Open" => AuthenticationProtocol.Open,
                                                                                  "WPA2" => AuthenticationProtocol.WPA2,
                                                                                  "WPA3" => AuthenticationProtocol.WPA3,
                                                                                  _ => AuthenticationProtocol.WPA2
                                                                              },
                                                                              reader.GetAttribute("NonBroadcast") switch
                                                                              {
                                                                                  "1" => true,
                                                                                  "0" => false,
                                                                                  _ => false
                                                                              });

                                return network;
                            }
                        }
                    }
                }
            }
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
            return null;
        }

    }
}
