using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace ReadinessTool
{
    class PortAvailableCheck : ReadinessCheck
    {
        public PortAvailableCheck() : base(false, ReportMode.Info)
        {
        }

        public PortAvailableCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            List<long> PortsToCheck = new List<long>();

            // check the check parameters +
            long _port = 0;

            foreach (ValidValue port in checkValue.ValidValues)
            {
                if (long.TryParse(port.value, out _port))
                {
                    PortsToCheck.Add(_port);
                }
                else
                {
                    checkConfigurationOK = false;
                    checkResult.ResultInfo += String.Format(" Wrong format: port {0};", port.value);
                }
            }
            // check the check parameters -


            if (checkConfigurationOK)
            {
                List<long> UsedPorts = getListOfUsedPorts();
                checkResult.Result = ResultType.succeeded;

                foreach (long _portToCheck in PortsToCheck)
                {
                    if (UsedPorts.Contains(_portToCheck))
                    {
                        checkResult.Result = ResultType.failed;
                        checkResult.ResultInfo += String.Format(" port {0} not available;", _portToCheck);
                    }
                    else 
                    { 
                        checkResult.ResultInfo += String.Format(" port {0} available;", _portToCheck); 
                    }
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

        private List<long> getListOfUsedPorts()
        {
            List<long> UsedPorts = new List<long>();

            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation c in connections)
                {
                    if (!UsedPorts.Contains(c.LocalEndPoint.Port))
                        UsedPorts.Add(c.LocalEndPoint.Port);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("\nPort listing failed with an unexpected error:");
                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            return UsedPorts;

        }

    }
}
