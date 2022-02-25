using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using YamlDotNet.Serialization;
using Newtonsoft.Json;
using System.Linq;
using System.IO.Compression;
using System.Reflection;


/*
 *  Sapling.StorageSpeedMeter based on: https://github.com/maxim-saplin/NetCoreStorageSpeedTest (MIT License)
 * 
 */

namespace ReadinessTool
{

    enum ReportMode { Silent, Error, Info, Verbose };

    class Program
    { 
        static void Main(string[] args)
        {
            bool Silent = false;
            bool Verbose = false;

            ReportMode reportMode = ReportMode.Info;

            string starLine = "********************************************************************************";
            List<string> txtReportList = new List<string>();
            string txtLine = "";

            //WMI calls may throw an exception
            bool WMIexceptionOccurred = false;

            List<string> RegistryKeys = new List<string>() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation" };

            //initialize the time when the player is started to a time long ago
            //this is used to find the player's output file later
            DateTime playerStartTime = new DateTime(2000, 1, 1);
            DateTime playerInitStartTime = playerStartTime;
            string playerWindowTitle = "DIPF TestApp Standalone";

            #region ConfigurationData

            bool checkScopeDiagnose = false;

            //string checkInfo = "";
            CheckValue checkValue = null;
            //CheckResult checkResult = null;
            ParameterValue parameterValue = null;

            string strAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string strOutputPath = strWorkPath;
            string strPlayerResultPath = System.IO.Path.Combine(strWorkPath, "..\\resultfolder");

            string configFileNameYaml = "ReadinessConfig.yaml";
            string configFileNameJson = "ReadinessConfig.json";
            string configFilePathYaml = System.IO.Path.Combine(strWorkPath, configFileNameYaml);
            string configFilePathJson = System.IO.Path.Combine(strWorkPath, configFileNameJson);
            string tempPath = System.IO.Path.Combine(strWorkPath, "temp");

            //prefixes for the result output files
            string resultFileNameText = "ReadinessResult_";
            string resultFileNameYaml = "ReadinessResult_";
            string resultFileNameJson = "ReadinessResult_";
            //the output path of the results may be affected by the parameter later
            string resultFilePathYaml = "";
            string resultFilePathJson = "";
            string studyName = "";
            List<string> libPlayerCheckList = new List<string>();

            bool debug = false;

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
                //Console.WriteLine("Config data was successfully read from file: " + configFilePathYaml);

                //if there were missing parameters or check values complete the map by adding defaults
                //configurationMap.SetDefaults();
            }
            #endregion

            // create only one instance of class SystemInfo because it contains static fields
            SystemInfo info = new SystemInfo();
            if(info == null)
            {
                Console.WriteLine("Create instance of class SystemInfo has failed. Program will exit");
                Environment.Exit(0);
            }

            //get the Player's folder and file name from the config otherwise keep the initial values
            CheckValue checkValuePlayer = null;
            checkValuePlayer = configurationMap.CheckRanges.TryGetValue("ExternalSoftwareCheck", out checkValuePlayer) ? checkValuePlayer : new CheckValue(false, false, "Config data missing", CheckExecution.conditional);
            if(checkValuePlayer.ValidValues.Count > 0)
            {
                info.AppFolder = checkValuePlayer.ValidValues[0].name;
                info.AppName = checkValuePlayer.ValidValues[0].value;
            }

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
                //otherwise both values are false (default setting "normal")

                // in the future enum ReportMode should be used, default is ReportMode.Info
                if (Silent) reportMode = ReportMode.Silent;
                if (Verbose) reportMode = ReportMode.Verbose;

            }

            if (configurationMap.Parameters.TryGetValue("ReadinessOutputFolder", out parameterValue))
            {
                if(parameterValue.Value.Length != 0) 
                {
                    string strOutputPathTemp = parameterValue.Value;
                    if (parameterValue.Value.ToUpper().Equals("USERTEMPFOLDER")) { strOutputPathTemp = Path.GetTempPath(); };

                    strOutputPathTemp  = System.IO.Path.Combine(strWorkPath, strOutputPathTemp);

                    try
                    {
                        DirectoryInfo dirInfo;
                        // Determine whether the directory exists.
                        if (!Directory.Exists(strOutputPathTemp))
                        {
                            // Try to create the directory.
                            dirInfo = Directory.CreateDirectory(strOutputPathTemp);
                            Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(strOutputPathTemp));
                        }
                        if (HasWritePermissionOnDir(strOutputPathTemp))
                        {
                            strOutputPath = strOutputPathTemp;
                            resultFilePathYaml = System.IO.Path.Combine(strOutputPath, resultFileNameYaml);
                            resultFilePathJson = System.IO.Path.Combine(strOutputPath, resultFileNameJson);
                        }
                        else
                        {
                            Console.WriteLine(String.Format("Could not set the output to: {0} - folder not writable\nOutput written to {1}", strOutputPathTemp, strOutputPath));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("The process failed: {0}", e.ToString());
                    }
                }
                //otherwise the initial values (all files are written to the app folder) are valid
            }
            if (configurationMap.Parameters.TryGetValue("ReadinessCheckScope", out parameterValue))
            {
                checkScopeDiagnose = parameterValue.Value.ToLower().Equals("diagnose");
                //otherwise "normal"
            }
            if (configurationMap.Parameters.TryGetValue("StudyName", out parameterValue))
            {
                studyName = parameterValue.Value;
            }

            if (configurationMap.Parameters.TryGetValue("LibPlayerChecks", out parameterValue))
            {
                libPlayerCheckList = new List<string>(parameterValue.Value.Split(','));
            }

            if (configurationMap.Parameters.TryGetValue("Debug", out parameterValue))
            {
                debug = parameterValue.Value.ToLower().Equals("true");
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

                    if (Configuration["ReadinessCheckScope"] != null)
                    {
                        checkScopeDiagnose = Configuration["ReadinessCheckScope"].ToString().ToLower() == "diagnose";
                    }

                    if (Configuration["StudyName"] != null)
                    {
                        studyName = Configuration["StudyName"].ToString();
                    }

                    if (Configuration["LibPlayerChecks"] != null)
                    {
                        string libPlayerChecks = Configuration["LibPlayerChecks"].ToString();
                        libPlayerCheckList = new List<string>(libPlayerChecks.Split(','));
                    }

                    if (Configuration["Debug"] != null)
                    {
                        debug = Configuration["Debug"].ToString().ToLower().Equals("true");
                    }

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
                #region VERSIONINFO
                //show the application version info
                string debugMode = debug == true ? ", running in debug mode" : "";

                txtLine = string.Format("\n{0}\n{1}{2}{3}\n{4}\n", starLine, "* IRTlib Readiness-Tool Version ", info.Version, debugMode, starLine);
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);

                #endregion

                #region START PLAYER BEFORE 

                info.PlayerAvailable = File.Exists(Path.Combine(info.AppFolder, info.AppName));

                if (info.DoApplicationStartBeforeCheck)
                {
                    info.PlayerStarted = false;

                    if (File.Exists(Path.Combine(info.AppFolder, info.AppName)))
                    {
                        if (!Silent) Console.WriteLine("The Player will now be started...");
                        try
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = Path.Combine(info.AppFolder, info.AppName),
                                    Arguments = string.Join(" ", args),
                                    UseShellExecute = false,
                                    RedirectStandardOutput = false,
                                    CreateNoWindow = true
                                }
                            };

                            ThreadWindowObserver two = new ThreadWindowObserver(
                                playerWindowTitle, process, debug);

                            // Create a thread to execute the task, and then
                            // start the thread.
                            Thread t = new Thread(new ThreadStart(two.ThreadProc));
                            t.Start();

                            if (process.Start())
                            {
                                //remember the time when the player is started
                                playerStartTime = DateTime.Now;
                                info.PlayerStarted = true;
                            }

                            /*
                            while (!process.StandardOutput.EndOfStream)
                            {
                                var line = process.StandardOutput.ReadLine();
                                Console.WriteLine(line);
                            }
                            */
                            process.WaitForExit();
                            //return;

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n Launching the player failed with an unexpected error:");
                            Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                            Console.WriteLine(e.StackTrace);
                            info.PlayerStarted = false;
                        }
                    }
                }

                #endregion

                #region STARTUP 

                if (reportMode == ReportMode.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    WriteLineWordWrap("\n\nThis tool, developed by DIPF/TBA and Software-Driven, checks the prerequisites for running the IRTlib player. \n\n");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("- BaseDirectory: {0}", AppContext.BaseDirectory);
                    Console.WriteLine("- CurrentDirectory (Directory): {0}", Directory.GetCurrentDirectory());
                    Console.WriteLine("- CurrentDirectory (Environment): {0}", Environment.CurrentDirectory);
                    Console.WriteLine("- CurrentProcess Folder: {0}\n", Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
                    Console.ResetColor();
                }

                #endregion

                #region SYSTEMINFO
                txtLine = string.Format("\n{0}\n{1}\n{2}\n", starLine, "* Info about the system", starLine);
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);

                txtLine = info.ToString();
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);
                #endregion

                #region CHECKS

                txtLine = string.Format("\n{0}\n{1}\n{2}\n", starLine, "* Performing the checks", starLine);
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);

                //perform checks according the configuration +

                List<ReadinessCheck> ReadinessCheckList = new List<ReadinessCheck>();

                //get the current namespace
                string nameSpace = MethodInfo.GetCurrentMethod().ReflectedType.Namespace;

                List<string> checkClassNameList = new List<string>(configurationMap.CheckRanges.Keys);

                foreach (string checkClassName in checkClassNameList)
                {
                    //get the class type
                    string formTypeFullName = string.Format("{0}.{1}", nameSpace, checkClassName);
                    Type type = Type.GetType(formTypeFullName, false);
                    CheckResult currentCheckResult = null;

                    if(type != null)
                    {
                        ReadinessCheck readinessCheck = (ReadinessCheck)Activator.CreateInstance(type); //uses the parameterless constructor
                        readinessCheck.SetCheckScope(checkScopeDiagnose);
                        txtLine = string.Format("Performing check: {0}... ", checkClassName);
                        ConsoleWrite(txtLine, ConsoleColor.Green, reportMode);
                        txtReportList.Add(txtLine);
                        currentCheckResult = readinessCheck.PerformCheck(ref configurationMap, ref checkResults);
                        txtLine = string.Format("Done ({0})", currentCheckResult.Result);
                        ConsoleWriteLine(txtLine, ConsoleColor.Green, reportMode);
                        txtReportList.Add(txtLine + "\n");
                        ReadinessCheckList.Add(readinessCheck);
                    }
                    else
                    {
                        txtLine = string.Format("Cannot perform check found in the configuration: {0}, Class not found", checkClassName);
                        ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                        txtReportList.Add(txtLine + "\n");
                    }
                }

                //perform checks according the configuration -
                #endregion

                #region START PLAYER AFTER

                if (info.DoApplicationStartAfterCheck)
                {

                    txtLine = string.Format("\n{0}\n{1}\n{2}\n", starLine, "* Manual Player diagnosis", starLine);
                    ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                    txtReportList.Add(txtLine);

                    if (info.PlayerAvailable)
                    {
                        if (checkResults.OverallResult)
                        {
                            txtLine = "The Player is available.";
                            ConsoleWriteLine(txtLine, ConsoleColor.Green, reportMode);
                            txtReportList.Add(txtLine + "\n");
                        }
                        else
                        {
                            txtLine = "The Player is available but won't be started because at least one check has failed.";
                            ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                            txtReportList.Add(txtLine + "\n");
                        }
                    }
                    else
                    {
                        txtLine = "The Player is not available. This part of the test will be skipped.";
                        ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                        txtReportList.Add(txtLine + "\n");
                    }

                    //don't start the player if the overall result is false
                    if (checkResults.OverallResult)
                    {
                        info.PlayerStarted = false;
                        if (File.Exists(Path.Combine(info.AppFolder, info.AppName)))
                        {
                            //if (!Silent) Console.WriteLine("The Player will now be started...");
                            txtLine = "The Player will now be started...";
                            ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                            txtReportList.Add(txtLine + "\n");

                            try
                            {
                                var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine(info.AppFolder, info.AppName),
                                        Arguments = string.Join(" ", args),
                                        UseShellExecute = false,
                                        RedirectStandardOutput = false,
                                        CreateNoWindow = true
                                    }
                                };

                                ThreadWindowObserver two = new ThreadWindowObserver(
                                    playerWindowTitle, process, debug);

                                // Create a thread to execute the task, and then
                                // start the thread.
                                Thread t = new Thread(new ThreadStart(two.ThreadProc));
                                t.Start();

                                if (process.Start())
                                {
                                    //remember the time when the player is started
                                    playerStartTime = DateTime.Now;
                                    info.PlayerStarted = true;
                                }
                                /*
                                while (!process.StandardOutput.EndOfStream)
                                {
                                    var line = process.StandardOutput.ReadLine();
                                    if (!Silent) Console.WriteLine(line);
                                }
                                */
                                process.WaitForExit();

                            }
                            catch (Exception e)
                            {
                                txtLine = string.Format("Launching the player failed with an unexpected error:\n{0} {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
                                ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                                txtReportList.Add(txtLine + "\n");

                                info.PlayerStarted = false;
                            }
                            txtLine = "The Player has terminated.";
                            ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                            txtReportList.Add(txtLine + "\n");
                        }
                        else
                        {
                            txtLine = "The Player could not be started because the executable was not found.";
                            ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                            txtReportList.Add(txtLine + "\n");

                            info.PlayerAvailable = false;
                        }
                    }
                }
                #endregion

                #region GETRESULTFROMPLAYER
                Dictionary<string, string> hitScore = new Dictionary<string, string>();
                Dictionary<string, string> missScore = new Dictionary<string, string>();

                if (info.DoApplicationStartAfterCheck | info.DoApplicationStartBeforeCheck)
                {

                    string playerOutputZipFile = "";
                    string playerOutputScoreFile = "";

                    if (info.PlayerStarted && (playerInitStartTime != playerStartTime))
                    //if (true)
                    {
                        if (Directory.Exists(strPlayerResultPath))
                        {
                            //find the relevant output of the player

                            DirectoryInfo dirInfo = new DirectoryInfo(strPlayerResultPath);
                            FileInfo[] files = dirInfo.GetFiles().OrderBy(p => p.CreationTime).ToArray();
                            foreach (FileInfo file in files)
                            {
                                //looking for a file written after the player was started
                                if (file.CreationTime > playerStartTime)
                                {
                                    if (!Silent) Console.WriteLine("Player output file is: " + file.FullName);
                                    playerOutputZipFile = file.FullName;
                                    break;
                                }
                            }

                            //extract the scoring file

                            //create a temp folder
                            if (!Directory.Exists(tempPath))
                            {
                                try
                                {
                                    dirInfo = Directory.CreateDirectory(tempPath);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Process has failed: Creating temp dir \"{0}\"{1}", tempPath, e.ToString());
                                }
                            }
                            if (dirInfo.Exists)
                            {
                                if (File.Exists(playerOutputZipFile))
                                {
                                    try
                                    {
                                        ZipFile.ExtractToDirectory(playerOutputZipFile, tempPath, true);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Process has failed: Unzip file \"{0}\" {1}", playerOutputZipFile, e.ToString());
                                    }
                                }
                            }
                            playerOutputScoreFile = System.IO.Path.Combine(tempPath, "ItemScore.json");
                        }
                        else
                        {
                            if (!Silent) Console.WriteLine("Player output folder not found: " + strPlayerResultPath);
                            //the player output folder doesn't exist
                        }

                        if (File.Exists(playerOutputScoreFile))
                        {
                            string[] jsonScoreString = File.ReadAllLines(playerOutputScoreFile);

                            for (int lineCnt = 0; lineCnt < jsonScoreString.Length; lineCnt++)
                            {
                                if (jsonScoreString[lineCnt].EndsWith(',')) { jsonScoreString[lineCnt] = jsonScoreString[lineCnt].Substring(0, jsonScoreString[lineCnt].Length - 1); }

                                JsonTextReader reader = new JsonTextReader(new StringReader(jsonScoreString[lineCnt]));

                                string PropertyName = "";

                                while (reader.Read())
                                {
                                    if (reader.Value != null)
                                    {
                                        //Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);

                                        if (PropertyName.Equals("ItemScore"))
                                        {
                                            string[] scoreInfo = reader.Value.ToString().Replace('{', ' ').Replace('}', ' ').Split(',');
                                            for (int cnt = 0; cnt < scoreInfo.Length; cnt++)
                                            {
                                                //remove some disturbing characters
                                                scoreInfo[cnt] = scoreInfo[cnt].Replace('"', ' ');
                                                scoreInfo[cnt] = scoreInfo[cnt].Replace('\\', ' ').Trim();
                                                string[] scoreDetails = scoreInfo[cnt].Split(':');
                                                if (scoreDetails.Length == 2)
                                                {
                                                    string key = scoreDetails[0].Trim();
                                                    string val = scoreDetails[1].Trim();

                                                    if (key.ToLower().StartsWith("hit."))
                                                    {
                                                        if (val.ToLower().Equals("true"))
                                                            if (!hitScore.ContainsKey(key)) hitScore.Add(key, val);
                                                    }
                                                    if (key.ToLower().StartsWith("miss."))
                                                    {
                                                        if (val.ToLower().Equals("true"))
                                                            if (!missScore.ContainsKey(key)) missScore.Add(key, val);
                                                    }
                                                }
                                            }
                                        }

                                        if (reader.TokenType == JsonToken.PropertyName)
                                            PropertyName = reader.Value.ToString();
                                        else
                                            PropertyName = "";
                                    }
                                    else
                                    {
                                        //Console.WriteLine("Token: {0}", reader.TokenType);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                //the current time will be used for all output files
                string currentTime = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");

                #region WRITERESULTS_CONSOLE
                bool suitable = true;
                //write the results to the console

                txtLine= string.Format("\n{0}\n{1}{2}\n{3}\n", starLine, "* ReadinessTool results. The overall check result is ", checkResults.OverallResult, starLine);
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);
                /*
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(" ");
                Console.WriteLine("********************************************************************************");

                Console.Write("* ReadinessTool results. The overall check result is ");
                if (checkResults.OverallResult)
                    ConsoleWriteLine(string.Format("{0}", checkResults.OverallResult), ConsoleColor.Green, reportMode);

                else
                    ConsoleWriteLine(string.Format("{0}", checkResults.OverallResult), ConsoleColor.Red, reportMode);

                Console.WriteLine("********************************************************************************");
                Console.WriteLine(" ");
                Console.ResetColor();
                */

                CheckValue currentValue;
                string optionalCheck = "";

                foreach (KeyValuePair<string, CheckResult> entry in checkResults.CheckResultMap)
                {
                    optionalCheck = "";
                    if (entry.Value.Result == ResultType.succeeded) 
                        Console.ForegroundColor = ConsoleColor.Green;
                    if (entry.Value.Result == ResultType.failed) { 
                        //Console.ForegroundColor = ConsoleColor.Red;
                        if(configurationMap.CheckRanges.TryGetValue(entry.Key, out currentValue))
                        {   //do not set the overall result to false if this is a optional check
                            if (!currentValue.OptionalCheck)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                suitable = false;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                optionalCheck = " (optional check)";
                            }
                        }
                    }
                    if (entry.Value.Result == ResultType.skipped) Console.ForegroundColor = ConsoleColor.Gray;

                    txtLine = string.Format("{0} - {1} Info: {2} {3}", entry.Value.Result, entry.Key, entry.Value.ResultInfo, optionalCheck);
                    Console.WriteLine(txtLine);
                    txtReportList.Add(txtLine + "\n");
                }
                Console.ResetColor();
                //ReadinessTool results -

                //IRTlibPlayer results +

                txtLine = string.Format("\n{0}\n{1}\n{2}\n", starLine, "* IRTlibPlayer system diagnose results", starLine);
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);

                if (info.DoApplicationStartAfterCheck | info.DoApplicationStartBeforeCheck)
                {
                    string[] hitNames = { "hit.hit01_KIOSK", "hit.hit01_TOUCH", "hit.hit02_TOUCH", "hit.hit01_AUDIO", "hit.hit01_TLMENU", "hit.hit02_TLMENU", "hit.hit03_TLMENU", "hit.hit01_AreaVisible_RB02", "hit.hit02_LinesVisible_RB02" };
                    string[] hitTexts = { "Kiosk mode and Alt-Tab", "Drag and Drop by mouse", "Drag and Drop by touch", "Audio: playback and volume adjustment", "Testleiter Menue: Open", "Testleiter Menue: Volume adjustment", "Testleiter Menue: Next button", "Screen: Item area completely visible", "Screen: Lines completely visible" };
                    string[] missNames = { "miss.miss01_KIOSK", "miss.miss02_KIOSK", "miss.miss01_TOUCH", "miss.miss01_AUDIO", "miss.miss02_AUDIO", "miss.miss01_TLMENU", "miss.miss02_TLMENU", "miss.miss03_TLMENU", "miss.miss01_AreaVisible_RB01", "miss.miss02_LinesVisible_RB01" };
                    string[] missTexts = { "Kiosk mode and ALt-Tab: Taskbar or window appeared", "Kiosk mode and Alt Tab: leaving test possible", "Drag and Drop", "Audio: playback but no adjustment", "Audio: no playback at all", "Testleiter Menue: Open", "Testleiter Menue: Volume adjustment", "Testleiter Menue: Next button", "Screen: Item area completely visible", "Screen: Lines completely visible" };

                    if (hitScore.Count == 0 && missScore.Count == 0)
                    {
                        txtLine = string.Format("\n{0}\n{1}\n", "No IRTlibPlayer diagnose result found.", "This part of the system diagnosis seems to have failed.");
                        ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                        txtReportList.Add(txtLine);
                        suitable = false;
                    }
                    else
                    {
                        //Console.WriteLine("Hits");
                        for (int hitCnt = 0; hitCnt < hitNames.Length; hitCnt++)
                        {
                            if (hitScore.ContainsKey(hitNames[hitCnt]))
                            {
                                txtLine = string.Format("{0}\n{1}", hitTexts[hitCnt],": OK");
                                ConsoleWriteLine(txtLine, ConsoleColor.Green, reportMode);
                                txtReportList.Add(txtLine);
                                info.PlayerResults.Add(hitTexts[hitCnt] + ": OK");
                            }
                        }
                        Console.ResetColor();
                        //Console.WriteLine("Misses");
                        for (int missCnt = 0; missCnt < missNames.Length; missCnt++)
                        {
                            if (missScore.ContainsKey(missNames[missCnt]))
                            {
                                txtLine = string.Format("{0}\n{1}", missTexts[missCnt], ": not OK");
                                ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                                txtReportList.Add(txtLine);
                                info.PlayerResults.Add(missTexts[missCnt] + ": not OK");
                                suitable = false;
                            }
                        }
                        Console.ResetColor();

                        //check for missing answers
                        Console.ForegroundColor = ConsoleColor.Red;
                        //Kiosk
                        if (libPlayerCheckList.Contains("KIOSK"))
                        {
                            if (!hitScore.ContainsKey("hit.hit01_KIOSK"))
                                if (!missScore.ContainsKey("miss.miss01_KIOSK"))
                                    if (!missScore.ContainsKey("miss.miss02_KIOSK"))
                                    {
                                        txtLine = "Question \"Kiosk Modus / ALt-Tab\" is not answered.";
                                        ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                                        txtReportList.Add(txtLine + "\n");
                                        info.PlayerResults.Add(txtLine);
                                        suitable = false;
                                    }
                        }

                        if (libPlayerCheckList.Contains("TOUCH"))
                        {
                            if (!hitScore.ContainsKey("hit.hit01_TOUCH") && !hitScore.ContainsKey("hit.hit02_TOUCH"))
                                if (!missScore.ContainsKey("miss.miss01_TOUCH"))
                                {
                                    txtLine = "Question \"Kiosk Modus / Drag and Drop\" is not answered.";
                                    ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                                    txtReportList.Add(txtLine + "\n");
                                    info.PlayerResults.Add(txtLine);
                                    suitable = false;
                                }
                        }

                        //Audio
                        if (libPlayerCheckList.Contains("AUDIO"))
                        {

                            if (!hitScore.ContainsKey("hit.hit01_AUDIO"))
                                if (!missScore.ContainsKey("miss.miss01_AUDIO"))
                                    if (!missScore.ContainsKey("miss.miss02_AUDIO"))
                                    {
                                        txtLine = "Question \"Audio\" is not answered.";
                                        ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                                        txtReportList.Add(txtLine + "\n");
                                        info.PlayerResults.Add(txtLine);
                                        suitable = false;
                                    }
                        }
                        Console.ResetColor();

                        if (libPlayerCheckList.Contains("TLMENU"))
                        {
                            //TL Menu (these answers are skipped if the page was left by using the TL Menu
                            txtLine = "\nHint: The questions concerning the TL Menue are skipped if the Next button of the TL menu was clicked.\n";
                            ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                            txtReportList.Add(txtLine);

                            if (!hitScore.ContainsKey("hit.hit01_TLMENU"))
                                if (!missScore.ContainsKey("miss.miss01_TLMENU"))
                                {
                                    txtLine = "Question \"TL Menue / Open\" is not answered.";
                                    ConsoleWriteLine(txtLine, ConsoleColor.DarkYellow, reportMode);
                                    txtReportList.Add(txtLine + "\n");
                                    info.PlayerResults.Add(txtLine);
                                }

                            if (libPlayerCheckList.Contains("AUDIO"))
                            {
                                if (!hitScore.ContainsKey("hit.hit02_TLMENU"))
                                    if (!missScore.ContainsKey("miss.miss02_TLMENU"))
                                    {
                                        txtLine = "Question \"TL Menue / Audio adjustment\" is not answered.";
                                        ConsoleWriteLine(txtLine, ConsoleColor.DarkYellow, reportMode);
                                        txtReportList.Add(txtLine + "\n");
                                        info.PlayerResults.Add(txtLine);
                                    }
                            }
                            if (!hitScore.ContainsKey("hit.hit03_TLMENU"))
                                if (!missScore.ContainsKey("miss.miss03_TLMENU"))
                                {
                                    txtLine = "Question \"TL Menue / Next button\" is not answered.";
                                    ConsoleWriteLine(txtLine, ConsoleColor.DarkYellow, reportMode);
                                    txtReportList.Add(txtLine + "\n");
                                    info.PlayerResults.Add(txtLine);
                                }
                        }
                        //the Screen questions don't need to be checked (not possible to end the test without giving answers)
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    txtLine = "The IRTlibPlayer was not configured to run.";
                    ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                    txtReportList.Add(txtLine);
                    Console.ResetColor();
                }
                //remove the temp folder
                if (Directory.Exists(tempPath))
                {
                    try
                    {
                        Directory.Delete(tempPath, true);
                    }
                    catch (Exception e)
                    {
                        txtLine = string.Format("Process failed: Deleting folder {0}\n {1}",tempPath, e.ToString());
                        ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                        txtReportList.Add(txtLine);
                    }
                }
                //IRTlibPlayer results -

                //Overall result +
                txtLine = string.Format("\n{0}\n{1}\n{2}\n", starLine, "* Result summary", starLine);
                ConsoleWriteLine(txtLine, ConsoleColor.Gray, reportMode);
                txtReportList.Add(txtLine);
                if (suitable)
                {
                    ConsoleColor cc = ConsoleColor.Green;
                    string summaryText = "This computer is suitable to run the " + studyName + " test system.";
                    if (info.DoApplicationStartAfterCheck | info.DoApplicationStartBeforeCheck)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        cc = ConsoleColor.DarkYellow;
                        summaryText += "\nPlease consider that the IRTlibPlayer diagnosis wasn't processed.";
                    }
                    txtLine = summaryText;
                    ConsoleWriteLine(txtLine, cc, reportMode);
                    txtReportList.Add(txtLine);
                    //if (!Silent) Console.WriteLine(summaryText);
                    info.OverallResult = summaryText;
                }
                else
                {
                    txtLine = string.Format("\n{0}\n{1}\n{2}\n\n{3}\n", "One or more checks of the system diagnose have failed or", "maybe there are missing answers of the IRTlibPlayer diagnosis.", "Please check the output for details.", "This computer is not suitable to run the " + studyName + " test system.");
                    ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                    txtReportList.Add(txtLine);
                    info.OverallResult = "This computer is not suitable to run the " + studyName + " test system.";
                }
                Console.WriteLine(" ");
                Console.ResetColor();
                //Overall result -

                
                #endregion

                #region WRITERESULTS_FILES
                Console.ResetColor();

                //text report
                resultFileNameText = resultFileNameText + currentTime + ".txt";
                string _filePath = System.IO.Path.Combine(strOutputPath, resultFileNameText);

                try
                {
                    string fileContent = "";

                    foreach(string _reportString in txtReportList)
                    {
                        fileContent += _reportString;
                    }
                    //write the collected content to a file
                    File.WriteAllText(_filePath, fileContent);
                    ConsoleWriteLine("Report written to file " + _filePath + "\n", ConsoleColor.Gray, reportMode);
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }

                if ((info.DoApplicationStartAfterCheck && !info.PlayerStarted) || (!info.DoApplicationStartAfterCheck))
                {
                    try
                    {
                        Process.Start("notepad.exe", _filePath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("The process failed: {0}", e.ToString());
                    }
                }

                //write the results to a yaml file
                resultFileNameYaml = resultFileNameYaml + currentTime + ".yaml";
                resultFilePathYaml = System.IO.Path.Combine(strOutputPath, resultFileNameYaml);
                StringBuilder sb = new StringBuilder();
                StringWriter writer = new StringWriter(sb);

                var serializer = new SerializerBuilder()
                .Build();

                serializer.Serialize(writer, checkResults);

                using (StreamWriter streamWriter = new StreamWriter(resultFilePathYaml))
                {
                    streamWriter.Write(writer.ToString());
                }
                if (!Silent) { Console.WriteLine("ReadinessTool results written to file " + resultFilePathYaml + "\n"); }

                //write the results to a json file
                resultFileNameJson = resultFileNameJson + currentTime + ".json";
                resultFilePathJson = System.IO.Path.Combine(strOutputPath, resultFileNameJson);

                string jsonString = System.Text.Json.JsonSerializer.Serialize(checkResults);
                File.WriteAllText(resultFilePathJson, jsonString);
                if (!Silent) { Console.WriteLine("ReadinessTool results written to file " + resultFilePathJson + "\n"); }

                #endregion

            }
            catch (Exception ex)
            {
                txtLine = string.Format("\nProgram interrupted due to unexpected error:{0}\n{1}\n{2}\n", ex.GetType(), ex.Message, ex.StackTrace);
                ConsoleWriteLine(txtLine, ConsoleColor.Red, reportMode);
                txtReportList.Add(txtLine);
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

        private static void ConsoleWrite(string outString, ConsoleColor outColor, ReportMode reportMode)
        {
            if (reportMode > ReportMode.Silent)
            {
                Console.ForegroundColor = outColor;
                Console.Write(outString);
                Console.ResetColor();
            }
        }
        private static void ConsoleWriteLine(string outString, ConsoleColor outColor, ReportMode reportMode)
        {
            if(reportMode > ReportMode.Silent)
            {
                Console.ForegroundColor = outColor;
                Console.WriteLine(outString);
                Console.ResetColor();
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