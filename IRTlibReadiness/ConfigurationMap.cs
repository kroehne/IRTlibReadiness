using System;
using System.Collections.Generic;
using System.Text;

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

            //Parameters +

            //create dictionary
            this.Parameters = new Dictionary<string, ParameterValue>();
            //Parameter
            ParameterValue parameterValue = null;

            parameterValue = new ParameterValue();
            parameterValue.PurposeInfo = "Determines if the Player should be started";
            parameterValue.AllowedValuesInfo = new string[3] { "startbefore", "startafter", "nostart" };
            parameterValue.Value = "nostart";
            this.Parameters.Add("ReadinessStartPlayer", parameterValue);

            parameterValue = new ParameterValue();
            parameterValue.PurposeInfo = "Output of the Readiness tool";
            parameterValue.AllowedValuesInfo = new string[2] { "silent", "verbose" };
            parameterValue.Value = "verbose";
            this.Parameters.Add("ReadinessMode", parameterValue);

            //Parameters -

            //Check values +
            //create dictionary
            this.CheckRanges = new Dictionary<string, CheckValue>();

            CheckValue checkValue = null;

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the operating system is suitable",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = -1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "operating system name"
            };
            checkValue.ValidValues.Add("Windows 7");
            checkValue.ValidValues.Add("Windows 8");
            checkValue.ValidValues.Add("Windows 10");
            this.CheckRanges.TryAdd("OperatingSystemCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the user role is suitable",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = -1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "role"
            };
            checkValue.ValidValues.Add("Administrator");
            checkValue.ValidValues.Add("User");
            this.CheckRanges.TryAdd("UserRoleCheck", checkValue);

            //Memory checks
            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if there is enough memory installed",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 2,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "GB"
            };
            this.CheckRanges.TryAdd("MemoryInstalledCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if there is enough memory available",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "GB"
            };
            this.CheckRanges.TryAdd("MemoryAvailableCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the device has a touch screen",
                OptionalCheck = true,
                RunThisCheck = true,
                Min = 1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "-"
            };
            this.CheckRanges.TryAdd("TouchScreenCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the horizontal screen resolution ist sufficient",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 1024,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "Pixels"
            };
            this.CheckRanges.TryAdd("HorizontalScreenResolutionCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the vertical screen resolution ist sufficient",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 768,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "Pixels"
            };
            this.CheckRanges.TryAdd("VerticalSreenResolutionCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the internet is reachable",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = -1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "-"
            };
            this.CheckRanges.TryAdd("NetworkConnectivityCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Registry key check. Apply keys and expected values or 'not set'",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = -1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "-"
            };
            checkValue.ValidValues.Add("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\");
            checkValue.ValidValues.Add("DisableLockWorkstation=1");
            this.CheckRanges.TryAdd("RegistryKeyCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the ports needed by the player are available. Apply a range or specify a list of ports",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 8000,
                Max = 8999,
                ValidValues = new List<String>(),
                UnitInfo = "port number"
            };
            this.CheckRanges.TryAdd("PortAvailableCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks if a folder is writable and the free space",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 500,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "kB"
            };
            checkValue.ValidValues.Add("C:\\Users\\<USER>\\AppData\\Local\\Temp\\");
            this.CheckRanges.TryAdd("FoldersWritableCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks the data transfer speed",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = 25,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "MB/s"
            };
            this.CheckRanges.TryAdd("ReadinessDriveSpeedCheck", checkValue);

            checkValue = new CheckValue
            {
                PurposeInfo = "Checks the audio capabilities",
                OptionalCheck = false,
                RunThisCheck = true,
                Min = -1,
                Max = -1,
                ValidValues = new List<String>(),
                UnitInfo = "-"
            };
            this.CheckRanges.TryAdd("ReadinessAudioCheck", checkValue);
            //Check values -

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
        public string PurposeInfo { get; set; }
        public bool RunThisCheck { get; set; }
        public bool OptionalCheck { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public string UnitInfo { get; set; }

        public List<String> ValidValues { get; set; }
    }

    public class CheckResult
    {
        public CheckResult() { }
        public CheckResult(bool result, string resultInfo) { Result = result; ResultInfo = resultInfo; }
        public string ResultInfo { get; set; }
        public bool Result  { get; set; }
    }

    public class CheckResults
    {
        public CheckResults()
        {
            CheckResultMap = new Dictionary<string, CheckResult>();
        }

        public Dictionary<string, CheckResult> CheckResultMap { get; set; }

    }

}
