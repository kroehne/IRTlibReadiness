using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class MemoryInstalledCheck : ReadinessCheck
    {

        public MemoryInstalledCheck() : base(false, ReportMode.Info)
        {
        }

        public MemoryInstalledCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            ValidValue minimalMemoryInstalled = checkValue.ValidValues.Find(item => item.name == "MinimalMemoryInstalled");

            if (minimalMemoryInstalled != null)
            {
                try
                {
                    // unit: GB
                    double mmi = Convert.ToDouble(minimalMemoryInstalled.value);//GB
                    double tms = SystemInfo.totalRam  / 1024 / 1024 / 1024; //info.TotalRam / 1024 / 1024 / 1024;

                    if (tms >= mmi) checkResult.Result = ResultType.succeeded;
                    checkResult.ResultInfo = String.Format("Memory installed: {0:0.00}GB (expected: {1:0.00}GB)", tms, mmi);

                }
                catch (OverflowException)
                {
                    Console.WriteLine("{0} or {1} is outside the range of the Int32 type.", minimalMemoryInstalled.value, SystemInfo.totalRam);
                }
                catch (FormatException)
                {
                    Console.WriteLine("The {0} or {1} value is not in a recognizable format.",
                                        minimalMemoryInstalled.value, SystemInfo.totalRam);
                }
            }
            else
            {
                checkConfigurationOK = false;
            }


            return checkConfigurationOK;

        }
        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format("- Memory: Total RAM = {0:0.00}Gb\n", SystemInfo.totalRam / 1024 / 1024 / 1024);

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if there is enough memory installed",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "GB"
            };
            checkValue.ValidValues.Add(new ValidValue("MinimalMemoryInstalled", "2"));

            return checkValue;

        }
    }
}
