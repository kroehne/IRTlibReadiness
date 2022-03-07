using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ReadinessTool
{
    class UserRoleCheck : ReadinessCheck
    {
        string Role = "";

        public UserRoleCheck() : base(false, ReportMode.Info)
        {
        }

        public UserRoleCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            try
            {
                string UserName = "Unknown";
                bool IsAdministrator = false;
                bool IsUser = false;
                bool IsGuest = false;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        UserName = identity.Name;
                        IsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
                        IsUser = principal.IsInRole(WindowsBuiltInRole.User);
                        IsGuest = principal.IsInRole(WindowsBuiltInRole.Guest);
                        /*
                        if (reportMode > ReportMode.Error)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(" - Current User: {0} (Roles: Administrator = {1}, User = {2}, Guest = {3})", UserName, IsAdministrator, IsUser, IsGuest);
                        }
                        */
                    }

                    if (IsAdministrator) Role = "Administrator";
                    if (IsUser) Role = "User";
                    if (IsGuest) Role = "Guest";

                    if (checkValue.ValidValues.Exists(item => item.value == Role)) checkResult.Result = ResultType.succeeded;
                    checkResult.ResultInfo = "Current users role is: " + Role;

                    string validRoles = "";
                    foreach (ValidValue validValue in checkValue.ValidValues)
                    {
                        validRoles += String.Format("{0} ", validValue.value);
                    }
                    checkResult.ResultInfo += String.Format(" (expected: {0})", validRoles);
                }
            }
            catch (Exception e)
            {
                
                if (reportMode > ReportMode.Silent)
                {
                    ConsoleColor cc = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nReading user account failed with an unexpected error:");
                    Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ForegroundColor = cc;
                }
                checkResult.ResultInfo = "Reading user account failed with an unexpected error";
            }

            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {

            string resultString = String.Format("- Current user role: {0} ", Role);

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
             CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if the user role is suitable",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "role"
            };
            checkValue.ValidValues.Add(new ValidValue("role", "Administrator"));
            checkValue.ValidValues.Add(new ValidValue("role", "User"));

            return checkValue;

        }
    }
}
