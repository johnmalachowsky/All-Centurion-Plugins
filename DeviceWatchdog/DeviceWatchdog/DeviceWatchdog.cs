
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CHMPluginAPI;
using CHMPluginAPICommon;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml.Linq;
using System.Net.NetworkInformation;

namespace CHMModules
{


    public class DeviceWatchdog
    {

        private static _PluginCommonFunctions PluginCommonFunctions;
        internal static _PluginDatabaseAccess _PDBA;


         public void PluginInitialize(int UniqueID)
        {

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            ServerAccessFunctions.PluginDescription = "Device Watchdog Plugin";
            ServerAccessFunctions.PluginSerialNumber = "00001-00005";
            ServerAccessFunctions.PluginVersion = "1.0.0";
            PluginCommonFunctions = new _PluginCommonFunctions();
            ServerAccessFunctions._HeartbeatServerEvent += HeartbeatServerEventHandler;
            ServerAccessFunctions._TimeEventServerEvent += TimeEventServerEventHandler;
            ServerAccessFunctions._InformationCommingFromServerServerEvent += InformationCommingFromServerServerEventHandler;
            ServerAccessFunctions._InformationCommingFromPluginServerEvent += InformationCommingFromPluginEventHandler;
            ServerAccessFunctions._WatchdogProcess += WatchdogProcessEventHandler;
            ServerAccessFunctions._ShutDownPlugin += ShutDownPluginEventHandler;
            ServerAccessFunctions._StartupInfoFromServer += StartupInfoEventHandler;
            ServerAccessFunctions._PluginStartupCompleted += PluginStartupCompleted;
            //            ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
            ServerAccessFunctions._Command += CommandEvent;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;
            ServerAccessFunctions._HTMLProcess += HTMLProcess;
            _PDBA = new _PluginDatabaseAccess(Path.GetFileNameWithoutExtension((System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)));


        }

        private static void CommandEvent(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

          private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = false;

            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;


        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();


            //Ping p1 = new Ping();
            //PingReply PR = p1.Send("172.30.23.126");
            //// check when the ping is not success
            //while (!PR.Status.ToString().Equals("Success"))
            //{
            //    Console.WriteLine(PR.Status.ToString());
            //    PR = p1.Send("172.30.23.126");
            //}
            //// check after the ping is n success
            //while (PR.Status.ToString().Equals("Success"))
            //{
            //    Console.WriteLine(PR.Status.ToString());
            //    PR = p1.Send("172.30.23.126");
            //}
        }

        private static void FlagCommingServerEventHandler(ServerEvents WhichEvent)
        {
        }


        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }


        private static void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent)
        {
            PluginEventArgs Value;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();

            try
            {
                while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
                {
                    try
                    {
                    }
                    catch (Exception CHMAPIEx)
                    {
                        _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    }
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Release();


        }

        private static void InformationCommingFromServerServerEventHandler(ServerEvents WhichEvent)
        {
            PluginEventArgs Value;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            ServerAccessFunctions.InformationCommingFromServerSlim.Wait();
            try
            {

                while (ServerAccessFunctions.InformationCommingFromServerQueue.TryDequeue(out Value))
                {
                    try
                    {


                    }
                    catch (Exception CHMAPIEx)
                    {
                        _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    }
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            ServerAccessFunctions.InformationCommingFromServerSlim.Release();

        }

        private static void ShutDownPluginEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        private static void WatchdogProcessEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void StartupInfoEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        private static void HTMLProcess(PluginEvents WhichEvent, PluginEventArgs Value)
        {

        }
       
        #region Plugin Routines 

 
        #endregion
    }





}

