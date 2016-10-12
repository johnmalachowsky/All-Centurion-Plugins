using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets; 
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using CHMPluginAPI;
using CHMPluginAPICommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



#region User Namespaces
using System.Collections;
#endregion

//Required Parameters
//  DeviceName
//  IPAddress
//  Port
//  UpdatePriority
//  DefaultEmptySleep
//  DefaultWaitingSleep
//  UniqueID
//  ResponseMaxWaitTime
//  ReceiveTimeout
// DefaultSleepingCheckInterval


//Suggested Parameters
//  MACAddress


//Other Parameters
//  Baud
//  Data
//  Flow
//  Parity
//  Stop
//  SpontaniousDataEndCharsHEX


namespace CHMModules
{

    public class NetworkGatewayInterface
    {

        internal static _PluginCommonFunctions PluginCommonFunctions;
        internal static int ProcessStatus = 0;
        internal static System.Threading.Timer WatchdogTimer;


        #region Required User Routines
        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Network Gateway Interface";
            ServerAccessFunctions.PluginSerialNumber = "00001-00004";
            ServerAccessFunctions.PluginVersion = "2.0.0";

            PluginCommonFunctions = new _PluginCommonFunctions();
            SecureCommunicationIDCodes = new string[1];

            PluginCommonFunctions.HeartbeatTimeCodeToInvoke = HeartbeatTimeCode.NewMinute;

            ServerAccessFunctions._HeartbeatServerEvent += HeartbeatEventHandler;
            ServerAccessFunctions._TimeEventServerEvent += PluginEventHandler;
            ServerAccessFunctions._WatchdogProcess += PluginEventHandler;
            ServerAccessFunctions._ShutDownPlugin += ShutDownEventHandler;
            ServerAccessFunctions._StartupInfoFromServer += PluginEventHandler;
            ServerAccessFunctions._CurrentServerStatus += CurrentServerStatusChangeEventHandler;
            ServerAccessFunctions._InformationCommingFromPluginServerEvent += InformationCommingFromPluginPluginEventHandler;
            ServerAccessFunctions._PluginStartupCompleted += PluginStartupCompleted;
            ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;

            MinuteThreadData = new ThreadedDataProcessing();
            HeartbeatThreadData = new ThreadedDataProcessing();

            return;
        }

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = false;

            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;
        }

        #endregion

        #region Optional User Stuff

        #region Optional User Globals, Structures, etc.

        internal enum FlowEnum { None, RTSCTS, XonXoff };
        internal ThreadedDataProcessing MinuteThreadData;
        internal ThreadedDataProcessing HeartbeatThreadData;

        internal struct DelayedDataTransactionStruct
        {
            internal DateTime DateTimeToExecute;
            internal OutgoingDataStruct OutgoingData;
        }

        internal struct NetworkInterfaceData
        {
            internal bool Active;
            internal bool Valid;
            internal int Sequence;
            internal string UniqueID;
            internal string ThisDLLServerKey;
            internal string[] DBInformation;
            internal string DLLToSendDataTo;
            internal bool ValidDestination;
            internal string DeviceName;
            internal string CommType;
            internal string MACAddress;
            internal string Address;
            internal string Port;
            internal string SetupString;
            internal DateTime LastActualIncomingDataTime;

            // Drivers
            internal Assembly Driver;
            internal Type AssemblyType;
            internal object Instance;


            //Sockets
            //internal System.Net.Sockets.TcpClient clientSocket;
            //internal NetworkStream serverStream;
            internal bool SentNoConnectError;
            internal int SentNoConnectErrorCount;
            internal int NoConnectRetryTime;


            ////HTTP
            //HttpWebRequest request;
            //HttpWebResponse response;
            //Stream Answer;
            //StreamReader _Answer;
            //string ReadToEnd;

            //General Setup Info            
            internal volatile int DefaultWaitingSleep;
            internal volatile int DefaultSleepingCheckInterval;
            internal volatile int ReceiveTimeout;
            internal volatile int TransmitTimeout;
            internal volatile int ResponseMaxWaitTime;
            internal volatile int UndesignatedMaxCharsToRead;
            internal DateTime LastCharactersSentTime;
            internal DateTime LastCharacersReceivedTime;
            internal volatile bool WaitOnIncomingDataStructActive;
            internal OutgoingDataStruct WaitOnIncomingDataStruct;
            internal volatile int WaitOnIncomingDataStructSleepTime;
            internal OutgoingDataStruct LoopIncomingDataStruct;
            internal bool LoopIncomingDataStructActive;
            internal DateTime LastLoopProcessing;
            internal DateTime NextLoopProcessing;
            internal volatile int LoopIntervalTime;
            internal ConcurrentQueue<OutgoingDataStruct> StoredOutgoingDataStructs;
            internal ConcurrentStack<OutgoingDataStruct> ProcessNextDataStructs;
            internal ConcurrentQueue<OutgoingDataStruct> PriorityDoNow;
            internal ArrayList Response;
            internal long TotalCharactersRead;
            internal long TotalCharactersWritten;
            internal Queue<char> UnattachedStoredIncommingData;
            internal DateTime NextDelayedDataTransaction;
            internal List<DelayedDataTransactionStruct> DelayedDataTransactions;

            //Thread Info
            internal Thread ProcessThread;
            internal SemaphoreSlim LockingSemaphore;
            internal DateTime NextWakeupTime;
            internal DateTime LastWakeupTime;
            internal int CurrentSleepMiliseconds;
            internal int LastSleepMiliseconds;
            internal bool InterruptSleep;

            //Optional Serial Setup Info             
            internal int Baud;
            internal int Data;
            internal string Flow;
            internal int Stop;
            internal string SpontaniousDataEndCharsHEX;
            internal string Parity;
            internal string SecureCommunicationIDCode;

        }

        internal static NetworkInterfaceData[] NetworkGatewayInterfaces;
        internal static string[] SecureCommunicationIDCodes;
        internal static bool ShutDownInProgress = false;
//        internal static System.Threading.Timer[] SystemTimers;
        #endregion

        #region Optional User Routines

        //static int ConvertToInt32(string Value)
        //{
        //    int x;
        //    Int32.TryParse(Value, out x);
        //    return (x);
        //}



        private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            int InterfacesCount = 0;
            String S;
            int index = 0;
            int location = 0;
            string[] DBStartupInfo;
            int t;
            IPAddress address;

            try
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

                index = 0;
                while (PluginCommonFunctions.GetDBRecord(index, out DBStartupInfo))
                {
                    index++;
                    if (DBStartupInfo[0] != "Interfaces")
                        continue;
                    InterfacesCount++;
                }

                NetworkGatewayInterfaces = new NetworkInterfaceData[InterfacesCount];
                SecureCommunicationIDCodes = new string[InterfacesCount];

                location = 0;
                index = 0;
                while (PluginCommonFunctions.GetDBRecord(index, out DBStartupInfo))
                {
                    index++;
                    if (DBStartupInfo[0] != "Interfaces")
                        continue;
                    NetworkGatewayInterfaces[location].LastActualIncomingDataTime = DateTime.MinValue;
                    NetworkGatewayInterfaces[location].SecureCommunicationIDCode = PluginCommonFunctions.GenerateSecureID();
                    NetworkGatewayInterfaces[location].Active = false;
                    NetworkGatewayInterfaces[location].Valid = false;
                    NetworkGatewayInterfaces[location].ReceiveTimeout = 25;
                    NetworkGatewayInterfaces[location].TransmitTimeout = 25;
                    NetworkGatewayInterfaces[location].Sequence = PluginCommonFunctions.NextSequence;
                    NetworkGatewayInterfaces[location].Response = new ArrayList(Math.Max(_PCF.GetStartupField("StartingResultSize",0), 10));
                    NetworkGatewayInterfaces[location].UnattachedStoredIncommingData = new Queue<char>();
                    NetworkGatewayInterfaces[location].StoredOutgoingDataStructs = new ConcurrentQueue<OutgoingDataStruct>();
                    NetworkGatewayInterfaces[location].DelayedDataTransactions = new List<DelayedDataTransactionStruct>();
                    NetworkGatewayInterfaces[location].ProcessNextDataStructs = new ConcurrentStack<OutgoingDataStruct>();
                    NetworkGatewayInterfaces[location].PriorityDoNow = new ConcurrentQueue<OutgoingDataStruct>();
                    NetworkGatewayInterfaces[location].NextDelayedDataTransaction = DateTime.MaxValue;
                    NetworkGatewayInterfaces[location].ValidDestination = false;
                    NetworkGatewayInterfaces[location].SentNoConnectError = false;
                    NetworkGatewayInterfaces[location].SentNoConnectErrorCount = 0;

                    PluginCommonFunctions.GetStartupField("NoConnectRetryTime", out NetworkGatewayInterfaces[location].NoConnectRetryTime, 30000);
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "NoConnectRetryTime", out S))
                        NetworkGatewayInterfaces[location].NoConnectRetryTime = PluginCommonFunctions.ConvertToInt32(S);

                    NetworkGatewayInterfaces[location].DBInformation = DBStartupInfo;
                    NetworkGatewayInterfaces[location].UniqueID = DBStartupInfo[1];
                    NetworkGatewayInterfaces[location].WaitOnIncomingDataStructActive = false;
                    NetworkGatewayInterfaces[location].LoopIntervalTime = 60000;
                    NetworkGatewayInterfaces[location].CommType = DBStartupInfo[4].ToUpper().Trim();
                    NetworkGatewayInterfaces[location].LockingSemaphore = new SemaphoreSlim(1);
                    NetworkGatewayInterfaces[location].InterruptSleep = false;

                    try
                    {
                        NetworkGatewayInterfaces[location].Driver = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "NetworkGatewayInterface_" + NetworkGatewayInterfaces[location].CommType + ".dll"));
                        string SX = "CHMModules.NetworkGatewayInterface_" + NetworkGatewayInterfaces[location].CommType;
                        NetworkGatewayInterfaces[location].AssemblyType = NetworkGatewayInterfaces[location].Driver.GetType("CHMModules.NetworkGatewayInterface_" + NetworkGatewayInterfaces[location].CommType);
                        NetworkGatewayInterfaces[location].Instance = NetworkGatewayInterfaces[location].AssemblyType.InvokeMember(String.Empty, BindingFlags.CreateInstance, null, null, null);

                    }
                    catch (Exception e)
                    {
                        _PluginCommonFunctions.GenerateErrorRecordLocalMessage(4, "NetworkGatewayInterface_" + NetworkGatewayInterfaces[location].CommType, e.Message);
                        NetworkGatewayInterfaces[location].Valid = false;
                        NetworkGatewayInterfaces[location].Active = false;
                        location++;
                        continue;
                    }


                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "DefaultWaitingSleep", out S))
                        NetworkGatewayInterfaces[location].DefaultWaitingSleep = PluginCommonFunctions.ConvertToInt32(S);
                    else
                         PluginCommonFunctions.GetStartupField("DefaultSleepTime", out NetworkGatewayInterfaces[location].DefaultWaitingSleep, 60000);

                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "DefaultSleepingCheckInterval", out S))
                        NetworkGatewayInterfaces[location].DefaultSleepingCheckInterval = PluginCommonFunctions.ConvertToInt32(S);
                    else
                        PluginCommonFunctions.GetStartupField("DefaultSleepingCheckInterval", out NetworkGatewayInterfaces[location].DefaultSleepingCheckInterval, 5000);

                    if (NetworkGatewayInterfaces[location].DefaultSleepingCheckInterval < 0)
                        NetworkGatewayInterfaces[location].DefaultSleepingCheckInterval = 100;


                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "Port", out S))
                        NetworkGatewayInterfaces[location].Port = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "Baud", out S))
                        NetworkGatewayInterfaces[location].Baud = PluginCommonFunctions.ConvertToInt32(S);
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "Data", out S))
                        NetworkGatewayInterfaces[location].Data = PluginCommonFunctions.ConvertToInt32(S);
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "SpontaniousDataEndCharsHEX", out S))
                        NetworkGatewayInterfaces[location].SpontaniousDataEndCharsHEX = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "Parity", out S))
                        NetworkGatewayInterfaces[location].Parity = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "Flow", out S))
                        NetworkGatewayInterfaces[location].Flow = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "Address", out S))
                        NetworkGatewayInterfaces[location].Address = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "SetupString", out S))
                        NetworkGatewayInterfaces[location].SetupString = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "DeviceName", out S))
                        NetworkGatewayInterfaces[location].DeviceName = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "MACAddress", out S))
                        NetworkGatewayInterfaces[location].MACAddress = S;
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "ReceiveTimeout", out S))
                        NetworkGatewayInterfaces[location].ReceiveTimeout = PluginCommonFunctions.ConvertToInt32(S);
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "TransmitTimeout", out S))
                        NetworkGatewayInterfaces[location].TransmitTimeout = PluginCommonFunctions.ConvertToInt32(S);
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "ResponseMaxWaitTime", out S))
                        NetworkGatewayInterfaces[location].ResponseMaxWaitTime = PluginCommonFunctions.ConvertToInt32(S);
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "UndesignatedMaxCharsToRead", out S))
                        NetworkGatewayInterfaces[location].UndesignatedMaxCharsToRead = PluginCommonFunctions.ConvertToInt32(S);
                
                    if (PluginCommonFunctions.FindValueInStartupInfo(DBStartupInfo[8], "IntervalLoopTime", out S))
                        NetworkGatewayInterfaces[location].LoopIntervalTime = PluginCommonFunctions.ConvertToInt32(S);


                    if (string.IsNullOrEmpty(DBStartupInfo[7])) //There is no Target DLL, so therefore it is not valid
                    {
                        location++;
                        continue;
                    }

                    SecureCommunicationIDCodes[location] = NetworkGatewayInterfaces[location].SecureCommunicationIDCode;

                    SetupInterface(location);

                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[location].AssemblyType.InvokeMember("NGWI_InitializePlugin", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[location].Instance, new object[] { CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[location].Address, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[location].Port });
                    NetworkGatewayInterfaces[location].ProcessThread = new Thread(new ThreadedDataProcessing().SingleObjectRoutineThread);
                    NetworkGatewayInterfaces[location].Valid = true;

                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();

                    PCS.Command = PluginCommandsToPlugins.RequestLink;
                    PCS.DestinationPlugin = DBStartupInfo[10];
                    PCS.DeviceUniqueID = DBStartupInfo[1];
                    PCS.Strings = DBStartupInfo;
                    PCS.String = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[location].Address;
                    PCS.String2=CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[location].Port;
                    PCS.PluginReferenceIdentifier = DBStartupInfo[1];
                    PCS.SecureCommunicationIDCode = NetworkGatewayInterfaces[location].SecureCommunicationIDCode;
                    _PCF.QueuePluginInformationToPlugin(PCS);

                    location++;
                }

                WatchdogTimer = new Timer(WatchDogNetworkChecker,null, 30000, 30000);
            }
            catch (Exception err)
            {
                string SE = err.StackTrace + err.Message;
                _PluginCommonFunctions.GenerateErrorRecord(2000000, SE + " PluginStartupCompleted ", err.Message, err);
            }
        }

        static void WatchDogNetworkChecker(object state )
        {
            try
            {
                DateTime Last = _PluginCommonFunctions.CurrentTime.AddSeconds(-30);
                for (int index = 0; index < NetworkGatewayInterfaces.Length; index++)
                {
                    if (NetworkGatewayInterfaces[index].LastActualIncomingDataTime < Last && CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].AssemblyType != null)
                    {

                        if (!(bool)CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].AssemblyType.InvokeMember("NGWI_StillConnectedToServer", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].Instance, null))
                        {
                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(7, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].Address, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].DLLToSendDataTo);
                            object[] Timeouts = new object[] { CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].ReceiveTimeout, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].TransmitTimeout };
                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].AssemblyType.InvokeMember("NGWI_ConnectToDevice", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].Instance, Timeouts);
                        }
                    }

                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }



        static void SetupInterface(int location)
        {
            if (NetworkGatewayInterfaces[location].DeviceName == "WIZ110SR") // WIZnet Serial-to-Ethernet Gateway
            {

                //TODO WIZ110SR Configuration Routine
            }
        }

        private static void ShutDownEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            for (int index = 0; index < NetworkGatewayInterfaces.Count(); index++)
            {
                try
                {
                    if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].Instance!=null)
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].AssemblyType.InvokeMember("NGWI_Close", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[index].Instance, null);
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
        }
        
        private static void PluginEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            if (WhichEvent == ServerEvents.TimeEvent)
            {
            }

            if (WhichEvent == ServerEvents.ShutDownPlugin)
            {
                ShutDownInProgress = true;

            }
        }

        private static void PluginEventHandler(ServerEvents WhichEvent)
        {

        }

        private static void HeartbeatEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void InformationCommingFromPluginPluginEventHandler(ServerEvents WhichEvent)
        {

            PluginEventArgs Value;
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();

            while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
            {

                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                int InterfaceIndex = Array.FindIndex(SecureCommunicationIDCodes, item => item == Value.PluginData.SecureCommunicationIDCode);
                if (InterfaceIndex<0) //Invalid Interface
                    continue;
                OutgoingDataStruct ODS = Value.PluginData.OutgoingDS;
                ODS.OriginalCommand = Value.PluginData.Command;
                switch (Value.PluginData.Command)
                {

                    case PluginCommandsToPlugins.ClearBufferAndProcessCommunication://Normal Command Lowest Priority except for loop
                    case PluginCommandsToPlugins.ProcessCommunicationWOClearingBuffer: //Normal Command Lowest Priority except for loop
                        NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.Enqueue(ODS);

                        //Okay, so restart the Thread so it can process this request
                        NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep =true;
                        break;


                    case PluginCommandsToPlugins.PriorityProcessNow: //This interrupts what is going on and does the command Priority #1
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.Enqueue(ODS);
                        NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep =true;
                        break;


                    case PluginCommandsToPlugins.ProcessNext: //This becomes the next command after the current command and loop is done Priority #2
                    case PluginCommandsToPlugins.ProcessCommunicationAtTime: //At a certain time Prioirty #3

                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].ProcessNextDataStructs.Push(ODS);
                        NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = true;
                        break;

                    case PluginCommandsToPlugins.DoLoopNow:  //Does loop before its time

                        if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStructActive)
                        {
                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextLoopProcessing = _PluginCommonFunctions.CurrentTime.AddSeconds(-1);

                            //Okay, so restart the Thread so it can process this request
                            NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = true;
                        }
                        else
                            NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = false;
                        break;

                    
                    case PluginCommandsToPlugins.WaitOnIncomingData://Also known as spontanious or streaming Data
                
//                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStruct.OriginalCommand = Value.PluginData.Command;
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStruct = ODS;
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructActive = true;
                        
                        //Setup Sleep Time Between Looks for incoming Data
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructSleepTime=CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStruct.SpontaniousData_SleepInterval;
                        if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructSleepTime==0)
                            PluginCommonFunctions.GetStartupField("DefaultWaitOnIncomingDataSleepTime", out CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructSleepTime, 100);

                        //Okay, so restart the Thread so it can process this request
                        NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = true;
                        break;
                
                    case PluginCommandsToPlugins.StopWaitOnIncomingData:
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructActive = false;
                        break;
                
                    case PluginCommandsToPlugins.StartTimedLoopForData:  //THis is lowest priority, for routine, non-time critical data monitoring
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStruct = ODS;
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextLoopProcessing=_PluginCommonFunctions.CurrentTime.AddSeconds(-1);
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStructActive = true;
                        NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = true;
                        break;

                    case PluginCommandsToPlugins.EndTimedLoopForData:
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStructActive = false;
                        NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = false;
                        break;

                    case PluginCommandsToPlugins.ChangeIntervalLoopTime:
                        NetworkGatewayInterfaces[InterfaceIndex].LoopIntervalTime = Value.PluginData.Integer;
                        break;

                    case PluginCommandsToPlugins.LinkAccepted:
                    case PluginCommandsToPlugins.LinkRejected:
                        if (Value.PluginData.Command == PluginCommandsToPlugins.LinkRejected)
                        {
                            NetworkGatewayInterfaces[InterfaceIndex].Active = false;
                        }

                        NetworkGatewayInterfaces[InterfaceIndex].UnattachedStoredIncommingData.Clear();
                        OutgoingDataStruct c;
                        while (NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.TryDequeue(out c))
                        {
                        }
                        NetworkGatewayInterfaces[InterfaceIndex].TotalCharactersRead = 0;
                        NetworkGatewayInterfaces[InterfaceIndex].TotalCharactersWritten = 0;

                        if (Value.PluginData.Command == PluginCommandsToPlugins.LinkAccepted)
                        {
                            NetworkGatewayInterfaces[InterfaceIndex].DLLToSendDataTo = Value.PluginData.OriginPlugin;
                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].ThisDLLServerKey = Value.PluginData.DestinationPlugin;
                            NetworkGatewayInterfaces[InterfaceIndex].Active = true;

                            //Okay, so restart the thread so it can process this request
                            if (!NetworkGatewayInterfaces[InterfaceIndex].ProcessThread.IsAlive) //Thread has never been started
                                NetworkGatewayInterfaces[InterfaceIndex].ProcessThread.Start(InterfaceIndex);
                            NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = true;
                                
                            PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                            PCS.Command = PluginCommandsToPlugins.LinkedCommReady;
                            PCS.DestinationPlugin = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[10];
                            if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[0] == "Interfaces")
                            {
                                if (string.IsNullOrEmpty(PCS.DeviceUniqueID))
                                    PCS.DeviceUniqueID = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[1];
                            }
                            PCS.PluginReferenceIdentifier = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[1];
                            PCS.SecureCommunicationIDCode = NetworkGatewayInterfaces[InterfaceIndex].SecureCommunicationIDCode;
                            _PCF.QueuePluginInformationToPlugin(PCS);

                        }
                        break;

                }


                if (NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep && !CHMModules.NetworkGatewayInterface.ShutDownInProgress)
                {
                //    if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LockingSemaphore.CurrentCount > 0)
                //        NetworkGatewayInterfaces[InterfaceIndex].ProcessThread.Interrupt();
                }

            }
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Release();
        }

        private static void CurrentServerStatusChangeEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }
    }
}

#region Optional User Classes

class ThreadedDataProcessing
{

    internal void SingleObjectRoutineThread(object DownInterfaceIndex)
    {
        OutgoingDataStruct OutgoingData = new OutgoingDataStruct();

  
        int CurrentTrack=0;
        int CurrentCommDataControlInfoIndex=0;
        DateTime TimeBeforeLastSleep, WakeupTime, CurrentTime;
        List<Byte> IncomingData = new List<Byte>();
        int MaxReadSize=0;
        bool SomthingToProcess, LoadedNewIPRequest;

        _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

   
        int InterfaceIndex = (int)DownInterfaceIndex;
  
        if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Active == false)
            return;

        while (true)
        {
            if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LockingSemaphore.CurrentCount > 0)
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LockingSemaphore.Wait();
            try
            {
                if (CHMModules.NetworkGatewayInterface.ShutDownInProgress)
                {
                    try
                    {
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_Close", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, null);
                    }
                    catch (Exception CHMAPIEx)
                    {
                        _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    }
                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Active = false;
                    return;
                }


                try
                {
                    if (!(bool)CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_IsConnectedToDevice", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, null))
                    {
                        object[] Timeouts = new object[] { CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].ReceiveTimeout, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].TransmitTimeout };
                        if (!(bool)CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_ConnectToDevice", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, Timeouts))
                        {
                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].SentNoConnectErrorCount++;
                            Exception e=(Exception)CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_GetLastError", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, null);
                            string ie = "";
                            if (e.InnerException!=null)
                                ie = e.InnerException.ToString();

                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(6, e.Message, ie + " " + e.StackTrace);
                            Thread.Sleep(CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NoConnectRetryTime);
                            continue;
                        }
                        else
                        {
                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].SentNoConnectError = false;
                            _PluginCommonFunctions.GenerateLocalMessage(2, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Address, "");
                        }
                    }
                }
                catch (Exception e)
                {
                    string ie = "";
                    if (e.InnerException!=null)
                        ie = e.InnerException.ToString();

                    _PluginCommonFunctions.GenerateErrorRecordLocalMessage(5, e.Message, ie + " " + e.StackTrace);

                }

                //Now We Actually do a Transaction :<))                                
                try
                {
                    while (true) //Process Transactions
                    {

                        CurrentTime = _PluginCommonFunctions.CurrentTime;
                        SomthingToProcess = false;
                        LoadedNewIPRequest = false;

                        
                        if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)  //First Priority-Do Now
                        {
                            SomthingToProcess = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.TryDequeue(out OutgoingData);
                            if (!SomthingToProcess)
                                continue;
                        }
                        else //No do Now Transactions
                        {
                            //Second Priority Do next
                            if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].ProcessNextDataStructs.IsEmpty)  
                            {
                                SomthingToProcess=CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].ProcessNextDataStructs.TryPop(out OutgoingData);
                                if(SomthingToProcess)
                                {
                                    if(OutgoingData.OriginalCommand==PluginCommandsToPlugins.ProcessCommunicationAtTime)//Add to At Time List
                                    {
                                        CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct DDTS;
                                        DDTS.OutgoingData = OutgoingData.DeepCopy();
                                        DDTS.DateTimeToExecute =OutgoingData.ProcessCommunicationAtTimeTime;
                                        if(DDTS.DateTimeToExecute==DateTime.MinValue)
                                            DDTS.DateTimeToExecute= _PluginCommonFunctions.CurrentTime.AddSeconds(OutgoingData.SecondsBetweenProcessCommunicationAtTime);
                                        DDTS.OutgoingData.LastTransactionCompleted = DDTS.DateTimeToExecute.AddSeconds(-OutgoingData.SecondsBetweenProcessCommunicationAtTime);
                                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions.Add(DDTS);
                                        IComparer<CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct> comparer = new ThreadedDataProcessing.DelayedDataTransactionsOrderingClass();
                                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions.Sort(comparer);
                                        continue; //Restart Loop and Try Again
                                    }
                                }
                            }
                            else
                            {
                                //Third Priority-CommunicationAtTime
                                if (!SomthingToProcess && CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions.Count > 0) 
                                {
                                    if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions[0].DateTimeToExecute <= CurrentTime)
                                    {
                                        CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct DDTS = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions[0];
                                        OutgoingData = DDTS.OutgoingData.DeepCopy();
                                        if (OutgoingData.NumberOfTimesToProcessCommunicationAtTime <= 1) //We Are Done With This Command
                                        {
                                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions.RemoveAt(0);
                                            SomthingToProcess = true;
                                        }
                                        else  //Setup to Process This Command Again
                                        {
                                            if (DDTS.OutgoingData.NumberOfTimesToProcessCommunicationAtTime<int.MaxValue)
                                                DDTS.OutgoingData.NumberOfTimesToProcessCommunicationAtTime--;
                                            DDTS.DateTimeToExecute = DDTS.OutgoingData.LastTransactionCompleted.AddSeconds(OutgoingData.SecondsBetweenProcessCommunicationAtTime);
                                            while (DDTS.DateTimeToExecute < CurrentTime)
                                                DDTS.DateTimeToExecute =DDTS.DateTimeToExecute.AddSeconds(OutgoingData.SecondsBetweenProcessCommunicationAtTime);
                                            DDTS.OutgoingData.LastTransactionCompleted = DDTS.DateTimeToExecute.AddSeconds(-OutgoingData.SecondsBetweenProcessCommunicationAtTime);
                                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions[0] = DDTS;
                                            IComparer<CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct> comparer = new ThreadedDataProcessing.DelayedDataTransactionsOrderingClass();
                                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions.Sort(comparer);
                                            SomthingToProcess = true;
                                        }
                                    }
                                    ;
                                }

                                //Fourth Prioirty-Pending Transactions
                                if (!SomthingToProcess && !CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.IsEmpty)  
                                {
                                    if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructActive) //If Waiting on incoming Data then only peek
                                        LoadedNewIPRequest = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.TryPeek(out OutgoingData);//Dequeue Outgoing Data Loop
                                    else  //try DeQueue
                                        LoadedNewIPRequest = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.TryDequeue(out OutgoingData);//Dequeue Outgoing Data Loop

                                    if (LoadedNewIPRequest && OutgoingData.OriginalCommand == PluginCommandsToPlugins.ProcessCommunicationWOClearingBuffer)//We don't need to clear the buffer so we need to pop it out!
                                        LoadedNewIPRequest = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.TryDequeue(out OutgoingData);//Dequeue Outgoing Data Loop
                                    SomthingToProcess = LoadedNewIPRequest;
                                }

                                //Next to Last Priority-Timed Loop
                                if (!SomthingToProcess && CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStructActive && CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextLoopProcessing <= CurrentTime)
                                {
                                    OutgoingData = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStruct.DeepCopy();
                                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastLoopProcessing = CurrentTime;
                                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextLoopProcessing = CurrentTime.AddMilliseconds(CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIntervalTime);
                                    SomthingToProcess = true;
                                }

                                //Last Priority-Wait on Incoming Data
                                if (!SomthingToProcess && CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructActive)
                                {
                                    OutgoingData = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStruct.DeepCopy();
                                    SomthingToProcess = true;
                                }

                                //Finally if not WaitOnIncomingDataStruct then we end this and go back to sleep
                                if (!SomthingToProcess && !CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructActive) //Not Waiting and No Incomming Requests
                                    break;
                            }
                        }
                        OutgoingData.TransactionStart = _PluginCommonFunctions.CurrentTime;
                        OutgoingData.StartofCurrentTransaction = _PluginCommonFunctions.CurrentTime;
                        OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionRunning;
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].SentNoConnectError = false;
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].SentNoConnectErrorCount = 0;
                        CurrentTrack = 0;
                        CurrentCommDataControlInfoIndex = 0;
                        OutgoingData.LastDataReceived = _PluginCommonFunctions.CurrentTime;
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastCharacersReceivedTime = OutgoingData.LastDataReceived;

                        //Clear the incomming Stream
                        if (OutgoingData.OriginalCommand != PluginCommandsToPlugins.ProcessCommunicationWOClearingBuffer && OutgoingData.OriginalCommand != PluginCommandsToPlugins.WaitOnIncomingData)
                        {
                            try
                            {
                                bool flag = (bool)CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_ClearIncommingStream", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, null);
                            }
                            catch (Exception CHMAPIEx)
                            {
                                  _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                            }
                            IncomingData.Clear();
                        }

                        if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                        {
                            break;
                        }

                        while (CurrentCommDataControlInfoIndex < OutgoingData.CommDataControlInfo.Length) //Process Loop
                        {
                            if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                            {
                                break;
                            }

                            if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Track != CurrentTrack 
                                ||OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Type != CommDataControlInfoStruct_CommDataControlInfoType.NormalRecord)
                            {
                                CurrentCommDataControlInfoIndex++;
                                continue;
                            }

                            //Define Max Read For This Part of the Read
                            if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor > 0)
                                MaxReadSize = (int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor;
                            else
                                CHMModules.NetworkGatewayInterface.PluginCommonFunctions.GetStartupField("MaxCharToRead", out MaxReadSize, 9999);

                            //Some Setup Stuff

                            if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.Unknown)
                            {
                                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor > 0)
                                {
                                    OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.SpecificLength;
                                }
                                else
                                {
                                    if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ResponseToWaitFor != null)
                                    {
                                        OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.SpecificCharacters;
                                    }
                                    else
                                    {
                                        OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Nothing;
                                    }
                                }
                            }

                            //Step One-Transmit the Initiator 
                            if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CharactersToSend != null)
                            {
                                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].TransmitDelayMiliseconds > 0)
                                    Thread.Sleep((int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].TransmitDelayMiliseconds);

                                try
                                {
                                    object[] WriteCharsParams = new Object[] { OutgoingData, CurrentCommDataControlInfoIndex };
                                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_WriteChars", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, WriteCharsParams);
                                    OutgoingData = (OutgoingDataStruct)WriteCharsParams[0];
                                }
                                catch (Exception CHMAPIEx)
                                {
     
                                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                                
                                   OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionError;
                                    break;                                  
                                }

                                if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                                {
                                    break;
                                }
                                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastCharactersSentTime = OutgoingData.LastDataSent;
                            }

                            //Step Two-Read What is there
                            if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.Nothing)
                            {
                                CurrentTrack = (int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].NextTrack;
                                CurrentCommDataControlInfoIndex++;
                            }
                            else
                            {
                            ReadData:
                                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReceiveDelayMiliseconds > 0)
                                    Thread.Sleep((int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReceiveDelayMiliseconds);
                                int ReturnFlag = -1;
                                try
                                {
                                    object[] ReadCharsParams = new Object[] { OutgoingData, IncomingData, MaxReadSize, CurrentCommDataControlInfoIndex };
                                    ReturnFlag = (int)CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_ReadChars", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, ReadCharsParams);
                                    OutgoingData = (OutgoingDataStruct)ReadCharsParams[0];
                                    IncomingData = (List<Byte>)ReadCharsParams[1];
                                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastCharacersReceivedTime = OutgoingData.LastDataReceived;
                                }
                                catch (Exception CHMAPIEx)
                                {
                                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                                    ReturnFlag = -1;
                                }
                                bool ContinueProcessing = true;
                                switch (ReturnFlag)
                                {
                                    case -1: //Error COndition
                                        OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionError;
                                        ContinueProcessing = false;
                                        break;                                  
                                    case 0: //Continue Processing
                                        ContinueProcessing = true;
                                        break;
                                    case 1: //Return with Transaction Succeeded
                                        OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionComplete;
                                        ContinueProcessing = false;
                                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastActualIncomingDataTime = CurrentTime;
                                        break;
                                    case 2://Return Immediatly with Transaction Failed
                                        OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionFailed;
                                        ContinueProcessing = false;
                                        break;
                                    case 3: //Go To Next Comm Control Index
                                        CurrentCommDataControlInfoIndex++;
                                        ContinueProcessing = false;
                                        continue;
                                }

                                if (!ContinueProcessing)
                                    break;
                                
                                
                                if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                                {
                                    break;
                                }

                            //Now we see if it matches  
                                if (OutgoingData.OriginalCommand == PluginCommandsToPlugins.WaitOnIncomingData && IncomingData.Count == 0) //No Spontanious Data
                                {
                                    OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.NoSpontaniousDataReceived;
                                    break;
                                }
                                if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                                {
                                    break;
                                }

                            CheckIncomingData:
                                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.SpecificLength && IncomingData.Count < OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor)
                                {
                                    if ((_PluginCommonFunctions.CurrentTime - OutgoingData.LastDataReceived).TotalMilliseconds <= OutgoingData.MaxMilisecondsToWaitForIncommingData)
                                        goto ReadData;
                                    OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionFailed;
                                }

                                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.SpecificLength && IncomingData.Count >= OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor)
                                {
                                    try
                                    {
                                        OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ActualResponseReceived = IncomingData.GetRange(0, (int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor).ToArray();
                                        IncomingData.RemoveRange(0, (int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ReponseSizeToWaitFor);
                                    }
                                    catch
                                    {
                                        IncomingData.Clear();
                                    }
                                }

                                if(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.Unknown 
                                    || OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.Anything)
                                {
                                    try
                                    {
                                        OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ActualResponseReceived = IncomingData.ToArray();
                                        IncomingData.Clear();
                                    }
                                    catch
                                    {
                                        IncomingData.Clear();
                                    }
                                }

                                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType == CommDataControlInfoStruct_WhatToWaitFor.SpecificCharacters)
                                {
                                    bool flag = false;
                                    try
                                    {
                                        for (int i = 0, p = 0; i < IncomingData.Count; i++)
                                        {
                                            if (IncomingData[i] == OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ResponseToWaitFor[0])
                                            {
                                                if (i + OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ResponseToWaitFor.Length > IncomingData.Count)
                                                    break;
                                                flag = true;
                                                for (p = 0; p < OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ResponseToWaitFor.Length; p++)
                                                {
                                                    if (IncomingData[i + p] != OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ResponseToWaitFor[p])
                                                    {
                                                        flag = false;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (flag == true)
                                            {
                                                try
                                                {
                                                    //IncomingData.RemoveRange(0, OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ResponseToWaitFor.Length);
                                                    OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ActualResponseReceived = IncomingData.GetRange(0, i + p).ToArray();
                                                    IncomingData.RemoveRange(0, i + p);
                                                }
                                                catch
                                                {
                                                    IncomingData.Clear();
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }

                                    if (!flag)
                                    {
                                        if ((_PluginCommonFunctions.CurrentTime - OutgoingData.LastDataReceived).TotalMilliseconds < OutgoingData.MaxMilisecondsToWaitForIncommingData)
                                            goto ReadData;

                                        if (OutgoingData.OriginalCommand == PluginCommandsToPlugins.WaitOnIncomingData) //No Spontanious Data
                                        {
                                            OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.NoSpontaniousDataReceived;
                                        }
                                        else
                                        {
                                            OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionFailed;
                                        }
                                    }
                                }


                                if (OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionFailed)
                                {
                                    //Check if next record is Alternative Response;
                                    int i = 1;
                                    while (CurrentCommDataControlInfoIndex + i < OutgoingData.CommDataControlInfo.Length && OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex + i].Type == CommDataControlInfoStruct_CommDataControlInfoType.AlternativeResponse)
                                    {
                                        if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex + i].Track == CurrentTrack)
                                        {
                                            CurrentCommDataControlInfoIndex = CurrentCommDataControlInfoIndex + i;
                                            goto CheckIncomingData;
                                        }
                                        i++;
                                    }
                                    //I Guess Not, so we got a failure
                                    CurrentCommDataControlInfoIndex++; //When we are finished, the results routine expects the CurrentCommDataControlInfoIndex to be one Greater than the end
                                    break;//Drops us Out of the Process Loop To the Dequeue Outgoing DataLoop

                                }
                                //If We are here, so far so good!
                                CurrentTrack = (int)OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].NextTrack;
                                CurrentCommDataControlInfoIndex++;
                            }



                        }
                        if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                        {
                            break;
                        }

                        if (OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.NoSpontaniousDataReceived)  //We are out of here, No Spontanious Data
                            break;


                        //Send The Results Back to the Calling Plugin
                        if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty)
                        {
                            break;
                        }
                        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                        if (OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionFailed
                            || OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionError
                            || OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionAborted
                            || OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionIOError)
                        {
                            PCS2.Command = PluginCommandsToPlugins.TransactionFailed;
                        }

                        if (OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionComplete)
                        {
                            PCS2.Command = PluginCommandsToPlugins.TransactionComplete;
                        }

                        if (OutgoingData.Status == OutgoingDataStruct_StatusOfTransaction.TransactionRunning)
                        {
                            if (OutgoingData.OriginalCommand == PluginCommandsToPlugins.WaitOnIncomingData)
                            {
                                OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.SpontaniousDataReceived;
                                PCS2.Command = PluginCommandsToPlugins.SpontaniousDataReceived;
                            }
                            else
                            {
                                OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionComplete;
                                PCS2.Command = PluginCommandsToPlugins.TransactionComplete;

                            }
                        }
                        OutgoingData.DebugFinalCommDataControlInfoIndex = CurrentCommDataControlInfoIndex - 1;
                        OutgoingData.DebugFinalTrack = CurrentTrack;
                        if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[0] == "Interfaces")
                        {
                            if (string.IsNullOrEmpty(PCS2.DeviceUniqueID))
                                PCS2.DeviceUniqueID = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[1];
                        }
                        PCS2.DestinationPlugin = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[10];
                        PCS2.OriginPlugin = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].UniqueID;
                        PCS2.OutgoingDS = OutgoingData.DeepCopy();
                        PCS2.PluginReferenceIdentifier = OutgoingData.RequestUniqueIDCode;
                        _PCF.QueuePluginInformationToPlugin(PCS2);

                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(IOException))
                    {
                        try
                        {
                            CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_Close", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, null);
                            _PluginCommonFunctions.GenerateErrorRecord(2000000, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DLLToSendDataTo + " IO Error Read Data", e.Message, e);
                        }
                        catch (Exception CHMAPIEx)
                        {
                             _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                        }

                    }

                    //Send The Results Back to the Calling Plugin
                    OutgoingData.Status = OutgoingDataStruct_StatusOfTransaction.TransactionIOError;
                    OutgoingData.DebugFinalCommDataControlInfoIndex = CurrentCommDataControlInfoIndex;
                    OutgoingData.DebugFinalTrack = CurrentTrack;
                    OutgoingData.Except = e;
                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    PCS.Command = PluginCommandsToPlugins.DataLinkLost;
                    if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[0] == "Interfaces")
                    {
                        if (string.IsNullOrEmpty(PCS.DeviceUniqueID))
                            PCS.DeviceUniqueID = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[1];
                    }
                    PCS.DestinationPlugin = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DBInformation[10];
                    PCS.OriginPlugin = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].UniqueID;
                    PCS.OutgoingDS = OutgoingData.DeepCopy();
                    PCS.PluginReferenceIdentifier = OutgoingData.RequestUniqueIDCode;
                    _PCF.QueuePluginInformationToPlugin(PCS);
                    _PluginCommonFunctions.GenerateErrorRecord(2000000, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DLLToSendDataTo, e.Message, e);

                    continue;
                }



                //Put Thread to Sleep to await the next action required
                if (!CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].PriorityDoNow.IsEmpty 
                    || !CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].ProcessNextDataStructs.IsEmpty
                    || !CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].StoredOutgoingDataStructs.IsEmpty)
                {
                    continue;
                }


                if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LockingSemaphore.CurrentCount==0)
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LockingSemaphore.Release();

                TimeBeforeLastSleep = _PluginCommonFunctions.CurrentTime;
                if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructActive)
                {
                //Wait on Incoming Data Sleep Time
                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime = TimeBeforeLastSleep.AddMilliseconds(CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].WaitOnIncomingDataStructSleepTime);
                }
                else
                {
                //Default Sleep Time
                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime = TimeBeforeLastSleep.AddMilliseconds(CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DefaultWaitingSleep);
                }

                //Check if Loop is next Item
                if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LoopIncomingDataStructActive)//Timed Loop
                {
                    if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextLoopProcessing < CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime)
                        CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextLoopProcessing;
                }
                
                
                //Check if AtTime is next
                if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions.Count > 0
                    && CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions[0].DateTimeToExecute<CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime)
                {
                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime = CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DelayedDataTransactions[0].DateTimeToExecute;
                }
                
                //Now go to Sleep
                TimeSpan span=CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime - _PluginCommonFunctions.CurrentTime;
                int ActualSleepMiliseconds = 0;
                if (span.TotalMilliseconds>1)
                {
                    CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].CurrentSleepMiliseconds = (int)span.TotalMilliseconds;

                    int slptime, sleepcount= CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].CurrentSleepMiliseconds;
                    while (sleepcount > 0)
                    {
                        slptime = Math.Min(sleepcount, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].DefaultSleepingCheckInterval);
                        ActualSleepMiliseconds += slptime;
                        Thread.Sleep(slptime);
                        sleepcount -= slptime;
                        if (CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep)
                            break;
                    }

                    
                }
 
                //Goodmorning, I have just woken up
                WakeupTime = _PluginCommonFunctions.CurrentTime;
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastWakeupTime = WakeupTime;
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].NextWakeupTime=DateTime.MinValue;
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].LastSleepMiliseconds = ActualSleepMiliseconds;
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].InterruptSleep = false;
            }

            catch (ThreadInterruptedException e) //This is used to wake up the thread early
            {
                continue;
            }
            catch (ThreadAbortException e) //This shuts down the thread (either a shutdown command or a unlink command
            {
                Thread.ResetAbort();
                //Shut It Down
                CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].AssemblyType.InvokeMember("NGWI_Close", BindingFlags.InvokeMethod, null, CHMModules.NetworkGatewayInterface.NetworkGatewayInterfaces[InterfaceIndex].Instance, null);
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }
    }

    public class DelayedDataTransactionsOrderingClass : IComparer<CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct>
    {
        public int Compare(CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct x, CHMModules.NetworkGatewayInterface.DelayedDataTransactionStruct y)
        {
            int compareDate = x.DateTimeToExecute.CompareTo(y.DateTimeToExecute);
            return compareDate;
        }
    }

}




#endregion Optional User Classes
        #endregion Optional User Routines


        #endregion Optional User Stuff


