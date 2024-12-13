using System;
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
                return $"{start.ToString()}-{current.ToString()}";
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

        static void DebugWrite(string msg, bool debugMsg = false)
        {
            if (debugMsg)
                Console.WriteLine($"DEBUG: {msg}");
        }

        static void ShowHelpMessage()
        {
            Console.WriteLine("=== PROGRAM HELP ===\n");
            Console.WriteLine("USAGE\n\n" +
                "\tUnattendGen [/target=<targetPath>] [/regionfile=<regionFile>] [/architecture={ x86 ; i386 | x64 ; amd64 | aarch64 ; arm64 }] [/LabConfig] [/BypassNRO] [/ConfigSet] [/computername=<compName>] [/tzImplicit] [/partmode={ interactive | unattended | custom }] [/generic | /customkey=<key>] [/msa] [/customusers] [/autologon={ firstadmin | builtinadmin }] [/b64obscure] [/pwExpire=<days>] [/lockout={ yes | no } [/vm={ vbox_gas | vmware | virtio }] [/wifi={ yes | no }] [/telem={ yes | no }] [/customcomponents]\n");
            Console.WriteLine("SWITCHES\n\n" +
                "\tGeneral switches:\n\n" +
                "\t\t/?         \t\tShows this help screen\n" +
                "\t\t/target    \t\tSaves the unattended answer file to the path specified by <targetPath>. Defaults to \"unattend.xml\" in the current directory if not set.\n\n" +
                "\tRegional settings:\n\n" +
                "\t\t/regionfile\t\tConfigures regional settings given a XML file specified by <regionFile>. Defaults to Interactive regional settings if not set.\n\n" +
                "\tBasic system settings:\n\n" +
                "\t\t/architecture\t\tConfigures the system architecture of the target answer file. Possible values: x86, i386 (Desktop 32-Bit); x64, amd64 (Desktop 64-Bit); aarch64, arm64 (Windows on ARM). Defaults to amd64 if not set\n" +
                "\t\t/LabConfig\t\tBypasses system requirement checks (Windows 11 only)\n" +
                "\t\t/BypassNRO\t\tBypasses mandatory network connection setup (Windows 11 only, may not work on Windows 11 24H2)\n" +
                "\t\t/ConfigSet\t\tConfigures the target system to use a configuration set or distribution share. Said set or share needs to be present in the ISO you copy the answer file to beforehand\n" +
                "\t\t/computername\t\tSets a computer name defined by <compName>. Defaults to a random computer name if not set\n\n" +
                "\tTime zone settings:\n\n" +
                "\t\t/tzImplicit\t\tSets the system time zone to be determined from regional settings. Defaults to time zone settings from the regional settings file if not set\n\n" +
                "\tDisk configuration settings:\n\n" +
                "\t\t/partmode\t\tSets the partitioning mode. Possible values: interactive (ask during system setup); unattended (configure settings of Disk 0); custom (use a DiskPart script). Defaults to interactive if not set\n\n" +
                "\tEdition settings: (USE EITHER SWITCH BUT NOT BOTH)\n\n" +
                "\t\t/generic\t\tSets generic edition settings using a configuration file. Defaults to Pro edition if not set\n" +
                "\t\t/customkey\t\tSets a custom key, defined by <key> to be used for installation, which may or may not be valid\n\n" +
                "\tUser settings:\n\n" +
                "\t\t/msa     \t\tConfigures the target system to ask for a Microsoft account. No additional user account parameters need to be passed, or the system will not ask for the online account\n" +
                "\t\t/customusers\t\tConfigures the users of the target system with a \"userAccounts.xml\" configuration file. Defaults to an interactive setup if not specified\n" +
                "\t\t/autologon\t\tConfigures user automatic log-on settings. Possible values: firstadmin (first admin in account list); builtinadmin (built-in Windows admin account). Defaults to disabled auto log-on if not set\n" +
                "\t\t/b64obscure\t\tObscures passwords with Base64\n" +
                "\t\t/pwExpire\t\tConfigures password expiration settings (not recommended by NIST) given the value defined in <days>. Defaults to no password expiration if not set\n" +
                "\t\t/lockout\t\tConfigures account lockout settings. Possible values: yes (enable settings determined by a config file); no (disable settings - NOT RECOMMENDED)\n\n" +
                "\tVirtual Machine Support:\n\n" +
                "\t\t/vm        \t\tConfigures virtual machine support. Possible values: vbox_gas (VirtualBox Guest Additions); vmware (VMware Tools); virtio (VirtIO Guest Tools). Defaults to no VM support if not set\n\n" +
                "\tWireless settings:\n\n" +
                "\t\t/wifi    \t\tConfigures wireless networking for the target system. Possible values: yes (configure settings with a wireless configuration file); no (skip configuration). Defaults to interactive if not set\n\n" +
                "\tSystem telemetry:\n\n" +
                "\t\t/telem     \t\tConfigures system telemetry. Possible values: yes (enable telemetry); no (disable telemetry). Defaults to interactive if not set\n\n" +
                "\tCustom configuration:\n\n" +
                "\t\t/customcomponents\tConfigures custom components for your unattended answer file using a \"components.xml\" configuration file");
        }

        static async Task Main(string[] args)
        {
            bool debugMode = false;

            string targetPath = "";
            bool regionInteractive = true;
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
            SystemEdition defaultEdition = new SystemEdition("pro", "Pro", "VK7JG-NPHTM-C97JM-9MPGT-3V66T");

            bool accountsInteractive = true;

            AutoLogon defaultLogonSettings = new AutoLogon(AutoLogon.AutoLogonMode.None, "");
            AutoLogon logonSettings = new AutoLogon();

            logonSettings = defaultLogonSettings;

            AccountLockout defaultlockout = new AccountLockout(true, 10, 10, 10);
            AccountLockout lockout = new AccountLockout();

            lockout = defaultlockout;

            AnswerFileGenerator.VirtualMachineSolution vm = AnswerFileGenerator.VirtualMachineSolution.No;

            bool wirelessInteractive = true;
            bool wirelessSkip = false;
            WirelessNetwork wirelessNetwork = new WirelessNetwork();

            AnswerFileGenerator.SystemTelemetry telemetry = AnswerFileGenerator.SystemTelemetry.Interactive;

            List<SystemComponent> defaultComponents = new List<SystemComponent>();
            // Add Microsoft-Windows-Shell-Setup in oobeSystem pass. It's already filled in, but add it anyway
            List<SystemPass> defaultPasses = new List<SystemPass>();
            defaultPasses.Add(new SystemPass("oobeSystem"));
            SystemComponent defaultComponent = new SystemComponent("Microsoft-Windows-Shell-Setup", defaultPasses);
            defaultComponents.Add(defaultComponent);

            Console.WriteLine($"UnattendGen{(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DT")) ? " for DISMTools" : "")}, version {Assembly.GetEntryAssembly().GetName().Version.ToString()}");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Program: (c) {GetCopyrightTimespan(2024, DateTime.Today.Year)}. CodingWonders Software\nLibrary: (c) {GetCopyrightTimespan(2024, DateTime.Today.Year)}. Christoph Schneegans");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine("SEE ATTACHED PROGRAM LICENSES FOR MORE INFORMATION REGARDING USE AND REDISTRIBUTION\n");

            var generator = new AnswerFileGenerator();

            if (Environment.GetCommandLineArgs().Contains("/debug"))
                debugMode = true;

            if (Environment.GetCommandLineArgs().Length >= 2)
            {
                foreach (string cmdLine in Environment.GetCommandLineArgs())
                {
                    if (cmdLine == "/?")
                    {
                        ShowHelpMessage();
                        return;
                    }
                    else if (cmdLine.StartsWith("/target", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = cmdLine.Replace("/target=", "").Trim();
                    }
                    else if (cmdLine.StartsWith("/regionfile", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Region file specified. Reading settings...");
                        regionInteractive = false;
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
                                DebugWrite($"Regional Settings:\n\n\t- Image Language: {region.regionLang[0].Id}\n\t- Locale: {region.regionLocales[0].Id}\n\t- Keyboard: {region.regionKeys[0].Id}\n\t- Geo ID: {region.regionGeo[0].Id}\n\t- Time Offset: {region.regionTimes[0].Id}\n", (debugMode | Debugger.IsAttached));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse regional settings file. Continuing with Interactive...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
                                region = defaultRegion;
                                regionInteractive = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Regional settings file does not exist. Continuing with Interactive...");
                            regionInteractive = true;
                        }
                    }
                    else if (cmdLine.StartsWith("/architecture", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (cmdLine.Replace("/architecture=", "").Trim())
                        {
                            case "x86":
                            case "i386":
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.x86;
                                DebugWrite("Architecture: x86", (debugMode | Debugger.IsAttached));
                                break;
                            case "x64":
                            case "amd64":
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.amd64;
                                DebugWrite("Architecture: amd64", (debugMode | Debugger.IsAttached));
                                break;
                            case "aarch64":
                            case "arm64":
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.arm64;
                                DebugWrite("Architecture: arm64", (debugMode | Debugger.IsAttached));
                                break;
                            default:
                                Console.WriteLine($"WARNING: Unknown processor architecture: {cmdLine.Replace("/architecture=", "").Trim()}. Continuing with AMD64...");
                                generator.architecture = Schneegans.Unattend.ProcessorArchitecture.amd64;
                                break;
                        }
                    }
                    else if (cmdLine.StartsWith("/LabConfig", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("LabConfig: True", (debugMode | Debugger.IsAttached));
                        generator.SV_LabConfig = true;
                    }
                    else if (cmdLine.StartsWith("/BypassNRO", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("BypassNRO: True", (debugMode | Debugger.IsAttached));
                        Console.WriteLine($"INFO: BypassNRO setting will be configured. You will be able to use the target file only on Windows 11. Do note that this setting may not work for you on Windows 11 24H2.");
                        generator.SV_BypassNRO = true;
                    }
                    else if (cmdLine.StartsWith("/ConfigSet", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("Windows SIM Configuration Set: True", (debugMode | Debugger.IsAttached));
                        generator.UseConfigSet = true;
                    }
                    else if (cmdLine.StartsWith("/computername", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = cmdLine.Replace("/computername=", "").Trim();
                        name = ValidateComputerName(name);

                        if (name == "")
                            Console.WriteLine($"WARNING: Computer name \"{cmdLine.Replace("/computername=", "").Trim()}\" is not valid. Continuing with a random computer name...");

                        DebugWrite($"Computer name: {name}", (debugMode | Debugger.IsAttached));

                        computerName = name;
                    }
                    else if (cmdLine.StartsWith("/tzImplicit", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("Time Zone is now implicit (determine from Regional Settings - See Respective Settings For More Info!!!)", (debugMode | Debugger.IsAttached));
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
                                        DebugWrite($"Disk 0 settings:\n\n\t- Partition Style: {diskZero.partStyle.ToString()}\n\t- Install Recovery Environment? {(diskZero.recoveryEnvironment != DiskZeroSettings.RecoveryEnvironmentMode.None ? $"Yes\n\t\t- Location: {diskZero.recoveryEnvironment.ToString()}\n\t{(diskZero.partStyle == DiskZeroSettings.PartitionStyle.GPT ? $"- EFI System Partition Size: {diskZero.ESPSize} MB\n\t" : "")}" : "No")}- Recovery Partition Size: {diskZero.recEnvSize} MB\n", (debugMode | Debugger.IsAttached));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("WARNING: Could not parse partition settings file. Continuing with Interactive...");
                                        if (Debugger.IsAttached)
                                            Debugger.Break();
                                        DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
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
                                        DebugWrite($"DiskPart settings:\n\n\t- Script file: \"{diskPart.scriptFile}\". Contents:\n\n{File.ReadAllText(diskPart.scriptFile)}\n\n\t- Automatic configuration? {(diskPart.automaticInstall ? "Yes" : $"No\n\t\t- Disk: {diskPart.diskNum}\n\t\t- Partition: {diskPart.partNum}")}\n", (debugMode | Debugger.IsAttached));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("WARNING: Could not parse partition settings file. Continuing with Interactive...");
                                        if (Debugger.IsAttached)
                                            Debugger.Break();
                                        DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
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
                                DebugWrite($"Edition settings:\n\n\t- Edition ID: {edition.Id}\n\t- Edition name: {edition.DisplayName}\n\t- Product key: {edition.ProductKey}\n", (debugMode | Debugger.IsAttached));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse edition settings file. Continuing with default Pro edition...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
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
                        DebugWrite($"Edition settings:\n\n\t- Product key: {key}\n", (debugMode | Debugger.IsAttached));
                        generator.customKey = key;
                    }
                    else if (cmdLine.StartsWith("/msa", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("The system will ask you for a Microsoft account. 24H2 does not present ways to bypass this with bypassnro, unless you join a domain", (debugMode | Debugger.IsAttached));
                        generator.msaInteractive = true;
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
                                DebugWrite($"User accounts:\n", (debugMode | Debugger.IsAttached));
                                if (debugMode | Debugger.IsAttached)
                                {
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
                                                Console.WriteLine($"\t\t- Group: {account.Group switch
                                                {
                                                    UserAccount.UserGroup.Administrators => "Administrators",
                                                    UserAccount.UserGroup.Users => "Users",
                                                    _ => "Users"
                                                }}");
                                            }
                                        }
                                        Console.WriteLine();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse user accounts file. Continuing with Interactive Settings...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
                                accountsInteractive = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARNING: User accounts file does not exist. Continuing with Interactive Settings...");
                            accountsInteractive = true;                            
                        }
                    }
                    else if (cmdLine.StartsWith("/autologon", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!accountsInteractive)
                        {
                            Console.WriteLine("INFO: Configuring auto-logon settings...");
                            switch (cmdLine.Replace("/autologon=", "").Trim())
                            {
                                case "firstadmin":
                                    DebugWrite("Setting auto-logon to first admin...", (debugMode | Debugger.IsAttached));
                                    logonSettings.logonMode = AutoLogon.AutoLogonMode.FirstAdmin;
                                    if (generator.accounts.Count > 0)
                                    {
                                        foreach (UserAccount account in generator.accounts)
                                        {
                                            if (account.Group == UserAccount.UserGroup.Administrators)
                                            {
                                                DebugWrite($"First Admin in Accounts list: {account.Name}", (debugMode | Debugger.IsAttached));
                                                break;
                                            }
                                        }
                                    }
                                    break;
                                case "builtinadmin":
                                    DebugWrite("Setting auto-logon to Windows admin...", (debugMode | Debugger.IsAttached));
                                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autoLogon.xml")))
                                    {
                                        try
                                        {
                                            logonSettings.winAdminPass = AutoLogon.GetAdminPassword(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autoLogon.xml"));
                                            logonSettings.logonMode = AutoLogon.AutoLogonMode.BuiltInAdmin;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("WARNING: Could not parse auto-logon settings file. Disabling auto-logon...");
                                            if (Debugger.IsAttached)
                                                Debugger.Break();
                                            DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
                                            logonSettings.logonMode = AutoLogon.AutoLogonMode.None;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("WARNING: Auto-logon settings file does not exist. Disabling auto-logon...");
                                        logonSettings.logonMode = AutoLogon.AutoLogonMode.None;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("INFO: Auto-logon settings will not be configured since you need to configure accounts during Setup. Please pass the \"/customusers\" flag after providing a user data file with the name of \"userAccounts.xml\" to be able to configure these settings");
                        }
                    }
                    else if (cmdLine.StartsWith("/b64obscure", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugWrite("User passwords will be obscured with Base64", (debugMode | Debugger.IsAttached));
                        generator.Base64Obscure = true;
                    }
                    else if (cmdLine.StartsWith("/pwExpire", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            Console.WriteLine("INFO: Configuring password expiration settings...");
                            generator.ExpirationDays = Convert.ToInt32(cmdLine.Replace("/pwExpire=", "").Trim());
                            DebugWrite($"Password expiration: {generator.ExpirationDays} day(s)", (debugMode | Debugger.IsAttached));
                        }
                        catch
                        {
                            generator.ExpirationDays = 0;
                        }
                    }
                    else if (cmdLine.StartsWith("/lockout", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (cmdLine.Replace("/lockout=", "").Trim())
                        {
                            case "yes":
                                Console.WriteLine("INFO: Enforcing Account Lockout policy...");
                                lockout.Enabled = true;
                                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockout.xml")))
                                {
                                    Console.WriteLine("INFO: Lockout policy file detected. Reading settings...");
                                    try
                                    {
                                        lockout = AccountLockout.GetAccountLockout(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockout.xml"));
                                        DebugWrite($"Account Lockout Settings:\n\n\tAfter {lockout.FailedAttempts} attempt(s) within {lockout.TimeFrame} minute(s), unlock accounts automatically after {lockout.AutoUnlock} minute(s)\n", (debugMode | Debugger.IsAttached));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("WARNING: Could not parse Account Lockout settings file. Continuing with default options...");
                                        if (Debugger.IsAttached)
                                            Debugger.Break();
                                        DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
                                        lockout = defaultlockout;
                                    }
                                }
                                break;
                            case "no":
                                Console.WriteLine("INFO: Disabling Account Lockout policy. User accounts may be easier to penetrate into with brute-force attacks");
                                lockout.Enabled = false;
                                break;
                            default:

                                break;
                        }
                    }
                    else if (cmdLine.StartsWith("/vm", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Configuring Virtual Machine Support...");
                        switch (cmdLine.Replace("/vm=", "").Trim())
                        {
                            case "vbox_gas":
                                DebugWrite("VM Solution: VirtualBox Guest Additions", (debugMode | Debugger.IsAttached));
                                vm = AnswerFileGenerator.VirtualMachineSolution.VBox_GAs;
                                break;
                            case "vmware":
                                DebugWrite("VM Solution: VMware Tools", (debugMode | Debugger.IsAttached));
                                vm = AnswerFileGenerator.VirtualMachineSolution.VMware_Tools;
                                break;
                            case "virtio":
                                DebugWrite("VM Solution: VirtIO Guest Tools", (debugMode | Debugger.IsAttached));
                                vm = AnswerFileGenerator.VirtualMachineSolution.VirtIO;
                                break;
                            default:
                                Console.WriteLine($"WARNING: Unknown VM solution: {cmdLine.Replace("/vm=", "").Trim()}. Continuing without VM support...");
                                break;                                
                        }
                    }
                    else if (cmdLine.StartsWith("/wifi", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Configuring Wireless Networks...");
                        switch (cmdLine.Replace("/wifi=", "").Trim())
                        {
                            case "yes":
                                Console.WriteLine("INFO: Wireless settings will be configured. Reading configuration file...");
                                wirelessInteractive = false;
                                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wireless.xml")))
                                {
                                    try
                                    {
                                        WirelessNetwork wireless = WirelessNetwork.LoadSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wireless.xml"));
                                        wirelessNetwork = wireless;
                                        DebugWrite($"Wireless settings:\n", (debugMode | Debugger.IsAttached));
                                        if (debugMode | Debugger.IsAttached)
                                        {
                                            Console.WriteLine($"\t- SSID: {wireless.SSID}");
                                            Console.WriteLine($"\t- Password: {new string('*', wireless.Password.Length)} (hidden for your security)");
                                            Console.WriteLine($"\t- Authentication mode: {wireless.Authentication switch
                                            {
                                                WirelessNetwork.AuthenticationProtocol.Open => "Open (most vulnerable)",
                                                WirelessNetwork.AuthenticationProtocol.WPA2 => "WPA2-PSK",
                                                WirelessNetwork.AuthenticationProtocol.WPA3 => "WPA3-SAE",
                                                _ => "WPA2-PSK"
                                            }}");
                                            Console.WriteLine($"\t- Connect even if not broadcasting? {(wireless.NonBroadcast ? "Yes" : "No")}");
                                            Console.WriteLine();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("WARNING: Could not parse wireless settings file. Continuing with Interactive Settings...");
                                        if (Debugger.IsAttached)
                                            Debugger.Break();
                                        DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
                                        wirelessInteractive = true;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("WARNING: Wireless settings file does not exist. Continuing with Interactive Settings...");
                                    wirelessInteractive = true;                            
                                }
                                break;
                            case "no":
                                wirelessSkip = true;
                                break;
                        }
                    }
                    else if (cmdLine.StartsWith("/telem", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Configuring system telemetry settings...");
                        switch (cmdLine.Replace("/telem=", "").Trim())
                        {
                            case "yes":
                                DebugWrite("Enabling system telemetry...", (debugMode | Debugger.IsAttached));
                                telemetry = AnswerFileGenerator.SystemTelemetry.Yes;
                                break;
                            case "no":
                                DebugWrite("(Attempting to) disable system telemetry...", (debugMode | Debugger.IsAttached));
                                telemetry = AnswerFileGenerator.SystemTelemetry.No;
                                break;
                            default:
                                Console.WriteLine($"WARNING: Unknown telemetry configuration: {cmdLine.Replace("/telem=", "").Trim()}. Continuing with Interactive settings...");
                                telemetry = AnswerFileGenerator.SystemTelemetry.Interactive;
                                break;
                        }
                    }
                    else if (cmdLine.StartsWith("/customcomponents", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("INFO: Configuring custom components...");
                        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "components.xml")))
                        {
                            try
                            {
                                List<SystemComponent> components = SystemComponent.LoadComponents(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "components.xml"));
                                generator.SystemComponents = components;
                                DebugWrite($"System components:\n", (debugMode | Debugger.IsAttached));
                                if (debugMode | Debugger.IsAttached)
                                {
                                    if (components.Count > 0)
                                    {
                                        foreach (SystemComponent component in components)
                                        {
                                            Console.WriteLine($"\t- Component name: {component.Id}");
                                            Console.WriteLine($"\t\t- Passes:");
                                            foreach (SystemPass pass in component.Passes)
                                            {
                                                Console.WriteLine($"\t\t\t- \"{pass.Name}\"");
                                            }
                                        }
                                    }
                                    Console.WriteLine();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("WARNING: Could not parse system components file. Continuing without settings...");
                                if (Debugger.IsAttached)
                                    Debugger.Break();
                                DebugWrite($"Error Message - {ex.Message}", (debugMode | Debugger.IsAttached));
                                generator.SystemComponents = defaultComponents;
                            }
                        }
                        else
                        {
                            generator.SystemComponents = defaultComponents;
                        }
                    }
                    if (cmdLine != Assembly.GetExecutingAssembly().Location)
                        DebugWrite($"Successfully parsed command-line switch {cmdLine}", (debugMode | Debugger.IsAttached));
                }
            }
            generator.regionalInteractive = regionInteractive;
            generator.regionalSettings = region;
            generator.randomComputerName = (computerName == "");
            generator.computerName = computerName;
            generator.accountsInteractive = accountsInteractive;
            generator.partitionSettings = partition;
            generator.editionGenericChosen = genericChosen;
            generator.autoLogonSettings = logonSettings;
            generator.lockout = lockout;
            generator.virtualMachine = vm;
            generator.WirelessInteractive = wirelessInteractive;
            generator.WirelessSkip = wirelessSkip;
            generator.WirelessSettings = wirelessNetwork;
            generator.Telemetry = telemetry;
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

        public enum VirtualMachineSolution
        {
            No,
            VBox_GAs,
            VMware_Tools,
            VirtIO
        }

        public enum SystemTelemetry
        {
            Interactive,
            No,
            Yes
        }

        public bool regionalInteractive;

        public RegionFile regionalSettings = new RegionFile();

        public bool randomComputerName;

        public string computerName = "";

        public Schneegans.Unattend.ProcessorArchitecture architecture = Schneegans.Unattend.ProcessorArchitecture.amd64;

        public bool SV_LabConfig;

        public bool SV_BypassNRO;

        public bool UseConfigSet;

        public bool timeZoneImplicit;

        public PartitionSettingsMode partitionSettings;

        public DiskZeroSettings? diskZeroSettings;

        public DiskPartSettings? diskPartSettings;

        public bool editionGenericChosen;

        public SystemEdition? genericEdition;

        public string? customKey;

        public bool accountsInteractive;

        public bool msaInteractive;

        public List<UserAccount>? accounts;

        public AutoLogon? autoLogonSettings;

        public bool Base64Obscure;

        public int ExpirationDays = 0;

        public AccountLockout? lockout;

        public VirtualMachineSolution virtualMachine;

        public bool WirelessInteractive;

        public bool WirelessSkip;

        public WirelessNetwork? WirelessSettings;

        public SystemTelemetry Telemetry;

        public List<SystemComponent>? SystemComponents = new List<SystemComponent>();

        public async Task GenerateAnswerFile(string targetPath)
        {

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
                            UserAccount.UserGroup.Users => "Users",
                            _ => "Users"
                        }));
                }
            }

            userAccounts = userAccounts.AddRange(accountList.ToArray());

            ImmutableHashSet<Schneegans.Unattend.ProcessorArchitecture> architectures = ImmutableHashSet<Schneegans.Unattend.ProcessorArchitecture>.Empty;
            architectures = architectures.Add(architecture);

            var componentDictionary = ImmutableDictionary.Create<string, ImmutableSortedSet<Pass>>();
            
            foreach (SystemComponent component in SystemComponents)
            {
                var passSet = ImmutableSortedSet.CreateBuilder<Pass>();

                foreach (SystemPass componentPass in component.Passes)
                {
                    passSet.Add(componentPass.Name switch
                    {
                        "offlineServicing" => Pass.offlineServicing,
                        "windowsPE" => Pass.windowsPE,
                        "generalize" => Pass.generalize,
                        "specialize" => Pass.specialize,
                        "auditSystem" => Pass.auditSystem,
                        "auditUser" => Pass.auditUser,
                        "oobeSystem" => Pass.oobeSystem,
                        _ => Pass.oobeSystem        // Default to oobeSystem. This is the most unlikely case
                    });

                }

                componentDictionary = componentDictionary.Add(component.Id, passSet.ToImmutable());

            }

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
                    AccountSettings = accountsInteractive ? (msaInteractive ? new InteractiveMicrosoftAccountSettings() : new InteractiveLocalAccountSettings()) : new UnattendedAccountSettings(
                        accounts: userAccounts,
                        autoLogonSettings: autoLogonSettings.logonMode switch
                        {
                            AutoLogon.AutoLogonMode.None => new NoneAutoLogonSettings(),
                            AutoLogon.AutoLogonMode.FirstAdmin => new OwnAutoLogonSettings(),
                            AutoLogon.AutoLogonMode.BuiltInAdmin => new BuiltinAutoLogonSettings(
                                password: autoLogonSettings.winAdminPass),
                            _ => new NoneAutoLogonSettings()
                        },
                        obscurePasswords: Base64Obscure),
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
                    LockoutSettings = lockout.Enabled ? new CustomLockoutSettings(
                        lockoutThreshold: lockout.FailedAttempts,
                        lockoutWindow: lockout.TimeFrame,
                        lockoutDuration: lockout.AutoUnlock) : new DisableLockoutSettings(),
                    PasswordExpirationSettings = (ExpirationDays == 0 ? new UnlimitedPasswordExpirationSettings() : new CustomPasswordExpirationSettings(
                        maxAge: ExpirationDays)),
                    ComputerNameSettings = randomComputerName ? new RandomComputerNameSettings() : new CustomComputerNameSettings(
                        name: computerName),
                    TimeZoneSettings = timeZoneImplicit ? new ImplicitTimeZoneSettings() : new ExplicitTimeZoneSettings(
                        TimeZone: new TimeOffset(regionalSettings.regionTimes[0].Id, regionalSettings.regionTimes[0].DisplayName)),
                    WifiSettings = WirelessInteractive ? new InteractiveWifiSettings() : WirelessSkip ? new SkipWifiSettings() : new ParameterizedWifiSettings(
                        Name: WirelessSettings.SSID,
                        Password: WirelessSettings.Password,
                        ConnectAutomatically: true,
                        Authentication: WirelessSettings.Authentication switch
                        {
                            WirelessNetwork.AuthenticationProtocol.Open => WifiAuthentications.Open,
                            WirelessNetwork.AuthenticationProtocol.WPA2 => WifiAuthentications.WPA2PSK,
                            WirelessNetwork.AuthenticationProtocol.WPA3 => WifiAuthentications.WPA3SAE,
                            _ => WifiAuthentications.WPA2PSK
                        },
                        NonBroadcast: WirelessSettings.NonBroadcast),
                    ProcessorArchitectures = architectures,
                    Components = componentDictionary,
                    ExpressSettings = Telemetry switch
                    {
                        SystemTelemetry.Interactive => ExpressSettingsMode.Interactive,
                        SystemTelemetry.No => ExpressSettingsMode.DisableAll,
                        SystemTelemetry.Yes => ExpressSettingsMode.EnableAll,
                        _ => ExpressSettingsMode.DisableAll
                    },
                    BypassRequirementsCheck = SV_LabConfig,
                    BypassNetworkCheck = SV_BypassNRO,
                    VBoxGuestAdditions = virtualMachine switch
                    {
                        VirtualMachineSolution.VBox_GAs => true,
                        _ => false
                    },
                    VMwareTools = virtualMachine switch
                    {
                        VirtualMachineSolution.VMware_Tools => true,
                        _ => false
                    },
                    VirtIoGuestTools = virtualMachine switch
                    {
                        VirtualMachineSolution.VirtIO => true,
                        _ => false
                    },
                    UseConfigurationSet = UseConfigSet
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
                Console.WriteLine($"\nSUCCESS: Unattended answer file has been generated at \"{targetPath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nCould not generate unattended answer file due to the following error: {ex.Message}");
            }
        }
    }
}
