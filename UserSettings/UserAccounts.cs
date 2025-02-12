﻿using System;
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

        public string? DisplayName;

        public string? Password;

        public UserGroup Group;

        public UserAccount() 
        {
            Enabled = false;
            Name = "";
            DisplayName = "";
            Password = "";
            Group = UserGroup.Users;
        }

        public UserAccount(bool enabled, string? name, string? password, UserGroup group)
        {
            Enabled = enabled;
            Name = name;
            DisplayName = "";
            Password = password;
            Group = group;
        }

        public UserAccount(bool enabled, string? name, string? displayName, string? password, UserGroup group)
        {
            Enabled = enabled;
            Name = name;
            DisplayName = displayName;
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

                                string accountDisplayName = "";

                                if (reader.GetAttribute("DisplayName") != null)
                                {
                                    accountDisplayName = reader.GetAttribute("DisplayName");
                                }

                                UserAccount account = new UserAccount(reader.GetAttribute("Enabled") switch
                                                                      {
                                                                          "1" => true,
                                                                          "0" => false,
                                                                          _ => false
                                                                      },
                                                                      (reader.GetAttribute("Name").Length > 20 ?
                                                                          reader.GetAttribute("Name").Substring(0, 20) :
                                                                          reader.GetAttribute("Name")),
                                                                      (accountDisplayName.Length > 256 ?
                                                                          accountDisplayName.Substring(0, 256) :
                                                                          accountDisplayName),
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

    public class AccountLockout
    {

        public bool Enabled;

        public int FailedAttempts;

        public int TimeFrame;

        public int AutoUnlock;

        public AccountLockout() { }

        public AccountLockout(bool lockoutEnabled, int lockoutFailedAttempts, int lockoutTimeFrame, int lockoutAutoUnlock)
        {
            Enabled = lockoutEnabled;
            FailedAttempts = lockoutFailedAttempts;
            TimeFrame = lockoutTimeFrame;
            AutoUnlock = lockoutAutoUnlock;

            if (TimeFrame > AutoUnlock)
            {
                Console.WriteLine($"WARNING: Timeframe ({TimeFrame}) is higher than duration ({AutoUnlock}), making it equal");
                TimeFrame = AutoUnlock;
            }
        }

        public static AccountLockout? GetAccountLockout(string filePath)
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
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "AccountLockout")
                            {
                                AccountLockout lockout = new AccountLockout();
                                lockout.Enabled = true;
                                lockout.FailedAttempts = Convert.ToInt32(reader.GetAttribute("FailedAttempts"));
                                lockout.TimeFrame = Convert.ToInt32(reader.GetAttribute("Timeframe"));
                                lockout.AutoUnlock = Convert.ToInt32(reader.GetAttribute("AutoUnlock"));

                                if (lockout.TimeFrame > lockout.AutoUnlock)
                                {
                                    Console.WriteLine($"WARNING: Timeframe ({lockout.TimeFrame}) is higher than duration ({lockout.AutoUnlock}), making it equal");
                                    lockout.TimeFrame = lockout.AutoUnlock;
                                }

                                return lockout;
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
