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
        public void SetDefaults(){

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
                parameterValue = new ParameterValue();
                parameterValue.PurposeInfo = "Determines if the Player should be started";
                parameterValue.AllowedValuesInfo = new string[3] { "startbefore", "startafter", "nostart" };
                parameterValue.Value = "nostart";
                this.Parameters.Add(parameterKey, parameterValue);
            }

            parameterKey = "ReadinessMode";
            if (!this.Parameters.ContainsKey(parameterKey))
            {
                parameterValue = new ParameterValue();
                parameterValue.PurposeInfo = "Output of the Readiness tool";
                parameterValue.AllowedValuesInfo = new string[2] { "silent", "verbose" };
                parameterValue.Value = "verbose";
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

            checkValueKey = "OperatingSystemCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the operating system generally is suitable",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "operating system name"
                };
                checkValue.ValidValues.Add(new ValidValue("OS", "Windows 7"));
                checkValue.ValidValues.Add(new ValidValue("OS", "Windows 8"));
                checkValue.ValidValues.Add(new ValidValue("OS", "Windows 10"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "OperatingSystem64bitCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the operating system has 64bit architecture",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "Is64bit , true|false"
                };
                checkValue.ValidValues.Add(new ValidValue("Is64bit", "true"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "OperatingSystemTypeCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the operating system is suitable",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "operating system property"
                };
                checkValue.ValidValues.Add(new ValidValue("64bitexpected", "true"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "UserRoleCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {

                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the user role is suitable",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "role"
                };
                checkValue.ValidValues.Add(new ValidValue("role", "Administrator"));
                checkValue.ValidValues.Add(new ValidValue("role", "User"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            //Memory checks+
            checkValueKey = "MemoryInstalledCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {

                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if there is enough memory installed",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "GB"
                };
                checkValue.ValidValues.Add(new ValidValue("MinimalMemoryInstalled", "2"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "MemoryAvailableCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {

                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if there is enough memory available",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "GB"
                };
                checkValue.ValidValues.Add(new ValidValue("MinimalMemoryAvailable", "0,5"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }
            //Memory checks-

            checkValueKey = "TouchScreenCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the device has a touch screen",
                    OptionalCheck = true,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "-"
                };
                checkValue.ValidValues.Add(new ValidValue("TouchScreenExpected", "true"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "AntiVirusSoftwareCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks for anti virus software",
                    OptionalCheck = true,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "-"
                };
                checkValue.ValidValues.Add(new ValidValue("AntiVirusSoftwareExpected", "true"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "ScreenResolutionCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the horizontal screen resolution ist sufficient",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "Pixels"
                };
                checkValue.ValidValues.Add(new ValidValue("MinimalHorizontalRes", "1024"));
                checkValue.ValidValues.Add(new ValidValue("MinimalVerticalRes", "768"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "NetworkConnectivityCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the internet is reachable",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "-"
                };
                checkValue.ValidValues.Add(new ValidValue("WebClientURL", "http://www.google.com/"));
                checkValue.ValidValues.Add(new ValidValue("WebClientURLaccessExpected", "true"));
                checkValue.ValidValues.Add(new ValidValue("PingURL", "www.google.com"));
                checkValue.ValidValues.Add(new ValidValue("PingURLaccessExpected", "true"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "RegistryKeyCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Registry check. Apply keys, vars and expected values or 'not set'",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "-"
                };
                checkValue.ValidValues.Add(new ValidValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\;DisableLockWorkstation", "not set"));

                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "PortRangeAvailableCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the ports needed by the player are available. Specify a range of ports",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "port number"
                };
                checkValue.ValidValues.Add(new ValidValue("FirstPort", "8000"));
                checkValue.ValidValues.Add(new ValidValue("LastPort", "8999"));
                checkValue.ValidValues.Add(new ValidValue("MinimumPortsFree", "10"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "PortAvailableCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if the ports needed by the player are available. Specify a list of ports",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "port number"
                };
                checkValue.ValidValues.Add(new ValidValue("Port", "8000"));
                checkValue.ValidValues.Add(new ValidValue("Port", "8001"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "FoldersWritableCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if a folder is writable.",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "Folder, <FolderName> or one of [USERTEMPFOLDER, ROOTDRIVE]"
                };
                checkValue.ValidValues.Add(new ValidValue("Folder", "USERTEMPFOLDER"));
                checkValue.ValidValues.Add(new ValidValue("Folder", "ROOTDRIVE"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "FoldersFreeSpaceCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if a folder has sufficient free space",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "<FolderName> or one of [USERTEMPFOLDER, ROOTDRIVE], expected free space in MB"
                };
                checkValue.ValidValues.Add(new ValidValue("C:\\Users\\<USER>\\AppData\\Local\\Temp\\", "500"));
                checkValue.ValidValues.Add(new ValidValue("C:\\", "1024"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "DriveSpeedCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks the data transfer speed",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "MB/s"
                };
                checkValue.ValidValues.Add(new ValidValue("MinimalSpeedRead", "100"));
                checkValue.ValidValues.Add(new ValidValue("MinimalSpeedWrite", "100"));
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "AudioMidiToneCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if a midi tone can be played",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "-"
                };
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "AudioDevicesCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks if there are audio devices",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "-"
                };
                this.CheckRanges.TryAdd(checkValueKey, checkValue);
            }

            checkValueKey = "ExternalSoftwareCheck";
            if (!this.CheckRanges.ContainsKey(checkValueKey))
            {
                checkValue = new CheckValue
                {
                    PurposeInfo = "Checks for external programs to exist",
                    OptionalCheck = false,
                    RunThisCheck = true,
                    ValidValues = new List<ValidValue>(),
                    UnitInfo = "Folder , Program file name"
                };
                checkValue.ValidValues.Add(new ValidValue("", "TestApp.Player.Chromely.exe"));
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
        }

        public CheckValue(bool runThisCheck, bool optionalCheck, string purposeInfo)
        {
            PurposeInfo = purposeInfo;
            RunThisCheck = runThisCheck;
            OptionalCheck = optionalCheck;
            UnitInfo = "";
            ValidValues = new List<ValidValue>();

        }
        public string PurposeInfo { get; set; }
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
}
