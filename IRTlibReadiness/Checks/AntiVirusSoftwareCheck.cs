using System;
using System.Collections.Generic;
using System.Text;
using System.Management;


namespace ReadinessTool
{
    class AntiVirusSoftwareCheck : ReadinessCheck
    {
        List<string> VirusDetails = null;
        //const string className = "AntiVirusSoftwareCheck";

        public AntiVirusSoftwareCheck() : base(false, ReportMode.Info)
        {
            VirusDetails = new List<string>();
        }

        public AntiVirusSoftwareCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
            VirusDetails = new List<string>();
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;
            bool WMIexceptionOccurred = false;

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
                        try { var ts = instance.GetPropertyValue("displayName"); displayName = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection error: Property displayName not found"); }
                        try { var ts = instance.GetPropertyValue("productState"); productState = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection error: Property productState not found"); }
                        try { var ts = instance.GetPropertyValue("timestamp"); timestamp = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection error: Property timestamp not found"); }
                        try { var ts = instance.GetPropertyValue("pathToSignedProductExe"); pathToSignedProductExe = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection error: Property pathToSignedProductExe not found"); }
                        try { var ts = instance.GetPropertyValue("pathToSignedReportingExe"); pathToSignedReportingExe = ts.ToString(); } catch (Exception e) { Console.WriteLine("\nVirus software detection error: Property pathToSignedReportingExe not found"); }

                        VirusDetails.Add(String.Format("Name: {0}, State {1}, Timestamp {2}, ProductExe {3}, ReportingExe: {4}",
                            displayName,
                            productState,
                            timestamp,
                            pathToSignedProductExe,
                            pathToSignedReportingExe));
                    }

                    if (reportMode == ReportMode.Verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" - Virus Applications: {0} Application(s) found", VirusDetails.Count);
                        foreach (var s in VirusDetails)
                            Console.WriteLine("   " + s);
                    }
                }
            }
            catch (Exception e)
            {
                WMIexceptionOccurred = true;
                ConsoleColor cc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" - Could not run the Virus software detection. This may not denote that the application can't be run on this computer.");
                //Console.WriteLine("\nPlease check the .net Framework version installed on your system.");
                //Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                //Console.WriteLine(e.StackTrace);
                Console.ForegroundColor = cc;
                checkResult.ResultInfo = "Could not run the Virus software detection. This may not denote that the application can't be run on this computer.";
            }

            if (!WMIexceptionOccurred)
            {

                ValidValue antiVirusSoftwareExpected = checkValue.ValidValues.Find(item => item.name == "AntiVirusSoftwareExpected");
                if (antiVirusSoftwareExpected != null)
                {
                    if (antiVirusSoftwareExpected.value.ToLower().Equals("true")) { if (VirusDetails.Count > 0) checkResult.Result = ResultType.succeeded; }

                    checkResult.ResultInfo = "Anti virus software:";
                    foreach (var s in VirusDetails)
                    {
                        string[] ss = s.Split(',');
                        if (ss.Length > 0) checkResult.ResultInfo += String.Format(" {0},", ss[0]);
                    }

                    checkResult.ResultInfo += String.Format(" (expected: {0})", antiVirusSoftwareExpected.value);
                }
            }

            return checkConfigurationOK;

        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString += String.Format("- Virus Applications: {0} Application(s) found\n", VirusDetails.Count);
            foreach (var s in VirusDetails)
                resultString += "   " + s + "\n";
            resultString += "\n";

            return resultString;

        }
    }
}
