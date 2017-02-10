using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CHMPluginAPI;
using CHMPluginAPICommon;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Data.Entity.Design.PluralizationServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

//Required Parameters
//  UpdateInterval (In Milliseconds, default is 2500)

namespace CHMModules
{


    public class AutomationProcesses
    {

        internal static List<Tuple<string, string, string, string>> RoomList;
        //Todo Remove Static
        static Tuple<string, string, string, string>[] RoomArray;
        static internal Dictionary<String, DeviceStruct> DeviceDictionary;
        static NameValueCollection DeviceCollection;
        static NameValueCollection DeviceNames;
        static NameValueCollection CommandNames;
        static internal Dictionary<string, CommandTokenStruct> CommandDictionary;
        static string[] DeviceNamesSorted;
        static string[] CommandNamesSorted;
        static internal SemaphoreSlim CompileStatementSlim;
        static internal SemaphoreSlim CompileConditionSlim;


        DeviceStruct[] DeviceArray;
        internal static ConcurrentDictionary<string, FlagDataStruct> FlagDataDictionary;
        internal static System.Threading.Timer CommandLoopTimer;
        internal struct CommandTokenStruct
        {
            internal string TokenName;
            internal string EquivilantToken;
            internal string TokenType;
            internal string MasterToken;
            internal string AdditionalField;
        }

        internal static Dictionary<string, CommandTokenStruct> CommandTokenDictionary;
        internal static Dictionary<string, CommandTokenStruct> CommandTokenDictionaryByCommand;
        internal static KeyValuePair<string, CommandTokenStruct>[] CommandTokenGroup;
        internal static KeyValuePair<string, CommandTokenStruct>[] CommandTokenJoiner;
        internal static KeyValuePair<string, CommandTokenStruct>[] CommandTokenExcept;
        internal static KeyValuePair<string, CommandTokenStruct>[] CommandTokenReservedConditions;
        internal static KeyValuePair<string, CommandTokenStruct>[] CommandTokenReservedCommands;
        internal static KeyValuePair<string,CommandTokenStruct>[] CommandTokenCommand;
        internal static KeyValuePair<string, CommandTokenStruct>[] CommandTokenConjuctions;

        internal enum CommandType { FlagChanged, Time };
        internal struct FlagToWaitStructure
        {
            internal string Flag;
        }

        internal struct CommandStructure
        {
            internal string Origin;
            internal string OriginalStatement;
            internal string OriginalCommand;
            internal string OriginalCondition;
            internal PluginEventArgs OriginalPluginEventStruct;
            internal List<Tuple<string, string, string>> DeviceCommandsProcessWordFlagCompleted;
            internal PluginEventArgs PluginEventStructProcessWordFlagCompleted;
            internal List<Tuple<string, string, string>> DeviceCommandsProcessWordDisplayCompleted;
            internal PluginEventArgs PluginEventStructProcessWordDisplayCompleted;
            internal List<Tuple<string, string, string>> DeviceCommandsProcessWordCommandCompleted;
            internal PluginEventArgs PluginEventStructProcessWordCommandCompleted;
            internal List<Decimal> FoundNumbers;
            internal CommandType WhatTypeOfCommand;
            internal List<Tuple<string, object>> CompiledStack;
            internal string ConditionsMathLine;
            internal string[] FlagsToWatch;
            internal string[] TimesToWatch;
            internal DateTime NextProcessTime;


        };

        const char RoomStart = '\x10';
        const char RoomEnd = '\x11';
        const char DeviceStart = '\x12';
        const char DeviceEnd = '\x13';
        const char CommandStart = '\x14';
        const char CommandEnd = '\x15';
        const char AllStart = '\x16';
        const char AllEnd = '\x17';
        const char TypeStart = '\x18';
        const char TypeEnd = '\x19';
        const char JoinerStart = '\x1A';
        const char JoinerEnd = '\x1B';
        const char ExceptStart = '\x1C';
        const char ExceptEnd = '\x1D';

        const char FlagStart= '\x10';
        const char FlagEnd= '\x11';
        const char TimeStart = '\x12';
        const char TimeEnd = '\x13';


        static internal bool AreWeReadyToGo = false;

        private static _PluginCommonFunctions PluginCommonFunctions;

        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Automation Processing";
            ServerAccessFunctions.PluginSerialNumber = "00001-00007";
            ServerAccessFunctions.PluginVersion = "1.0.0";
            PluginCommonFunctions = new _PluginCommonFunctions();
          //  ServerAccessFunctions._FlagCommingServerEvent += FlagCommingServerEventHandler;
            ServerAccessFunctions._HeartbeatServerEvent += HeartbeatServerEventHandler;
            ServerAccessFunctions._TimeEventServerEvent += TimeEventServerEventHandler;
            ServerAccessFunctions._InformationCommingFromServerServerEvent += InformationCommingFromServerServerEventHandler;
            ServerAccessFunctions._InformationCommingFromPluginServerEvent += InformationCommingFromPluginEventHandler;
            ServerAccessFunctions._WatchdogProcess += WatchdogProcessEventHandler;
            ServerAccessFunctions._ShutDownPlugin += ShutDownPluginEventHandler;
            ServerAccessFunctions._StartupInfoFromServer += StartupInfoEventHandler;
            ServerAccessFunctions._PluginStartupCompleted += PluginStartupCompleted;
            ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
            ServerAccessFunctions._Command += CommandEvent;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;

        }

        private static void CommandEvent(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {

            CommandTokenDictionary = new Dictionary<string, CommandTokenStruct>();
            CommandTokenDictionaryByCommand = new Dictionary<string, CommandTokenStruct>();
            _PluginCommonFunctions PCF = new _PluginCommonFunctions();
            PCF.GetFromDatabase("CommandTokens", "", "CommandTokens");
            CompileStatementSlim = new SemaphoreSlim(1, 1);
            CompileConditionSlim = new SemaphoreSlim(1, 1);
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
            PluginEventArgs Value;
            ServerAccessFunctions.InformationCommingFromServerSlim.Wait();
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            try
            {

                while (ServerAccessFunctions.InformationCommingFromServerQueue.TryDequeue(out Value))
                {
                    if(Value.ServerData.ServerEventReturnCommand== ServerEvents.ProcessWordCommand) //
                    {
                        ServerAccessFunctions SAF = new ServerAccessFunctions();

                        bool processed = false;
                        if (SAF.ProcessCommandMacro(Value.ServerData.String,true)) //Macros
                        {

                            processed = true;
                            continue;
                        }
                        CommandStructure Command = new CommandStructure();
                        Command.DeviceCommandsProcessWordFlagCompleted = new List<Tuple<string, string, string>>();
                        Command.DeviceCommandsProcessWordDisplayCompleted = new List<Tuple<string, string, string>>();
                        Command.DeviceCommandsProcessWordCommandCompleted = new List<Tuple<string, string, string>>();
                        Command.CompiledStack = new List<Tuple<string, object>>();

                        if (!processed)
                        {
                            Tuple<string, string, string> AutomationValues;
                            if (SAF.GetAutomation(Value.ServerData.String, "",out AutomationValues)) 
                            {
                                Command.OriginalStatement = Value.ServerData.String;
                                Command.OriginalCondition = AutomationValues.Item2;
                                Command.OriginalCommand = AutomationValues.Item3;
                                Command.Origin = Value.ServerData.String4;
                                Command.OriginalPluginEventStruct = Value;
                                CompileCondition(ref Command);
                                CompileCommands(ref Command);
                                processed = true;
                                continue;
                            }
                        }

                        if (!processed)
                        {
                            Command.OriginalStatement = Value.ServerData.String;
                            Command.OriginalCommand = Value.ServerData.String; //If No COndition
                            Command.Origin = Value.ServerData.String4;
                            Command.OriginalPluginEventStruct = Value;
                            CompileCommands(ref Command);
                            continue;
                        }
                    }
                    if (Value.ServerData.ServerEventReturnCommand == ServerEvents.RequestedDBInfoReady)
                    {

                        List<string[]> DataStuff = (List<string[]>)Value.ServerData.ReferenceObject;
                        foreach (string[] s in DataStuff)
                        {
                            CommandTokenStruct c = new CommandTokenStruct();
                            c.TokenName = s[0];
                            c.EquivilantToken = s[1];
                            c.TokenType = s[2];
                            c.MasterToken = s[3];
                            c.AdditionalField = s[4];
                            CommandTokenDictionary.Add(c.TokenType.ToLower() + c.TokenName.ToLower(), c);
                            CommandTokenDictionaryByCommand.Add(c.TokenName.ToLower() + c.TokenType.ToLower(), c);
                        }

                        CommandTokenCommand = CommandTokenDictionary.Where(v => v.Value.TokenType.ToLower() == "command").ToArray();
                        for (int i = 0; i < CommandTokenCommand.Length; i++)
                        {
                            KeyValuePair<string, CommandTokenStruct> KP = new KeyValuePair<string, CommandTokenStruct>(CommandTokenCommand[i].Value.EquivilantToken.ToLower(), CommandTokenCommand[i].Value);
                            CommandTokenCommand[i] = KP;
                        }

                        CommandTokenJoiner = CommandTokenDictionary.Where(v => v.Value.EquivilantToken.ToLower() == "and").ToArray();
                        for (int i = 0; i < CommandTokenJoiner.Length; i++)
                        {
                            KeyValuePair<string, CommandTokenStruct> KP = new KeyValuePair<string, CommandTokenStruct>(CommandTokenJoiner[i].Key.Substring(6).ToLower(), CommandTokenJoiner[i].Value);
                            CommandTokenJoiner[i] = KP;
                        }

                        CommandTokenGroup = CommandTokenDictionary.Where(v => v.Value.EquivilantToken.ToLower() == "all").ToArray();
                        for (int i = 0; i < CommandTokenGroup.Length; i++)
                        {
                            KeyValuePair<string, CommandTokenStruct> KP = new KeyValuePair<string, CommandTokenStruct>(CommandTokenGroup[i].Key.Substring(5).ToLower(), CommandTokenGroup[i].Value);
                            CommandTokenGroup[i] = KP;
                        }

                        CommandTokenExcept = CommandTokenDictionary.Where(v => v.Value.EquivilantToken.ToLower() == "except").ToArray();
                        for (int i = 0; i < CommandTokenExcept.Length; i++)
                        {
                            KeyValuePair<string, CommandTokenStruct> KP = new KeyValuePair<string, CommandTokenStruct>(CommandTokenExcept[i].Key.Substring(9).ToLower(), CommandTokenExcept[i].Value);
                            CommandTokenExcept[i] = KP;
                        }

                        CommandTokenReservedCommands = CommandTokenDictionary.Where(v => v.Value.TokenType.ToLower() == "reservedcommands").ToArray();
                        CommandTokenReservedConditions = CommandTokenDictionary.Where(v => v.Value.TokenType.ToLower() == "reservedconditions").ToArray();
                        CommandTokenConjuctions = CommandTokenDictionary.Where(v => v.Value.TokenType.ToLower() == "conjunction").ToArray();


                        CompileDeviceTable();
                        AreWeReadyToGo = true;
                        continue;
                    }
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

        }

        private static void WatchdogProcessEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
 
        }

        private static void StartupInfoEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }

        public void CommandProcessing_ServerLinks(ConcurrentDictionary<string, FlagDataStruct> _FlagDataDictionary, Dictionary<String, DeviceStruct> _DeviceDictionary, List<Tuple<string, string, string, string>> _RoomList)
        {
            RoomList = (List<Tuple<string, string, string, string>>)_RoomList;
            DeviceDictionary = (Dictionary<String, DeviceStruct>)_DeviceDictionary;
            FlagDataDictionary = (ConcurrentDictionary<string, FlagDataStruct>)_FlagDataDictionary;

            CompileRoomTable();

        }


        private static void CompileDeviceTable()
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            ServerAccessFunctions SAF = new ServerAccessFunctions();
            DeviceCollection = new NameValueCollection();
            DeviceNames = new NameValueCollection();
            CommandDictionary = new Dictionary<string, CommandTokenStruct>();
            PluralizationService ps = PluralizationService.CreateService(System.Globalization.CultureInfo.GetCultureInfo("en-us"));
            CommandNames = new NameValueCollection();

            foreach (KeyValuePair<string, DeviceStruct> Dev in DeviceDictionary)
            {
                DeviceCollection.Add(Dev.Value.RoomUniqueID, Dev.Value.DeviceName.ToLower() + DeviceStart + Dev.Value.DeviceUniqueID);
                try
                {
                    string[] s = Dev.Value.DeviceName.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string nv = "";
                    if (s.Length < 1)
                    {
                        nv = Dev.Value.DeviceName.ToLower();
                    }
                    else
                    {
                        if (ps.IsSingular(s[s.Length - 1]))
                            s[s.Length - 1] = ps.Pluralize(s[s.Length - 1]);
                        else
                            s[s.Length - 1] = ps.Singularize(s[s.Length - 1]);
                        foreach (string x in s)
                            nv = nv + " " + x;
                    }
                    DeviceCollection.Add(Dev.Value.RoomUniqueID, nv.Trim() + DeviceStart + Dev.Value.DeviceUniqueID);
                    DeviceNames.Add(Dev.Value.DeviceName.ToLower(), Dev.Value.DeviceUniqueID);
                    DeviceNames.Add(nv.Trim().ToLower(), Dev.Value.DeviceUniqueID);
                    if (!string.IsNullOrWhiteSpace(Dev.Value.DeviceGrouping))
                    {
                        string x;
                        DeviceNames.Add(Dev.Value.DeviceGrouping.ToLower(), Dev.Value.DeviceUniqueID);
                        if (ps.IsSingular(Dev.Value.DeviceGrouping))
                            x = ps.Pluralize(Dev.Value.DeviceGrouping);
                        else
                            x = ps.Singularize(Dev.Value.DeviceGrouping);
                        DeviceNames.Add(x.ToLower(), Dev.Value.DeviceUniqueID);
                    }


                    Tuple<string, string, string>[] Values = SAF.GetListOfValidCommands(Dev.Value);
                        //string[] DevCmnd = Dev.Value.CommandList.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (Tuple<string, string, string> Value in Values)
                    {
                        CommandNames.Add(Value.Item1.ToLower(), Dev.Value.DeviceUniqueID + Value.Item3);
                        CommandTokenStruct CTS = new CommandTokenStruct();
                        CTS.TokenName = Value.Item1.ToLower();
                        CTS.MasterToken = Value.Item1.ToLower();
                        CTS.EquivilantToken = Value.Item1.ToLower();
                        CTS.TokenType = "Command";
                        CommandDictionary.Add(Dev.Value.DeviceUniqueID + Value.Item1.ToLower(), CTS);
                        foreach (KeyValuePair<string, CommandTokenStruct> KVP in CommandTokenCommand)
                        {
                            if (Value.Item1.ToLower() == KVP.Key)
                            {
                                if (KVP.Value.TokenName.ToLower() == Value.Item1.ToLower())
                                    CommandDictionary.Remove(Dev.Value.DeviceUniqueID + KVP.Value.TokenName.ToLower());

                                CommandNames.Add(KVP.Value.TokenName.ToLower(), Dev.Value.DeviceUniqueID + Value.Item3);
                                CommandDictionary.Add(Dev.Value.DeviceUniqueID + KVP.Value.TokenName.ToLower(), KVP.Value);
                            }
                        }
                    }

                }

                catch
                {

                }
                
            }
            DeviceNamesSorted = DeviceNames.AllKeys;
            Array.Sort(DeviceNamesSorted, ((x, y) => y.Length.CompareTo(x.Length)));
            List<string> CommandList = new List<string>();

            CommandNamesSorted = CommandNames.AllKeys;

            Array.Sort(CommandNamesSorted, ((x, y) => y.Length.CompareTo(x.Length)));
        }




        private void CompileRoomTable()
        {

            RoomArray = RoomList.ToArray();
            for (int i = 0; i < RoomArray.Length; i++)
            {
                Tuple<string, string, string, string> R = Tuple.Create(RoomArray[i].Item1, " " + RoomArray[i].Item2.ToLower() + " ","","");
                RoomArray[i] = R;

            }

            Array.Sort(RoomArray, ((x, y) => y.Item2.Length.CompareTo(x.Item2.Length)));

        }

        //Todo Remove Static
        static bool SendCommandToDevicePlugin(CommandStructure CommandStruct)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            if (CommandStruct.DeviceCommandsProcessWordCommandCompleted.Count>0)
            {
                CommandStruct.PluginEventStructProcessWordCommandCompleted=CommandStruct.OriginalPluginEventStruct;
                CommandStruct.PluginEventStructProcessWordCommandCompleted.ServerData.Command = ServerPluginCommands.ProcessWordCommandCompleted;
                CommandStruct.PluginEventStructProcessWordCommandCompleted.ServerData.ReferenceObject = CommandStruct.DeviceCommandsProcessWordCommandCompleted;
                _PCF.QueuePluginInformationToServer(CommandStruct.PluginEventStructProcessWordCommandCompleted.ServerData);
            }

            if (CommandStruct.DeviceCommandsProcessWordFlagCompleted.Count > 0)
            {
                CommandStruct.PluginEventStructProcessWordFlagCompleted = CommandStruct.OriginalPluginEventStruct;
                CommandStruct.PluginEventStructProcessWordFlagCompleted.ServerData.Command = ServerPluginCommands.ProcessWordFlagCompleted;
                CommandStruct.PluginEventStructProcessWordFlagCompleted.ServerData.ReferenceObject = CommandStruct.DeviceCommandsProcessWordFlagCompleted;
                _PCF.QueuePluginInformationToServer(CommandStruct.PluginEventStructProcessWordFlagCompleted.ServerData);
            }

            if (CommandStruct.DeviceCommandsProcessWordDisplayCompleted.Count > 0)
            {
                CommandStruct.PluginEventStructProcessWordDisplayCompleted = CommandStruct.OriginalPluginEventStruct;
                CommandStruct.PluginEventStructProcessWordDisplayCompleted.ServerData.Command = ServerPluginCommands.ProcessWordDisplayCompleted;
                CommandStruct.PluginEventStructProcessWordDisplayCompleted.ServerData.ReferenceObject = CommandStruct.DeviceCommandsProcessWordDisplayCompleted;
                _PCF.QueuePluginInformationToServer(CommandStruct.PluginEventStructProcessWordDisplayCompleted.ServerData);
            }
            return (true);
        }


        //Todo Remove Static
        static internal bool CompileCondition(ref CommandStructure CommandStruct)
        {
            CompileConditionSlim.Wait();
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            string ConditionXML = CommandStruct.OriginalCondition;
            string[] DayNames = new string[7] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
 
            try
            {
                CommandStruct.CompiledStack = new List<Tuple<string, object>>();
                XmlReader xmlReader = XmlReader.Create(new StringReader(ConditionXML));
                StringBuilder Days = new StringBuilder("0000000");
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Whitespace)
                        continue;

                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        switch (xmlReader.Name)
                        {
                            case "paren":
                                CommandStruct.CompiledStack.Add(new Tuple<string, object>(xmlReader.Name, "("));
                                continue;

                            case "logic":

                                string S = xmlReader.ReadString();
                                if (S=="and")
                                    CommandStruct.CompiledStack.Add(new Tuple<string, object>(xmlReader.Name, "&&"));
                                if (S == "or")
                                    CommandStruct.CompiledStack.Add(new Tuple<string, object>(xmlReader.Name, "||"));
                                continue;


                            case "time":
                                int[] tme= new int[4]; 
                                for (int i = 0; i < xmlReader.AttributeCount; i++)
                                {
                                    xmlReader.MoveToAttribute(i);
                                    int v = _PCF.ConvertToInt32(xmlReader.Value);
                                    switch (xmlReader.Name)
                                    {
                                        case "hour":
                                            tme[0] = v;
                                            continue;
                                        case "minute":
                                            tme[1] = v;
                                            continue;
                                        case "second":
                                            tme[2] = v;
                                            continue;
                                        case "variable":
                                            tme[3] = v;
                                            continue;
                                    }

                                }
                                //string s = string.Format("{0,2:00}:{1,2:00}:{2,2:00}", hour, minute, second);
                                CommandStruct.CompiledStack.Add(new Tuple<string, object>(xmlReader.Name, tme));
                                continue;

                            case "days":
                            case "notdays":
                                Days.Clear();
                                Days.Append("0000000");
                                continue;

                            case "run":
                            case "notrun":
                                int pos = Array.IndexOf(DayNames, xmlReader.ReadString());
                                if (pos >= 0)
                                    Days[pos] = '1';
                                continue;
                        }
                    }

                    if (xmlReader.NodeType == XmlNodeType.EndElement)
                    {
                        switch (xmlReader.Name)
                        {
                            case "paren":
                                CommandStruct.CompiledStack.Add(new Tuple<string, object>(xmlReader.Name, ")"));
                                continue;

                            case "days":
                            case "notdays":
                                CommandStruct.CompiledStack.Add(new Tuple<string, object>(xmlReader.Name, Days.ToString()));
                                Days.Clear();
                                Days.Append("0000000");
                                continue;
                        }
                    }


                    if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "Cube"))
                    {
                        if (xmlReader.HasAttributes)
                            Console.WriteLine(xmlReader.GetAttribute("currency") + ": " + xmlReader.GetAttribute("rate"));
                    }
                }
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, "Compile Condition "+CommandStruct.OriginalCommand);
            }
            CompileConditionSlim.Release();
            return (true);
        }


        //Todo Remove Static
        static internal void CompileCommands(ref CommandStructure CommandStruct)
        {
            CompileStatementSlim.Wait();

            string LastError;
            int LastErrorNumber;
            int sindex, eindex;
            decimal d;
            string x;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            string FormattedCommand, PreCompiledCommand, CompiledCommand;

            string[] SplitOriginalCommad = CommandStruct.OriginalCommand.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string SplitOC in SplitOriginalCommad)
            {
                try
                {
                    CommandStruct.FoundNumbers = new List<Decimal>();

                    //Format Statement
                    bool formatted = FormatString(SplitOC, out FormattedCommand, out LastError, out LastErrorNumber);
                    PreCompiledCommand = FormattedCommand;

                    CompiledCommand = " " + PreCompiledCommand;

                    //First Search for any ReservedCommands
                    int q;
                    bool RCSent = false;
                    foreach (KeyValuePair<string, CommandTokenStruct> KVP in CommandTokenReservedCommands)
                    {
                        q = CompiledCommand.ToLower().IndexOf(KVP.Value.TokenName.ToLower());
                        if (q > -1)
                        {
                            string s = KVP.Value.MasterToken.ToLower();

                            int eq;
                            switch (s)
                            {
                                case "setflag":
                                    eq = CompiledCommand.ToLower().IndexOf(" = ");
                                    if (eq > -1)
                                    {
                                        string fl = CompiledCommand.Substring(q + KVP.Value.TokenName.Length, eq - (q + KVP.Value.TokenName.Length));
                                        string vl = CompiledCommand.Substring(eq + 3);
                                        CommandStruct.DeviceCommandsProcessWordFlagCompleted.Add(Tuple.Create(fl.Trim(), vl.Trim(), ""));
                                        //SendCommandToDevicePlugin(CommandStruct, ServerPluginCommands.ProcessWordFlagCompleted);
                                        RCSent = true;
                                    }
                                    break;

                                case "display":

                                    if (CompiledCommand.Length > q + KVP.Value.TokenName.Length)
                                    {
                                        string fl = CompiledCommand.Substring(q + KVP.Value.TokenName.Length, CompiledCommand.Length - (q + KVP.Value.TokenName.Length));
                                        CommandStruct.DeviceCommandsProcessWordDisplayCompleted.Add(Tuple.Create(fl.Trim(), "", ""));
                                        //SendCommandToDevicePlugin(CommandStruct, ServerPluginCommands.ProcessWordDisplayCompleted);
                                        RCSent = true;
                                    }
                                    break;
                            }


                        }
                        if (RCSent)
                            break;
                    }
                    if (RCSent)
                        continue;

                    //Find The Rooms in the Command portion of the Statement
                    foreach (Tuple<string, string, string, string> Room in RoomArray)
                    {
                        //Tag Each Room
                        CompiledCommand = CompiledCommand.Replace(Room.Item2, " " + RoomStart + Room.Item1 + RoomEnd + " ");
                    }

                    string[] Words = CompiledCommand.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string S in Words)
                    {
                        if (S[0] > 32)
                        {
                            if (decimal.TryParse(S, out d))
                                CommandStruct.FoundNumbers.Add(d);
                        }
                    }

                    int rooms = Words.Count(c => c.ToCharArray()[0] == RoomStart);

                    //Now we Process Group Commands
                    int GroupCommand = 0;

                    //Remove Punctuation and add leading and trailing spaces
                    //                string GroupProcess = " " + Regex.Replace( CommandStruct.CompiledCommand, "(\\p{P})", "" ) + " ";
                    string GroupProcess = " " + CompiledCommand + " ";


                    //Okay, so there is....Now Create a list of all device names and search...Just Like Devices
                    //and add all the devices into the command line where the word is.

                    foreach (KeyValuePair<string, CommandTokenStruct> kvp in CommandTokenGroup)
                    {
                        while (GroupProcess.Contains(" " + kvp.Key + " "))
                        {
                            GroupProcess = GroupProcess.Replace(" " + kvp.Key + " ", " " + AllStart + kvp.Value.EquivilantToken + AllEnd + " ");
                            GroupCommand++;
                        }

                    }

                    foreach (KeyValuePair<string, CommandTokenStruct> kvp in CommandTokenJoiner)
                    {
                        while (GroupProcess.Contains(" " + kvp.Key + " "))
                        {
                            GroupProcess = GroupProcess.Replace(" " + kvp.Key + " ", " " + JoinerStart + kvp.Value.EquivilantToken + JoinerEnd + " ");
                            // GroupCommand++;
                        }

                    }

                    foreach (KeyValuePair<string, CommandTokenStruct> kvp in CommandTokenExcept)
                    {
                        while (GroupProcess.Contains(" " + kvp.Key + " "))
                        {
                            GroupProcess = GroupProcess.Replace(" " + kvp.Key + " ", " " + ExceptStart + kvp.Value.EquivilantToken + ExceptEnd + " ");
                            // GroupCommand++;
                        }

                    }

                    int i;
                    foreach (string S in DeviceNamesSorted)
                    {
                        while ((i = GroupProcess.IndexOf(" " + S + " ")) > -1)
                        {
                            GroupProcess = GroupProcess.Replace(" " + S + " ", " " + TypeStart + S.Trim() + TypeEnd + " ");
                        }
                    }

                    foreach (string S in CommandNamesSorted)
                    {
                        while ((i = GroupProcess.IndexOf(" " + S + " ")) > -1)
                        {
                            GroupProcess = GroupProcess.Replace(" " + S + " ", " " + CommandStart + S.Trim() + CommandEnd + " ");
                        }
                    }

                    string Command = "", LastCommand = "";
                    string Type = "", LastType = "";
                    string LRoom = "", LastLroom = "";
                    CommandTokenStruct sr;
                    bool Joiner;
                    bool JoinerUsed = false;
                    int Except = 0;

                    //if (rooms == 1)
                    //{
                    //    sindex = GroupProcess.IndexOf(RoomStart);
                    //    eindex = GroupProcess.IndexOf(RoomEnd);
                    //    if (sindex != -1 && eindex != -1)
                    //    {
                    //        LRoom = GroupProcess.Substring(sindex + 1, eindex - sindex - 1);
                    //    }
                    //}

                    Queue<string> queuedrooms = new Queue<string>();

                    for (sindex = 0; sindex < GroupProcess.Length; sindex++)
                    {
                        Joiner = false;
                        switch (GroupProcess[sindex])
                        {
                            case RoomStart:
                                if (!string.IsNullOrWhiteSpace(LRoom))
                                {
                                    queuedrooms.Enqueue(LRoom);
                                }
                                eindex = GroupProcess.IndexOf(RoomEnd, sindex);
                                if (eindex > sindex)
                                {
                                    LRoom = GroupProcess.Substring(sindex + 1, eindex - sindex - 1);
                                    sindex = eindex;
                                    break;
                                }
                                break;


                            case TypeStart:
                                eindex = GroupProcess.IndexOf(TypeEnd, sindex);
                                if (eindex > sindex)
                                {
                                    Type = GroupProcess.Substring(sindex + 1, eindex - sindex - 1);
                                    sindex = eindex;
                                    break;
                                }
                                break;

                            case ExceptStart:
                                eindex = GroupProcess.IndexOf(ExceptEnd, sindex);
                                if (eindex > sindex)
                                {
                                    sindex = eindex;
                                }
                                Except = 1;
                                break;

                            case CommandStart:
                                eindex = GroupProcess.IndexOf(CommandEnd, sindex);
                                if (eindex > sindex)
                                {
                                    Command = GroupProcess.Substring(sindex + 1, eindex - sindex - 1);
                                    sindex = eindex;
                                    break;
                                }
                                break;

                            case JoinerStart:
                                eindex = GroupProcess.IndexOf(JoinerEnd, sindex);
                                if (eindex > sindex)
                                {
                                    sindex = eindex;
                                }
                                Joiner = true;
                                JoinerUsed = true;
                                break;

                            default:
                                break;

                        }

                        if ((!string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Command)) ||
                            Except == 1 ||
                            (!string.IsNullOrEmpty(Type) && Except == 2) ||
                            (!string.IsNullOrEmpty(LRoom) && Except == 2))
                        {
                            if (Except == 2 && !string.IsNullOrWhiteSpace(LRoom))
                            {
                                if (string.IsNullOrWhiteSpace(Command))
                                {
                                    Command = LastCommand;
                                }

                                if (string.IsNullOrWhiteSpace(Type))
                                {
                                    Type = LastType;
                                }

                            }


                            if ((!string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Command)))
                            {
                                if ((rooms == 0 || (rooms > 0 && !string.IsNullOrWhiteSpace(LRoom))) || Except == 1)
                                {
                                    queuedrooms.Enqueue(LRoom);
                                    while (queuedrooms.Count > 0)
                                    {
                                        LRoom = queuedrooms.Dequeue();

                                        string[] DevNames = DeviceNames.GetValues(Type);
                                        DeviceStruct GroupDeviceStruct = new DeviceStruct();
                                        foreach (string X in DevNames)
                                        {
                                            DeviceDictionary.TryGetValue(X, out GroupDeviceStruct);
                                            if (!string.IsNullOrWhiteSpace(LRoom))
                                            {
                                                if (LRoom != GroupDeviceStruct.RoomUniqueID)
                                                    continue;
                                            }
                                            if (Except < 2)
                                            {
                                                if (!CommandStruct.DeviceCommandsProcessWordCommandCompleted.Exists(c => c.Item2 == GroupDeviceStruct.DeviceUniqueID))
                                                {
                                                    x = "";
                                                    CommandDictionary.TryGetValue(GroupDeviceStruct.DeviceUniqueID + Command.ToLower(), out sr);
                                                    if (sr.AdditionalField == "###" && CommandStruct.FoundNumbers.Count > 0)
                                                    {
                                                        x = CommandStruct.FoundNumbers[0].ToString();
                                                    }

                                                    CommandStruct.DeviceCommandsProcessWordCommandCompleted.Add(Tuple.Create(sr.EquivilantToken, GroupDeviceStruct.DeviceUniqueID, x));
                                                }
                                            }
                                            else
                                            {
                                                int CTD = CommandStruct.DeviceCommandsProcessWordCommandCompleted.FindIndex(c => c.Item2 == GroupDeviceStruct.DeviceUniqueID);
                                                if (CTD > -1)
                                                    CommandStruct.DeviceCommandsProcessWordCommandCompleted.RemoveAt(CTD);
                                            }
                                        }
                                    }
                                    if (!Joiner)
                                    {
                                        if (!string.IsNullOrWhiteSpace(Command))
                                        {
                                            LastCommand = Command;
                                            Command = "";
                                        }

                                        if (!string.IsNullOrWhiteSpace(Type))
                                        {
                                            LastType = Type;
                                            Type = "";
                                        }

                                        if (!string.IsNullOrWhiteSpace(LRoom))
                                        {
                                            LastLroom = LRoom;
                                            LRoom = "";
                                        }
                                    }
                                }
                            }
                        }
                        if (Joiner || Except == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(Command))
                            {
                                LastCommand = Command;
                                Command = "";
                            }

                            if (!string.IsNullOrWhiteSpace(Type))
                            {
                                LastType = Type;
                                Type = "";
                            }

                            if (!string.IsNullOrWhiteSpace(LRoom))
                            {
                                queuedrooms.Enqueue(LRoom);
                                LastLroom = LRoom;
                                LRoom = "";
                            }
                        }
                        if (Except == 1)
                            Except = 2;
                    }
                    //Okay, Now lets look at what we have
                    if (JoinerUsed || Except == 2)
                    {
                        if (!string.IsNullOrWhiteSpace(LRoom))
                        {
                            if (string.IsNullOrWhiteSpace(Type))
                                Type = LastType;
                            if (string.IsNullOrWhiteSpace(Command))
                                Command = LastCommand;
                        }

                        if (!string.IsNullOrWhiteSpace(Type))
                        {
                            if (string.IsNullOrWhiteSpace(LRoom))
                                LRoom = LastLroom;
                            if (string.IsNullOrWhiteSpace(Command))
                                Command = LastCommand;
                        }

                        if (!string.IsNullOrWhiteSpace(Command))
                        {
                            if (string.IsNullOrWhiteSpace(LRoom))
                                LRoom = LastLroom;
                            if (string.IsNullOrWhiteSpace(Type))
                                Type = LastType;
                        }
                    }


                    if ((!string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Command)) ||
                        (!string.IsNullOrEmpty(Type) && Except == 2))
                    {
                        string[] DevNames = DeviceNames.GetValues(Type);
                        DeviceStruct GroupDeviceStruct = new DeviceStruct();
                        queuedrooms.Enqueue(LRoom);
                        while (queuedrooms.Count > 0)
                        {
                            LRoom = queuedrooms.Dequeue();
                            foreach (string X in DevNames)
                            {
                                DeviceDictionary.TryGetValue(X, out GroupDeviceStruct);
                                if (!string.IsNullOrWhiteSpace(LRoom))
                                {
                                    if (LRoom != GroupDeviceStruct.RoomUniqueID)
                                        continue;
                                }
                                if (Except < 2)
                                {
                                    if (!CommandStruct.DeviceCommandsProcessWordCommandCompleted.Exists(c => c.Item2 == GroupDeviceStruct.DeviceUniqueID))
                                    {
                                        x = "";
                                        CommandDictionary.TryGetValue(GroupDeviceStruct.DeviceUniqueID + Command.ToLower(), out sr);
                                        if (sr.AdditionalField == "###" && CommandStruct.FoundNumbers.Count > 0)
                                        {
                                            x = CommandStruct.FoundNumbers[0].ToString();
                                        }

                                        CommandStruct.DeviceCommandsProcessWordCommandCompleted.Add(Tuple.Create(sr.EquivilantToken, GroupDeviceStruct.DeviceUniqueID, x));
                                    }
                                }
                                else
                                {
                                    int CTD = CommandStruct.DeviceCommandsProcessWordCommandCompleted.FindIndex(c => c.Item2 == GroupDeviceStruct.DeviceUniqueID);
                                    if (CTD > -1)
                                        CommandStruct.DeviceCommandsProcessWordCommandCompleted.RemoveAt(CTD);
                                }
                            }
                        }
                        Type = "";
                    }
                }

                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx, "Compile Command");
                }
                //SendCommandToDevicePlugin(CommandStruct, ServerPluginCommands.ProcessWordCommandCompleted);
            }
            CompileStatementSlim.Release();
        }

        //Todo Remove Static
        static private bool FormatString(string Originalexpression, out string FormattedString, out string LastError, out int LastErrorNumber)
        {
            FormattedString = "";
            LastError = "";
            LastErrorNumber = 0;

            string expression = Originalexpression.ToLower();
            if (string.IsNullOrEmpty(expression))
            {
                LastError = "Expression is null or empty";
                LastErrorNumber = 1;
                return (false);
            }

            StringBuilder formattedString = new StringBuilder();
            int balanceOfParenth = 0; // Check number of parenthesis
            int balanceOfBrackets = 0; //Check Brackets
            int balanceOfBraces = 0; //Check Braces

            // Format string in one iteration and check number of parenthesis
            // (this function do 2 tasks because performance priority)
            for (int i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];

                if (ch == '(')
                {
                    balanceOfParenth++;
                    formattedString.Append(ch);
                    continue;
                }
                if (ch == ')')
                {
                    balanceOfParenth--;
                    formattedString.Append(ch);
                    continue;
                }

                if (ch == '{')
                {
                    balanceOfBraces++;
                    formattedString.Append(ch);
                    continue;
                }
                if (ch == '}')
                {
                    balanceOfBraces--;
                    formattedString.Append(ch);
                    continue;
                }

                if (ch == '[')
                {
                    balanceOfBrackets++;
                    formattedString.Append(ch);
                    continue;
                }
                if (ch == ']')
                {
                    balanceOfBrackets--;
                    formattedString.Append(ch);
                    continue;
                }

                if (Char.IsSymbol(ch))
                {
                    formattedString.Append(" ");
                    formattedString.Append(ch);
                    formattedString.Append(" ");
                    continue;
                }

                if (Char.IsUpper(ch))
                {
                    formattedString.Append(Char.ToLower(ch));
                }
                else
                {
                    formattedString.Append(ch);
                }
            }

            if (balanceOfParenth != 0)
            {
                LastError = "Number of left and right parenthesis '()' is not equal";
                LastErrorNumber = 2;
                return (false);
            }

            if (balanceOfBraces != 0)
            {
                LastError = "Number of left and right braces '{}' is not equal";
                LastErrorNumber = 3;
                return (false);
            }

            if (balanceOfParenth != 0)
            {
                LastError = "Number of left and right balanceOfBrackets '[]' is not equal";
                LastErrorNumber = 4;
                return (false);
            }

            FormattedString = Regex.Replace(Regex.Replace(formattedString.ToString(), @"\s+", " "), @"\p{P}*$", string.Empty) + " ";


            return (true);
        }


    }
}
