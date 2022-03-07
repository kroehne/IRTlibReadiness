using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace ReadinessTool
{

    class PortRangeAvailableCheck : ReadinessCheck
    {

        public PortRangeAvailableCheck() : base(false, ReportMode.Info)
        {
        }

        public PortRangeAvailableCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            long _firstPort = 0;
            long _lastPort = 0;
            long _minimumPortsFree = 0;

            // check the check parameters +

            ValidValue firstPort = checkValue.ValidValues.Find(item => item.name == "FirstPort");
            ValidValue lastPort = checkValue.ValidValues.Find(item => item.name == "LastPort");
            ValidValue minimumPortsFree = checkValue.ValidValues.Find(item => item.name == "MinimumPortsFree");

            if (firstPort == null || lastPort == null || minimumPortsFree == null)
            {
                checkConfigurationOK = false;
                checkResult.ResultInfo += "Config data partly missing";
            }
            else
            {
                if (!(long.TryParse(firstPort.value, out _firstPort) && long.TryParse(lastPort.value, out _lastPort) && long.TryParse(minimumPortsFree.value, out _minimumPortsFree)))
                {
                    checkConfigurationOK = false;
                    checkResult.ResultInfo += "Wrong config value format";
                }
            }
            // check the check parameters -

            if (checkConfigurationOK)
            {
                List<long> UsedPorts = GetListOfUsedPorts();
                long _freePorts = 0;
                //count the number of available ports within the range 
                for (long i = _firstPort; i <= _lastPort; i++)
                {
                    if (!UsedPorts.Contains(i)) _freePorts++;
                }

                if (_freePorts >= _minimumPortsFree) checkResult.Result = ResultType.succeeded;

                checkResult.ResultInfo = String.Format("{0} ports are available in the range of port {1} to port {2}", _freePorts, _firstPort, _lastPort);
                checkResult.ResultInfo += String.Format(" (expected: minimum {0} available ports)", _minimumPortsFree);

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
                PurposeInfo = "Checks if the ports needed by the player are available. Specify a range of ports",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "port number"
            };
            checkValue.ValidValues.Add(new ValidValue("FirstPort", "8000"));
            checkValue.ValidValues.Add(new ValidValue("LastPort", "8999"));
            checkValue.ValidValues.Add(new ValidValue("MinimumPortsFree", "10"));

            return checkValue;

        }


        private List<long> GetListOfUsedPorts()
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

                /*
                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Local TCP/IP ports: {0} ports used", info.UsedPorts.Count);
                    if (Verbose)
                    {
                        Console.WriteLine("    " + string.Join(",", info.UsedPorts));
                    }
                }
                */
                /*
                int _port = info.StartScanMin;
                int _i = 0;

                while (FreePorts.Count < info.RequiredNumberOfPorts & _i < info.NumberOfPortsToCheck)
                {
                    if (!UsedPorts.Contains(_port))
                    {
                        if (!IsPortOpen("127.0.0.1", _port, new TimeSpan(250)))
                        {
                            FreePorts.Add(_port);
                        }
                    }
                    _port++;
                    _i++;
                }
                */
                /*
                if (!Silent)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" - Check open TCP/IP ports: >= {0} of {1} ports available", FreePorts.Count, info.RequiredNumberOfPorts);
                    if (Verbose)
                    {
                        Console.WriteLine("    " + string.Join(",", FreePorts));
                    }
                }
                */
            }
            catch (Exception e)
            {
                Console.WriteLine("\nPort listing failed with an unexpected error:");
                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            return UsedPorts;

        }

        private static bool IsPortOpen(string host, int port, TimeSpan timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(timeout);
                    client.EndConnect(result);
                    return success;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
