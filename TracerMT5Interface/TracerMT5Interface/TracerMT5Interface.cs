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
    public class TracerMT5Interface
    {
       
        static internal _PluginCommonFunctions PluginCommonFunctions;
        private static string LinkPlugin;
        private static string LinkPluginReferenceIdentifier;
        private static string LinkPluginSecureCommunicationIDCode;

        private static DeviceStruct[] Devices;
        private static Tuple<string, string>[] Rooms;
        private static bool StartupCompleteAndLinked = false;
        private static bool FirstHeartbeat = true;

        internal struct TracerMT5InterfaceDevices
        {
            internal DeviceStruct Devices;
            internal bool HasValidDevice;
            internal bool HasReceivedValidData;
            internal string PreviousValue;
            internal string PreviousRawValue;
            internal string Room;
            internal DateTime LastChangeTime;
            internal double AccumlatedValue;
            internal int AccumlatedSeconds;
            internal char AccumType;
        }


        internal static TracerMT5InterfaceDevices[] _TracerMT5InterfaceDevices;
        internal static ConcurrentQueue<PluginEventArgs> IncomingDataQueue;
        private static System.Threading.Timer ProcessTimer;
        internal static SemaphoreSlim LockingSemaphore;

        /// <summary>
        /// PluginInitialize
        /// </summary>
        /// <param name="UniqueID"></param>


        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Outback Mate Interface";
            ServerAccessFunctions.PluginSerialNumber = "00001-00014";
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
            ServerAccessFunctions._Command += CommandEvent;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;



            IncomingDataQueue = new ConcurrentQueue<PluginEventArgs>();


            return;
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
 
            TimerCallback ProcessTimerCallBack = new TimerCallback(new ThreadedDataProcessing().ProcessIncomingSpontaniousData);
            ProcessTimer = new System.Threading.Timer(ProcessTimerCallBack, null, Timeout.Infinite, Timeout.Infinite);
            LockingSemaphore = new SemaphoreSlim(1);
            _TracerMT5InterfaceDevices = new TracerMT5InterfaceDevices[_PluginCommonFunctions.Devices.Length];
            int index=0;
            foreach (DeviceStruct SN in _PluginCommonFunctions.Devices)
            {
                _TracerMT5InterfaceDevices[(int)index].Devices = SN;
                _TracerMT5InterfaceDevices[(int)index].HasValidDevice = true;
                _TracerMT5InterfaceDevices[(int)index].HasReceivedValidData = false;
                _TracerMT5InterfaceDevices[(int)index].Room = PluginCommonFunctions.GetRoomFromUniqueID(SN.RoomUniqueID);
                _TracerMT5InterfaceDevices[(int)index].AccumlatedValue=0;
                _TracerMT5InterfaceDevices[(int)index].AccumlatedSeconds=0;

                index++;
            }

        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF= new _PluginCommonFunctions();

            if (Value.HeartBeatTC == HeartbeatTimeCode.NewHour || Value.HeartBeatTC == HeartbeatTimeCode.NewDay)
            {
                for (int index = 0; index < CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices.Length; index++)
                {
                    if (!CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].HasReceivedValidData)
                        continue;

                    if ((CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices.LogCode.ToLower() == "daily" && Value.HeartBeatTC == HeartbeatTimeCode.NewDay) || (CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices.LogCode.ToLower() == "hourly" && Value.HeartBeatTC == HeartbeatTimeCode.NewHour))
                    {
                        CHMModules.TracerMT5Interface.PluginCommonFunctions.LocalSaveLogs(_PluginCommonFunctions.Interfaces[0].InterfaceName, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices.DeviceName, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].PreviousValue, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].PreviousRawValue, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices);
                        CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedSeconds = 0;
                        CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedValue = 0;
                    }
                }
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

                    OutgoingDataStruct T = new OutgoingDataStruct();
                    T.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    T.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)13 };
                    T.SpontaniousData_SleepInterval = 1000;
                    T.LocalIDTag = "Spont Data";
                    PCS2.OutgoingDS = T.Copy();
                    _PCF.QueuePluginInformationToPlugin(PCS2);
                    StartupCompleteAndLinked = true;
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
            for (int index = 0; index < CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices.Length; index++)
            {
                if (CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices.LogCode.ToLower() == "daily" || CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices.LogCode.ToLower() == "hourly")
                    if (CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].HasReceivedValidData)
                        CHMModules.TracerMT5Interface.PluginCommonFunctions.LocalSaveLogs(_PluginCommonFunctions.Interfaces[0].InterfaceName, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices.DeviceName, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].PreviousValue, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].PreviousRawValue, CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].Devices);
            }
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

    void TracerMT5InterfaceData(DeviceStruct Device, string StateValue, string RawDisplay, string Room, int InfoIndex)
    {

        if (InfoIndex > -1)//Check if This flag is a duplicate
        {
            if(CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[InfoIndex].HasReceivedValidData)
            {
                if (StateValue == CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[InfoIndex].PreviousValue)
                    return;
            }
        }

        CHMModules.TracerMT5Interface.PluginCommonFunctions.AddFlagForTransferToServer(
            Room,
            Device.DeviceName,
            StateValue,
            RawDisplay,
            Device.RoomUniqueID,
            Device.InterfaceUniqueID,
            FlagChangeCodes.OwnerOnly,
            FlagActionCodes.addorupdate);

        if (Device.LogCode.ToLower() == "always" && InfoIndex>-1)
            CHMModules.TracerMT5Interface.PluginCommonFunctions.LocalSaveLogs(_PluginCommonFunctions.Interfaces[0].InterfaceName, Device.DeviceName, StateValue, RawDisplay, Device);

        if (Device.AdditionalFlagName.Length > 0)
        {
            CHMModules.TracerMT5Interface.PluginCommonFunctions.AddFlagForTransferToServer(
                Device.AdditionalFlagName,
                "",
                StateValue,
                RawDisplay,
                "",
                "",
                FlagChangeCodes.OwnerOnly,
                FlagActionCodes.addorupdate);
        }
        if (InfoIndex > -1)
        {
            CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[InfoIndex].PreviousValue = StateValue;
            CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[InfoIndex].PreviousRawValue = RawDisplay;
            CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[InfoIndex].HasReceivedValidData = true;
            CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[InfoIndex].LastChangeTime = _PluginCommonFunctions.CurrentTime;
        }
    }
    
    
    public void ProcessIncomingSpontaniousData(object DownInterfaceIndex)
    {
        PluginEventArgs Value;

        CHMModules.TracerMT5Interface.LockingSemaphore.Wait();
        _PluginCommonFunctions _PCF = new _PluginCommonFunctions();


        try
        {
            while (CHMModules.TracerMT5Interface.IncomingDataQueue.TryDequeue(out Value))
            {
                OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS.Copy();
                string Raw = new string(_PCF.ConvertByteArrayToCharArray(ODS.CommDataControlInfo[0].ActualResponseReceived)).Trim();
                string DCode = Raw.Substring(0, 1);
                for (int index = 0; index < CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices.Length; index++)
                {
                    try
                    {
                        CHMModules.TracerMT5Interface.TracerMT5InterfaceDevices OMID = CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index];

                        if (OMID.Devices.DeviceIdentifier.Length < 9)
                            continue;
                        if (OMID.Devices.DeviceIdentifier.Substring(2, 1) != DCode)
                            continue;
                        int loc1 = _PCF.ConvertToInt32(OMID.Devices.DeviceIdentifier.Substring(4, 2));
                        int len1 = _PCF.ConvertToInt32(OMID.Devices.DeviceIdentifier.Substring(6, 2));
                        string Function = OMID.Devices.DeviceIdentifier.Substring(8, 1);
                        if (loc1 < 1 || len1 < 1)
                            continue;
                        string FVS=Raw.Substring(loc1, len1);
                        int FV = _PCF.ConvertToInt32(FVS);

                        switch (Function)
                        {
                           
                            case "V":
                                TracerMT5InterfaceData(OMID.Devices, FV.ToString(), FV.ToString(), OMID.Room, index);
                                continue;
                            
                            case "T": //Table
                                string[] Table=OMID.Devices.CommandList.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < Table.Length;i++)
                                {
                                    if(Table[i].Substring(0,2)==FVS)
                                    {
                                        TracerMT5InterfaceData(OMID.Devices, Table[i].Substring(3).Trim(), FVS, OMID.Room, index);
                                        break;
                                    }

                                }
                                continue;

                            case "B":  //Byte Table
                            case "X": //AUX Output
                                string[] Bytes = OMID.Devices.CommandList.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                byte FVB = (byte)FV;

                                if(Function=="X")
                                {
                                    if (Bytes.Length < 2)
                                        continue;
                                     if ((FVB & (1 << _PCF.ConvertToInt32(Bytes[0].Substring(0, 1))))!=0)
                                     {
                                         TracerMT5InterfaceData(OMID.Devices, Bytes[0].Substring(2).Trim(), FVS, OMID.Room, index);
                                     }
                                     else
                                     {
                                         TracerMT5InterfaceData(OMID.Devices, Bytes[1].Substring(2).Trim(), FVS, OMID.Room, index);
                                     }
                                     continue;
                                }

                                if(FV==0)
                                    TracerMT5InterfaceData(OMID.Devices, "False", FVS, OMID.Room, index);
                                else
                                    TracerMT5InterfaceData(OMID.Devices, "True", FVS, OMID.Room, index);

                                if (!CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].HasReceivedValidData || FVS!=CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].PreviousValue)
                                { 
                                    for (int i = 0; i < Bytes.Length; i++)
                                    {
                                        DeviceStruct DV = OMID.Devices;
                                        DV.DeviceName = DV.DeviceName + " " + Bytes[i].Substring(2).Trim();
                                        if ((FVB & (1 << _PCF.ConvertToInt32(Bytes[i].Substring(0, 1)))) != 0)
                                        {
                                            TracerMT5InterfaceData(DV, "True", FVS, OMID.Room,-1);
                                        }
                                        else
                                        {
                                            TracerMT5InterfaceData(DV, "False", FVS, OMID.Room, -1);
                                        }
                                    }
                                }
                                continue;


                            case "*":
                            case "/":
                            case "+":
                            case "-":
                            case "H":
                            case "D":
                                int SV=0;

                                if (OMID.Devices.DeviceIdentifier.Length < 13)
                                    continue;
                                if (OMID.Devices.DeviceIdentifier.Substring(9, 1)=="V")
                                {
                                    int loc2 = _PCF.ConvertToInt32(OMID.Devices.DeviceIdentifier.Substring(10, 2));
                                    int len2 = _PCF.ConvertToInt32(OMID.Devices.DeviceIdentifier.Substring(12, 2));
                                    SV = _PCF.ConvertToInt32(Raw.Substring(loc2, len2));
                                }
                                if (OMID.Devices.DeviceIdentifier.Substring(9, 1)=="L")
                                {
                                    SV = _PCF.ConvertToInt32(OMID.Devices.DeviceIdentifier.Substring(10, 3));
                                }

                                double Result=0;

                                if (Function == "H" || Function == "D")
                                {
                                    CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedSeconds++;
                                    if(OMID.Devices.DeviceIdentifier.Substring(OMID.Devices.DeviceIdentifier.Length-1)=="*")
                                        CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedValue += ((double)((double)FV * (double)SV));
                                    if (OMID.Devices.DeviceIdentifier.Substring(OMID.Devices.DeviceIdentifier.Length - 1)=="/" && SV!=0)
                                        CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedValue += ((double)((double)FV / (double)SV));
                                    Result = CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedValue / CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumlatedSeconds;
                                    string Formatted2 = String.Format("{0:0.00}", Result);
                                    TracerMT5InterfaceData(OMID.Devices, Formatted2, Result.ToString(), OMID.Room, index);
                                    CHMModules.TracerMT5Interface._TracerMT5InterfaceDevices[index].AccumType = (char) Function[0];
                                    continue;
                                }


                                switch (Function)
                                {
                                    case "*":
                                        Result=(double)((double)FV*(double)SV);
                                        break;
                                    case "/":
                                        if(SV!=0)
                                            Result=(double)((double)FV/(double)SV);
                                        break;
                                    case "+":
                                        Result=(double)((double)FV+(double)SV);
                                        break;
                                    case "-":
                                        Result=(double)((double)FV-(double)SV);
                                        break;
                                }
                                string Formatted=Result.ToString();
                                string Fmt = OMID.Devices.DeviceIdentifier.Substring(OMID.Devices.DeviceIdentifier.Length-2);
                                if (Fmt == "II")
                                    Formatted = Convert.ToInt32(Result).ToString();
                                if (Fmt == "D2")
                                    Formatted = String.Format("{0:0.00}", Result);
                                TracerMT5InterfaceData(OMID.Devices, Formatted, Result.ToString(), OMID.Room, index);
                                continue;


                        }
                    }
                    catch
                    {

                    }
                }
       
            }
        }
        catch
        {

        }
        CHMModules.TracerMT5Interface.LockingSemaphore.Release();

    }
}