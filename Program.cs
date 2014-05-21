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
        /// Hide password.
        /// </summary>
        /// <returns></returns>
        static string GetPassword()
        {
            ConsoleKeyInfo key;
            string str = "";
            do
            {
                key = System.Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    str += key.KeyChar;
                    System.Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && str.Length > 0)
                    {
                        str = str.Substring(0, (str.Length - 1));
                        System.Console.Write("\b \b");
                    }
                }
            }
            // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);
            System.Console.WriteLine();
            return str;
        }

        /// <summary>
        /// Start GuruxAMI Data collector as console.
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
            bool trace = false;
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
                        //Trace messages
                        if (tag == "t")
                        {
                            trace = true;
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
                Console.WriteLine("Connecting " + host);
                GXAmiUser user = null;
                string r, dcName = null;
                GuruxAMI.Client.GXAmiClient cl = null;
                if (guid == Guid.Empty)
                {
                    Console.WriteLine("Registering Data Collector to GuruxAMI Service: ");                    
                    int pos = 0;
                    do
                    {
                        Console.WriteLine("Enter user name:");
                        string username = Console.ReadLine();
                        if (username == "")
                        {
                            return;
                        }
                        Console.WriteLine("Enter password:");
                        string password = GetPassword();
                        if (password == "")
                        {
                            return;
                        }
                        cl = new GXAmiClient(host, username, password);
                        //Get info from registered user.
                        try
                        {
                            user = cl.GetUserInfo();
                            break;
                        }
                        catch(UnauthorizedAccessException)
                        {
                            continue;
                        }                        
                    }while(++pos != 3);
                    //If authorisation failed.
                    if (user == null)
                    {
                        return;
                    }
                    Console.WriteLine("Finding data collectors.");                    
                    GXAmiDataCollector[] dcs = cl.GetDataCollectors();
                    //If there are existing DCs...
                    if (dcs.Length != 0)
                    {
                        Console.WriteLine("Do you want to register new data collector or bind old? (n/b)");
                        do
                        {
                            r = Console.ReadLine().Trim().ToLower();
                            if (r == "n" || r == "b")
                            {
                                break;
                            }
                        }
                        while (r == "");
                    }
                    else
                    {
                        r = "n";
                    }
                    //Old DC replaced.
                    if (r == "b")
                    {
                        Console.WriteLine("Select data collector number that you want to bind:");
                        pos = 0;
                        foreach (GXAmiDataCollector it in dcs)
                        {
                            ++pos;
                            Console.WriteLine(pos.ToString() + ". " + it.Name);
                        }
                        do
                        {
                            r = Console.ReadLine().Trim();
                            int sel = 0;
                            if (int.TryParse(r, out sel) && sel > 0 && sel <= pos)
                            {
                                guid = dcs[sel - 1].Guid;
                                break;
                            }
                        }
                        while (true);
                    }
                    else
                    {
                        do
                        {
                            Console.WriteLine("Enter name of the data collector:");
                            dcName = Console.ReadLine().Trim();
                            if (dcName == "")
                            {
                                return;
                            }
                            if (cl.Search(new string[] { dcName }, ActionTargets.DataCollector, SearchType.Name).Length == 0)
                            {
                                GXAmiDataCollector tmp = new GXAmiDataCollector(dcName, "", "");
                                cl.AddDataCollector(tmp, cl.GetUserGroups(false));
                                guid = tmp.Guid;
                                break;
                            }
                            Console.WriteLine("Name exists. Give new one.");
                        }
                        while (true);
                    }
                }
                collector = new GXAmiDataCollectorServer(host, guid);
                if (trace)
                {
                    collector.OnTasksAdded += new TasksAddedEventHandler(OnTasksAdded);
                    collector.OnTasksClaimed += new TasksClaimedEventHandler(OnTasksClaimed);
                    collector.OnTasksRemoved += new TasksRemovedEventHandler(OnTasksRemoved);
                    collector.OnError += new ErrorEventHandler(OnError);                    
                }
                collector.OnAvailableSerialPorts += new AvailableSerialPortsEventHandler(OnAvailableSerialPorts);
                GXAmiDataCollector dc = collector.Init(dcName);
                //If new Data collector is added bind it to the user groups.
                if (guid == Guid.Empty && cl != null)
                {
                    cl.AddDataCollector(dc, cl.GetUserGroups(false));
                }
                if (dc != null)
                {
                    GuruxAMI.DataCollector.Properties.Settings.Default.AmiDCGuid = dc.Guid;
                }                
                Console.WriteLine(string.Format("Data Collector '{0}' started.", dc.Name));
                GuruxAMI.DataCollector.Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {                
                if (ex is UnauthorizedAccessException)
                {
                    Console.WriteLine("Unknown data collector.");
                    GuruxAMI.DataCollector.Properties.Settings.Default.AmiDCGuid = Guid.Empty;
                    GuruxAMI.DataCollector.Properties.Settings.Default.Save();
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }   
            }
            //Wait until user press enter.
            ConsoleKeyInfo key;
            while ((key = System.Console.ReadKey()).Key != ConsoleKey.Enter)
            {
                System.Console.Write("\b \b");
            }
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
                if (it.TaskType == TaskType.MediaWrite)
                {
                    string[] tmp = it.Data.Split(Environment.NewLine.ToCharArray());
                    Console.WriteLine(string.Format("{0} Task {1} Added. {2}", it.TaskType, it.Id, tmp[tmp.Length - 1]));
                }
                else
                {
                    Console.WriteLine(string.Format("{0} Task {1} Added.", it.TaskType, it.Id));
                }
            }
        }
    }
}
