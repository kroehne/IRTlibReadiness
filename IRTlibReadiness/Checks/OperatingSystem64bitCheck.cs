using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class OperatingSystem64bitCheck : ReadinessCheck
    {
        bool Is64bitOS = false;

        public OperatingSystem64bitCheck() : base(false, ReportMode.Info)
        {
        }

        public OperatingSystem64bitCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            ValidValue validValue = checkValue.ValidValues.Find(item => item.name == "Is64bit");

            if(validValue != null)
            {
                Is64bitOS = Environment.Is64BitOperatingSystem;

                if (validValue.value.ToLower().Equals("true"))
                {
                    if (Is64bitOS) checkResult.Result = ResultType.succeeded;
                }
                checkResult.ResultInfo += String.Format(" 64bitOS is {0} (expected: {1})", Is64bitOS, validValue.value);
            }
            else
            {
                checkResult.ResultInfo = "Config data incomplete";
                checkConfigurationOK = false;
            }

            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format("- Operating system is 64 bit: {0} \n", Is64bitOS);

            return resultString;

        }

    }
}
