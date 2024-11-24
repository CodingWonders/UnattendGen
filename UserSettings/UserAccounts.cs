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

        public UserAccount() 
        {
            Enabled = false;
            Name = "";
            Password = "";
            Group = UserGroup.Users;
        }

        public UserAccount(bool enabled, string? name, string? password, UserGroup group)
        {
            Enabled = enabled;
            Name = name;
            Password = password;
            Group = group;
        }

        public static List<UserAccount>? LoadAccounts(string filePath)
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
                                int nameLength = reader.GetAttribute("Name").Length;

                                UserAccount account = new UserAccount(reader.GetAttribute("Enabled") switch
                                                                      {
                                                                          "1" => true,
                                                                          "0" => false,
                                                                          _ => false
                                                                      },
                                                                      (reader.GetAttribute("Name").Length > 20 ?
                                                                          reader.GetAttribute("Name").Substring(0, 20) :
                                                                          reader.GetAttribute("Name")),
                                                                      reader.GetAttribute("Password"),
                                                                      reader.GetAttribute("Group") switch
                                                                      {
                                                                          "Admins" => UserGroup.Administrators,
                                                                          "Users" => UserGroup.Users,
                                                                          _ => UserGroup.Users
                                                                      });


                                if (nameLength > 20)
                                {
                                    Console.WriteLine($"WARNING: Account name \"{reader.GetAttribute("Name")}\" has been truncated to 20 characters because its length exceeds the limit.");
                                }

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

    public class AutoLogon
    {

        public enum AutoLogonMode
        {
            None,
            FirstAdmin,
            BuiltInAdmin
        }

        public AutoLogonMode logonMode;

        public AutoLogon() { }

        public AutoLogon(AutoLogonMode autoLogonMode, string adminPassword)
        {
            logonMode = autoLogonMode;
            winAdminPass = adminPassword;
        }

        // Built-in Admin Settings

        public string? winAdminPass;

        public static string? GetAdminPassword(string filePath)
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "BuiltInAdmin")
                            {
                                return reader.GetAttribute("Password");
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

    public class AccountLockdown
    {

        public bool Enabled;

        public int FailedAttempts;

        public int TimeFrame;

        public int AutoUnlock;

        public AccountLockdown() { }

        public AccountLockdown(bool lockdownEnabled, int lockdownFailedAttempts, int lockdownTimeFrame, int lockdownAutoUnlock)
        {
            Enabled = lockdownEnabled;
            FailedAttempts = lockdownFailedAttempts;
            TimeFrame = lockdownTimeFrame;
            AutoUnlock = lockdownAutoUnlock;
        }

        public static AccountLockdown? GetAccountLockdown(string filePath)
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "AccountLockdown")
                            {
                                AccountLockdown lockdown = new AccountLockdown();
                                lockdown.Enabled = true;
                                lockdown.FailedAttempts = Convert.ToInt32(reader.GetAttribute("FailedAttempts"));
                                lockdown.TimeFrame = Convert.ToInt32(reader.GetAttribute("Timeframe"));
                                lockdown.AutoUnlock = Convert.ToInt32(reader.GetAttribute("AutoUnlock"));

                                if (lockdown.TimeFrame > lockdown.AutoUnlock)
                                {
                                    Console.WriteLine($"WARNING: Timeframe is higher than duration, making it equal...");
                                    lockdown.TimeFrame = lockdown.AutoUnlock;
                                }

                                return lockdown;
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
