﻿using System;
using System.Xml;
using Schneegans.Unattend;
using System.IO;
using System.Text;
using System.Collections.Immutable;
using System.Reflection;
using UnattendGen.UserSettings;
using System.Diagnostics;

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

        static string ValidateComputerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            if (name.Length > 15)
                return "";

            if (name.ToCharArray().Any(char.IsWhiteSpace))
                return "";

            if (name.ToCharArray().All(char.IsAsciiDigit))
                return "";

            if (name.IndexOfAny(['{', '|', '}', '~', '[', '\\', ']', '^', '\'', ':', ';', '<', '=', '>', '?', '@', '!', '"', '#', '$', '%', '`', '(', ')', '+', '/', '.', ',', '*', '&']) > -1)
                return "";

            return name;
        }

        static void DebugWrite(string msg)
        {
            Console.WriteLine($"DEBUG: {msg}");
        }

        static async Task Main(string[] args)
        {
            bool debugMode = true;

            string targetPath = "";
            bool regionInteractive = false;
            string regionFile = "";
            RegionFile region = new RegionFile();
            RegionFile defaultRegion = new RegionFile();
            defaultRegion.regionLang.Add(new ImageLanguages("en-US", "English (United States)"));
            defaultRegion.regionLocales.Add(new UserLocales("en-US", "English (United States)", "0409", "00000409", "244"));
            defaultRegion.regionKeys.Add(new KeyboardIdentifiers("00000409", "US", "Keyboard"));
            defaultRegion.regionGeo.Add(new GeoIds("244", "United States"));
            defaultRegion.regionTimes.Add(new TimeOffsets("UTC", "(UTC) Coordinated Universal Time"));

            region = defaultRegion;

            string computerName = "";

            AnswerFileGenerator.PartitionSettingsMode partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;

            bool genericChosen = true;
            SystemEdition defaultEdition = new SystemEdition();
            defaultEdition.Id = "pro";
            defaultEdition.DisplayName = "Pro";
            defaultEdition.ProductKey = "VK7JG-NPHTM-C97JM-9MPGT-3V66T";

            bool accountsInteractive = true;

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
                    else if (cmdLine.StartsWith("/region-interactive", StringComparison.OrdinalIgnoreCase))
                    {
                        regionInteractive = true;
                    }
                    else if (cmdLine.StartsWith("/regionfile", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Region file specified. Reading settings...");
                        regionFile = cmdLine.Replace("/regionfile=", "").Trim();
                        if (regionFile != "" && File.Exists(regionFile))
                        {
                            try
                            {
                                region.regionLang = ImageLanguages.LoadItems(regionFile);
                                region.regionGeo = GeoIds.LoadItems(regionFile);
                                region.regionLocales = UserLocales.LoadItems(regionFile);
                                region.regionKeys = KeyboardIdentifiers.LoadItems(regionFile);
                                region.regionTimes = TimeOffsets.LoadItems(regionFile);
                                DebugWrite($"Regional Settings:\n\n\t- Image Language: {region.regionLang[0].Id}\n\t- Locale: {region.regionLocales[0].Id}\n\t- Keyboard: {region.regionKeys[0].Id}\n\t- Geo ID: {region.regionGeo[0].Id}\n\t- Time Offset: {region.regionTimes[0].Id}\n");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse regional settings file. Continuing with default settings...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}");
                                region = defaultRegion;
                            }
                        }
                    }
                    else if (cmdLine.StartsWith("/architecture", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (cmdLine.Replace("/architecture=", "").Trim())
                        {
                            case "x86":
                            case "i386":
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.x86;
                                DebugWrite("Architecture: x86");
                                break;
                            case "x64":
                            case "amd64":
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.amd64;
                                DebugWrite("Architecture: amd64");
                                break;
                            case "arm64":
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.arm64;
                                DebugWrite("Architecture: arm64");
                                break;
                            default:
                                Console.WriteLine($"WARNING: Unknown processor architecture: {cmdLine.Replace("/architecture=", "").Trim()}. Continuing with AMD64...");
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.amd64;
                                break;
                        }
                    }
                    else if (cmdLine.StartsWith("/LabConfig", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("LabConfig: True");
                        generator.SV_LabConfig = true;
                    }
                    else if (cmdLine.StartsWith("/BypassNRO", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("BypassNRO: True");
                        Console.WriteLine($"INFO: BypassNRO setting will be configured. You will be able to use the target file only on Windows 11. Do note that this setting may not work for you on Windows 11 24H2.");
                        generator.SV_BypassNRO = true;
                    }
                    else if (cmdLine.StartsWith("/computername", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = cmdLine.Replace("/computername=", "").Trim();
                        name = ValidateComputerName(name);

                        if (name == "")
                            Console.WriteLine($"WARNING: Computer name \"{cmdLine.Replace("/computername=", "").Trim()}\" is not valid. Continuing with a random computer name...");

                        DebugWrite($"Computer name: {name}");

                        computerName = name;
                    }
                    else if (cmdLine.StartsWith("/tzImplicit", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("Time Zone is now implicit (determine from Regional Settings - See Respective Settings For More Info!!!)");
                        generator.timeZoneImplicit = true;
                    }
                    else if (cmdLine.StartsWith("/partmode", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (cmdLine.Replace("/partmode=", "").Trim())
                        {
                            case "interactive":
                                partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;
                                break;
                            case "unattended":
                                Console.WriteLine("INFO: Selected partition mode is unattended. Reading settings...");
                                partition = AnswerFileGenerator.PartitionSettingsMode.Unattended;
                                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unattPartSettings.xml")))
                                {
                                    try
                                    {
                                        DiskZeroSettings? diskZero = DiskZeroSettings.LoadDiskSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unattPartSettings.xml"));
                                        generator.diskZeroSettings = diskZero;
                                        DebugWrite($"Disk 0 settings:\n\n\t- Partition Style: {diskZero.partStyle.ToString()}\n\t- Install Recovery Environment? {(diskZero.recoveryEnvironment != DiskZeroSettings.RecoveryEnvironmentMode.None ? $"Yes\n\t\t- Location: {diskZero.recoveryEnvironment.ToString()}\n\t{(diskZero.partStyle == DiskZeroSettings.PartitionStyle.GPT ? $"- EFI System Partition Size: {diskZero.ESPSize} MB\n\t" : "")}" : "No")}- Recovery Partition Size: {diskZero.recEnvSize} MB\n");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("WARNING: Could not parse partition settings file. Continuing with Interactive...");
                                        if (Debugger.IsAttached)
                                            Debugger.Break();
                                        DebugWrite($"Error Message - {ex.Message}");
                                        partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("WARNING: Partition settings file does not exist. Continuing with Interactive...");
                                    partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;
                                }
                                break;
                            case "custom":
                                Console.WriteLine("INFO: Selected partition mode is custom (use DiskPart script). Reading settings...");
                                partition = AnswerFileGenerator.PartitionSettingsMode.Custom;
                                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiskPartSettings.xml")))
                                {
                                    try
                                    {
                                        DiskPartSettings? diskPart = DiskPartSettings.LoadDiskSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiskPartSettings.xml"));
                                        generator.diskPartSettings = diskPart;
                                        DebugWrite($"DiskPart settings:\n\n\t- Script file: \"{diskPart.scriptFile}\". Contents:\n\n{File.ReadAllText(diskPart.scriptFile)}\n\n\t- Automatic configuration? {(diskPart.automaticInstall ? "Yes" : $"No\n\t\t- Disk: {diskPart.diskNum}\n\t\t- Partition: {diskPart.partNum}")}\n");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("WARNING: Could not parse partition settings file. Continuing with Interactive...");
                                        if (Debugger.IsAttached)
                                            Debugger.Break();
                                        DebugWrite($"Error Message - {ex.Message}");
                                        partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("WARNING: Partition settings file does not exist. Continuing with Interactive...");
                                    partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;
                                }
                                break;
                            default:
                                Console.WriteLine($"WARNING: Unknown partition mode: {cmdLine.Replace("/partmode=", "").Trim()}. Continuing with Interactive...");
                                partition = AnswerFileGenerator.PartitionSettingsMode.Interactive;
                                break;
                        }
                    }
                    else if (cmdLine.StartsWith("/generic", StringComparison.OrdinalIgnoreCase))
                    {
                        generator.genericEdition = defaultEdition;
                        Console.WriteLine("INFO: The unattended answer file will use a generic product key. Reading edition configuration...");
                        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edition.xml")))
                        {
                            try
                            {
                                SystemEdition edition = SystemEdition.LoadSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edition.xml"));
                                generator.genericEdition = edition;
                                DebugWrite($"Edition settings:\n\n\t- Edition ID: {edition.Id}\n\t- Edition name: {edition.DisplayName}\n\t- Product key: {edition.ProductKey}\n");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse edition settings file. Continuing with default Pro edition...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}");
                                generator.genericEdition = defaultEdition;
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Edition settings file does not exist. Continuing with default Pro edition...");
                            generator.genericEdition = defaultEdition;
                        }
                    }
                    else if (cmdLine.StartsWith("/customkey", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: The unattended answer file will not use a generic product key");
                        genericChosen = false;
                        string key = cmdLine.Replace("/customkey=", "").Trim();
                        DebugWrite($"Edition settings:\n\n\t- Product key: {key}\n");
                        generator.customKey = key;
                    }
                    else if (cmdLine.StartsWith("/customusers", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Manual user configuration will be used. Reading user list...");
                        accountsInteractive = false;
                        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userAccounts.xml")))
                        {
                            try
                            {
                                List<UserAccount> accounts = UserAccount.LoadAccounts(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userAccounts.xml"));
                                generator.accounts = accounts;
                                DebugWrite($"User accounts:\n");
                                if (accounts.Count > 0)
                                {
                                    foreach (UserAccount account in accounts)
                                    {
                                        Console.WriteLine($"\t- User {accounts.IndexOf(account) + 1}:");
                                        Console.WriteLine($"\t\t- Enabled? {(account.Enabled ? "Yes" : "No")}");
                                        if (account.Enabled)
                                        {
                                            Console.WriteLine($"\t\t- Name: {account.Name}");
                                            Console.WriteLine($"\t\t- Password: {account.Password}");
                                            Console.WriteLine($"\t\t- Group: {account.Group switch { 
                                                UserAccount.UserGroup.Administrators => "Administrators",
                                                UserAccount.UserGroup.Users => "Users"
                                            }}");
                                        }
                                    }
                                    Console.WriteLine();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse user accounts file. Continuing with default Pro edition...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}");
                                accountsInteractive = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Edition settings file does not exist. Continuing with default Pro edition...");
                            accountsInteractive = true;                            
                        }
                    }
                    if (cmdLine != Assembly.GetExecutingAssembly().Location && debugMode)
                        DebugWrite($"Successfully parsed command-line switch {cmdLine}");
                }
            }
            generator.regionalInteractive = regionInteractive;
            generator.regionalSettings = region;
            generator.randomComputerName = (computerName == "");
            generator.computerName = computerName;
            generator.accountsInteractive = accountsInteractive;
            generator.partitionSettings = partition;
            generator.editionGenericChosen = genericChosen;
            if (generator.genericEdition is null)
            {
                Console.WriteLine("WARNING: No edition settings have been specified. Continuing with the default Pro edition...");
                generator.genericEdition = defaultEdition;
            }
            if (generator.accounts is null)
            {
                Console.WriteLine("WARNING: No users have been specified. Continuing with Interactive settings...");
                generator.accountsInteractive = true;
            }

            await generator.GenerateAnswerFile(targetPath != "" ? targetPath : "unattend.xml");
        }
    }

    public class AnswerFileGenerator
    {

        public enum PartitionSettingsMode
        {
            Interactive,
            Unattended,
            Custom
        }

        public bool regionalInteractive;

        public RegionFile regionalSettings = new RegionFile();

        public bool randomComputerName;

        public string computerName = "";

        public Schneegans.Unattend.ProcessorArchitecture architecture = Schneegans.Unattend.ProcessorArchitecture.amd64;

        public bool SV_LabConfig;

        public bool SV_BypassNRO;

        public bool timeZoneImplicit;

        public PartitionSettingsMode partitionSettings;

        public DiskZeroSettings? diskZeroSettings;

        public DiskPartSettings? diskPartSettings;

        public bool editionGenericChosen;

        public SystemEdition? genericEdition;

        public string? customKey;

        public bool accountsInteractive;

        public List<UserAccount>? accounts;

        public async Task GenerateAnswerFile(string targetPath)
        {
            // follow example for now, document settings for later DT integration

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
            ImmutableList<Account> userAccounts = ImmutableList<Account>.Empty;
            List<Account> accountList = new List<Account>();

            if (null != accounts && accounts.Count > 0)
            {
                foreach (UserAccount account in accounts)
                {
                    if (!account.Enabled)
                        continue;
                    accountList.Add(new Account(
                        name: account.Name,
                        password: account.Password,
                        group: account.Group switch
                        {
                            UserAccount.UserGroup.Administrators => "Administrators",
                            UserAccount.UserGroup.Users => "Users"
                        }));
                }
            }

            userAccounts = userAccounts.AddRange(accountList.ToArray());

            ImmutableHashSet<Schneegans.Unattend.ProcessorArchitecture> architectures = ImmutableHashSet<Schneegans.Unattend.ProcessorArchitecture>.Empty;
            architectures = architectures.Add(architecture);

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
                        accounts: userAccounts,
                        autoLogonSettings: new BuiltinAutoLogonSettings(
                            password: account1.Password),
                        obscurePasswords: true),
                    PartitionSettings = partitionSettings switch
                    {
                        PartitionSettingsMode.Interactive => new InteractivePartitionSettings(),
                        PartitionSettingsMode.Unattended => new UnattendedPartitionSettings(
                            PartitionLayout: diskZeroSettings.partStyle switch
                            {
                                DiskZeroSettings.PartitionStyle.GPT => PartitionLayout.GPT,
                                DiskZeroSettings.PartitionStyle.MBR => PartitionLayout.MBR,
                                _ => PartitionLayout.GPT
                            },
                            RecoveryMode: diskZeroSettings.recoveryEnvironment switch
                            {
                                DiskZeroSettings.RecoveryEnvironmentMode.None => RecoveryMode.None,
                                DiskZeroSettings.RecoveryEnvironmentMode.Partition => RecoveryMode.Partition,
                                DiskZeroSettings.RecoveryEnvironmentMode.Windows => RecoveryMode.Folder,
                                _ => RecoveryMode.Partition
                            },
                            EspSize: diskZeroSettings.ESPSize,
                            RecoverySize: diskZeroSettings.recEnvSize),
                        PartitionSettingsMode.Custom => new CustomPartitionSettings(
                            Script: File.ReadAllText(diskPartSettings.scriptFile),
                            InstallTo: diskPartSettings.automaticInstall switch
                            {
                                true => new AvailableInstallToSettings(),
                                false => new CustomInstallToSettings(
                                    installToDisk: diskPartSettings.diskNum,
                                    installToPartition: diskPartSettings.partNum)
                            }),
                        _ => new InteractivePartitionSettings()
                    },
                    EditionSettings = editionGenericChosen ? new UnattendedEditionSettings(
                        Edition: new WindowsEdition(
                            id: genericEdition.Id,
                            displayName: genericEdition.DisplayName,
                            productKey: genericEdition.ProductKey,
                            visible: true)) : new DirectEditionSettings(
                                productKey: customKey),
                    ComputerNameSettings = randomComputerName ? new RandomComputerNameSettings() : new CustomComputerNameSettings(
                        name: computerName),
                    TimeZoneSettings = timeZoneImplicit ? new ImplicitTimeZoneSettings() : new ExplicitTimeZoneSettings(
                        TimeZone: new TimeOffset(regionalSettings.regionTimes[0].Id, regionalSettings.regionTimes[0].DisplayName)),
                    ProcessorArchitectures = architectures,
                    BypassRequirementsCheck = SV_LabConfig,
                    BypassNetworkCheck = SV_BypassNRO
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
                Console.WriteLine($"\nUnattended answer file has been generated at \"{targetPath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nCould not generate unattended answer file due to the following error: {ex.Message}");
            }
        }
    }
}
