using Saplin.StorageSpeedMeter;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReadinessTool
{
    class DriveSpeedCheck : ReadinessCheck
    {

        public const long fileSize = 1024 * 1024 * 1024;
        public const string unit = "MB/s";

        public DriveSpeedCheck() : base(false, ReportMode.Info)
        {
        }

        public DriveSpeedCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            double ReadScore = 0;
            double WriteScore = 0;
            string Executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string RootDrive = System.IO.Path.GetPathRoot(Executable);

            List<string> SpeedDetails = new List<string>(); 

            try
            {
                var bigTest = new BigTest(RootDrive, fileSize, false);
                using (bigTest)
                {
                    if (reportMode > ReportMode.Silent)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("\n");
                        Console.Write(" - Speed Test for file: {0}, Size: {1:0.00}Gb\n   Press ESC to break", bigTest.FilePath, (double)bigTest.FileSize / 1024 / 1024 / 1024);
                        Console.ResetColor();
                    }

                    string currentTest = null;
                    const int curCursor = 40;
                    var breakTest = false;

                    bigTest.StatusUpdate += (sender, e) =>
                    {
                        if (breakTest) return;
                        if (e.Status == TestStatus.NotStarted) return;

                        if ((sender as Test).DisplayName != currentTest)
                        {
                            currentTest = (sender as Test).DisplayName;
                            if (reportMode > ReportMode.Silent)
                                Console.Write("\n   * {0}/{1} {2}", bigTest.CompletedTests + 1, bigTest.TotalTests, (sender as Test).DisplayName);
                        }

                        if (reportMode > ReportMode.Silent) ClearLine(curCursor);

                        if (e.Status != TestStatus.Completed)
                        {
                            if (reportMode > ReportMode.Silent)
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                            switch (e.Status)
                            {
                                case TestStatus.Started:
                                    if (reportMode > ReportMode.Silent)
                                        Console.Write("Started");
                                    break;
                                case TestStatus.InitMemBuffer:
                                    if (reportMode > ReportMode.Silent)
                                        Console.Write("Initializing test data in RAM...");
                                    break;
                                case TestStatus.PurgingMemCache:
                                    if (reportMode == ReportMode.Verbose)
                                        Console.Write("Purging file cache in RAM...");
                                    break;
                                case TestStatus.WarmigUp:
                                    if (reportMode > ReportMode.Silent)
                                        Console.Write("Warming up...");
                                    break;
                                case TestStatus.Interrupted:
                                    if (reportMode > ReportMode.Silent)
                                        Console.Write("Test interrupted");
                                    break;
                                case TestStatus.Running:
                                    if (reportMode > ReportMode.Silent)
                                        Console.Write("{0}% {2} {1:0.00} MB/s", e.ProgressPercent, e.RecentResult, GetNextAnimation());
                                    break;
                            }
                            if (reportMode > ReportMode.Silent)
                                Console.ResetColor();
                        }
                        else if ((e.Status == TestStatus.Completed) && (e.Results != null))
                        {

                            SpeedDetails.Add(String.Format("{5} -- Avg: {1} {0}, Min: {2} {0}, Max: {3} {0}, Time: {4} ms", unit, e.Results.AvgThroughput, e.Results.Min, e.Results.Max, e.ElapsedMs, e.Results.TestDisplayName));
                            if (reportMode > ReportMode.Silent)
                            {
                                Console.Write(string.Format("Avg: {1:0.00}{0}\t", unit, e.Results.AvgThroughput));
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Write(
                                    string.Format(" Min÷Max: {1:0.00} ÷ {2:0.00}, Time: {3}m{4:00}s",
                                    unit,
                                    e.Results.Min,
                                    e.Results.Max,
                                    e.ElapsedMs / 1000 / 60,
                                    e.ElapsedMs / 1000 % 60)
                                );
                                Console.ResetColor();
                            }

                        }

                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                        {
                            if (reportMode > ReportMode.Silent)
                                Console.WriteLine("  Stopping...");
                            breakTest = true;
                            bigTest.Break();
                            //info.DoDriveSpeedTest = false;
                        }

                        if (reportMode == ReportMode.Verbose) ShowCounters(bigTest);
                    };

                    var results = bigTest.Execute();

                    if (reportMode == ReportMode.Verbose)
                        HideCounters();

                    if (!breakTest)
                    {
                        ReadScore = bigTest.ReadScore;
                        WriteScore = bigTest.WriteScore;

                        if (results != null)
                        {
                            //overwrite the values read from bigTest
                            foreach (TestResults tr in results)
                            {
                                if (tr.TestName.Equals("SequentialWriteTest")) WriteScore = tr.AvgThroughput;
                                if (tr.TestName.Equals("SequentialReadTest")) ReadScore = tr.AvgThroughput;
                            }
                        }

                        if (reportMode > ReportMode.Silent)
                        {
                            if (reportMode == ReportMode.Verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine("\n   Test file deleted.");
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\n - Drive Speed (sequential): Read {0:0.00} MB/s, sequential Write {1:0.00} MB/s", ReadScore, WriteScore);
                            Console.ResetColor();
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nSpeed test failed with an unexpected error:");
                Console.WriteLine("\t" + e.GetType() + " " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            if (checkValue != null)
            {

                ValidValue minimalSpeedRead = checkValue.ValidValues.Find(item => item.name == "MinimalSpeedRead");
                ValidValue minimalSpeedWrite = checkValue.ValidValues.Find(item => item.name == "MinimalSpeedWrite");

                if (minimalSpeedRead != null && minimalSpeedWrite != null)
                {
                    double msr = -1;
                    double msw = -1;
                    try
                    {
                        msr = Convert.ToDouble(minimalSpeedRead.value);
                        msw = Convert.ToDouble(minimalSpeedWrite.value);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("Unable to convert '{0}' or '{1}' to a Double.", minimalSpeedRead.value, minimalSpeedWrite.value);
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("'{0}' or '{1}' is outside the range of a Double.", minimalSpeedRead.value, minimalSpeedWrite.value);
                    }

                    if (msr > -1 && msw > -1)
                    {
                        if (ReadScore >= msr && WriteScore >= msw) checkResult.Result = ResultType.succeeded;

                        checkResult.ResultInfo = String.Format("ReadScore (sequential): {0:0.00} (expected: {1}) WriteScore (sequential): {2:0.00} (expected: {3})",
                            ReadScore,
                            msr,
                            WriteScore,
                            msw);
                    }
                }

            }

            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {
            string resultString = "";

            resultString = String.Format("- Current user role: {0} \n", "");

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks the data transfer speed",
                OptionalCheck = true,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "MB/s",
                CheckExec = CheckExecution.diagnoseMode
            };
            checkValue.ValidValues.Add(new ValidValue("MinimalSpeedRead", "20"));
            checkValue.ValidValues.Add(new ValidValue("MinimalSpeedWrite", "7"));

            return checkValue;

        }

        private static void ShowCounters(TestSuite ts)
        {

            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            var elapsedSecs = ts.ElapsedMs / 1000;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (prevElapsedSecs != elapsedSecs)
            {
                var elapsed = string.Format("                          Elapsed: {0:00}m {1:00}s", elapsedSecs / 60, elapsedSecs % 60);
                Console.CursorLeft = Console.WindowWidth - elapsed.Length - 1;
                Console.CursorTop = 0;
                Console.Write(elapsed);

                var remaing = string.Format("                          Remaining: {0:00}m {1:00}s", ts.RemainingMs / 1000 / 60, ts.RemainingMs / 1000 % 60);
                Console.CursorLeft = Console.WindowWidth - remaing.Length - 1;
                Console.CursorTop = 1;
                Console.Write(remaing);

                prevElapsedSecs = elapsedSecs;
            }

            Console.CursorLeft = left;
            Console.CursorTop = top;
            Console.ResetColor();
        }

        private static void HideCounters()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;

            var elapsed = "                                                   ";
            Console.CursorLeft = Console.WindowWidth - elapsed.Length - 1;
            Console.CursorTop = 0;
            Console.Write(elapsed);

            var remaing = "                                                   ";
            Console.CursorLeft = Console.WindowWidth - remaing.Length - 1;
            Console.CursorTop = 1;
            Console.Write(remaing);

            Console.CursorLeft = left;
            Console.CursorTop = top;
            Console.ResetColor();
        }
        private static void ClearLine(int cursorLeft)
        {
            Console.CursorLeft = cursorLeft;
            Console.Write(new string(' ', Console.WindowWidth - cursorLeft - 1));
            Console.CursorLeft = cursorLeft;
        }

        static char[] anim = new char[] { '/', '|', '\\', '-', '/', '|', '\\', '-' };
        static int animCounter = 0;
        static long prevElapsedSecs = 0;


        private static char GetNextAnimation()
        {
            animCounter++;
            return anim[animCounter % anim.Length];
        }

    }
}
