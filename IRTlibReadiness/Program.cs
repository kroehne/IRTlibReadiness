using Microsoft.Win32;
using NAudio.Midi;
using Saplin.StorageSpeedMeter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

/*
 *  Sapling.StorageSpeedMeter based on: https://github.com/maxim-saplin/NetCoreStorageSpeedTest (MIT License)
 * 
 */

namespace ReadinessTool
{
    class Program
    { 
        static void Main(string[] args)
        {
            bool Silent = false;
            bool Verbose = false;

            bool DriveSpeedTest = true;
            bool CheckAUdio = true;
            bool CheckPorts = true; 
            
            int StartScanMin = 9000;
            int RequiredNumberOfPorts = 2;
            int NumberOfPortsToCheck = 20;
            int MinimalWidth = 1024;
            int MinimalHeight = 768;

            List<string> RegistryKeys = new List<string>() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation" };

            SystemInfo info = new SystemInfo()
            {
                TotalRam = (double)RamDiskUtil.TotalRam,
                FreeRam = (double)RamDiskUtil.FreeRam,
                RequiredNumberOfPorts = RequiredNumberOfPorts
            };

            try
            {
                #region STARTUP 

                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("IRTlib: Readiness-Tool ({0})\n", info.Version);
                    Console.ResetColor();
                }

                if (!Silent & Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    WriteLineWordWrap("This tool, developed by DIPF/TBA and Software-Driven, checks the prerequisites for running the IRTlib player. \n");
                    Console.ResetColor();
                }

                #endregion

                #region SYSTEM
                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Machine: {0} (Hostname: {1})", info.MachineName, info.HostName);
                    Console.WriteLine(" - System: {0} (64 bit OS: {1} / 64 bit Process: {2})", info.OSVersion, info.Is64BitOS, info.Is64BitProcess);
                    Console.WriteLine(" - CPU: {0} (Usage: {1} %)", info.CPUType, info.CPUUse);
                    Console.WriteLine(" - Memory: Total RAM = {0:0.00}Gb, Available RAM = {1:0.00}Gb", info.TotalRam / 1024 / 1024 / 1024, info.FreeRam / 1024 / 1024 / 1024);
                    Console.WriteLine(" - Touch Enabled Device: {0}", info.TouchEnabled);
                    Console.ResetColor();
                }
                #endregion

                #region USER

                try
                {
                    info.UserName = "Unkonwn";
                    info.IsAdministrator = false;
                    info.IsUser = false;
                    info.IsGuest = false;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                        {
                            WindowsPrincipal principal = new WindowsPrincipal(identity);
                            info.UserName = identity.Name;
                            info.IsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
                            info.IsUser = principal.IsInRole(WindowsBuiltInRole.User);
                            info.IsGuest = principal.IsInRole(WindowsBuiltInRole.Guest);

                            if (!Silent)
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine(" - Current User: {0} (Roles: Administrator = {1}, User = {2}, Guest = {3})", info.UserName, info.IsAdministrator, info.IsUser, info.IsGuest);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nReading user account failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                #endregion

                #region VIRUS

                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"\\" + Environment.MachineName +  @"\root\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                    {
                        var searcherInstance = searcher.Get();
                        foreach (var instance in searcherInstance)
                        {
                            info.VirusDetais.Add(String.Format("Name: {0}, State {1}, Timestamp {2}, ProductExe {3}, ReportingExe: {4}", 
                                instance["displayName"].ToString(),
                                instance["productState"].ToString(),
                                instance["timestamp"].ToString(), 
                                instance["pathToSignedProductExe"].ToString() ,
                                instance["pathToSignedReportingExe"].ToString()));
                        }

                        if (!Silent)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(" - Virus Applications: {0} Application(s) found", info.VirusDetais.Count);
                            if (Verbose)
                            {
                                foreach (var s in info.VirusDetais)
                                    Console.WriteLine("   " + s);
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nVirus software detection failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                #endregion
 
                #region GRAPHIC
                try
                {
                    info.MinimalScreenSize = false;
                    var gr = Display.GetGraphicsAdapters();
                    foreach (var g in gr)
                    {
                        var _monitors = Display.GetMonitors(g.DeviceName);
                        string _monitorName = g.DeviceName.Replace(@"\\.\", "");
                        string _monitorState = "UNKOWN";
                        if (_monitors.Count > 0)
                        {
                            _monitorName = String.Format("{0} ({1})", g.DeviceName.Replace(@"\\.\",""), _monitors[0].DeviceString);
                            _monitorState = _monitors[0].StateFlags.ToString();
                        }
                        var _mode = Display.GetDeviceMode(g.DeviceName);
                        info.MonitorDetails.Add(String.Format("{0}: {1}x{2}-{3}", _monitorName, _mode.dmPelsWidth, _mode.dmPelsHeight, _monitorState));

                        if (_mode.dmPelsWidth >= MinimalWidth && _mode.dmPelsHeight >= MinimalHeight)
                            info.MinimalScreenSize = true;
                    }

                    if (!Silent)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" - Displays: {0} device(s) (Minimal Size: {1}) ", info.MonitorDetails.Count, info.MinimalScreenSize);
                        if (Verbose)
                        {
                            foreach (var s in info.MonitorDetails)
                                Console.WriteLine("   " + s);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nReading graphic devices failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                #endregion

                #region AUDIO

                if (CheckAUdio)
                {
                    try
                    {
                        try
                        {
                            info.PlayTestSuccess = true;
                            using (MidiOut midiOut = new MidiOut(0))
                            {
                                midiOut.Send(MidiMessage.StartNote(60, 127, 1).RawData);
                                Thread.Sleep(1000);
                                midiOut.Send(MidiMessage.StopNote(60, 0, 1).RawData);
                                Thread.Sleep(1000);
                            }
                        }
                        catch
                        {
                            info.PlayTestSuccess = false;
                        }

                        var _audioSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
                        var _audioCollection = _audioSearcher.Get();
                        info.NumberOfAudioDevices = _audioCollection.Count;
                        foreach (var d in _audioCollection)
                        {
                            info.AudioDetails.Add(String.Format("Name: {0}, Status: {1}", d.GetPropertyValue("Name"), d.GetPropertyValue("Status")));
                        }

                        if (!Silent)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(" - Audio: {0} device(s) (Test: {1}) ", info.NumberOfAudioDevices, info.PlayTestSuccess);
                            if (Verbose)
                            {
                                foreach (var s in info.AudioDetails)
                                    Console.WriteLine("   " + s);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nReading graphic devices failed with an unexpected error:");
                        Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                        Console.WriteLine(e.StackTrace);
                    }

                }

                #endregion
                 
                #region NETWORK
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        using (client.OpenRead("http://www.google.com/"))
                        {
                            info.OpenReadSucces = true;
                        }
                    }
                }
                catch
                {
                    info.OpenReadSucces = false;
                }

                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send("www.google.com");
                        if (reply != null && reply.Status == IPStatus.Success)
                        {
                            info.PingSucces = true;
                        }
                    }
                }
                catch
                {
                    info.PingSucces = false;
                }

                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Network Connectivity: Ping = {0}, OpenRead = {1}", info.PingSucces, info.OpenReadSucces);
                }

                #endregion

                #region PORTS

                try
                {

                    IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                    TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
                    foreach (TcpConnectionInformation c in connections)
                    {
                        if (!info.UsedPorts.Contains(c.LocalEndPoint.Port))
                            info.UsedPorts.Add(c.LocalEndPoint.Port);
                    }

                    
                    if (!Silent)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" - Local TCP/IP ports: {0} ports used", info.UsedPorts.Count);
                        if (Verbose)
                        {
                            Console.WriteLine("    " + string.Join(",", info.UsedPorts));
                        }
                    }

                    if (CheckPorts)
                    {
                        int _port = StartScanMin;
                        int _i = 0;

                        while (info.FreePorts.Count < RequiredNumberOfPorts & _i < NumberOfPortsToCheck)
                        {
                            if (!info.UsedPorts.Contains(_port))
                            {
                                if (!IsPortOpen("127.0.0.1", _port, new TimeSpan(250)))
                                {
                                    info.FreePorts.Add(_port);
                                } 
                            }
                            _port++;
                            _i++;
                        }
                       
                        if (!Silent)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(" - Check open TCP/IP ports: >= {0} of {1} ports available", info.FreePorts.Count, RequiredNumberOfPorts);
                            if (Verbose)
                            {
                                Console.WriteLine("    " + string.Join(",", info.FreePorts));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nPort listing failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                #endregion

                #region FILEACCESS
                 
                try
                {
                    info.TempFolder = Path.GetTempPath();
                    info.WriteAccessTempFolder = HasWritePermissionOnDir(info.TempFolder);
                    info.WriteAccessRoot = HasWritePermissionOnDir(info.RootDrive);
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nChecking write permission failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Tempfolder: {0} (Write Access: {1})", info.TempFolder, info.WriteAccessTempFolder);
                    Console.WriteLine(" - Executable: {0} (Root drive: {1}, Write Access: {2})", info.Executable, info.RootDrive, info.WriteAccessRoot);
                }

                #endregion

                #region DRIVESPEED

                if (DriveSpeedTest)
                {
                    
                    try
                    {
                        var bigTest = new BigTest(info.RootDrive, fileSize, false);
                        using (bigTest)
                        {
                            if (!Silent)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Write(" - Speed Test for file: {0}, Size: {1:0.00}Gb\n   Press ESC to break", bigTest.FilePath, (double)bigTest.FileSize / 1024 / 1024 / 1024);
                                Console.ResetColor();
                            }

                            string currentTest = null;
                            const int curCursor = 40;
                            var breakTest = false;

                            bigTest.StatusUpdate += (sender, e) =>
                            {
                                if (breakTest) return;
                                if (e.Status == TestStatus.NotStarted) return;

                                if ((sender as Test).DisplayName != currentTest)
                                {
                                    currentTest = (sender as Test).DisplayName;
                                    if (!Silent)
                                        Console.Write("\n   * {0}/{1} {2}", bigTest.CompletedTests + 1, bigTest.TotalTests, (sender as Test).DisplayName);
                                }

                                ClearLine(curCursor);

                                if (e.Status != TestStatus.Completed)
                                {
                                    if (!Silent)
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                    switch (e.Status)
                                    {
                                        case TestStatus.Started:
                                            if (!Silent)
                                                Console.Write("Started");
                                            break;
                                        case TestStatus.InitMemBuffer:
                                            if (!Silent)
                                                Console.Write("Initializing test data in RAM...");
                                            break;
                                        case TestStatus.PurgingMemCache:
                                            if (!Silent & Verbose)
                                                Console.Write("Purging file cache in RAM...");
                                            break;
                                        case TestStatus.WarmigUp:
                                            if (!Silent)
                                                Console.Write("Warming up...");
                                            break;
                                        case TestStatus.Interrupted:
                                            if (!Silent)
                                                Console.Write("Test interrupted");
                                            break;
                                        case TestStatus.Running:
                                            if (!Silent)
                                                Console.Write("{0}% {2} {1:0.00} MB/s", e.ProgressPercent, e.RecentResult, GetNextAnimation());
                                            break;
                                    }
                                    if (!Silent)
                                        Console.ResetColor();
                                }
                                else if ((e.Status == TestStatus.Completed) && (e.Results != null))
                                {

                                    info.SpeedDetails.Add(String.Format("Avg: {1} {0}, Min: {2} {0}, Max: {3} {0}, Time: {4} ms", unit, e.Results.AvgThroughput,  e.Results.Min , e.Results.Max , e.ElapsedMs));
                                    if (!Silent)
                                    {
                                        Console.Write(string.Format("Avg: {1:0.00}{0}\t",unit,e.Results.AvgThroughput));
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                        Console.Write(
                                            string.Format(" Min÷Max: {1:0.00} ÷ {2:0.00}, Time: {3}m{4:00}s",
                                            unit,
                                            e.Results.Min,
                                            e.Results.Max,
                                            e.ElapsedMs / 1000 / 60,
                                            e.ElapsedMs / 1000 % 60)
                                        );
                                        Console.ResetColor();
                                    }
                                    
                                }

                                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                                {
                                    if (!Silent)
                                        Console.WriteLine("  Stopping...");
                                    breakTest = true;
                                    bigTest.Break();
                                }

                                ShowCounters(bigTest);
                            };

                            var results = bigTest.Execute();
                            if (!Silent & Verbose)
                                HideCounters();

                            if (!breakTest)
                            { 
                                info.ReadScore = bigTest.ReadScore;
                                info.WriteScore = bigTest.WriteScore;

                                if (!Silent)
                                {
                                    if (Verbose)
                                    {
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                        Console.WriteLine("\n   Test file deleted.");
                                    }
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.WriteLine("\n - Drive Speed: Read {0:0.00} MB/s, Write {0:0.00} MB/s", info.ReadScore, info.WriteScore);
                                    Console.ResetColor();
                                }

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nSpeed test failed with an unexpected error:");
                        Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                     
                }

                #endregion

                #region REGISTRY
                 
                try
                {
                    if (RegistryKeys.Count > 0)
                    {
                      
                        foreach (var p in RegistryKeys)
                        {
                            var _parts = p.Split(";", StringSplitOptions.RemoveEmptyEntries);
                            string _result = (string)Registry.GetValue(_parts[0], _parts[1], "not set");
                            info.RegistryDetails.Add(String.Format("Key: {0}, Value: {1}, Result: {2}", _parts[0], _parts[1], _result));
                        }

                        if (!Silent)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(" - Registry: Check {0} keys/value-pairs", info.RegistryDetails.Count);

                            if (Verbose)
                            {
                                foreach (var s in info.RegistryDetails)
                                    Console.WriteLine("   " + s);
                            }
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n Reading registry failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }


                #endregion

                try
                {
                    string _fileName = "Readiness_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt";
                    File.WriteAllText(_fileName, info.ToString());
                    Process.Start("notepad.exe", _fileName);
                }
                catch { }

            }
            catch (Exception ex)
            {
                Console.WriteLine("\nProgram interrupted due to unexpected error:");
                Console.WriteLine("\t" + ex.GetType() + " " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

           
        }

        public static bool HasWritePermissionOnDir(string path)
        {
            var writeAllow = false;
            var writeDeny = false;
            var accessControlList =  new FileInfo(path).GetAccessControl();
            if (accessControlList == null)
                return false;
            var accessRules = accessControlList.GetAccessRules(true, true,
                                        typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules == null)
                return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    writeAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    writeDeny = true;
            }

            return writeAllow && !writeDeny;
        }
        private static bool IsPortOpen(string host, int port, TimeSpan timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(timeout);
                    client.EndConnect(result);
                    return success;
                }
            }
            catch
            {
                return false;
            }
        }

        public const long fileSize = 1024 * 1024 * 1024;
        public const string unit = "MB/s";

        private static void ClearLine(int cursorLeft)
        {
            Console.CursorLeft = cursorLeft;
            Console.Write(new string(' ', Console.WindowWidth - cursorLeft - 1));
            Console.CursorLeft = cursorLeft;
        }

        static char[] anim = new char[] { '/', '|', '\\', '-', '/', '|', '\\', '-' };
        static int animCounter = 0;

        private static char GetNextAnimation()
        {
            animCounter++;
            return anim[animCounter % anim.Length];
        }

        static long prevElapsedSecs = 0;

        private static void ShowCounters(TestSuite ts)
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            var elapsedSecs = ts.ElapsedMs / 1000;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (prevElapsedSecs != elapsedSecs)
            {
                var elapsed = string.Format("                          Elapsed: {0:00}m {1:00}s", elapsedSecs / 60, elapsedSecs % 60);
                Console.CursorLeft = Console.WindowWidth - elapsed.Length - 1;
                Console.CursorTop = 0;
                Console.Write(elapsed);

                var remaing = string.Format("                          Remaining: {0:00}m {1:00}s", ts.RemainingMs / 1000 / 60, ts.RemainingMs / 1000 % 60);
                Console.CursorLeft = Console.WindowWidth - remaing.Length - 1;
                Console.CursorTop = 1;
                Console.Write(remaing);

                prevElapsedSecs = elapsedSecs;
            }

            Console.CursorLeft = left;
            Console.CursorTop = top;
            Console.ResetColor();
        }

        private static void HideCounters()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;

            var elapsed = "                                                   ";
            Console.CursorLeft = Console.WindowWidth - elapsed.Length - 1;
            Console.CursorTop = 0;
            Console.Write(elapsed);

            var remaing = "                                                   ";
            Console.CursorLeft = Console.WindowWidth - remaing.Length - 1;
            Console.CursorTop = 1;
            Console.Write(remaing);

            Console.CursorLeft = left;
            Console.CursorTop = top;
            Console.ResetColor();
        }

       public static void WriteLineWordWrap(string text, int tabSize = 8)
        {
            string[] lines = text
                .Replace("\t", new String(' ', tabSize))
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string process = lines[i];
                List<String> wrapped = new List<string>();

                while (process.Length > Console.WindowWidth)
                {
                    int wrapAt = process.LastIndexOf(' ', Math.Min(Console.WindowWidth - 1, process.Length));
                    if (wrapAt <= 0) break;

                    wrapped.Add(process.Substring(0, wrapAt));
                    process = process.Remove(0, wrapAt + 1);
                }

                foreach (string wrap in wrapped)
                {
                    Console.WriteLine(wrap);
                }

                Console.WriteLine(process);
            }
        }
    }

    
    public class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayDevices(string deviceName, int modeNum, ref DISPLAY_DEVICE displayDevice, int flags);
    }
     
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    public static class Display
    {
        public static List<DISPLAY_DEVICE> GetGraphicsAdapters()
        {
            int i = 0;
            DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE();
            List<DISPLAY_DEVICE> result = new List<DISPLAY_DEVICE>();
            displayDevice.cb = Marshal.SizeOf(displayDevice);
            while (NativeMethods.EnumDisplayDevices(null, i, ref displayDevice, 1))
            {
                result.Add(displayDevice);
                i++;
            }

            return result;
        }

        public static List<DISPLAY_DEVICE> GetMonitors(string graphicsAdapter)
        {

            DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE();
            List<DISPLAY_DEVICE> result = new List<DISPLAY_DEVICE>();
            int i = 0;
            displayDevice.cb = Marshal.SizeOf(displayDevice);
            while (NativeMethods.EnumDisplayDevices(graphicsAdapter, i, ref displayDevice, 0))
            {
                result.Add(displayDevice);
                i++;
            }

            return result;
        }



        public static DEVMODE GetDeviceMode(string graphicsAdapter)
        {
            DEVMODE devMode = new DEVMODE();
            NativeMethods.EnumDisplaySettings(graphicsAdapter, -1, ref devMode);
            return devMode;
        }
        public static List<DEVMODE> GetDeviceModes(string graphicsAdapter)
        {
            int i = 0;
            DEVMODE devMode = new DEVMODE();
            List<DEVMODE> result = new List<DEVMODE>();
            while (NativeMethods.EnumDisplaySettings(graphicsAdapter, i, ref devMode))
            {
                result.Add(devMode);
                i++;
            }
            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}