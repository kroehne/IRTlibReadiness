using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace ReadinessTool
{
    class NetworkConnectivityCheck : ReadinessCheck
    {
        ResultType PingCheckResult = ResultType.failed;
        ResultType URLaccessCheckResult = ResultType.failed;

        public NetworkConnectivityCheck() : base(false, ReportMode.Info)
        {
        }

        public NetworkConnectivityCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK_URLaccess = true;
            ResultType CheckResult_URLaccess = ResultType.failed;
            string ResultInfo_URLaccess = "";
            bool checkConfigurationOK_Ping = true;
            ResultType CheckResult_Ping = ResultType.failed;
            string ResultInfo_Ping = "";

            // URL access check +

            ValidValue webClientURL = checkValue.ValidValues.Find(item => item.name == "WebClientURL");
            ValidValue webClientURLaccessExpected = checkValue.ValidValues.Find(item => item.name == "WebClientURLaccessExpected");

            if(webClientURL == null || webClientURLaccessExpected == null)
            {
                // one of the values is incorrect
                ResultInfo_URLaccess = String.Format("Access a Web URL: invalid check configuration ");
                checkConfigurationOK_URLaccess = false;
            }

            if (checkConfigurationOK_URLaccess)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        using (client.OpenRead(webClientURL.value))
                        {
                            CheckResult_URLaccess = ResultType.succeeded;
                            ResultInfo_URLaccess = String.Format("Access to {0} successful", webClientURL.value);
                        }
                    }
                }
                catch
                {
                    ResultInfo_URLaccess = String.Format("Access to {0} failed", webClientURL.value);
                }
            }

            // URL access check -

            // Ping check +

            ValidValue pingURL = checkValue.ValidValues.Find(item => item.name == "PingURL");
            ValidValue pingURLaccessExpected = checkValue.ValidValues.Find(item => item.name == "PingURLaccessExpected");

            if (pingURL == null || pingURLaccessExpected == null)
            {
                ResultInfo_Ping = String.Format("Ping check: invalid check configuration ");
                checkConfigurationOK_Ping = false;
            }

            if (checkConfigurationOK_Ping)
            {

                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send(pingURL.value);
                        if (reply != null && reply.Status == IPStatus.Success)
                        {
                            CheckResult_Ping = ResultType.succeeded;
                            ResultInfo_Ping = String.Format("Ping of {0} successful", pingURL.value);
                        }
                        else
                        {
                            ResultInfo_Ping = String.Format("Ping of {0} failed", pingURL.value);
                        }
                    }
                }
                catch
                {
                    ResultInfo_Ping = String.Format("Ping of {0} failed", pingURL.value);
                }
            }

            // Ping check -

            if(CheckResult_URLaccess == ResultType.succeeded && CheckResult_Ping == ResultType.succeeded)
            {
                checkResult.Result = ResultType.succeeded;
            }

            checkResult.ResultInfo = string.Format("Network Connectivity: Ping = {0}, OpenRead = {1}", ResultInfo_URLaccess, ResultInfo_Ping);
            if (reportMode == ReportMode.Verbose)
            {
                Console.WriteLine(checkResult.ResultInfo);
            }

            return ((checkConfigurationOK_URLaccess == true) && (checkConfigurationOK_Ping == true));
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = checkResult.ResultInfo;

            return resultString;

        }
    }
}
