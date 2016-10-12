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


//Required Parameters
//  UpdateInterval (In Milliseconds, default is 2500)

namespace CHMModules
{


    public class MenuCommands
    {

        private static _PluginCommonFunctions PluginCommonFunctions;

        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Menu Processing";
            ServerAccessFunctions.PluginSerialNumber = "00001-00200";
            ServerAccessFunctions.PluginVersion = "1.0.0";
            PluginCommonFunctions = new _PluginCommonFunctions();
            ServerAccessFunctions._FlagCommingServerEvent += FlagCommingServerEventHandler;
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

        }

        private static void CommandEvent(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
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

 
        }

        private static void InformationCommingFromServerServerEventHandler(ServerEvents WhichEvent)
        {

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



    }
}
