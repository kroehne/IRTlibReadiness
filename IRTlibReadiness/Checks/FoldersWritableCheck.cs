using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace ReadinessTool
{
    class FoldersWritableCheck : ReadinessCheck
    {
        public FoldersWritableCheck() : base(false, ReportMode.Info)
        {
        }

        public FoldersWritableCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
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
                if (folder.value.Contains("<USER>")) { folder.value = folder.value.Replace("<USER>", Environment.UserName.ToString()); };
                if (folder.value.ToUpper().Equals("USERTEMPFOLDER")) { folder.value = Path.GetTempPath(); };
                if (folder.value.ToUpper().Equals("ROOTDRIVE")) { folder.value = System.IO.Path.GetPathRoot(Executable); };

                try
                {
                    checkResult.ResultInfo += String.Format("Folder {0}: ", folder.value);
                    if (!HasWritePermissionOnDir(folder.value))
                    {
                        checkResult.Result = ResultType.failed;
                        checkResult.ResultInfo += "NOK ";
                    }
                    else
                        checkResult.ResultInfo += "OK ";
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nChecking write permission failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    checkResult.ResultInfo += " Checking write permission failed ";
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

        private static bool HasWritePermissionOnDir(string path)
        {
            var writeAllow = false;
            var writeDeny = false;
            var accessControlList = new FileInfo(path).GetAccessControl();
            if (accessControlList == null)
                return false;
            var accessRules = accessControlList.GetAccessRules(true, true,
                                        typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules == null)
                return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    writeAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    writeDeny = true;
            }

            return writeAllow && !writeDeny;
        }

    }
}
