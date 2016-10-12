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
    public class OutbackMateInterface
    {
       
        static internal _PluginCommonFunctions PluginCommonFunctions;
        private static string LinkPlugin;
        private static string LinkPluginReferenceIdentifier;
        private static string LinkPluginSecureCommunicationIDCode;

        private static DeviceStruct[] Devices;
        private static Tuple<string, string>[] Rooms;
        private static bool StartupCompleteAndLinked = false;
        private static bool FirstHeartbeat = true;

        internal class OutbackMateInterfaceDevices
        {
            internal DeviceStruct Devices;
            internal bool HasValidDevice;
            internal bool HasReceivedValidData;
            internal bool FirstValidValue;
            internal string PreviousValue;
            internal string PreviousRawValue;
            internal string Room;
            internal DateTime LastChangeTime;
            internal double AccumlatedValue;
            internal int AccumlatedSeconds;
            internal char AccumType;
        }


        internal static OutbackMateInterfaceDevices[] _OutbackMateInterfaceDevices;
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

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            XMLDeviceScripts XMLScripts = new XMLDeviceScripts();

            TimerCallback ProcessTimerCallBack = new TimerCallback(new ThreadedDataProcessing().ProcessIncomingSpontaniousData);
            ProcessTimer = new System.Threading.Timer(ProcessTimerCallBack, null, Timeout.Infinite, Timeout.Infinite);
            LockingSemaphore = new SemaphoreSlim(1);
            _OutbackMateInterfaceDevices = new OutbackMateInterfaceDevices[_PluginCommonFunctions.LocalDevicesByUnique.Count];
            int index=0;
            foreach (KeyValuePair<string, DeviceStruct> SN in _PluginCommonFunctions.LocalDevicesByUnique)
            {
                _OutbackMateInterfaceDevices[(int)index] = new OutbackMateInterfaceDevices();
                _OutbackMateInterfaceDevices[(int)index].Devices = SN.Value;
                _OutbackMateInterfaceDevices[(int)index].HasValidDevice = true;
                XMLScripts.SetupXMLConfiguration(ref _OutbackMateInterfaceDevices[(int)index].Devices);
                _OutbackMateInterfaceDevices[(int)index].HasReceivedValidData = false;
                _OutbackMateInterfaceDevices[(int)index].FirstValidValue = false;
                _OutbackMateInterfaceDevices[(int)index].Room = PluginCommonFunctions.GetRoomFromUniqueID(SN.Value.RoomUniqueID);
                _OutbackMateInterfaceDevices[(int)index].AccumlatedValue=0;
                _OutbackMateInterfaceDevices[(int)index].AccumlatedSeconds=0;

                index++;
            }

        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF= new _PluginCommonFunctions();

            if (Value.HeartBeatTC == HeartbeatTimeCode.NewHour || Value.HeartBeatTC == HeartbeatTimeCode.NewDay)
            {
                for (int index = 0; index < CHMModules.OutbackMateInterface._OutbackMateInterfaceDevices.Length; index++)
                {
                    if (!CHMModules.OutbackMateInterface._OutbackMateInterfaceDevices[index].HasReceivedValidData)
                        continue;

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
                    PCS2.OutgoingDS = new OutgoingDataStruct();
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS2.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)13 };
                    PCS2.OutgoingDS.SpontaniousData_SleepInterval = 1000;
                    PCS2.OutgoingDS.LocalIDTag = "Spont Data";
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

    public void ProcessIncomingSpontaniousData(object DownInterfaceIndex)
    {
        PluginEventArgs Value;
        Tuple<string, string, string, Tuple<int, string>[]> SU;

        CHMModules.OutbackMateInterface.LockingSemaphore.Wait();
        _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
        XMLDeviceScripts XMLScripts = new XMLDeviceScripts();

        try
        {
            while (CHMModules.OutbackMateInterface.IncomingDataQueue.TryDequeue(out Value)) 
            {
                OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS;
                string Raw = new string(_PCF.ConvertByteArrayToCharArray(ODS.CommDataControlInfo[0].ActualResponseReceived)).Trim();
                string DCode = Raw.Substring(0, 1);
                for (int index = 0; index < CHMModules.OutbackMateInterface._OutbackMateInterfaceDevices.Length; index++)
                {
                    try
                    {
                        CHMModules.OutbackMateInterface.OutbackMateInterfaceDevices OMID = CHMModules.OutbackMateInterface._OutbackMateInterfaceDevices[index];

                        if (OMID.Devices.DeviceIdentifier.Length < 9)
                            continue;
                        if (OMID.Devices.DeviceIdentifier.Substring(2, 1) != DCode)
                            continue;
                        _PluginCommonFunctions.UOM.TryGetValue(_PCF.ConvertToInt32(OMID.Devices.UOMCode), out SU);

                        string V = "<property spontdata=\"" + Raw + "\"  uom=\"" + SU.Item2 + "\"/>";
                        XMLScripts.ProcessDeviceXMLScriptFromData(ref OMID.Devices, V, XMLDeviceScripts.DeviceScriptsDataTypes.XML);
                        continue;
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
        CHMModules.OutbackMateInterface.LockingSemaphore.Release();

    }
}