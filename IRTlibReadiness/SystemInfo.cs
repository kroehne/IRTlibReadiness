using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using Saplin.StorageSpeedMeter;

namespace ReadinessTool
{
    public class SystemInfo
    {
        #region JOB
        //Parameters
        public bool DoApplicationStartAfterCheck = false;
        public bool DoApplicationStartBeforeCheck = false;

        public bool DoDriveSpeedTest = false;
        public bool DoAudioCheck = false;
        public bool DoPortScan = false;

        //Check configuration
        public int StartScanMin = 8001;
        public int RequiredNumberOfPorts = 2;
        public int NumberOfPortsToCheck = 1000;
        public int MinimalWidth = 1024;
        public int MinimalHeight = 768;

        public string AppName = "TestApp.Player.Chromely.exe";
        public string AppFolder = "";
        #endregion

        #region SYSTEM
        public string MachineName { get; set; }
        public string HostName { get; set; }
        public string OSVersion { get; set; }
        public string OSName { get; set; }
        public bool Is64BitOS { get; set; }
        public bool Is64BitProcess { get; set; }

        //private static double TotalRam { get; set; }
        private static double TotalRam;
        public static double totalRam
        {
            get => TotalRam;
        }

        private static double FreeRam;
        public static double freeRam
        {
            get => FreeRam;
        }

        private static string TotalMemorySystem;
        public static string totalMemorySystem
        {
            get => TotalMemorySystem;
        }

        private static string FreeMemorySystem;
        public static string freeMemorySystem
        {
            get => FreeMemorySystem;
        }

        public string CPUUse { get; set; }
        public string CPUType { get; set; }
        public bool TouchEnabled { get; set; }
        public bool HasTouch { get; set; }
        public string Version { get; set; }
        public string DotNetFrameworkVersion { get; set; }
        #endregion

        #region USER

        public string UserName { get; set; }
        public bool IsAdministrator { get; set; }
        public bool IsUser { get; set; }
        public bool IsGuest { get; set; }

        #endregion

        #region NETWORK

        public bool PingSucces { get; set; }
        public bool OpenReadSucces { get; set; }

        #endregion

        #region GRAPHICS
        public bool MinimalScreenSizeCheck { get; set; }
        public string MinimalScreenSize { get; set; }
        public int NumberOfMonitors { get; set; }
        public List<string> MonitorDetails { get; set; }

        #endregion

        #region AUDIO
        public bool PlayTestSuccess { get; set; }
        public int NumberOfAudioDevices { get; set; }
        public List<string> AudioDetails { get; set; }

        #endregion

        #region NETWORK
        public List<long> UsedPorts { get; set; }
        public List<long> FreePorts { get; set; }

        #endregion

        #region FILEACCESS
        public bool WriteAccessRoot { get; set; }
        public string TempFolder { get; set; }
        public bool WriteAccessTempFolder { get; set; }

        public long TempFolderFreeBytes { get; set; }
        public long CurrentDriveFreeBytes { get; set; }

        #endregion

        #region DRIVESPEED
        public double ReadScore { get; set; }
        public double WriteScore { get; set; }

        public List<string> SpeedDetails { get; set; }

        public string Executable { get; set; }
        public string RootDrive { get; set; }
        #endregion

        #region VIRUS
        public List<string> VirusDetais { get; set; }
        #endregion

        #region REGISTRY
        public List<string> RegistryDetails { get; set; }
        #endregion

        #region PLAYER
        public bool PlayerAvailable { get; set; }
        public bool PlayerStarted { get; set; }
        public List<string> PlayerResults { get; set; }

        #endregion

        public string OverallResult { get; set; }

        #region Touch helper
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        public static bool IsTouchEnabled()
        {
            const int MAXTOUCHES_INDEX = 95;
            int maxTouches = GetSystemMetrics(MAXTOUCHES_INDEX);

            return maxTouches > 0;
        }
        public static bool HasTouchDevice()
        {
            bool hasTouch = false;
            /*
            bool hasTouch = Windows.Devices.Input
                          .PointerDevice.GetPointerDevices()
                          .Any(p => p.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch);
            */
            return hasTouch;
        }
        #endregion

        #region WMIC helper
        private static string GetWmicOutput(string query, bool redirectStandardOutput = true)
        {
            try
            { 
                var info = new ProcessStartInfo("wmic");
                info.Arguments = query;
                info.RedirectStandardOutput = redirectStandardOutput;
                var output = "";
                using (var process = Process.Start(info))
                {
                    output = process.StandardOutput.ReadToEnd();
                }
                return output.Trim();
            }
            catch
            {
                return "";
            }
        }
        #endregion

        static string HKLM_GetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return "";
                return (string)rk.GetValue(key);
            }
            catch { return ""; }
        }

        public static string FriendlyName()
        {
            string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");
            if (ProductName != "")
            {
                return (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName +
                            (CSDVersion != "" ? " " + CSDVersion : "");
            }
            return "";
        }

        public SystemInfo()
        {
            #region SYSTEM
            MachineName = Environment.MachineName.ToString();
            try
            {
                HostName = System.Net.Dns.GetHostName();
            }
            catch { HostName = "Unkown"; }

            OSVersion = Environment.OSVersion.ToString();

            var osName = FriendlyName();
            /*
            var osName = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            */
            OSName = osName != null ? osName.ToString() : "Unknown";

            Is64BitOS = Environment.Is64BitOperatingSystem;
            Is64BitProcess = Environment.Is64BitProcess;
            Executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RootDrive = System.IO.Path.GetPathRoot(Executable);
            Version = GetType().Assembly.GetName().Version.ToString();
            TouchEnabled = IsTouchEnabled();
            HasTouch = HasTouchDevice();
            TotalMemorySystem = "";
            FreeMemorySystem = "";
            CPUUse = "";
            CPUType = "";
            TotalRam = (double)RamDiskUtil.TotalRam;
            FreeRam = (double)RamDiskUtil.FreeRam;

            try
            {
                var memorielines = GetWmicOutput("OS get FreePhysicalMemory,TotalVisibleMemorySize /Value").Split("\n");

                FreeMemorySystem = memorielines[0].Split("=", StringSplitOptions.RemoveEmptyEntries)[1];
                TotalMemorySystem = memorielines[1].Split("=", StringSplitOptions.RemoveEmptyEntries)[1];

                var cpuLines = GetWmicOutput("CPU get Name,LoadPercentage /Value").Split("\n");

                CPUUse = cpuLines[0].Split("=", StringSplitOptions.RemoveEmptyEntries)[1].Replace("\r", "");
                CPUType = cpuLines[1].Split("=", StringSplitOptions.RemoveEmptyEntries)[1].Replace("\r", "");
            }
            catch
            {
            }

            TempFolderFreeBytes = 0;
            CurrentDriveFreeBytes = 0;
            WriteAccessRoot = false;
            TempFolder = "";
            WriteAccessTempFolder = false;

            DotNetFrameworkVersion = getDotNetFrameworkVersion(false);

            #endregion

            #region VIRUS
            VirusDetais = new List<string>();
            #endregion

            #region REGISTRY 
            RegistryDetails = new List<string>();
            #endregion

            #region GRAPHICS
            MinimalScreenSizeCheck = false;
            MinimalScreenSize = "";
            NumberOfMonitors = 0;
            MonitorDetails = new List<string>();
            #endregion

            #region AUDIO
            PlayTestSuccess = false;
            NumberOfAudioDevices = 0;
            AudioDetails = new List<string>();
            #endregion

            #region NETWORK
            PingSucces = false;
            OpenReadSucces = false;
            UsedPorts = new List<long>();
            FreePorts = new List<long>();
            #endregion

            #region DRIVESPEED

            SpeedDetails = new List<string>();

            #endregion

            #region PLAYER
            PlayerAvailable = false;
            PlayerStarted = false;
            PlayerResults = new List<string>();
            #endregion

            OverallResult = "Overallresult missing";

            string HKLM_GetString(string path, string key)
            {
                try
                {
                    RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                    if (rk == null) return "";
                    return (string)rk.GetValue(key);
                }
                catch { return ""; }
            }

            string FriendlyName()
            {
                string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
                string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");
                if (ProductName != "")
                {
                    return (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName +
                                (CSDVersion != "" ? " " + CSDVersion : "");
                }
                return "";
            }


        }
        public override string ToString()
        {
            string _ret = "";
            //_ret += String.Format("IRTlib: Readiness-Tool ({0})\n\nAbout the system:\n\n", this.Version);
            _ret += String.Format("- Machine: {0} (Hostname: {1})\n", this.MachineName, this.HostName);
            _ret += String.Format("- System: {0} \"{1}\" (64 bit OS: {2} / 64 bit Process: {3})\n", this.OSVersion, this.OSName, this.Is64BitOS, this.Is64BitProcess);
            _ret += String.Format("- CPU: {0} (Usage: {1} %)\n", this.CPUType, this.CPUUse);
            //_ret += String.Format("- Memory: Total RAM = {0:0.00}Gb, Available RAM = {1:0.00}Gb\n", this.TotalRam / 1024 / 1024 / 1024, this.FreeRam / 1024 / 1024 / 1024);
            _ret += String.Format("- Memory: Total RAM = {0:0.00}Gb, Available RAM = {1:0.00}Gb\n", totalRam / 1024 / 1024 / 1024, freeRam / 1024 / 1024 / 1024);
            _ret += String.Format("- Touch Enabled: {0} \n", this.TouchEnabled);
            //_ret += String.Format("- Current User: {0} (Roles: Administrator = {1}, User = {2}, Guest = {3})\n", this.UserName, this.IsAdministrator, this.IsUser, this.IsGuest);
            _ret += String.Format("- .Net Framework version: {0} \n", this.DotNetFrameworkVersion);

            return _ret;
        }

        public string getManualDiagnosisResult()
        {
            string _ret = "Manual Player diagnosis:\n\n";

            if (!(this.DoApplicationStartAfterCheck | this.DoApplicationStartBeforeCheck))
            {
                return _ret += "- The Player was not configured to run\n";
            }

            if (this.PlayerAvailable && this.PlayerStarted)
            {
                _ret += "Player run results:\n";
                foreach (var s in this.PlayerResults)
                    _ret += String.Format("- {0}\n", s);
            }
            else
            {
                if (!this.PlayerAvailable)
                {
                    _ret += "The Player could not be found\n";

                }
                else
                {
                    _ret += "The Player was not started although it should be\n";
                }
            }

            return _ret;

        }

        public string getDotNetFrameworkVersion(bool lookForVersionsBeforeVersion45 = false)
        {
            string dotNetFrameworkVersion = "unknown";

            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    dotNetFrameworkVersion = $".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}";
                }
                else
                {
                    if (lookForVersionsBeforeVersion45)
                    {
                        dotNetFrameworkVersion = CheckForVersionBefore45();
                    }
                    else
                    {
                        dotNetFrameworkVersion = ".NET Framework Version 4.5 or later is not detected.";
                    }
                }
            }

            return dotNetFrameworkVersion;
        }

        // Checking the version using >= enables forward compatibility.
        private string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
                return "4.8 or later";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "Version 4.5 or later is not detected.";

        }

        private string CheckForVersionBefore45()
        {
            string dotNetFrameworkVersion = ".NET Framework Version is not detected.";

            // Open the registry key for the .NET Framework entry.
            using (RegistryKey ndpKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).
                    OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                foreach (var versionKeyName in ndpKey.GetSubKeyNames())
                {
                    // Skip .NET Framework 4.5 version information.
                    if (versionKeyName == "v4")
                    {
                        continue;
                    }

                    if (versionKeyName.StartsWith("v"))
                    {
                        RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);

                        // Get the .NET Framework version value.
                        var name = (string)versionKey.GetValue("Version", "");
                        // Get the service pack (SP) number.
                        var sp = versionKey.GetValue("SP", "").ToString();

                        // Get the installation flag.
                        var install = versionKey.GetValue("Install", "").ToString();
                        if (string.IsNullOrEmpty(install))
                        {
                            // No install info; it must be in a child subkey.
                            return $"{versionKeyName}  {name}";
                        }
                        else if (install == "1")
                        {
                            // Install = 1 means the version is installed.

                            if (!string.IsNullOrEmpty(sp))
                            {
                                return $"{versionKeyName}  {name}  SP{sp}";
                            }
                            else
                            {
                                return $"{versionKeyName}  {name}";
                            }
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            continue;
                        }
                        // else print out info from subkeys...

                        // Iterate through the subkeys of the version subkey.
                        foreach (var subKeyName in versionKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (!string.IsNullOrEmpty(name))
                                sp = subKey.GetValue("SP", "").ToString();

                            install = subKey.GetValue("Install", "").ToString();
                            if (string.IsNullOrEmpty(install))
                            {
                                // No install info; it must be later.
                                return $"{versionKeyName}  {name}";
                            }
                            else if (install == "1")
                            {
                                if (!string.IsNullOrEmpty(sp))
                                {
                                    return $"{subKeyName}  {name}  SP{sp}";
                                }
                                else
                                {
                                    return $"  {subKeyName}  {name}";
                                }
                            }
                        }
                    }
                }

                return dotNetFrameworkVersion;
            }
        }

        }
    }
