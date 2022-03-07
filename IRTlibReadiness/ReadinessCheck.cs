using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    public abstract class ReadinessCheck
    {

        public string className = "";
        bool checkScopeDiagnose = false;
        ReportMode reportMode = ReportMode.Info;
        public CheckResult checkResult = null;
        bool checkConfigurationOK = true;
        string purposeInfo = "Huch!";

        public ReadinessCheck() 
        {
            className = this.GetType().Name;
        }

        public ReadinessCheck(bool checkScopeDiag, ReportMode rm) {

            className = this.GetType().Name;

            checkScopeDiagnose = checkScopeDiag;
            reportMode = rm;
            checkResult = new CheckResult(ResultType.failed, "");
        }

        public void SetCheckScope(bool checkScopeDiag)
        {
            checkScopeDiagnose = checkScopeDiag;
        }

        public void SetReportMode(ReportMode rm)
        {
            reportMode = rm;
        }

        public CheckResult PerformCheck( ref ConfigurationMap configurationMap, ref CheckResults checkResults) {

            CheckValue checkValue = configurationMap.CheckRanges.TryGetValue(className, out checkValue) ? checkValue : new CheckValue(false, false, "Config data missing");
            purposeInfo = checkValue.PurposeInfo;

            if (!CanExecute(checkValue, checkScopeDiagnose))
            {
                checkResult.Result = checkValue.PurposeInfo.Equals("Config data missing") ? ResultType.failed : ResultType.skipped;
                checkResult.ResultInfo = checkValue.PurposeInfo;
            }
            else
            {
                checkConfigurationOK = Check(checkValue, reportMode);
            }

            if (!checkResults.CheckResultMap.ContainsKey(className)) { if (!checkValue.OptionalCheck) checkResults.OverallResult = checkResult.Result != ResultType.failed && checkResults.OverallResult; checkResults.CheckResultMap.Add(className, checkResult); }

            return checkResult;
        }

        public virtual bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            //override this method to implement the check procedure

            return true;

        }

        public virtual string GetInfoString()
        {
            //override this method to implement providing a formatted string

            return "";

        }

        public virtual string GetPurposeString()
        {
            return purposeInfo;
        }

        public virtual CheckValue GetConfigurationDefault()
        {
            return new CheckValue();

        }

        public static bool CanExecute(CheckValue checkValue, bool checkScopeDiag)
        {
            if (checkValue == null) return false;
            //don't execute the check if RunThisCheck is false
            if (!checkValue.RunThisCheck) return false;
            //run the check if it is configured to run in all modes
            if (checkValue.CheckExec == CheckExecution.always) return true;
            //if this check is for diagnose purposes only run the check if the diagnose mode is enabled
            if (checkValue.CheckExec == CheckExecution.diagnoseMode) return checkScopeDiag == true;
            //otherwise skip the check
            return false;

        }

    }
}
