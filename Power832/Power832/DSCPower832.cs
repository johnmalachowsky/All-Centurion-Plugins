using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using CHMPluginAPI;
using CHMPluginAPICommon;


namespace CHMModules
{
    public class DSCPower832
    {
       
        static internal _PluginCommonFunctions PluginCommonFunctions;
        private static string LinkPlugin;
        private static string LinkPluginReferenceIdentifier;
        private static string LinkPluginSecureCommunicationIDCode;

        private static DeviceStruct[] Devices;
        private static Tuple<string, string>[] Rooms;
        private static bool StartupCompleteAndLinked = false;
        private static bool FirstHeartbeat = true;

        internal struct DSCPower832Devices
        {
            internal DeviceStruct Devices;
            internal bool HasValidDevice;
            internal bool HasReceivedValidData;
            internal int CurrentValue;
            internal int PreviousValue;
            internal string Room;
            internal DateTime LastChangeTime;
        }


        internal static DSCPower832Devices[] _DSCPower832Devices;
        internal static ConcurrentQueue<PluginEventArgs> IncomingDataQueue;
        private static System.Threading.Timer ProcessTimer;
        internal static SemaphoreSlim LockingSemaphore;
        internal static DeviceStruct DeviceFlagStruct;

        /// <summary>
        /// PluginInitialize
        /// </summary>
        /// <param name="UniqueID"></param>


        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "DSC Power 832 Alarm Console";
            ServerAccessFunctions.PluginSerialNumber = "00001-00011";
            ServerAccessFunctions.PluginVersion = "1.0.0";

            PluginCommonFunctions = new _PluginCommonFunctions();
            ServerAccessFunctions._HeartbeatServerEvent += HeartbeatServerEventHandler;
            ServerAccessFunctions._TimeEventServerEvent += TimeEventServerEventHandler;
            ServerAccessFunctions._InformationCommingFromPluginServerEvent += InformationCommingFromPluginEventHandler;
            ServerAccessFunctions._WatchdogProcess += WatchdogProcessEventHandler;
            ServerAccessFunctions._ShutDownPlugin += ShutDownPluginEventHandler;
            ServerAccessFunctions._StartupInfoFromServer += StartupInfoEventHandler;
            ServerAccessFunctions._PluginStartupCompleted += PluginStartupCompleted;
//            ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;


            IncomingDataQueue = new ConcurrentQueue<PluginEventArgs>();


            return;
        }


       
        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = false;

            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;
        }
        /// <summary>
        /// PluginStartupCompleted
        /// </summary>
        /// <param name="WhichEvent"></param>
        /// <param name="Value"></param>
        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            LockingSemaphore = new SemaphoreSlim(1);

            if (_PluginCommonFunctions.Interfaces.Length >1)
            {
                _PluginCommonFunctions.GenerateErrorRecordLocalMessage(20001, "", _PluginCommonFunctions.Interfaces.Length.ToString());
            }

            _DSCPower832Devices = new DSCPower832Devices[64];
            for (int i = 0; i < 64; i++)
                _DSCPower832Devices[i].Devices = new DeviceStruct();

//            XMLDeviceScripts XMLScripts = new XMLDeviceScripts();
            try
            {
                foreach (KeyValuePair<string, DeviceStruct> SN in _PluginCommonFunctions.LocalDevicesByUnique)
                {
                    if(SN.Value.DeviceIdentifier== "DSCTEMPLATE")
                    {
                        DeviceFlagStruct = SN.Value;
                        continue;
                    }

                    int i = -1;
                    if (SN.Value.DeviceIdentifier.Substring(0, 3).ToLower() != "dsc" || !int.TryParse(SN.Value.DeviceIdentifier.Substring(3), out i))
                        continue;
                    _DSCPower832Devices[(int)i - 1].Devices = SN.Value;
                    _DSCPower832Devices[(int)i - 1].HasValidDevice = true;
                    _DSCPower832Devices[(int)i - 1].Room = PluginCommonFunctions.GetRoomFromUniqueID(SN.Value.RoomUniqueID);
  //                  XMLScripts.SetupXMLConfiguration(ref _DSCPower832Devices[(int)i - 1].Devices);


                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            TimerCallback ProcessTimerCallBack = new TimerCallback(new ThreadedDataProcessing().ProcessIncomingSpontaniousData);
            ProcessTimer = new System.Threading.Timer(ProcessTimerCallBack, null, Timeout.Infinite, Timeout.Infinite);
        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF= new _PluginCommonFunctions();

            if (StartupCompleteAndLinked)
            {
                if(FirstHeartbeat)
                {
                    ThreadedDataProcessing TDP = new ThreadedDataProcessing();
                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    PCS.Command = PluginCommandsToPlugins.ProcessCommunicationWOClearingBuffer;
                    PCS.DestinationPlugin = LinkPlugin;
                    PCS.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    PCS.OutgoingDS = new OutgoingDataStruct();
                    PCS.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    string S = "0550";
                    PCS.OutgoingDS.CommDataControlInfo[0].CharactersToSend = TDP.CalcualteChecksum(S);
                    PCS.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Nothing;
                    PCS.OutgoingDS.LocalIDTag = "Time Stamp";
                    _PCF.QueuePluginInformationToPlugin(PCS);

                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                    PCS2.Command = PluginCommandsToPlugins.ProcessCommunicationWOClearingBuffer;
                    PCS2.DestinationPlugin = LinkPlugin;
                    PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    PCS2.OutgoingDS = new OutgoingDataStruct();
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    S = "0501";
                    PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = TDP.CalcualteChecksum(S);
                    PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Nothing;
                    PCS2.OutgoingDS.LocalIDTag = "Desc Arming";

                    _PCF.QueuePluginInformationToPlugin(PCS2);
                }
                
                if ((HeartbeatTimeCode)Value.HeartBeatTC == HeartbeatTimeCode.NewDay || FirstHeartbeat)
                {
                    ThreadedDataProcessing TDP = new ThreadedDataProcessing();

                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    PCS.Command = PluginCommandsToPlugins.ProcessCommunicationWOClearingBuffer;
                    PCS.DestinationPlugin = LinkPlugin;
                    PCS.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    PCS.OutgoingDS = new OutgoingDataStruct();
                    DateTime CT = _PluginCommonFunctions.CurrentTime;
                    string S = "010" + CT.Hour.ToString("00") + CT.Minute.ToString("00") + CT.Month.ToString("00") + CT.Day.ToString("00") + CT.Year.ToString().Substring(2, 2);
                    PCS.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS.OutgoingDS.CommDataControlInfo[0].CharactersToSend = TDP.CalcualteChecksum(S);
                    PCS.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Nothing;
                    PCS.OutgoingDS.LocalIDTag = "Set Time";
                    _PCF.QueuePluginInformationToPlugin(PCS);

               }
               FirstHeartbeat = false;
            }

        }

        private static void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent)
        {
            PluginEventArgs Value;

            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
            {

                if (Value.PluginData.Command == PluginCommandsToPlugins.SpontaniousDataReceived)
                {
                    IncomingDataQueue.Enqueue(Value);
                    if (LockingSemaphore.CurrentCount > 0)
                        ProcessTimer.Change(0, System.Threading.Timeout.Infinite);
                }

                if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionComplete)
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

                    LinkPlugin = Value.PluginData.OriginPlugin;
                    LinkPluginReferenceIdentifier = Value.PluginData.PluginReferenceIdentifier;
                    LinkPluginSecureCommunicationIDCode = Value.PluginData.SecureCommunicationIDCode;

                    continue;
                }


                if (Value.PluginData.Command == PluginCommandsToPlugins.LinkedCommReady)
                {
                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();

                    PCS2.Command = PluginCommandsToPlugins.WaitOnIncomingData;
                    PCS2.DestinationPlugin = Value.PluginData.OriginPlugin;
                    PCS2.PluginReferenceIdentifier = Value.PluginData.PluginReferenceIdentifier;
                    PCS2.ReferenceUniqueNumber = Value.PluginData.UniqueNumber;
                    PCS2.SecureCommunicationIDCode = Value.PluginData.SecureCommunicationIDCode;

                    PCS2.OutgoingDS = new OutgoingDataStruct();
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS2.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n' };
                    PCS2.OutgoingDS.SpontaniousData_SleepInterval = 100;
                    PCS2.OutgoingDS.LocalIDTag = "Spont Data";
                    _PCF.QueuePluginInformationToPlugin(PCS2);
                    StartupCompleteAndLinked = true;

                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    PCS.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
                    PCS.DestinationPlugin = LinkPlugin;
                    PCS.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    PCS.OutgoingDS = new OutgoingDataStruct();
                    PCS.OutgoingDS.NumberOfTimesToProcessCommunicationAtTime = int.MaxValue;
                    PCS.OutgoingDS.SecondsBetweenProcessCommunicationAtTime = PluginCommonFunctions.GetStartupField("SecondsBetweenFullDump", 300);

                    PCS.OutgoingDS.ProcessCommunicationAtTimeTime = _PluginCommonFunctions.CurrentTime.AddSeconds(60);
                    PCS.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'0', (Byte)'0', (Byte)'1', (Byte)'9', (Byte)'1', (Byte)'\r', (Byte)'\n' };
                    PCS.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Nothing;
                    PCS.OutgoingDS.LocalIDTag = "SYS Status";
                    _PCF.QueuePluginInformationToPlugin(PCS);
                }

                if (Value.PluginData.Command == PluginCommandsToPlugins.CancelLink)
                {
                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();

                    PCS.Command = PluginCommandsToPlugins.ActionCompleted;
                    PCS.DestinationPlugin = Value.PluginData.OriginPlugin;
                    PCS.PluginReferenceIdentifier = Value.PluginData.PluginReferenceIdentifier;
                    PCS.ReferenceUniqueNumber = Value.PluginData.UniqueNumber;
                    _PCF.QueuePluginInformationToPlugin(PCS);
                    continue;
                }
            }
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Release();
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

class ThreadedDataProcessing
{
    static int Smoke = 0, ManualAlarm = 0, AlarmStatus = 0, AccessCodes = 0, PanelTroubles = 841;

    
    internal byte[] CalcualteChecksum(string Command)
    {
        byte B = 0;

        foreach (Byte q in Command)
        {
            B = (byte) (B + q);
        }
        string S = Command+String.Format("{0:X}\r\n", (int) B);
         char[] C= S.ToCharArray();
        return(CHMModules.DSCPower832.PluginCommonFunctions.ConvertCharArrayToByteArray(C));
    }

    void ProcessPower832Data(DeviceStruct Device, string StateValue, string RawDisplay, string Room)
    {
        CHMModules.DSCPower832.PluginCommonFunctions.AddFlagForTransferToServer(
            Room,
            Device.DeviceName,
            StateValue,
            RawDisplay,
            Device.RoomUniqueID,
            Device.DeviceUniqueID,
            FlagChangeCodes.OwnerOnly,
            FlagActionCodes.addorupdate,
            "");

        if (Device.AdditionalFlagName.Length > 0)
        {
            CHMModules.DSCPower832.PluginCommonFunctions.AddFlagForTransferToServer(
                Device.AdditionalFlagName,
                "",
                StateValue,
                RawDisplay,
                "",
                Device.DeviceUniqueID,
                FlagChangeCodes.OwnerOnly,
                FlagActionCodes.addorupdate,
                "");
        }
    }

    string ProcessPower832Data(string StatusMessageCode, string Command, string RawTrimmed, bool Logit)
    {
        string StatMessage="", StatMessageLog="";
        bool ChangeArchiveStatus = false, NewArchiveStatus = false;
        if (_PluginCommonFunctions.LookupStatusDictionary(Command, out StatMessage, out StatMessageLog))
        {

            string AlarmStatusStatusMessage = "", AlarmStatusStatusMessageLog = "";
            if (!string.IsNullOrEmpty(StatusMessageCode))
            {
                if (_PluginCommonFunctions.LookupStatusDictionary(StatusMessageCode, out AlarmStatusStatusMessage, out AlarmStatusStatusMessageLog))
                {
                    if (CHMModules.DSCPower832.DeviceFlagStruct.DeviceIdentifier.ToLower()=="dsctemplate")
                    {
                        ChangeArchiveStatus = true;
                        NewArchiveStatus = true;
                    }


                    CHMModules.DSCPower832.PluginCommonFunctions.AddFlagForTransferToServer(
                        CHMModules.DSCPower832.PluginCommonFunctions.GetRoomFromUniqueID(CHMModules.DSCPower832.DeviceFlagStruct.RoomUniqueID)+" "+ CHMModules.DSCPower832.DeviceFlagStruct.DeviceName,
                        AlarmStatusStatusMessage,
                        StatMessage,
                        RawTrimmed,
                        CHMModules.DSCPower832.DeviceFlagStruct.RoomUniqueID,
                        _PluginCommonFunctions.LocalInterface.InterfaceUniqueID,
                        FlagChangeCodes.OwnerOnly,
                        FlagActionCodes.addorupdate,
                        "",
                        ChangeArchiveStatus,
                        NewArchiveStatus);
                }
            }
         }
        return (StatMessage);
    }




    public void ProcessIncomingSpontaniousData(object DownInterfaceIndex)
    {
        PluginEventArgs Value;
        int Zone, Partition, AlarmCommand, CommandInt;
        string Raw, Command, Data, StateValue, RawTrimmed, AlarmTime, RawTimed, RawDisplay;

        CHMModules.DSCPower832.LockingSemaphore.Wait();
        _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
        XMLDeviceScripts XMLScripts = new XMLDeviceScripts();

        try
        {
            while (CHMModules.DSCPower832.IncomingDataQueue.TryDequeue(out Value))
            {
                OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS;
                Raw = new string(_PCF.ConvertByteArrayToCharArray(ODS.CommDataControlInfo[0].ActualResponseReceived));
                RawTimed = Raw.Trim('\r', '\n');
                if(RawTimed.Substring(2,1)==":")
                {
                    RawTrimmed = RawTimed.Substring(9);
                    AlarmTime = RawTimed.Substring(0, 8);

                }
                else
                {
                    RawTrimmed=RawTimed;
                    AlarmTime = "        ";
                }
                Command = RawTrimmed.Substring(0, 3);
                CommandInt = CHMModules.DSCPower832.PluginCommonFunctions.ConvertToInt32(Command);
                Data = RawTrimmed.Substring(3);
                Data=Data.Substring(0, Data.Length - 2);
                Partition = 0;
                switch (Command)
                {
                    case "601": //Zone Alarm
                    case "602": //Zone Alarm Restore
                    case "603": //Zone Tamper
                    case "604": //Zone Tamper Restore
                    case "700": //User Closing 
                    case "750": //User Opening 

                        Partition = CHMModules.DSCPower832.PluginCommonFunctions.ConvertToInt32(Data.Substring(0,1));
                        Data = Data.Substring(1);
                        break;
                }
                RawDisplay = RawTrimmed + " " + AlarmTime;

                switch (Command)
                {
                    case "500": //Command acknowledge
                        break;

                    case "501": //Command Error
                    case "502": //System Error
                        _PluginCommonFunctions.GenerateErrorRecordLocalMessage(20000, ProcessPower832Data("Command", Command, RawDisplay, true), RawDisplay);
                        break;

//Smoke Detectors
                    case "631": //2-Wire Smoke Alarm
                    case "632": //2-Wire Smoke Restore 
                        if (Smoke == CommandInt)
                            ProcessPower832Data("Smoke", Command, RawDisplay, false);
                        else
                            ProcessPower832Data("Smoke", Command, RawDisplay, true);
                        Smoke = CommandInt;
                        break;

//Manual Alarm Activities
                    case "620": //Duress Alarm 
                    case "621": //Fire Key Alarm 
                    case "622": //Fire Key Restore 
                    case "623": //Auxiliary Key Alarm 
                    case "624": //Auxiliary Restoral 
                    case "625": //Panic Key Alarm 
                    case "626": //Panic Restoral
                        if(ManualAlarm==CommandInt)
                            ProcessPower832Data("ManualAlarm", Command, RawDisplay, false);
                        else
                            ProcessPower832Data("ManualAlarm", Command, RawDisplay, true);
                        ManualAlarm = CommandInt;
                        break;

//Alarm Status Stuff (Partition)
                    case "650": //Partition Ready 
                    case "651": //Partition Not Ready 
                    case "652": //Partition Armed 
                    case "654": //Partition In Alarm 
                    case "655": //Partition Disarmed 
                    case "656": //Exit Delay in Progress 
                    case "657": //Entry Delay in Progress 
                    case "658": //Keypad Lock-out 
                    case "700": //User Closing 
                    case "701": //Special Closing 
                    case "702": //Partial Closing 
                        if (AlarmStatus == CommandInt)
                            ProcessPower832Data("AlarmStatus", Command, RawDisplay, false);
                        else
                            ProcessPower832Data("AlarmStatus", Command, RawDisplay, true);
                        AlarmStatus = CommandInt;
                        break;

                  
//Alarm Access Codes Stuff
                    case "670": //Invalid Access Code 
                    case "750": //User Opening 
                    case "751": //Special Opening 
                        if (AccessCodes == CommandInt)
                            ProcessPower832Data("AccessCodes", Command, RawDisplay, false);
                        else
                            ProcessPower832Data("AccessCodes", Command, RawDisplay, true);
                        AccessCodes = CommandInt;
                        break;


//Device Stuff
                    case "601": //Zone Alarm
                    case "602": //Zone Alarm Restore
                    case "603": //Zone Tamper
                    case "604": //Zone Tamper Restore
                    case "605": //Zone Fault
                    case "606": //Zone Fault Restore
                    case "609": //Zone Open
                    case "610": //Zone Restored

                        if(!int.TryParse(Data, out Zone) || !int.TryParse(Command, out AlarmCommand))
                        {
                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(20000, Data+""+ Command, RawDisplay);
                            break;
                        }
                        if(Zone<1 || Zone>64)
                        {
                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(20000, Data + "" + Command, RawDisplay);
                            break;
                        }


                        if (AlarmCommand!=610)
                            CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].HasReceivedValidData = true;
                        CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].PreviousValue = CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].CurrentValue;
                        CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].CurrentValue = AlarmCommand;
                        CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].LastChangeTime = _PluginCommonFunctions.CurrentTime;

                        StateValue = AlarmCommand.ToString();
                        int index = AlarmCommand - 601;

                        if (CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].HasValidDevice)
                        {
                            if (CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices.StoredDeviceData==null)
                            {
                                CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices.StoredDeviceData = new DeviceDataStruct();
                                _PluginCommonFunctions.GenerateErrorRecord(2000003, "StoredDeviceDataStruct Not Found For Record", CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices.NativeDeviceIdentifier, null);
                            }
                            if (CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices.StoredDeviceData.Local_StatesFlagAttributes.Count > 0)
                            {
                                if (index < CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices.StoredDeviceData.Local_StatesFlagAttributes.Count)
                                {
                                    StateValue = CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices.StoredDeviceData.Local_StatesFlagAttributes[index];
                                }
                            }
                            ProcessPower832Data(CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Devices, StateValue, RawDisplay, CHMModules.DSCPower832._DSCPower832Devices[Zone - 1].Room);
                        }
                        break;

//Panel Stuff
                    case "800": //Panel Battery Trouble 
                    case "801": //Panel Battery Trouble Restore 
                    case "802": //Panel AC Trouble 
                    case "803": //Panel AC Restore 
                    case "806": //System Bell Trouble 
                    case "807": //System Bell Trouble Restoral 
                    case "821": //Device Low Battery 
                    case "822": //Device Low Battery Restore 
                    case "825": //Wireless Key Low Battery Trouble 
                    case "826": //Wireless Key Low Battery Trouble Restore 
                    case "827": //Handheld Keypad Low Battery Alarm 
                    case "828": //keypad low battery condition has been restored
                    case "829": //General System Tamper 
                    case "830": //General System Tamper Restore 
                    case "840": //Trouble Status 
                    case "841": //Trouble Status Restore 
                    case "842": //Fire Trouble Alarm
                    case "843": //Fire Trouble Alarm Restore
                        if (PanelTroubles == CommandInt)
                            ProcessPower832Data("PanelTroubles", Command, RawDisplay, false);
                        else
                            ProcessPower832Data("PanelTroubles", Command, RawDisplay, true);
                        PanelTroubles = CommandInt;
                        break;
                }
            }
        }
        catch (Exception CHMAPIEx)
        {
            _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
        }
        CHMModules.DSCPower832.LockingSemaphore.Release();

    }
}