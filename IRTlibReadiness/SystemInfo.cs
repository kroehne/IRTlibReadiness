using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReadinessTool
{
    public class SystemInfo
    {
        #region SYSTEM
        public string MachineName { get; set; }
        public string HostName { get; set; }
        public string OSVersion { get; set; }
        public bool Is64BitOS { get; set; }
        public bool Is64BitProcess { get; set; }

        public double TotalRam { get; set; }
        public double FreeRam { get; set; }

        public string TotalMemorySystem { get; set; }
        public string FreeMemorySystem { get; set; }
        public string CPUUse { get; set; }
        public string CPUType { get; set; }
        public bool TouchEnabled { get; set; }
        public string Version { get; set; }
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
        public bool MinimalScreenSize { get; set; }
        public int NumberOfMonitors { get; set; }
        public List<string> MonitorDetails { get; set; }

        #endregion

        #region AUDIO
        public bool PlayTestSuccess { get; set; }
        public int NumberOfAudioDevices { get; set; }
        public List<string> AudioDetails { get; set; }

        #endregion

        #region NETWORK
        public int RequiredNumberOfPorts { get; set; }
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
     
        #region Touch helper
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        public static bool IsTouchEnabled()
        {
            const int MAXTOUCHES_INDEX = 95;
            int maxTouches = GetSystemMetrics(MAXTOUCHES_INDEX);

            return maxTouches > 0;
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
            Is64BitOS = Environment.Is64BitOperatingSystem;
            Is64BitProcess = Environment.Is64BitProcess;
            Executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RootDrive = System.IO.Path.GetPathRoot(Executable);
            Version = GetType().Assembly.GetName().Version.ToString();
            TouchEnabled = IsTouchEnabled();
            TotalMemorySystem = "";
            FreeMemorySystem = "";
            CPUUse = "";
            CPUType = "";
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

            #endregion

            #region VIRUS
            VirusDetais = new List<string>();
            #endregion

            #region REGISTRY 
            RegistryDetails = new List<string>();
            #endregion

            #region GRAPHICS
            MinimalScreenSize = false;
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

        }

        public override string ToString()
        {
            string _ret = "";
            _ret += String.Format("IRTlib: Readiness-Tool ({0})\n\n", this.Version);
            _ret += String.Format("- Machine: {0} (Hostname: {1})\n", this.MachineName, this.HostName);
            _ret += String.Format("- System: {0} (64 bit OS: {1} / 64 bit Process: {2})\n", this.OSVersion, this.Is64BitOS, this.Is64BitProcess);
            _ret += String.Format("- CPU: {0} (Usage: {1} %)\n", this.CPUType, this.CPUUse);
            _ret += String.Format("- Memory: Total RAM = {0:0.00}Gb, Available RAM = {1:0.00}Gb\n", this.TotalRam / 1024 / 1024 / 1024, this.FreeRam / 1024 / 1024 / 1024);
            _ret += String.Format("- Touch Enabled Device: {0}\n", this.TouchEnabled);
            _ret += String.Format("- Current User: {0} (Roles: Administrator = {1}, User = {2}, Guest = {3})\n", this.UserName, this.IsAdministrator, this.IsUser, this.IsGuest);

            _ret += "\n";

            _ret += String.Format("- Virus Applications: {0} Application(s) found\n", this.VirusDetais.Count);
            foreach (var s in this.VirusDetais)
                _ret += "   " + s + "\n";
            _ret += "\n";

            _ret += String.Format("- Displays: {0} device(s) (Minimal Size: {1})\n", this.MonitorDetails.Count, this.MinimalScreenSize);
            foreach (var s in this.MonitorDetails)
                _ret += "   " + s + "\n";
            _ret += "\n"; 
            
            _ret += String.Format("- Audio: {0} device(s) (Test: {1})\n", this.NumberOfAudioDevices, this.PlayTestSuccess);
            foreach (var s in this.AudioDetails)
                _ret += "   " + s + "\n";
            _ret += "\n";
             
            _ret += String.Format("- Network Connectivity: Ping = {0}, OpenRead = {1}\n", this.PingSucces, this.OpenReadSucces);

            _ret += String.Format("- Local TCP/IP ports: {0} ports used", this.UsedPorts.Count);
            _ret += String.Format("  " + string.Join(",", this.UsedPorts));
            _ret += "\n\n";

            _ret += String.Format("- Check open TCP/IP ports: >= {0} of {1} ports available\n", this.FreePorts.Count, this.RequiredNumberOfPorts);
            _ret += String.Format("  " + string.Join(",", this.FreePorts));
            _ret += "\n\n";

            _ret += String.Format("- Registry: Check {0} keys/value-pairs\n", this.RegistryDetails.Count);
            foreach (var s in this.RegistryDetails)
                _ret += "   " + s + "\n";
            _ret += "\n";

            _ret += String.Format("- Tempfolder: {0} (Write Access: {1}, Free Bytes: {2})\n", this.TempFolder, this.WriteAccessTempFolder, this.TempFolderFreeBytes);
            _ret += String.Format("- Executable: {0} (Root drive: {1}, Write Access: {2}, Free Bytes: {3})\n", this.Executable, this.RootDrive, this.WriteAccessRoot, this.CurrentDriveFreeBytes);
            _ret += "\n";

            _ret += String.Format("- Drive Speed: Read {0:0.00} MB/s, Write {0:0.00} MB/s\n", this.ReadScore, this.WriteScore);
            foreach (var s in this.SpeedDetails)
                _ret += "   " + s + "\n";
            _ret += "\n";

            return _ret;
        }

    }
}
