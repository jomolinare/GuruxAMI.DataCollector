//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuruxAMI.Client;
using GuruxAMI.Common;
using Gurux.Common;

namespace GuruxAMI.DataCollector
{
    class Program
    {
        static void ShowHelp()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //Update previous installed settings.
            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }
            GXAmiDataCollectorServer collector = null;
            try
            {
                for (int pos = 0; pos != args.Length; ++pos)
                {
                    string tag = args[pos];
                    if (tag[0] == '/' || tag[0] == '-')
                    {
                        tag = tag.Substring(1).ToLower();
                        if (tag == "h")
                        {
                            GuruxAMI.DataCollector.Properties.Settings.Default.AmiHostName = args[++pos];
                        }
                        if (tag == "p")
                        {
                            GuruxAMI.DataCollector.Properties.Settings.Default.AmiHostPort = args[++pos];
                        }
                        //Register Data Collector again.
                        if (tag == "r")
                        {
                            GuruxAMI.DataCollector.Properties.Settings.Default.AmiDCGuid = Guid.Empty;
                        }
                    }
                }
                string host = GuruxAMI.DataCollector.Properties.Settings.Default.AmiHostName;                
                if (string.IsNullOrEmpty(host))
                {
                    ShowHelp();
                    return;
                }
                Guid guid = GuruxAMI.DataCollector.Properties.Settings.Default.AmiDCGuid;
                Console.WriteLine("Starting Data Collector...");
                if (!host.StartsWith("http://"))
                {
                    host = "http://" + host + ":" + GuruxAMI.DataCollector.Properties.Settings.Default.AmiHostPort + "/";
                }
                collector = new GXAmiDataCollectorServer(host, guid);
                collector.OnTasksAdded += new TasksAddedEventHandler(OnTasksAdded);
                collector.OnTasksClaimed += new TasksClaimedEventHandler(OnTasksClaimed);
                collector.OnTasksRemoved += new TasksRemovedEventHandler(OnTasksRemoved);
                collector.OnError += new ErrorEventHandler(OnError);
                collector.OnAvailableSerialPorts += new AvailableSerialPortsEventHandler(OnAvailableSerialPorts);                
                if (guid == Guid.Empty)
                {
                    Console.WriteLine("Registering Data Collector to GuruxAMI Service with MAC address: " + BitConverter.ToString(GXAmiClient.GetMACAddress()).Replace('-', ':'));
                }
                GXAmiDataCollector dc = collector.Init("Unassigned Data collector");
                if (dc != null)
                {
                    GuruxAMI.DataCollector.Properties.Settings.Default.AmiDCGuid = dc.Guid;
                }                
                Console.WriteLine("Data Collector Started.");
            }
            catch (Exception ex)
            {
                GuruxAMI.DataCollector.Properties.Settings.Default.AmiDCGuid = Guid.Empty;
                Console.WriteLine(ex.Message);
            }            
            while (Console.ReadKey().Key != ConsoleKey.Enter);
            GuruxAMI.DataCollector.Properties.Settings.Default.Save();
            if (collector != null)
            {
                collector.Dispose();
            }            
        }

        /// <summary>
        /// Get info from available serial ports.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnAvailableSerialPorts(object sender, GXSerialPortInfo e)
        {
            e.SerialPorts = Gurux.Serial.GXSerial.GetPortNames();
        }

        /// <summary>
        /// Error has occurred.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ex"></param>
        static void OnError(object sender, Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        /// <summary>
        /// Task is removed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tasks"></param>
        static void OnTasksRemoved(object sender, GXAmiTask[] tasks)
        {
            foreach(GXAmiTask it in tasks)
            {
                Console.WriteLine(string.Format("Task {0} removed.", it.Id));
            }
        }

        /// <summary>
        /// Task is claimed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tasks"></param>
        static void OnTasksClaimed(object sender, GXAmiTask[] tasks)
        {
            foreach (GXAmiTask it in tasks)
            {
                Console.WriteLine(string.Format("{0} Task {1} claimed.", it.TaskType, it.Id));
            }
        }

        /// <summary>
        /// New task is added.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tasks"></param>
        static void OnTasksAdded(object sender, GXAmiTask[] tasks)
        {
            foreach (GXAmiTask it in tasks)
            {
                Console.WriteLine(string.Format("{0} Task {1} Added.", it.TaskType, it.Id));
            }
        }
    }
}
