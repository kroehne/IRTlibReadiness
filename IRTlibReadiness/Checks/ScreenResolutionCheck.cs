using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class ScreenResolutionCheck : ReadinessCheck
    {
        int minScreenResH = 0;
        int minScreenResV = 0;

        List<string> MonitorDetails = new List<string>();

        public ScreenResolutionCheck() : base(false, ReportMode.Info)
        {
        }

        public ScreenResolutionCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            try
            {
                ValidValue ValidValueMinScreenResH = checkValue.ValidValues.Find(item => item.name == "MinimalHorizontalRes");
                ValidValue ValidValueMinScreenResV = checkValue.ValidValues.Find(item => item.name == "MinimalVerticalRes");

                // convert the values to a comparable type +
                if (ValidValueMinScreenResH != null && ValidValueMinScreenResV != null)
                {
                    try
                    {
                        minScreenResH = Convert.ToInt32(ValidValueMinScreenResH.value);
                        minScreenResV = Convert.ToInt32(ValidValueMinScreenResV.value);
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("{0} or {1} is outside the range of the Int32 type.", ValidValueMinScreenResH.value, ValidValueMinScreenResV.value);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("The {0} or {1} value '{2}' or {3} is not in a recognizable format.",
                                            ValidValueMinScreenResH.value.GetType().Name, ValidValueMinScreenResV.value.GetType().Name, ValidValueMinScreenResH.value,ValidValueMinScreenResV .value);
                    }
                }
                else
                {
                    checkResult.ResultInfo = "Config data incomplete, using defaults: " + className;
                    checkConfigurationOK = false;
                }
                // convert the values to a comparable type -

                //info.MinimalScreenSizeCheck = false;
                //info.MinimalScreenSize = String.Format("{0}x{1}", info.MinimalWidth, info.MinimalHeight);
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
                    MonitorDetails.Add(String.Format("{0}: {1}x{2}-{3}", _monitorName, _mode.dmPelsWidth, _mode.dmPelsHeight, _monitorState));

                    //check the size
                    //at least one of the displays must meet the check values
                    if (_mode.dmPelsWidth >= minScreenResV && _mode.dmPelsHeight >= minScreenResH)
                    {
                        checkResult.Result = ResultType.succeeded;
                        //add all monitors which have the recommended resolution
                        checkResult.ResultInfo += String.Format("{0}: {1}x{2}-{3}", _monitorName, _mode.dmPelsWidth, _mode.dmPelsHeight, _monitorState);

                    }
                }//end foreach

                if(checkResult.Result != ResultType.succeeded)
                {
                    checkResult.ResultInfo = "No suitable monitors found";
                }
                //if there were no suitable monitors found add a negative check result
                //if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { if (!checkValue.OptionalCheck) checkResults.OverallResult = false; checkResults.CheckResultMap.Add(checkInfo, new CheckResult(ResultType.failed, "No suitable monitors found")); }

                if (reportMode == ReportMode.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Displays: {0} device(s) (Minimal Size: {1} - {2}) ", MonitorDetails.Count, String.Format("{0}X{1}", minScreenResH, minScreenResV), checkResult.Result);
                    foreach (var s in MonitorDetails)
                        Console.WriteLine("   " + s);

                }
            }//end try
            catch (Exception e)
            {
                ConsoleColor cc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("\nReading graphic devices failed with an unexpected error:");
                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                Console.WriteLine(e.StackTrace);

                Console.ForegroundColor = cc;
                //in case an error occurred add a negative check result
                checkResult.ResultInfo = "Check has failed. An unexpected error occurred";
                //if (!checkResults.CheckResultMap.ContainsKey(checkInfo)) { if (!checkValue.OptionalCheck) checkResults.OverallResult = false; checkResults.CheckResultMap.Add(checkInfo, new CheckResult(ResultType.failed, "Check has failed. An unexpected error occurred")); }
            }

            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format(" - Displays: {0} device(s) (Minimal Size: {1} - {2}) ", MonitorDetails.Count, String.Format("{0}X{1}", minScreenResH, minScreenResV), checkResult.Result);
            //Console.WriteLine(" - Displays: {0} device(s) (Minimal Size: {1} - {2}) ", MonitorDetails.Count, String.Format("{0}X{1}", minScreenResH, minScreenResV), checkResult.Result);

            return resultString;

        }
    }
}
