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
using System.Management;
using System.ServiceProcess;

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
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SSEFIX.dat")))
                checkBox2.Checked=true;
            
            
                
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
                try
                {
                    if (!File.Exists(Environment.CurrentDirectory + "\\" + exeName))
                        File.Copy(fullRunPath, Environment.CurrentDirectory + "\\" + exeName, true);

                    //Extracting sde.7z to working directory
                    startHidden("sde_7z.exe", "-o \"" + Environment.CurrentDirectory + "\" -y 2>extractDebugErrors.txt", true);
                    //Running the Emulation in the working directory
                    startHidden("CMD.EXE", "/c sde.exe -- " + exeName + " 1>SSEFIX_debugStandard.txt 2>SSEFIX_debugError.txt", false);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Fatal Error while Enabling the SSE-Fix. Full error written to SSE-Crash.log", "Fatal Error for SSE-Emulation");
                    File.WriteAllText("SSE-Crash.log", ex.ToString());
                }
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
                if (!startProcess.WaitForExit(3000))
                    if (!startProcess.WaitForExit(5000))
                        if (!startProcess.WaitForExit(5000))
                            startProcess.Kill();
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
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            
            bool passA;            
            if (!checkBox2.Checked)
            {
                stopService();
                //cleanup                
                File.Delete("sde.exe");
                File.Delete("sde_7z.exe");
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SSEFIX.dat"));
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
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SSEFIX.dat"), "1");
                timer2.Stop();
                passA = getResource.get("OculusTool", "sde_7z.exe");                             
            }
         
            startService();
            timer2.Start(); 
            
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
        }

        /// <summary>
        /// Various system debug information to help troubleshoot problems
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Enabled = false;
            List<string> report = new List<string>();
            report.Add("Oculus Troubleshooting Report: ");
            report.Add("");
            report.Add("Operating System:");
            string output = Environment.OSVersion.VersionString;
            string program;
            if (Program.is64BitOperatingSystem)
            {
                program = "devcon64.exe";
                output = output + " 64bit";
            }
            else
            {
                program = "devcon32.exe";
                output = output + " 64bit";
            }
            report.Add(output);
            report.Add("");
            report.Add("CPU Name:");
            ManagementObjectSearcher mos =  new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
            foreach (ManagementObject mo in mos.Get())
            {
                report.Add(mo["Name"].ToString());
            }
            report.Add("");
            report.Add("Graphics Card name: ");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DisplayConfiguration");

            string graphicsCard = string.Empty;
            foreach (ManagementObject mo in searcher.Get())
            {
                foreach (PropertyData property in mo.Properties)
                {
                    if (property.Name == "Description")
                    {
                        graphicsCard = property.Value.ToString();
                    }
                }
            }
            if (!string.IsNullOrEmpty(graphicsCard))
                report.Add(graphicsCard);
            else
                report.Add("Graphics card not detected");
            report.Add("");
            report.Add("Screen information:");
            foreach (var screen in Screen.AllScreens)
            {
                // For each screen, add the screen properties to a list box.
                report.Add("Device Name: " + screen.DeviceName);
                report.Add("Bounds: " + screen.Bounds.ToString());
                report.Add("Type: " + screen.GetType().ToString());
                report.Add("Working Area: " + screen.WorkingArea.ToString());
                report.Add("Primary Screen: " + screen.Primary.ToString());
                report.Add("");
            }
            report.Add("");
            report.Add("Oculus Install Path: " + installPath);
            report.Add("");
            if (getResource.get("OculusTool", program))
            {           
                report.Add("Oculus Service Status:");

                bool service = false;
                bool configutil = false;
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower().Contains("ovrservice_"))
                    {
                        service = true;  
                    }
                    if (p.ProcessName.ToLower().Contains("oculusconfigutil"))
                    {
                        configutil = true;
                    }
                }
                if (service)
                    report.Add("OVR Service: Running");
                else
                    report.Add("OVR Service: Not Running");
                if (configutil)
                   report.Add("Config Utility: Running");
                else
                    report.Add("Config Utility: Not Running");
                report.Add("");
                report.Add("Oculus Connected Devices:");

                startHidden("cmd.exe", "/c " + program + " find *VID_2833*PID_0201*REV_0002* >devConOutput.txt", true);
                startHidden("cmd.exe", "/c " + program + " find *VID_2833*PID_0021*REV_02* >>devConOutput.txt", true);
                foreach (string line in File.ReadAllLines("devConOutput.txt")) report.Add(line);
                File.Delete("devConOutput.txt");
                report.Add("");
                report.Add("===================================================================");
                report.Add("Services:");
                foreach (ServiceController sc in ServiceController.GetServices())
                {
                    report.Add(sc.DisplayName + ": " + sc.Status.ToString());
                }
                report.Add("");
                report.Add("Listing All Installed Drivers: ");
                startHidden("cmd.exe", "/c " + program + " driverfiles * >devConOutput.txt", true);
                foreach (string line in File.ReadAllLines("devConOutput.txt")) report.Add(line);
                report.Add("");
               
                try
                {
                    Clipboard.SetText(string.Join(Environment.NewLine,report.ToArray()));
                }
                catch
                {
                    try
                    {
                    System.Threading.Thread.Sleep(5000);
                    Clipboard.SetText(string.Join(Environment.NewLine,report.ToArray()));
                    }
                    catch
                    {
                        MessageBox.Show("Unable to copy troubleshooting data to clipboard. Info has been written to \"Troubleshooting_DeviceInfo.txt\" instead.","Cannot Write To Clipboard");
                        File.WriteAllLines("Troubleshooting_DeviceInfo.txt",report.ToArray());
                        this.Enabled = true;
                        return;
                    }
                }
                MessageBox.Show("Debug info has been copied to clipboard! Paste information into a post or PM for support.");
                this.Enabled = true;
                
              
            }
        }               
    }
}

