using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OculusTool
{
    static class Program
    {
        public static string workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        static bool is64BitProcess = (IntPtr.Size == 8);
        public static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        /// <summary>
        /// checking to see if the computer is 64bit or 32bit
        /// </summary>
        /// <returns></returns>
        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }
        public static bool wd = false;
        public static string exeName = Path.Combine(workingDirectory, Process.GetCurrentProcess().MainModule.FileName);
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(workingDirectory);
            if (args.Length != 0)
                wd = true;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }


        public static int monitor = 1;
        public static bool dx11Force = false;
    }
}
