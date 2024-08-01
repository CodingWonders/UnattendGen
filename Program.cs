using System;
using System.Xml;
using Schneegans.Unattend;
using System.IO;
using System.Text;
using System.Collections.Immutable;

namespace UnattendGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // follow example for now, document settings for later DT integration

            /* NOTES:
             * - If user decides to configure something interactively, DO NOT add setting
             * 
             */

            Console.WriteLine("Unattended Answer File Generator");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine("Program: (c) 2024. CodingWonders Software\nLibrary: (c) 2024. Christoph Schneegans");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine("SEE ATTACHED PROGRAM LICENSES FOR MORE INFORMATION REGARDING USE AND REDISTRIBUTION\n\n");

            Account account1 = new Account(
                name: "Test",
                password: "Test1234",
                group: "Administrators"
            );
            Account account2 = new Account(
                name: "Test2",
                password: "Test2_1234",
                group: "Users"
            );
            ImmutableList<Account> accounts = ImmutableList<Account>.Empty;
            accounts = accounts.AddRange(new Account[] { account1, account2 });

            UnattendGenerator generator = new();
            XmlDocument xml = generator.GenerateXml(
                Configuration.Default with
                {
                    LanguageSettings = new UnattendedLanguageSettings(
                        ImageLanguage: generator.Lookup<ImageLanguage>("en-US"),        // Image language
                        LocaleAndKeyboard: new LocaleAndKeyboard(
                            generator.Lookup<UserLocale>("en-US"),                      // User locale
                            generator.Lookup<KeyboardIdentifier>("0000040a")            // Keyboard identifier. DO NOT PREPEND 0409 as it does that automatically
                        ),
                        LocaleAndKeyboard2: null,                                       // Set value to null as DT doesn't support additional layouts
                        LocaleAndKeyboard3: null,                                       // -- same here
                        GeoLocation: generator.Lookup<GeoLocation>("244")),             // Home Location
                    AccountSettings = new UnattendedAccountSettings(
                        accounts: accounts,
                        autoLogonSettings: new BuiltinAutoLogonSettings(
                            password: account1.Password),
                        obscurePasswords: true),
                    ComputerNameSettings = new CustomComputerNameSettings(
                        name: "WIN-NHV7230VJNS")
                }
                );
            using XmlWriter writer = XmlWriter.Create(Console.Out, new XmlWriterSettings()
            {
                CloseOutput = false,
                Indent = true,
            });
            xml.WriteTo(writer);
            File.WriteAllText("unattend.xml", xml.OuterXml, Encoding.UTF8);
        }
    }
}
