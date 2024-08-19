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
                                UserAccount account = new UserAccount();
                                account.Enabled = reader.GetAttribute("Enabled") switch
                                {
                                    "1" => true,
                                    "0" => false,
                                    _ => false
                                };
                                account.Name = reader.GetAttribute("Name");
                                if (account.Name.Length > 20)
                                {
                                    Console.WriteLine($"WARNING: Account name {account.Name} is over 20 characters long. Truncating to 20 characters...");
                                    account.Name = account.Name.Substring(0, 20);
                                }
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

    public class AutoLogon
    {

        public enum AutoLogonMode
        {
            None,
            FirstAdmin,
            BuiltInAdmin
        }

        public AutoLogonMode logonMode;

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
