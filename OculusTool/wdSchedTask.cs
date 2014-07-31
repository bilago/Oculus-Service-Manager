﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OculusTool
{
    class wdSchedTask
    {
        
        public static void createTaskxml()
        {
            string[] xmlContents = { "<?xml version=\"1.0\" encoding=\"UTF-16\"?>", "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">", "<RegistrationInfo>", "<Date>2014-07-31T06:58:01</Date>", "</RegistrationInfo>", "<Triggers>", "<LogonTrigger>", "<StartBoundary>2014-07-31T06:58:00</StartBoundary>", "<Enabled>true</Enabled>", "</LogonTrigger>", "</Triggers>", "<Principals>", "<Principal id=\"Author\">", "<LogonType>InteractiveToken</LogonType>", "<RunLevel>HighestAvailable</RunLevel>", "</Principal>", "</Principals>", "<Settings>", "<MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>", "<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>", "<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>", "<AllowHardTerminate>true</AllowHardTerminate>", "<StartWhenAvailable>false</StartWhenAvailable>", "<RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>", "<IdleSettings>", "<StopOnIdleEnd>true</StopOnIdleEnd>", "<RestartOnIdle>false</RestartOnIdle>", "</IdleSettings>", "<AllowStartOnDemand>true</AllowStartOnDemand>", "<Enabled>true</Enabled>", "<Hidden>false</Hidden>", "<RunOnlyIfIdle>false</RunOnlyIfIdle>", "<WakeToRun>false</WakeToRun>", "<ExecutionTimeLimit>PT0S</ExecutionTimeLimit>", "<Priority>7</Priority>", "</Settings>", "<Actions Context=\"Author\">", "<Exec>", "<Command>\"" + Program.exeName + "\"</Command>","<Arguments>-toTray</Arguments>","</Exec>", "</Actions>", "</Task>" };
            System.IO.File.WriteAllLines("schedTask.xml", xmlContents);
        }
    }
}
