using System;
using System.Collections.Generic;

namespace ReadinessTool
{
    public class ConfigurationMap
    {
	    public ConfigurationMap()
	    {
	    }
    
        public Dictionary<string, ParameterValue> Parameters { get; set; }
        public Dictionary<string, CheckValue> CheckRanges { get; set; }
        public void SetDefaults(List<ReadinessCheck> checkObjectList)
        {

            #region Parameter
            //Parameters +

            //create dictionary if not already done
            if (this.Parameters == null) this.Parameters = new Dictionary<string, ParameterValue>();

            //Parameter
            ParameterValue parameterValue = null;
            string parameterKey = "";

            parameterKey = "ReadinessStartPlayer";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue
                {
                    PurposeInfo = "Determines if the Player should be started",
                    AllowedValuesInfo = new string[3] { "startbefore", "startafter", "nostart" },
                    Value = "startafter"
                };
                this.Parameters.Add(parameterKey, parameterValue);
            }

            parameterKey = "ReadinessMode";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue
                {
                    PurposeInfo = "Output of the Readiness tool",
                    AllowedValuesInfo = new string[3] { "silent", "normal", "verbose" },
                    Value = "normal"
                };
                this.Parameters.Add(parameterKey, parameterValue);
            }

            parameterKey = "ReadinessOutputFolder";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue
                {
                    PurposeInfo = "Output of the Readiness tool",
                    AllowedValuesInfo = new string[3] { "Folder name", "USERTEMPFOLDER", "keep empty for the app folder" },
                    Value = @"..\ReadinessToolOutput"
                };
                this.Parameters.Add(parameterKey, parameterValue);
            }

            parameterKey = "ReadinessCheckScope";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue
                {
                    PurposeInfo = "Scope of the Readiness tool checks",
                    AllowedValuesInfo = new string[2] { "normal", "diagnose" },
                    Value = "normal"
                };
                this.Parameters.Add(parameterKey, parameterValue);
            }

            parameterKey = "StudyName";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue
                {
                    PurposeInfo = "Name of the current study used for text output",
                    AllowedValuesInfo = new string[1] { "Character string" },
                    Value = "Study"
                };
                this.Parameters.Add(parameterKey, parameterValue);
            }

            parameterKey = "LibPlayerChecks";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue
                {
                    PurposeInfo = "List of checks performed by the IRTlibPlayer separated by \",\"",
                    AllowedValuesInfo = new string[6] { "KIOSK", "TOUCH", "AUDIO", "TLMENU", "AreaVisible", "LinesVisible" },
                    Value = "KIOSK,TOUCH,AUDIO,TLMENU,AreaVisible,LinesVisible"
                };
                this.Parameters.Add(parameterKey, parameterValue);
            }

            //Parameters -
            #endregion

            #region Checks
            //Check values +
            //create dictionary
            if (this.CheckRanges == null) this.CheckRanges = new Dictionary<string, CheckValue>();

            CheckValue checkValue = null;
            string checkValueKey = "";

            foreach (ReadinessCheck rc in checkObjectList)
            {
                Type type = rc.GetType();
                checkValue = rc.GetConfigurationDefault();
                checkValueKey = type.Name;
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            //Check values -
            #endregion
        }
    }

    public class ParameterValue
    {
        public string PurposeInfo { get; set; }

        public string[] AllowedValuesInfo { get; set; }
        public string Value { get; set; }
    }
    public class CheckValue
    {
        public CheckValue() 
        {
            ValidValues = new List<ValidValue>();
            CheckExec = CheckExecution.always;
        }

        public CheckValue(bool runThisCheck, bool optionalCheck, string purposeInfo)
        {
            PurposeInfo = purposeInfo;
            RunThisCheck = runThisCheck;
            OptionalCheck = optionalCheck;
            UnitInfo = "";
            ValidValues = new List<ValidValue>();
            CheckExec = CheckExecution.always;
        }
        public CheckValue(bool runThisCheck, bool optionalCheck, string purposeInfo, CheckExecution checkExec)
        {
            PurposeInfo = purposeInfo;
            RunThisCheck = runThisCheck;
            OptionalCheck = optionalCheck;
            UnitInfo = "";
            ValidValues = new List<ValidValue>();
            CheckExec = checkExec;
        }
        public string PurposeInfo { get; set; }
        public CheckExecution CheckExec { get; set; }
        public bool RunThisCheck { get; set; }
        public bool OptionalCheck { get; set; }
        public string UnitInfo { get; set; }

        public List<ValidValue> ValidValues { get; set; }
    }

    public class ValidValue
    {
        public ValidValue() { }
        public ValidValue(string newName, string newValue) { name = newName; value = newValue; }
        public string name { get; set; }
        public string value { get; set; }

    }

    public class CheckResult
    {
        public CheckResult() { }
        public CheckResult(ResultType result, string resultInfo) { Result = result; ResultInfo = resultInfo; }
        public string ResultInfo { get; set; }
        public ResultType Result  { get; set; }
    }

    public class CheckResults
    {

        public CheckResults()
        {
            CheckResultMap = new Dictionary<string, CheckResult>();
            OverallResult = true;
        }

        //public bool OverallResult;
        public bool OverallResult { get; set; }
        public Dictionary<string, CheckResult> CheckResultMap { get; set; }

    }

    public enum ResultType
    {
        failed,
        succeeded,
        skipped
    }

    public enum CheckExecution
    {
        always,         //execute the check always
        diagnoseMode,   //execute only when in diagnose mode
        conditional     //execution depends on some conditions
    }

}
