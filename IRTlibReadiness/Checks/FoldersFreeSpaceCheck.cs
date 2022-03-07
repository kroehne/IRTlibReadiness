using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReadinessTool
{
    class FoldersFreeSpaceCheck : ReadinessCheck
    {
        public FoldersFreeSpaceCheck() : base(false, ReportMode.Info)
        {
        }

        public FoldersFreeSpaceCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            string Executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            checkResult.Result = ResultType.succeeded;
            checkResult.ResultInfo = "";

            foreach (ValidValue folder in checkValue.ValidValues)
            {
                if (folder.name.Contains("<USER>")) { folder.name = folder.name.Replace("<USER>", Environment.UserName.ToString()); };
                if (folder.name.ToUpper().Equals("USERTEMPFOLDER")) { folder.name = Path.GetTempPath(); };
                if (folder.name.ToUpper().Equals("ROOTDRIVE")) { folder.name = System.IO.Path.GetPathRoot(Executable); };

                try
                {
                    long freeBytesOut = 0;
                    long expectedFreeMB = 0;

                    try
                    {
                        expectedFreeMB = Convert.ToInt32(folder.value);//MB
                        if (NativeMethods.GetDiskFreeSpaceEx(folder.name, out freeBytesOut, out var _1, out var _2))//bytes
                        {
                            long freeMB = freeBytesOut / 1024 / 1024;
                            checkResult.ResultInfo += String.Format("Folder {0} free space {1}MB: ", folder.name, freeMB);
                            if (freeMB < expectedFreeMB)
                            {
                                checkResult.Result = ResultType.failed;
                                checkResult.ResultInfo += "NOK ";
                            }
                            else
                                checkResult.ResultInfo += "OK ";

                            checkResult.ResultInfo += String.Format(" (expected: {0}MB) ", expectedFreeMB);

                        }
                        else
                            checkResult.ResultInfo += "getting free space failed ";

                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("{0} is outside the range of the Int32 type.", folder.value);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("The {0} value is not in a recognizable format.",
                                            folder.value.GetType().Name);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nChecking disk space failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    checkResult.ResultInfo += " Checking disk space failed ";
                }
            }

            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = checkResult.ResultInfo;

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if a folder has sufficient free space",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "<FolderName> or one of [USERTEMPFOLDER, ROOTDRIVE], expected free space in MB"
            };
            checkValue.ValidValues.Add(new ValidValue("C:\\Users\\<USER>\\AppData\\Local\\Temp\\", "500"));
            checkValue.ValidValues.Add(new ValidValue("C:\\", "1024"));

            return checkValue;

        }
    }
}
