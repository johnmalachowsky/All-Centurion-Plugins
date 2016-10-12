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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CHMPluginAPI;
using CHMPluginAPICommon;
using System.Collections.Concurrent;

//Required Parameters

namespace CHMModules
{
    using Extensions;
    class WiFiThermostat3M50
    {
        private static _PluginCommonFunctions PluginCommonFunctions;
        private static _PluginDatabaseAccess PluginDatabaseAccess = new _PluginDatabaseAccess(Path.GetFileNameWithoutExtension((System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)));
        private static int HowManyThermoValues;
        private static System.Threading.Timer PluginTransactionsTimer;
        public static string CloudLinkPlugin;
        public static string CloudLinkPluginSecureCommunicationIDCode;
        public static string LocalLinkPlugin;
        public static string LocalLinkPluginSecureCommunicationIDCode;

        static internal int TimerTimeslice = 100;

        static internal ConcurrentQueue<PluginEventArgs> PluginEventArgsValue;
        internal static SemaphoreSlim LockingSemaphore;



        internal struct ThermostatInformationStruct
        {
            public string Interface;
            public bool HasProcessedOnce;
            public string ThermosatLocation;
            public string ThermoID;
            public int ProcessSequence;
            public string[] DevicesFlagNames;
            public decimal[] OldThermoValues;
            public decimal[] CurrentThermoValues;
            public string[] RawData;
            public short[] NewValues; ///Are there new values?????????????
            public string[] LogCodes;
            public bool[] DontCreateFlagCode; //0=never (always create flag), 1=WHen Thermo is Off
            public DeviceStruct[] ThermoDevices;
            public DeviceStruct[] ArchiveDevices;
            public string[] DeviceValueNames;
            public bool[] FlagCurrentlyExists;
            public string SecurityCookie;
            public string LastURISource;
            public int LocalScheduleIntervalSeconds;
            public DateTime HeatScheduleLastUpdate;
            public string[] HeatSchedule;
            public DateTime CoolScheduleLastUpdate;
            public string[] CoolSchedule;
            public string LastScheduleUpdate;
            public InterfaceStruct DBInterface;
            public DateTime LastUpdateDateTime;
            public int CurrentTargetTemp;
            public bool CurrentTargetChanged;
            public int NextTargetTemp;
            public DateTime NextTargetTime;
            public bool NextTargetChanged;
            public string HeatTarget;
            public string CoolTarget;
            public string NextHeatTarget;
            public DateTime NextHeatTargetTime;
            public string NextCoolTarget;
            public DateTime NextCoolTargetTime;
            public string Target;
            public bool TargetFlag;
            public string Mode;
            public string LastMode;
            public string LastDateSummaryArchived;
            public int ThermoOnMinutes;
            public int AverageThermoTemp;
            public int MinutesActiveInCurrentHour;
            public int LastMinuteNumberProcessed;
            public int MinutesSinceLastConnect;
        }

        private static ThermostatInformationStruct[] ThermostatInformation;
        private static Int16 HowManyThermostats = 0;
        private static string CloudPassword, CloudUsername;
        private static bool FirstCloud = true;



        public void PluginInitialize(int UniqueID)
        {
            try
            {
                ServerAccessFunctions.PluginDescription = "3M-50 Wifi Thermostat Console";
                ServerAccessFunctions.PluginSerialNumber = "00001-00013";
                ServerAccessFunctions.PluginVersion = "1.0.0";

                PluginEventArgsValue = new ConcurrentQueue<PluginEventArgs>();
                TimerCallback ProcessTimerCallBack = new TimerCallback(new WiFiThermostat3M50().ProcessIncommingStuff);
                PluginTransactionsTimer = new System.Threading.Timer(ProcessTimerCallBack, null, Timeout.Infinite, Timeout.Infinite);


                ServerAccessFunctions._FlagCommingServerEvent += FlagCommingServerEventHandler;
                ServerAccessFunctions._HeartbeatServerEvent += HeartbeatServerEventHandler;
                ServerAccessFunctions._TimeEventServerEvent += TimeEventServerEventHandler;
                ServerAccessFunctions._InformationCommingFromServerServerEvent += InformationCommingFromServerEventHandler;
                ServerAccessFunctions._InformationCommingFromPluginServerEvent += InformationCommingFromPluginEventHandler;
                ServerAccessFunctions._WatchdogProcess += WatchdogProcessEventHandler;
                ServerAccessFunctions._ShutDownPlugin += ShutDownPluginEventHandler;
                ServerAccessFunctions._StartupInfoFromServer += StartupInfoEventHandler;
                ServerAccessFunctions._PluginStartupCompleted += PluginStartupCompleted;
                ServerAccessFunctions._AddDBRecord += AddDBRecordEventHandler;
//                ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
                ServerAccessFunctions._Command += CommandEvent;
                ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;
        }

        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}




        private static void CommandEvent(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }
        
        private static void FlagCommingServerEventHandler(ServerEvents WhichEvent)
        {
            PluginEventArgs Value;

            ServerAccessFunctions.FlagCommingFromServerSlim.Wait();

            while (ServerAccessFunctions.FlagCommingFromServerQueue.TryDequeue(out Value))
            {
                try
                {

                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
            ServerAccessFunctions.FlagCommingFromServerSlim.Release();
        }

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = false;

            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;
        }
        
        private static void AddDBRecordEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        private static void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        private static void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent)
        {
            string R;
            PluginEventArgs Value;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();

            while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
            {

                try
                {
                    Int16 MacThermo = MacThermoIndex(Value.PluginData.DeviceUniqueID);

                    if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionComplete)
                    {
//                        string T = _PCF.ConvertByteArrayToString(Value.PluginData.OutgoingDS.CommDataControlInfo[0].CharactersToSend);
//                        string S = _PCF.ConvertByteArrayToString(Value.PluginData.OutgoingDS.CommDataControlInfo[0].ActualResponseReceived);
                        PluginEventArgsValue.Enqueue(Value);
                        PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                        continue;
                    }

                    if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionFailed)
                    {
                        continue;
                    }

                    if (Value.PluginData.Command == PluginCommandsToPlugins.RequestLink)
                    {
                        PluginCommunicationStruct PCS = new PluginCommunicationStruct();

                        PCS.Command = PluginCommandsToPlugins.LinkAccepted;
                        PCS.DestinationPlugin = Value.PluginData.OriginPlugin;
                        PCS.PluginReferenceIdentifier = Value.PluginData.PluginReferenceIdentifier;
                        PCS.ReferenceUniqueNumber = Value.PluginData.UniqueNumber;
                        PCS.SecureCommunicationIDCode = Value.PluginData.SecureCommunicationIDCode;

                        _PCF.QueuePluginInformationToPlugin(PCS);
                        continue;
                    }


                    if (Value.PluginData.Command == PluginCommandsToPlugins.LinkedCommReady)
                    {

                        if (ThermostatInformation[MacThermo].ThermosatLocation.ToLower() == "local")
                        {
                            LocalLinkPlugin = Value.PluginData.OriginPlugin;
                            LocalLinkPluginSecureCommunicationIDCode = Value.PluginData.SecureCommunicationIDCode;

                            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();

                            PCS2.DestinationPlugin = LocalLinkPlugin;
                            PCS2.SecureCommunicationIDCode = LocalLinkPluginSecureCommunicationIDCode;

                            PCS2.Command = PluginCommandsToPlugins.StartTimedLoopForData;
                            OutgoingDataStruct T = new OutgoingDataStruct();
                            T.LocalIDTag = "LocalThermoData";
                            T.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            T.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                            T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "LocalThermoData", "http://$$IPAddress/tstat/"));
//                            T.CommDataControlInfo[0].ReceiveDelayMiliseconds = 10000;
                            PCS2.OutgoingDS = T.Copy();
                            _PCF.QueuePluginInformationToPlugin(PCS2);
                            R= _PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "LocalScheduleIntervalSeconds", "300");
                            int.TryParse(R, out  ThermostatInformation[MacThermo].LocalScheduleIntervalSeconds);
                            if (ThermostatInformation[MacThermo].LocalScheduleIntervalSeconds == 0)
                                ThermostatInformation[MacThermo].LocalScheduleIntervalSeconds = 300;

                            PluginCommunicationStruct PCS3 = new PluginCommunicationStruct();
                            PCS3.DestinationPlugin = LocalLinkPlugin;
                            PCS3.SecureCommunicationIDCode = LocalLinkPluginSecureCommunicationIDCode;

                            PCS3.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
                            OutgoingDataStruct TH = new OutgoingDataStruct();
                            TH.LocalIDTag = "LocalHeatSch";
                            TH.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            TH.CommDataControlInfo[0].CharactersToSend =  _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "LocalHeatSch", "http://$$IPAddress/tstat/program/heat"));
                            TH.SecondsBetweenProcessCommunicationAtTime = ThermostatInformation[MacThermo].LocalScheduleIntervalSeconds;
                            TH.NumberOfTimesToProcessCommunicationAtTime = int.MaxValue;
                            TH.ProcessCommunicationAtTimeTime = DateTime.Now.AddSeconds(10); 
                            PCS3.OutgoingDS = TH.Copy();
                            _PCF.QueuePluginInformationToPlugin(PCS3);

                            PluginCommunicationStruct PCS4 = new PluginCommunicationStruct();
                            PCS4.DestinationPlugin = LocalLinkPlugin;
                            PCS4.SecureCommunicationIDCode = LocalLinkPluginSecureCommunicationIDCode;
                            PCS4.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
                            OutgoingDataStruct TX = new OutgoingDataStruct();
                            TX.LocalIDTag = "LocalCoolSch";
                            TX.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            TX.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "LocalCoolSch", "http://$$IPAddress/tstat/program/cool"));
                            TX.SecondsBetweenProcessCommunicationAtTime = ThermostatInformation[MacThermo].LocalScheduleIntervalSeconds;
                            TX.NumberOfTimesToProcessCommunicationAtTime = int.MaxValue;
                            TX.ProcessCommunicationAtTimeTime = DateTime.Now.AddSeconds(10 + ThermostatInformation[MacThermo].LocalScheduleIntervalSeconds/2);
                            PCS4.OutgoingDS = TX.Copy();
                            _PCF.QueuePluginInformationToPlugin(PCS4);

                           
                            PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                     
                            continue;
                        }

                        if (ThermostatInformation[MacThermo].ThermosatLocation.ToLower() == "cloud")
                        {
                            CloudLinkPlugin = Value.PluginData.OriginPlugin;
                            CloudLinkPluginSecureCommunicationIDCode = Value.PluginData.SecureCommunicationIDCode;

                            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();

                            PCS2.DestinationPlugin = CloudLinkPlugin;
                            PCS2.SecureCommunicationIDCode = CloudLinkPluginSecureCommunicationIDCode;
                            PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                            ThermostatInformation[MacThermo].ProcessSequence = 0;
                            ThermostatInformation[MacThermo].LastURISource = "";
                            OutgoingDataStruct T = new OutgoingDataStruct();
                            T.LocalIDTag = "CloudLogin";
                            T.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudLogin", "https://my.radiothermostat.com/filtrete/login.html"));
                            T.CommDataControlInfo[0].Method = "Get";
                            PCS2.OutgoingDS = T.Copy();
                            _PCF.QueuePluginInformationToPlugin(PCS2);
                            continue;
                        }
                    }

                    if (Value.PluginData.Command == PluginCommandsToPlugins.CancelLink)
                    {
                        //End Process Timer based on Interval

                        PluginCommunicationStruct PCS = new PluginCommunicationStruct();

                        PCS.Command = PluginCommandsToPlugins.ActionCompleted;
                        PCS.DestinationPlugin = Value.PluginData.OriginPlugin;
                        PCS.PluginReferenceIdentifier = Value.PluginData.PluginReferenceIdentifier;
                        PCS.ReferenceUniqueNumber = Value.PluginData.UniqueNumber;
                        _PCF.QueuePluginInformationToPlugin(PCS);
                        continue;
                    }

                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Release();
        }

        private static void InformationCommingFromServerEventHandler(ServerEvents WhichEvent)
        {

            PluginEventArgs Value; 
            ServerAccessFunctions.InformationCommingFromServerSlim.Wait( );

            while (ServerAccessFunctions.InformationCommingFromServerQueue.TryDequeue(out Value))
            {
                try
                {
                    if (Value.ServerData.Command == ServerPluginCommands.GetAccessCode)
                    {
                        CloudPassword = Value.ServerData.String3;
                        CloudUsername = Value.ServerData.String2;
                    }

                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
            ServerAccessFunctions.InformationCommingFromServerSlim.Release();
        }

        private static void ShutDownPluginEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        private static void WatchdogProcessEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        private static void StartupInfoEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

            try
            {

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }


        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {
                int index, slen, i;
                XMLDeviceScripts XML= new XMLDeviceScripts();
                int ArchiveDevices=0, NonArchiveDevices=0;
                PluginCommonFunctions = new _PluginCommonFunctions();

                for (index = 0; index < _PluginCommonFunctions.Devices.Length; index++)
                {
                    if (string.IsNullOrEmpty(_PluginCommonFunctions.Devices[index].DeviceIdentifier) && _PluginCommonFunctions.Devices[index].DeviceIdentifier.Length>=7 && _PluginCommonFunctions.Devices[index].DeviceIdentifier.Substring(0, 7).ToLower() == "archive")
                        ArchiveDevices++;
                    else
                        NonArchiveDevices++;
                }

                HowManyThermostats = (short)_PluginCommonFunctions.Interfaces.Length;
                ThermostatInformation = new ThermostatInformationStruct[HowManyThermostats];
                HowManyThermoValues = NonArchiveDevices + 6;
                for (i = 0; i < HowManyThermostats; i++)
                {
                    ThermostatInformation[i].ThermoDevices =  new DeviceStruct[NonArchiveDevices+1];
                    ThermostatInformation[i].ArchiveDevices = new DeviceStruct[ArchiveDevices + 1];
                    ThermostatInformation[i].DeviceValueNames = new string[HowManyThermoValues];
                    ThermostatInformation[i].OldThermoValues = new decimal[HowManyThermoValues];
                    ThermostatInformation[i].CurrentThermoValues = new decimal[HowManyThermoValues];
                    ThermostatInformation[i].DevicesFlagNames = new string[HowManyThermoValues];
                    ThermostatInformation[i].FlagCurrentlyExists = new bool[HowManyThermoValues];
                    ThermostatInformation[i].LogCodes = new string[HowManyThermoValues];
                    ThermostatInformation[i].DontCreateFlagCode = new bool[HowManyThermoValues];
                    ThermostatInformation[i].HeatSchedule = new string[7];
                    ThermostatInformation[i].HeatScheduleLastUpdate=_PluginCommonFunctions.CurrentTime.AddMinutes(1);
                    ThermostatInformation[i].CoolSchedule = new string[7];
                    ThermostatInformation[i].CoolScheduleLastUpdate = _PluginCommonFunctions.CurrentTime.AddMinutes(2);
                    ThermostatInformation[i].NewValues = new short[HowManyThermoValues];
                    ThermostatInformation[i].RawData = new string[HowManyThermoValues];
                    ThermostatInformation[i].HasProcessedOnce = false;
                    ThermostatInformation[i].Interface = _PluginCommonFunctions.Interfaces[i].InterfaceUniqueID;
                    ThermostatInformation[i].DBInterface = _PluginCommonFunctions.Interfaces[i];
                    ThermostatInformation[i].Mode = "off";
                    ThermostatInformation[i].LastMode = "";
                    ThermostatInformation[i].LastScheduleUpdate = "cool";
                    ThermostatInformation[i].ThermoOnMinutes=0;
                    ThermostatInformation[i].AverageThermoTemp=0;
                    ThermostatInformation[i].MinutesActiveInCurrentHour=0;
                    ThermostatInformation[i].LastMinuteNumberProcessed=0;
                    ThermostatInformation[i].ThermoID=  ThermostatInformation[i].DBInterface.HardwareIdentifier.ToLower().Replace(":","");


                    String[] Values, FieldNames;
                    PluginDatabaseAccess.FindDatedRecord("ThermostatSummaryArchive", ThermostatInformation[i].Interface, "", "", out Values, out FieldNames, _PluginDatabaseAccess.PluginDataLocationType.Newest);
                    if (Values == null || Values.Length == 0 || string.IsNullOrEmpty(Values[0]))
                    {
                        ThermostatInformation[i].LastDateSummaryArchived = PluginCommonFunctions.FindValueInStartupInfo(ThermostatInformation[i].DBInterface.StartupInformation, "ArchiveStartDate", "2014-01-01");

                    }
                    else
                    {
                        DateTime D1 = PluginCommonFunctions.LogFileToDateTime(Values[0]);
                        if (D1 == DateTime.MinValue)
                            ThermostatInformation[i].LastDateSummaryArchived = PluginCommonFunctions.FindValueInStartupInfo(ThermostatInformation[i].DBInterface.StartupInformation, "ArchiveStartDate", "2014-01-01");
                        else

                            ThermostatInformation[i].LastDateSummaryArchived = PluginCommonFunctions.SaveLogsDateFormat(D1.AddDays(1));
                    }
                    
                    if (ThermostatInformation[i].LastDateSummaryArchived.Length > 10)
                        ThermostatInformation[i].LastDateSummaryArchived.Substring(0, 10);

                }

                for (index = 0; index < HowManyThermostats; index++)
                {
                    for (i = 0; i < _PluginCommonFunctions.Devices.Length; i++)
                    {
                        if (index != MacThermoIndex(_PluginCommonFunctions.Devices[i].InterfaceUniqueID))
                            continue;
                        ThermostatInformation[index].ThermoDevices[i] = _PluginCommonFunctions.Devices[i];
                        slen = 0;

                      //  ThermostatInformation[index].ThermoDevices[i].FlagStatesList = XML.SetupStates(ThermostatInformation[index].ThermoDevices[i].CommandSet);
                        ThermostatInformation[index].DevicesFlagNames[i] = _PluginCommonFunctions.Devices[i].DeviceName;
                        ThermostatInformation[index].DeviceValueNames[i] = _PluginCommonFunctions.Devices[i].DeviceIdentifier.ToLower();
                        ThermostatInformation[index].LogCodes[i] = _PluginCommonFunctions.Devices[i].LogCode.ToLower();
                        ThermostatInformation[index].DontCreateFlagCode[i] = PluginCommonFunctions.DoesValueExistInStartupField("CodesToUseOnOff", _PluginCommonFunctions.Devices[i].DeviceIdentifier.ToLower());
                    }
                }

                for (i = 0; i < ThermostatInformation.Length; i++)
                {
                    ThermostatInformation[i].ThermosatLocation = ThermostatInformation[i].DBInterface.HardwareSettings;
                    if (ThermostatInformation[i].ThermosatLocation.ToLower() != "cloud")
                        ThermostatInformation[i].ThermosatLocation = "Local";
                }

                PluginServerDataStruct PSS = new PluginServerDataStruct();
                PSS.String = "RadioThermostatCloud";
                PSS.Command = ServerPluginCommands.GetAccessCode;
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.QueuePluginInformationToServer(PSS);
                LockingSemaphore = new SemaphoreSlim(1);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            
        }

        internal void ProcessIncommingStuff(object source)
        {

            try
            {
                PluginEventArgs Value;
                bool flag = false;
                int a, b,ixx2;
                string SX;
                int MacThermo;
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();


                CHMModules.WiFiThermostat3M50.LockingSemaphore.Wait();
                PluginTransactionsTimer.Change(Timeout.Infinite, Timeout.Infinite);

                if (!PluginEventArgsValue.TryDequeue(out Value))
                {
                    PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                    CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                    return;
                }
                OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS.Copy();
                if (ODS.LocalIDTag == "CloudGetArchive")
                {
                    MacThermo = MacThermoIndex(ODS.LocalInterface);

                    if (MacThermo >= 0)
                    {
                        DateTime CurrentDate = _PCF.LogFileToDateTime(ODS.LocalData);
                        try
                        {
                            string CloudArchiveInfo = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived);
                            JObject root = JObject.Parse(CloudArchiveInfo);
                            JObject series = JObject.Parse(root["series"].ToString());
                            JObject Results = JObject.Parse(series["results"].ToString());
                            JArray total = (JArray)Results["total"];
                            JArray cool = (JArray)Results["cool"];
                            JArray heat = (JArray)Results["heat"];
                            JArray thermostats = (JArray)Results["thermostats"];
                            JArray off = (JArray)Results["off"];
                            JArray missing = (JArray)Results["missing"];
                            JArray on = (JArray)Results["on"];

                            JArray dates = (JArray)series["dates"];
                            JArray dateOffsetMilliseconds = (JArray)series["dateOffsetMilliseconds"];

                            JObject Events = JObject.Parse(root["events"].ToString());
                            JObject ambient_change = JObject.Parse(Events["ambient_change"].ToString());
                            JArray ambient_change_values = (JArray)ambient_change["values"];
                            JArray ambient_change_dates = (JArray)ambient_change["dates"];
                            JArray ambient_change_dateOffsetMilliseconds = (JArray)ambient_change["dateOffsetMilliseconds"];
                            JObject setpoint_change = JObject.Parse(Events["setpoint_change"].ToString());
                            JArray setpoint_change_values = (JArray)setpoint_change["values"];
                            JArray setpoint_change_dates = (JArray)setpoint_change["dates"];
                            JArray setpoint_change_dateOffsetMilliseconds = (JArray)setpoint_change["dateOffsetMilliseconds"];


                            double[] AmbTemp = new double[ambient_change_values.Count];
                            DateTime[] AmbDate = new DateTime[ambient_change_values.Count];
                            for (int index = 0; index < ambient_change_values.Count; index++) //Temps
                            {
                                AmbDate[index] = new DateTime(1970, 1, 1).AddMilliseconds(_PCF.ConvertToInt64(ambient_change_dates[index].ToString()) + _PCF.ConvertToInt64(ambient_change_dateOffsetMilliseconds[index].ToString()));
                                AmbTemp[index] = _PCF.ConvertToDouble(ambient_change_values[index].ToString());
                            }

                            int[] SetTemp = new int[setpoint_change_values.Count];
                            DateTime[] SetDate = new DateTime[setpoint_change_values.Count];
                            for (int index = 0; index < setpoint_change_values.Count; index++) //Targets
                            {
                                SetDate[index] = new DateTime(1970, 1, 1).AddMilliseconds(_PCF.ConvertToInt64(setpoint_change_dates[index].ToString()) + _PCF.ConvertToInt64(setpoint_change_dateOffsetMilliseconds[index].ToString()));
                                SetTemp[index] = _PCF.ConvertToInt32(setpoint_change_values[index].ToString());
                            }

                            int[] _total = new int[total.Count];
                            int[] _cool = new int[cool.Count];
                            int[] _heat = new int[heat.Count];
                            int[] _thermostats = new int[thermostats.Count];
                            int[] _off = new int[off.Count];
                            int[] _missing = new int[missing.Count];
                            int[] _on = new int[on.Count];
                            for (int index = 0; index < cool.Count; index++) //Hourly Times
                            {
                                _total[index] = _PCF.ConvertToInt32(total[index].ToString());
                                _cool[index] = _PCF.ConvertToInt32(cool[index].ToString());
                                _heat[index] = _PCF.ConvertToInt32(heat[index].ToString());
                                _thermostats[index] = _PCF.ConvertToInt32(thermostats[index].ToString());
                                _off[index] = _PCF.ConvertToInt32(off[index].ToString());
                                _missing[index] = _PCF.ConvertToInt32(missing[index].ToString());
                                _on[index] = _PCF.ConvertToInt32(on[index].ToString());

                            }
                            int t = 0;
                            for (int i = 0; i < _thermostats.Length; i++)
                                t = t + _thermostats[i];
                            if (t == 0)//THermostat was not online at this time
                            {
                                _PCF.NamedSaveLogs("ThermostatSummaryArchive", "E", "", "**EMPTY**", "**EMPTY**", ThermostatInformation[MacThermo].DBInterface, CurrentDate);
                            }
                            else
                            {
                                foreach (DeviceStruct xx in ThermostatInformation[MacThermo].ThermoDevices)
                                {
                                    if (string.IsNullOrEmpty(xx.DeviceUniqueID) || string.IsNullOrEmpty(xx.DeviceIdentifier))
                                        continue;
                                    if (xx.DeviceIdentifier.ToLower() == "arc_temp")
                                    {
                                        for (int index = 0; index < AmbDate.Length; index++) //Temps
                                        {
                                            _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", AmbTemp[index].ToString(), AmbTemp[index].ToString(), xx, AmbDate[index]);
                                        }

                                    }
                                    if (xx.DeviceIdentifier.ToLower() == "arc_setPoint")
                                    {
                                        for (int index = 0; index < SetDate.Length; index++) //Temps
                                        {
                                            _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", SetTemp[index].ToString(), SetTemp[index].ToString(), xx, SetDate[index]);
                                        }

                                    }
                                    if (xx.DeviceIdentifier.ToLower() == "arc_mincool")
                                    {
                                        for (int index = 0; index < _cool.Length; index++) //Temps
                                        {
                                            if (_cool[index] > 0)
                                                _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", _cool[index].ToString(), cool[index].ToString(), xx, CurrentDate.AddHours(index));
                                        }

                                    }
                                    if (xx.DeviceIdentifier.ToLower() == "arc_minheat")
                                    {
                                        for (int index = 0; index < _heat.Length; index++) //Temps
                                        {
                                            if (_heat[index] > 0)
                                                _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", _heat[index].ToString(), heat[index].ToString(), xx, CurrentDate.AddHours(index));
                                        }

                                    }
                                    if (xx.DeviceIdentifier.ToLower() == "arc_minoff")
                                    {
                                        for (int index = 0; index < _off.Length; index++) //Temps
                                        {
                                            _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", _off[index].ToString(), off[index].ToString(), xx, CurrentDate.AddHours(index));
                                        }

                                    }
                                    if (xx.DeviceIdentifier.ToLower() == "arc_minnoconnect")
                                    {
                                        for (int index = 0; index < _missing.Length; index++) //Temps
                                        {
                                            if (_missing[index] > 0)
                                                _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", _missing[index].ToString(), missing[index].ToString(), xx, CurrentDate.AddHours(index));
                                        }

                                    }
                                    if (xx.DeviceIdentifier.ToLower() == "arc_minon")
                                    {
                                        for (int index = 0; index < _on.Length; index++) //Temps
                                        {
                                            _PCF.NamedSaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", _on[index].ToString(), on[index].ToString(), xx, CurrentDate.AddHours(index));
                                        }

                                    }
                                    //    if (xx.DeviceIdentifier.ToLower() == "arc_mintotal")
                                    //    {
                                    //        for (int index = 0; index < _total.Length; index++) //Temps
                                    //        {
                                    //            _PCF.SaveLogs("ThermostatSummaryArchive", xx.DeviceName, "", _total[index].ToString(), total[index].ToString(), xx, CurrentDate.AddHours(index));
                                    //        }

                                    //    }
                                }
                            }
                            ThermostatInformation[MacThermo].LastDateSummaryArchived = _PCF.SaveLogsDateFormat(CurrentDate.AddDays(1));
                        }
                        catch (Exception ex)
                        {
                            _PCF.NamedSaveLogs("ThermostatSummaryArchive", "X", "", "**ERR**", "**ERR**", ThermostatInformation[MacThermo].DBInterface, CurrentDate);
                            ThermostatInformation[MacThermo].LastDateSummaryArchived = _PCF.SaveLogsDateFormat(CurrentDate.AddDays(1));
                        }
                    } 
                    PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                    CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                    return;
                }
                
                MacThermo = MacThermoIndex(Value.PluginData.DeviceUniqueID);

                if (MacThermo >= 0)
                {
                    
                    if (ThermostatInformation[MacThermo].ThermosatLocation.ToLower() == "cloud")
                    {
                        string SQX = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].CharactersToSend);
                        string CloudThermoInfo = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived);
                        switch (ThermostatInformation[MacThermo].ProcessSequence)
                        {
                            case 0:
                                if (ODS.CommDataControlInfo[0].CookiesReturned != null)
                                {
                                    foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                    {
                                        if (C.Name == "JSESSIONID")
                                        {
                                            flag = true;
                                            ThermostatInformation[MacThermo].SecurityCookie = C.Value;
                                            break;
                                        }
                                    }
                                    if (flag)
                                    {
                                        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                        PCS2.DestinationPlugin = CloudLinkPlugin;
                                        PCS2.SecureCommunicationIDCode = CloudLinkPluginSecureCommunicationIDCode;
                                        PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                                        ThermostatInformation[MacThermo].ProcessSequence = 1;
                                        ThermostatInformation[MacThermo].LastURISource = "";
                                        OutgoingDataStruct T = new OutgoingDataStruct();
                                        T.CommDataControlInfo = new CommDataControlInfoStruct[1];

                                        T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudJSpringCookies", "https://my.radiothermostat.com/filtrete/j_spring_security_check"));
                                        T.CommDataControlInfo[0].Method = "POST";

                                        T.CommDataControlInfo[0].BodyData = "j_password=" + CloudPassword + "&j_username=" + CloudUsername + "&recaptcha_challenge_field=03AHJ_Vuuinb9-NbsHM5g9qP67GjUFaxmnA2aZIbPK-6MTa7UkYkkzwTHCGNVugBKjUWmJQULLCV83gH0zg_kFpyeGwzE734Krob-kalAr2DSFGX-SQgP7gcqRVCP86PW0o8JBMpjwXQFSg5O2qhA0C0EoFktofTqNbxmNJL8FPwwq_ejopAzhMhY&recaptcha_response_field=";
                                        T.CommDataControlInfo[0].ContentType = "application/x-www-form-urlencoded";
                                        T.CommDataControlInfo[0].UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
                                        T.CommDataControlInfo[0].Referer = _PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudLogout", "https://my.radiothermostat.com/filtrete/explicitLogout.html");
                                        T.CommDataControlInfo[0].CookiesToSend = new Cookie[ODS.CommDataControlInfo[0].CookiesReturned.Count];
                                        int i = 0;
                                        foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                        {
                                            T.CommDataControlInfo[0].CookiesToSend[i] = C;
                                            i++;
                                        }
                                        T.LocalIDTag = "CloudJSpringCookies";
                                        PCS2.OutgoingDS = T.Copy();
                                        _PCF.QueuePluginInformationToPlugin(PCS2);
                                    }
                                }
                                break; //Case=0

                            case 1:
                                if (ODS.CommDataControlInfo[0].CookiesReturned != null)
                                {
                                    foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                    {
                                        if (C.Name == "JSESSIONID")
                                        {
                                            flag = true;
                                            ThermostatInformation[MacThermo].SecurityCookie = C.Value;
                                            break;
                                        }
                                    }
                                }
                                if (flag)
                                {
                                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                    PCS2.DestinationPlugin = CloudLinkPlugin;
                                    PCS2.SecureCommunicationIDCode = CloudLinkPluginSecureCommunicationIDCode;
                                    PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                                    ThermostatInformation[MacThermo].ProcessSequence = 2;
                                    ThermostatInformation[MacThermo].LastURISource = "";
                                    OutgoingDataStruct T = new OutgoingDataStruct();
                                    T.CommDataControlInfo = new CommDataControlInfoStruct[1];

                                    T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudGetRestData", "https://my.radiothermostat.com/filtrete/rest/gateways?"));
                                    T.CommDataControlInfo[0].Method = "GET";

                                    T.CommDataControlInfo[0].ContentType = "application/x-www-form-urlencoded";
                                    T.CommDataControlInfo[0].UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
                                    T.CommDataControlInfo[0].Referer =_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudJSpringCookies", "https://my.radiothermostat.com/filtrete/j_spring_security_check");
                                    T.CommDataControlInfo[0].CookiesToSend = new Cookie[ODS.CommDataControlInfo[0].CookiesReturned.Count];
                                    int i = 0;
                                    foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                    {
                                        C.Domain = "";
                                        T.CommDataControlInfo[0].CookiesToSend[i] = C;
                                        i++;
                                    }
                                    T.LocalIDTag = "CloudGetRestData";
                                    PCS2.OutgoingDS = T.Copy();
                                    _PCF.QueuePluginInformationToPlugin(PCS2);
                                }
                                break; //Case=1


                            case 2:
                                if (ODS.CommDataControlInfo[0].CookiesReturned != null)
                                {
                                    foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                    {
                                        if (C.Name == "JSESSIONID")
                                        {
                                            flag = true;
                                            if (ThermostatInformation[MacThermo].SecurityCookie != C.Value)
                                                flag = false;
                                            break;
                                        }
                                    }
                                }
                                if (!flag)
                                {
                                    ThermostatInformation[MacThermo].ProcessSequence = 0;
                                }
                                else //decode
                                {
                                    try  //Analyize the returned data for the total themostats returned by the cloud server
                                    {
                                        JArray CloudInfo = JArray.Parse(CloudThermoInfo);

                                        for (int index = 0; index < CloudInfo.Count; index++)
                                        {
                                            JObject JData = JObject.Parse(CloudInfo[index].ToString());
                                            JObject fixedStuff = JObject.Parse(JData["fixed"].ToString());
                                            JObject settings = JObject.Parse(JData["settings"].ToString());
                                            string ThermostatID = PluginCommonFunctions.ConvertStringToMacAddressFormat(JData["id"].ToString());
                                            WhichTermostatIndexByMacAddress(ThermostatID, out a, out b);
                                            PluginCommonFunctions.GetStartupField("MergeCloudData", out SX);
                                            string LastConnected = (string)settings["lastConnected"];
                                            string SinceLastConnected = (string)fixedStuff["since_last_connected_ms"];
                                            int MinutesSinceLastConnect = (int)(_PCF.ConvertToInt64(SinceLastConnected) / (Int64)60000);
                                            string TimeOffset = (string)fixedStuff["timezone_offset_ms"];
                                            DateTime LastConnectTime = new DateTime(1970, 1, 1).AddMilliseconds(_PCF.ConvertToInt64(LastConnected) + _PCF.ConvertToInt64(TimeOffset));
                                            JObject nextSetpoint = JObject.Parse(fixedStuff["nextSetpoint"].ToString());
                                            JObject CoolSetpoint = JObject.Parse(nextSetpoint["COOL"].ToString());
                                            string CoolSetpointTemp = (string)CoolSetpoint["setpoint"];
                                            string CoolSetpointTime = (string)CoolSetpoint["time"];
                                            JObject HeatSetpoint = JObject.Parse(nextSetpoint["HEAT"].ToString());
                                            string HeatSetpointTemp = (string)HeatSetpoint["setpoint"];
                                            string HeatSetpointTime = (string)HeatSetpoint["time"];
                                            JObject scheduledSetpoint = JObject.Parse(fixedStuff["scheduledSetpoint"].ToString());
                                            string CurrentCoolSetpoint = (string) scheduledSetpoint["COOL"];
                                            string CurrentHeatSetpoint = (string)scheduledSetpoint["HEAT"]; 


                                            ThermostatInformation[b].NextCoolTarget=CoolSetpointTemp;
                                            ThermostatInformation[b].NextHeatTarget=HeatSetpointTemp;
                                            ThermostatInformation[b].NextCoolTargetTime=new DateTime(1970, 1, 1).AddMilliseconds(_PCF.ConvertToInt64(CoolSetpointTime) + _PCF.ConvertToInt64(TimeOffset));
                                            ThermostatInformation[b].NextHeatTargetTime = new DateTime(1970, 1, 1).AddMilliseconds(_PCF.ConvertToInt64(HeatSetpointTime) + _PCF.ConvertToInt64(TimeOffset));
                                            ThermostatInformation[b].MinutesSinceLastConnect = MinutesSinceLastConnect;
                                            if (SX != "Y" || a == -1)
                                            {
                                                MacThermo = b;
                                            }
                                            else
                                            {
                                                MacThermo = a;
                                            }

                                            if (MacThermo == -1)
                                            {
                                                if (FirstCloud)
                                                {
                                                    string Loc = "";
                                                    try
                                                    {
                                                        JObject JLoc = (JObject)JData["settings"];
                                                        Loc = JLoc["locationId"].ToString();
                                                    }
                                                    catch
                                                    {
                                                        Loc = "";
                                                    }
                                                    _PluginCommonFunctions.GenerateLocalMessage(2, "", Loc+" "+ThermostatID);
                                                }
                                                continue; //Thermostat is not in interface list, so we ignore it!!!!!
                                            }
                                            
                                            for (int t = 0; t < HowManyThermoValues; t++)
                                            {
                                                ThermostatInformation[MacThermo].NewValues[t] = 0;
                                                ThermostatInformation[MacThermo].RawData[t] = "";
                                            }
                                            for (int which = 0; which < ThermostatInformation[MacThermo].CurrentThermoValues.Length; which++)
                                            {
                                                if (which > ThermostatInformation[MacThermo].ThermoDevices.Length-1)
                                                    continue;
                                                if (string.IsNullOrEmpty(ThermostatInformation[MacThermo].ThermoDevices[which].DeviceUniqueID))
                                                    continue;
                                                DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[which];
                                                if (string.IsNullOrEmpty(xx.DeviceUniqueID))
                                                    continue;
                                                if (xx.DeviceIdentifier.Substring(0, 3).ToLower() == "arc")
                                                    continue;
                                                int ixx=JData.ToString().IndexOf(xx.DeviceIdentifier.ToLower());
                                                if(ixx>-1)
                                                { 
                                                    ixx2=JData.ToString().IndexOf(",",ixx);
                                                    if(ixx2>-1)
                                                    {
                                                        string Item = JData.ToString().Substring(ixx, ixx2 - ixx).Replace("\"", "");
                                                        Item = Item.Trim();
                                                        SaveThermoValues(Item, MacThermo);
                                                    }
                                                }
                                            }
                                            ThermostatInformation[MacThermo].LastUpdateDateTime = LastConnectTime;                                           
                                        //Setup Schedule
                                            ThermostatInformation[MacThermo].NextTargetTemp = 0;
                                            SetNextTargetTempCloud(MacThermo, CurrentCoolSetpoint, CurrentHeatSetpoint);

                                        //Now Save the Flags
                                            CreateThermoFlags(MacThermo);
                                        
                                        }
                                        FirstCloud=false;
                                        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                        PCS2.DestinationPlugin = CloudLinkPlugin;
                                        PCS2.SecureCommunicationIDCode = CloudLinkPluginSecureCommunicationIDCode;

                                        PCS2.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
                                        ThermostatInformation[MacThermo].ProcessSequence = 2;
                                        ThermostatInformation[MacThermo].LastURISource = "";
                                        OutgoingDataStruct T = new OutgoingDataStruct();
                                        T.CommDataControlInfo = new CommDataControlInfoStruct[1];

                                        T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudGetRestData", "https://my.radiothermostat.com/filtrete/rest/gateways?"));
                                        T.CommDataControlInfo[0].Method = "GET";

                                        T.CommDataControlInfo[0].ContentType = "application/x-www-form-urlencoded";
                                        T.CommDataControlInfo[0].UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
                                        T.CommDataControlInfo[0].Referer =_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudJSpringCookies", "https://my.radiothermostat.com/filtrete/j_spring_security_check");
                                        T.CommDataControlInfo[0].CookiesToSend = new Cookie[ODS.CommDataControlInfo[0].CookiesReturned.Count];
                                        int i = 0;
                                        foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                        {
                                            C.Domain = "";
                                            T.CommDataControlInfo[0].CookiesToSend[i] = C;
                                            i++;
                                        }
                                        T.LocalIDTag = "CloudGetRestData";
                                        T.NumberOfTimesToProcessCommunicationAtTime=1;
                                        T.SecondsBetweenProcessCommunicationAtTime = _PCF.ConvertToInt32(_PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "IntervalLoopTimeInSeconds", "300"));
                                        if (T.SecondsBetweenProcessCommunicationAtTime < 1)
                                            T.SecondsBetweenProcessCommunicationAtTime = 300;
                                        PCS2.OutgoingDS = T.Copy();
                                        _PCF.QueuePluginInformationToPlugin(PCS2);

                                        for (int NMacThermo = 0; NMacThermo < ThermostatInformation.Length; NMacThermo++)
                                        {
                                            if (ThermostatInformation[NMacThermo].ThermosatLocation.ToLower() == "cloud")
                                            {
                                                if (ThermostatInformation[NMacThermo].LastDateSummaryArchived != _PCF.SaveLogsDateFormat(DateTime.Now).Substring(1, 10))
                                                {
                                                    PluginCommunicationStruct PCS3 = new PluginCommunicationStruct();
                                                    PCS3.DestinationPlugin = CloudLinkPlugin;
                                                    PCS3.SecureCommunicationIDCode = CloudLinkPluginSecureCommunicationIDCode;
                                                    PCS3.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
                                                    ThermostatInformation[NMacThermo].LastURISource = "";
                                                    OutgoingDataStruct TX = new OutgoingDataStruct();
                                                    TX.CommDataControlInfo = new CommDataControlInfoStruct[1];

                                                    string S2 = _PCF.FindValueInStartupInfo(ThermostatInformation[NMacThermo].DBInterface.StartupInformation, "CloudGetArchive", "https://my.radiothermostat.com/filtrete/rest/gateways/$$ThermoID/usage?bucket-size=PT60M&summarize=true&day=$$DataDate");
                                                    S2 = S2.Replace("$$ThermoID", ThermostatInformation[NMacThermo].ThermoID, StringComparison.OrdinalIgnoreCase);
                                                    S2 = S2.Replace("$$DataDate", ThermostatInformation[NMacThermo].LastDateSummaryArchived.Substring(0,10));
                                                    TX.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(S2);
                                                    TX.CommDataControlInfo[0].Method = "GET";

                                                    TX.CommDataControlInfo[0].ContentType = "application/x-www-form-urlencoded";
                                                    TX.CommDataControlInfo[0].UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
                                                    TX.CommDataControlInfo[0].Referer = _PCF.FindValueInStartupInfo(ThermostatInformation[NMacThermo].DBInterface.StartupInformation, "CloudJSpringCookies", "https://my.radiothermostat.com/filtrete/j_spring_security_check");
                                                    TX.CommDataControlInfo[0].CookiesToSend = new Cookie[ODS.CommDataControlInfo[0].CookiesReturned.Count];
                                                    i = 0;
                                                    foreach (Cookie C in ODS.CommDataControlInfo[0].CookiesReturned)
                                                    {
                                                        C.Domain = "";
                                                        TX.CommDataControlInfo[0].CookiesToSend[i] = C;
                                                        i++;
                                                    }
                                                    TX.LocalIDTag = "CloudGetArchive";
                                                    TX.LocalData = ThermostatInformation[NMacThermo].LastDateSummaryArchived;
                                                    TX.LocalInterface = ThermostatInformation[NMacThermo].Interface;
                                                    TX.NumberOfTimesToProcessCommunicationAtTime = 1;
                                                    TX.SecondsBetweenProcessCommunicationAtTime = _PCF.ConvertToInt32(_PCF.GetStartupFieldWithDefault("ArchiveQueryDelay", "30"));
                                                    if (TX.SecondsBetweenProcessCommunicationAtTime < 1)
                                                        TX.SecondsBetweenProcessCommunicationAtTime = 30;
                                                    TX.SecondsBetweenProcessCommunicationAtTime = TX.SecondsBetweenProcessCommunicationAtTime * (NMacThermo + 1);
                                                    PCS3.OutgoingDS = TX.Copy();
                                                    _PCF.QueuePluginInformationToPlugin(PCS3);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        _PluginCommonFunctions.GenerateErrorRecord(2000000, "Unexpected Data Error: " + CloudThermoInfo.Substring(1, 20), err.Message, err);
                                        ThermostatInformation[MacThermo].ProcessSequence = 0;
                                    }
                                }
                                break; //Case 2
                        }

                        if (ThermostatInformation[MacThermo].ProcessSequence == 0 && !string.IsNullOrEmpty(ThermostatInformation[MacThermo].DBInterface.PluginName))
                        {
                            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                            PCS2.DestinationPlugin = CloudLinkPlugin;
                            PCS2.SecureCommunicationIDCode = CloudLinkPluginSecureCommunicationIDCode;
                            PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                            ThermostatInformation[MacThermo].ProcessSequence = 0;
                            ThermostatInformation[MacThermo].LastURISource = "";
                            OutgoingDataStruct T = new OutgoingDataStruct();
                            T.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("https://my.radiothermostat.com/filtrete/login.html");
                            T.CommDataControlInfo[0].Method = "Get";

                            PCS2.OutgoingDS = T.Copy();
                            T.LocalIDTag = "Cloud-ResetLogin";
                            _PCF.QueuePluginInformationToPlugin(PCS2);
                            PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                            return;
                        }

                        PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                        CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                        return;
                    }//End of Cloud Processing


                    //IP Direct Processing
                    if (ODS.CommDataControlInfo[0].ActualResponseReceived == null || ODS.CommDataControlInfo[0].ActualResponseReceived.Length == 0)
                    {
                        PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                        CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                        return;
                    }
                    String S = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived).Replace("\"", "");
                    if (ODS.LocalIDTag == "LocalCoolSch" || ODS.LocalIDTag == "LocalHeatSch") //Schedule-Find Next Target
                    {                     
                        string S1 = S.Replace("{", "");
                        string S2 = S1.Replace("}", "");
                        string[] SV = S2.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] SD = SV[((int) ThermostatInformation[MacThermo].LastUpdateDateTime.DayOfWeek * 2) + 1].Split(new char[] { ',' });
                        if (ODS.LocalIDTag == "LocalHeatSch")
                        {
                            for (int q = 1; q < 14; q = q + 2)
                                ThermostatInformation[MacThermo].HeatSchedule[q / 2] = SV[q];
                        }
                        else
                        {
                            for (int q = 1; q < 14; q = q + 2)
                                ThermostatInformation[MacThermo].CoolSchedule[q / 2] = SV[q];
                        }

                        SetNextTargetTempIP(MacThermo);
                        PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                        CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                        return;
                    }

                    if (ODS.LocalIDTag == "LocalThermoData")
                    {
                        string[] V = S.Split(new char[] { ',', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                        if (V.Length == 0)
                        {
                            PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                            CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                            return;
                        }

                        for (int t = 0; t < HowManyThermoValues; t++)
                        {
                            ThermostatInformation[MacThermo].NewValues[t] = 0;
                            ThermostatInformation[MacThermo].RawData[t] = "";
                        }

                        foreach (string T in V)
                        {
                            SaveThermoValues(T, MacThermo);
                        }
                        ThermostatInformation[MacThermo].LastUpdateDateTime = _PluginCommonFunctions.CurrentTime;
                        
                        
                        
                        CreateThermoFlags(MacThermo);

                    }
                    PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                    CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
                    return;
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                PluginTransactionsTimer.Change(TimerTimeslice, Timeout.Infinite);
                CHMModules.WiFiThermostat3M50.LockingSemaphore.Release();
            }
  
         }

        private void SetNextTargetTempCloud(int MacThermo, string CurrentCoolSetpoint, string CurrentHeatSetpoint)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            
            int OldNextTargetTemp = ThermostatInformation[MacThermo].NextTargetTemp;
            DateTime OldNextTargetTime = ThermostatInformation[MacThermo].NextTargetTime;
            int OldCurrentTargetTemp = ThermostatInformation[MacThermo].CurrentTargetTemp;

            if (ThermostatInformation[MacThermo].Mode.ToLower() == "off")
                return;
            
            if (ThermostatInformation[MacThermo].Mode.ToLower() == "heat")
            {
                ThermostatInformation[MacThermo].NextTargetTemp=_PCF.ConvertToInt32(ThermostatInformation[MacThermo].NextHeatTarget);
                ThermostatInformation[MacThermo].NextTargetTime = ThermostatInformation[MacThermo].NextHeatTargetTime;
                ThermostatInformation[MacThermo].CurrentTargetTemp = _PCF.ConvertToInt32(CurrentHeatSetpoint);
            }

            if (ThermostatInformation[MacThermo].Mode.ToLower() == "cool")
            {
                ThermostatInformation[MacThermo].NextTargetTemp = _PCF.ConvertToInt32(ThermostatInformation[MacThermo].NextCoolTarget);
                ThermostatInformation[MacThermo].NextTargetTime = ThermostatInformation[MacThermo].NextCoolTargetTime;
                ThermostatInformation[MacThermo].CurrentTargetTemp=_PCF.ConvertToInt32(CurrentCoolSetpoint);
            }
            
            if (OldNextTargetTemp != ThermostatInformation[MacThermo].NextTargetTemp 
    || OldNextTargetTime != ThermostatInformation[MacThermo].NextTargetTime)
            {
                ThermostatInformation[MacThermo].NextTargetChanged = true;
            }
            
            if (OldCurrentTargetTemp != ThermostatInformation[MacThermo].CurrentTargetTemp)
                ThermostatInformation[MacThermo].CurrentTargetChanged = true;
       }

        private void SetNextTargetTempIP(int MacThermo)
        {
            string [] SV=null;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            int OldNextTargetTemp = ThermostatInformation[MacThermo].NextTargetTemp;
            DateTime OldNextTargetTime = ThermostatInformation[MacThermo].NextTargetTime;
            int OldCurrentTargetTemp = ThermostatInformation[MacThermo].CurrentTargetTemp;
 

            if (ThermostatInformation[MacThermo].Mode.ToLower() == "off")
                return;    

            if (ThermostatInformation[MacThermo].Mode.ToLower() == "heat")
                
            {
                SV = ThermostatInformation[MacThermo].HeatSchedule;
                ThermostatInformation[MacThermo].CurrentTargetTemp=_PCF.ConvertToInt32(ThermostatInformation[MacThermo].HeatTarget);
            }

            if (ThermostatInformation[MacThermo].Mode.ToLower() == "cool")
            {
                SV = ThermostatInformation[MacThermo].CoolSchedule;
                ThermostatInformation[MacThermo].CurrentTargetTemp=_PCF.ConvertToInt32(ThermostatInformation[MacThermo].CoolTarget);
            }

            if (SV==null)
                return;

            DateTime Today = ThermostatInformation[MacThermo].LastUpdateDateTime.Date;
            DateTime Tomorrow = Today.AddDays(1);

            string[] SD = SV[(int) Today.DayOfWeek].Split(new char[] { ',' });
            string[] SDT = SV[(int) Tomorrow.DayOfWeek].Split(new char[] { ',' });

            int m = ThermostatInformation[MacThermo].LastUpdateDateTime.Hour * 60 + ThermostatInformation[MacThermo].LastUpdateDateTime.Minute;
            ThermostatInformation[MacThermo].NextTargetTemp = 0;
            for (int p = 0; p < SD.Length; p += 2)
            {
                if (_PCF.ConvertToInt32(SD[p]) > m)
                {
                    ThermostatInformation[MacThermo].NextTargetTemp = _PCF.ConvertToInt32(SD[p + 1]);
                    ThermostatInformation[MacThermo].NextTargetTime = Today.AddMinutes(_PCF.ConvertToInt32(SD[p]));
                    break;
                }
            }

            if (ThermostatInformation[MacThermo].NextTargetTemp == 0)
            {
                ThermostatInformation[MacThermo].NextTargetTime = Tomorrow.AddMinutes(_PCF.ConvertToInt32(SDT[0]));
                ThermostatInformation[MacThermo].NextTargetTemp = _PCF.ConvertToInt32(SDT[1]);
            }

            if (OldNextTargetTemp != ThermostatInformation[MacThermo].NextTargetTemp 
                || OldNextTargetTime != ThermostatInformation[MacThermo].NextTargetTime)
            {
                ThermostatInformation[MacThermo].NextTargetChanged = true;
            }
            if (OldCurrentTargetTemp != ThermostatInformation[MacThermo].CurrentTargetTemp)
                ThermostatInformation[MacThermo].CurrentTargetChanged = true;
        }

        private void CreateThermoFlags(int MacThermo)
        {
            try //Devices
            {
                String StateValue = "";
                int ThermoModeLocation;
                int _3MCurrentTarget=0;
                int _3MTarget=0;
                int overrideLocation = -1;
                bool OverrideChange = false;
                bool Hold = false;
                decimal ThermoModeValue;
                Int16 LastOverrideValue = -1;
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

                try
                {
                    ThermoModeLocation = Array.IndexOf(ThermostatInformation[MacThermo].DeviceValueNames, "tmode");
                    ThermoModeValue = ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation];
                }
                catch (Exception e)
                {
                    _PluginCommonFunctions.GenerateErrorRecordLocalMessage(1, ThermostatInformation[MacThermo].DBInterface.InterfaceName, e.Message);
                    return;
                }
                int r=_PCF.ConvertToInt32( _PCF.FindValueInStartupInfo(ThermostatInformation[MacThermo].DBInterface.StartupInformation, "CloudOfflineTimeoutMinutes", "60"));
                if (r == 0)
                    r = 60;
                if (ThermostatInformation[MacThermo].MinutesSinceLastConnect > r)
                {
                    if (ThermostatInformation[MacThermo].LastMode != "offline")
                    {
                        for (int which = 0; which < ThermostatInformation[MacThermo].ThermoDevices.Length; which++)
                        {
                            DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[which];
                            if (string.IsNullOrEmpty(xx.DeviceUniqueID))
                                continue;
                            if(xx.DeviceIdentifier.ToLower() == "tmode")
                            {
                                string Unkn;
                                string[] St = XMLDeviceScripts.GetStates(xx.CommandSet, out Unkn);
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                    xx.DeviceName,
                                    St[4],
                                    "",
                                    ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = true;
                                PluginCommonFunctions.LocalSaveLogs(ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, xx.States[4], "", xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                if (xx.AdditionalFlagName.Length>0)
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                        "",
                                        StateValue,
                                        ThermostatInformation[MacThermo].CurrentThermoValues[which].ToString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                }
                                continue;
                            }
                                

                            if (xx.DeviceIdentifier.ToLower() == "3mupdatetime")
                            {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                    xx.DeviceName,
                                    ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                    ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                    ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                if (xx.AdditionalFlagName.Length > 0)
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                        "",
                                        ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                        ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                }
                                continue;
                            }


                            PluginCommonFunctions.AddFlagForTransferToServer(
                                ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                xx.DeviceName,
                                "",
                                "",
                                ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                xx.DeviceUniqueID,
                                FlagChangeCodes.OwnerOnly,
                                FlagActionCodes.delete);
                            ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = false;
                            if (xx.AdditionalFlagName.Length > 0)
                            {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                            "",
                                            StateValue,
                                        ThermostatInformation[MacThermo].CurrentThermoValues[which].ToString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.delete);
                                ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = false;
                            }
                        }
                    }
                    ThermostatInformation[MacThermo].LastMode = "offline";
                    return;
                }


                if (ThermoModeValue == 0 && ThermostatInformation[MacThermo].LastMode.ToLower() != "off") //Now Off
                {
                    for (int which = 0; which < ThermostatInformation[MacThermo].ThermoDevices.Length; which++)
                    {
                        DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[which];
                        if (string.IsNullOrEmpty(xx.DeviceUniqueID))
                            continue;

                        //Flags We Keep when Off
                        if (ThermostatInformation[MacThermo].DontCreateFlagCode[which])
                            continue;

                        //Delete These Flags if Off
                        PluginCommonFunctions.AddFlagForTransferToServer(
                            ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                            xx.DeviceName,
                            "",
                            "",
                            ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                            xx.DeviceUniqueID,
                            FlagChangeCodes.OwnerOnly,
                            FlagActionCodes.delete);
                        ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = false;
                        if (xx.AdditionalFlagName.Length > 0)
                        {
                            PluginCommonFunctions.AddFlagForTransferToServer(
                                    xx.AdditionalFlagName,
                                        "",
                                        StateValue,
                                    ThermostatInformation[MacThermo].CurrentThermoValues[which].ToString(),
                                    "",
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.delete);
                            ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = false;
                        }
                    }
                }


                for (int which = 0; which < ThermostatInformation[MacThermo].ThermoDevices.Length; which++)
                {
                    DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[which];
                    if (string.IsNullOrEmpty(xx.DeviceUniqueID))
                        continue;
                    if (xx.DeviceIdentifier.ToLower() == "override")//This fixes a bug in the local thermostat control by determaining ovveride at the end by comparing scheduled temp with target temp when hold not in effect.
                    {
                        overrideLocation = which;
                        continue;
                    }


                    if (ThermostatInformation[MacThermo].NewValues[which]>0)
                    {
                        if (ThermostatInformation[MacThermo].NewValues[which] == 2)
                        {
                            int x = (int)ThermostatInformation[MacThermo].CurrentThermoValues[which];
                            if (xx.States == null)
                                StateValue = x.ToString();
                            else
                            {
                                if (x < 0 || x > 9)
                                    StateValue = xx.StateUnknown;
                                else
                                    StateValue = xx.States[x];
                            }
                            if (string.IsNullOrEmpty(StateValue))
                                StateValue = x.ToString();
                        }
                        if (ThermostatInformation[MacThermo].NewValues[which] == 1)
                            StateValue = ThermostatInformation[MacThermo].RawData[which];

                        if(xx.DeviceIdentifier=="hold")
                        {
                            OverrideChange=true;
                            if (ThermostatInformation[MacThermo].RawData[which]=="1")
                                Hold = true;
                            else
                                Hold=false;

                        }

                        if (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] == 0 &&!ThermostatInformation[MacThermo].DontCreateFlagCode[which])//If 3MMode is 0 then thermostat is off
                        {
                                    //skip this flag
                        }
                        else
                        {
                            if (ThermostatInformation[MacThermo].FlagCurrentlyExists[which] == false || ThermostatInformation[MacThermo].CurrentThermoValues[which] != ThermostatInformation[MacThermo].OldThermoValues[which])
                            {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                    xx.DeviceName,
                                    StateValue,
                                    ThermostatInformation[MacThermo].RawData[which],
                                    ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                                    PluginCommonFunctions.LocalSaveLogs(ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, StateValue, ThermostatInformation[MacThermo].RawData[which], xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                if(xx.AdditionalFlagName.Length>0)
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                        "",
                                        StateValue,
                                        ThermostatInformation[MacThermo].CurrentThermoValues[which].ToString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                    if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                        PluginCommonFunctions.LocalSaveLogs(xx.AdditionalFlagName, "", StateValue, ThermostatInformation[MacThermo].RawData[which], xx, ThermostatInformation[MacThermo].LastUpdateDateTime);
                                    ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = true;
                                }
                            }

                            ThermostatInformation[MacThermo].FlagCurrentlyExists[which] = true;
                        }
                    }
                }


                for (int which = 0; which < ThermostatInformation[MacThermo].ThermoDevices.Length; which++)
                {
                    DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[which];
                    if (string.IsNullOrEmpty(xx.DeviceUniqueID))
                        continue;
                    if (xx.DeviceIdentifier.Substring(0, 2).ToLower() == "3m") //Manual Flags
                    {
                        if(xx.DeviceIdentifier.ToLower()=="3mupdatetime")
                        {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                    xx.DeviceName,
                                    ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                    ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                    ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                if (xx.AdditionalFlagName.Length > 0)
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                        "",
                                        ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                        ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortDateString() + " " + ThermostatInformation[MacThermo].LastUpdateDateTime.ToShortTimeString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                }
                               continue;
                        }

                        if (xx.DeviceIdentifier.ToLower() == "3mnexttemp" && ThermostatInformation[MacThermo].NextTargetChanged)
                        {

                            if(ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] ==0 )
                            {
                            }
                            else
                            {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                    xx.DeviceName,
                                    ThermostatInformation[MacThermo].NextTargetTemp.ToString(),
                                    ThermostatInformation[MacThermo].NextTargetTemp.ToString(),
                                    ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                                    PluginCommonFunctions.LocalSaveLogs(ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, ThermostatInformation[MacThermo].NextTargetTemp.ToString(), ThermostatInformation[MacThermo].NextTargetTemp.ToString(), xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                if (xx.AdditionalFlagName.Length > 0)
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                        "",
                                        ThermostatInformation[MacThermo].NextTargetTemp.ToString(),
                                        ThermostatInformation[MacThermo].NextTargetTemp.ToString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                    if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                        PluginCommonFunctions.LocalSaveLogs(xx.AdditionalFlagName, "", ThermostatInformation[MacThermo].NextTargetTemp.ToString(), ThermostatInformation[MacThermo].NextTargetTemp.ToString(), xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                }
                            }
                            continue;
                        }
                       
                        if (xx.DeviceIdentifier.ToLower() == "3mnexttime" && ThermostatInformation[MacThermo].NextTargetChanged)
                        {
                            if (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] == 0)
                            {
                            }
                            else
                            {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                    xx.DeviceName,
                                    _PCF.SaveLogsDateFormat(ThermostatInformation[MacThermo].NextTargetTime),
                                    ThermostatInformation[MacThermo].NextTargetTime.ToString(),
                                    ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                                    PluginCommonFunctions.LocalSaveLogs(ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, string.Format("{0:00}:{1:00}", ThermostatInformation[MacThermo].NextTargetTime.Hour, ThermostatInformation[MacThermo].NextTargetTime.Minute), ThermostatInformation[MacThermo].NextTargetTime.ToString(), xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                if (xx.AdditionalFlagName.Length > 0)
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        xx.AdditionalFlagName,
                                        "",
                                        _PCF.SaveLogsDateFormat(ThermostatInformation[MacThermo].NextTargetTime),
                                        ThermostatInformation[MacThermo].NextTargetTime.ToString(),
                                        "",
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                    if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                        PluginCommonFunctions.LocalSaveLogs( xx.AdditionalFlagName, "", _PCF.SaveLogsDateFormat(ThermostatInformation[MacThermo].NextTargetTime), ThermostatInformation[MacThermo].NextTargetTime.ToString(), xx, ThermostatInformation[MacThermo].LastUpdateDateTime);
                                }
                            }
                            continue;
                         }

                        if (xx.DeviceIdentifier.ToLower() == "3mcurrenttarget" )
                        {
                            _3MCurrentTarget = ThermostatInformation[MacThermo].CurrentTargetTemp;

                            if(ThermostatInformation[MacThermo].CurrentTargetChanged)
                            {
                                if (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] == 0)
                                {
                                }
                                else
                                {
                                    PluginCommonFunctions.AddFlagForTransferToServer(
                                        ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                        xx.DeviceName,
                                        ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(),
                                        ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(),
                                        ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                        xx.DeviceUniqueID,
                                        FlagChangeCodes.OwnerOnly,
                                        FlagActionCodes.addorupdate);
                                    if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                                        PluginCommonFunctions.LocalSaveLogs( ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(), ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(), xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                    if (xx.AdditionalFlagName.Length > 0)
                                    {
                                        PluginCommonFunctions.AddFlagForTransferToServer(
                                            xx.AdditionalFlagName,
                                            "",
                                            ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(),
                                            ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(),
                                            "",
                                            xx.DeviceUniqueID,
                                            FlagChangeCodes.OwnerOnly,
                                            FlagActionCodes.addorupdate);
                                        if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                            PluginCommonFunctions.LocalSaveLogs( xx.AdditionalFlagName, "", ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(), ThermostatInformation[MacThermo].CurrentTargetTemp.ToString(), xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                                    }
                                }
                                OverrideChange = true;
                                ThermostatInformation[MacThermo].CurrentTargetChanged = false;
                            }
                            continue;
                        }

                        if (xx.DeviceIdentifier.ToLower() == "3mtarget")
                        {
                            string Value="";

                            if (ThermostatInformation[MacThermo].Mode.ToLower() == "heat")
                                Value = ThermostatInformation[MacThermo].HeatTarget;
                            if (ThermostatInformation[MacThermo].Mode.ToLower() == "cool")
                                Value = ThermostatInformation[MacThermo].CoolTarget;
                            if (ThermostatInformation[MacThermo].Mode.ToLower() == "off")
                            {
                                continue;
                            }
                            if (ThermostatInformation[MacThermo].Mode.ToLower() == "auto")
                            {
                                continue;
                            }
                             _3MTarget =PluginCommonFunctions.ConvertToInt32(Value);

                            if (Value == ThermostatInformation[MacThermo].Target && ThermostatInformation[MacThermo].TargetFlag)
                                continue;
                            if (Value == "" && ThermostatInformation[MacThermo].TargetFlag)
                            {
                                ThermostatInformation[MacThermo].TargetFlag = false;
                                continue;
                            }
                                
                            PluginCommonFunctions.AddFlagForTransferToServer(
                                ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                                xx.DeviceName,
                                Value,
                                Value,
                                ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                                xx.DeviceUniqueID,
                                FlagChangeCodes.OwnerOnly,
                                FlagActionCodes.addorupdate);
                            if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                                PluginCommonFunctions.LocalSaveLogs( ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, StateValue, ThermostatInformation[MacThermo].RawData[which], xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                            if (xx.AdditionalFlagName.Length > 0)
                            {
                                PluginCommonFunctions.AddFlagForTransferToServer(
                                    xx.AdditionalFlagName,
                                    "",
                                    Value,
                                    Value,
                                    "",
                                    xx.DeviceUniqueID,
                                    FlagChangeCodes.OwnerOnly,
                                    FlagActionCodes.addorupdate);
                                if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                    PluginCommonFunctions.LocalSaveLogs(xx.AdditionalFlagName, "", Value, Value, xx, ThermostatInformation[MacThermo].LastUpdateDateTime);
                            }


                            ThermostatInformation[MacThermo].TargetFlag = true;
                            ThermostatInformation[MacThermo].Target = Value;
                            OverrideChange = true;
                            continue;
                        }
                        continue;                       
                    }

                }
                
                
                //Now Check For Override (override if 3MCurrentTarget!=3MTarget)
                if (_3MCurrentTarget > 0 && _3MTarget > 0 && overrideLocation >= 0 && OverrideChange && !Hold)
                {
                    DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[overrideLocation];
                    if (_3MCurrentTarget == _3MTarget)
                    {
                        ThermostatInformation[MacThermo].CurrentThermoValues[overrideLocation] = 0;
                    }
                    else
                    {
                        ThermostatInformation[MacThermo].CurrentThermoValues[overrideLocation] = 1;
                    }
                    
                    if (_3MCurrentTarget == _3MTarget)
                    {
                        if (string.IsNullOrEmpty(xx.States[0]))
                            StateValue = xx.StateUnknown;
                        else
                            StateValue = xx.States[0];
                        if (string.IsNullOrEmpty(StateValue))
                            StateValue = "0";
                        PluginCommonFunctions.AddFlagForTransferToServer(
                            ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                            xx.DeviceName,
                            StateValue,
                            "0",
                            ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                            xx.DeviceUniqueID,
                            FlagChangeCodes.OwnerOnly,
                            FlagActionCodes.addorupdate);
                        LastOverrideValue =0;
                        if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                            PluginCommonFunctions.LocalSaveLogs(ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, StateValue, "0", xx, ThermostatInformation[MacThermo].LastUpdateDateTime);

                        if (xx.AdditionalFlagName.Length > 0)
                        {
                            PluginCommonFunctions.AddFlagForTransferToServer(
                                xx.AdditionalFlagName,
                                "",
                                StateValue,
                                "0",
                                "",
                                xx.DeviceUniqueID,
                                FlagChangeCodes.OwnerOnly,
                                FlagActionCodes.addorupdate);
                            if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                PluginCommonFunctions.LocalSaveLogs(xx.AdditionalFlagName, "", StateValue, "0", xx, ThermostatInformation[MacThermo].LastUpdateDateTime);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(xx.States[1]))
                            StateValue = xx.StateUnknown;
                        else
                            StateValue = xx.States[1];
                        if (string.IsNullOrEmpty(StateValue))
                            StateValue = "1";
                        PluginCommonFunctions.AddFlagForTransferToServer(
                            ThermostatInformation[MacThermo].DBInterface.InterfaceName,
                            xx.DeviceName,
                            StateValue,
                            "1",
                            ThermostatInformation[MacThermo].DBInterface.RoomUniqueID,
                            xx.DeviceUniqueID,
                            FlagChangeCodes.OwnerOnly,
                            FlagActionCodes.addorupdate);
                        LastOverrideValue =1;
                        if (xx.LogCode.ToLower() == "always" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "on"))
                            PluginCommonFunctions.LocalSaveLogs( ThermostatInformation[MacThermo].DBInterface.InterfaceName, xx.DeviceName, StateValue, "1", xx, ThermostatInformation[MacThermo].LastUpdateDateTime);
                        if (xx.AdditionalFlagName.Length > 0)
                        {
                            PluginCommonFunctions.AddFlagForTransferToServer(
                                xx.AdditionalFlagName,
                                "",
                                StateValue,
                                "1",
                                "",
                                xx.DeviceUniqueID,
                                FlagChangeCodes.OwnerOnly,
                                FlagActionCodes.addorupdate);
                            if (xx.LogCode.ToLower() == "salways" || (ThermostatInformation[MacThermo].CurrentThermoValues[ThermoModeLocation] > 0 && xx.LogCode.ToLower() == "son"))
                                PluginCommonFunctions.LocalSaveLogs( xx.AdditionalFlagName, "", StateValue, "1", xx, ThermostatInformation[MacThermo].LastUpdateDateTime);
                        }
                    }
                }
                Array.Copy(ThermostatInformation[MacThermo].CurrentThermoValues, ThermostatInformation[MacThermo].OldThermoValues, ThermostatInformation[MacThermo].CurrentThermoValues.Length);
                ThermostatInformation[MacThermo].NextTargetChanged = false;
            }

            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }
        
        private void SaveThermoValues(string T,int MacThermo)
        {
            int i;
            int mode;
            try
            {
                string N = T.Substring(0, T.IndexOf(':')).ToLower().Trim();
                string V = T.Substring(T.IndexOf(':') + 1).ToLower().Trim();
                if (N.Substring(0, 3).ToLower() == "arc")
                    return;

                if (N.ToLower() == "tmode")
                {
                    ThermostatInformation[MacThermo].LastMode = ThermostatInformation[MacThermo].Mode;

                    if (!int.TryParse(V, out mode))
                    {
                        ThermostatInformation[MacThermo].Mode = V.Substring(0,1)+V.Substring(1).ToLower();
                        ThermostatInformation[MacThermo].Mode = ThermostatInformation[MacThermo].Mode.Trim();
                        if (ThermostatInformation[MacThermo].Mode == "off")
                            mode = 0;
                        if (ThermostatInformation[MacThermo].Mode == "heat")
                            mode = 1;
                        if (ThermostatInformation[MacThermo].Mode == "cool")
                            mode = 2;
                        if (ThermostatInformation[MacThermo].Mode == "auto")
                            mode = 3;
                    }
                    else
                    {
                            if (mode == 0)
                                ThermostatInformation[MacThermo].Mode = "off";
                            if (mode == 1)
                                ThermostatInformation[MacThermo].Mode = "heat";
                            if (mode == 2)
                                ThermostatInformation[MacThermo].Mode = "cool";
                            if (mode == 3)
                                ThermostatInformation[MacThermo].Mode = "auto";
                    }
                    int lpos = Array.IndexOf(ThermostatInformation[MacThermo].DeviceValueNames, N);
                    if (lpos >= 0)//Found
                    {
                        ThermostatInformation[MacThermo].RawData[lpos] = ThermostatInformation[MacThermo].Mode;
                        ThermostatInformation[MacThermo].NewValues[lpos] = 1;
                        ThermostatInformation[MacThermo].CurrentThermoValues[lpos] = mode;
                    }
                    return;
                }

                if (N.ToLower() == "t_heat")
                {
                    ThermostatInformation[MacThermo].HeatTarget = V;
//                    ThermostatInformation[MacThermo].CoolTarget = "";
                }

                if (N.ToLower() == "t_cool")
                {
                    ThermostatInformation[MacThermo].CoolTarget = V;
//                    ThermostatInformation[MacThermo].HeatTarget = "";
                }

                int pos = Array.IndexOf(ThermostatInformation[MacThermo].DeviceValueNames, N);
                if(pos>=0)//Found
                {
                    ThermostatInformation[MacThermo].RawData[pos] = V.ToLower().Trim();
                    ThermostatInformation[MacThermo].NewValues[pos] = 2;
                    if(!decimal.TryParse(V,out ThermostatInformation[MacThermo].CurrentThermoValues[pos]))
                    {
                            string S = ThermostatInformation[MacThermo].RawData[pos];
                            DeviceStruct xx = ThermostatInformation[MacThermo].ThermoDevices[pos];
                        if (xx.States != null)
                        {
                            for (i = 0; i < 10; i++)
                            {
                                if (xx.States[i].ToLower() == S)
                                {
                                    ThermostatInformation[MacThermo].CurrentThermoValues[pos] = i;
                                    ThermostatInformation[MacThermo].NewValues[pos] = 1;
                                    break;
                                }

                            }
                        }
                    }
                    else
                    {
                        ThermostatInformation[MacThermo].NewValues[pos] = 2; //was 1
                    }
                }
            }
            catch
            {
            }
        }
        
        private void WhichTermostatIndexByMacAddress(string Mac, out int LocalThermostat, out int CloudThermostat)
        {
            LocalThermostat = -1;
            CloudThermostat = -1;
            try
            {

                if (HowManyThermostats == 0)
                    return;
                string p = Mac.ToUpper();
                for (Int16 i = 0; i < HowManyThermostats; i++)
                {
                    if (ThermostatInformation[i].DBInterface.HardwareIdentifier.ToUpper() == p)
                    {
                        if (ThermostatInformation[i].ThermosatLocation.ToLower() == "cloud")
                            CloudThermostat = i;
                        else
                            LocalThermostat = i;
                    }
                }

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;
        }
        
        
        private static Int16 MacThermoIndex(string p)
        {
            try
            {
                if (HowManyThermostats == 0)
                    return (-1);
                for (Int16 i = 0; i < HowManyThermostats; i++)
                {
                    if (ThermostatInformation[i].DBInterface.InterfaceUniqueID == p)
                    {
                        return (i);
                    }
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
  
            return(-1);
        }

    }
}
