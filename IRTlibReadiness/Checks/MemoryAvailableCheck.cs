using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class MemoryAvailableCheck : ReadinessCheck
    {

        public MemoryAvailableCheck() : base(false, ReportMode.Info)
        {
        }

        public MemoryAvailableCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            ValidValue minimalMemoryAvailable = checkValue.ValidValues.Find(item => item.name == "MinimalMemoryAvailable");
            if (minimalMemoryAvailable != null)
            {
                try
                {
                    // unit: GB
                    double mma = Convert.ToDouble(minimalMemoryAvailable.value);
                    double fms = SystemInfo.freeRam / 1024 / 1024 / 1024;

                    if (fms >= mma) checkResult.Result = ResultType.succeeded;
                    checkResult.ResultInfo = String.Format("Memory available: {0:0.00}GB (expected: {1:0.00}GB)", fms, mma);

                }
                catch (OverflowException)
                {
                    Console.WriteLine("{0} or {1} is outside the range of the Int32 type.", minimalMemoryAvailable.value, SystemInfo.freeRam);
                    checkConfigurationOK = false;
                }
                catch (FormatException)
                {
                    Console.WriteLine("The {0} or {1} value is not in a recognizable format.",
                                        minimalMemoryAvailable.value, SystemInfo.freeRam);
                    checkConfigurationOK = false;
                }
            }

            return checkConfigurationOK;

        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format("- Memory: Available RAM = {0:0.00}Gb\n", SystemInfo.freeRam / 1024 / 1024 / 1024);

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if there is enough memory available",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "GB"
            };
            checkValue.ValidValues.Add(new ValidValue("MinimalMemoryAvailable", "0,5"));

            return checkValue;

        }

    }
}
