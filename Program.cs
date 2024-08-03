﻿using System;
using System.Xml;
using Schneegans.Unattend;
using System.IO;
using System.Text;
using System.Collections.Immutable;
using System.Reflection;

namespace UnattendGen
{
    internal class Program
    {

        static string GetCopyrightTimespan(int start, int current)
        {
            if (current == start)
            {
                return current.ToString();
            }
            else
            {
                return $"{start.ToString()} - {current.ToString()}";
            }
        }

        static async Task Main(string[] args)
        {

            Console.WriteLine($"Unattended Answer File Generator, version {Assembly.GetEntryAssembly().GetName().Version.ToString()}");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Program: (c) {GetCopyrightTimespan(2024, DateTime.Today.Year)}. CodingWonders Software\nLibrary: (c) {GetCopyrightTimespan(2024, DateTime.Today.Year)}. Christoph Schneegans");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine("SEE ATTACHED PROGRAM LICENSES FOR MORE INFORMATION REGARDING USE AND REDISTRIBUTION\n\n");

            string filePath = "unattend.xml";
            var generator = new AnswerFileGenerator();
            generator.regionalInteractive = false;
            generator.randomComputerName = true;
            generator.timeZoneImplicit = false;
            generator.accountsInteractive = false;
            generator.partitionsInteractive = false;
            await generator.GenerateAnswerFile(filePath);
        }
    }

    public class AnswerFileGenerator
    {
        public bool regionalInteractive;

        public bool randomComputerName;

        public bool timeZoneImplicit;

        public bool accountsInteractive;

        public bool partitionsInteractive;

        public async Task GenerateAnswerFile(string targetPath)
        {
            // follow example for now, document settings for later DT integration

            /* NOTES:
             * - If user decides to configure something interactively, DO NOT add setting
             * 
             */

            TimeOffset offset = new TimeOffset("W. Europe Standard Time", "(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna");

            Account account1 = new Account(
                name: "Homer",
                password: "Test_1234",
                group: "Administrators"
            );
            Account account2 = new Account(
                name: "Marge",
                password: "Test_1234",
                group: "Administrators"
            );
            Account account3 = new Account(
                name: "Bart",
                password: "Test_1234",
                group: "Users"
            );
            Account account4 = new Account(
                name: "Lisa",
                password: "Test_1234",
                group: "Users"
            );
            Account account5 = new Account(
                name: "Maggie",
                password: "Test_1234",
                group: "Users"
            );
            ImmutableList<Account> accounts = ImmutableList<Account>.Empty;
            accounts = accounts.AddRange(new Account[] { account1, account2, account3, account4, account5 });

            UnattendGenerator generator = new();
            XmlDocument xml = generator.GenerateXml(
                Configuration.Default with
                {
                    LanguageSettings = regionalInteractive ? new InteractiveLanguageSettings() : new UnattendedLanguageSettings(
                        ImageLanguage: generator.Lookup<ImageLanguage>("en-US"),        // Image language
                        LocaleAndKeyboard: new LocaleAndKeyboard(
                            generator.Lookup<UserLocale>("en-US"),                      // User locale
                            generator.Lookup<KeyboardIdentifier>("0000040a")            // Keyboard identifier. DO NOT PREPEND 0409 as it does that automatically
                        ),
                        LocaleAndKeyboard2: null,                                       // Set value to null as DT doesn't support additional layouts
                        LocaleAndKeyboard3: null,                                       // -- same here
                        GeoLocation: generator.Lookup<GeoLocation>("244")),             // Home Location
                    AccountSettings = accountsInteractive ? new InteractiveAccountSettings() : new UnattendedAccountSettings(
                        accounts: accounts,
                        autoLogonSettings: new BuiltinAutoLogonSettings(
                            password: account1.Password),
                        obscurePasswords: true),
                    PartitionSettings = partitionsInteractive ? new InteractivePartitionSettings() : new UnattendedPartitionSettings(
                        PartitionLayout: PartitionLayout.GPT,
                        RecoveryMode: RecoveryMode.Partition),                          // PLEASE MODIFY THIS!!!
                    ComputerNameSettings = randomComputerName ? new RandomComputerNameSettings() : new CustomComputerNameSettings(
                        name: "WIN-NHV7230VJNS"),
                    TimeZoneSettings = timeZoneImplicit ? new ImplicitTimeZoneSettings() : new ExplicitTimeZoneSettings(
                        TimeZone: offset)
                }
                );
            try
            {
                using XmlWriter writer = XmlWriter.Create(targetPath, new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    CloseOutput = true,
                    Indent = true,
                    IndentChars = "\t",
                    NewLineChars = "\r\n",
                });
                xml.Save(writer);
                Console.WriteLine($"Unattended answer file has been generated at \"{targetPath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not generate unattended answer file due to the following error: {ex.Message}");
            }
        }
    }
}
