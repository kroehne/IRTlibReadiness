using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ReadinessTool
{
    public class ThreadWindowObserver
    {

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // State information used in the task.
        private string windowToWatchFor;
        private Process process;
        private int threadWaitTime = 2000; //ms
        private int threadWaitTimeMultiplier = 90; // 3 minutes
        private bool dbg = false;

        // The constructor obtains the state information.
        public ThreadWindowObserver(string text, Process number, bool debug = false)
        {
            windowToWatchFor = text;
            process = number;
            dbg = debug;
        }

        // The thread procedure performs the task, such as formatting
        // and printing a document.
        public void ThreadProc()
        {
            if (dbg) Console.WriteLine("Thread: observing window \"" + windowToWatchFor + "\"");

            int waitCnt = 0;
            string activeWin = "";

            //wait for the window to appear
            activeWin = GetActiveWindowTitle();
            if (dbg) Console.WriteLine("Thread: waiting for the window to observe...");
            while (!activeWin.Equals(windowToWatchFor) && waitCnt <= threadWaitTimeMultiplier)
            {
                Thread.Sleep(threadWaitTime);
                activeWin = GetActiveWindowTitle();
                waitCnt++;
                if (dbg) Console.WriteLine("Thread: waiting counter is " + waitCnt + ", active window is " + activeWin);
                Console.Write(".");
            }
            Console.WriteLine("*");

            if (dbg) Console.WriteLine("Thread: active window is \"" + activeWin + "\"");

            if (activeWin.Equals(windowToWatchFor))
            {
                if (dbg) Console.WriteLine("Thread: waiting for the window to observe to disappear... ");
                //wait for the window to disappear
                while (activeWin.Equals(windowToWatchFor))
                {
                    Thread.Sleep(threadWaitTime);
                    activeWin = GetActiveWindowTitle();
                }
                if (dbg) Console.WriteLine("Thread: window to observe has disappeared or is no more active.");

                if (dbg) Console.WriteLine("Thread: waiting for the process to exit... ");
                waitCnt = 0;
                while (!process.HasExited && waitCnt <= threadWaitTimeMultiplier)
                {
                    Thread.Sleep(threadWaitTime);
                    waitCnt++;
                    if (dbg) Console.WriteLine("Thread: waiting counter is " + waitCnt);
                }
                if (!process.HasExited)
                {
                    if (dbg) Console.WriteLine("Thread: process hasn't exited yet, will kill it manually (1).");
                    process.Kill();
                }
                else
                {
                    if (dbg) Console.WriteLine("Thread: process has exited.");
                }
            }
            else
            {   //the window to observe did not appear
                Console.WriteLine("Thread: the window to observe did not appear.");

                if (!process.HasExited)
                {
                    if (dbg) Console.WriteLine("Thread: process hasn't exited yet, will kill it manually (2).");
                    process.Kill();
                }
                else
                {
                    if (dbg) Console.WriteLine("Thread: process has exited or wasn't started.");
                }
            }
        }

        static private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            //return null;
            return "";
        }
    }
}
