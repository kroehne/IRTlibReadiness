using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace ReadinessTool
{
    class AudioDevicesCheck : ReadinessCheck
    {
        private List<string> AudioDetails = null;

        public AudioDevicesCheck() : base(false, ReportMode.Info)
        {
            AudioDetails = new List<string>();
        }

        public AudioDevicesCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
            AudioDetails = new List<string>();
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            try
            {
                var _audioSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
                var _audioCollection = _audioSearcher.Get();

                foreach (var d in _audioCollection)
                {
                    AudioDetails.Add(String.Format("Name: {0}, Status: {1}", d.GetPropertyValue("Name"), d.GetPropertyValue("Status")));

                    checkResult.Result = ResultType.succeeded;
                    checkResult.ResultInfo += String.Format(" ; Name: {0}, Status: {1}", d.GetPropertyValue("Name"), d.GetPropertyValue("Status"));

                }

                if (reportMode == ReportMode.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Audio: {0} device(s) (Test: {1}) ", AudioDetails.Count, checkResult.Result);
                    foreach (var s in AudioDetails)
                        Console.WriteLine("   " + s);
                }
            }
            catch (Exception e)
            {
                //WMIexceptionOccurred = true;

                ConsoleColor cc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                //Console.WriteLine(" - Could not run the Audio hardware detection. This may not denote that '" + studyName + "' can't be run on this computer.");
                Console.WriteLine(" - Could not run the Audio hardware detection. This may not denote that the application can't be run on this computer.");
                //Console.WriteLine("\nPlease check the .net Framework version installed on your system.");
                Console.ForegroundColor = cc;
                checkResult.ResultInfo = "Could not run the Audio hardware detection. This may not denote that the application can't be run on this computer.";
                //Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                //Console.WriteLine(e.StackTrace);
            }



            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {

            string resultString = String.Format(" - Audio: {0} device(s) (Test: {1}) ", AudioDetails.Count, checkResult.Result);
            Console.WriteLine(resultString);

            return resultString;

        }
    }
}
