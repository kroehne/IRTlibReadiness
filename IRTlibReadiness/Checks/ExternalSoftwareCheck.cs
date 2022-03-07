using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class ExternalSoftwareCheck : ReadinessCheck
    {
        public ExternalSoftwareCheck() : base(false, ReportMode.Info)
        {
        }

        public ExternalSoftwareCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;


            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format("- Current user role: {0} \n", "");

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks for external programs to exist",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "Folder , Program file name",
                CheckExec = CheckExecution.conditional
            };
            checkValue.ValidValues.Add(new ValidValue("", "TestApp.Player.Chromely.exe"));

            return checkValue;

        }
    }
}
