using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OculusTool
{
    public partial class Form1 : Form
    {

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern int DwmEnableComposition(bool fEnable);
        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        public Form1()
        {
            InitializeComponent();
            timer2.Start();
            
            notifyIcon1.Visible = checkBox1.Checked;
            this.Text = "Oculus Runtime Utility by Bilago v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (Program.wd)
            {
                if (!checkBox1.Checked)
                    checkBox1.Checked = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
        }

        public string installPath;

        /// <summary>
        /// This will locate the installation path of the Oculus Configuration tool and the services
        /// </summary>
        /// <returns></returns>
        private string getPath()
        {
            try
            {
                if (Program.is64BitOperatingSystem)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\OculusRuntime\\InstallPath", false))
                    {
                        if (key != null)
                        {
                            if (key.GetValue("OVRInstallDir", null) != null)
                            {
                                return key.GetValue("OVRInstallDir").ToString();
                            }
                        }
                    }
                }
                else
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\OculusRuntime\\InstallPath", false))
                    {
                        if (key != null)
                        {
                            if (key.GetValue("OVRInstallDir", null) != null)
                            {
                                return key.GetValue("OVRInstallDir").ToString();
                            }
                        }
                    }
                }

                //If there are no registry entries, let's try to 'bruteforce' find it in the Programs Folder Directory
                string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
                if (Directory.Exists(Path.Combine(systemDrive, "Program Files (x86)\\Oculus\\Service")))
                    return Path.Combine(systemDrive, "Program Files (x86)\\Oculus");
                if (Directory.Exists(Path.Combine(systemDrive, "Program Files\\Oculus\\Service")))
                    return Path.Combine(systemDrive, "Program Files\\Oculus");

                //Since we still cannot locate the install directory, let the user manually select it
                MessageBox.Show("Unable to detect the Oculus Runtime install folder on your system.\nPlease Press OK and select the install folder.\nIf you have no installed the runtime yet, please visit the Oculus Developer website.", "Error");
                if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (File.Exists(Path.Combine(folderBrowserDialog1.SelectedPath,"Service\\OVRService_x86.exe")))
                        return Path.Combine(folderBrowserDialog1.SelectedPath,"Service");
                    if (File.Exists(Path.Combine(folderBrowserDialog1.SelectedPath,"OVRService_x86.exe")))
                        return folderBrowserDialog1.SelectedPath;

                    //User Selected an incorrect folder, aborting 
                    MessageBox.Show("Selected Directory did not contain the Oculus Runtime.\nPlease Relaunch this utility and try again.", "Error");
                    Application.Exit();                    
                }
                return "Not Installed";
            }
            catch
            {
                //blindly catching all exceptions as a Not Installed return
                return "Not Installed";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {            
            installPath = getPath();
            label1.Text = "Service Status: " + checkService();
            label2.Text = "Path: " + installPath;
            if (DwmIsCompositionEnabled())           
                button3.Text = "Disable Aero";
            else
                button3.Text = "Enable Aero";
            //quick way to auto start the watchdog if it was enabled
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomOculusWatchdog.dat")))
                checkBox1.Checked = true;
            
                
        }

        /// <summary>
        /// This will check if either OVRService is installed (64bit or 32bit)
        /// Might make more sense to return a bool, but for now it will return if its running or stopped.
        /// </summary>
        /// <returns></returns>
        public string checkService()
        {
            foreach (Process p in Process.GetProcesses())
            {                
                if (p.ProcessName.ToLower().Contains("ovrservice_"))
                {
                    button1.Text = "Stop Service";
                    return "Running";
                }
            }
            
            button1.Text = "Start Service";
            return "Stopped";
        }

        /// <summary>
        /// This is like the method above, but checks for the configuration utility. This is the custom watchdog that I created.
        /// Simple but effective.
        /// </summary>
        public void WDcheckService()
        {
            bool service = false;
            bool configUtil = false;
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("OVRService_"))
                    service = true;
                else if (p.ProcessName.Contains("OculusConfigUtil"))
                    configUtil = true;
            }
            if (!service || !configUtil)
            {
                startService();
                notifyIcon1.ShowBalloonTip(5, "Oculus Watchdog: Service was stopped", "The Service or config utility was not running. Both have been restarted.", ToolTipIcon.Warning);
            }            
        }

        /// <summary>
        /// This will toggle On/off windows Aero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (DwmIsCompositionEnabled())
            {
                button3.Text = "Enable Aero";
                DwmEnableComposition(false);
            }
            else
            {
                button3.Text = "Disable Aero";
                DwmEnableComposition(true);
            }
        }

        /// <summary>
        /// This will toggle the services and display a warning if the watchdog is enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (label1.Text.ToLower().Contains("stopped"))
            {
                startService();
            }
            else
            {
                if (checkBox1.Checked)
                    MessageBox.Show("You have the custom watchdog enabled, which will automatically restart the service even though you just pressed stop.\nIf you want to turn the service off for good (until you turn it back on or reboot), uncheck \"Enable Custom Watchdog\" and press Stop again.", "[Warning] WatchDog Enabled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                stopService();
            }
        }

        /// <summary>
        /// This will stop the service, wscript, and the configuration utility
        /// </summary>
        private void stopService()
        {
            bool kill = false;
            foreach (Process p in Process.GetProcesses())
            {

                if (p.ProcessName.ToLower().Contains("ovrservice_"))
                    kill = true;
                else if (p.ProcessName.ToLower().Contains("wscript"))
                    kill = true;
                else if (p.ProcessName.ToLower().Contains("oculusconfigutil"))
                    kill = true;
                else
                    kill = false;

                if (kill)
                {
                    p.Kill();                   
                }
                
            }
            //Some times it takes the process some time to fully quit, so lets sleep a little
            System.Threading.Thread.Sleep(800);
            label1.Text = "Service Status: " + checkService();
        }

        /// <summary>
        /// Stops, then Starts the service
        /// </summary>
        private void startService()
        {
            stopService();
            string exeName;
            if (Program.is64BitOperatingSystem)
            {
                exeName = "OVRService_x64.exe";
            }
            else
                exeName = "OVRService_x86.exe";
            string workPath = Path.Combine(installPath, "Service\\");
            string fullRunPath = Path.Combine(installPath, "Service\\" + exeName);            
            
            if (checkBox2.Checked)
            {                                
                string sdePath = Path.Combine(workPath, "sde.exe");
                //Transferring the oculus driver to current directory. SDE.exe doesn't seem to work on files outside 
                File.Copy(fullRunPath, Environment.CurrentDirectory + "\\" + exeName, true);
                //Extracting sde.7z to working directory
                startHidden("CMD.EXE", "/c 7z.exe x -y sde.7z *",true);                
                //Running the Emulation in the working directory
                startHidden("CMD.EXE", "/c sde.exe -- " + exeName,false);
            }
            else if (checkBox1.Checked)
            {
                //Starting the service directly since my watchdog is enabled
                Process scriptProc = new Process();
                scriptProc.StartInfo.FileName = fullRunPath;
                scriptProc.StartInfo.WorkingDirectory = Path.Combine(installPath, "Service");                
                scriptProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                scriptProc.StartInfo.CreateNoWindow = true;
                scriptProc.Start();
            }
            else
            {               
                //starting the service with wscript since my watchdog is disabled
                Process scriptProc = new Process();
                scriptProc.StartInfo.FileName = Path.Combine(installPath, "Service\\LaunchAndRestart.vbs");
                scriptProc.StartInfo.WorkingDirectory = Path.Combine(installPath, "Service");               
                scriptProc.StartInfo.Arguments = "\"" + Path.Combine(installPath, "Service\\" + exeName) + "\"";
                scriptProc.Start();
            }
            //Sleep time - Config utility doesn't detect the service immediately, needs a little bit of time
            System.Threading.Thread.Sleep(300);

            //starting the configuration utility to open in tray only
            Process configUtil = new Process();
            configUtil.StartInfo.FileName =Path.Combine(installPath, "Tools\\OculusConfigUtil.exe");            
            configUtil.StartInfo.Arguments ="--tray_only";
            configUtil.StartInfo.WorkingDirectory = Path.Combine(installPath, "Tools");       
            configUtil.Start();

            //More sleep before checking service state
            System.Threading.Thread.Sleep(600);
            label1.Text = "Service Status: " + checkService();
        }

        /// <summary>
        /// Starts the service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            startService();
        }

        /// <summary>
        /// Detects when the checkbox is checked/unchecked. This will start/stop the custom watchdog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            notifyIcon1.Visible = checkBox1.Checked;
            
            if (checkBox1.Checked)
            {
                timer1.Start();
                timer2.Stop();
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomOculusWatchdog.dat"), "1");
                string sched = "";

                //getResource will extract the scheduled task xml files I created for importing into windows
                
                    notifyIcon1.ShowBalloonTip(10, "Custom Oculus Watchdog Enabled", "This will ensure that the OVR Service is running, and will restart it when it fails", ToolTipIcon.Info);                    
                    //This will run schtasks without displaying a command prompt
                    startHidden("schtasks.exe", "/change /tn \"Oculus Service Scheduler\" /Disable",true);

                    wdSchedTask.createTaskxml();
                    sched = "/c schtasks /Create /tn \"Custom Oculus Service Scheduler\" /XML schedTask.xml /F 2>debug.txt";
                    startHidden("CMD", sched, true);
                    File.Delete("schedTask.xml");
                              
          
                    //Process schedTask = new Process();
                    //schedTask.StartInfo.FileName = "cmd.exe";
                    //schedTask.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                    //schedTask.StartInfo.CreateNoWindow = true;
                    //schedTask.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; 
                    //if (Program.is64BitOperatingSystem)
                    //{
                    //    sched="/c schtasks /Create /tn \"Custom Oculus Service Scheduler\" /XML CustomWatchdogx64.xml /F";
                    //}
                    //else
                    //    sched = "/c schtasks /Create /tn \"Custom Oculus Service Scheduler\" /XML CustomWatchdogx32.xml /F";
                    //getResource.get("OculusTool", "watchdogStart.xml");                    
                
                //schedTask.StartInfo.Arguments = sched;                    
                //schedTask.Start();
                    //startHidden("CMD", "/c Schtasks /change /tn \"Custom Oculus Service Scheduler\" /TR \"" + thisRunning + "\" 2>debug1.txt", true);
                    // /RU " + System.Security.Principal.WindowsIdentity.GetCurrent().Name + "
              
            }
            else
            {
                //stopping watchdog
                timer1.Stop();
                timer2.Start();
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomOculusWatchdog.dat"));
                startHidden("schtasks.exe", "/change /tn \"Oculus Service Scheduler\" /Enable",true);
                startHidden("schtasks.exe", "/change /tn \"Custom Oculus Service Scheduler\" /Disable",true);
                this.ShowInTaskbar = true;      
            }
            //cleanup
            File.Delete("CustomWatchdogx64.xml");
            File.Delete("CustomWatchdogx32.xml");
        }

        /// <summary>
        /// method that starts a hidden process
        /// </summary>
        /// <param name="process">Name of the program to execute</param>
        /// <param name="arguments">Arguments to supply to the program</param>
        private void startHidden(string process, string arguments,bool wait)
        {
            Process startProcess = new Process();
            startProcess.StartInfo.FileName = process;
            startProcess.StartInfo.Arguments = arguments;
            startProcess.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            startProcess.StartInfo.CreateNoWindow = true;
            startProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startProcess.Start();
            if (wait)
                startProcess.WaitForExit(3000);
        }

        /// <summary>
        /// Detect form resize. When watchdog is enabled it will minimize to system tray instead of taskbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                if (checkBox1.Checked)
                {
                    this.Hide();
                    notifyIcon1.ShowBalloonTip(10, "Custom Oculus Watchdog Enabled", "This will ensure that the OVR Service is running, and will restart it when it fails",ToolTipIcon.Info);
                }
                else
                    this.Show();
            }
        }

        /// <summary>
        /// Instructions to open the program when the system tray icon is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.BringToFront();
            this.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Instructions for the watchdog, every 30 seconds or so
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            WDcheckService();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            restartDrivers();
        }

        /// <summary>
        /// This will restart the DK2 drivers for the Camera and the HMD (4 drivers total)
        /// </summary>
        private void restartDrivers()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            bool custWD = false;
            if (checkBox1.Checked)
            {
                custWD = true;
                checkBox1.Checked = false;
            }
            string program;
            if (Program.is64BitOperatingSystem)
                program = "devcon64.exe";
            else
                program = "devcon32.exe";

            if (getResource.get("OculusTool", program))
            {
                stopService();
                System.Threading.Thread.Sleep(500);
                //Restarting HardwareID's. Firmware upgrades may break this (v3.x ++)
                startHidden("cmd.exe", "/c "+program+" restart *VID_2833*PID_0201*REV_0002*",true);
                startHidden("cmd.exe", "/c " + program + " restart *VID_2833*PID_0021*REV_02*",true);              
                System.Threading.Thread.Sleep(500);
                startService();
                MessageBox.Show("Drivers have been restarted successfully!","Success!",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                try
                {
                    File.Delete(program);
                }
                catch
                {
                    //just in case the file cannot delete... Sleep a second and try one more time.
                    System.Threading.Thread.Sleep(1000);
                    File.Delete(program);
                }
            }
            else
                MessageBox.Show("Unable to restart the driver!, Extraction Error!");
            if (custWD)
                checkBox1.Checked = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
        }

        /// <summary>
        /// This will check the status of the service regardless if the watchdog is enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick(object sender, EventArgs e)
        {            
            label1.Text = "Service Status: " + checkService();
        }

        /// <summary>
        /// Enables and disables the SSE-Emulation Fix
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bool passA;
            bool passB;
            if (!checkBox2.Checked)
            {
                stopService();
                //cleanup
                File.Delete("7z.exe");
                File.Delete("sde.exe");
                File.Delete("sde.7z");
                try
                {
                    Directory.Delete("ia32", true);
                    Directory.Delete("intel64", true);
                    Directory.Delete("misc", true);
                }
                catch
                {
                    //Empty catch in case Directory didn't exist - no need to catch it
                }
            }
            else
            {
                timer2.Stop();
                passA = getResource.get("OculusTool", "7z.exe");
                passB = getResource.get("OculusTool", "sde.7z");                
            }
         
            startService();
            timer2.Start();
        }               
    }
}
