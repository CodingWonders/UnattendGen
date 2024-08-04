using System;
using System.Xml;
using Schneegans.Unattend;
using System.IO;
using System.Text;
using System.Collections.Immutable;
using System.Reflection;
using UnattendGen.UserSettings;

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

        static void ConfigureDefaultOptions()
        {

        }

        static async Task Main(string[] args)
        {

            string targetPath = "";
            string regionFile = "";
            RegionFile region = new RegionFile();
            RegionFile defaultRegion = new RegionFile();
            defaultRegion.regionLang.Add(new ImageLanguages("en-US", "English (United States)"));
            defaultRegion.regionLocales.Add(new UserLocales("en-US", "English (United States)", "0409", "00000409", "244"));
            defaultRegion.regionKeys.Add(new KeyboardIdentifiers("00000409", "US", "Keyboard"));
            defaultRegion.regionGeo.Add(new GeoIds("244", "United States"));
            defaultRegion.regionTimes.Add(new TimeOffsets("UTC", "(UTC) Coordinated Universal Time"));

            region = defaultRegion;

            Console.WriteLine($"Unattended Answer File Generator, version {Assembly.GetEntryAssembly().GetName().Version.ToString()}");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Program: (c) {GetCopyrightTimespan(2024, DateTime.Today.Year)}. CodingWonders Software\nLibrary: (c) {GetCopyrightTimespan(2024, DateTime.Today.Year)}. Christoph Schneegans");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine("SEE ATTACHED PROGRAM LICENSES FOR MORE INFORMATION REGARDING USE AND REDISTRIBUTION\n");

            var generator = new AnswerFileGenerator();

            if (Environment.GetCommandLineArgs().Length >= 2)
            {
                Console.WriteLine("Parsing command-line switches...\n");
                foreach (string cmdLine in Environment.GetCommandLineArgs())
                {
                    if (cmdLine.StartsWith("/target", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = cmdLine.Replace("/target=", "").Trim();
                    }
                    else if (cmdLine.StartsWith("/regionfile", StringComparison.OrdinalIgnoreCase))
                    {
                        regionFile = cmdLine.Replace("/regionfile=", "").Trim();
                        if (regionFile != "" && File.Exists(regionFile))
                        {
                            region.regionLang = ImageLanguages.LoadItems(regionFile);
                            region.regionGeo = GeoIds.LoadItems(regionFile);
                            region.regionLocales = UserLocales.LoadItems(regionFile);
                            region.regionKeys = KeyboardIdentifiers.LoadItems(regionFile);
                            region.regionTimes = TimeOffsets.LoadItems(regionFile);
                        }
                    }
                }
            }
            //generator.regionalInteractive = false;
            generator.regionalSettings = region;
            generator.randomComputerName = true;
            //generator.timeZoneImplicit = false;
            generator.accountsInteractive = false;
            generator.partitionsInteractive = false;
            await generator.GenerateAnswerFile(targetPath != "" ? targetPath : "unattend.xml");
        }
    }

    public class AnswerFileGenerator
    {
        public bool regionalInteractive;

        public RegionFile regionalSettings = new RegionFile();

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
                        ImageLanguage: generator.Lookup<ImageLanguage>(regionalSettings.regionLang[0].Id),
                        LocaleAndKeyboard: new LocaleAndKeyboard(
                            generator.Lookup<UserLocale>(regionalSettings.regionLocales[0].Id),
                            generator.Lookup<KeyboardIdentifier>(regionalSettings.regionKeys[0].Id)
                        ),
                        LocaleAndKeyboard2: null,
                        LocaleAndKeyboard3: null,
                        GeoLocation: generator.Lookup<GeoLocation>(regionalSettings.regionGeo[0].Id)),
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
