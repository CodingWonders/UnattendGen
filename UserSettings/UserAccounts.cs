using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace UnattendGen.UserSettings
{
    public class UserAccount
    {

        public enum UserGroup
        {
            Administrators,
            Users
        }

        public bool Enabled;

        public string? Name;

        public string? Password;

        public UserGroup Group;

        public static List<UserAccount> LoadAccounts(string filePath)
        {
            List<UserAccount> accountList = new List<UserAccount>();

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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "UserAccount")
                            {
                                UserAccount account = new UserAccount();
                                account.Enabled = reader.GetAttribute("Enabled") switch
                                {
                                    "1" => true,
                                    "0" => false,
                                    _ => false
                                };
                                account.Name = reader.GetAttribute("Name");
                                account.Password = reader.GetAttribute("Password");
                                account.Group = reader.GetAttribute("Group") switch
                                {
                                    "Admins" => UserGroup.Administrators,
                                    "Users" => UserGroup.Users,
                                    _ => UserGroup.Users
                                };

                                accountList.Add(account);
                            }
                        }
                    }
                }
                return accountList;
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
