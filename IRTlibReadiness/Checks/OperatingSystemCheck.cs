using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace ReadinessTool
{
    class OperatingSystemCheck : ReadinessCheck
    {

        string OsName = "";

        public OperatingSystemCheck() : base(false, ReportMode.Info)
        {
        }

        public OperatingSystemCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            OsName = SystemInfo.FriendlyName();
            OsName = OsName != null ? OsName.ToString() : "Unknown";

            string validValues = "";
            foreach (ValidValue validValue in checkValue.ValidValues)
            {
                if (OsName.Contains(validValue.value)) checkResult.Result = ResultType.succeeded;
                validValues += String.Format("{0} ", validValue.value);
            }

            checkResult.ResultInfo += String.Format("OS name is \"{0}\" (expected: {1})", OsName, validValues);

            return true;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format("- Operating system: \"{0}\" \n", OsName);

            return resultString;

        }

        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the operating system generally is suitable",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "operating system name"
            };
            checkValue.ValidValues.Add(new ValidValue("OS", "Windows 8"));
            checkValue.ValidValues.Add(new ValidValue("OS", "Windows 10"));

            return checkValue;

        }
    }
}
