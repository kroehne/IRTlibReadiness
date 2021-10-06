using Microsoft.Extensions.Configuration;
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
using System.Text;
using System.Text.Json;
using System.Threading;
using YamlDotNet.Serialization;

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
             
            List<string> RegistryKeys = new List<string>() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation" };

            #region ConfigurationData

            string checkInfo = "";
            CheckValue checkValue = null;
            CheckResult checkResult = null;
            ParameterValue parameterValue = null;

            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);

            string configFileNameYaml = @"ReadinessConfig.yaml";
            string configFileNameJson = @"ReadinessConfig.json";
            string configFilePathYaml = System.IO.Path.Combine(strWorkPath, configFileNameYaml);
            string configFilePathJson = System.IO.Path.Combine(strWorkPath, configFileNameJson);

            string resultFileNameYaml = @"ReadinessResult.yaml";
            string resultFileNameJson = @"ReadinessResult.json";
            string resultFilePathYaml = System.IO.Path.Combine(strWorkPath, resultFileNameYaml);
            string resultFilePathJson = System.IO.Path.Combine(strWorkPath, resultFileNameJson);

            ConfigurationMap configurationMap = new ConfigurationMap();
            CheckResults checkResults = new CheckResults();

            if (!File.Exists(configFilePathYaml))
            {
                //no config file so set the defaults and create a config file
                configurationMap.SetDefaults();

                StringBuilder sb = new StringBuilder();
                StringWriter writer = new StringWriter(sb);

                var serializer = new SerializerBuilder()
                .Build();

                //serialize the defaults to a new config file
                serializer.Serialize(writer, configurationMap);

                using (StreamWriter streamWriter = new StreamWriter(configFilePathYaml))
                {
                    streamWriter.Write(writer.ToString());
                }
                Console.WriteLine("Default config data was saved successfully to file: " + configFilePathYaml);

            }
            else
            {
                string configDataAsText = File.ReadAllText(configFilePathYaml);
                StringReader stringReader = new StringReader(configDataAsText);

                var deserializer = new DeserializerBuilder()
                .Build();

                configurationMap = deserializer.Deserialize<ConfigurationMap>(stringReader);
                Console.WriteLine("Config data was successfully read from file: " + configFilePathYaml);

                //if there were missing parameters or check values complete the map by adding defaults
                configurationMap.SetDefaults();
            }
            #endregion

            SystemInfo info = new SystemInfo()
            {
                TotalRam = (double)RamDiskUtil.TotalRam,
                FreeRam = (double)RamDiskUtil.FreeRam,
                //the flags below are initialized to false, the setting is retrieved from the
                //assigned CheckValue section within the yaml file
                //DoDriveSpeedTest = args.Length == 0,
                //DoPortScan = args.Length == 0,
                //the flag below is initialized to false, the setting is retrieved from the
                //assigned Parameter section within the yaml file or from the parameter applied (see below)
                //DoApplicationStartAfterCheck = args.Length != 0
            };

            //later the two if statements below can be removed
            if (configurationMap.CheckRanges.TryGetValue("ReadinessDriveSpeedCheck", out checkValue))
            {
                info.DoDriveSpeedTest = checkValue.RunThisCheck;
            }
            if (configurationMap.CheckRanges.TryGetValue("PortAvailableCheck", out checkValue))
            {
                info.DoPortScan = checkValue.RunThisCheck;
            }

            #region setParametersFromYamlFile
            if (configurationMap.Parameters.TryGetValue("ReadinessStartPlayer", out parameterValue))
            {
                info.DoApplicationStartBeforeCheck = parameterValue.Value.ToLower() == "startbefore";
                info.DoApplicationStartAfterCheck = parameterValue.Value.ToLower() == "startafter";
                //otherwise the Player will not be startet
            }
            if (configurationMap.Parameters.TryGetValue("ReadinessMode", out parameterValue))
            {
                Silent = parameterValue.Value.ToLower() == "silent";
                Verbose = parameterValue.Value.ToLower() == "verbose";
                //otherwise both values are false
            }
            #endregion

            info.AppFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            #region Command Line

            //If a parameter is applied it will overwrite the setting in the yaml file
            var Configuration = new ConfigurationBuilder()
               .AddCommandLine(args)
               .Build();

            try
            { 
                if (args.Length > 0)
                { 
                    if (Configuration["ReadinessStartPlayer"] != null)
                    {
                        if (Configuration["ReadinessStartPlayer"].ToString().ToLower() == "startbefore")
                        {
                            info.DoApplicationStartAfterCheck = false;
                            info.DoApplicationStartBeforeCheck = true;
                        }
                        else if (Configuration["ReadinessStartPlayer"].ToString().ToLower() == "startafter")
                        {
                            info.DoApplicationStartAfterCheck = true;
                            info.DoApplicationStartBeforeCheck = false;
                        }
                        else if (Configuration["ReadinessStartPlayer"].ToString().ToLower() == "nostart")
                        {
                            info.DoApplicationStartAfterCheck = false;
                            info.DoApplicationStartBeforeCheck = false;
                        }
                    }

                    if (Configuration["ReadinessMode"] != null)
                    {
                        if (Configuration["ReadinessMode"].ToString().ToLower() == "silent")
                        {
                            Silent = true;
                            Verbose = false;
                        } 
                        else if (Configuration["ReadinessMode"].ToString().ToLower() == "verbose")
                        {
                            Silent = false;
                            Verbose = true;
                        }
                    }
                    //the check values previously were taken from the command line
                    //now the values are located in the yaml file therefore the code below is inactive
                    /*
                    if (Configuration["ReadinessDriveSpeed"] != null)
                    {
                        if (Configuration["ReadinessDriveSpeed"].ToString().ToLower() == "true")
                        {
                            info.DoDriveSpeedTest = true;
                        }
                        else if (Configuration["ReadinessDriveSpeed"].ToString().ToLower() == "false")
                        {
                            info.DoDriveSpeedTest = false;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessAudio"] != null)
                    {
                        if (Configuration["ReadinessAudio"].ToString().ToLower() == "true")
                        {
                            info.DoAudioCheck = true;
                        }
                        else if (Configuration["ReadinessAudio"].ToString().ToLower() == "false")
                        {
                            info.DoAudioCheck = false;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessPortScan"] != null)
                    {
                        if (Configuration["ReadinessPortScan"].ToString().ToLower() == "true")
                        {
                            info.DoPortScan = true;
                        }
                        else if (Configuration["ReadinessPortScan"].ToString().ToLower() == "false")
                        {
                            info.DoPortScan = false;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessPortScanStart"] != null)
                    {
                        int _port = -1;
                        if (int.TryParse(Configuration["ReadinessPortScanStart"].ToString(), out _port))
                        {
                            info.StartScanMin = _port;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessPortScanRequiredNumber"] != null)
                    {
                        int _number = 1;
                        if (int.TryParse(Configuration["ReadinessPortScanRequiredNumber"].ToString(), out _number))
                        {
                            info.RequiredNumberOfPorts = _number;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessPortScanNumbersToCheck"] != null)
                    {
                        int _number = 1000;
                        if (int.TryParse(Configuration["ReadinessPortScanNumbersToCheck"].ToString(), out _number))
                        {
                            info.NumberOfPortsToCheck = _number;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessScreenCheckWidth"] != null)
                    {
                        int _width = 1024;
                        if (int.TryParse(Configuration["ReadinessScreenCheckWidth"].ToString(), out _width))
                        {
                            info.MinimalWidth = _width;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessScreenCheckHeight"] != null)
                    {
                        int _height = 768;
                        if (int.TryParse(Configuration["ReadinessScreenCheckHeight"].ToString(), out _height))
                        {
                            info.MinimalHeight = _height;
                        }
                    }
                    */
                    /*
                    if (Configuration["ReadinessAppFolder"] != null)
                    {
                        info.AppFolder = Configuration["ReadinessAppFolder"].ToString();
                    }
                    */
                    /*
                    if (Configuration["ReadinessAppName"] != null)
                    {
                        info.AppName = Configuration["ReadinessAppName"].ToString();
                    }
                    */
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nProcessing command line parameters failed with an unexpected error:");
                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            #endregion

            try
            {
                #region START PLAYER BEFORE 

                info.PlayerAvailable = File.Exists(Path.Combine(info.AppFolder, info.AppName));

                if (info.DoApplicationStartBeforeCheck)
                {
                    info.PlayerStarted = false;

                    if (File.Exists(Path.Combine(info.AppFolder, info.AppName)))
                    {
                        try
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = Path.Combine(info.AppFolder, info.AppName),
                                    Arguments = string.Join(" ", args),
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                }
                            };

                            if (process.Start())
                            {
                                info.PlayerStarted = true;
                            }

                            while (!process.StandardOutput.EndOfStream)
                            {
                                var line = process.StandardOutput.ReadLine();
                                Console.WriteLine(line);
                            }

                            process.WaitForExit();
                            return;

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n Lunching the player failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                            info.PlayerStarted = false;
                        }
                    }

                }

                #endregion

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
                    WriteLineWordWrap("\n\nThis tool, developed by DIPF/TBA and Software-Driven, checks the prerequisites for running the IRTlib player. \n\n");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("- BaseDirectory: {0}", AppContext.BaseDirectory);
                    Console.WriteLine("- CurrentDirectory (Directory): {0}", Directory.GetCurrentDirectory());
                    Console.WriteLine("- CurrentDirectory (Environment): {0}", Environment.CurrentDirectory);
                    Console.WriteLine("- CurrentProcess Folder: {0}\n", Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
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

                #region MEMORY
                //Memory check #1
                checkInfo = "MemoryInstalledCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                //get config data
                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        ValidValue minimalMemoryInstalled = checkValue.ValidValues.Find(item => item.name == "MinimalMemoryInstalled");
                        if(minimalMemoryInstalled != null)
                        {
                            try
                            {
                                double mmi = Convert.ToDouble(minimalMemoryInstalled.value);//GB
                                double tms = info.TotalRam / 1024 / 1024 / 1024;

                                if(tms >= mmi) checkResult.Result = ResultType.succeeded;                               
                                checkResult.ResultInfo = String.Format("Memory installed: {0:0.00}GB (expected: {1:0.00}GB)", tms, mmi);

                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("{0} or {1} is outside the range of the Int32 type.", minimalMemoryInstalled.value, info.TotalMemorySystem);
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("The {0} or {1} value is not in a recognizable format.",
                                                  minimalMemoryInstalled.value, info.TotalMemorySystem);
                            }
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                //Memory check #2
                checkInfo = "MemoryAvailableCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                //get config data
                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        ValidValue minimalMemoryAvailable = checkValue.ValidValues.Find(item => item.name == "MinimalMemoryAvailable");
                        if (minimalMemoryAvailable != null)
                        {
                            try
                            {
                                double mma = Convert.ToDouble(minimalMemoryAvailable.value);//GB
                                double fms = info.FreeRam / 1024 / 1024 / 1024;

                                if (fms >= mma) checkResult.Result = ResultType.succeeded;
                                checkResult.ResultInfo = String.Format("Memory available: {0:0.00}GB (expected: {1:0.00}GB)", fms, mma);

                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("{0} or {1} is outside the range of the Int32 type.", minimalMemoryAvailable.value, info.FreeMemorySystem);
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("The {0} or {1} value is not in a recognizable format.",
                                                  minimalMemoryAvailable.value, info.FreeMemorySystem);
                            }
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region USER
                //User role check
                checkInfo = "UserRoleCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                //get config data
                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {

                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        try
                        {
                            info.UserName = "Unknown";
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

                                    //principal.

                                    if (!Silent)
                                    {
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.WriteLine(" - Current User: {0} (Roles: Administrator = {1}, User = {2}, Guest = {3})", info.UserName, info.IsAdministrator, info.IsUser, info.IsGuest);
                                    }
                                }

                                if (checkResult != null)
                                {
                                    string role = "";
                                    if (info.IsAdministrator) role = "Administrator";
                                    if (info.IsUser) role = "User";
                                    if (info.IsGuest) role = "Guest";

                                    if (checkValue.ValidValues.Exists(item => item.value == role)) checkResult.Result = ResultType.succeeded;
                                    checkResult.ResultInfo = "Current users role is: " + role;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nReading user account failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                            checkResult.ResultInfo = "Reading user account failed with an unexpected error";
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region VIRUS
                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"\\" + Environment.MachineName + @"\root\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                    {
                        var searcherInstance = searcher.Get();
                        foreach (var instance in searcherInstance)
                        {
                            string displayName = "unknown";
                            string productState = "unknown";
                            string timestamp = "unknown";
                            string pathToSignedProductExe = "unknown";
                            string pathToSignedReportingExe = "unknown";

                            //some of the properties may not be present
                            try { var ts = instance.GetPropertyValue("displayName"); displayName = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection failed: Property displayName not found"); }
                            try { var ts = instance.GetPropertyValue("productState"); productState = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection failed: Property productState not found"); }
                            try { var ts = instance.GetPropertyValue("timestamp"); timestamp = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection failed: Property timestamp not found"); }
                            try { var ts = instance.GetPropertyValue("pathToSignedProductExe"); pathToSignedProductExe = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection failed: Property pathToSignedProductExe not found"); }
                            try { var ts = instance.GetPropertyValue("pathToSignedReportingExe"); pathToSignedReportingExe = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection failed: Property pathToSignedReportingExe not found"); }

                            info.VirusDetais.Add(String.Format("Name: {0}, State {1}, Timestamp {2}, ProductExe {3}, ReportingExe: {4}",
                                displayName,
                                productState,
                                timestamp,
                                pathToSignedProductExe,
                                pathToSignedReportingExe));
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

                //anti virus software check check
                checkInfo = "AntiVirusSoftwareCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        ValidValue antiVirusSoftwareExpected = checkValue.ValidValues.Find(item => item.name == "AntiVirusSoftwareExpected");
                        if (antiVirusSoftwareExpected != null)
                        {
                            if (antiVirusSoftwareExpected.value.ToLower().Equals("true")) { if (info.VirusDetais.Count > 0) checkResult.Result = ResultType.succeeded; }

                            checkResult.ResultInfo = "Anti virus software:";
                            foreach (var s in info.VirusDetais)
                            {
                                string[] ss = s.Split(',');
                                if (ss.Length > 0) checkResult.ResultInfo += String.Format(" {0},", ss[0]);
                            }

                            checkResult.ResultInfo += String.Format(" (expected: {0})", antiVirusSoftwareExpected.value);
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }
                #endregion

                #region GRAPHIC
                //screen size check
                checkInfo = "ScreenResolutionCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        try
                        {
                            ValidValue srH = checkValue.ValidValues.Find(item => item.name == "MinimalHorizontalRes");
                            ValidValue srV = checkValue.ValidValues.Find(item => item.name == "MinimalVerticalRes");

                            if (srH != null && srH != null)
                            {
                                try
                                {
                                    info.MinimalWidth = Convert.ToInt32(srH.value);
                                    info.MinimalHeight = Convert.ToInt32(srV.value);
                                }
                                catch (OverflowException)
                                {
                                    Console.WriteLine("{0} or {1} is outside the range of the Int32 type.", srH.value, srV.value);
                                }
                                catch (FormatException)
                                {
                                    Console.WriteLine("The {0} or {1} value '{2}' or {3} is not in a recognizable format.",
                                                      srH.value.GetType().Name, srV.value.GetType().Name, srH.value, srV.value);
                                }
                            }
                            else
                            {
                                checkResult.ResultInfo = "Config data incomplete, using defaults: " + checkInfo;
                            }

                            info.MinimalScreenSizeCheck = false;
                            info.MinimalScreenSize = String.Format("{0}x{1}", info.MinimalWidth, info.MinimalHeight);
                            var gr = Display.GetGraphicsAdapters();
                            foreach (var g in gr)
                            {
                                var _monitors = Display.GetMonitors(g.DeviceName);
                                string _monitorName = g.DeviceName.Replace(@"\\.\", "");
                                string _monitorState = "UNKOWN";
                                if (_monitors.Count > 0)
                                {
                                    _monitorName = String.Format("{0} ({1})", g.DeviceName.Replace(@"\\.\", ""), _monitors[0].DeviceString);
                                    _monitorState = _monitors[0].StateFlags.ToString();
                                }
                                var _mode = Display.GetDeviceMode(g.DeviceName);
                                info.MonitorDetails.Add(String.Format("{0}: {1}x{2}-{3}", _monitorName, _mode.dmPelsWidth, _mode.dmPelsHeight, _monitorState));

                                //check the size
                                //at least one of the displays must meet the check values
                                if (_mode.dmPelsWidth >= info.MinimalWidth && _mode.dmPelsHeight >= info.MinimalHeight)
                                {
                                    //SystemInfo structure:
                                    info.MinimalScreenSizeCheck = true;

                                }
                                //checkResult structure
                                if (_mode.dmPelsWidth >= info.MinimalWidth && _mode.dmPelsHeight >= info.MinimalHeight)
                                {
                                    if (!checkResults.CheckResultMap.ContainsKey(checkInfo))
                                        //add the check result for the current monitor
                                        checkResults.CheckResultMap.Add(checkInfo, new CheckResult(ResultType.succeeded, String.Format("{0}: {1}x{2}-{3}", _monitorName, _mode.dmPelsWidth, _mode.dmPelsHeight, _monitorState)));
                                    else
                                        //append the check result of the current monitor to the existing one
                                        checkResults.CheckResultMap[checkInfo].ResultInfo += String.Format(" ; {0}: {1}x{2}-{3}", _monitorName, _mode.dmPelsWidth, _mode.dmPelsHeight, _monitorState);
                                }
                            }//end foreach

                            //if there were no suitable monitors found add a negative check result
                            if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = false; checkResults.CheckResultMap.Add(checkInfo, new CheckResult(ResultType.failed, "No suitable monitors found")); }

                            if (!Silent)
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine(" - Displays: {0} device(s) (Minimal Size: {1} - {2}) ", info.MonitorDetails.Count, info.MinimalScreenSize, info.MinimalScreenSizeCheck);
                                if (Verbose)
                                {
                                    foreach (var s in info.MonitorDetails)
                                        Console.WriteLine("   " + s);
                                }

                            }
                        }//end try
                        catch (Exception e)
                        {
                            Console.WriteLine("\nReading graphic devices failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                            //in case an error occurred add a negative check result
                            if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = false; checkResults.CheckResultMap.Add(checkInfo, new CheckResult(ResultType.failed, "Check has failed. An unexpected error occurred")); }
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }
                #endregion

                #region TOUCHSCREEN
                //touch screen check
                checkInfo = "TouchScreenCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        ValidValue touchScreenExpected = checkValue.ValidValues.Find(item => item.name == "TouchScreenExpected");
                        if (touchScreenExpected != null)
                        {
                            if (touchScreenExpected.value.ToLower().Equals("true")) { if (info.TouchEnabled == true) checkResult.Result = ResultType.succeeded; }
                            checkResult.ResultInfo = String.Format("Touch screen enabled: {0} (expected: {1})", info.TouchEnabled, touchScreenExpected.value);
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region AUDIO
                //Audio checks
                checkInfo = "AudioDevicesCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        try
                        {
                            var _audioSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
                            var _audioCollection = _audioSearcher.Get();
                            info.NumberOfAudioDevices = _audioCollection.Count;
                            foreach (var d in _audioCollection)
                            {
                                info.AudioDetails.Add(String.Format("Name: {0}, Status: {1}", d.GetPropertyValue("Name"), d.GetPropertyValue("Status")));

                                if (!checkResults.CheckResultMap.ContainsKey(checkInfo))
                                    //add the check result for the current monitor
                                    checkResults.CheckResultMap.Add(checkInfo, new CheckResult(ResultType.succeeded, String.Format("Name: {0}, Status: {1}", d.GetPropertyValue("Name"), d.GetPropertyValue("Status"))));
                                else
                                    //append the check result of the current monitor to the existing one
                                    checkResults.CheckResultMap[checkInfo].ResultInfo += String.Format(" ; Name: {0}, Status: {1}", d.GetPropertyValue("Name"), d.GetPropertyValue("Status"));

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
                            Console.WriteLine("\nReading audio devices failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }


                checkInfo = "AudioMidiToneCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
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

                                checkResult.Result = ResultType.succeeded;
                                checkResult.ResultInfo = "Midi tone was played successfully";
                            }
                        }
                        catch
                        {
                            info.PlayTestSuccess = false;
                            checkResult.ResultInfo = "Error while playing midi tone";

                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region NETWORK

                //internet access check
                checkInfo = "NetworkConnectivityCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {

                        ValidValue webClientURL = checkValue.ValidValues.Find(item => item.name == "WebClientURL");
                        ValidValue webClientURLaccessExpected = checkValue.ValidValues.Find(item => item.name == "WebClientURLaccessExpected");

                        if (webClientURL != null && webClientURLaccessExpected != null)
                        {
                            try
                            {
                                using (WebClient client = new WebClient())
                                {
                                    using (client.OpenRead(webClientURL.value))
                                    {
                                        info.OpenReadSucces = true;
                                        checkResult.ResultInfo = String.Format("Access to {0} successful", webClientURL.value);
                                    }
                                }
                            }
                            catch
                            {
                                info.OpenReadSucces = false;
                                checkResult.ResultInfo = String.Format("Access to {0} failed", webClientURL.value);
                            }
                            ValidValue pingURL = checkValue.ValidValues.Find(item => item.name == "PingURL");
                            ValidValue pingURLaccessExpected = checkValue.ValidValues.Find(item => item.name == "PingURLaccessExpected");

                            if (pingURL != null && pingURLaccessExpected != null)
                            {
                                try
                                {
                                    using (var ping = new Ping())
                                    {
                                        var reply = ping.Send(pingURL.value);
                                        if (reply != null && reply.Status == IPStatus.Success)
                                        {
                                            info.PingSucces = true;
                                            checkResult.ResultInfo += String.Format(" ; Ping of {0} successful", pingURL.value);
                                        }
                                        else
                                        {
                                            checkResult.ResultInfo += String.Format(" ; Ping of {0} failed", pingURL.value);
                                        }
                                    }
                                }
                                catch
                                {
                                    info.PingSucces = false;
                                    checkResult.ResultInfo += String.Format(" ; Ping of {0} failed", pingURL.value);
                                }

                                if (info.OpenReadSucces && info.PingSucces) checkResult.Result = ResultType.succeeded;
                            }

                            if (!Silent)
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine(" - Network Connectivity: Ping = {0}, OpenRead = {1}", info.PingSucces, info.OpenReadSucces);
                            }

                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region PORTS
                /*
                 * checks
                 * a) if a number of ports is available within a range of ports (PortRangeAvailableCheck)
                 * b) if ports provided in a list are available (PortAvailableCheck)
                 * 
                 */
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

                    if (info.DoPortScan)
                    {
                        int _port = info.StartScanMin;
                        int _i = 0;

                        while (info.FreePorts.Count < info.RequiredNumberOfPorts & _i < info.NumberOfPortsToCheck)
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
                            Console.WriteLine(" - Check open TCP/IP ports: >= {0} of {1} ports available", info.FreePorts.Count, info.RequiredNumberOfPorts);
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


                //port checks

                //port check #1
                checkInfo = "PortRangeAvailableCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        ValidValue firstPort = checkValue.ValidValues.Find(item => item.name == "FirstPort");
                        ValidValue lastPort = checkValue.ValidValues.Find(item => item.name == "LastPort");
                        ValidValue minimumPortsFree = checkValue.ValidValues.Find(item => item.name == "MinimumPortsFree");

                        if (firstPort != null && lastPort != null && minimumPortsFree != null) {

                            long _firstPort = 0;
                            long _lastPort = 0;
                            long _minimumPortsFree = 0;

                            if (long.TryParse(firstPort.value, out _firstPort) && long.TryParse(lastPort.value, out _lastPort) && long.TryParse(minimumPortsFree.value, out _minimumPortsFree))
                            {
                                long _freePorts = 0;
                                //count the number of available ports within the range 
                                for(long i = _firstPort; i <= _lastPort; i++)
                                {
                                    if (!info.UsedPorts.Contains(i)) _freePorts++;
                                }

                                if(_freePorts >= _minimumPortsFree) checkResult.Result = ResultType.succeeded;

                                checkResult.ResultInfo = String.Format("{0} ports are available in the range of port {1} to port {2}", _freePorts, _firstPort, _lastPort);

                            }
                            else
                            {
                                checkResult.ResultInfo += "Wrong config value format: ";
                            }
                        }
                        else { checkResult.ResultInfo += "Config data partly missing";}
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                //port check #2
                checkInfo = "PortAvailableCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        long _port = 0;
                        checkResult.Result = ResultType.succeeded;
                        checkResult.ResultInfo = "";
                        foreach (ValidValue port in checkValue.ValidValues)
                        {
                            if (long.TryParse(port.value, out _port))
                            {
                                if (info.UsedPorts.Contains(_port))
                                {
                                    checkResult.Result = ResultType.failed;
                                    checkResult.ResultInfo += String.Format(" port {0} not available;", _port);
                                }
                                else { checkResult.ResultInfo += String.Format(" port {0} available;", _port); }

                            }
                            else { checkResult.ResultInfo += String.Format(" Wrong format: port {0};", port.value); }
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }


                #endregion

                #region FILEACCESS
                /*
                 * folder accessable check
                 * a) checks if the user's temp folder (C:\Users\<currentUser>\AppData\Local\Temp\) is writeable
                 * b) checks if the root folder (C:\) is writeable
                 */
                #region FILEACCESS - originalCheck

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
                try
                {
                    long freeBytesOut;

                    if (NativeMethods.GetDiskFreeSpaceEx(info.TempFolder, out freeBytesOut, out var _1, out var _2))
                        info.TempFolderFreeBytes = freeBytesOut;

                    if (NativeMethods.GetDiskFreeSpaceEx(info.RootDrive, out freeBytesOut, out var _3, out var _4))
                        info.CurrentDriveFreeBytes = freeBytesOut;
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nChecking disk space failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Tempfolder: {0} (Write Access: {1}, Free Bytes: {2})", info.TempFolder, info.WriteAccessTempFolder, info.TempFolderFreeBytes);
                    Console.WriteLine(" - Executable: {0} (Root drive: {1}, Write Access: {2}, Free Bytes: {3})", info.Executable, info.RootDrive, info.WriteAccessRoot, info.CurrentDriveFreeBytes);
                }
                #endregion

                //folder writable check
                checkInfo = "FoldersWritableCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        string Executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        checkResult.Result = ResultType.succeeded;
                        checkResult.ResultInfo = "";
                        foreach (ValidValue folder in checkValue.ValidValues)
                        {
                            if (folder.value.Contains("<USER>")) { folder.value = folder.value.Replace("<USER>", Environment.UserName.ToString()); };
                            if (folder.value.ToUpper().Equals("USERTEMPFOLDER")) { folder.value = Path.GetTempPath(); };
                            if (folder.value.ToUpper().Equals("ROOTDRIVE")) { folder.value = System.IO.Path.GetPathRoot(Executable); };

                            try
                            {
                                checkResult.ResultInfo += String.Format("Folder {0}: ", folder.value);
                                if (!HasWritePermissionOnDir(folder.value))
                                {
                                    checkResult.Result = ResultType.failed;
                                    checkResult.ResultInfo += "NOK ";
                                }else
                                    checkResult.ResultInfo += "OK ";
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("\nChecking write permission failed with an unexpected error:");
                                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                                Console.WriteLine(e.StackTrace);
                                checkResult.ResultInfo += " Checking write permission failed ";
                            }
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                //folder free space
                checkInfo = "FoldersFreeSpaceCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (!checkValue.RunThisCheck)
                    {
                        if (!Silent) Console.WriteLine("Test skipped: {0}", checkInfo);
                        checkResult.Result = ResultType.skipped;
                    }
                    else
                    {
                        string Executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        checkResult.Result = ResultType.succeeded;
                        checkResult.ResultInfo = "";
                        foreach (ValidValue folder in checkValue.ValidValues)
                        {
                            if (folder.name.Contains("<USER>")) { folder.name = folder.name.Replace("<USER>", Environment.UserName.ToString()); };
                            if (folder.name.ToUpper().Equals("USERTEMPFOLDER")) { folder.name = Path.GetTempPath(); };
                            if (folder.name.ToUpper().Equals("ROOTDRIVE")) { folder.name = System.IO.Path.GetPathRoot(Executable); };

                            try
                            {
                                long freeBytesOut = 0;
                                long expectedFreeBytes = 0;

                                try
                                {
                                    expectedFreeBytes = Convert.ToInt32(folder.value) * 1024;
                                }
                                catch (OverflowException)
                                {
                                    Console.WriteLine("{0} is outside the range of the Int32 type.", folder.value);
                                }
                                catch (FormatException)
                                {
                                    Console.WriteLine("The {0} value is not in a recognizable format.",
                                                      folder.value.GetType().Name);
                                }

                                if (NativeMethods.GetDiskFreeSpaceEx(folder.name, out freeBytesOut, out var _1, out var _2))
                                { 
                                    checkResult.ResultInfo += String.Format("Folder {0} free space {1}: ", folder.name, freeBytesOut);
                                    if (freeBytesOut <  expectedFreeBytes)
                                    {
                                        checkResult.Result = ResultType.failed;
                                        checkResult.ResultInfo += "NOK ";
                                    }
                                    else
                                        checkResult.ResultInfo += "OK ";
                                }else
                                    checkResult.ResultInfo += "getting free space failed ";
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("\nChecking disk space failed with an unexpected error:");
                                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                                Console.WriteLine(e.StackTrace);
                                checkResult.ResultInfo += " Checking disk space failed ";
                            }
                        }

                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region DRIVESPEED
                checkInfo = "DriveSpeedCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.failed, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (checkValue.RunThisCheck)
                        info.DoDriveSpeedTest = true;
                    else
                        checkResult.Result = ResultType.skipped;
                }

                if (info.DoDriveSpeedTest)
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

                                    info.SpeedDetails.Add(String.Format("{5} -- Avg: {1} {0}, Min: {2} {0}, Max: {3} {0}, Time: {4} ms", unit, e.Results.AvgThroughput,  e.Results.Min , e.Results.Max , e.ElapsedMs, e.Results.TestDisplayName));
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
                                    info.DoDriveSpeedTest = false;
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
                                    Console.WriteLine("\n - Drive Speed: Read {0:0.00} MB/s, Write {1:0.00} MB/s", info.ReadScore, info.WriteScore);
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

                    if (checkValue != null)
                    {

                        ValidValue minimalSpeedRead = checkValue.ValidValues.Find(item => item.name == "MinimalSpeedRead");
                        ValidValue minimalSpeedWrite = checkValue.ValidValues.Find(item => item.name == "MinimalSpeedWrite");

                        if (minimalSpeedRead != null && minimalSpeedWrite != null)
                        {
                            double msr = -1;
                            double msw = -1;
                            try
                            {
                                msr = Convert.ToDouble(minimalSpeedRead.value);
                                msw = Convert.ToDouble(minimalSpeedWrite.value);
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Unable to convert '{0}' or '{1}' to a Double.", minimalSpeedRead.value, minimalSpeedWrite.value);
                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("'{0}' or '{1}' is outside the range of a Double.", minimalSpeedRead.value, minimalSpeedWrite.value);
                            }

                            if (msr > -1 && msw > -1)
                            {
                                if (info.ReadScore >= msr && info.WriteScore >= msw) checkResult.Result = ResultType.succeeded;

                                checkResult.ResultInfo = String.Format("ReadScore: {0:0.00} (expected: {1}) WriteScore: {2:0.00} (expected: {3})",
                                    info.ReadScore,
                                    msr,
                                    info.WriteScore,
                                    msw);
                            }
                        }

                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                #endregion

                #region REGISTRY
                checkInfo = "RegistryKeyCheck";
                checkValue = null;
                checkResult = new CheckResult(ResultType.succeeded, "");

                if (!configurationMap.CheckRanges.TryGetValue(checkInfo, out checkValue))
                {
                    checkResult.ResultInfo = "Config data missing: " + checkInfo;
                }
                else
                {
                    if (checkValue.RunThisCheck)
                    {

                        try
                        {
                            //if (RegistryKeys.Count > 0)
                            if (checkValue.ValidValues.Count > 0)
                                {

                                foreach (ValidValue p in checkValue.ValidValues)
                                {
                                    var _parts = p.name.Split(";", StringSplitOptions.RemoveEmptyEntries);
                                    string _result = (string)Registry.GetValue(_parts[0], _parts[1], "not set");
                                    if (_result != null)
                                    {
                                        if (!_result.Equals(p.value)) checkResult.Result = ResultType.failed;
                                        checkResult.ResultInfo += String.Format("Key: {0} value {1} result {2} (expected:{3}) ", _parts[0], _parts[1], _result, p.value);
                                    }
                                    else
                                    {
                                        checkResult.Result = ResultType.failed;
                                        checkResult.ResultInfo += String.Format("Key: {0} value {1} undefined (expected:{2}) ", _parts[0], _parts[1], p.value);
                                    }
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n Reading registry failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                            checkResult.ResultInfo += " Reading registry failed with an unexpected error";
                        }
                    }
                }
                if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(checkInfo, checkResult); }

                /*
                  List<string> RegistryKeys = new List<string>() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation" };
                */
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
                            Console.WriteLine(" - Registry: Checked {0} keys/value-pairs (see output for details)", info.RegistryDetails.Count);

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

                #region START PLAYER AFTER 

                if (info.DoApplicationStartAfterCheck)
                {
                    info.PlayerStarted = false;
                    if (File.Exists(Path.Combine(info.AppFolder, info.AppName)))
                    {
                        try
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = Path.Combine(info.AppFolder, info.AppName),
                                    Arguments = string.Join(" ", args),
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                }
                            };

                            if (process.Start())
                            {
                                info.PlayerStarted = true;
                            } 

                            while (!process.StandardOutput.EndOfStream)
                            {
                                var line = process.StandardOutput.ReadLine();
                                Console.WriteLine(line);
                            }
                             
                            process.WaitForExit();
                              
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n Lunching the player failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                            info.PlayerStarted = false;
                        }
                    }
                    else
                    {
                        info.PlayerAvailable = false;
                    }
                }

                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\n - Player (Found Application: {0}, Started: {1})", info.PlayerAvailable, info.PlayerStarted);
                    Console.ResetColor();
                }

                #endregion

                #region WRITE INFO
                string _fileName = "Readiness_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt";

                try
                {
                    File.WriteAllText(_fileName, info.ToString());
                }
                catch { }

                if ((info.DoApplicationStartAfterCheck && !info.PlayerStarted) || (!info.DoApplicationStartAfterCheck))
                {
                    try
                    {
                        Process.Start("notepad.exe", _fileName);
                    }
                    catch { }
                }

                #endregion

                #region WRITERESULTS

                //write the results to the console

                if (!Silent)
                {
                    if (checkResults.OverallResult)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine("Overall check result is {0} See details below \n", checkResults.OverallResult);
                    Console.ResetColor();


                foreach (KeyValuePair<string, CheckResult> entry in checkResults.CheckResultMap)
                    {

                        if (entry.Value.Result == ResultType.succeeded) Console.ForegroundColor = ConsoleColor.Green;
                        if (entry.Value.Result == ResultType.failed) Console.ForegroundColor = ConsoleColor.Red;
                        if (entry.Value.Result == ResultType.skipped) Console.ForegroundColor = ConsoleColor.Gray;

                        Console.WriteLine("{0} - {1} Info: {2}", entry.Value.Result, entry.Key, entry.Value.ResultInfo);
                    }
                    Console.ResetColor();
                }

                //write the results to a yaml file
                StringBuilder sb = new StringBuilder();
                StringWriter writer = new StringWriter(sb);

                var serializer = new SerializerBuilder()
                .Build();

                serializer.Serialize(writer, checkResults);

                using (StreamWriter streamWriter = new StreamWriter(resultFilePathYaml))
                {
                    streamWriter.Write(writer.ToString());
                }

                //write the results to a json file
                
                string jsonString = JsonSerializer.Serialize(checkResults);
                File.WriteAllText(resultFilePathJson, jsonString);

                #endregion

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

    
    public static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out long lpFreeBytesAvailable,
        out long lpTotalNumberOfBytes,
        out long lpTotalNumberOfFreeBytes);

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