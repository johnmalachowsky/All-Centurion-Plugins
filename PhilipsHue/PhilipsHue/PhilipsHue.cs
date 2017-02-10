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
using System.Text.RegularExpressions;



//Required Parameters
//  UpdateInterval (In Milliseconds, default is 2500)

namespace CHMModules
{


    public class PhilipsHue
    {

#region Standard Functions
        private static _PluginCommonFunctions PluginCommonFunctions;
        private static string Password;
        private static string Username;
        internal static string LinkPlugin;
        internal static string LinkPluginReferenceIdentifier;
        internal static string LinkPluginSecureCommunicationIDCode;
        internal static bool LinkedCommReady = false;
        internal static bool Connected = false;
        internal static DateTime LastHueAccess = DateTime.MinValue;
        
        

        [JsonObject]
        public class WhitelistItem
        {
            [JsonProperty("name")]
            public string ApplicationID { get; private set; }
            [JsonProperty("last use date")]
            public DateTime LastUsed { get; private set; }
            [JsonProperty("create date")]
            public DateTime Created { get; private set; }
            public override string ToString()
            {
                return ApplicationID;
            }
        }

        [JsonObject]
        public class UpdateInfo
        {
            [JsonProperty("updatestate")]
            public int UpdateStatus { get; private set; }
            [JsonProperty("url")]
            public string Url { get; private set; }
            [JsonProperty("text")]
            public string Message { get; private set; }
            [JsonProperty("notify")]
            public bool ShouldNotify { get; private set; }
        }

        [JsonObject]
        public class PortalInfo
        {
            [JsonProperty("signedon")]
            public bool IsSignedOn { get; private set; }
            [JsonProperty("incoming")]
            public bool IsIncomingEnabled { get; private set; }
            [JsonProperty("outgoing")]
            public bool IsOutgoingEnabled { get; private set; }
            [JsonProperty("communication")]
            public string CommunicationState { get; private set; }
        }


        [JsonObject]
        public class Configuration
        {
            [JsonProperty("name")]
            public string BridgeName { get; private set; }
            [JsonProperty("zigbeechannel")]
            public int ZigbeeChannel { get; private set; }
            [JsonProperty("mac")]
            public string MacAddress { get; private set; }
            [JsonProperty("dhcp")]
            public bool IsDhcpEnabled { get; private set; }
            [JsonProperty("ipaddress")]
            public string IpAddress { get; private set; }
            [JsonProperty("netmask")]
            public string NetMask { get; private set; }
            [JsonProperty("gateway")]
            public string GatewayAddress { get; private set; }
            [JsonProperty("proxyaddress")]
            public string ProxyAddress { get; private set; }
            [JsonProperty("proxyport")]
            public int ProxyPort { get; private set; }
            [JsonProperty("UTC")]
            public DateTime CurrentTimeUtc { get; private set; }
            [JsonProperty("localtime")]
            public DateTime CurrentTime { get; private set; }
            [JsonProperty("timezone")]
            public string TimeZone { get; private set; }
            [JsonProperty("swversion")]
            public string SoftwareVersion { get; private set; }
            [JsonProperty("apiversion")]
            public string ApiVersion { get; private set; }
            [JsonProperty("linkbutton")]
            public bool IsLinkButtonPressed { get; private set; }
            [JsonProperty("portalservices")]
            public bool IsPortalServicesEnabled { get; private set; }
            [JsonProperty("portalconnection")]
            public string PortalConnectionState { get; private set; }
            [JsonProperty("whitelist")]
            public Dictionary<string, WhitelistItem> Whitelist { get; private set; }
            [JsonProperty("swupdate")]
            public UpdateInfo UpdateState { get; private set; }
            [JsonProperty("portalstate")]
            public PortalInfo PortalState { get; private set; }
        }

        [JsonObject]
        public class LightState
        {
            [JsonProperty("on")]
            public bool IsOn { get; set; }
            [JsonProperty("hue")]
            public uint Hue { get; set; }
            [JsonProperty("bri")]
            public byte Brightness { get; set; }
            [JsonProperty("sat")]
            public byte Saturation { get; set; }
            [JsonProperty("ct")]
            public uint ColorTemperature { get; set; }
            [JsonProperty("xy")]
            public float[] ColorSpaceCoordinates { get; set; }
            [JsonProperty("effect")]
            public LightEffect Effect { get; set; }
            [JsonProperty("alert")]
            public LightAlert Alert { get; set; }
            [JsonProperty("reachable")]
            public bool IsReachable { get; private set; }
            [JsonProperty("colormode")]
            public string CurrentColorMode { get; private set; }
        }

        public enum LightEffect
        {
            /// No effect is running.
            None,
            /// The light cycles through all hues at the current brightness and saturation levels.
            ColorLoop
        }

        public enum LightAlert
        {
            /// Turns off the light alert.
            None,
            /// Flashes the light once.
            Select,
            /// Flashes the light repeatedly for 30 seconds, or until <see cref="LightAlert.None" /> is sent.
            LSelect
        }

        [JsonObject]
        public class Light
        {
            public string ID { get; internal set; }
            [JsonProperty("name")]
            public string Name { get; internal set; }
            [JsonProperty("state")]
            public LightState State { get; internal set; }
            [JsonProperty("type")]
            public string Type { get; private set; }
            [JsonProperty("modelid")]
            public string ModelID { get; private set; }
            [JsonProperty("swversion")]
            public string SoftwareVersion { get; private set; }
        }



        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Phiips Hue Devices";
            ServerAccessFunctions.PluginSerialNumber = "00001-00016";
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
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;
            



    }

        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            PasswordStruct Pw = new PasswordStruct();

            _PCF.GetPasswordInfo("", "", ref Pw);

            Password = Pw.Password;
            Username = Pw.Account;
            if(string.IsNullOrEmpty(Password))
            {
                _PCF.AddActionitem(20000, "CHM_" + ServerAccessFunctions.PluginSerialNumber, "", "");
            }
 
        }

        private static void FlagCommingServerEventHandler(ServerEvents WhichEvent)
        {

        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            if (!Connected)
            {
                if (LinkedCommReady == true && string.IsNullOrEmpty(Password) && (_PluginCommonFunctions.CurrentTime - LastHueAccess).TotalSeconds>20)
                {
                    LastHueAccess = _PluginCommonFunctions.CurrentTime;
                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                    PCS2.DestinationPlugin = LinkPlugin;
                    PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                    PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                    PCS2.OutgoingDS = new OutgoingDataStruct();
                    PCS2.OutgoingDS.LocalIDTag = "Create user";
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/api");
                    PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Post";
                    PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.SpecificLength;
                    PCS2.OutgoingDS.CommDataControlInfo[0].ReponseSizeToWaitFor = 10;
                    PCS2.OutgoingDS.CommDataControlInfo[0].BodyData = "{ \"devicetype\": \"CHM_" + ServerAccessFunctions.PluginSerialNumber + "#\"}";
                    _PCF.QueuePluginInformationToPlugin(PCS2);
                }
                if (LinkedCommReady == true && !string.IsNullOrEmpty(Password))
                {
                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                    PCS2.DestinationPlugin = LinkPlugin;
                    PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                    PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                    PCS2.OutgoingDS = new OutgoingDataStruct();
                    PCS2.OutgoingDS.LocalIDTag = "Configuration";
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                    PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/api/" + Password + "/config");
                    PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                    PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.SpecificLength;
                    _PCF.QueuePluginInformationToPlugin(PCS2);
                    Connected = true;
                }
            }

            if(Connected && (_PluginCommonFunctions.CurrentTime - LastHueAccess).TotalSeconds > _PCF.GetStartupField("HueTimeslice",3))
            {
                LastHueAccess = _PluginCommonFunctions.CurrentTime;
                RequestListOfLights();
            }
        }

        private static void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent)
        {
            PluginEventArgs Value;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            XMLDeviceScripts XMLScripts = new XMLDeviceScripts();
            string ErrorDescription = "", ErrorAddress = "";
            int ErrorType = -1;
            JToken ParseInData=null;

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
                            string InData = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived);
                            if (!string.IsNullOrEmpty(InData))
                            {
                                ParseInData = JToken.Parse(InData);

                                // Check for a Hue error
                                if (ParseInData is JArray && ((JArray)ParseInData).Count>0)
                                {
                                    JObject error = ParseInData[0]["error"] as JObject;
                                    if (error != null)
                                    {
                                        ErrorDescription = error["description"].ToString();
                                        ErrorType = error["type"].Value<int>();
                                        ErrorAddress = error["address"].ToString();
                                    }
                                }
                            }
                            switch (ODS.LocalIDTag)
                            {
                                case "Lights":
                                    if (ErrorType == -1)
                                    {
                                        foreach (JProperty j in ParseInData)
                                        {
                                            string Name = j.Name;
                                            Light l = j.Value.ToObject<Light>();
                                            l.ID = Name;
                                            DeviceStruct Device;
                                            if (!_PluginCommonFunctions.LocalDevicesByDeviceIdentifier.TryGetValue(Name, out Device))
                                            {
                                                Device = new DeviceStruct();
                                                foreach (Tuple<string, string, int> Room in _PluginCommonFunctions.RoomArray)
                                                {
                                                    string R = l.Name.Trim();
                                                    string S = R.ToLower();
                                                    if (S.IndexOf(Room.Item2) == 0)
                                                    {
                                                        Device.RoomUniqueID = Room.Item1;
                                                        Device.DeviceName = R.Substring(Room.Item2.Length).Trim();
                                                        break;
                                                    }
                                                }

                                                Device.InterfaceUniqueID = Value.PluginData.DeviceUniqueID;
                                                Device.DeviceType = l.Type;
                                                Device.DeviceClassID = l.ModelID;
                                                Device.DeviceIdentifier = Name;
                                                Device.NativeDeviceIdentifier = Name;

                                                try
                                                {
                                                    DeviceTemplateStruct DTS = _PluginCommonFunctions.DeviceTemplates.First(c => c.DeviceClassID == Device.DeviceClassID);
                                                    _PCF.CopyDeviceTemplateIntoNewDevice(DTS, ref Device);
                                                    _PCF.AddNewDevice(Device);

                                                }
                                                catch
                                                {
                                                    Device.XMLConfiguration = Device.XMLConfiguration + "<root device=\"" + l.Type + "\" version=\"1.0\">" + "\r\n";
                                                    Device.XMLConfiguration = Device.XMLConfiguration + "<flags>" + "\r\n";
                                                    Device.XMLConfiguration = Device.XMLConfiguration + "<flag  datafield = \"state\"> </flag >" + "\r\n";
                                                    Device.XMLConfiguration = Device.XMLConfiguration + "</flags>" + "\r\n";
                                                    Device.XMLConfiguration = Device.XMLConfiguration + "</root>" + "\r\n";
                                                    Device.DeviceUniqueID = _PCF.CreateDBUniqueID("D");
                                                    _PCF.AddNewDevice(Device);
                                                }
                                                XMLScripts.SetupXMLConfiguration(ref Device);

                                            }
                                            UpdateLightValues(l.State, Device);
                                        }
                                    }
                                    break;
 

                                case "One Light":
                                    if (ErrorType == -1)
                                    {
                                        Light State = new Light();
                                        JsonConvert.PopulateObject(ParseInData.ToString(), State);

                                        DeviceStruct Device;
                                        if (_PluginCommonFunctions.LocalDevicesByDeviceIdentifier.TryGetValue(ODS.LocalData, out Device))
                                        {
                                            UpdateLightValues(State.State, Device);
                                        }
                                        break; //Only want state information
                                    }
                                    break;

                                case "LightCommand":
                                    if (ErrorType == -1)
                                    {
                                        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                        PCS2.DestinationPlugin = LinkPlugin;
                                        PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                        PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                        PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                                        PCS2.OutgoingDS = new OutgoingDataStruct();
                                        PCS2.OutgoingDS.LocalIDTag = "One Light";
                                        PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                        PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/api/" + Password + "/lights/"+ ODS.LocalData);
                                        PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
                                        PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.SpecificLength;
                                        PCS2.OutgoingDS.LocalData =ODS.LocalData;
                                        _PCF.QueuePluginInformationToPlugin(PCS2);





                                    }
                                    break;




                                case "Configuration":
                                    if (ErrorType == -1)
                                    {
                                        Configuration Config= ParseInData.ToObject<Configuration>();
                                        RequestListOfLights();
                                    }
                                    break;

                                case "Create user":
                                    if (ErrorType!=-1)
                                    {
                                        if (ErrorType != 101)
                                        {
                                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(20001, "", ErrorType.ToString() + " " + ErrorDescription + " @" + ErrorAddress);

                                        }
                                    }
                                    else
                                    {
                                        JToken ParseInData2 = JToken.Parse(InData);
                                        JObject Returned= ParseInData2[0]["success"] as JObject;
                                        if (Returned != null)
                                        {
                                            Username = "CHM_" + ServerAccessFunctions.PluginSerialNumber;
                                            Password = Returned["username"].ToString();
                                            _PCF.AddPassword("PhilipsHue", Username, Password, "");
                                            _PCF.DeleteActionitem("CHM_" + ServerAccessFunctions.PluginSerialNumber);

                                        }

                                    }

                                    break;
                            }
                            continue;
                        }

                        if (Value.PluginData.Command == PluginCommandsToPlugins.ProcessCommandWords || Value.PluginData.Command == PluginCommandsToPlugins.DirectCommand)
                        {
                            PluginCommunicationStruct PCS = Value.PluginData;
                            DeviceStruct DV;
                            if (_PluginCommonFunctions.LocalDevicesByUnique.TryGetValue(PCS.DeviceUniqueID, out DV))
                            {
                                try
                                {
                                    bool Doit = false;
                                    string CommandString = "";

                                    if (string.IsNullOrEmpty(PCS.String2) && !string.IsNullOrEmpty(PCS.String3))
                                    {
                                        Doit = true;
                                        CommandString = PCS.String3;
                                    }

                                    string State = "", RangeStart = "", RangeEnd = "", SubField = "", RangeStates = "", Type = "", BodyData = "";
                                    if (string.IsNullOrEmpty(PCS.String3) && !string.IsNullOrEmpty(PCS.String2))
                                    {
                                        XmlDocument XML = new XmlDocument();
                                        XML.LoadXml(DV.XMLConfiguration);
                                        XmlNodeList CommandList = XML.SelectNodes("/root/commands/command");
                                        if (CommandList.Count == 0)
                                            CommandList = XML.SelectNodes("/commands/command");


                                        foreach (XmlElement el in CommandList)
                                        {
                                            State = "";
                                            RangeStart = "";
                                            RangeEnd = "";
                                            SubField = "";
                                            RangeStates ="";
                                            Type = "";
                                            for (int i = 0; i < el.Attributes.Count; i++)
                                            {
                                                if (el.Attributes[i].Name.ToLower() == "state")
                                                {
                                                    State = el.Attributes[i].Value.ToLower();
                                                    continue;
                                                }
                                                if (el.Attributes[i].Name.ToLower() == "commandstring")
                                                {
                                                    CommandString = el.Attributes[i].Value;
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

                                                if (el.Attributes[i].Name.ToLower() == "rangestates")
                                                {
                                                    RangeStates = el.Attributes[i].Value;
                                                    continue;
                                                }

                                                if (el.Attributes[i].Name.ToLower() == "type")
                                                {
                                                    Type = el.Attributes[i].Value;
                                                    continue;
                                                }



                                            }
                                            if (State != PCS.String2.ToLower() && (RangeStart == "" || RangeEnd == ""))
                                                continue;
                                            Doit = true;
                                            break;
                                        }
                                    }
                                    if (Doit)
                                    {

                                        if (!string.IsNullOrWhiteSpace(Type) && Type.ToLower()== "monolight") //Lets Turn It On and Off
                                        {
                                            string on = "true";
                                            bool UseRange = true;
                                            int level = _PCF.ConvertToInt32(PCS.String2);
                                            string[] S = RangeStates.Split(',');
                                            foreach(string s in S)
                                            {
                                                string[] q = s.Trim().Split('=');
                                                if(level== _PCF.ConvertToInt32(q[1]))
                                                {
                                                    if (q[0].ToLower() == "off")
                                                    {
                                                        on = "false";
                                                        UseRange = false;
                                                    }
                                                    if (q[0].ToLower() == "on")
                                                    {
                                                        on = "true";
                                                        level = _PCF.ConvertToInt32(RangeEnd) - 1;
                                                        UseRange = true;
                                                    }

                                                }
                                            }
                                            BodyData = "{ " + "\r\n"+"\"on\": "+on;
                                            if (UseRange)
                                                BodyData = BodyData + ", " + "\r\n" + "\"bri\": " + level.ToString() + "\r\n";
                                            BodyData= BodyData+ " }";


                                        }

                                        string CMD = Regex.Replace(CommandString, "\\$\\$devnum", DV.NativeDeviceIdentifier, RegexOptions.IgnoreCase);
                                        CMD = Regex.Replace(CMD, "\\$\\$username", Password, RegexOptions.IgnoreCase);
                                        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                                        PCS2.Command = PluginCommandsToPlugins.PriorityProcessNow;
                                        PCS2.DestinationPlugin = LinkPlugin;
                                        PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                                        PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
                                        PCS2.OutgoingDS = new OutgoingDataStruct();
                                        //                   PCSAquired.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                        PCS2.OutgoingDS.LocalIDTag = "LightCommand";
                                        PCS2.OutgoingDS.LocalData = DV.NativeDeviceIdentifier;

                                        PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
                                        PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray(CMD);
                                        PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Put";
                                        PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                                        PCS2.OutgoingDS.CommDataControlInfo[0].BodyData = BodyData;
                                        _PCF.QueuePluginInformationToPlugin(PCS2);
                                    }

                                }
                                catch (Exception CHMAPIEx)
                                {
                                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                                }

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
#endregion



#region Unique Functions To This Plugin

        static internal void RequestListOfLights()
        {
            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            PCS2.DestinationPlugin = LinkPlugin;
            PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
            PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;
            PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
            PCS2.OutgoingDS = new OutgoingDataStruct();
            PCS2.OutgoingDS.LocalIDTag = "Lights";
            PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[1];
            PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/api/" + Password + "/lights");
            PCS2.OutgoingDS.CommDataControlInfo[0].Method = "Get";
            PCS2.OutgoingDS.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.SpecificLength;
            _PCF.QueuePluginInformationToPlugin(PCS2);
        }

        static internal void UpdateLightValues(LightState Lght, DeviceStruct Device)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();



            string State ="", Brightnesss="";
            bool NewValue = false;
            if (Lght.IsReachable)
            {
                if (Lght.IsOn)
                {
                    if (Device.LocalStrVar02 != "On" || Device.LocalStrVar01 != Lght.Brightness.ToString())
                    {
                        NewValue = true;
                        State = "On";
                        Brightnesss = Lght.Brightness.ToString();
                        Device.LocalIntVar01 = 0;
                        Device.LocalStrVar01 = Brightnesss;
                        Device.LocalStrVar02 = "On";
                    }
                }
                else
                {
                    if (Device.LocalStrVar02 != "Off")
                    {
                        NewValue = true;
                        State = "Off";
                        Brightnesss = "0";
                        Device.LocalIntVar01 = 0;
                        Device.LocalStrVar01 = "0";
                        Device.LocalStrVar02 = "Off";
                    }
                }
            }
            else
            {
                Device.LocalIntVar01++;
                if (Device.LocalIntVar01 > _PCF.GetStartupField("NumberOfOffLinesBeforeDisplayOffLine", 5))
                {
                    string OffLineName;
                    _PCF.GetCHMStartupField("OffLineName", out OffLineName, "Off-Line");
                    if (Device.LocalStrVar02 != OffLineName)
                    {
                        State = OffLineName;
                        Brightnesss = "-1";
                        Device.LocalStrVar01 = "-1";
                        Device.LocalStrVar02 = OffLineName;
                        _PCF.TakeDeviceOffLine(Device.DeviceUniqueID);
                        NewValue = false;
                    }

                }
            }
            if (NewValue)
            {
                XMLDeviceScripts XMLScripts = new XMLDeviceScripts();
                string V = "<property state=\"" + State + "\"  Hue=\"" + Lght.Hue.ToString() + "\" brightness=\"" + Brightnesss + "\"  saturation=\"" + Lght.Saturation.ToString() + "\" colortemp=\"" + Lght.ColorTemperature.ToString() + "\" colormode=\"" + Lght.CurrentColorMode + "\"/>";
                XMLScripts.ProcessDeviceXMLScriptFromData(ref Device, V, XMLDeviceScripts.DeviceScriptsDataTypes.XML);
            }


        }

        #endregion


    }
}
