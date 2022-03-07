using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;



namespace ReadinessTool
{
    class TouchScreenCheck : ReadinessCheck
    {
        private bool TouchEnabledInSystem = false;

        public TouchScreenCheck() : base(false, ReportMode.Info)
        {
        }

        public TouchScreenCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            TouchEnabledInSystem = IsTouchEnabled();

            ValidValue touchScreenExpected = checkValue.ValidValues.Find(item => item.name == "TouchScreenExpected");
            if (touchScreenExpected != null)
            {
                bool _touchScreenExpected = touchScreenExpected.value.ToLower().Equals("true");

                if (TouchEnabledInSystem == _touchScreenExpected) checkResult.Result = ResultType.succeeded;
                checkResult.ResultInfo = String.Format("Touch screen present: {0} (expected: {1})", TouchEnabledInSystem, touchScreenExpected.value);
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

            resultString = String.Format("- Touch screen enabled: {0} \n", TouchEnabledInSystem);

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the device has a touch screen",
                OptionalCheck = true,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "-"
            };
            checkValue.ValidValues.Add(new ValidValue("TouchScreenExpected", "false"));

            return checkValue;

        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        public static bool IsTouchEnabled()
        {

            const int MAXTOUCHES_INDEX = 95;
            int maxTouches = GetSystemMetrics(MAXTOUCHES_INDEX);

            return maxTouches > 0;
        }

    }
}
