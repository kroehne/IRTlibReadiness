using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class RegistryKeyCheck : ReadinessCheck
    {
        public RegistryKeyCheck() : base(false, ReportMode.Info)
        {
        }

        public RegistryKeyCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            List<string> RegistryKeys = new List<string>() { @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation" };
            List<string> RegistryDetails = new List<string>();

            try
            {
                //if (RegistryKeys.Count > 0)
                if (checkValue.ValidValues.Count > 0)
                {
                    checkResult.Result = ResultType.succeeded;

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

            try
            {
                if (RegistryKeys.Count > 0)
                {

                    foreach (var p in RegistryKeys)
                    {
                        var _parts = p.Split(";", StringSplitOptions.RemoveEmptyEntries);
                        string _result = (string)Registry.GetValue(_parts[0], _parts[1], "not set");
                        RegistryDetails.Add(String.Format("Key: {0}, Value: {1}, Result: {2}", _parts[0], _parts[1], _result));
                    }

                    if (reportMode == ReportMode.Verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" - Registry: Checked {0} keys/value-pairs (see output for details)", RegistryDetails.Count);

                        foreach (var s in RegistryDetails)
                            Console.WriteLine("   " + s);
                    }
                }

            }
            catch (Exception e)
            {
                ConsoleColor cc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("\n Reading registry failed with an unexpected error:");
                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                Console.WriteLine(e.StackTrace);

                Console.ForegroundColor = cc;
            }


            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = checkResult.ResultInfo;

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Registry check. Apply keys, vars and expected values or 'not set'",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "-"
            };
            checkValue.ValidValues.Add(new ValidValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation", "not set"));

            return checkValue;

        }
    }
}
