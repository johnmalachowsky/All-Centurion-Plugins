
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
using WebSocketSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml.Linq;
using System.Text.RegularExpressions;


//Required Parameters
//  UpdateInterval (In Milliseconds, default is 2500)

namespace CHMModules
{


    public class UniversalDevicesInterface
    {

        private static _PluginCommonFunctions PluginCommonFunctions;

        private static string LinkPlugin;
        private static string LinkPluginReferenceIdentifier;
        private static string LinkPluginSecureCommunicationIDCode;

        private static SortedList<string, string> ISYFamilyInformation;
        private static SortedList<string, string> ISYDeviceInformation;

        private static ConcurrentQueue<string> AquiredAddresses;
        private static string UnKnownCatagory;

        //private static ConcurrentQueue<string> SubscribedInfo;
        private static SemaphoreSlim SubscribedInfoSlim;


        internal static WebSocket client;
        private static string  Address;
        private static string Port;
        private static string Password;
        private static string Username;
        private static String EncodedPassword;

        private static string Configuration;
        private static string Status;
        private static string AllNodes;

        private static bool LinkedCommReady = false;
        private static bool ISYFirstInit = false;

        private static SemaphoreSlim CheckForNodeListSlim;
        private static SemaphoreSlim ProcessNodeInformationSlim;

        private static ConcurrentDictionary<string, Tuple<DateTime, string, int>> SentCommands;
        private static int ISYResendTime;
        private static int ISYMaxTimesToResend;


        static internal string ISYDeviceInformationKey(string V1, string V2)
        {
            return (string.Format("{0,3}{1,3}", V1, V2));

        }

        static internal string ISYFamilyInformationKey(string[] Values)
        {
            return (string.Format("{0,3}{1,3}{2,3}", Values[0], Values[3], Values[5]));

        }

        static internal string ISYFamilyInformationKey(string V1, string V2, string V3)
        {
            return (string.Format("{0,3}{1,3}{2,3}", V1, V2, V3));

        }

        public void PluginInitialize(int UniqueID)
        {

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            
            ServerAccessFunctions.PluginDescription = "Universal Devices Interface";
            ServerAccessFunctions.PluginSerialNumber = "00001-00015";
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
            ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
             ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;

            AquiredAddresses = new ConcurrentQueue<string>();
            CheckForNodeListSlim = new SemaphoreSlim(1,1);
            //SubscribedInfo = new ConcurrentQueue<string>();
            SubscribedInfoSlim = new SemaphoreSlim(1, 1);
            ProcessNodeInformationSlim = new SemaphoreSlim(1, 1);
            SentCommands = new ConcurrentDictionary<string, Tuple<DateTime, string, int>>();
 

        }

         private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
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

            PasswordStruct Pw= new PasswordStruct();

            _PCF.GetPasswordInfo("", "", ref Pw);

            Password = Pw.Password;
            Username = Pw.Account;
            EncodedPassword = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password));


            string S;
            ISYDeviceInformation = new SortedList<string, string>();
            ISYResendTime = _PCF.GetStartupField("ISYResendTime", 5);
            ISYMaxTimesToResend = _PCF.GetStartupField("ISYMaxTimesToResend", 10);

            _PCF.GetStartupField("ISYDeviceDataCodes", out S);
            try
            {
                string[] FI = S.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string SS in FI)
                {
                    try
                    {
                        string[] X = SS.Split(new char[] { ',' }, StringSplitOptions.None);
                        ISYDeviceInformation.Add(ISYDeviceInformationKey(X[0],X[1]), X[2]);
                    }
                    catch
                    {


                    }
                }
            }
            catch
            {

            }






            ISYFamilyInformation = new SortedList<string, string>();
            _PCF.GetStartupField("ISYFamilyInformation", out S);
            try
            {
                string[] FI = S.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string SS in FI)
                {
                    try
                    {
                        string[] X = SS.Split(new char[] { ',' }, StringSplitOptions.None);
                        ISYFamilyInformation.Add(ISYFamilyInformationKey(X), X[6]);
                    }
                    catch
                    {


                    }
                }
            }
            catch
            {

            }
            return;
        }

        private static void FlagCommingServerEventHandler(ServerEvents WhichEvent)
        {
        }
    

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            if (!ISYFirstInit)
            {
                if(LinkedCommReady && !string.IsNullOrEmpty(EncodedPassword))
                {
                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                    PCS2.DestinationPlugin = LinkPlugin;
                    PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                    PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                    PCS2.OutgoingDS  = new OutgoingDataStruct();
                    PCS2.OutgoingDS.LocalIDTag = "System Config";
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/rest/config");
                    PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                    PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                    PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                    PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                    SentCommands.TryAdd("http://$$IPAddress/rest/config", new Tuple<DateTime, String,int>(_PluginCommonFunctions.CurrentTime, PCS2.OutgoingDS.LocalIDTag,0));
                    _PCF.QueuePluginInformationToPlugin(PCS2);
                    ISYFirstInit = true;

                }

            }
            else //Has the Link to the ISY Expired/Died && Check for Re-sends
            {
                var diffInSeconds = (_PluginCommonFunctions.CurrentTime - WebSocketSharp_LastHeartbeat).TotalSeconds;
                if(diffInSeconds> _PCF.GetStartupField("MaxHeartbeatTime", 120))
                {
                    _PluginCommonFunctions.GenerateErrorRecordLocalMessage(2000010, "", WebSocketSharp_LastHeartbeat.ToShortDateString()+" "+ WebSocketSharp_LastHeartbeat.ToLongTimeString());
                    WebSocketSharp_LastHeartbeat = _PluginCommonFunctions.CurrentTime;
                    WebSocketSharp_Close();
                    WebSocketSharp_DoStuff();
                }

                if(Value.HeartBeatTC!= HeartbeatTimeCode.Nothing)
                {
                    foreach (var Commands in SentCommands)
                    {
                        if (Value.DateValue>Commands.Value.Item1.AddSeconds(ISYResendTime))
                        {
                            Tuple<DateTime, String, int> VX;

                            if (!SentCommands.TryRemove(Commands.Key, out VX))
                            {
                                VX = new Tuple<DateTime, string, int>(_PluginCommonFunctions.CurrentTime, "", 0);
                            }

                            if (VX.Item3 > ISYMaxTimesToResend)
                            {
                                _PluginCommonFunctions.GenerateLocalMessage(2000013, "", Commands.Key);
                            }
                            else
                            {
                                PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                PCS2.Command = PluginCommandsToPlugins.PriorityProcessNow;
                                PCS2.DestinationPlugin = LinkPlugin;
                                PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                PCS2.OutgoingDS = new OutgoingDataStruct();
                                //                   PCSAquired.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                PCS2.OutgoingDS.LocalIDTag = Commands.Value.Item2;
                                PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(Commands.Key);
                                PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                                PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                                SentCommands.TryAdd(Commands.Key, new Tuple<DateTime, String, int>(_PluginCommonFunctions.CurrentTime, PCS2.OutgoingDS.LocalIDTag, VX.Item3 + 1));
                                _PCF.QueuePluginInformationToPlugin(PCS2);
                                _PluginCommonFunctions.GenerateLocalMessage(2000012, "", Commands.Key);

                                break;
                            }
                        }
                    }



                }
            }

 //Regular Heart Beat Processes
            CheckForNodeList();
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
                        if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionComplete)
                        {
                            OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS;

                            //Debug.WriteLine("TransComplete: " + ODS.LocalIDTag+" " + System.Text.Encoding.Default.GetString(Value.PluginData.OutgoingDS.CommDataControlInfo[0].CharactersToSend));

                            if (ODS.CommDataControlInfo[0].ActualResponseReceived == null) //Not Valid Data
                            {
                                continue;
                            }
                            string sXYY = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].CharactersToSend);
                            Tuple<DateTime, String, int> Tpl;
                            SentCommands.TryRemove(sXYY, out Tpl);
                            switch (ODS.LocalIDTag)
                            {
                                case "Command":
                                    if (!string.IsNullOrEmpty(ODS.LocalData))
                                    {
                                        PluginCommunicationStruct PCS3 = new PluginCommunicationStruct();
                                        PCS3.DestinationPlugin = LinkPlugin;
                                        PCS3.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                        PCS3.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                        PCS3.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
                                        PCS3.OutgoingDS = new OutgoingDataStruct();
                                        PCS3.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                        PCS3.OutgoingDS.LocalIDTag = "Query";
                                        PCS3.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                        PCS3.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(ODS.LocalData);
                                        PCS3.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                        PCS3.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                        PCS3.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                                        PCS3.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                                    //    int i = _PCF.ConvertToInt32(ODS.LocalData2);
                                    //    if (i >= 0) //-1 means no query needed (Insteon Devices)
                                    //    {
                                    //        PCS3.OutgoingDS.ProcessCommunicationAtTimeTime = _PluginCommonFunctions.CurrentTime.AddSeconds(_PCF.ConvertToInt32(ODS.LocalData2));
                                    //        _PCF.QueuePluginInformationToPlugin(PCS3);
                                    //        SentCommands.TryAdd(ODS.LocalData, new Tuple<DateTime, String, int>(_PluginCommonFunctions.CurrentTime, PCS3.OutgoingDS.LocalIDTag,0)); 
                                    //    }
                                    }
                                    break;

                                case "System Config":
                                    Configuration = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived);

                                    PluginCommunicationStruct PCSConfig = new PluginCommunicationStruct();
                                    PCSConfig.DestinationPlugin = LinkPlugin;
                                    PCSConfig.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                    PCSConfig.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                    PCSConfig.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                                    PCSConfig.OutgoingDS = new OutgoingDataStruct();
                                    PCSConfig.OutgoingDS.LocalIDTag = "All Nodes";
                                    PCSConfig.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                    PCSConfig.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/rest/nodes/devices");
                                    PCSConfig.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                    PCSConfig.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                    PCSConfig.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                                    PCSConfig.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                                    SentCommands.TryAdd("http://$$IPAddress/rest/nodes/devices", new Tuple<DateTime, String, int>(_PluginCommonFunctions.CurrentTime, PCSConfig.OutgoingDS.LocalIDTag,0));

                                    _PCF.QueuePluginInformationToPlugin(PCSConfig);
                                    WebSocketSharp_DoStuff();
                                    XElement XMLConfig = XElement.Parse(Configuration);

                                    XElement Controls = XMLConfig.Element("controls");

                                    foreach (XElement x in Controls.Elements("control"))
                                    {
                                        string s = x.ToString();
                                    }
                                    break;

                                case "All Nodes":
                                    AllNodes = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived);

                                    XElement AllNodesXMLNodes = XElement.Parse(AllNodes);

                                    foreach (XElement x in AllNodesXMLNodes.Elements("node"))
                                    {
                                        ProcessNodeInformation("All Nodes", "<nodeInfo>" + x.ToString() + "</nodeInfo>", Value.PluginData.DeviceUniqueID);
                                    }
                                    CheckForNodeList();
                                    break;


                                case "Node":
                                    ProcessNodeInformation("Node", _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived), Value.PluginData.DeviceUniqueID);
                                    CheckForNodeList();
                                    XMLDeviceScripts.StartMaintenance();
                                    break;

                                case "Maintenance Processed":
                                    XMLDeviceScripts.UseNormalMaintenanceTime();
                                    break;
                            }
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


                            LinkPlugin = Value.PluginData.OriginPlugin;
                            LinkPluginReferenceIdentifier = Value.PluginData.PluginReferenceIdentifier;
                            LinkPluginSecureCommunicationIDCode = Value.PluginData.SecureCommunicationIDCode;
                            Address = Value.PluginData.String;
                            Port = Value.PluginData.String2;

                            continue;
                        }


                        if (Value.PluginData.Command == PluginCommandsToPlugins.LinkedCommReady)
                        {
                            LinkedCommReady = true;
                            continue;
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

                        if(Value.PluginData.Command == PluginCommandsToPlugins.ProcessCommandWords || Value.PluginData.Command == PluginCommandsToPlugins.DirectCommand)
                        {
                            PluginCommunicationStruct PCS = Value.PluginData;
                            DeviceStruct DV;
                            //Debug.WriteLine("command: " + PCS.DeviceUniqueID+" "+ PCS.String2);

                            XMLDeviceScripts XMLScripts = new XMLDeviceScripts();
                            MaintenanceStruct MA = null;
                            XMLScripts.ProcessXMLMaintanenceInformation(ref MA, "", "",MaintanenceCommands.SkipOneMaintenanceCycle);

                            if (_PluginCommonFunctions.LocalDevicesByUnique.TryGetValue(PCS.DeviceUniqueID, out DV))
                            {
                                try
                                {
                                    bool Doit = false;
                                    string QueryDelay = "";
                                    string CommandString = "";

                                    if ((string.IsNullOrEmpty(PCS.String2) && !string.IsNullOrEmpty(PCS.String3) || Value.PluginData.Command == PluginCommandsToPlugins.DirectCommand))
                                    {
                                        Doit = true;
                                        CommandString = PCS.String3;
                                    }
                                    else
                                    {
                                        XmlDocument XML = new XmlDocument();
                                        XML.LoadXml(DV.XMLConfiguration);
                                        XmlNodeList CommandList = XML.SelectNodes("/root/commands/command");
                                        if (CommandList.Count == 0)
                                            CommandList = XML.SelectNodes("/commands/command");

                                        foreach (XmlElement el in CommandList)
                                        {
                                            string State = "", RangeStart = "", RangeEnd = "", SubField = "", Command = "";
                                            QueryDelay = "";
                                            for (int i = 0; i < el.Attributes.Count; i++)
                                            {
                                                if (el.Attributes[i].Name.ToLower() == "state")
                                                {
                                                    State = el.Attributes[i].Value.ToLower();
                                                    continue;
                                                }
                                                if (el.Attributes[i].Name.ToLower() == "commandstring")
                                                {
                                                    Command = el.Attributes[i].Value;
                                                    continue;
                                                }
                                                if (el.Attributes[i].Name.ToLower() == "rangestart")
                                                {
                                                    RangeStart = el.Attributes[i].Value;
                                                    continue;
                                                }
                                                if (el.Attributes[i].Name.ToLower() == "rangeend")
                                                {
                                                    RangeEnd = el.Attributes[i].Value;
                                                    continue;
                                                }

                                                if (el.Attributes[i].Name.ToLower() == "subfield")
                                                {
                                                    SubField = el.Attributes[i].Value;
                                                    continue;
                                                }

                                                if (el.Attributes[i].Name.ToLower() == "querydelay")
                                                {
                                                    QueryDelay = el.Attributes[i].Value;
                                                    continue;
                                                }
                                            }

                                            if (!string.IsNullOrEmpty(SubField) && SubField.ToLower() != PCS.String5)
                                                continue;

                                            if (State != PCS.String2.ToLower() && (RangeStart == "" || RangeEnd == ""))
                                                continue;
                                            if (string.IsNullOrEmpty(PCS.String3) && !string.IsNullOrEmpty(PCS.String2))
                                            {
                                                Doit = true;
                                                CommandString = Command;
                                            }
                                            break;
                                        }
                                    }
                                    if (Doit)
                                    {
                                        if (!string.IsNullOrEmpty(CommandString))
                                        {
                                            string CMD = Regex.Replace(CommandString, "\\$\\$devnum", DV.NativeDeviceIdentifier, RegexOptions.IgnoreCase);
                                            CMD = Regex.Replace(CMD, "\\$\\$newvalue", PCS.String2, RegexOptions.IgnoreCase);
                                            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                            PCS2.Command = PluginCommandsToPlugins.PriorityProcessNow;
                                            PCS2.DestinationPlugin = LinkPlugin;
                                            PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                            PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                            PCS2.OutgoingDS = new OutgoingDataStruct();
                                            //                   PCSAquired.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                            PCS2.OutgoingDS.LocalIDTag = "Command";
                                            PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                            PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(CMD);
                                            PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                            PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                            PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                                            PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                                            SentCommands.TryAdd(CMD, new Tuple<DateTime, String, int>(_PluginCommonFunctions.CurrentTime, PCS2.OutgoingDS.LocalIDTag, 0));

                                            if (DV.XMLConfiguration.IndexOf("BATLVL") == -1 && !DV.IsDeviceOffline)//Not A battery Operated Device or offline
                                            {
                                                PCS2.OutgoingDS.LocalData = "http://$$IPAddress/rest/query/" + DV.NativeDeviceIdentifier;
                                                PCS2.OutgoingDS.LocalData2 = QueryDelay;
                                            }
                                            _PCF.QueuePluginInformationToPlugin(PCS2);
                                        }
                                    }
                                    continue;
                                }
                                catch (Exception CHMAPIEx)
                                {
                                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                                }

                            }
                            continue;
                        }

                        
                        if (Value.PluginData.Command == PluginCommandsToPlugins.MaintanenceRequest)
                        {
                            PluginCommunicationStruct PCS = Value.PluginData;
                            DeviceStruct DV;
                            if (_PluginCommonFunctions.LocalDevicesByUnique.TryGetValue(PCS.DeviceUniqueID, out DV))
                            {
                                if(DV.OffLine.ToLower()=="y") //A Device Not in Current Use, so no maint command
                                {
                                    XMLDeviceScripts.UseMaintenanceFallbackTime();
                                    continue;
                                }

                                //Debug.WriteLine("maintenance: " + DV.NativeDeviceIdentifier);
                                string CMD = Regex.Replace(Value.PluginData.String3, "\\$\\$devnum", DV.NativeDeviceIdentifier, RegexOptions.IgnoreCase);
                                CMD = Regex.Replace(CMD, "\\$\\$newvalue", PCS.String2, RegexOptions.IgnoreCase);
                                PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                PCS2.Command = PluginCommandsToPlugins.PriorityProcessNow;
                                PCS2.DestinationPlugin = LinkPlugin;
                                PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                PCS2.OutgoingDS = new OutgoingDataStruct();
                                //                   PCSAquired.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                PCS2.OutgoingDS.LocalIDTag = "Maintenance Processed";
                                PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(CMD);
                                PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                                PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                                _PCF.QueuePluginInformationToPlugin(PCS2);
                                XMLDeviceScripts.UseMaintenanceFallbackTime();
                            }
                            continue;
                        }

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
            ServerAccessFunctions.InformationCommingFromServerSlim.Wait();
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            try
            {

                while (ServerAccessFunctions.InformationCommingFromServerQueue.TryDequeue(out Value))
                {

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
            XMLDeviceScripts.StopMaintenance();
            WebSocketSharp_Close();
        }

        private static void WatchdogProcessEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void StartupInfoEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        private static void ProcessNodeInformation(string NodeType, string SNode, string PluginName)
        {
            ProcessNodeInformationSlim.Wait();
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            try
            {
                DeviceStruct Dev = new DeviceStruct();
                List<string> Local_RawProperties = new List<string>(); 

                XMLDeviceScripts XMLScripts = new XMLDeviceScripts();

                string Family = "1";//Default Family is (1) Insteon
                string Cat = "", Mfg="";
                XElement NodeXMLNodes = XElement.Parse(SNode);
                foreach (XElement x in NodeXMLNodes.Elements("node"))
                {
                    foreach (XElement n in x.Nodes())
                    {
                        switch (n.Name.LocalName)
                        {
                            case "address":
                                Dev.DeviceIdentifier = "ISY" + n.Value;
                                Dev.NativeDeviceIdentifier = n.Value;
                                break;

                            case "name":
                                foreach (Tuple<string, string, int> Room in _PluginCommonFunctions.RoomArray)
                                {
                                    string R = n.Value.Trim();
                                    string S = R.ToLower();
                                    if (S.IndexOf(Room.Item2) == 0)
                                    {
                                        Dev.RoomUniqueID = Room.Item1;
                                        Dev.DeviceName = R.Substring(Room.Item2.Length).Trim();
                                        break;
                                    }
                                }
                                break;

                            case "type":
                                Dev.DeviceClassID = n.Value;
                                break;

                            case "sgid":
                                Dev.StrVal01 = n.Value;
                                break;

                            case "family":
                                Family = n.Value;
                                break;

                            case "devtype":
                                Cat = n.Element("cat").Value;
                                Mfg = n.Element("mfg").Value;
                                break;



                        }

                    }
                    if(!string.IsNullOrEmpty(Cat))
                        Dev.DeviceClassID = Dev.DeviceClassID + "." + Cat;

                    foreach (XAttribute a in x.Attributes())
                    {
                        if (a.Name.LocalName == "nodeDefId")
                        {
                            //

                        }

                    }
                }

                foreach (XElement x in NodeXMLNodes.Elements("properties"))
                {
                    foreach (XElement n in x.Nodes())
                    {
                        switch (n.Name.LocalName)
                        {
                            case "property":
                                Local_RawProperties.Add(n.ToString());
                                break;
                        }

                    }
                }
                DeviceStruct Device = new DeviceStruct();
                if (_PluginCommonFunctions.LocalDevicesByDeviceIdentifier.TryGetValue(Dev.NativeDeviceIdentifier, out Device))
                {
                    if (!Device.Local_IsLocalDevice)
                    {
                        string[] Status;
                        if (string.IsNullOrEmpty(Device.XMLConfiguration))
                            Status = new string[0];
                        else
                            Status = Device.XMLConfiguration.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        if (NodeType == "All Nodes")
                            AquiredAddresses.Enqueue(Dev.NativeDeviceIdentifier);
                        else
                        {
                            if (Device.XMLConfiguration.IndexOf("BATLVL") == -1)//Not A battery Operated Device
                            {
                                Device.Local_TableLoc = -1;
                                XMLScripts.SetupXMLConfiguration(ref Dev);
                                Device.Local_Flag1 = false;
                                PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                PCS2.DestinationPlugin = LinkPlugin;
                                PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                                PCS2.OutgoingDS = new OutgoingDataStruct();
                                PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                PCS2.OutgoingDS.LocalIDTag = "Query";
                                PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/rest/query/" + Dev.NativeDeviceIdentifier);
                                PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                                PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);
                                _PCF.QueuePluginInformationToPlugin(PCS2);

                            }
                            else
                            {

                            }
                        }
                   }
                }
                else //New Device
                {
                    Dev.DeviceUniqueID = _PCF.CreateDBUniqueID("D");
                    Dev.InterfaceUniqueID = PluginName;
                    //                    if (!string.IsNullOrEmpty(Dev.RoomUniqueID) && (string.IsNullOrEmpty(Dev.StrVal01) || Dev.StrVal01.Length <= 2))
                    if (!string.IsNullOrEmpty(Dev.RoomUniqueID))
                    {
                        Dev.Local_IsLocalDevice = false; //This is a Valid Device
                        DeviceTemplateStruct DTS;
                        bool TemplateFound = false;
                        if (Family == "1")
                            Dev.DeviceClassID = "1." + Dev.DeviceClassID;
                        try
                        {
                            DTS = _PluginCommonFunctions.DeviceTemplates.First(c => c.DeviceClassID == Dev.DeviceClassID);
                            _PCF.CopyDeviceTemplateIntoNewDevice(DTS, ref Dev);
                            TemplateFound = true;
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                DTS = _PluginCommonFunctions.DeviceTemplates.First(c => c.DeviceClassID == Dev.DeviceClassID+"."+Mfg);
                                Dev.DeviceClassID = Dev.DeviceClassID + "." + Mfg;
                                _PCF.CopyDeviceTemplateIntoNewDevice(DTS, ref Dev);
                                TemplateFound = true;
                            }
                            catch
                            {
                                DTS = new DeviceTemplateStruct();
                            }
                        }

                        if (!TemplateFound) //No Template-Create a default view
                        {
                            if (NodeType == "All Nodes") //New device without a template, So We must Have All Node Information 
                            {
                                AquiredAddresses.Enqueue(Dev.NativeDeviceIdentifier);

                                ProcessNodeInformationSlim.Release();
                                return;
                            }



                            string[] id = Dev.DeviceClassID.Split('.');

                            //< flag subfield = ""  states = "Not Pushed, Pushed"  datafield = "status" stateunknown = "unknown"  archive = "n" > </ flag >
                            switch (Family)
                            {
                                case "1": //Insteon Device
                                    string S1 = "";
                                    ISYFamilyInformation.TryGetValue(ISYFamilyInformationKey(id[0], id[1], id[2]), out S1);
                                    Dev.XMLConfiguration = Dev.XMLConfiguration + "<root device=\"" + S1 + "\" version=\"1.0\">" + "\r\n";
                                    Dev.XMLConfiguration = Dev.XMLConfiguration + "<flags>" + "\r\n";
                                    break;

                                case "4": //Zwave Device
                                    string S4 = "";
                                    ISYFamilyInformation.TryGetValue(ISYFamilyInformationKey(id[0], "0", Cat), out S4);
                                    Dev.XMLConfiguration = Dev.XMLConfiguration + "<root device=\"" + S4 + "\" version=\"1.0\">" + "\r\n";
                                    Dev.XMLConfiguration = Dev.XMLConfiguration + "<flags>" + "\r\n";
                                    break;

                            }
                            foreach (string Q in Local_RawProperties)
                            {
                                XElement NXLM = XElement.Parse(Q);
                                string S = NXLM.Attribute("id").Value; 
                                string SUOM = NXLM.Attribute("uom").Value;
                                string SX;
                                Tuple<string, string, string, Tuple<int, string>[]> SU;
                                SX=ISYDeviceInformation[ISYDeviceInformationKey(Family, S)];
                                string U;
                                if (Family == "1")
                                    U = SUOM;
                                else
                                {
                                    _PluginCommonFunctions.UOM.TryGetValue(_PCF.ConvertToInt32(SUOM), out SU);
                                    U = SU.Item1;
                                }
                                Dev.XMLConfiguration = Dev.XMLConfiguration + "<flag  datafield = \"formatted\" dataattributeelementname = \"id\"   dataattributelementevalue= \"" + S + "\"   subfield = \"" + SX + "\"   uom = \"" + U + "\" > </flag >" + "\r\n";
                            }
                            Dev.XMLConfiguration = Dev.XMLConfiguration + "</flags>" + "\r\n";
                            Dev.XMLConfiguration = Dev.XMLConfiguration + "</root>" + "\r\n";
                        }
                        string[] Status = Dev.XMLConfiguration.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        Dev.Local_TableLoc = -1;
                        XMLScripts.SetupXMLConfiguration(ref Dev);
                        Dev.Local_Flag1 = false;

                        _PCF.AddNewDevice(Dev);

                        if (Dev.XMLConfiguration.IndexOf("BATLVL") == -1)//Not A battery Operated Device
                        {

                            AquiredAddresses.Enqueue(Dev.NativeDeviceIdentifier);
                            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                            PCS2.DestinationPlugin = LinkPlugin;
                            PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                            PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                            PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                            PCS2.OutgoingDS = new OutgoingDataStruct();
                            PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            PCS2.OutgoingDS.LocalIDTag = "Query";
                            PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                            PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/rest/query/" + Dev.DeviceIdentifier.Substring(3));
                            PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                            PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                            PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                            PCS2.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);

                            _PCF.QueuePluginInformationToPlugin(PCS2);

                            
                        }
                        else
                        {
                            AquiredAddresses.Enqueue(Dev.NativeDeviceIdentifier);
                        }
                    }
                    else
                    {
                        Dev.Local_IsLocalDevice = true; //This is not Valid Device

                        Dev.StoredDeviceData = new DeviceDataStruct();
                        Dev.StoredDeviceData.Local_RawValues = new List<Tuple<DateTime, string>>();
                        Dev.StoredDeviceData.Local_RawValueLastStates = null;
                        Dev.StoredDeviceData.Local_RawValueCurrentStates = null;
                        Dev.StoredDeviceData.Local_FlagValueLastStates = null;
                        Dev.StoredDeviceData.Local_ArchiveFlagValueCurrentStates = null;
                        Dev.StoredDeviceData.Local_ArchiveRawValueCurrentStates = null;
                        Dev.StoredDeviceData.Local_FlagValueCurrentStates = null;
                        Dev.StoredDeviceData.Local_MaintanenceInformation = new List<MaintenanceStruct>();
                        Dev.StoredDeviceData.Local_MaintanenceHistory = new List<Tuple<DateTime, string, bool>>();
                        _PluginCommonFunctions.AddLocalDevice(Dev);
                    }
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, SNode);
            }
            ProcessNodeInformationSlim.Release();

        }


        private static void CheckForNodeList()
        {

            if (AquiredAddresses.Count > 0)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                try
                {
                    CheckForNodeListSlim.Wait();
                    string N;
                    AquiredAddresses.TryDequeue(out N);
                    PluginCommunicationStruct PCSAquired = new PluginCommunicationStruct();
                    PCSAquired.DestinationPlugin = LinkPlugin;
                    PCSAquired.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCSAquired.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                    PCSAquired.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;

                    PCSAquired.OutgoingDS = new OutgoingDataStruct();
 //                   PCSAquired.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCSAquired.OutgoingDS.LocalIDTag = "Node";
                    PCSAquired.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCSAquired.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/rest/nodes/" + N);
                    PCSAquired.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                    PCSAquired.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                    PCSAquired.OutgoingDS.CommDataControlInfo[0].HeadersToSend = new WebHeaderCollection();
                    PCSAquired.OutgoingDS.CommDataControlInfo[0].HeadersToSend.Add("Authorization", "Basic " + EncodedPassword);

                    _PCF.QueuePluginInformationToPlugin(PCSAquired);
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
                CheckForNodeListSlim.Release();
            }

        }

        static DateTime WebSocketSharp_LastHeartbeat=DateTime.MaxValue;
        static bool ISYFirstConnect = false;

        static void WebSocketSharp_DoStuff()
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            ISYFirstConnect = true;
            client = new WebSocket("ws://"+ Address +"/rest/subscribe", "ISYSUB");
            client.SetCredentials(Username, Password, true);
            client.Origin = "com.universal-devices.websockets.isy";
            client.OnMessage += (sender, e) => WebSocketSharp_Notify(e);
            client.OnError += (sender, e) => WebSocketSharp_ErrorNotify(e);
            client.Connect();
            //WebSocketSharp_LastHeartbeat = _PluginCommonFunctions.CurrentTime.AddSeconds(-(_PCF.GetStartupField("MaxHeartbeatTime", 120)));
            WebSocketSharp_LastHeartbeat = _PluginCommonFunctions.CurrentTime.AddSeconds(-((_PCF.GetStartupField("MaxHeartbeatTime", 120))/2));
        }

        static void WebSocketSharp_ErrorNotify(WebSocketSharp.ErrorEventArgs e)
        {
            if (e.Message == "An error has occurred in closing the connection.")
                return;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            _PCF.AddToUnexpectedErrorQueue(e.Exception, e.Message);
        }

        static void WebSocketSharp_Notify(MessageEventArgs message)
        {
            SubscribedInfoSlim.Wait();

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            string S;

            try
            {
                XMLDeviceScripts XMLScripts = new XMLDeviceScripts();
                //SubscribedInfo = new ConcurrentQueue<string>();
                XmlDocument XML = new XmlDocument();
//                Debug.WriteLine("ISY: "+message.Data);
                XML.LoadXml(message.Data);
                if (ISYFirstConnect)
                {
                    ISYFirstConnect = false;
                    S = "";
                    XmlNodeList FirstConnectNode = XML.GetElementsByTagName("SubscriptionResponse");
                    if (FirstConnectNode.Count == 1)
                    {
                        XmlNodeList Lnodes = XML.SelectNodes("//*");
                        foreach (XmlElement XM in Lnodes)
                        {
                            switch (XM.Name)
                            {

                                case "SID":
                                    S = XM.InnerText;
                                    break;
                            }
                        }
                    }

                    _PluginCommonFunctions.GenerateLocalMessage(2000011, "", S);
                    WebSocketSharp_LastHeartbeat = _PluginCommonFunctions.CurrentTime;
                }
                XmlNodeList ControlNode =XML.GetElementsByTagName("control");
                if (ControlNode.Count == 1)
                {
                    string Control = ControlNode[0].InnerXml;
                    if(Control=="_0")//Heartbeat
                    {
                        WebSocketSharp_LastHeartbeat = _PluginCommonFunctions.CurrentTime;
                        SubscribedInfoSlim.Release();
                        return;
                    }
                    else
                        WebSocketSharp_LastHeartbeat = _PluginCommonFunctions.CurrentTime;

                }

                XmlNodeList Nodes = XML.GetElementsByTagName("node");
                if (Nodes.Count == 1)
                {
                    string Node = Nodes[0].InnerXml;
                    if (!string.IsNullOrEmpty(Node))
                    {
                        DeviceStruct Device;
                        string Control = "", Value = "", Formatted = "", UOM = "";
                        int UOMNum=0, Prec=0;

                        if (_PluginCommonFunctions.LocalDevicesByDeviceIdentifier.TryGetValue(Node, out Device))
                        {
                            Device.IsDeviceOffline = false;
                            if (!Device.Local_IsLocalDevice)
                            {
                                XmlNodeList Lnodes = XML.SelectNodes("//*");
                                foreach (XmlElement XM in Lnodes)
                                {
//                                    Debug.WriteLine("Node: " + Node + "/" + Device.NativeDeviceIdentifier +":"+ XM.Name+" "+XM.InnerText);
                                    switch (XM.Name)
                                    {
                                        case "control":
                                            Control = XM.InnerText;
                                            break;

                                        case "fmtAct":
                                            Formatted = XM.InnerText;
                                            break;

                                        case "action":
                                            Value = XM.InnerText;
                                            foreach (XmlAttribute a in XM.Attributes)
                                            {
                                                switch (a.Name)
                                                {
                                                    case "uom":
                                                        UOMNum = _PCF.ConvertToInt32(a.Value);
                                                        break;

                                                    case "prec":
                                                        Prec = _PCF.ConvertToInt32(a.Value);
                                                        break;
                                                }
                                            }
                                            break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(Control))
                                {
                                    //Format Values
                                    Tuple<string, string, string, Tuple<int, string>[]> TP;
                                    if (Prec > 0)
                                    {
                                        Value = (_PCF.ConvertToDouble(Value) / Math.Pow(10, Prec)).ToString();
                                    }
                                    Formatted = Value;

                                    if (_PluginCommonFunctions.UOM.TryGetValue(UOMNum, out TP))
                                    {
                                        UOM = TP.Item2;
                                        if (TP.Item4 != null)
                                        {
                                            int i = _PCF.ConvertToInt32(Value);
                                            if (i >= 0)
                                            {
                                                foreach (Tuple<int, string> X in TP.Item4)
                                                {
                                                    if (X.Item1 == i)
                                                    {
                                                        Formatted = X.Item2;
                                                        break;
                                                    }

                                                }

                                            }
                                        }
                                    }
                                    if (string.IsNullOrWhiteSpace(Value))
                                    {
                                        _PCF.GetCHMStartupField("UnknownName", out Value, "Unknown");
                                        _PCF.GetCHMStartupField("UnknownName", out Formatted, "Unknown");

                                    }
                                    string V = "<property id=\"" + Control + "\"  value=\"" + Value + "\" formatted=\"" + Formatted + "\"  uom=\"" + UOM + "\"/>";
                                    Debug.WriteLine(V);
                                    XMLScripts.ProcessDeviceXMLScriptFromData(ref Device, V, XMLDeviceScripts.DeviceScriptsDataTypes.XML);
                                    MaintenanceStruct MA =null;
                                    if (Control == "ST")
                                    {
                                        XMLScripts.ProcessXMLMaintanenceInformation(ref MA, Device.NativeDeviceIdentifier, "Status", MaintanenceCommands.TaskSucessful);
                                    }
                                    else
                                    {
                                        if (Device.StoredDeviceData != null)
                                        {
                                            foreach (FlagAttributes FlagAtt in Device.StoredDeviceData.Local_FlagAttributes)
                                            {
                                                int i;
                                                string DataAttributeElementValue = "";
                                                string MaintTransaction = "";
                                                for (i = 0; i < FlagAtt.AttributeNames.Length; i++)
                                                {
                                                    switch (FlagAtt.AttributeNames[i])
                                                    {
                                                        case "dataattributelementevalue":
                                                            DataAttributeElementValue = FlagAtt.AttributeValues[i];
                                                            break;

                                                        case "mainttransaction":
                                                            MaintTransaction = FlagAtt.AttributeValues[i];
                                                            break;
                                                    }

                                                }
                                                //if (Control == DataAttributeElementValue && MaintTransaction == "complete")
                                                //{
                                                XMLScripts.ProcessXMLMaintanenceInformation(ref MA, Device.NativeDeviceIdentifier, "Status", MaintanenceCommands.TaskSucessful);
                                                //    break;
                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else //New Node Found, perhaps
                        {
                            AquiredAddresses.Enqueue(Node);
                            CheckForNodeList();
                        }
                    }
                }
            }
           catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            SubscribedInfoSlim.Release();
        }

        static void WebSocketSharp_Close()
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                }
            }
            catch
            {

            }
        }
    }
}

//<flag datafield = "formatted" dataattributeelementname = "id"   dataattributelementevalue= "ST"   subfield = "Status"   uom = "percent"   queryfieldname="formatted"  queryfieldvalue="100"  queryfieldoperation="EQ"  flagvalue="On"  > </flag >