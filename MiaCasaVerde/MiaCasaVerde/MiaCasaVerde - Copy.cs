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
//  UpdateInterval (In Miliseconds, default is 2500)
namespace CHMModules
{
    class MiaCasaVerde
    {
        static private _PluginCommonFunctions PluginCommonFunctions;
        private static string LinkPlugin;
        private static string LinkPluginReferenceIdentifier;
        private static string LinkPluginSecureCommunicationIDCode;

        private static List <DeviceStruct> LocalDevices;
        private static List<Tuple<string, string, int>> LocalRooms;
        private static List<Tuple<string, string, int>> LocalCategories;
        private static List<DeviceTemplateStruct> LocalDeviceTemplates;
        private static string UnKnownCatagory;

        private struct FlagAttributes
        {
            public string[] FlagAttributeNames;
            public string[] FlagAttributeValues;
            public string[] ArchiveAttributeNames;
            public string[] ArchiveAttributeValues;
        }


        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Mia Casa Vera Console";
            ServerAccessFunctions.PluginSerialNumber = "00001-00012";
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

            LocalDevices = new List <DeviceStruct>();
            LocalRooms = new List<Tuple<string, string, int>>();
            LocalCategories = new List<Tuple<string, string, int>>();
            LocalDeviceTemplates = new List<DeviceTemplateStruct>();

            return;
        }

        private static void CommandEvent(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }
        
        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            foreach (DeviceStruct DV in _PluginCommonFunctions.Devices)
            {
                string[] Status = DV.CommandSet.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                DeviceStruct DVX = DV;
                string[] LS= new string[Status.Length];
                for (int i = 0; i < LS.Length; i++)
                    LS[i] = "";
                DVX.LastStates = LS.Clone();
                DVX.CurrentStates = LS.Clone();
                DVX.TableLoc = -1;
                SetupCommandSetXML(ref DVX);
                LocalDevices.Add(DVX);
            }

            foreach (Tuple<string, string> RM in _PluginCommonFunctions.Rooms)
            {
                LocalRooms.Add(new Tuple<string, string, int>(RM.Item1, RM.Item2, -1));
            }

            foreach(DeviceTemplateStruct DT in _PluginCommonFunctions.DeviceTemplates)
            {
                LocalDeviceTemplates.Add(DT);
            }
            UnKnownCatagory=_PCF.GetStartupFieldWithDefault("UnknownCatagory", "Soft Button");
            
            return;
        
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

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            PluginEventArgs Value;
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();

            while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
            {

                try
                {


                    if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionComplete)
                    {
                        OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS.Copy();

                        if (ODS.CommDataControlInfo[0].ActualResponseReceived == null) //Not Valid Data
                        {
                            if (ODS.LocalIDTag == "GeneralData")
                            {
                                continue;
                            }
                            continue;
                        }
                        if (ODS.LocalIDTag == "GeneralData")
                        {
                            string InfoReceived = _PCF.ConvertByteArrayToString(ODS.CommDataControlInfo[0].ActualResponseReceived);
                            JObject root = JObject.Parse(InfoReceived);
                            DateTime  LoadTime;
                            try
                            {
                                //LoadTime= new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(_PCF.ConvertToInt32(root["loadtime"].ToString()));
                                LoadTime = DateTime.Now;
                            }
                            catch
                            {
                                LoadTime=DateTime.Now;
                            }
                           
                            JArray Categories = (JArray)root["categories"];
                            if (Categories != null)
                            {
                                foreach (JObject CategoriesInfo in Categories)
                                {
                                    string ID = CategoriesInfo["id"].ToString();
                                    string Name = CategoriesInfo["name"].ToString();
                                    int index = LocalCategories.FindIndex(c => c.Item2.ToLower() == Name.ToLower());
                                    if (index == -1)
                                    {
                                        LocalCategories.Add(new Tuple<string, string, int>(_PCF.CreateDBUniqueID("XC"), Name, _PCF.ConvertToInt32(ID)));
                                    }
                                    else
                                    {
                                        LocalCategories[index] = new Tuple<string, string, int>(LocalCategories[index].Item1, LocalCategories[index].Item2, _PCF.ConvertToInt32(ID));
                                    }

                                }
                            }
                            JArray Rooms = (JArray)root["rooms"];
                            if (Rooms != null)
                            {
                                foreach (JObject RoomInfo in Rooms)
                                {
                                    string ID = RoomInfo["id"].ToString();
                                    string Name = RoomInfo["name"].ToString();
                                    int index = LocalRooms.FindIndex(c => c.Item2.ToLower() == Name.ToLower());
                                    if (index == -1)
                                    {
                                        string RoomID = _PCF.CreateDBUniqueID("R");
                                        LocalRooms.Add(new Tuple<string, string, int>(RoomID, Name, _PCF.ConvertToInt32(ID)));
                                        _PCF.AddNewRoom(RoomID, Name, "");

                                    }
                                    else
                                    {
                                        LocalRooms[index] = new Tuple<string, string, int>(LocalRooms[index].Item1, LocalRooms[index].Item2, _PCF.ConvertToInt32(ID));
                                    }

                                }
                            }

                            //CommandSet *, ??????????   The field that Has the value that shows the state of the device
                            JArray Devices = (JArray)root["devices"];
                            if (Devices != null)
                            {
                                foreach (JObject DeviceInfo in Devices)
                                {
                                    string ID = DeviceInfo["id"].ToString();
                                    int Dev = LocalDevices.FindIndex(c => _PCF.ConvertToInt32(ID) == c.TableLoc);
                                    if (Dev == -1)
                                    {
                                        string Name = DeviceInfo["name"].ToString();
                                        int Room = _PCF.ConvertToInt32(DeviceInfo["room"].ToString());
                                        int i = LocalRooms.FindIndex(c => c.Item3 == Room);
                                        string DeviceRoomID = LocalRooms[i].Item1;
                                        string DeviceRoomName = LocalRooms[i].Item2;
                                        int index = LocalDevices.FindIndex(c => c.DeviceIdentifier.ToLower() == ("VERA" + ID).ToLower());
                                        if (index != -1)
                                        {
                                            DeviceStruct NDV = LocalDevices[index];
                                            NDV.TableLoc=_PCF.ConvertToInt32(ID);
                                            LocalDevices[index] = NDV;
                                        }
                                        else
                                        {
                                            int Catagory = _PCF.ConvertToInt32(DeviceInfo["category"].ToString());
                                            int x = LocalCategories.FindIndex(c => c.Item3 == Catagory);
                                            string CatName;
                                            if (x < 0)
                                                CatName = UnKnownCatagory;
                                            else
                                                CatName = LocalCategories[x].Item2;

                                            DeviceStruct DVS = new DeviceStruct();
                                            DVS.States = new string[10];
                                            DeviceTemplateStruct DTS;
                                            try
                                            {
                                                DTS = _PluginCommonFunctions.DeviceTemplates.First(c => c.DeviceKey.ToLower() == CatName.ToLower());
                                            }
                                            catch
                                            {
                                                DTS = new DeviceTemplateStruct();
                                            }
                                            _PCF.CopyDeviceTemplateIntoNewDevice(DTS, ref DVS);


                                            DVS.DeviceName = Name;
                                            DVS.RoomUniqueID = DeviceRoomID;
                                            DVS.InterfaceUniqueID = Value.PluginData.DeviceUniqueID;
                                            DVS.DeviceType = CatName;
                                            DVS.DeviceIdentifier = "VERA" + ID;
                                            string[] Status = DVS.CommandSet.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                            string[] LS = new string[Status.Length];
                                            for (int ii = 0; ii < LS.Length; ii++)
                                                LS[ii] = "";
                                            DVS.LastStates = LS.Clone();
                                            DVS.CurrentStates = LS.Clone();
                                            DVS.TableLoc=_PCF.ConvertToInt32(ID);
                                            SetupCommandSetXML(ref DVS);
                                            LocalDevices.Add(DVS);
                                            _PCF.AddNewDevice(DVS);
                                        }
                                    }
                                    int DevIndex = LocalDevices.FindIndex(c => _PCF.ConvertToInt32(ID) == c.TableLoc);
                                    if (DevIndex < 0)// Bad Device Index-Not Found
                                    {
                                    }
                                    else
                                    {
                                        DeviceStruct Device = LocalDevices[DevIndex];
                                        string[] temp = (string[])Device.CurrentStates;
                                        Device.LastStates = temp.Clone();
                                        FlagAttributes FlagAtt = (FlagAttributes) Device.Object1
                                        
                                        
                                        string[] Status;
                                        if (string.IsNullOrEmpty(Device.CommandSet))
                                        {
                                            Status = new string[1];
                                            Status[0] = "*,status";
                                        }
                                        else
                                        {
                                            Status = Device.CommandSet.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                        }
                                        
                                        for(int StatIndex=0;StatIndex<=Status.Length;StatIndex++)
                                        {
                                            try
                                            {
                                                string Stat = Status[StatIndex];
                                                string[] S = Stat.Split(',');
                                                string RawValue = DeviceInfo[S[1].ToLower().Trim()].ToString();
                                                string FlagValue = RawValue;
                                                string[] LS=(string[])Device.CurrentStates;
                                                LS[StatIndex] = RawValue;
                                                Device.CurrentStates = LS;
                                                if(S[0].ToUpper().IndexOf('*')>-1)
                                                {
                                                    if (Device.States == null)
                                                        FlagValue = RawValue;
                                                    else
                                                    {
                                                        int x = _PCF.ConvertToInt32(RawValue);
                                                        if (x < 0 || x > 9)
                                                            FlagValue = Device.StateUnknown;
                                                        else
                                                            FlagValue = Device.States[x];
                                                    }
                                                    try
                                                    {
                                                        string SL = DeviceInfo["level"].ToString();
                                                        if (SL != null)
                                                        {
                                                            if (SL != "100" && SL != "0" && Device.States[2] != null)
                                                            {
                                                                FlagValue = Device.States[2];
                                                                RawValue = "2";
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                    if (string.IsNullOrEmpty(FlagValue))
                                                        FlagValue = RawValue;
                                                    S[1] = "";
                                                }
                                                else
                                                {
                                                    if (S.Length == 5)
                                                    {
                                                        string SL = DeviceInfo[S[1]].ToString();
                                                        if (SL == "0")
                                                            FlagValue = S[3];
                                                        if (SL == "1")
                                                            FlagValue = S[4];
                                                        S[1] = S[2];
                                                    }
                                                }

                                                LocalDevices[DevIndex] = Device;
                                                _PCF.AddFlagForTransferToServer(
                                                    _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                                    S[1].Trim(),
                                                    FlagValue,
                                                    RawValue,
                                                    Device.RoomUniqueID,
                                                    Device.InterfaceUniqueID,
                                                    FlagChangeCodes.OwnerOnly,
                                                    FlagActionCodes.addorupdate);
                                                if (Device.DeviceType.ToLower() == "door lock" && S.Length==6 && S[0].ToUpper().IndexOf('*')>-1)
                                                {
                                                    string[] LSX = (string[])Device.LastStates;
                                                    if(!string.IsNullOrEmpty(LSX[StatIndex]))
                                                    {
                                                        string Comment = DeviceInfo["comment"].ToString();
                                                        string Locked = DeviceInfo["locked"].ToString();
                                                        string EventTime=_PCF.SaveLogsDateFormat(DateTime.Now);
                                                        if (Locked == "0")
                                                        {
                                                            _PCF.AddFlagForTransferToServer(
                                                                _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                                                S[3].Trim(),
                                                                _PCF.SaveLogsDateFormat(LoadTime),
                                                                _PCF.SaveLogsDateFormat(LoadTime),
                                                                Device.RoomUniqueID,
                                                                Device.InterfaceUniqueID,
                                                                FlagChangeCodes.OwnerOnly,
                                                                FlagActionCodes.addorupdate);
                                                            if (string.IsNullOrEmpty(Comment))
                                                            {
                                                                _PCF.AddFlagForTransferToServer(
                                                                    _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                                                    S[5].Trim(),
                                                                    "",
                                                                    "",
                                                                    Device.RoomUniqueID,
                                                                    Device.InterfaceUniqueID,
                                                                    FlagChangeCodes.OwnerOnly,
                                                                    FlagActionCodes.delete);

                                                            }
                                                            else
                                                            {
                                                                _PCF.AddFlagForTransferToServer(
                                                                    _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                                                    S[5].Trim(),
                                                                    Comment,
                                                                    Comment,
                                                                    Device.RoomUniqueID,
                                                                    Device.InterfaceUniqueID,
                                                                    FlagChangeCodes.OwnerOnly,
                                                                    FlagActionCodes.addorupdate);

                                                            }
                                                        }

                                                        if (Locked == "1")
                                                        {
                                                            _PCF.AddFlagForTransferToServer(
                                                                _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                                                S[2].Trim(),
                                                                _PCF.SaveLogsDateFormat(LoadTime),
                                                                _PCF.SaveLogsDateFormat(LoadTime),
                                                                Device.RoomUniqueID,
                                                                Device.InterfaceUniqueID,
                                                                FlagChangeCodes.OwnerOnly,
                                                                FlagActionCodes.addorupdate);
                                                        }
                                                    }

                                                    
                                                    
                                                }
  
                                                if (S[0].ToUpper().IndexOf('A') > -1 )
                                                {
                                                    string[] CS1 = (string[])Device.CurrentStates;
                                                    string[] LS1 = (string[])Device.LastStates;
                                                    if(CS1[StatIndex]!=LS1[StatIndex])
                                                    {
  
                                                        switch (Device.DeviceType.ToLower())
                                                        {
                                                            case "door lock":
                                                                 if (S[1].ToLower() == "locked")
                                                                 {
                                                                     PluginCommonFunctions.LocalSaveLogs(_PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName, Device.DeviceIdentifier, FlagValue, RawValue, Device);
                                                                 }
                                                                 else
                                                                 {
                                                                     PluginCommonFunctions.LocalSaveLogs(_PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName, Device.DeviceIdentifier, FlagValue, RawValue, Device);
                                                                 }
                                                                 break;

                                                            default:
                                                                PluginCommonFunctions.LocalSaveLogs(_PCF.GetRoomFromUniqueID(Device.RoomUniqueID)+" "+Device.DeviceName, Device.DeviceIdentifier, FlagValue, RawValue, Device);
                                                                break;
                                                        }                                                    
                                                    }
                                                    
                                                }
                                            
                                            }
                                            catch
                                            {

                                            }
                                        }


                                    }
                                }
                            }
                        }
                        continue;
                    }
                    if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionFailed)
                    {
                        OutgoingDataStruct ODS = (OutgoingDataStruct)Value.PluginData.OutgoingDS.Copy();
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

                        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                        PCS2.Command = PluginCommandsToPlugins.StartTimedLoopForData;

                        PCS2.DestinationPlugin = LinkPlugin;
                        PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                        PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                        OutgoingDataStruct T = new OutgoingDataStruct();
                        T.ReplaceableFieldValues = new List<OutgoingDataStruct.ReplaceFieldValues>();
                        OutgoingDataStruct.ReplaceFieldValues RPF = new OutgoingDataStruct.ReplaceFieldValues();
                        RPF.ReplaceFieldValueName = "loadtime";
                        RPF.ReplaceFieldValueValue = "0";
                        RPF.HowToReplace = ReplaceFieldValues_ReplaceFieldValuesType.BracketedByChars;
                        RPF.ReplaceStartingChar = '=';
                        RPF.ReplaceEndingChar = '&';
                        RPF.WhereToFindDataValue = ReplaceFieldValues_DataFieldValueLocation.Json;
                        RPF.DataFieldValueName = "loadtime";
                        T.ReplaceableFieldValues.Add(RPF);

                        RPF.ReplaceFieldValueName = "dataversion";
                        RPF.ReplaceFieldValueValue = "0";
                        RPF.HowToReplace = ReplaceFieldValues_ReplaceFieldValuesType.BracketedByChars;
                        RPF.ReplaceStartingChar = '=';
                        RPF.ReplaceEndingChar = '&';
                        RPF.WhereToFindDataValue = ReplaceFieldValues_DataFieldValueLocation.Json;
                        RPF.DataFieldValueName = "dataversion";
                        T.ReplaceableFieldValues.Add(RPF);

                        T.CommDataControlInfo = new CommDataControlInfoStruct[1];
                        T.CommDataControlInfo[0].CharactersToSend = _PCF.ConvertStringToByteArray("http://$$IPAddress/data_request?id=lu_sdata&loadtime=0&dataversion=0&output_format=jason");
                        T.CommDataControlInfo[0].Method = "Get";
                        T.CommDataControlInfo[0].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                        T.LocalIDTag = "GeneralData";
                        PCS2.OutgoingDS = T.Copy();
                        _PCF.QueuePluginInformationToPlugin(PCS2);
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


        private static bool SetupCommandSetXML(ref DeviceStruct Device)
        {
            FlagAttributes FlagAtt;
            FlagAtt.ArchiveAttributeNames = null;
            FlagAtt.ArchiveAttributeValues = null;
            FlagAtt.FlagAttributeNames = null;
            FlagAtt.FlagAttributeValues = null;
            XmlDocument XML = new XmlDocument();

            try
            {
                
                XML.LoadXml(Device.CommandSet);
            }
            catch
            {
                Device.Object1 = FlagAtt;
                return (false);
            }
            
            try
            {
                XmlNodeList FlagList = XML.GetElementsByTagName("flag");
                foreach (XmlElement e in FlagList)
                {
                    FlagAtt.FlagAttributeNames = new string[e.Attributes.Count];
                    FlagAtt.FlagAttributeValues = new string[e.Attributes.Count];

                    for (int i = 0; i < e.Attributes.Count; i++)
                    {
                        FlagAtt.FlagAttributeNames[i] = e.Attributes[i].Name;
                        FlagAtt.FlagAttributeValues[i] = e.Attributes[i].Value;
                    }

                }

            }
            catch
            {
            }
            
            try
            {
                XmlNodeList ArchiveList = XML.GetElementsByTagName("Archive");
                foreach (XmlElement e in ArchiveList)
                {
                    FlagAtt.ArchiveAttributeNames = new string[e.Attributes.Count];
                    FlagAtt.ArchiveAttributeValues = new string[e.Attributes.Count];

                    for (int i = 0; i < e.Attributes.Count; i++)
                    {
                        FlagAtt.ArchiveAttributeNames[i] = e.Attributes[i].Name;
                        FlagAtt.ArchiveAttributeValues[i] = e.Attributes[i].Value;
                    }

                }

            }
            catch
            {
            }
            
            Device.Object1 = FlagAtt;
            return (true);
            
        }
    
    }
}
