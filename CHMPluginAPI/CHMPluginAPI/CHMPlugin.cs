using System;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using CHMPluginAPICommon;
using CHMPluginAPI;
using Eval3;
using System.Xml.Linq;


//GitHub Stored

namespace Extensions
{
    public static class StringStuff
    {
        /// <summary>
        /// Case insensitive version of String.Replace().
        /// </summary>
        /// <param name="s">String that contains patterns to replace</param>
        /// <param name="oldValue">Pattern to find</param>
        /// <param name="newValue">New pattern to replaces old</param>
        /// <param name="comparisonType">String comparison type</param>
        /// <returns></returns>
        public static string Replace(this string s, string oldValue, string newValue, StringComparison comparisonType)
        {
            if (s == null)
                return null;

            if (String.IsNullOrEmpty(oldValue))
                return s;

            StringBuilder result = new StringBuilder(Math.Min(4096, s.Length));
            int pos = 0;

            while (true)
            {
                int i = s.IndexOf(oldValue, pos, comparisonType);
                if (i < 0)
                    break;

                result.Append(s, pos, i - pos);
                result.Append(newValue);

                pos = i + oldValue.Length;
            }
            result.Append(s, pos, s.Length - pos);

            return result.ToString();
        }

    }


}

namespace CHMPluginAPI
{
    using Extensions;
    using System.Collections;
    using System.Xml.Linq;
    internal delegate void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void InformationCommingFromServerEventHandler(ServerEvents WhichEvent);
    internal delegate void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent);
    internal delegate void WatchdogProcess(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void ShutDownPlugin(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void IncedentFlag(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void CommandEventHandler(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void StartupInfoFromServer(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void CurrentServerStatus(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void AddDBRecord(ServerEvents WhichEvent, PluginEventArgs Value);
    internal delegate void HTMLProcess(PluginEvents WhichEvent, PluginEventArgs Value);

    internal struct PendingFlagQueueStruct
    {
        internal string FlagName;
        internal string FlagSubType;
    }

    internal class DynamicHTMLAttributes
    {
        internal string ID;
        internal string Flag;
        internal string Case;
        internal string LiteralCase;
        internal string Mode;
        internal string LiteralMode;
        internal string Color;
        internal string Text;
        internal string TextColor;
        internal string Default;
    }

    internal class DynamicHTMLDataStruct
    {
        internal string CurrentFlagValue;
        internal DateTime CurrentFlagValueUpdateTime;
        internal string LastFlagValue;
        internal DateTime LastFlagValueUpdateTime;
        internal string Id;
        internal string Literal;
        internal string Flag;
        internal bool NoFlagDisplay;
        internal bool UseRawFlagValue;
        internal List<DynamicHTMLAttributes> DisplayAttributes;
    }

    internal class PluginEventArgs : EventArgs
    {
        public string StringValue;
        public string StringValue2;
        public string[] StringArray;
        public string[] StringArray1;
        public string[] StringArray2;
        public int IntValue;
        public int IntValue2;
        public DateTime DateValue;
        public int MiliSecondSpan;
        public HeartbeatTimeCode HeartBeatTC;
        public HeartbeatTimeCode HeartbeatTimeCode;
        public ServerStatusCodes OldServerStatusCode;
        public ServerStatusCodes NewServerStatusCode;
        public PluginCommunicationStruct PluginData;
        public PluginCommunicationStruct OldPluginData;
        public PluginServerDataStruct ServerData;
        public PluginServerDataStruct OldServerData;
        public object Object;
        public PluginIncedentFlags IncedentFlags;

    }

    internal class ServerAccessFunctions
    {
        internal static _PluginCommonFunctions PluginCommonFunctions;
        internal static event HeartbeatServerEventHandler _HeartbeatServerEvent;
        internal static event TimeEventServerEventHandler _TimeEventServerEvent;
        internal static event InformationCommingFromServerEventHandler _InformationCommingFromServerServerEvent;
        internal static event InformationCommingFromPluginEventHandler _InformationCommingFromPluginServerEvent;
        internal static event WatchdogProcess _WatchdogProcess;
        internal static event StartupInfoFromServer _StartupInfoFromServer;
        internal static event AddDBRecord _AddDBRecord;
        internal static event CurrentServerStatus _CurrentServerStatus;
        internal static event PluginStartupCompleted _PluginStartupCompleted;
        internal static event PluginStartupCompleted __PluginStartupCompleted;
        internal static event PluginStartupInitialize _PluginStartupInitialize;
        internal static event PluginStartupInitialize __PluginStartupInitialize;
        internal static event IncedentFlag _IncedentFlag;
         internal static event ShutDownPlugin _ShutDownPlugin;
        internal static event ShutDownPlugin __ShutDownPlugin;
        internal static event HTMLProcess _HTMLProcess;
        internal static event CommandEventHandler _Command;
 

        static internal ConcurrentQueue<PluginEventArgs> InformationCommingFromServerQueue;
        static internal ConcurrentQueue<PluginEventArgs> PluginInformationCommingFromPluginQueue;
        static internal ConcurrentQueue<PluginEventArgs> FlagCommingFromServerQueue;

        static internal SemaphoreSlim InformationCommingFromServerSlim;
        static internal SemaphoreSlim PluginInformationCommingFromPluginSlim;
        static internal SemaphoreSlim IncedentFlagServerSlim;
        static internal SemaphoreSlim ProcessDeviceXMLScriptFromDataSlim;
        static internal SemaphoreSlim MaintenanceProcessSlim;

        static internal ServerFunctionsStruct ServerFunctions;

        public static String PluginVersion, PluginSerialNumber, PluginDescription, PluginDataDirectory;
        public static PluginStatusStruct PluginStatus;
        internal static bool StartupComplete = false;
        static private DateTime _StartupTime, _StartupCompletedTime;


        private static ConcurrentQueue<FlagArchiveStruct> ChangedFlags; 

        static private DeviceStruct[] Devices;

        static internal DateTime StartupTime
        {
            get
            {
                return _StartupTime;
            }

        }

        static internal DateTime StartupCompletedTime
        {
            get
            {
                return _StartupCompletedTime;
            }

        }



        internal static Tuple<string, string, string>[] GetFlagInListFromServer(string[] FlagValuesToGet)
        {
            return (ServerFunctions.GetFlags(FlagValuesToGet));
        }

        internal static string GetSingleFlagFromServer(string FlagValueToGet)
        {
            return (ServerFunctions.GetSingleFlag(FlagValueToGet));
        }

        internal static FlagDataStruct GetSingleFlagFromServerFull(string FlagValueToGet)
        {
            return (ServerFunctions.GetSingleFlagFromServerFull(FlagValueToGet));
        }

        internal static bool GetDeviceFromDB(string UniqueID, ref DeviceStruct DS, ref RoomStruct Room)
        {
            return (ServerFunctions.GetDeviceFromDB(UniqueID, ref DS, ref Room));
        }

        internal Tuple<string, string, string>[] GetListOfValidCommands(DeviceStruct DV)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            XmlDocument XML = new XmlDocument();
            PluginCommunicationStruct PCS = new PluginCommunicationStruct();
            int ValidCommand = 0;
            List<Tuple<string, string, string>> Values = new List<Tuple<string, string, string>>();

            try
            {
                if (!string.IsNullOrEmpty(DV.XMLConfiguration))
                {
                    XML.LoadXml(DV.XMLConfiguration);
                    XmlNodeList CommandList = XML.SelectNodes("/root/commands/command");
                    if (CommandList.Count == 0)
                        CommandList = XML.SelectNodes("/commands/command");

                    bool RangeEqual = false;
                    string SpecialRangeEqual = "";

                    foreach (XmlElement el in CommandList)
                    {
                        string State = "", SubField = null, RangeStart = "", RangeEnd = "", FlagValueToUse = "";
                        for (int i = 0; i < el.Attributes.Count; i++)
                        {
                            if (el.Attributes[i].Name.ToLower() == "state")
                            {
                                State = el.Attributes[i].Value;
                                ValidCommand = 2;
                            }

                            if (el.Attributes[i].Name.ToLower() == "subfield")
                            {
                                SubField = el.Attributes[i].Value.ToLower();
                            }

                            if (el.Attributes[i].Name.ToLower() == "rangestart")
                            {
                                RangeStart = el.Attributes[i].Value;
                                ValidCommand++;
                            }

                            if (el.Attributes[i].Name.ToLower() == "rangeend")
                            {
                                RangeEnd = el.Attributes[i].Value;
                                ValidCommand++;
                            }

                            if (el.Attributes[i].Name.ToLower() == "flagvaluetouse")
                            {
                                FlagValueToUse = el.Attributes[i].Value;
                            }
                        }

                        if (RangeStart == RangeEnd && !string.IsNullOrEmpty(RangeStart))
                        {
                            RangeEqual = true;
                            SpecialRangeEqual = RangeStart;
                            continue;
                        }

                        if (ValidCommand >= 2) //A Valid Choice
                        {
                            if (RangeStart != "" && RangeEnd != "") //Range Value
                            {
                                int Minimum, Maximum;
                                Minimum = _PCF.ConvertToInt32(RangeStart);
                                Maximum = _PCF.ConvertToInt32(RangeEnd);
                                if (RangeEqual)
                                {
                                    int r = _PCF.ConvertToInt32(SpecialRangeEqual);
                                    if (r < Minimum)
                                        Minimum = r;
                                    if (r > Maximum)
                                        Maximum = r;
                                }

                                for(int i=Minimum; i<=Maximum;i++)
                                {
                                    Values.Add(new Tuple<string, string, string>( i.ToString(), DV.DeviceUniqueID, SubField));
                                }
                            }
                            else //fixed state
                            {
                                Values.Add(new Tuple<string, string, string>(State, DV.DeviceUniqueID, SubField));
                            }
                        }
                    }
                }
                else
                {
                }
                return (Values.ToArray());
            }
            catch
            {
                return (Values.ToArray());
            }
        }

        internal bool GetAutomation(string AutomationName, string AutomationType, out Tuple<string, string, string> AutomationValues)
        {
            AutomationValues = ServerFunctions.GetAutomation(AutomationName, AutomationType);
            if (AutomationValues == null)
                return (false);
            return (true);
        }


        internal bool ProcessCommandMacro(string Macro, bool SendInfoToServer)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            try
            {
                string MacroXML = ServerFunctions.GetMacro(Macro, "Command", "");
                if (MacroXML == "")
                {
                    return (false);
                }
                if (SendInfoToServer)
                {
                    PluginServerDataStruct PDS = new PluginServerDataStruct();
                    PDS.Command = ServerPluginCommands.ProcessWordMacroCompleted;
                    PDS.String = Macro;
                    PDS.Bool = false;
                    _PCF.QueuePluginInformationToServer(PDS);
                }
                XmlDocument XML = new XmlDocument();
                XML.LoadXml(MacroXML);
                XmlNodeList Macrolist = XML.SelectNodes("/macro");
                int CommandsSent = 0;
                foreach (XmlElement nl in Macrolist)
                {
                    foreach (XmlElement e in nl)
                    {
                        string type = e.Name.ToLower();
                        string Name = "", Device = "", Command = "", Flag="";
                        for (int i = 0; i < e.Attributes.Count; i++)
                        {
                            bool Resultb = false;
                            switch (e.Attributes[i].Name.ToLower())
                            {
                                case "name":
                                    Name = e.Attributes[i].Value;
                                    break;

                                case "device":
                                    Device = e.Attributes[i].Value.ToLower();
                                    break;

                                case "command":
                                    Command = e.Attributes[i].Value.ToLower();
                                    break;

                                case "flag":
                                    Flag = e.Attributes[i].Value.ToLower();
                                    break;
                            }
                        }

                        if (type == "htmlaction")
                        {
                            FlagDataStruct FD;
                            if (string.IsNullOrEmpty(Flag))
                                FD = new FlagDataStruct();
                            else
                            {
                                FD = ServerAccessFunctions.GetSingleFlagFromServerFull(Flag);
                            }
                            ProcessButtonMacro(FD, Name, SendInfoToServer);

                        }
                        if (type == "devicecommand")
                        {
                            DeviceStruct DV = null, DBN ;
                            RoomStruct RM = new RoomStruct();
                            if (_PluginCommonFunctions.LocalDevicesByName.TryGetValue(Device, out DBN))
                            {
                                if(ServerAccessFunctions.GetDeviceFromDB(DBN.DeviceUniqueID, ref DV, ref RM))
                                {
                                    InterfaceStruct IS  = Array.Find(_PluginCommonFunctions.Interfaces, c => c.InterfaceUniqueID == DBN.InterfaceUniqueID);
                                    if(IS!=null)
                                    {
                                        PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                                        PCS.Command = PluginCommandsToPlugins.ProcessCommandWords;
                                        PCS.DeviceUniqueID = DV.DeviceUniqueID;
                                        PCS.String = DV.DeviceUniqueID;
                                        PCS.String2 = Command;
                                        PCS.String3 = "";
                                        PCS.UniqueNumber = string.Format("{0:0000}-{1:0000000000}", _PCF.PluginIDCode, _PCF.NextSequence);
                                        PCS.OriginPlugin = ServerAccessFunctions.PluginSerialNumber;
                                        PCS.DestinationPlugin = IS.ControllingDLL;
                                        ServerFunctions.RunDirectCommand(ref PCS, DV);
                                        if (SendInfoToServer)
                                        {
                                            PluginServerDataStruct PDS = new PluginServerDataStruct();
                                            PDS.ReferenceObject=(Tuple<string, string>) Tuple.Create(DV.DeviceUniqueID, Command);
                                            PDS.Command = ServerPluginCommands.ProcessMacroDeviceCommandCompleted;
                                            PDS.String = Macro;
                                            PDS.String2 = Command;
                                            PDS.Bool = false;
                                            _PCF.QueuePluginInformationToServer(PDS);
                                        }

                                    }

                                }
                            }
                        }
                    }
                }
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (false);
            }
        }

        internal static bool ProcessButtonMacro(FlagDataStruct Flag, string Macro, bool SendInfoToServer)
        {
            return (ProcessButtonMacro(Flag, Macro, "", SendInfoToServer));
        }

        internal static bool ProcessButtonMacro(FlagDataStruct Flag, string Macro, string ReplacementValue, bool SendInfoToServer)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            try
            {
                string MacroXML = ServerFunctions.GetMacro(Macro, "HTMLAction", "");
                if (MacroXML == "")
                {
                    return (false);
                }

                if (!string.IsNullOrEmpty(ReplacementValue))
                {
                    MacroXML = MacroXML.Replace("$$REPLACEVALUE", ReplacementValue);
                }

                XmlDocument XML = new XmlDocument();
                XML.LoadXml(MacroXML);
                XmlNodeList Macrolist = XML.SelectNodes("/macro/pushed");
                int CommandsSent = 0;
                foreach (XmlElement e in Macrolist)
                {
                    string NewValue="", CurrentState = "", CommandString = "", CurrentStateMath = "", SetValue = "", NewCurrentStateMath = "", FlagSetRawValue = "", FlagSetValue = "", TheFlag = "";
                    bool Resultb = false;
                    for (int i = 0; i < e.Attributes.Count; i++)
                    {
                        switch (e.Attributes[i].Name.ToLower())
                        {
                            case "currentstate":
                                CurrentState = e.Attributes[i].Value.ToLower();
                                break;

                            case "commandstring":
                                CommandString = e.Attributes[i].Value;
                                break;

                            case "currentstatemath":
                                CurrentStateMath = e.Attributes[i].Value;
                                break;

                            case "setvalue":
                                SetValue = e.Attributes[i].Value;
                                break;

                            case "newcurrentstatemath":
                                NewCurrentStateMath = e.Attributes[i].Value;
                                break;

                            case "flagsetrawvalue":
                                FlagSetRawValue = e.Attributes[i].Value;
                                break;
                            case "flagsetvalue":
                                FlagSetValue = e.Attributes[i].Value;
                                break;

                            case "alwaysprocess":
                               Resultb = true;
                                break;

                            case "flag":
                                TheFlag= e.Attributes[i].Value;
                                break;

                            case "newvalue":
                                NewValue = e.Attributes[i].Value;
                                break;
                        }
                    }
                    if(!string.IsNullOrEmpty(TheFlag))
                    {
                        Flag = ServerAccessFunctions.GetSingleFlagFromServerFull(TheFlag);
                    }

                    if (!string.IsNullOrEmpty(CurrentStateMath))
                    {
                        string Result;
                        _PCF.DoMathEquations(CurrentStateMath, Flag.Value.ToLower(), Flag.RawValue.ToLower(), null, out Result);
                        if (Result.ToLower() == "true")
                        {
                            Resultb = true;
                        }

                    }

                    if (!string.IsNullOrEmpty(NewCurrentStateMath))
                    {
                        string Result;
                        _PCF.DoMathEquations(NewCurrentStateMath, Flag.Value.ToLower(), Flag.RawValue.ToLower(), null, out Result);
                        SetValue = Result;
                    }


                    if (Flag.Value != null)
                    {
                        if (CurrentState == Flag.Value.ToLower() || Resultb)
                        {
                            DeviceStruct Device;

                            if (!string.IsNullOrEmpty(FlagSetRawValue) || !string.IsNullOrEmpty(FlagSetValue))//Direct Set of Flag
                            {
                                if (string.IsNullOrEmpty(FlagSetRawValue))
                                    FlagSetRawValue = Flag.RawValue;

                                if (string.IsNullOrEmpty(FlagSetValue))
                                    FlagSetValue = Flag.Value;

                                _PCF.AddFlagForTransferToServer(
                                    Flag.Name,
                                    Flag.SubType,
                                    FlagSetValue,
                                    FlagSetRawValue,
                                    Flag.RoomUniqueID,
                                    Flag.SourceUniqueID,
                                    Flag.ChangeMode,
                                    FlagActionCodes.addorupdate,
                                    "");

                                if (SendInfoToServer)
                                {
                                    PluginServerDataStruct PDS = new PluginServerDataStruct();
                                    List <Tuple <string, string>> DC = new List<Tuple<string, string>>();
                                    DC.Add(Tuple.Create(Flag.Name.Trim(), Flag.Value.Trim()));


                                    PDS.ReferenceObject = DC;
                                    PDS.Command = ServerPluginCommands.ProcessWordFlagCompleted;
                                    PDS.String = Macro;
                                    PDS.String2 = NewValue;
                                    PDS.Bool = false;
                                    _PCF.QueuePluginInformationToServer(PDS);
                                }

                            }

                            if (_PluginCommonFunctions.LocalDevicesByUnique.TryGetValue(Flag.SourceUniqueID, out Device))
                            {
                                PluginCommunicationStruct PCS = new PluginCommunicationStruct();

                                PCS.Command = PluginCommandsToPlugins.DirectCommand;
                                PCS.DeviceUniqueID = Flag.SourceUniqueID;

                                PCS.String = Flag.Name;
                                PCS.String2 = SetValue;
                                PCS.String3 = CommandString;
                                PCS.UniqueNumber = string.Format("{0:0000}-{1:0000000000}", _PCF.PluginIDCode, _PCF.NextSequence);
                                PCS.OriginPlugin = ServerAccessFunctions.PluginSerialNumber;
                                if (ServerFunctions.RunDirectCommand(ref PCS, Device))
                                    CommandsSent++;

                                if (SendInfoToServer)
                                {
                                    PluginServerDataStruct PDS = new PluginServerDataStruct();
                                    PDS.ReferenceObject = (Tuple<string, string>)Tuple.Create(Device.DeviceUniqueID, NewValue);
                                    PDS.Command = ServerPluginCommands.ProcessMacroDeviceCommandCompleted;
                                    PDS.String = Macro;
                                    PDS.String2 = NewValue;
                                    PDS.Bool = false;
                                    _PCF.QueuePluginInformationToServer(PDS);
                                }

                            }

                        }
                    }
                }
                if (CommandsSent == 0)
                    return (false);
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (false);
            }
        }



        /// <summary>
        /// PluginStartupCompleted
        /// </summary>
        /// <param name="WhichEvent"></param>
        /// <param name="Value"></param>
        private static void ___PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {
                _PluginCommonFunctions.LocalDevicesByDeviceIdentifier = new ConcurrentDictionary<string, DeviceStruct>();
                _PluginCommonFunctions.LocalDevicesByUnique = new ConcurrentDictionary<string, DeviceStruct>();
                _PluginCommonFunctions.LocalDevicesByName = new ConcurrentDictionary<string, DeviceStruct>();

                _PluginCommonFunctions.LocalRooms = new List<Tuple<string, string, int>>();
                _PluginCommonFunctions.LocalCategories = new List<Tuple<string, string, int>>();
                _PluginCommonFunctions.LocalDeviceTemplates = new List<DeviceTemplateStruct>();
                PluginEventArgs e = new PluginEventArgs();
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                XMLDeviceScripts XMLScripts = new XMLDeviceScripts();

                if (!_PCF.GetStartupField("NumberOfFlagChangesToStore", out _PluginCommonFunctions.NumberOfFlagChangesToStore))
                {
                    string S;
                    _PCF.GetCHMStartupField("FlagChangeHistoryMaxSize", out S, "1000");
                    _PCF.GetCHMStartupField("OffLineName", out _PluginCommonFunctions._OffLineName, "Off-Line");
                    _PluginCommonFunctions.NumberOfFlagChangesToStore= _PCF.ConvertToInt32(S);
                }
        


                foreach (Tuple<string, string, string, string> RM in _PluginCommonFunctions.Rooms)
                {
                    _PluginCommonFunctions.LocalRooms.Add(new Tuple<string, string, int>(RM.Item1, RM.Item2, -1));
                }
                _PluginCommonFunctions.RoomArray = _PluginCommonFunctions.LocalRooms.ToArray();
                for (int i = 0; i < _PluginCommonFunctions.RoomArray.Length; i++)
                {
                    Tuple<string, string, int> R = Tuple.Create(_PluginCommonFunctions.RoomArray[i].Item1, _PluginCommonFunctions.RoomArray[i].Item2.ToLower() + " ", _PluginCommonFunctions.RoomArray[i].Item3);
                    _PluginCommonFunctions.RoomArray[i] = R;

                }
                Array.Sort(_PluginCommonFunctions.RoomArray, ((x, y) => y.Item2.Length.CompareTo(x.Item2.Length)));


                foreach (DeviceStruct DV in Devices)
                {
                    string[] Status = DV.XMLConfiguration.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    DeviceStruct DVX = DV.DeepCopy();

                    DVX.Local_TableLoc = -1;
                    DVX.IsDeviceOffline = false;
                    XMLScripts.SetupXMLConfiguration(ref DVX);
                    DVX.Local_Flag1 = false;
                    _PluginCommonFunctions.AddLocalDevice(DVX);
                }

                foreach (DeviceTemplateStruct DT in _PluginCommonFunctions.DeviceTemplates)
                {
                    _PluginCommonFunctions.LocalDeviceTemplates.Add(DT);
                }

                try
                {
                    if (_PluginStartupCompleted != null)
                        _PluginStartupCompleted.Invoke(ServerEvents.StartupCompleted, e);
                    StartupComplete = true;

                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;

        }

        private static void ___PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();

                try
                {
                    if (_PluginStartupInitialize != null)
                        _PluginStartupInitialize.Invoke(ServerEvents.StartupInitialize, e);
                    StartupComplete = true;

                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;

        }





        private static void ___IncedentFlag(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();

                try
                {
                    if (_IncedentFlag != null)
                    {
                        IncedentFlagServerSlim.Wait();
                        _IncedentFlag.Invoke(ServerEvents.IncedentFlag, e);
                        IncedentFlagServerSlim.Release();
                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;
        }

        private static void ___ShutDownPlugin(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();

                try
                {
                    if (_ShutDownPlugin != null)
                        _ShutDownPlugin.Invoke(ServerEvents.ShutDownPlugin, e);
                    if (_PluginDatabaseAccess.DBData.DB != null && _PluginDatabaseAccess.DBData.DB.State == System.Data.ConnectionState.Open)
                        _PluginDatabaseAccess.DBData.DB.Close();
                }
                catch
                {
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;
        }

        public void CHMAPI_QueuesCommingFromServer(object[] Queues)
        {
            ChangedFlags=(ConcurrentQueue<FlagArchiveStruct>)Queues[0];
        }


        public void CHMAPI_ServerFunctions(ServerFunctionsStruct _ServerGetFlagsInList)
        {
            ServerFunctions = _ServerGetFlagsInList.DeepCopy();
        }


        public int CHMAPI_InitializePlugin(int Uniqueid, DateTime CT, string PluginFileDirectory)
        {
            try
            {
                PluginCommonFunctions = new _PluginCommonFunctions();

                PluginDataDirectory = PluginFileDirectory;
                PluginCommonFunctions.ActivatePlugin(Uniqueid);
                _StartupTime = CT;
                InformationCommingFromServerQueue = new ConcurrentQueue<PluginEventArgs>();
                PluginInformationCommingFromPluginQueue = new ConcurrentQueue<PluginEventArgs>();
                FlagCommingFromServerQueue = new ConcurrentQueue<PluginEventArgs>();
                InformationCommingFromServerSlim = new SemaphoreSlim(1, 1);
                PluginInformationCommingFromPluginSlim = new SemaphoreSlim(1, 1);
                IncedentFlagServerSlim = new SemaphoreSlim(1, 1);
                 ProcessDeviceXMLScriptFromDataSlim = new SemaphoreSlim(1, 1);
                MaintenanceProcessSlim = new SemaphoreSlim(1, 1);


                //throw new IndexOutOfRangeException();
                ServerAccessFunctions.__PluginStartupCompleted += ___PluginStartupCompleted;
                ServerAccessFunctions.__PluginStartupInitialize += ___PluginStartupInitialize;
                ServerAccessFunctions.__ShutDownPlugin += ___ShutDownPlugin;
                return (0);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (-1);
            }

        }

        public bool CHMAPI_GetUnexpectedErrors(out PluginErrorMessage UnexpectedError)
        {
            try
            {
                return (_PluginCommonFunctions.UnexpectedErrorDeEnqueue(out UnexpectedError));
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                UnexpectedError.ExceptionData = null;
                UnexpectedError.Comment = null;
                UnexpectedError.DateTimeOfException = _PluginCommonFunctions.CurrentTime;
                return (false);
            }
        }

        public int CHMAPI_InformationCommingFromServerServer(DateTime CurrentTime, PluginServerDataStruct Data)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();
                e.DateValue = CurrentTime;
                e.ServerData = Data;
                //if (!string.IsNullOrEmpty(Data.ReferenceUniqueNumber))
                //    _PluginCommonFunctions.ServerCommunicationSentDictionary.TryGetValue(Data.ReferenceUniqueNumber, out e.OldServerData);
                try
                {
                    if (_InformationCommingFromServerServerEvent != null)
                        InformationCommingFromServerQueue.Enqueue(e);

                    if (_InformationCommingFromServerServerEvent != null && StartupComplete)
                        _InformationCommingFromServerServerEvent.BeginInvoke(ServerEvents.InformationCommingFromServer, null, null);
                    else
                    {
                    }
                }
                catch
                {

                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);

            }
            return (0);
        }

        public bool CHMAPI_PluginInformationGoingToServer(out PluginServerDataStruct Data)
        {
            PluginServerDataStruct PIS = new PluginServerDataStruct();
            try
            {
                bool flag = PluginCommonFunctions.DeQueuePluginInformationToServer(out PIS);
                Data = PIS;
                return (flag);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Data = PIS;
                return (false);
            }

        }

        public void CHMAPI_PluginInformationCommingFromPlugin(DateTime CurrentTime, PluginCommunicationStruct Data)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();
                e.DateValue = CurrentTime;
                e.PluginData = Data;
                if (!string.IsNullOrEmpty(Data.ReferenceUniqueNumber))
                    _PluginCommonFunctions.PluginCommunicationSentDictionary.TryGetValue(Data.ReferenceUniqueNumber, out e.OldPluginData);
                try
                {
                    if (_InformationCommingFromPluginServerEvent != null)
                        PluginInformationCommingFromPluginQueue.Enqueue(e);

                    if (_InformationCommingFromPluginServerEvent != null && StartupComplete)
                        _InformationCommingFromPluginServerEvent.BeginInvoke(ServerEvents.InformationCommingFromPlugin, null, null);
                    else
                    {
                    }

                }
                catch
                {
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return;

        }

        public bool CHMAPI_PluginStartupInitialize()
        {
            PluginEventArgs e = new PluginEventArgs();
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;
            __PluginStartupInitialize.BeginInvoke(ServerEvents.StartupInitialize, e, null, null);
            Thread.Sleep(1000);
            return (true);

        }

        public bool CHMAPI_PluginStartupCompleted()
        {
            PluginEventArgs e = new PluginEventArgs();
            _StartupCompletedTime = _PluginCommonFunctions.CurrentTime;
            __PluginStartupCompleted.BeginInvoke(ServerEvents.StartupCompleted, e, null, null);
            return (true);

        }

        public bool CHMAPI_HTMLProcess()
        {
            PluginEventArgs e = new PluginEventArgs();
            _HTMLProcess.BeginInvoke(PluginEvents.ProcessWebpage, e, null, null);
            return (true);

        }




        public bool CHMAPI_PluginInformationGoingToPlugin(out PluginCommunicationStruct Data)
        {
            PluginCommunicationStruct PIS = new PluginCommunicationStruct();
            Data = PIS;
            try
            {
                try
                {
                    PIS = new PluginCommunicationStruct();
                    bool flag = PluginCommonFunctions.DeQueuePluginInformationToPlugin(out PIS);
                    Data = PIS;
                    return (flag);
                }
                catch
                {

                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (false);
            }
            return (false);
        }


        public bool CHMAPI_ShutDownPlugin()
        {
            PluginEventArgs e = new PluginEventArgs();
            __ShutDownPlugin.BeginInvoke(ServerEvents.ShutDownPlugin, e, null, null);
            return (true);
        }

        public bool CHMAPI_IncedentFlag(PluginIncedentFlags IncFlag, Object IncedentObject)
        {
            PluginEventArgs e = new PluginEventArgs();
            e.Object = IncedentObject;
            e.IncedentFlags = IncFlag;

            _IncedentFlag.BeginInvoke(ServerEvents.IncedentFlag, e, null, null);
            return (true);

        }

         public bool CHMAPI_WatchdogProcess()
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();

                PluginStatus.SetFlagCount = _PluginCommonFunctions.CountNewFlagQueue;
                PluginStatus.ToPluginCount = _PluginCommonFunctions.CountPluginCommunicationQueue;
                PluginStatus.ToServerCount = _PluginCommonFunctions.CountServerCommunicationQueue;
                PluginStatus.NumberOfUEErrorsCount = _PluginCommonFunctions.CountNumberOfUEErrorsQueue;


                try
                {
                    if (_WatchdogProcess != null)
                        _WatchdogProcess.BeginInvoke(ServerEvents.WatchdogProcess, e, null, null);
                }
                catch
                {
                }

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return (true);
        }

        public bool CHMAPI_SetFlagOnServer(out NewFlagStruct Flag, out Tuple<string, string, DateTime> Events)
        {
            try
            {
                return (PluginCommonFunctions.GetFlagFromQueue(out Flag, out Events));
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);

                NewFlagStruct Lflag = new NewFlagStruct();
                Flag = Lflag;
                Events = new Tuple<string, string, DateTime>("", "", DateTime.MinValue);
                return (false);
            }
        }

        public void CHMAPI_Heartbeat(DateTime CT, int MSSinceLastHeartbeat, HeartbeatTimeCode HeartBeatTC, ServerStatusCodes ServerStatus)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();
                e.DateValue = CT;
                e.MiliSecondSpan = MSSinceLastHeartbeat;
                e.HeartBeatTC = HeartBeatTC;
                try
                {
                    if (_HeartbeatServerEvent != null && StartupComplete)
                        _HeartbeatServerEvent.BeginInvoke(ServerEvents.Heartbeat, e, null, null);
                }
                catch
                {
                }


                if (PluginCommonFunctions.CurrentServerStatus != ServerStatus)
                {
                    PluginEventArgs e3 = new PluginEventArgs();
                    e3.OldServerStatusCode = PluginCommonFunctions.CurrentServerStatus;
                    e3.NewServerStatusCode = ServerStatus;
                    PluginCommonFunctions._ServerCurrentServerStatus = ServerStatus;
                    try
                    {
                        if (_CurrentServerStatus != null)
                            _CurrentServerStatus.BeginInvoke(ServerEvents.CurrentServerStatus, e3, null, null);
                    }
                    catch (Exception CHMAPIEx)
                    {
                        _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                        _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    }
                }

                //Now We Reset Any Flags That Need resetting
                if (HeartBeatTC >= HeartbeatTimeCode.NewSecond)
                {
                    XMLDeviceScripts XMLScripts = new XMLDeviceScripts();
                    DateTime CurrentTime = _PluginCommonFunctions.CurrentTime;
                    while (_PluginCommonFunctions.FlagsNeedingReset.Count > 0)
                    {
                        if (_PluginCommonFunctions.FlagsNeedingReset.ElementAt(0).Key <= CurrentTime)
                        {
                            _PluginCommonFunctions.FlagResetStruct FSR;
                            if (_PluginCommonFunctions.FlagsNeedingReset.TryRemove(_PluginCommonFunctions.FlagsNeedingReset.ElementAt(0).Key, out FSR))
                            {
                                XMLScripts.ProcessDeviceXMLScriptFromDataBySequence(ref FSR.Device, null, XMLDeviceScripts.DeviceScriptsDataTypes.NoData, FSR.SequenceCode);
                                continue;
                            }
                            else
                                break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    MaintenanceStruct MA = null;
                    XMLScripts.ProcessXMLMaintanenceInformation(ref MA, "", "", MaintanenceCommands.DoTasks);



                    if (PluginCommonFunctions.HeartbeatTimeCodeToInvoke == HeartbeatTimeCode.Nothing)
                        return;
                    if (HeartBeatTC == HeartbeatTimeCode.Nothing)
                        return;
                    if ((int)HeartBeatTC >= (int)PluginCommonFunctions.HeartbeatTimeCodeToInvoke)
                    {
                        PluginEventArgs e2 = new PluginEventArgs();
                        e2.HeartBeatTC = HeartBeatTC;
                        e2.HeartbeatTimeCode = HeartBeatTC;
                        try
                        {
                            if (_TimeEventServerEvent != null)
                                _TimeEventServerEvent.BeginInvoke(ServerEvents.TimeEvent, e2, null, null);
                        }
                        catch
                        {
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


        public bool CHMAPI_AddDBRecord(string[] DBRecord, string DBOrigin)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();

                string[] NewStringArray = new string[DBRecord.Length + 1];
                DBRecord.CopyTo(NewStringArray, 1);
                NewStringArray[0] = DBOrigin;
                PluginCommonFunctions._AddDBRecord(NewStringArray);
                try
                {
                    if (_AddDBRecord != null)
                        _AddDBRecord.BeginInvoke(ServerEvents.Heartbeat, e, null, null);
                }
                catch
                {
                }


            }
            catch (Exception CHMAPIEx)
            {

                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return (true);
        }


        public bool CHMAPI_StartupInfoFromServer(object[] Stuff)
        {
            try
            {
                PluginEventArgs e = new PluginEventArgs();
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

                string[] FieldName = (string[])Stuff[4];
                string[] FieldValue = (string[])Stuff[5];
                string[] DBRecord = (string[])Stuff[6];
                string[] SubField = (string[])Stuff[8];
                string[] CHMFieldName = (string[])Stuff[14];
                string[] CHMFieldValue = (string[])Stuff[16];
                string[] CHMSubField = (string[])Stuff[15];


                if (FieldName != null && FieldValue != null && FieldName.Length != 0 && FieldValue.Length != 0)
                {
                    int Values = PluginCommonFunctions._LoadStartupFields(FieldName, SubField, FieldValue);
                }

                if (CHMFieldName != null && CHMFieldValue != null && CHMFieldName.Length != 0 && CHMFieldValue.Length != 0)
                {
                    int Values = PluginCommonFunctions._LoadCHMStartupFields(CHMFieldValue, CHMSubField, CHMFieldValue);
                }
                string[] NewStringArray = new string[DBRecord.Length + 1];
                DBRecord.CopyTo(NewStringArray, 1);
                NewStringArray[0] = (string)Stuff[7];
                PluginCommonFunctions._AddDBRecord(NewStringArray);
                e.StringArray = FieldName;
                e.StringArray1 = FieldValue;
                e.StringArray2 = NewStringArray;


                try
                {
                    Devices = (DeviceStruct[])Stuff[0];
                    _PluginCommonFunctions.Rooms = new List<Tuple<string, string, string, string>>();
                    _PluginCommonFunctions.Rooms.AddRange((Tuple<string, string, string, string>[])Stuff[1]);
                    _PluginCommonFunctions.Interfaces = (InterfaceStruct[])Stuff[2];
                    string N = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name+".dll";
                    _PluginCommonFunctions.LocalInterface = Array.Find(_PluginCommonFunctions.Interfaces, c => c.ControllingDLL == N);
                    if (_PluginCommonFunctions.LocalInterface == null)
                        _PluginCommonFunctions.LocalInterface = new InterfaceStruct();
                    _PluginCommonFunctions.Passwords = (PasswordStruct[])Stuff[3];
                    _PluginCommonFunctions.GenerateStatusDictionary((StatusMessagesStruct[])Stuff[9]);
                    _PluginCommonFunctions.DeviceTemplates = (DeviceTemplateStruct[])Stuff[10];
                    _PluginCommonFunctions.SystemFlags = (SystemFlagStruct[])Stuff[11];
                    _PluginCommonFunctions.DBPassword = (string)Stuff[13];

                    _PluginCommonFunctions.UOM = new SortedList<int, Tuple<string, string, string, Tuple<int, string>[]>>();

                    for (int i = 0; i < Devices.Length; i++)
                    {
                        if (string.IsNullOrEmpty(Devices[i].NativeDeviceIdentifier))
                            Devices[i].NativeDeviceIdentifier = Devices[i].DeviceIdentifier;
                        if (string.IsNullOrEmpty(Devices[i].NativeDeviceIdentifier))
                            Devices[i].NativeDeviceIdentifier = _PluginCommonFunctions.UniqueNumber.ToString();
                    }




                    foreach (KeyValuePair<int, Tuple<string, string, string>> K in (KeyValuePair<int, Tuple<string, string, string>>[])Stuff[12])
                    {
                        Tuple<int, string>[] S = null;
                        if (!string.IsNullOrEmpty(K.Value.Item3))
                        {
                            string[] SS = K.Value.Item3.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            S = new Tuple<int, string>[SS.Count()];
                            for (int x = 0; x < SS.Count(); x++)
                            {
                                int q = SS[x].IndexOf('=');
                                if (q > -1)
                                {
                                    S[x] = new Tuple<int, string>(_PCF.ConvertToInt32(SS[x].Substring(0, q)), SS[x].Substring(q + 1).Trim());
                                }
                            }

                        }
                        _PluginCommonFunctions.UOM.Add(K.Key, new Tuple<string, string, string, Tuple<int, string>[]>(K.Value.Item1, K.Value.Item2, K.Value.Item3, S));
                    }
                }
                catch
                {
                }

                try
                {
                    if (_StartupInfoFromServer != null)
                        _StartupInfoFromServer.BeginInvoke(ServerEvents.StartupInfo, e, null, null);
                }
                catch
                {
                }

                return (true);
            }
            catch (Exception CHMAPIEx)
            {

                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (false);
            }
        }

        internal bool GetAnyUpdatedFlagsSentFromServer(out FlagArchiveStruct Flag)
        {
            return(ChangedFlags.TryDequeue(out Flag));
        }
    }

    internal class _PluginCommonFunctions
    {

        static private int _PluginIDCode;
        static private DateTime _CurrentTime;
        static private ConcurrentQueue<NewFlagStruct> NewFlagQueue;
        static private ConcurrentQueue<Tuple<string, string, DateTime>> EventsQueue;
        //static private ConcurrentDictionary<string, PluginFlagDataStruct> PluginFlagDataDictionary;
        static private ConcurrentQueue<PluginCommunicationStruct> PluginCommunicationQueue;
        static public ConcurrentDictionary<string, PluginCommunicationStruct> PluginCommunicationSentDictionary;
        static private ConcurrentQueue<PluginServerDataStruct> ServerCommunicationQueue;
        //static public ConcurrentDictionary<string, PluginServerDataStruct> ServerCommunicationSentDictionary;
        static private ConcurrentQueue<PluginErrorMessage> PluginErrorMessageQueue;

        static private List<String[]> DBRecords;
        static private List<Tuple<string, string, string>> StartupFields;
        static private string[] CHMStartupFields, CHMStartupValues, CHMStartupSubFields;
        static private int _SequenceVariable = 0;
        static private HeartbeatTimeCode _HeartbeatTimeCodeToInvoke = HeartbeatTimeCode.Nothing;
        static private ServerStatusCodes _CurrentServerStatus = ServerStatusCodes.Unknown;
        static private int _UniqueNumber = 0;

        static internal InterfaceStruct[] Interfaces;
        static internal PasswordStruct[] Passwords;
        static internal DeviceTemplateStruct[] DeviceTemplates;
        static internal StatusMessagesStruct[] StatusMessages;
        static internal SystemFlagStruct[] SystemFlags;
        static internal string DBPassword;
        static internal InterfaceStruct LocalInterface;
        static internal SortedList<int, Tuple<string, string, string, Tuple<int, string>[]>> UOM;
        static internal List<Tuple<string, string, string, string>> Rooms;
        static internal readonly string FlagDateTimeFormat = "{0:0000}:{1:00}:{2:00} {3:00}:{4:00}:{5:00}";
        static internal ConcurrentDictionary<String, StatusMessagesStruct> StatusMessagesDictionary;
        static internal Random random = new Random((int)DateTime.Now.Ticks);
        internal static ConcurrentDictionary<string, DeviceStruct> LocalDevicesByDeviceIdentifier;
        internal static ConcurrentDictionary<string, DeviceStruct> LocalDevicesByUnique;
        internal static ConcurrentDictionary<string, DeviceStruct> LocalDevicesByName;
        internal static List<Tuple<string, string, int>> LocalRooms;
        internal static Tuple<string, string, int>[] RoomArray;
        internal static List<Tuple<string, string, int>> LocalCategories;
        internal static List<DeviceTemplateStruct> LocalDeviceTemplates;
        internal static int NumberOfFlagChangesToStore;


        static private Type AssemblyType = null;
        static private object Instance = null;
        static private Type ServerAssemblyType = null;
        static private object ServerInstance = null;
        static internal string _OffLineName;


        static private Dictionary<string, string> _UserVariables;
        private Evaluator ev;
        static private EvalFunctions EvalMathFunctions;
        internal struct FlagResetStruct
        {
            internal DateTime TimeToResetFlag;
            internal string SequenceCode;
            internal DeviceStruct Device;
        }
        static internal ConcurrentDictionary<DateTime, FlagResetStruct> FlagsNeedingReset;
        static internal SortedDictionary<DateTime, MaintenanceStruct> MaintenanceRequests;

        public Eval3.EvalVariable flagvalue_;
        public Eval3.EvalVariable rawvalue_;
        public Eval3.EvalVariable flagvalue;
        public Eval3.EvalVariable rawvalue;

        internal _PluginCommonFunctions()
        {

        }

        static internal void AddLocalDevice(DeviceStruct DVX)
        {

            try
            {
                if (DVX.StoredDeviceData == null)
                    DVX.StoredDeviceData = new DeviceDataStruct();

                if (DVX.StoredDeviceData.Local_FlagAttributes == null)
                    DVX.StoredDeviceData.Local_FlagAttributes = new List<FlagAttributes>();
                if (DVX.StoredDeviceData.Local_ArchiveFlagAttributes == null)
                    DVX.StoredDeviceData.Local_ArchiveFlagAttributes = new List<FlagAttributes>();
                if (DVX.StoredDeviceData.Local_LookupFlagAttributes == null)
                    DVX.StoredDeviceData.Local_LookupFlagAttributes = new List<FlagAttributes>();
                if (DVX.StoredDeviceData.Local_StatesFlagAttributes == null)
                    DVX.StoredDeviceData.Local_StatesFlagAttributes = new List<string>();
                if (DVX.StoredDeviceData.Local_RawValues == null)
                    DVX.StoredDeviceData.Local_RawValues = new List<Tuple<DateTime, string>>();
                if (DVX.StoredDeviceData.Local_MaintanenceInformation == null)
                    DVX.StoredDeviceData.Local_MaintanenceInformation = new List<MaintenanceStruct>();
                if (DVX.StoredDeviceData.Local_MaintanenceHistory == null)
                    DVX.StoredDeviceData.Local_MaintanenceHistory = new List<Tuple<DateTime, string, bool>>();


                LocalDevicesByDeviceIdentifier.TryAdd(DVX.NativeDeviceIdentifier, DVX);
                LocalDevicesByUnique.TryAdd(DVX.DeviceUniqueID, DVX);
                if (!string.IsNullOrEmpty(DVX.RoomUniqueID) && !string.IsNullOrEmpty(DVX.DeviceName))
                    LocalDevicesByName.TryAdd((LocalRooms.Find(c => c.Item1 == DVX.RoomUniqueID).Item2 + " " + DVX.DeviceName).ToLower(), DVX);
            }
            catch (Exception CHMAPIEx)
            {

                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }



        internal bool DoMathEquationsWithFlags(string Equation, out string Result)
        {
            try
            {
                if (ev == null)
                {
                    ev = new Evaluator(Eval3.eParserSyntax.cSharp, false);
                    EvalMathFunctions = new EvalFunctions();
                    ev.AddEnvironmentFunctions(this);
                    ev.AddEnvironmentFunctions(EvalMathFunctions);
                    flagvalue_ = new EvalVariable("", typeof(string));
                    rawvalue_ = new EvalVariable("", typeof(string));
                }

                if (EvalMathFunctions.ServerFunctions == null)
                {
                    EvalMathFunctions.ServerFunctions = ServerAccessFunctions.ServerFunctions;
                }


                opCode lCode = null;
                EvalMathFunctions.UseFlags = true;
                lCode = ev.Parse(Equation + "\r\n");
                EvalMathFunctions.UseFlags = false;
                object res = null;
                try
                {
                    res = lCode.value;
                }
                catch
                {

                }

                if (res == null)
                {
                    Result = "";
                    return (false);
                }
                else
                {
                    Result = Evaluator.ConvertToString(res);
                    return (true);
                }
            }
            catch (Exception CHMAPIEx)
            {
                AddToUnexpectedErrorQueue(CHMAPIEx, "Math Equation: '" + Equation + "' -Variable '" + "'");
                Result = "";
                return (false);
            }
        }


        internal bool DoMathEquations(string Equation, string _FlagValue, string _RawValue, DeviceStruct Device, out string Result)
        {
            double r;

            try
            {
                if (ev == null)
                {
                    ev = new Evaluator(Eval3.eParserSyntax.cSharp, false);
                    EvalMathFunctions = new EvalFunctions();
                    ev.AddEnvironmentFunctions(this);
                    ev.AddEnvironmentFunctions(EvalMathFunctions);
                    flagvalue_ = new EvalVariable(_FlagValue, typeof(string));
                    rawvalue_ = new EvalVariable(_RawValue, typeof(string));
                }
                else
                {
                    flagvalue_.Value = _FlagValue;
                    rawvalue_.Value = _RawValue;
                }

                EvalMathFunctions.UseFlags = false;
                if (double.TryParse(_FlagValue, out r))
                    flagvalue = new EvalVariable(r, typeof(double));
                else
                    flagvalue = new EvalVariable(_FlagValue, typeof(string));
                if (double.TryParse(_RawValue, out r))
                    rawvalue = new EvalVariable(r, typeof(double));
                else
                    rawvalue = new EvalVariable(_RawValue, typeof(string));

                if (Device != null)
                {
                    KeyValuePair<string, string>[] kvp = GetAllUserValuesBySubset(Device.InterfaceUniqueID + "_");
                    if (kvp.Length > 0)
                    {
                        foreach (KeyValuePair<string, string> V in kvp)
                        {
                            if (V.Key.Last() == '_')
                            {
                                EvalMathFunctions.AddLocalVariable(V.Key.Substring(Device.InterfaceUniqueID.Length + 1), V.Value, "$");
                            }
                            else
                            {
                                if (double.TryParse(V.Value, out r))
                                    EvalMathFunctions.AddLocalVariable(V.Key.Substring(Device.InterfaceUniqueID.Length + 1), V.Value, "#");
                                else
                                    EvalMathFunctions.AddLocalVariable(V.Key.Substring(Device.InterfaceUniqueID.Length + 1), V.Value, "$");

                                EvalMathFunctions.AddLocalVariable(V.Key.Substring(Device.InterfaceUniqueID.Length + 1) + "_", V.Value, "$");
                            }
                        }
                    }
                }
                opCode lCode = null;
                lCode = ev.Parse(Equation + "\r\n");

                object res = null;
                try
                {
                    res = lCode.value;
                }
                catch
                {

                }

                if (res == null)
                {
                    Result = "";
                    return (false);
                }
                else
                {
                    Result = Evaluator.ConvertToString(res);
                    return (true);
                }




            }
            catch (Exception CHMAPIEx)
            {
                AddToUnexpectedErrorQueue(CHMAPIEx, "Math Equation: '" + Equation + "' -Variable '" + "'");
                Result = "";
                return (false);
            }

        }


        internal bool DoMathEquations(string Equation, List<Tuple<string, string>> Variables, out bool Result)
        {
            string R;

            if (!DoMathEquations(Equation, Variables, out R))
            {
                Result = false;
                return (false);
            }

            if (R.ToLower() == "true")
            {
                Result = true;
                return true;
            }

            if (R.ToLower() == "false")
            {
                Result = false;
                return true;
            }

            Result = false;
            return (false);
        }


        internal bool DoMathEquations(string Equation, List<Tuple<string, string>> Variables, out double Result)
        {
            string R;
            if (!DoMathEquations(Equation, Variables, out R))
            {
                Result = 0;
                return (false);
            }
            return (Double.TryParse(R, out Result));
        }


        internal bool DoMathEquations(string Equation, List<Tuple<string, string>> Variables, out string Result)
        {
            try
            {
                if (ev == null)
                {
                    ev = new Evaluator(Eval3.eParserSyntax.cSharp, false);
                    EvalMathFunctions = new EvalFunctions();
                    ev.AddEnvironmentFunctions(this);
                    ev.AddEnvironmentFunctions(EvalMathFunctions);
                    flagvalue_ = new EvalVariable("", typeof(string));
                    rawvalue_ = new EvalVariable("", typeof(string));
                }

                opCode lCode = null;
                EvalMathFunctions.ClearLocalVariables();
                EvalMathFunctions.UseFlags = false;

                if (Variables != null) //Add Local Variables
                {
                    foreach (Tuple<string, string> V in Variables)
                    {
                        double r;
                        if (V.Item1.Last() == '_')
                        {
                            EvalMathFunctions.AddLocalVariable(V.Item1, V.Item2, "$");
                        }
                        else
                        {
                            if (double.TryParse(V.Item2, out r))
                                EvalMathFunctions.AddLocalVariable(V.Item1, V.Item2, "#");
                            else
                                EvalMathFunctions.AddLocalVariable(V.Item1, V.Item2, "$");

                            EvalMathFunctions.AddLocalVariable(V.Item1 + "_", V.Item2, "$");
                        }
                    }

                }


                lCode = ev.Parse(Equation + "\r\n");

                object res = null;
                try
                {
                    res = lCode.value;
                }
                catch
                {

                }

                if (res == null)
                {
                    Result = "";
                    return (false);
                }
                else
                {
                    Result = Evaluator.ConvertToString(res);
                    return (true);
                }
            }
            catch (Exception CHMAPIEx)
            {
                AddToUnexpectedErrorQueue(CHMAPIEx, "Math Equation: '" + Equation + "' -Variable '" + "'");
                Result = "";
                return (false);
            }
        }

        internal bool GetPasswordInfo(string UserName, string password, ref PasswordStruct PWFound)
        {
            try
            {
                if (Passwords == null)
                    return (false);

                if (string.IsNullOrEmpty(UserName))
                {
                    if (Passwords.Length > 0)
                    {
                        PWFound = Passwords[0];
                        return (true);
                    }
                    return (false);
                }
                PWFound = Array.Find(Passwords, w => w.Account.ToLower() == UserName.ToLower());
                if (password == PWFound.Password)
                    return (true);
                else
                    return (false);
            }
            catch
            {
                return (false);
            }
        }

        internal string ConvertArrayToCSVRecord(string[] Data)
        {

            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (string value in Data)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                    builder.Append(',');
                builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                firstColumn = false;
            }
            return (builder.ToString());
        }

        internal string[] ConvertCSVRecordtoStringArray(string CSVFile)
        {
            string[] s;

            try
            {
                s = Regex.Split(CSVFile, "\",\"", RegexOptions.Compiled);
                if (s[0] == CSVFile)
                {
                    s = CSVFile.Split(',');
                }
                else
                {
                    if (s[0].StartsWith("\""))
                        s[0] = s[0].Substring(1);
                    if (s[s.Length - 1].EndsWith("\""))
                        s[s.Length - 1] = s[s.Length - 1].Substring(0, s[s.Length - 1].Length - 1);
                }

                return (s);
            }
            catch
            {
                string[] x = new string[1];
                x[0] = CSVFile;
                return (x);
            }


        }



        internal void CopyDeviceTemplateIntoNewDevice(DeviceTemplateStruct DTS, ref DeviceStruct DS)
        {
            DS.DeviceUniqueID = CreateDBUniqueID("D");
            if (!string.IsNullOrEmpty(DTS.Comments))
                DS.Comments = DTS.Comments;
            if (!string.IsNullOrEmpty(DTS.XMLConfiguration))
                DS.XMLConfiguration = DTS.XMLConfiguration;
            if (!string.IsNullOrEmpty(DTS.DeviceClassID))
                DS.DeviceClassID = DTS.DeviceClassID;
            DS.IntVal01 = DTS.IntVal01;
            DS.IntVal01 = DTS.IntVal02;
            DS.IntVal01 = DTS.IntVal03;
            DS.IntVal01 = DTS.IntVal04;
            DS.objVal = DTS.objVal;
            if (!string.IsNullOrEmpty(DTS.StrVal01))
                DS.StrVal01 = DTS.StrVal01;
            if (!string.IsNullOrEmpty(DTS.StrVal02))
                DS.StrVal02 = DTS.StrVal02;
            if (!string.IsNullOrEmpty(DTS.StrVal03))
                DS.StrVal03 = DTS.StrVal03;
            if (!string.IsNullOrEmpty(DTS.StrVal04))
                DS.StrVal04 = DTS.StrVal04;
            if (!string.IsNullOrEmpty(DTS.UndesignatedFieldsInfo))
                DS.UndesignatedFieldsInfo = DTS.UndesignatedFieldsInfo;
            if (!string.IsNullOrEmpty(DTS.UOMCode))
                DS.UOMCode = DTS.UOMCode;
            if (!string.IsNullOrEmpty(DTS.DeviceType))
                DS.DeviceType = DTS.DeviceType;


        }

        internal string CreateDBUniqueID(string Code)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < 12; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(74 * random.NextDouble() + 48)));
                builder.Append(ch);
            }

            return Code.ToUpper() + builder.ToString();
        }

        internal void _AddDBRecord(string[] DBRecord)
        {
            try
            {
                DBRecords.Add(DBRecord);
            }
            catch (Exception CHMAPIEx)
            {

                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }

        internal bool ConvertCharsToInt(char[] Data, string Offset, string Length, out uint IntValue, bool HiByte)
        {
            return (ConvertCharsToInt(Data, ConvertToInt32(Offset), ConvertToInt32(Length), out IntValue, HiByte));
        }

        internal bool ConvertCharsToInt(char[] Data, int Offset, int Length, out uint IntValue, bool HiByte)
        {
            if (Offset < 0 || Length <= 0 || Offset + Length > Data.Length)
            {
                IntValue = 0;
                return (false);
            }
            if (Length == 1)
            {
                IntValue = Data[Offset];
                return (true);
            }

            if (Length == 2 && !HiByte)
            {
                IntValue = (uint)Data[Offset + 1] * 256 + Data[Offset];
                return (true);
            }

            if (Length == 2 && HiByte)
            {
                IntValue = (uint)Data[Offset] * 256 + Data[Offset + 1];
                return (true);
            }

            IntValue = 0;
            return (false);
        }

        static internal int CountNumberOfUEErrorsQueue
        {
            get
            {
                return (ServerCommunicationQueue.Count());
            }
        }

        static internal void GenerateStatusDictionary(StatusMessagesStruct[] StatusMess)
        {
            _PluginCommonFunctions.StatusMessages = StatusMess;

            foreach (StatusMessagesStruct SM in StatusMess)
            {
                StatusMessagesDictionary.TryAdd(SM.StatusCode, SM);
            }
        }

        static internal bool LookupStatusDictionary(string Code, out string Value, out string LogCode)
        {
            string S;
            bool f = LookupStatusDictionary(Code, out Value, out LogCode, out S);
            return (f);
        }

        static internal bool LookupStatusDictionary(string Code, out string Value, out string LogCode, out string Status)
        {
            StatusMessagesStruct SMS;
            bool flag = StatusMessagesDictionary.TryGetValue(Code, out SMS);
            if (!flag)
            {
                Value = null;
                LogCode = null;
                Status = null;
                return (false);
            }
            Value = SMS.StatusMessage;
            LogCode = SMS.LogCode;
            Status = SMS.Status;
            return (true);
        }


        static internal int CountNewFlagQueue
        {
            get
            {
                return (NewFlagQueue.Count());
            }
        }

        static internal int CountPluginCommunicationQueue
        {
            get
            {
                return (PluginCommunicationQueue.Count());
            }
        }

        static internal int CountServerCommunicationQueue
        {
            get
            {
                return (ServerCommunicationQueue.Count());
            }
        }

        static internal bool UnexpectedErrorDeEnqueue(out PluginErrorMessage UnexpectedError)
        {
            if (PluginErrorMessageQueue.Count < 2)
                ServerAccessFunctions.PluginStatus.UEErrors = false;
            return (PluginErrorMessageQueue.TryDequeue(out UnexpectedError));
        }

        internal void AddToUnexpectedErrorQueue(Exception EMessage)
        {
            try
            {
                PluginErrorMessage PEM;

                PEM.ExceptionData = EMessage;
                PEM.DateTimeOfException = CurrentTime;
                PEM.Comment = "";

                PluginErrorMessageQueue.Enqueue(PEM);
                ServerAccessFunctions.PluginStatus.UEErrors = true;

            }
            catch
            {

            }
        }

        internal void AddToUnexpectedErrorQueue(Exception EMessage, string Comment)
        {
            try
            {
                PluginErrorMessage PEM;

                PEM.ExceptionData = EMessage;
                PEM.DateTimeOfException = CurrentTime;
                PEM.Comment = Comment;

                PluginErrorMessageQueue.Enqueue(PEM);
                ServerAccessFunctions.PluginStatus.UEErrors = true;

            }
            catch
            {

            }
        }

        static internal int UniqueNumber
        {
            get
            {
                Interlocked.Increment(ref _UniqueNumber);
                return (_UniqueNumber);
            }
        }

        internal ServerStatusCodes _ServerCurrentServerStatus
        {
            set
            {
                _CurrentServerStatus = value;
            }
        }

        internal ServerStatusCodes CurrentServerStatus
        {
            get
            {
                return (_CurrentServerStatus);
            }
        }

        internal int ActivatePlugin(int UniqueID)
        {
            try
            {
                NewFlagQueue = new ConcurrentQueue<NewFlagStruct>();
                EventsQueue = new ConcurrentQueue<Tuple<string, string, DateTime>>();
                // PluginFlagDataDictionary = new ConcurrentDictionary<string, PluginFlagDataStruct>();
                PluginCommunicationQueue = new ConcurrentQueue<PluginCommunicationStruct>();
                PluginCommunicationSentDictionary = new ConcurrentDictionary<string, PluginCommunicationStruct>();
                ServerCommunicationQueue = new ConcurrentQueue<PluginServerDataStruct>();
                //ServerCommunicationSentDictionary = new ConcurrentDictionary<string, PluginServerDataStruct>();
                PluginErrorMessageQueue = new ConcurrentQueue<PluginErrorMessage>();
                StatusMessagesDictionary = new ConcurrentDictionary<String, StatusMessagesStruct>();


                DBRecords = new List<String[]>();
                _UserVariables = new Dictionary<string, string>();
                _PluginCommonFunctions.FlagsNeedingReset = new ConcurrentDictionary<DateTime, FlagResetStruct>();
                _PluginCommonFunctions.MaintenanceRequests = new SortedDictionary<DateTime, MaintenanceStruct>();
                _PluginIDCode = UniqueID;
            }
            catch (Exception CHMAPIEx)
            {

                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            return (UniqueID);
        }

 
        internal int _LoadStartupFields(string[] FieldName, string[] SubField, string[] FieldValue)
        {
            try
            {
                if (FieldName == null)
                    return (0);
                StartupFields = new List<Tuple<string, string, string>>();
 
                int Size = Math.Min(FieldName.Length, FieldValue.Length);
                for (int i = 0; i < Size; i++)
                {
                    StartupFields.Add(new Tuple<string, string, string>(FieldName[i], SubField[i], FieldValue[i]));
                }
                return (Size);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (0);
            }
        }

        internal int _LoadCHMStartupFields(string[] FieldName, string[] SubField, string[] FieldValue)
        {
            try
            {
                if (FieldName == null)
                    return (0);
                CHMStartupFields = new String[FieldName.Length];
                CHMStartupValues = new String[FieldValue.Length];
                CHMStartupSubFields = new String[SubField.Length];

                int Size = Math.Min(FieldName.Length, FieldValue.Length);
                for (int i = 0; i < Size; i++)
                {
                    CHMStartupFields[i] = FieldName[i];
                    CHMStartupValues[i] = FieldValue[i];
                }
                return (Size);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (0);
            }

        }



        internal int NextSequence
        {
            get
            {
                Interlocked.Increment(ref _SequenceVariable);
                return _SequenceVariable;
            }
        }

        internal string OffLineName
        {
            get
            {
                return _OffLineName;
            }
        }


        internal HeartbeatTimeCode HeartbeatTimeCodeToInvoke
        {
            get
            {
                return _HeartbeatTimeCodeToInvoke;
            }

            set
            {
                _HeartbeatTimeCodeToInvoke = value;
            }
        }



        internal int PluginIDCode
        {
            get
            {
                return _PluginIDCode;
            }
        }

        static internal DateTime CurrentTime
        {
            get
            {
                return DateTime.Now;
            }
        }

        static internal DateTime StartupTime
        {
            get
            {
                return ServerAccessFunctions.StartupTime;
            }

        }

        static internal DateTime StartupCompletedTime
        {
            get
            {
                return ServerAccessFunctions.StartupCompletedTime;
            }

        }


        internal bool GetDBRecord(int Which, out string[] Record)
        {
            try
            {
                if (Which >= DBRecords.Count)
                {
                    Record = null;
                    return (false);
                }

                Record = DBRecords[Which];
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Record = null;
                return (false);
            }


        }

        internal bool AddEventForTransferToServer(string Name, string Value, DateTime TimeCode)
        {
            EventsQueue.Enqueue(new Tuple<string, string, DateTime>(Name, Value, TimeCode));
            ServerAccessFunctions.PluginStatus.SetFlag = true;
            return (true);

        }

        internal bool AddFlagForTransferToServer(string Name, string SubType, string Value, string RawValue, string RoomUniqueID, string DeviceUniqueID, FlagChangeCodes ChangeCode, FlagActionCodes Operation, string UOM)
        {
            return (AddFlagForTransferToServer(Name, SubType, Value, RawValue, RoomUniqueID, DeviceUniqueID, ChangeCode, Operation, UOM, false, false));
        }


        internal bool AddFlagForTransferToServer(string Name, string SubType, string Value, string RawValue, string RoomUniqueID, string DeviceUniqueID, FlagChangeCodes ChangeCode, FlagActionCodes Operation, string UOM, bool ChangeArchiveStatus, bool NewArchiveStatus)
        {
            try
            {
                NewFlagStruct FlagToSave = new NewFlagStruct();

                FlagToSave.FlagName = Name;
                FlagToSave.FlagValue = Value;
                FlagToSave.FlagRawValue = RawValue;
                FlagToSave.Type = ChangeCode;
                FlagToSave.Operation = Operation;
                FlagToSave.FlagSubType = SubType;
                FlagToSave.RoomUniqueID = RoomUniqueID;
                FlagToSave.SourceUniqueID = DeviceUniqueID;
                FlagToSave.UOM = UOM;
                FlagToSave.MaxHistoryToSave = NumberOfFlagChangesToStore;
                FlagToSave.ChangeArchiveStatus = ChangeArchiveStatus;
                FlagToSave.NewArchiveStatus = NewArchiveStatus;
                NewFlagQueue.Enqueue(FlagToSave);
                ServerAccessFunctions.PluginStatus.SetFlag = true;
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (false);
            }

        }

        internal bool AddFlagForTransferToServer(string Name, string Value, FlagChangeCodes ChangeCode, FlagActionCodes Operation)
        {
            try
            {
                string Q;
                NewFlagStruct FlagToSave = new NewFlagStruct();

                FlagToSave.FlagName = Name;
                FlagToSave.FlagValue = Value;
                FlagToSave.FlagRawValue = Value;
                FlagToSave.Type = ChangeCode;
                FlagToSave.Operation = Operation;
                FlagToSave.MaxHistoryToSave = NumberOfFlagChangesToStore;
                FlagToSave.ChangeArchiveStatus = false; 
                FlagToSave.NewArchiveStatus = false;

                NewFlagQueue.Enqueue(FlagToSave);
                ServerAccessFunctions.PluginStatus.SetFlag = true;
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return (false);
            }

        }

        internal bool GetFlagFromQueue(out NewFlagStruct Flag, out Tuple<string, string, DateTime> Event)
        {
            try
            {
                bool flag = NewFlagQueue.TryDequeue(out Flag);
                bool flag2 = EventsQueue.TryDequeue(out Event);
                ServerAccessFunctions.PluginStatus.SetFlag = !NewFlagQueue.IsEmpty && !EventsQueue.IsEmpty;
                return (flag);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Flag = new NewFlagStruct();
                Event = new Tuple<string, string, DateTime>("","",DateTime.MinValue);
                return (false);
            }
        }

        internal bool DoesValueExistInStartupField(string FieldName, string Value)
        {
            string S;
            if (!GetStartupField(FieldName, out S))
                return (false);
            S = S.ToLower();
            string[] T = S.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (Array.IndexOf(T, Value.ToLower()) > -1)
                return (true);
            return (false);
        }

        internal string GetStartupFieldWithDefault(string FieldName, string DefaultValue)
        {
            string S;
            if (GetStartupField(FieldName, out S))
                return (S);
            else
                return (DefaultValue);

        }


        internal bool GetCHMStartupField(string FieldName, out string Value)
        {
            try
            {
                int i = Array.IndexOf(CHMStartupFields, FieldName);
                if (i == -1)
                {
                    Value = "";
                    return (false);
                }

                Value = CHMStartupValues[i];
                return (true);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Value = "";
                return (false);
            }
        }


        internal bool GetCHMStartupField(string FieldName, out string Value, string DefaultValue)
        {
            if (GetCHMStartupField(FieldName, out Value))
                return (true);
            Value = DefaultValue;
            return (false);

        }



        internal bool GetStartupField(string FieldName, out string Value)
        {
            try
            {
                if(StartupFields==null)
                {
                    Value = "";
                    return (false);
                }

                var value = StartupFields.Find(item => item.Item1 == FieldName);
                if(value==null)
                {
                    Value = "";
                    return (false);
                }

                Value = value.Item3;
                return (true);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Value = "";
                return (false);
            }
        }

        internal bool GetStartupField(string FieldName, out int Value, int DefaultValue)
        {
            if (GetStartupField(FieldName, out Value))
                return (true);
            Value = DefaultValue;
            return (false);

        }

        internal int GetStartupField(string FieldName, int DefaultValue)
        {
            int Value;
            if (GetStartupField(FieldName, out Value))
                return (Value);
            return (DefaultValue);

        }

        internal string GetStartupField(string FieldName, string DefaultValue)
        {
            string Value;
            if (GetStartupField(FieldName, out Value))
                return (Value);
            return (DefaultValue);

        }

        internal bool GetStartupField(string FieldName, out int Value)
        {
            try
            {
                string S;

                if (!GetStartupField(FieldName, out S))
                {
                    Value = 0;
                    return (false);
                }
                int.TryParse(S, out Value);
                return (true);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Value = 0;
                return (false);
            }
        }

        internal int DuplicateStartupValues(out string[] Fields, out string[] Values)
        {
            try
            {
                Fields = new string[StartupFields.Count];
                Values = new string[StartupFields.Count];

                for(int i=0;i< StartupFields.Count;i++)
                {
                    Fields[i] = StartupFields[i].Item1;
                    Values[i] = StartupFields[i].Item3;
                }
                return (StartupFields.Count);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Fields = new string[0];
                Values = new string[0];
                return (0);
            }
        }

        internal bool DeQueuePluginInformationToServer(out PluginServerDataStruct PDS)
        {
            try
            {
                bool flag = ServerCommunicationQueue.TryDequeue(out PDS);
                //if (flag)
                ////    ServerCommunicationSentDictionary.TryAdd(PDS.UniqueNumber, PDS);
                //ServerAccessFunctions.PluginStatus.ToServer = !ServerCommunicationSentDictionary.IsEmpty;
                return (flag);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                PDS = new PluginServerDataStruct();
                return (false);
            }
        }

        internal void GetFromDatabase(string DatabaseName, string Conditions, string TransactionID)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.GetDataBaseInfo;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            PDS.String = DatabaseName;
            PDS.String2 = Conditions;
            PDS.ReferenceIdentifier = TransactionID;

            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }

        internal void AddOrUpdateConfigurationInformation(string FieldName, string SubField, String Value, String ValueType)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.ReferenceObject = new Tuple<string, string, string, string>(FieldName,  SubField,  Value,  ValueType);
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.AddToConfigurationInfo;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;

            string S;
            if (GetStartupField(FieldName, out S))
            {
                int v = StartupFields.FindIndex(item => item.Item1 == FieldName);
                if (v > -1)
                {
                    StartupFields.RemoveAt(v);
                }
            }
            StartupFields.Add(new Tuple<string, string, string>(FieldName, SubField, Value));
            return;
        }

        internal void AddNewDevice(DeviceStruct DVS)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.ReferenceObject = DVS;
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.AddDevice;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
            AddLocalDevice(DVS);
        }

        internal void AddNewRoom(String RoomUniqueID, String RoomName, string Location)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.AddRoom;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            PDS.String = RoomUniqueID;
            PDS.String2 = RoomName;
            PDS.String3 = Location;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }

        internal void UpdateDevice(DeviceStruct DVS)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.ReferenceObject = DVS;
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.UpdateDevice;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }

        internal void TakeDeviceOffLine(string DeviceUniqueID)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.String = DeviceUniqueID;
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.DeviceIsOffline;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;

            //Now We Set THe Device Information to Off-Line
            DeviceStruct Device;
            if (_PluginCommonFunctions.LocalDevicesByUnique.TryGetValue(DeviceUniqueID, out Device))
            {

                try
                {
                    Device.IsDeviceOffline = true;
                    foreach (FlagAttributes FlagAtt in Device.StoredDeviceData.Local_FlagAttributes)
                    {
                        int i;
                        for (i = 0; i < Device.StoredDeviceData.Local_RawValueCurrentStates.Length; i++)
                        {
                            if (OffLineName != (string)Device.StoredDeviceData.Local_RawValueCurrentStates[i])
                            {
                                Device.StoredDeviceData.Local_RawValueLastStates[i] = Device.StoredDeviceData.Local_RawValueCurrentStates[i];
                                Device.StoredDeviceData.Local_RawValueCurrentStates[i] = OffLineName;
                                Device.StoredDeviceData.Local_RawValues.Add(new Tuple<DateTime, string>(CurrentTime, OffLineName));
                            }

                            if (OffLineName != (string)Device.StoredDeviceData.Local_FlagValueCurrentStates[i])
                            {
                                Device.StoredDeviceData.Local_FlagValueLastStates[i] = Device.StoredDeviceData.Local_FlagValueCurrentStates[i];
                                Device.StoredDeviceData.Local_FlagValueCurrentStates[i] = OffLineName;
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

        }

        internal void DeleteDevice(DeviceStruct DVS)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.ReferenceObject = DVS;
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.DeleteDevice;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }


        internal void AddActionitem(int LocalMessageNumber, string IDCode, string PreInfo, string PostInfo)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.AddActionItem;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            PDS.CommandNumber = LocalMessageNumber;
            PDS.String = PreInfo;
            PDS.String2 = PostInfo;
            PDS.String3 = IDCode;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }

        internal void DeleteActionitem(string IDCode)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.DeleteActionItem;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            PDS.String = IDCode;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }

        internal void AddPassword(string PasswordCode, string UserName, string Password, string PasswordLevel)
        {
            PluginServerDataStruct PDS = new PluginServerDataStruct();
            PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
            PDS.Command = ServerPluginCommands.AddPassword;
            PDS.Plugin = ServerAccessFunctions.PluginSerialNumber;
            PDS.String = PasswordCode;
            PDS.String2 = UserName;
            PDS.String3 = Password;
            PDS.String4 = PasswordLevel;
            ServerCommunicationQueue.Enqueue(PDS);
            ServerAccessFunctions.PluginStatus.ToServer = true;
        }

        internal void QueuePluginInformationToServer(PluginServerDataStruct PDS)
        {
            try
            {
                PDS.UniqueNumber = ServerAccessFunctions.PluginSerialNumber + string.Format("-{0:0000000000}", UniqueNumber);
                ServerCommunicationQueue.Enqueue(PDS);
                ServerAccessFunctions.PluginStatus.ToServer = true;

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }

        internal bool DeQueuePluginInformationToPlugin(out PluginCommunicationStruct PIS)
        {
            try
            {
                bool flag = PluginCommunicationQueue.TryDequeue(out PIS);
                if (flag)
                    PluginCommunicationSentDictionary.TryAdd(PIS.UniqueNumber, PIS);
                ServerAccessFunctions.PluginStatus.ToPlugin = !PluginCommunicationQueue.IsEmpty;
                return (flag);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                PIS = new PluginCommunicationStruct();
                return (false);
            }

        }

        internal void QueuePluginInformationToPlugin(PluginCommunicationStruct PIS)
        {
            try
            {
                PIS.UniqueNumber = string.Format("{0:0000}-{1:0000000000}", _PluginIDCode, UniqueNumber);
                PluginCommunicationQueue.Enqueue(PIS);
                ServerAccessFunctions.PluginStatus.ToPlugin = true;
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }


        internal string FindValueInStartupInfo(string StartupInfo, string Key, string DefaultValue)
        {
            string S;
            if (FindValueInStartupInfo(StartupInfo, Key, out S))
                return (S);
            else
                return (DefaultValue);
        }


        internal bool FindValueInStartupInfo(string StartupInfo, string Key, out string Value)
        {
            try
            {
                String U = StartupInfo.Replace("\r", "");
                String S = "\n" + U.ToLower() + "\n";
                String T = U + "\n";

                int i = S.IndexOf("\n" + Key.ToLower() + "=");
                if (i == -1)
                {
                    Value = "";
                    return (false);
                }
                i = i + Key.Length;
                int t = T.IndexOf("\n", i);
                if (t == -1 || t - i < 1)
                {
                    Value = "";
                    return (true);
                }
                Value = U.Substring(i + 1, t - i - 1);
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Value = "";
                return (false);
            }

        }

        internal string GetRoomFromUniqueID(string UniqueID)
        {

            try
            {
                Tuple<string, string, string, string> RM = _PluginCommonFunctions.Rooms.Find(c => c.Item1 == UniqueID);

                if (string.IsNullOrEmpty(RM.Item2))
                    return ("");
                return (RM.Item2);
            }
            catch
            {
                return ("");
            }
        }

        static internal void GenerateErrorRecord(int ErrorNumber, string PreErrorInfo, string PostErrorInfo, Exception Err)
        {
            try
            {
                PluginServerDataStruct PES = new PluginServerDataStruct();

                PES.Command = ServerPluginCommands.ErrorMessage;
                PES.CommandNumber = ErrorNumber;
                PES.String = PreErrorInfo;
                PES.String2 = PostErrorInfo;
                PES.Plugin = ServerAccessFunctions.PluginSerialNumber;
                PES.String3 = "00001-00000";
                if (Err != null)
                    PES.String4 = Err.StackTrace;
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.QueuePluginInformationToServer(PES);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        static internal void GenerateErrorRecordLocalMessage(int ErrorNumber, string PreErrorInfo, string PostErrorInfo)
        {
            try
            {
                PluginServerDataStruct PES = new PluginServerDataStruct();

                PES.Command = ServerPluginCommands.LocalErrorMessage;
                PES.CommandNumber = -ErrorNumber;
                PES.String = PreErrorInfo;
                PES.String2 = PostErrorInfo;
                PES.Plugin = ServerAccessFunctions.PluginSerialNumber;
                PES.String3 = ServerAccessFunctions.PluginSerialNumber;
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.QueuePluginInformationToServer(PES);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        static internal void GenerateLocalMessage(int MessageNumber, string PreInfo, string PostInfo)
        {
            try
            {
                PluginServerDataStruct PES = new PluginServerDataStruct();

                PES.Command = ServerPluginCommands.LocalGeneralMesssage;
                PES.CommandNumber = MessageNumber;
                PES.String = PreInfo;
                PES.String2 = PostInfo;
                PES.Plugin = ServerAccessFunctions.PluginSerialNumber;
                PES.String3 = ServerAccessFunctions.PluginSerialNumber;
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.QueuePluginInformationToServer(PES);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }

        }

        internal void ConvertHexStringToCharArray(String HexField, out char[] CharArray)
        {
            try
            {
                int i, l;

                if (HexField.Length < 2)
                {
                    CharArray = null;
                    return;
                }
                CharArray = new char[HexField.Length / 2];
                l = 0;
                for (i = 0; i < HexField.Length; i = i + 2)
                {
                    CharArray[l] = (char)Convert.ToInt32(HexField.Substring(i, 2), 16);
                    l++;
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                CharArray = null;
                return;
            }

        }

        internal int SplitConfigurationString(string ConfigString, out string[] Fields, out string[] Values)
        {
            try
            {
                int i = 0;
                if (string.IsNullOrEmpty(ConfigString))
                {
                    Fields = null;
                    Values = null;
                    return (0);

                }

                String S = ConfigString.Replace("\r", "");
                string[] lines = S.Split(new Char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                Fields = new string[lines.Count()];
                Values = new string[lines.Count()];

                foreach (var s in lines)
                {
                    var split = s.Split('=');
                    Fields[i] = split[0];
                    Values[i] = split[1];
                    i++;
                }
                return (i);
            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                Fields = null;
                Values = null;
                return (0);
            }


        }

        internal decimal ConvertToDecimal(string Value)
        {
            try
            {
                decimal x;
                decimal.TryParse(Value, out x);
                return (x);

            }
            catch
            {
                return (0);
            }
        }

        internal double ConvertToDouble(string Value)
        {
            try
            {
                double x;
                Double.TryParse(Value, out x);
                return (x);

            }
            catch
            {
                return (0);
            }
        }

        internal int ConvertToInt32(string Value)
        {
            try
            {
                double x;
                Double.TryParse(Value, out x);
                int xx = Convert.ToInt32(x);
                return (xx);

            }
            catch
            {
                return (0);
            }
        }

        internal Int64 ConvertToInt64(string Value)
        {
            try
            {
                double x;
                Double.TryParse(Value, out x);
                Int64 xx = Convert.ToInt64(x);
                return (xx);

            }
            catch
            {
                return (0);
            }
        }

        internal string GenerateSecureID()
        {
            return Guid.NewGuid().ToString("N");
        }

        internal string ConvertStringToMacAddressFormat(string Value)
        {
            try
            {
                string MACwithColons = "";
                for (int i = 0; i < Value.Length; i++)
                {
                    MACwithColons = MACwithColons + Value.Substring(i, 2) + ":";
                    i++;
                }
                if (MACwithColons.Substring(MACwithColons.Length - 1, 1) == ":")
                    MACwithColons = MACwithColons.Substring(0, MACwithColons.Length - 1); // Remove the last colon
                return (MACwithColons);

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                return ("");
            }
        }

        internal char[] ConvertByteArrayToCharArray(Byte[] ByteValue)
        {
            char[] Value = new char[ByteValue.Length];
            for (int i = 0; i < ByteValue.Length; i++)
                Value[i] = (char)ByteValue[i];
            return (Value);
        }

        internal Byte[] ConvertCharArrayToByteArray(Char[] CharValue)
        {
            Byte[] Value = new Byte[CharValue.Length];
            for (int i = 0; i < CharValue.Length; i++)
                Value[i] = (Byte)CharValue[i];
            return (Value);
        }

        internal Byte[] ConvertStringToByteArray(string StringValue)
        {

            return (Encoding.ASCII.GetBytes(StringValue));
        }

        internal string ConvertByteArrayToString(Byte[] ByteValue)
        {
            if (ByteValue == null)
                return ("");
            return (System.Text.Encoding.ASCII.GetString(ByteValue));
        }

        private string LocalTableName()
        {
            return (Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetCallingAssembly().Location) + "Data");
        }

        internal bool NamedSaveLogs(string TableName, string Name, string SubType, string Value, string RawValue, InterfaceStruct Interface, DateTime EventDateTime)
        {
            return (SaveLogs(TableName, Name, SubType, Value, RawValue, Interface, EventDateTime));
        }

        internal bool LocalSaveLogs(string Name, string SubType, string Value, string RawValue, InterfaceStruct Interface, DateTime EventDateTime)
        {
            return (SaveLogs(LocalTableName(), Name, SubType, Value, RawValue, Interface, EventDateTime));
        }


        internal bool LocalSaveLogs(string Name, string SubType, string Value, string RawValue, InterfaceStruct Interface)
        {

            DateTime DD = _PluginCommonFunctions.CurrentTime;
            return (SaveLogs(LocalTableName(), Name, SubType, Value, RawValue, Interface, DD));
        }

        internal bool NamedSaveLogs(string TableName, string Name, string SubType, string Value, string RawValue, DeviceStruct Device, DateTime EventDateTime)
        {
            return (SaveLogs(TableName, Name, SubType, Value, RawValue, Device, EventDateTime));
        }

        internal bool LocalSaveLogs(string Name, string SubType, string Value, string RawValue, DeviceStruct Device, DateTime EventDateTime)
        {
            return (SaveLogs(LocalTableName(), Name, SubType, Value, RawValue, Device, EventDateTime));
        }

        internal bool LocalSaveLogs(string Name, string SubType, string Value, string RawValue, DeviceStruct Device)
        {
            DateTime DD = _PluginCommonFunctions.CurrentTime;
            return (SaveLogs(LocalTableName(), Name, SubType, Value, RawValue, Device, DD));
        }

        private bool SaveLogs(string TableName, string Name, string SubType, string Value, string RawValue, InterfaceStruct Interface, DateTime EventDateTime)
        {
            DeviceStruct Device = new DeviceStruct();

            Device.DeviceName = Interface.InterfaceName;
            Device.InterfaceUniqueID = Interface.InterfaceUniqueID;
            Device.DeviceUniqueID = Interface.InterfaceUniqueID;
            Device.RoomUniqueID = Interface.RoomUniqueID;
            return (SaveLogs(TableName, Name, SubType, Value, RawValue, Device, EventDateTime));
        }

        private bool SaveLogs(string TableName, string Name, string SubType, string Value, string RawValue, DeviceStruct Device, DateTime EventDateTime)
        {

            string DBFileName;
            bool DBOpen;
            string DBVersion = "";
            string[] Fields = { "InterfaceID", "DeviceID", "EventTime", "FlagName", "Value", "RawData", "FullDeviceName", "RoomID" };


            _PluginDatabaseAccess PluginDatabaseAccess = new _PluginDatabaseAccess(Path.GetFileNameWithoutExtension((System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)));
            if (!_PluginDatabaseAccess.DBData.DBOpen)
            {
                DBOpen = PluginDatabaseAccess.OpenOrCreatePluginDB(ServerAccessFunctions.PluginDataDirectory, out DBVersion, out DBFileName, true, _PluginCommonFunctions.DBPassword, "", ref _PluginDatabaseAccess.DBData);
                if (!DBOpen)
                {
                    _PluginCommonFunctions.GenerateErrorRecord(2000000, "Could Not Create Database File '" + DBFileName + "'", PluginDatabaseAccess.GetLastError(), new System.Exception());
                    return (false);
                }
            }
            if (!PluginDatabaseAccess.VerifyIfTableExists(TableName))
            {
                string[] Type = { "text", "varchar", "varchar", "varchar", "varchar", "varchar", "varchar", "varchar", };
                bool[] NotNull = { true, true, false, false, false, false, false, false };
                string[] PrimaryKeys = { "InterfaceID", "DeviceID", "EventTime", "FlagName" };
                if (!PluginDatabaseAccess.CreateTable(TableName, Fields, Type, NotNull, PrimaryKeys, _PluginDatabaseAccess.DBData))
                    return (false);
            }
            string[] Values = { Device.InterfaceUniqueID, Device.DeviceUniqueID, SaveLogsDateFormat(EventDateTime), (Name + " " + SubType).Trim(), Value, RawValue, Device.DeviceName, Device.RoomUniqueID };
            PluginDatabaseAccess.WriteRecord(TableName, Fields, Values);
            return (true);
        }
        internal string SaveLogsDateFormat(DateTime EventDateTime)
        {
            return (EventDateTime.ToString("s"));
        }

        internal DateTime LogFileToDateTime(string Date)
        {
            try
            {
                if (Date.Length < 10)
                    return (DateTime.MinValue);
                if (Date.Length < 19)
                    return (new DateTime(ConvertToInt32(Date.Substring(0, 4)), ConvertToInt32(Date.Substring(5, 2)), ConvertToInt32(Date.Substring(8, 2))));
                else
                    return (new DateTime(ConvertToInt32(Date.Substring(0, 4)), ConvertToInt32(Date.Substring(5, 2)), ConvertToInt32(Date.Substring(8, 2)),
                        ConvertToInt32(Date.Substring(11, 2)), ConvertToInt32(Date.Substring(14, 2)), ConvertToInt32(Date.Substring(17, 2))));

            }
            catch
            {
                return (DateTime.MinValue);
            }
        }

        internal string ExtractFromString(string Target, string Start, string End)
        {
            int sindex = Target.IndexOf(Start, StringComparison.OrdinalIgnoreCase);
            if (sindex == -1)
                return ("");
            int findex = Target.IndexOf(End, sindex + Start.Length, StringComparison.OrdinalIgnoreCase);
            if (findex == -1)
                return ("");
            return (Target.Substring(sindex, findex - sindex));
        }

        internal void ClearAllUserVariables()
        {
            _UserVariables.Clear();
        }

        internal void UpdateorAddUserVariable(string Name, string Value)
        {
            _UserVariables[Name.Trim()] = Value.Trim();
        }

        internal bool GetUserVariable(string Name, out string Value)
        {

            return (_UserVariables.TryGetValue(Name.Trim(), out Value));
        }

        internal bool DeleteUserVariable(string Name)
        {
            return (_UserVariables.Remove(Name.Trim()));
        }

        internal KeyValuePair<string, string>[] GetAllUserValuesBySubset(string Subset)
        {
            return (_UserVariables.Where(d => d.Key.Contains(Subset)).ToArray());

        }





    }





    internal class XMLDeviceScripts
    {
        static private bool DoMaintenance = false;
        static DateTime LastMaintTime;
        public enum DeviceScriptsDataTypes { NoData, Json, XML };
        static internal SemaphoreSlim ProcessXMLMaintanenceInformationSlim;
        static private bool _UseMaintenanceFallbackTime = false;


        static internal void UseMaintenanceFallbackTime()
        {
            _UseMaintenanceFallbackTime = true;
        }

        static internal void UseNormalMaintenanceTime()
        {
            _UseMaintenanceFallbackTime = false;
        }

        static internal void StartMaintenance()
        {
            LastMaintTime = _PluginCommonFunctions.CurrentTime;
            DoMaintenance = true;
            ProcessXMLMaintanenceInformationSlim = new SemaphoreSlim(1);
        }

        static internal void StopMaintenance()
        {
            DoMaintenance = false;
        }


        internal bool SetupXMLConfiguration(ref DeviceStruct Device)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Wait();

            try
            {
                XmlDocument XML = new XmlDocument();
                bool UseInitialValue = false;
                int SequenceCode = 0;
                if (Device.StoredDeviceData == null)
                {
                    Device.StoredDeviceData = new DeviceDataStruct();
                    Device.StoredDeviceData.Local_RawValueLastStates = null;
                    Device.StoredDeviceData.Local_RawValueCurrentStates = null;
                    Device.StoredDeviceData.Local_FlagValueLastStates = null;
                    Device.StoredDeviceData.Local_ArchiveFlagValueCurrentStates = null;
                    Device.StoredDeviceData.Local_ArchiveRawValueCurrentStates = null;
                    Device.StoredDeviceData.Local_FlagValueCurrentStates = null;
                    Device.StoredDeviceData.Local_RawValues = new List<Tuple<DateTime, string>>();
                    Device.StoredDeviceData.Local_FlagAttributes = new List<FlagAttributes>();
                    Device.StoredDeviceData.Local_ArchiveFlagAttributes = new List<FlagAttributes>();
                    Device.StoredDeviceData.Local_MaintanenceInformation = new List<MaintenanceStruct>();
                    Device.StoredDeviceData.Local_MaintanenceHistory = new List<Tuple<DateTime, string, bool>>();
                    Device.StoredDeviceData.Local_LookupFlagAttributes = new List<FlagAttributes>();
                    Device.StoredDeviceData.Local_StatesFlagAttributes = new List<string>();
                }

                try
                {
                    if (string.IsNullOrEmpty(Device.XMLConfiguration))
                    {
                        ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                        return (false);
                    }
                    XML.LoadXml(Device.XMLConfiguration);
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }

                try //Flags
                {
                    XmlNodeList FlagList = XML.SelectNodes("/root/flags/flag");
                    if (FlagList.Count == 0)
                        FlagList = XML.SelectNodes("/flags/flag");

                    foreach (XmlElement e in FlagList)
                    {
                        FlagAttributes FlagAtt = new FlagAttributes();
                        FlagAtt.AttributeNames = new string[e.Attributes.Count + 1];
                        FlagAtt.AttributeValues = new string[e.Attributes.Count + 1];
                        for (int i = 0; i < e.Attributes.Count; i++)
                        {
                            FlagAtt.AttributeNames[i] = e.Attributes[i].Name.ToLower().Trim();
                            FlagAtt.AttributeValues[i] = e.Attributes[i].Value;
                            if (FlagAtt.AttributeNames[i] == "useinitialvalue")
                                UseInitialValue = true;
                        }
                        FlagAtt.AttributeNames[e.Attributes.Count] = "<INTERNALSEQUENCECODE>";
                        FlagAtt.AttributeValues[e.Attributes.Count] = SequenceCode.ToString();
                        SequenceCode++;
                        Device.StoredDeviceData.Local_FlagAttributes.Add(FlagAtt);
                    }
                    if (Device.StoredDeviceData.Local_FlagAttributes.Count > 0)
                    {
                        Device.StoredDeviceData.Local_RawValueLastStates = new string[Device.StoredDeviceData.Local_FlagAttributes.Count];
                        Device.StoredDeviceData.Local_RawValueCurrentStates = new string[Device.StoredDeviceData.Local_FlagAttributes.Count];
                        Device.StoredDeviceData.Local_FlagValueLastStates = new string[Device.StoredDeviceData.Local_FlagAttributes.Count];
                        Device.StoredDeviceData.Local_FlagValueCurrentStates = new string[Device.StoredDeviceData.Local_FlagAttributes.Count];
                        Device.StoredDeviceData.Local_ArchiveRawValueCurrentStates = new string[Device.StoredDeviceData.Local_FlagAttributes.Count];
                        Device.StoredDeviceData.Local_ArchiveFlagValueCurrentStates = new string[Device.StoredDeviceData.Local_FlagAttributes.Count];
                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }

                try  //Archives
                {
                    XmlNodeList ArchiveList = XML.SelectNodes("/root/archives/archive");
                    if (ArchiveList.Count == 0)
                        ArchiveList = XML.SelectNodes("/archives/archive");
                    foreach (XmlElement e in ArchiveList)
                    {
                        FlagAttributes FlagAtt = new FlagAttributes();
                        FlagAtt.AttributeNames = new string[e.Attributes.Count];
                        FlagAtt.AttributeValues = new string[e.Attributes.Count];

                        for (int i = 0; i < e.Attributes.Count; i++)
                        {
                            FlagAtt.AttributeNames[i] = e.Attributes[i].Name.ToLower();
                            FlagAtt.AttributeValues[i] = e.Attributes[i].Value;
                        }
                        Device.StoredDeviceData.Local_ArchiveFlagAttributes.Add(FlagAtt);
                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }


                try //heartbeat
                {
                    XmlNodeList MaintList = XML.SelectNodes("/root/maintenance/heartbeat");
                    if (MaintList.Count == 0)
                        MaintList = XML.SelectNodes("/maintenance/heartbeat");

                    foreach (XmlElement e in MaintList)
                    {
                        MaintenanceStruct MA = new MaintenanceStruct();
                        MA.URL = "";
                        MA.FailInterval = -1;
                        MA.NormalInterval = -1;
                        MA.LastResult = false;
                        MA.LastTime = DateTime.MinValue;
                        MA.NextTime = DateTime.MaxValue;
                        MA.NumberOfConsecutiveFails = 0;
                        MA.NumberOfConsecutiveFailsForDeviceToBeOffline = 0;
                        MA.DeviceUniqueID = Device.DeviceUniqueID;
                        MA.NativeDeviceIdentifer = Device.NativeDeviceIdentifier;
                        MA.HeartbeatTime = -1;
                        MA.UseHeartbeatProcessing = false;
                        MA.UseMaintanenceProcessing = false;

                        for (int i = 0; i < e.Attributes.Count; i++)
                        {
                            switch (e.Attributes[i].Name.ToLower())
                            {
                                case "heartbeatinterval":
                                    MA.HeartbeatTime = _PCF.ConvertToInt32(e.Attributes[i].Value);
                                    break;

                                case "mainttask":
                                    MA.Task = e.Attributes[i].Value;
                                    break;

                            }
                        }
                        MA.UseHeartbeatProcessing = true;
                        MA.LastResult = true;
                        Device.StoredDeviceData.Local_MaintanenceInformation.Add(MA);
                        ProcessXMLMaintanenceInformation(ref MA, "","", MaintanenceCommands.NewTask);
                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }


                try //Maintenance
                {
                    XmlNodeList MaintList = XML.SelectNodes("/root/maintenance/command");
                    if (MaintList.Count == 0)
                        MaintList = XML.SelectNodes("/maintenance/command");

                    foreach (XmlElement e in MaintList)
                    {
                        MaintenanceStruct MA = new MaintenanceStruct();
                        MA.URL = "";
                        MA.FailInterval = _PCF.GetStartupField("DefaultFailinterval", 300);
                        MA.NormalInterval = _PCF.GetStartupField("DefaultNormalInterval", 60);
                        MA.StartDelay = _PCF.GetStartupField("DefaultStartDelay", 60);
                        MA.NumberOfConsecutiveFailsForDeviceToBeOffline = _PCF.GetStartupField("DefaultMaxConsecutiveFails", 4);
                        MA.LastResult = false;
                        MA.LastTime = DateTime.MinValue;
                        MA.NextTime = DateTime.MaxValue;
                        MA.NumberOfConsecutiveFails = 0;
                        MA.DeviceUniqueID = Device.DeviceUniqueID;
                        MA.NativeDeviceIdentifer = Device.NativeDeviceIdentifier;
                        MA.HeartbeatTime = -1;
                        MA.UseHeartbeatProcessing = false;
                        MA.UseMaintanenceProcessing = false;
                        bool StartFailMode = false;
                        for (int i = 0; i < e.Attributes.Count; i++)
                        {
                            switch (e.Attributes[i].Name.ToLower())
                            {
                                case "url":
                                    MA.URL = e.Attributes[i].Value;
                                    break;

                                case "failinterval":
                                    MA.FailInterval = _PCF.ConvertToInt32(e.Attributes[i].Value);
                                    break;

                                case "normalinterval":
                                    MA.NormalInterval = _PCF.ConvertToInt32(e.Attributes[i].Value);
                                    break;

                                case "maxconsecutivefails":
                                    MA.NumberOfConsecutiveFailsForDeviceToBeOffline = _PCF.ConvertToInt32(e.Attributes[i].Value);
                                    break;

                                case "startdelay":
                                    MA.StartDelay = _PCF.ConvertToInt32(e.Attributes[i].Value);
                                    break;

                                case "startmode":
                                    if (e.Attributes[i].Value.ToLower() == "fail")
                                        StartFailMode = true;
                                    break;

                                case "mainttask":
                                    MA.Task = e.Attributes[i].Value;
                                    break;
                            }
                        }
                        MA.UseMaintanenceProcessing = true;
                        Device.StoredDeviceData.Local_MaintanenceInformation.Add(MA);
                        if(!StartFailMode)
                            ProcessXMLMaintanenceInformation(ref MA, "", "", MaintanenceCommands.NewTask);
                        else
                            ProcessXMLMaintanenceInformation(ref MA, "", "", MaintanenceCommands.NewTaskDefaultFail);

                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }


                try  //Lookup
                {
                    XmlNodeList ArchiveList = XML.SelectNodes("/root/lookups/lookup");
                    if (ArchiveList.Count == 0)
                        ArchiveList = XML.SelectNodes("/lookups/lookup");
                    foreach (XmlElement e in ArchiveList)
                    {
                        FlagAttributes FlagAtt = new FlagAttributes();
                        FlagAtt.AttributeNames = new string[e.Attributes.Count];
                        FlagAtt.AttributeValues = new string[e.Attributes.Count];

                        for (int i = 0; i < e.Attributes.Count; i++)
                        {
                            FlagAtt.AttributeNames[i] = e.Attributes[i].Name.ToLower();
                            FlagAtt.AttributeValues[i] = e.Attributes[i].Value;
                        }
                        Device.StoredDeviceData.Local_LookupFlagAttributes.Add(FlagAtt);
                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }

                try  //States
                {
                    XmlNodeList StatesList = XML.SelectNodes("/root/states/state");
                    if (StatesList.Count == 0)
                        StatesList = XML.SelectNodes("/states/state");
                    string S = "";
                    foreach (XmlElement e in StatesList)
                    {
                        Device.StoredDeviceData.Local_StatesFlagAttributes.Add(e.Attributes[0].Value);
                    }
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                    ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                    return (false);
                }




                ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                if (UseInitialValue)
                    ProcessDeviceXMLScriptFromData(ref Device, null, DeviceScriptsDataTypes.NoData, true, "");
                return (true);

            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, Device.XMLConfiguration);
                ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                return (false);
            }

        }

        internal bool ProcessDeviceXMLScriptFromData(ref DeviceStruct Device, object DeviceData, DeviceScriptsDataTypes DeviceDataType) //Very Complex Script Data
        {
            return (ProcessDeviceXMLScriptFromData(ref Device, DeviceData, DeviceDataType, false, ""));

        }

        internal bool ProcessDeviceXMLScriptFromDataBySequence(ref DeviceStruct Device, object DeviceData, DeviceScriptsDataTypes DeviceDataType, string SequenceCodeToUse) //Very Complex Script Data
        {
            return (ProcessDeviceXMLScriptFromData(ref Device, DeviceData, DeviceDataType, false, SequenceCodeToUse));

        }

        private void CreateMathVariableList(out List<Tuple<string, string>> Vars, string FlagDataValue, string RawDataValue, DeviceStruct Device)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            Vars = new List<Tuple<string, string>>();
            Vars.Add(new Tuple<string, string>("flagvalue", FlagDataValue));
            Vars.Add(new Tuple<string, string>("rawvalue", RawDataValue));
            KeyValuePair<string, string>[] kvp = _PCF.GetAllUserValuesBySubset(Device.InterfaceUniqueID + "_");
            foreach (KeyValuePair<string, string> k in kvp)
            {
                try
                {
                    Vars.Add(new Tuple<string, string>(k.Key.Substring(Device.InterfaceUniqueID.Length + 1), k.Value));
                }
                catch (Exception e)
                {
                    _PCF.AddToUnexpectedErrorQueue(e);
                }

            }

        }


        private bool ProcessDeviceXMLScriptFromData(ref DeviceStruct Device, object DeviceData, DeviceScriptsDataTypes DeviceDataType, bool InitialData, string SequenceCodeToUse) //Very Complex Script Data
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Wait();

            try
            {
                DateTime CurrentTime = _PluginCommonFunctions.CurrentTime;
                //                DeviceStruct OriginalDevice = Device.DeepCopy();

                if (Device.StoredDeviceData == null)
                {
                    Device.StoredDeviceData = new DeviceDataStruct();
                    _PluginCommonFunctions.GenerateErrorRecord(2000003, "DeviceDataStruct Not Found For Record", Device.DeviceIdentifier, null);
                }

                foreach (FlagAttributes FlagAtt in Device.StoredDeviceData.Local_FlagAttributes)
                {
                    int i;
                    string DataField = "";
                    string DataValue = "";
                    string StateUnknown = "";
                    string Archive = "N";
                    string FlagValue = "";
                    string SubField = "";
                    string RawField = "";
                    string RawDataValue = "";
                    string FlagDataValue = "";
                    string VariableToSaveTo = "";
                    string FlagName = "";
                    string DataAttributeElementName = "";
                    string DataAttributeElementValue = "";
                    string UOM = "";
                    bool UOMFound = false;
                    string FixedRawValue = "";
                    string UseInitialValue = "";
                    string ResetTime = "";
                    string ResetRawValue = "";
                    string ResetFlagValue = "";
                    string ResetUOMValue = "";
                    string SequenceCode = "";
                    string FlagMathOperation = "";
                    string UOMMathOperation = "";
                    string RawMathOperation = "";
                    bool DoNotSaveFlag = false;
                    bool SaveFlag = true;
                    bool DeleteFlag = false;


                    for (i = 0; i < FlagAtt.AttributeNames.Length; i++)
                    {
                        switch (FlagAtt.AttributeNames[i])
                        {
                            case "subfield":
                                SubField = FlagAtt.AttributeValues[i];
                                break;

                            case "datafield":
                                DataField = FlagAtt.AttributeValues[i];
                                break;

                            case "rawfield":
                                RawField = FlagAtt.AttributeValues[i];
                                break;

                            case "stateunknown":
                                StateUnknown = FlagAtt.AttributeValues[i];
                                break;

                            case "archive":
                                Archive = FlagAtt.AttributeValues[i].ToUpper();
                                break;

                            case "datavalue":
                                DataValue = FlagAtt.AttributeValues[i];
                                break;

                            case "flagvalue":
                                FlagValue = FlagAtt.AttributeValues[i];
                                if (FlagValue == "$$datetime")
                                    FlagValue = _PCF.SaveLogsDateFormat(_PluginCommonFunctions.CurrentTime);
                                break;

                            case "rawvalue":
                                FixedRawValue = FlagAtt.AttributeValues[i];
                                break;

                            case "donotsaveflag":
                                DoNotSaveFlag = true;
                                break;

                            case "createvariable":
                                _PCF.UpdateorAddUserVariable(SetUpVariable(Device, FlagAtt.AttributeValues[i]), "");
                                break;

                            case "variabletosaveto":
                                VariableToSaveTo = FlagAtt.AttributeValues[i];
                                break;


                            case "flagname":
                                FlagName = FlagAtt.AttributeValues[i].ToUpper();
                                break;

                            case "deleteflag":
                                DeleteFlag = true;
                                break;

                            case "dataattributeelementname":
                                DataAttributeElementName = FlagAtt.AttributeValues[i];
                                break;


                            case "dataattributelementevalue":
                                DataAttributeElementValue = FlagAtt.AttributeValues[i];
                                break;

                            case "uom":
                                UOM = FlagAtt.AttributeValues[i];
                                UOMFound = true;
                                break;

                            case "useinitialvalue":
                                UseInitialValue = FlagAtt.AttributeValues[i];
                                break;

                            case "resettime":
                                ResetTime = FlagAtt.AttributeValues[i];
                                break;

                            case "resetrawvalue":
                                ResetRawValue = FlagAtt.AttributeValues[i];
                                break;

                            case "resetflagvalue":
                                ResetFlagValue = FlagAtt.AttributeValues[i];
                                break;

                            case "resetuomvalue":
                                ResetUOMValue = FlagAtt.AttributeValues[i];
                                break;

                            case "rawmathoperation":
                                RawMathOperation = FlagAtt.AttributeValues[i];
                                break;

                            case "flagmathoperation":
                                FlagMathOperation = FlagAtt.AttributeValues[i];
                                break;
                            case "uommathoperation":
                                UOMMathOperation = FlagAtt.AttributeValues[i];
                                break;

                            case "<INTERNALSEQUENCECODE>":
                                SequenceCode = FlagAtt.AttributeValues[i];
                                break;
                        }
                    }


                    if (InitialData && string.IsNullOrEmpty(UseInitialValue))
                        continue;

                    if (!string.IsNullOrEmpty(SequenceCodeToUse)) //FLag REset
                    {
                        if (SequenceCode != SequenceCodeToUse)
                            continue;
                        FlagDataValue = ResetFlagValue;
                        RawDataValue = ResetRawValue;

                        if (!string.IsNullOrEmpty(FlagName))
                        {
                            _PCF.AddFlagForTransferToServer(
                                FlagName,
                                SubField,
                                FlagDataValue,
                                RawDataValue,
                                Device.RoomUniqueID,
                                Device.DeviceUniqueID,
                                FlagChangeCodes.Changeable,
                                FlagActionCodes.addorupdate,
                                ResetUOMValue);
                        }
                        else
                        {
                            _PCF.AddFlagForTransferToServer(
                                _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                SubField,
                                FlagDataValue,
                                RawDataValue,
                                Device.RoomUniqueID,
                                Device.DeviceUniqueID,
                                FlagChangeCodes.Changeable,
                                FlagActionCodes.addorupdate,
                                ResetUOMValue);
                        }
                        int Secx = _PCF.ConvertToInt32(SequenceCode);
                        if (RawDataValue != (string)Device.StoredDeviceData.Local_RawValueCurrentStates[Secx])
                        {
                            Device.StoredDeviceData.Local_RawValueLastStates[Secx] = Device.StoredDeviceData.Local_RawValueCurrentStates[Secx];
                            Device.StoredDeviceData.Local_RawValueCurrentStates[Secx] = RawDataValue;
                        }

                        if (FlagDataValue != (string)Device.StoredDeviceData.Local_FlagValueCurrentStates[Secx])
                        {
                            Device.StoredDeviceData.Local_FlagValueLastStates[Secx] = Device.StoredDeviceData.Local_FlagValueCurrentStates[Secx];
                            Device.StoredDeviceData.Local_FlagValueCurrentStates[Secx] = FlagDataValue;
                        }
                        continue;
                    }



                    if (!string.IsNullOrWhiteSpace(DataField))
                    {
                        if (DeviceDataType == DeviceScriptsDataTypes.XML && !string.IsNullOrWhiteSpace(DataAttributeElementName) && !string.IsNullOrWhiteSpace(DataAttributeElementValue))
                        {
                            try
                            {
                                XElement NXLM = XElement.Parse((string)DeviceData);
                                if (NXLM.Attribute(DataAttributeElementName).Value != DataAttributeElementValue)
                                    continue;
                            }
                            catch (Exception e)
                            {

                                _PCF.AddToUnexpectedErrorQueue(e);
                            }

                        }


                        try  //Step #1, Get Raw Value
                        {
                            if (!string.IsNullOrEmpty(RawField))
                            {
                                RawDataValue = GetData(RawField.ToLower().Trim(), DeviceData, DeviceDataType);
                                if (!string.IsNullOrEmpty(DataField))
                                    FlagDataValue = GetData(DataField.ToLower().Trim(), DeviceData, DeviceDataType);
                                else
                                    FlagDataValue = RawDataValue;
                            }
                            else
                            {
                                RawDataValue = GetData(DataField.ToLower().Trim(), DeviceData, DeviceDataType);
                                FlagDataValue = RawDataValue;
                            }
                        }
                        catch (Exception CHMAPIEx)
                        {
                            _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                        }

                        try //Are there any valid states
                        {
                            if (Device.StoredDeviceData.Local_StatesFlagAttributes.Count > 0)
                            {
                                int num;
                                if (int.TryParse(RawDataValue, out num))
                                {
                                    if (num < Device.StoredDeviceData.Local_StatesFlagAttributes.Count)
                                    {
                                        FlagDataValue = Device.StoredDeviceData.Local_StatesFlagAttributes[num];
                                    }
                                }
                            }
                        }
                        catch
                        {
                            //I Guess Not!!!!
                        }
                    }


                    if (!string.IsNullOrWhiteSpace(DataValue))
                    {
                        if (RawDataValue == DataValue)
                        {
                            FlagDataValue = FlagValue;
                        }
                        else
                            continue;

                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(FlagValue))
                            FlagDataValue = FlagValue;
                    }


                    //Do any math required
                    if (!string.IsNullOrEmpty(RawMathOperation))
                    {
                        string Result;

                        SaveFlag = _PCF.DoMathEquations(RawMathOperation, FlagDataValue, RawDataValue, Device, out Result);
                        if (SaveFlag)
                        {
                            if (!string.IsNullOrEmpty(VariableToSaveTo))
                                _PCF.UpdateorAddUserVariable(SetUpVariable(Device, VariableToSaveTo), Result);
                            
                            RawDataValue = Result;
                        }
                        else
                        {
                        }
                    }

                    if (!string.IsNullOrEmpty(FlagMathOperation))
                    {
                        string Result;

                        SaveFlag = _PCF.DoMathEquations(FlagMathOperation, FlagDataValue, RawDataValue, Device, out Result);
                        if (SaveFlag)
                        {
                            if (!string.IsNullOrEmpty(VariableToSaveTo))
                                _PCF.UpdateorAddUserVariable(SetUpVariable(Device, VariableToSaveTo), Result);

                            FlagDataValue = Result;
                        }
                        else
                        {

                        }
                    }

                    if (!string.IsNullOrEmpty(UOMMathOperation))
                    {
                        string Result;

                        SaveFlag = _PCF.DoMathEquations(UOMMathOperation, FlagDataValue, RawDataValue, Device, out Result);
                        if (SaveFlag)
                        {
                            UOM = Result;
                            UOMFound = true;
                        }
                        else
                        {

                        }
                    }

                    if (string.IsNullOrEmpty(UOM) && !UOMFound && DeviceData != null)
                        UOM = GetData("uom", DeviceData, DeviceDataType);

                    //FinalFixedValues
                    if (!string.IsNullOrEmpty(FixedRawValue))
                        RawDataValue = FixedRawValue;

                    //Tablelookup?
                    {
                        if (Device.StoredDeviceData.Local_LookupFlagAttributes.Count > 0)
                        {
                            bool lookup = false;
                            foreach (FlagAttributes FA in (Device.StoredDeviceData.Local_LookupFlagAttributes))
                            {
                                string lookupvalue = "", value = "";
                                for (int x = 0; x < FA.AttributeNames.Length; x++)
                                {
                                    if (FA.AttributeNames[x].ToLower() == "raw")
                                        lookupvalue = FA.AttributeValues[x];
                                    if (FA.AttributeNames[x].ToLower() == "converted")
                                        value = FA.AttributeValues[x];

                                    if (lookupvalue == FlagDataValue.ToLower() && !string.IsNullOrEmpty(value))
                                    {
                                        FlagDataValue = value;
                                        lookup = true;
                                        break;
                                    }

                                }
                                if (lookup)
                                    break;
                            }



                        }


                    }



                    //Now We See If Values Have Changed!
                    int Sec = _PCF.ConvertToInt32(SequenceCode);
                    bool IsTheStuffDifferent = false;
                    if (RawDataValue != (string)Device.StoredDeviceData.Local_RawValueCurrentStates[Sec])
                    {
                        Device.StoredDeviceData.Local_RawValueLastStates[Sec] = Device.StoredDeviceData.Local_RawValueCurrentStates[Sec];
                        Device.StoredDeviceData.Local_RawValueCurrentStates[Sec] = RawDataValue;
                        Device.StoredDeviceData.Local_RawValues.Add(new Tuple<DateTime, string>(CurrentTime, RawDataValue));
                        IsTheStuffDifferent = true;
                    }

                    if (FlagDataValue != (string)Device.StoredDeviceData.Local_FlagValueCurrentStates[Sec])
                    {
                        Device.StoredDeviceData.Local_FlagValueLastStates[Sec] = Device.StoredDeviceData.Local_FlagValueCurrentStates[Sec];
                        Device.StoredDeviceData.Local_FlagValueCurrentStates[Sec] = FlagDataValue;
                        IsTheStuffDifferent = true;
                    }

                    //Now we Save the Flag
                    if ((SaveFlag && !string.IsNullOrWhiteSpace(DataField) && !DoNotSaveFlag) || InitialData || !string.IsNullOrEmpty(SequenceCodeToUse))
                    {

                        if (string.IsNullOrEmpty(FlagDataValue) || DeleteFlag) //Delete Empty Flag
                        {
                            if (!string.IsNullOrEmpty(FlagName))
                            {
                                _PCF.AddFlagForTransferToServer(
                                    FlagName,
                                    SubField,
                                    FlagDataValue,
                                    RawDataValue,
                                    Device.RoomUniqueID,
                                    Device.DeviceUniqueID,
                                    FlagChangeCodes.Changeable,
                                    FlagActionCodes.delete,
                                    "");
                            }
                            else
                            {
                                _PCF.AddFlagForTransferToServer(
                                    _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                    SubField,
                                    FlagDataValue,
                                    RawDataValue,
                                    Device.RoomUniqueID,
                                    Device.DeviceUniqueID,
                                    FlagChangeCodes.Changeable,
                                    FlagActionCodes.delete,
                                    "");
                            }
                        }
                        else //Save Flag
                        {
                            if (IsTheStuffDifferent)
                            {
                                if (!string.IsNullOrEmpty(FlagName))
                                {
                                    _PCF.AddFlagForTransferToServer(
                                        FlagName,
                                        SubField,
                                        FlagDataValue,
                                        RawDataValue,
                                        Device.RoomUniqueID,
                                        Device.DeviceUniqueID,
                                        FlagChangeCodes.Changeable,
                                        FlagActionCodes.addorupdate,
                                        UOM);
                                }
                                else
                                {
                                    _PCF.AddFlagForTransferToServer(
                                        _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                                        SubField,
                                        FlagDataValue,
                                        RawDataValue,
                                        Device.RoomUniqueID,
                                        Device.DeviceUniqueID,
                                        FlagChangeCodes.Changeable,
                                        FlagActionCodes.addorupdate,
                                        UOM);
                                }
                            }
                        }
                    }

                    if (Archive == "Y" && SaveFlag)
                    {
                        _PCF.LocalSaveLogs(_PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName + " " + SubField, Device.DeviceIdentifier, FlagDataValue, RawDataValue, Device);
                    }

                    if (SaveFlag && string.IsNullOrEmpty(SequenceCodeToUse) && !string.IsNullOrEmpty(ResetTime))//Set This Up to Reset Flag
                    {
                        _PluginCommonFunctions.FlagResetStruct FRS = new _PluginCommonFunctions.FlagResetStruct();
                        FRS.Device = Device;
                        FRS.SequenceCode = SequenceCode;
                        FRS.TimeToResetFlag = CurrentTime.AddMilliseconds(_PCF.ConvertToInt32(ResetTime));
                        while (!_PluginCommonFunctions.FlagsNeedingReset.TryAdd(FRS.TimeToResetFlag, FRS))
                        {
                            FRS.TimeToResetFlag.AddMilliseconds(1);

                        }
                    }
                }
                ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                ServerAccessFunctions.ProcessDeviceXMLScriptFromDataSlim.Release();
                return (false);
            }

        }

        private string SetUpVariable(DeviceStruct Device, string Variable)
        {
            return (Device.InterfaceUniqueID + "_" + Variable);
        }

        private string GetData(string Name, object DeviceData, DeviceScriptsDataTypes DeviceDataType)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            if (DeviceDataType == DeviceScriptsDataTypes.Json)
            {
                try
                {
                    JObject DeviceInfo = (JObject)DeviceData;
                    return (DeviceInfo[Name].ToString());
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return ("");
                }
            }
            if (DeviceDataType == DeviceScriptsDataTypes.XML)
            {
                try
                {
                    XElement NXLM = XElement.Parse((string)DeviceData);
                    if (NXLM.Attribute(Name) == null)
                        return ("");
                    return (NXLM.Attribute(Name).Value);
                }
                catch
                {
                    return ("");
                }
            }
            return ("");
        }


        internal bool ProcessXMLMaintanenceInformation(ref MaintenanceStruct MA, string NativeDeviceIdentifier, string Task, MaintanenceCommands Process)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            try
            {
                ServerAccessFunctions.MaintenanceProcessSlim.Wait();
                if (Process == MaintanenceCommands.SkipOneMaintenanceCycle) //To allow the system to process a manual command
                {
                    LastMaintTime = _PluginCommonFunctions.CurrentTime.AddMilliseconds(_PCF.GetStartupField("MinTimeBetweenMaintenanceRequest", 4)*2);
                    ServerAccessFunctions.MaintenanceProcessSlim.Release();
                    return (true);
                }

                string LocalTask = Task.ToLower();
                string FName = string.Format("Module {0:0000}", _PCF.PluginIDCode);
                DateTime DT = _PluginCommonFunctions.CurrentTime;

                if (Process == MaintanenceCommands.NewTask || Process == MaintanenceCommands.NewTaskDefaultFail)
                {
                    DateTime MT = DT;
                    MA.LastResult = true;
                    if (MA.UseMaintanenceProcessing)
                    {
                        if (Process == MaintanenceCommands.NewTask)
                            MT = DT.AddSeconds(MA.NormalInterval + MA.StartDelay);
                        if (Process == MaintanenceCommands.NewTaskDefaultFail)
                            MT = DT.AddSeconds(MA.FailInterval + MA.StartDelay);

                        //Make Sure Min Time between Events
                        while (_PluginCommonFunctions.MaintenanceRequests.ContainsKey(MT))
                        {
                            MT = MT.AddSeconds(1);
                        }
                        MA.NextTime = MT;
                        _PluginCommonFunctions.MaintenanceRequests.Add(MT, MA);
                    }

                    if (MA.UseHeartbeatProcessing)
                    {
                        MT = DT.AddSeconds(MA.HeartbeatTime);
                        while (_PluginCommonFunctions.MaintenanceRequests.ContainsKey(MT))
                        {
                            MT = MT.AddSeconds(1);
                        }
                        MA.NextTime = MT;
                        _PluginCommonFunctions.MaintenanceRequests.Add(MT, MA);
                    }
                }
                else
                {
                    if (DoMaintenance)
                    {
                        if (Process == MaintanenceCommands.DoTasks)
                        {
                            TimeSpan diff = _PluginCommonFunctions.CurrentTime - LastMaintTime;

                            if (Process == MaintanenceCommands.DoTasks && 
                                ((diff.TotalMilliseconds > _PCF.GetStartupField("MinTimeBetweenMaintenanceRequest", 4) && !_UseMaintenanceFallbackTime) ||
                                (diff.TotalMilliseconds > _PCF.GetStartupField("MaintenanceFallbackTime", 10) && _UseMaintenanceFallbackTime)))
                            {
                                if (_PluginCommonFunctions.MaintenanceRequests.Count > 0)
                                {
                                    if (_PluginCommonFunctions.MaintenanceRequests.ElementAt(0).Key <= DT)
                                    {
                                        MA = _PluginCommonFunctions.MaintenanceRequests.ElementAt(0).Value;
                                        _PluginCommonFunctions.MaintenanceRequests.Remove(_PluginCommonFunctions.MaintenanceRequests.ElementAt(0).Key);

                                        DateTime CT = DT;
                                        if (MA.UseMaintanenceProcessing)
                                        {
                                            MA.TotalNumberOfTimesProcessed++;
                                            if (!string.IsNullOrEmpty(MA.URL))
                                            {
                                                PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                                                PCS.Command = PluginCommandsToPlugins.MaintanenceRequest;
                                                PCS.DeviceUniqueID = MA.DeviceUniqueID;
                                                PCS.String3 = MA.URL;
                                                PCS.String2 = "";
                                                PCS.String = MA.DeviceUniqueID;
                                                PCS.UniqueNumber = string.Format("{0:0000}-{1:0000000000}", _PCF.PluginIDCode, _PCF.NextSequence);
                                                PCS.OriginPlugin = ServerAccessFunctions.PluginSerialNumber;
                                                PCS.DestinationPlugin = ServerAccessFunctions.PluginSerialNumber;
                                                ServerAccessFunctions SAF = new ServerAccessFunctions();
                                                SAF.CHMAPI_PluginInformationCommingFromPlugin(DT, PCS);
                                                if (MA.NumberOfConsecutiveFails == 0)
                                                    MA.TotalNumberOfSuccessfullProcesses++;
                                                else
                                                    MA.TotalNumberOfFailures++;
                                            }
                                            if (MA.NumberOfConsecutiveFails >= MA.NumberOfConsecutiveFailsForDeviceToBeOffline && MA.NumberOfConsecutiveFailsForDeviceToBeOffline > 0)
                                            {
                                                CT = DT.AddSeconds(MA.FailInterval);
                                                if (MA.LastResult == true) //Make Device  Off-Line
                                                {
                                                    _PCF.TakeDeviceOffLine(MA.DeviceUniqueID);
                                                    MA.LastResult = false;
                                                }
                                            }
                                            else
                                            {
                                                CT = DT.AddSeconds(MA.NormalInterval);
                                            }
                                            MA.NumberOfConsecutiveFails++;
                                        }

                                        if (MA.UseHeartbeatProcessing)
                                        {
                                            MA.TotalNumberOfFailures++;
                                            MA.NumberOfConsecutiveFails++;
                                            if (MA.LastResult == true) //Make Device  Off-Line
                                            {
                                                _PCF.TakeDeviceOffLine(MA.DeviceUniqueID);
                                                MA.LastResult = false;
                                            }
                                            CT = DT.AddSeconds(MA.HeartbeatTime);
                                        }

                                        //Make Sure Min Time between Events
                                        while (_PluginCommonFunctions.MaintenanceRequests.ContainsKey(CT))
                                        {
                                            CT = CT.AddSeconds(1);
                                        }
                                        MA.NextTime = CT;
                                        _PluginCommonFunctions.MaintenanceRequests.Add(CT, MA);
                                        LastMaintTime = _PluginCommonFunctions.CurrentTime;
                                    }
                                }
                            }
                        }

                        if (Process == MaintanenceCommands.TaskSucessful)
                        {
                            for (int i = 0; i < _PluginCommonFunctions.MaintenanceRequests.Count; i++)
                            {
                                MA = _PluginCommonFunctions.MaintenanceRequests.ElementAt(i).Value;
                                DateTime XDate = _PluginCommonFunctions.MaintenanceRequests.ElementAt(i).Key;
                                if (MA.NativeDeviceIdentifer != NativeDeviceIdentifier || MA.Task.ToLower() != LocalTask.ToLower())
                                    continue;

                                if (MA.UseMaintanenceProcessing)
                                {
                                    if (MA.NumberOfConsecutiveFails > MA.NumberOfConsecutiveFailsForDeviceToBeOffline && MA.NumberOfConsecutiveFailsForDeviceToBeOffline > 0)
                                    {
                                        //Delete and Change

                                        _PluginCommonFunctions.MaintenanceRequests.Remove(XDate);
                                        //Make Sure Min Time between Events
                                        DateTime CT = DT.AddSeconds(MA.NormalInterval);
                                        while (_PluginCommonFunctions.MaintenanceRequests.ContainsKey(CT))
                                        {
                                            CT = CT.AddSeconds(1);
                                        }
                                        MA.NextTime = CT;
                                        _PluginCommonFunctions.MaintenanceRequests.Add(CT, MA);
                                        MA.NumberOfConsecutiveFails = 0;
                                        MA.LastTime = DT;
                                        MA.LastResult = true;
                                        Debug.WriteLine("TaskSucessful" + MA.NativeDeviceIdentifer + " " + NativeDeviceIdentifier);
                                        break;

                                    }
                                    MA.NumberOfConsecutiveFails = 0;
                                    MA.LastTime = DT;
                                    MA.LastResult = true;
                                    Debug.WriteLine("TaskSucessful" + MA.NativeDeviceIdentifer + " " + NativeDeviceIdentifier);
                                    ServerAccessFunctions.MaintenanceProcessSlim.Release();
                                    return (true);

                                }

                                if (MA.UseHeartbeatProcessing)
                                {
                                    _PluginCommonFunctions.MaintenanceRequests.Remove(XDate);
                                    DateTime CT = DT.AddSeconds(MA.HeartbeatTime);
                                    while (_PluginCommonFunctions.MaintenanceRequests.ContainsKey(CT))
                                    {
                                        CT = CT.AddSeconds(1);
                                    }
                                    MA.NextTime = CT;
                                    MA.LastTime = DT;
                                    MA.LastResult = true;
                                    MA.NumberOfConsecutiveFails = 0;
                                    MA.TotalNumberOfSuccessfullProcesses++;
                                    _PluginCommonFunctions.MaintenanceRequests.Add(CT, MA);
                                    Debug.WriteLine("Heartbeat" + MA.NativeDeviceIdentifer + " " + NativeDeviceIdentifier);

                                }
                            }
                        }
                    }
                }

                //Update System Flags
                if (_PluginCommonFunctions.MaintenanceRequests.Count == 0)
                {
                    _PCF.AddFlagForTransferToServer(
                         FName,
                         "Maintenance Next Item",
                         "None Scheduled",
                         "None Scheduled",
                         "",
                         "",
                         FlagChangeCodes.Changeable,
                         FlagActionCodes.addorupdate,
                         "");

                }
                else
                {
                    DeviceStruct Device;
                    MaintenanceStruct LMA;
                    LMA = _PluginCommonFunctions.MaintenanceRequests.ElementAt(0).Value;
                    if (_PluginCommonFunctions.LocalDevicesByDeviceIdentifier.TryGetValue(LMA.NativeDeviceIdentifer, out Device))
                    {

                        _PCF.AddFlagForTransferToServer(
                            FName,
                            "Maintenance Next Item",
                            _PluginCommonFunctions.MaintenanceRequests.ElementAt(0).Key + "-" + _PCF.GetRoomFromUniqueID(Device.RoomUniqueID) + " " + Device.DeviceName,
                            string.Format("Times Process {0} Times Succeeded {1} Times Failed {2}/{3}", LMA.TotalNumberOfTimesProcessed, LMA.TotalNumberOfSuccessfullProcesses, LMA.TotalNumberOfFailures, LMA.NumberOfConsecutiveFails),
                            Device.RoomUniqueID,
                            Device.DeviceUniqueID,
                            FlagChangeCodes.Changeable,
                            FlagActionCodes.addorupdate,
                            Device.UOMCode);
                    }
                }
                _PCF.AddFlagForTransferToServer(
                    FName,
                    "Maintenance Queue",
                    string.Format("Items In Queue {0}", _PluginCommonFunctions.MaintenanceRequests.Count),
                    string.Format("Items In Queue {0}", _PluginCommonFunctions.MaintenanceRequests.Count),
                    "",
                    "",
                    FlagChangeCodes.Changeable,
                    FlagActionCodes.addorupdate,
                    "");
                ServerAccessFunctions.MaintenanceProcessSlim.Release();
                return (true);
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                ServerAccessFunctions.MaintenanceProcessSlim.Release();
                return (false);
            }
        }
    }
    

    internal class _PluginDatabaseAccess
    {

        private const string DATABASEQUOTE = "\"";
        internal struct DatabaseData
        {
            internal bool DBOpen;
            internal SQLiteConnection DB;
            internal Exception DBLastError;
        }

        static internal DatabaseData DBData;
        static internal DatabaseData AuxDBData;
        static internal string _PluginName;
        internal enum PluginDataLocationType { Oldest, Newest };

        internal _PluginDatabaseAccess(string PluginName)
        {
            _PluginName = PluginName;
        }


        internal string GetLastError()
        {
            return (DBData.DBLastError.Message);
        }

        //internal bool OpenOrCreatePluginDBByMonthAndYear(string DBDirectory, int MonthNumber, int YearNumber, out string Version, out string DBFileName, bool Create)
        //{
        //    string DateName = string.Format("{0,04:D4}{1,02:D2}",YearNumber,MonthNumber);
        //    DBFileName = DBDirectory + "\\" + _PluginName + "\\" + _PluginName + "_" + DateName + ".3db";

        //    if (DBData.DBOpen)
        //    {
        //        Version = DBData.DB.ServerVersion;
        //        if (DBData.DB.ConnectionString.ToLower().IndexOf(DBFileName.ToLower()) > -1)
        //            return (true);
        //        DBData.DB.Close();
        //        DBData.DBOpen = false;
        //    }
        //    return(OpenOrCreatePluginDB(DBDirectory, DateName, out Version, Create));
        //    //Return True If Already Open
        //}

        internal bool OpenSpecialPluginDB(string DBDirectory, out string Version, string DBAuxFileName, string Password)
        {
            string DBFileName = DBDirectory + "\\" + _PluginName + "\\" + DBAuxFileName + ".3db";
            Version = "";

            if (AuxDBData.DBOpen)
                return (true);

            try
            {
                AuxDBData.DB = new SQLiteConnection("Data Source=" + DBFileName + "; FailIfMissing=true;Synchronous=Full;");
                AuxDBData.DB.SetPassword(Password);
                AuxDBData.DB.Open();
                AuxDBData.DBOpen = true;
                Version = "-SQLite Version " + SQLiteConnection.SQLiteVersion;
                return (true);
            }
            catch (Exception err)
            {
                AuxDBData.DBOpen = false;
                AuxDBData.DBLastError = err;
                return (false);
            }
        }

        internal int SpecialPluginDBCalculateLargestField(string TableName, string FieldName)
        {
            long largest = 0, l;
            if (VerifyIfTableExists(TableName, AuxDBData) == false)
                return (-1);
            try
            {
                SQLiteCommand mycommand = new SQLiteCommand(AuxDBData.DB);
                mycommand.CommandText = "Select " + DATABASEQUOTE + FieldName + DATABASEQUOTE + " from " + DATABASEQUOTE + TableName + DATABASEQUOTE;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    return (-1);
                }
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        l = reader.GetBytes(0, 0L, null, 0, 0);
                        if (l > largest)
                            largest = l;
                    }
                }
                reader.Close();
                return ((int)largest);
            }
            catch (Exception err)
            {
                AuxDBData.DBLastError = err;
                return (-1); //Indicates error
            }
        }


        internal bool SpecialPluginDBLoadDictionarytable(string TableName, string Field1Name, string Field2Name, Dictionary<string, string> Dict)
        {
            if (VerifyIfTableExists(TableName, AuxDBData) == false)
                return (false);
            try
            {
                SQLiteCommand mycommand = new SQLiteCommand(AuxDBData.DB);
                mycommand.CommandText = "Select " + DATABASEQUOTE + Field1Name + DATABASEQUOTE + "," + DATABASEQUOTE + Field2Name + DATABASEQUOTE + " from " + DATABASEQUOTE + TableName + DATABASEQUOTE;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    return (false);
                }
                while (reader.Read())
                {
                    Dict[reader.GetString(0)] = reader.GetString(1);
                }
                reader.Close();
                return (true);
            }
            catch (Exception err)
            {
                AuxDBData.DBLastError = err;
                return (true); //Indicates error
            }
        }

        internal int GetObjectByFieldsIntoBytes(string TableName, string[] KeyFields, string[] KeyValues, string ReturnFieldName, ref byte[] Field, out Dictionary<string, string> OtherFields)
        {
            try
            {
                if (VerifyIfTableExists(TableName, AuxDBData) == false)
                {
                    OtherFields = null;
                    return (-1);
                }
                if (KeyFields.Length == 0 || KeyValues.Length == 0 || KeyFields.Length != KeyValues.Length)
                {
                    OtherFields = null;
                    return (-1);
                }

                string DBstmt = "";
                for (int i = 0; i < KeyFields.Length; i++)
                {
                    if (DBstmt.Length > 0)
                        DBstmt = DBstmt + " and ";

                    DBstmt = DBstmt + DATABASEQUOTE + KeyFields[i] + DATABASEQUOTE + " = " + DATABASEQUOTE + KeyValues[i] + DATABASEQUOTE;
                }
                SQLiteCommand mycommand = new SQLiteCommand(AuxDBData.DB);
                mycommand.CommandText = "Select * from " + TableName + " where " + DBstmt;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    OtherFields = null;
                    return (-1);
                }
                reader.Read();

                int o = reader.GetOrdinal(ReturnFieldName);
                try
                {
                    Field = (byte[])reader[o];
                }
                catch
                {

                }
                OtherFields = new Dictionary<string, string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (i == o)
                        continue;
                    try
                    {
                        OtherFields.Add(reader.GetName(i), reader.GetString(i));
                    }
                    catch
                    {
                        OtherFields.Add(reader.GetName(i), "");
                    }
                }
                return (Field.Length);
            }
            catch (Exception err)
            {
                AuxDBData.DBLastError = err;
                OtherFields = null;
                return (-99); //Indicates error
            }
        }

        internal int GetObjectByFieldsIntoString(string TableName, string[] KeyFields, string[] KeyValues, string ReturnFieldName, ref string Field, out Dictionary<string, string> OtherFields)
        {
            try
            {
                if (VerifyIfTableExists(TableName, AuxDBData) == false)
                {
                    OtherFields = null;
                    return (-1);
                }

                if (KeyFields.Length == 0 || KeyValues.Length == 0 || KeyFields.Length != KeyValues.Length)
                {
                    OtherFields = null;
                    return (-1);
                }

                string DBstmt = "";
                for (int i = 0; i < KeyFields.Length; i++)
                {
                    if (DBstmt.Length > 0)
                        DBstmt = DBstmt + " and ";

                    DBstmt = DBstmt + DATABASEQUOTE + KeyFields[i] + DATABASEQUOTE + " = " + DATABASEQUOTE + KeyValues[i] + DATABASEQUOTE;
                }
                SQLiteCommand mycommand = new SQLiteCommand(AuxDBData.DB);
                mycommand.CommandText = "Select * from " + TableName + " where " + DBstmt;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    OtherFields = null;
                    return (-1);
                }
                reader.Read();
                int o = reader.GetOrdinal(ReturnFieldName);
                try
                {
                    if (reader.IsDBNull(o))
                        Field = "";
                    else
                        Field = reader.GetString(o);
                }
                catch
                {
                    Field = "";
                }
                OtherFields = new Dictionary<string, string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (i == o)
                        continue;
                    try
                    {
                        if (reader.IsDBNull(i))
                            OtherFields.Add(reader.GetName(i), "");
                        else
                            OtherFields.Add(reader.GetName(i), reader.GetString(i));
                    }
                    catch
                    {
                        OtherFields.Add(reader.GetName(i), "");
                    }
                }
                return (Field.Length);
            }
            catch (Exception err)
            {
                AuxDBData.DBLastError = err;
                OtherFields = null;
                return (-99); //Indicates error
            }
        }

        internal bool OpenOrCreatePluginDB(string DBDirectory, out string Version, out string DBFileName, bool Create, string Password, string Suffix, ref DatabaseData DBDataToUse)
        {
            DBFileName = DBDirectory + "\\" + _PluginName + "\\" + _PluginName + Suffix+  ".3db";
            Version = "";

            if (DBDataToUse.DBOpen)
                return (true);

            if (!Directory.Exists(DBDirectory + "\\" + _PluginName))
                Directory.CreateDirectory(DBDirectory + "\\" + _PluginName);

            if (!File.Exists(DBFileName))
            {
                if (!Create)
                    return (false);

                try
                {
                    SQLiteConnection.CreateFile(DBFileName);
                }
                catch (Exception err)
                {
                    DBDataToUse.DBOpen = false;
                    DBDataToUse.DBLastError = err;
                    return (false);
                }

            }


            try
            {
                DBDataToUse.DB = new SQLiteConnection("Data Source=" + DBFileName + "; FailIfMissing=true;Synchronous=Full;");
                DBDataToUse.DB.SetPassword(Password);
                DBDataToUse.DB.Open();
                DBDataToUse.DBOpen = true;
                Version = "-SQLite Version " + SQLiteConnection.SQLiteVersion;
                return (true);
            }
            catch (Exception err)
            {
                DBDataToUse.DBOpen = false;
                DBDataToUse.DBLastError = err;
                return (false);
            }

        }

        internal int CalculateLargestField(string TableName, string FieldName)
        {
            long largest = 0, l;
            if (VerifyIfTableExists(TableName) == false)
                return (-1);
            try
            {
                SQLiteCommand mycommand = new SQLiteCommand(DBData.DB);
                mycommand.CommandText = "Select " + DATABASEQUOTE + FieldName + DATABASEQUOTE + " from " + DATABASEQUOTE + TableName + DATABASEQUOTE;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    return (-1);
                }
                while (reader.Read())
                {
                    l = reader.GetBytes(0, 0L, null, 0, 0);
                    if (l > largest)
                        largest = l;
                }
                reader.Close();
                return ((int)largest);
            }
            catch (Exception err)
            {
                DBData.DBLastError = err;
                return (-1); //Indicates error
            }
        }


        internal SQLiteDataReader ExecuteSQLCommandWithReader(string TableName, string Conditions, out bool ValidData)
        {
            SQLiteDataReader reader = null;
            ValidData = false;
            if (VerifyIfTableExists(TableName) == false)
            {
                return (reader);
            }

            try
            {
                SQLiteCommand mycommand = new SQLiteCommand(DBData.DB);
                mycommand.CommandText = "Select * from " + TableName;
                if (Conditions.Length > 0)
                    mycommand.CommandText = mycommand.CommandText + " where " + Conditions;
                reader = mycommand.ExecuteReader();
            }
            catch (Exception err)
            {
                DBData.DBLastError = err;
                return (null);
            }
            ValidData = true;
            return (reader);

        }


        internal SQLiteDataReader GetNextRecordWithReader(ref SQLiteDataReader reader, out string[] Fields, out bool ValidData)
        {
            ValidData = false;
            try
            {
                if (!reader.HasRows)
                {
                    reader.Close();
                    Fields = null;
                    return (null);
                }
                if (!reader.Read())
                {
                    reader.Close();
                    Fields = null;
                    return (null);
                }
                Fields = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Fields[i] = reader[i].ToString();
                }
                ValidData = true;
                return (reader);
            }
            catch (Exception err)
            {
                DBData.DBLastError = err;
                Fields = null;
                return (null);
            }
        }


        internal void CloseNextRecordWithReader(ref SQLiteDataReader reader)
        {
            try
            {
                if (reader != null)
                    reader.Close();
                return;
            }
            catch (Exception err)
            {
                DBData.DBLastError = err;
                return;
            }
        }


        private bool ExecuteSQLCommand(string Command, DatabaseData DBDataToUse)
        {
            try
            {
                SQLiteCommand mycommand = new SQLiteCommand(DBDataToUse.DB);
                mycommand.CommandText = Command;
                mycommand.ExecuteNonQuery();
            }
            catch (Exception err)
            {
                DBDataToUse.DBLastError = err;
                return (false);
            }
            return (true);

        }

        internal bool CreateTable(string TableName, string[] Fields, string[] Types, bool[] NotNull, string[] PrimaryKeys, DatabaseData DBDataToUse)
        {

            try
            {
                string T = "CREATE TABLE " + DATABASEQUOTE + TableName + DATABASEQUOTE + "(";

                for (int i = 0; i < Fields.Length; i++)
                {
                    T = T + DATABASEQUOTE + Fields[i] + DATABASEQUOTE + " " + Types[i];
                    if (NotNull[i])
                        T = T + " NOT NULL, ";
                    else
                        T = T + ",";
                }
                T = T + " PRIMARY KEY (";
                for (int i = 0; i < PrimaryKeys.Length; i++)
                {
                    if (i > 0)
                        T = T + ", ";
                    T = T + DATABASEQUOTE + PrimaryKeys[i] + DATABASEQUOTE;
                }
                T = T + "))";
                return (ExecuteSQLCommand(T, DBDataToUse));
            }
            catch (Exception err)
            {
                DBDataToUse.DBLastError = err;
                return (false); //Indicates error
            }
        }


        internal bool WriteRecord(string TableName, string[] FieldNames, string[] FieldValues)
        {
            StringBuilder Command = new StringBuilder("Insert or Replace into " + TableName + " (");
            foreach (string s in FieldNames)
            {
                Command.Append(s);
                Command.Append(", ");
            }
            Command.Remove(Command.Length - 2, 2);
            Command.Append(") Values (");
            foreach (string s in FieldValues)
            {
                Command.Append(DATABASEQUOTE);
                Command.Append(s);
                Command.Append(DATABASEQUOTE);
                Command.Append(", ");
            }
            Command.Remove(Command.Length - 2, 2);
            Command.Append(")");
            SQLiteCommand Cmd = DBData.DB.CreateCommand();
            Cmd.CommandText = Command.ToString();
            Cmd.ExecuteNonQuery();
            return (true);

        }

        internal bool FindDatedRecord(string TableName, string InterfaceID, string DeviceID, string FlagName, out string[] Values, out string[] FieldNames, PluginDataLocationType PDLT)
        {
            if (string.IsNullOrEmpty(TableName))
            {
                Values = null;
                FieldNames = null;
                return (false);
            }

            if (!_PluginDatabaseAccess.DBData.DBOpen)
            {
                string DBVersion, DBFileName;
                bool DBOpen = OpenOrCreatePluginDB(ServerAccessFunctions.PluginDataDirectory, out DBVersion, out DBFileName, true, _PluginCommonFunctions.DBPassword, "", ref DBData);
                if (!DBOpen)
                {
                    Values = null;
                    FieldNames = null;
                    _PluginCommonFunctions.GenerateErrorRecord(2000000, "Could Not Create Database File '" + DBFileName + "'", GetLastError(), new System.Exception());
                    return (false);
                }
            }

            SQLiteDataReader reader = null;
            if (VerifyIfTableExists(TableName) == false)
            {
                string[] Type = { "text", "varchar", "varchar", "varchar", "varchar", "varchar", "varchar", "varchar" };
                bool[] NotNull = { true, true, false, false, false, false, false, false };
                string[] PrimaryKeys = { "InterfaceID", "DeviceID", "EventTime", "FlagName" };
                string[] Fields = { "InterfaceID", "DeviceID", "EventTime", "FlagName", "Value", "RawData", "FullDeviceName", "RoomID" };
                if (!CreateTable(TableName, Fields, Type, NotNull, PrimaryKeys, _PluginDatabaseAccess.DBData))
                {
                    Values = null;
                    FieldNames = null;
                    return (false);
                }
            }

            try
            {
                bool conditions = false;
                string CondStatement = " where ";

                SQLiteCommand mycommand = new SQLiteCommand(DBData.DB);
                if (PDLT == PluginDataLocationType.Oldest)
                    mycommand.CommandText = "select min(EventTime) as ETime, *  from " + TableName;
                if (PDLT == PluginDataLocationType.Newest)
                    mycommand.CommandText = "select max(EventTime) as ETime, *  from " + TableName;
                if (!string.IsNullOrEmpty(InterfaceID))
                {
                    if (conditions)
                        CondStatement = CondStatement + " and ";
                    CondStatement = CondStatement + "InterfaceID=\"" + InterfaceID + "\"";
                    conditions = true;
                }

                if (!string.IsNullOrEmpty(DeviceID))
                {
                    if (conditions)
                        CondStatement = CondStatement + " and ";
                    CondStatement = CondStatement + "DeviceID=\"" + DeviceID + "\"";
                    conditions = true;
                }

                if (!string.IsNullOrEmpty(FlagName))
                {
                    if (conditions)
                        CondStatement = CondStatement + " and ";
                    CondStatement = CondStatement + "FlagName=\"" + FlagName + "\"";
                    conditions = true;
                }
                if (conditions)
                    mycommand.CommandText = mycommand.CommandText + CondStatement;
                reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    Values = null;
                    FieldNames = null;
                    return (false);
                }
                if (!reader.Read())
                {
                    reader.Close();
                    Values = null;
                    FieldNames = null;
                    return (false);
                }
                Values = new string[reader.FieldCount];
                FieldNames = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Values[i] = reader[i].ToString();
                    FieldNames[i] = reader.GetName(i);
                }
                return (true);

            }
            catch (Exception err)
            {
                DBData.DBLastError = err;
                Values = null;
                FieldNames = null;
                return (false);
            }
        }

        internal bool VerifyIfTableExists(string TableName)
        {
            return (VerifyIfTableExists(TableName, DBData));
        }


        internal bool VerifyIfTableExists(string TableName, DatabaseData DBDataToUse)
        {
            try
            {

                SQLiteCommand mycommand = new SQLiteCommand(DBDataToUse.DB);
                mycommand.CommandText = "SELECT name FROM sqlite_master WHERE type = " + DATABASEQUOTE + "table" + DATABASEQUOTE;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    return (false);
                }
                while (reader.Read())
                {
                    if (reader["name"].ToString().ToLower() == TableName.ToLower())
                    {
                        reader.Close();
                        return (true);
                    }
                }
                reader.Close();
                return (false);
            }
            catch (Exception err)
            {
                DBDataToUse.DBLastError = err;
                return (false); //Indicates error
            }
        }

    }
}






