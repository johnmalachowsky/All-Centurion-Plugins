using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Net;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CHMPluginAPICommon
{
    public class PasswordStruct
    {
        public string PluginID;
        public string PWCode;
        public string Account;
        public string Password;
        public string PWLevel;
    }


    public class InterfaceStruct
    {
        public string InterfaceName;
        public string InterfaceUniqueID;
        public string RoomUniqueID;
        public string InterfaceType;
        public string InterfaceHardware;
        public string HardwareSettings;
        public string PluginName;
        public string StartupInformation;
        public string ControllingDLL;
        public string HardwareIdentifier;
        public string PreInitializeTimeOut;
        public string Comments;
        public int TableLoc;
    }


    public class DeviceStruct
    {
        public string DeviceUniqueID;
        public string DeviceName;
        public string DeviceType;
        public string DeviceClassID;
        public string RoomUniqueID;
        public string InterfaceUniqueID;
        public string DeviceIdentifier;
        public string NativeDeviceIdentifier;
        public string UOMCode;
        public string Origin;
        public string AdditionalFlagName;
        public string HTMLDisplayName;
        public string AFUOMCode;
        public string DeviceGrouping;
        public string XMLConfiguration;
        public string UndesignatedFieldsInfo;
        public int IntVal01;
        public int IntVal02;
        public int IntVal03;
        public int IntVal04;
        public string StrVal01;
        public string StrVal02;
        public string StrVal03;
        public string StrVal04;
        public Byte[] objVal;
        public string Comments; //End Of Database Stored Stuff
                                //General Usage Fields-NonDB Stored
        public int Local_TableLoc;
        public bool Local_Flag1;
        public bool Local_Flag2;
        public bool Local_IsLocalDevice;
        public string Local_OriginalInfo;
        public string Local_CommandStatementIgnore;
        public DeviceDataStruct StoredDeviceData;

        //
        // Summary:
        //Creates, Does Not Copy StoredDeviceData Class    
        //Override Copy
        //
        public DeviceStruct DeepCopy() 
        {
 
            DeviceStruct OD = new DeviceStruct();

            OD.DeviceUniqueID = this.DeviceUniqueID;
            OD.DeviceName = this.DeviceName;
            OD.DeviceType = this.DeviceType;
            OD.DeviceClassID = this.DeviceClassID;
            OD.RoomUniqueID = this.RoomUniqueID;
            OD.InterfaceUniqueID = this.InterfaceUniqueID;
            OD.DeviceIdentifier = this.DeviceIdentifier;
            OD.NativeDeviceIdentifier = this.NativeDeviceIdentifier;
            OD.UOMCode = this.UOMCode;
            OD.Origin = this.Origin;
            OD.AdditionalFlagName = this.AdditionalFlagName;
            OD.HTMLDisplayName = this.HTMLDisplayName;
            OD.AFUOMCode = this.AFUOMCode;
            OD.DeviceGrouping = this.DeviceGrouping;
            OD.XMLConfiguration = this.XMLConfiguration;
            OD.UndesignatedFieldsInfo = this.UndesignatedFieldsInfo;
            OD.IntVal01 = this.IntVal01;
            OD.IntVal02 = this.IntVal02;
            OD.IntVal03 = this.IntVal03;
            OD.IntVal04 = this.IntVal04;
            OD.StrVal01 = this.StrVal01;
            OD.StrVal02 = this.StrVal02;
            OD.StrVal03 = this.StrVal03;
            OD.StrVal04 = this.StrVal04;
            OD.Comments = this.Comments;
            OD.Local_TableLoc = this.Local_TableLoc;
            OD.Local_Flag1 = this.Local_Flag1;
            OD.Local_Flag2 = this.Local_Flag2;
            OD.Local_IsLocalDevice = this.Local_IsLocalDevice;
            OD.Local_OriginalInfo = this.Local_OriginalInfo;
            OD.Local_CommandStatementIgnore = this.Local_CommandStatementIgnore;
            OD.StoredDeviceData = new DeviceDataStruct();
            OD.StoredDeviceData.Local_FlagAttributes = new List<FlagAttributes>();
            OD.StoredDeviceData.Local_ArchiveFlagAttributes = new List<FlagAttributes>();
            OD.StoredDeviceData.Local_LookupFlagAttributes = new List<FlagAttributes>();
            OD.StoredDeviceData.Local_StatesFlagAttributes = new List<string>();
            OD.StoredDeviceData.Local_RawValues = new List<Tuple<DateTime, string>>();
            OD.StoredDeviceData.Local_MaintanenceInformation = new List<MaintenanceStruct>();
            OD.StoredDeviceData.Local_MaintanenceHistory = new List<Tuple<DateTime, string, bool>>();
            if (OD.objVal != null)
            {
                OD.objVal = new Byte[this.objVal.Length];
                Array.Copy(this.objVal, OD.objVal, this.objVal.Length);
            }
            return (OD);

        }
    }

    public class FlagAttributes
    {
        public string[] AttributeNames;
        public string[] AttributeValues;
    }

    public class FlagStates
    {
        public string DataAttributeElementName;  //Name of This Data Element name field in multi-element XML Data
        public string DataAttributeElementValue;  //Name of This Data Element value field in multi-element XML Data
        public string DataField; //Field that contains the actual data to display
        public string RawDataValue;
        public string DisplayedValue;
        public string SubField;
        public string UOM;
        public string[] StateCodes; // Possible RawData Values
        public string[] StateValues;// DisplayValue for Raw Values
        public string Archive;
        public string ValidValues;
        public object LastStates;
        public object CurrentStates;

    }

    public class DeviceDataStruct
    {
        //Complex Data Processing
        public string[] Local_FlagValueLastStates;
        public string[] Local_FlagValueCurrentStates;
        public string[] Local_RawValueLastStates;
        public string[] Local_RawValueCurrentStates;
        public string[] Local_ArchiveFlagValueCurrentStates;
        public string[] Local_ArchiveRawValueCurrentStates;
        public List<FlagAttributes> Local_FlagAttributes;
        public List<FlagAttributes> Local_ArchiveFlagAttributes;
        public List<FlagAttributes> Local_LookupFlagAttributes;
        public List<string> Local_StatesFlagAttributes;


        //Simple Data Processing
        public List<Tuple<DateTime, string>> Local_RawValues;

        //Maintenance
        public List<MaintenanceStruct> Local_MaintanenceInformation;
        public List<Tuple<DateTime, string, bool>> Local_MaintanenceHistory;

        //etc
        public object Etc01;
        public object Etc02;
        public object Etc03;
        public object Etc04;
        public object Etc05;
    }

    public enum MaintanenceCommands { NewTask, NewTaskDefaultFail, DoTasks, TaskSucessful, SkipOneMaintenanceCycle };
    public class MaintenanceStruct
    {
        public string DeviceUniqueID;
        public string URL;
        public int FailInterval;
        public int NormalInterval;
        public bool LastResult;
        public string NativeDeviceIdentifer;
        public DateTime LastTime;
        public DateTime NextTime;
        public int NumberOfConsecutiveFails;
        public int NumberOfConsecutiveFailsForDeviceToBeOffline;

    }

    public enum DeviceScriptsDataTypes { NoData, Json, XML };

    public class StatusMessagesStruct
    {
        public string ModuleSerialNumber;
        public string Module;
        public string StatusCode;
        public string Status;
        public string Comment;
        public string StatusMessage;
        public string LogCode;
    }

    public class RoomStruct
    {
        public string UniqueID;
        public string RoomName;
        public string Location;
        public string InterfaceUniqueIDs;
        public string AIProcessCode;
    }

    public class DeviceTemplateStruct
    {
        public string DeviceUniqueID;
        public string DeviceKey;
        public string DeviceType;
        public string DeviceClassID;
        public string ControllingDLL;
        public string UOMCode;
        public string XMLConfiguration;
        public string UndesignatedFieldsInfo;
        public int IntVal01;
        public int IntVal02;
        public int IntVal03;
        public int IntVal04;
        public string StrVal01;
        public string StrVal02;
        public string StrVal03;
        public string StrVal04;
        public Byte[] objVal;
        public string Comments; //End Of Database Stored Stuff
        public int Local_TableLoc;
    }

    public class PluginCommunicationStruct
    {
        public string UniqueNumber;
        public string ReferenceUniqueNumber;
        public string OriginPlugin;
        public string DestinationPlugin;
        public string PluginReferenceIdentifier;
        public string SecureCommunicationIDCode;
        public string DeviceUniqueID;
        public CommunicationResultCode ResultCode;

        //Data
        public PluginCommandsToPlugins Command;
        public PluginCommandsToPluginsAdendum AdendumResultCode;
        public PluginCommandsToPluginsHTMLSubCommands HTMLSubCommand;
        public PasswordStruct PWStruct;
        public int CommandNumber;
        public int Integer;
        public string String;
        public string String2;
        public string String3;
        public string String4;
        public double Double;
        public String[] Strings;
        public String[] Strings2;
        public String[] Strings3;
        public String[] Strings4;
        public String[] Strings5;
        public char[] Chars;
        public byte[] Bytes;
        public OutgoingDataStruct OutgoingDS;
        public object ReferenceObject;
        public object ReferenceObject2;
        public object ReferenceObject3;
        public object ReferenceObject4;
    }


    public class PluginServerDataStruct
    {
        public string UniqueNumber;
        public string ReferenceUniqueNumber;
        public string Plugin;
        public string ResponseCode;
        public string ReferenceIdentifier;
        public CommunicationResultCode ResultCode;
        public ServerEvents ServerEventReturnCommand;
        //Data
        public ServerPluginCommands Command;
        public int CommandNumber;
        public string String;
        public string String2;
        public string String3;
        public string String4;
        public double Double;
        public String[] Strings;
        public String[] Strings2;
        public String[] Strings3;
        public String[] Strings4;
        public String[] Strings5;
        public double[] Doubles;
        public double[] Doubles2;
        public OutgoingDataStruct DataStruct;
        public object ReferenceObject;
        public object ReferenceObject2;
        public object ReferenceObject3;
        public object ReferenceObject4;

    }

    public class SystemFlagStruct
    {
        public string FlagType;
        public string FlagCatagory;
        public string FlagName;
        public string StartupValue;
        public string UOM;
        public string ValidValues;
        public string Comments;
    }

    public class FlagArchiveStruct
    {
        public string SourceUniqueID;
        public long CreateTick;
        public long ChangeTick;
        public string Name;
        public string SubType;
        public string Value;
        public string RawValue;
        public string RoomUniqueID;
        public bool IsDeviceOffline;
    }

    public class FlagChangeHistory
    {
        public long ChangeTime;
        public string Value;
        public string RawValue;
        public string ChangedBy;
    }


    public class FlagDataStruct
    {
        public string Name;
        public string SubType;
        public string Value;
        public string UOM;
        public string RawValue;
        public string RoomUniqueID;
        public FlagChangeCodes ChangeMode;
        public long ChangeTick;
        public string ChangedBy;
        public long CreateTick;
        public string CreatedBy;
        public string CreatedValue;
        public string CreatedRawValue;
        public string SourceUniqueID;
        public string TranslatedValue;
        public string ValidValues;
        public string SystemFlag_FlagType;
        public string SystemFlag_FlagCatagory;
        public Int64 UniqueID;
        public int[] ModulesToNotifyOnChange;
        public string LogCode;
        public long LastLogTick;
        public long NextLogTick;
        public int MaxHistoryToSave;
        public bool IsDeviceOffline;
        public bool Archive;
        public FlagChangeHistory LastChangeHistory;
        public List<FlagChangeHistory> ChangeHistory;



        //Override Copy
        public FlagDataStruct DeepCopy()
        {
            FlagDataStruct OD = new FlagDataStruct();

            OD.Name = this.Name;
            OD.SubType = this.SubType;
            OD.Value = this.Value;
            OD.UOM = this.UOM;
            OD.RawValue = this.RawValue;
            OD.RoomUniqueID = this.RoomUniqueID;
            OD.ChangeMode = this.ChangeMode;
            OD.ChangeTick = this.ChangeTick;
            OD.ChangedBy = this.ChangedBy;
            OD.CreateTick = this.CreateTick;
            OD.CreatedBy = this.CreatedBy;
            OD.CreatedValue = this.CreatedValue;
            OD.CreatedRawValue = this.CreatedRawValue;
            OD.SourceUniqueID = this.SourceUniqueID;
            OD.TranslatedValue = this.TranslatedValue;
            OD.ValidValues = this.ValidValues;
            OD.SystemFlag_FlagType = this.SystemFlag_FlagType;
            OD.SystemFlag_FlagCatagory = this.SystemFlag_FlagCatagory;
            OD.UniqueID = this.UniqueID;
            OD.LogCode = this.LogCode;
            OD.LastLogTick = this.LastLogTick;
            OD.NextLogTick = this.NextLogTick;
            OD.MaxHistoryToSave = this.MaxHistoryToSave;
            OD.IsDeviceOffline = this.IsDeviceOffline;
            OD.Archive = this.Archive;
             if (OD.ModulesToNotifyOnChange != null)
            {
                OD.ModulesToNotifyOnChange = new int[this.ModulesToNotifyOnChange.Length];
                Array.Copy(this.ModulesToNotifyOnChange, OD.ModulesToNotifyOnChange, this.ModulesToNotifyOnChange.Length);
            }
            OD.LastChangeHistory = this.LastChangeHistory;
            OD.ChangeHistory = new List<FlagChangeHistory>(this.ChangeHistory);
            return (OD);
        }
    }

    public struct PluginFlagDataStruct
    {
        public FlagDataStruct GeneralFlagData;
        public long TransmittedFromServerTick;
    }

    public enum CommDataControlInfoStruct_CommDataControlInfoType { NormalRecord = 0, AlternativeResponse };
    public enum OutgoingDataStruct_WhatToDoWithGarbageData { Ignore = 0, SendWhenArrives, SendWhenValidData };
    public enum OutgoingDataStruct_StatusOfTransaction { TransactionRunning = 0, TransactionComplete, TransactionFailed, TransactionIOError, SpontaniousDataReceived, NoSpontaniousDataReceived, TransactionAborted, TransactionError };
    public enum LogCodes { Never = 0, Change, Start, End, Daily, Hourly, Minute, Second };
    public enum CommDataControlInfoStruct_WhatToWaitFor { Unknown = 0, SpecificCharacters, SpecificLength, Nothing, Anything }
    public enum ReplaceFieldValues_ReplaceFieldValuesType { DirectReplace, BracketedByChars }
    public enum ReplaceFieldValues_DataFieldValueLocation { Json }
    public struct CommDataControlInfoStruct
    {
        //Control Stuff
        public CommDataControlInfoStruct_CommDataControlInfoType Type;
        public uint Track;
        public uint TransmitDelayMiliseconds;
        public uint ReceiveDelayMiliseconds;
        public uint NextTrack;

        //IP Stuff
        public Byte[] CharactersToSend;
        public CommDataControlInfoStruct_WhatToWaitFor WaitForType;
        public Byte[] ResponseToWaitFor;
        public uint ReponseSizeToWaitFor;
        public Byte[] ActualResponseReceived;

        //HTTP-HTTPS Stuff
        //Sent Stuff    
        public string Method;
        public string Request;
        public string ContentType;
        public string Referer;
        public string Host;
        public string UserAgent;
        public string BodyData;
        public bool KeepAlive;
        public int Timeout;
        public bool UseDefaultCredentials;
        public Cookie[] CookiesToSend;
        public WebHeaderCollection HeadersToSend;

        //Returned Values
        public CookieCollection CookiesReturned;
        public HttpResponseHeader HeaderReturned;

    }

    public struct OutgoingDataStruct
    {
        public string RequestUniqueIDCode;
        public OutgoingDataStruct_StatusOfTransaction Status;
        public PluginCommandsToPlugins OriginalCommand;
        public string LocalIDTag;
        public string LocalData;
        public string LocalData2;
        public string LocalData3;
        public string LocalData4;
        public string LocalInterface;

        //What To do
        public bool WaitForIncomingDataToStop;

        //Comm Data Control Info
        public CommDataControlInfoStruct[] CommDataControlInfo;

        //Fields To Extract
        public struct ReplaceFieldValues
        {
            public string ReplaceFieldValueName;
            public string ReplaceFieldValueValue;
            public ReplaceFieldValues_ReplaceFieldValuesType HowToReplace;
            public char ReplaceStartingChar;
            public char ReplaceEndingChar;
            public ReplaceFieldValues_DataFieldValueLocation WhereToFindDataValue;
            public string DataFieldValueName;
        }
        public List<ReplaceFieldValues> ReplaceableFieldValues;

        //Control Information        
        public int MaxMilisecondsToWaitForIncommingData;
        public int SpontaniousData_SleepInterval;
        public DateTime ProcessCommunicationAtTimeTime;
        public int SecondsBetweenProcessCommunicationAtTime;
        public int NumberOfTimesToProcessCommunicationAtTime;
        public OutgoingDataStruct_WhatToDoWithGarbageData HowToProcessGarbageData;

        //Results
        public DateTime StartofCurrentTransaction;
        public DateTime TransactionStart;
        public DateTime LastDataSent;
        public DateTime LastDataReceived;
        public DateTime LastTransactionCompleted;

        //Debug Info
        public bool FullDebug;
        public int DebugFinalCommDataControlInfoIndex;
        public int DebugFinalTrack;
        public Exception Except;


        //Override Copy

        public OutgoingDataStruct DeepCopy()
        {
            OutgoingDataStruct OD;

            OD = this;

            if (OD.CommDataControlInfo != null)
            {
                OD.CommDataControlInfo = new CommDataControlInfoStruct[this.CommDataControlInfo.Length];
                Array.Copy(this.CommDataControlInfo, OD.CommDataControlInfo, this.CommDataControlInfo.Length); 
            }
            return (OD);

        }
    }

   // }

    public enum FlagActionCodes { Invalid, addorupdate, delete, addonly, updateonly };
    public enum FlagChangeCodes { Changeable = 0, NotChangeable, OwnerOnly };
    public enum HeartbeatTimeCode { Invalid, Nothing, NewSecond, NewMinute, NewHour, NewDay, NewWeek, NewMonth, NewYear }
    // public enum ServerEvents { Invalid, startup, flag, FlagComming, Heartbeat, TimeEvent, InformationCommingFromServer, InformationCommingFromPlugin, WatchdogProcess, CurrentServerStatus, ShutDownPlugin, StartupInfo, StartupInitialize, StartupCompleted, IncedentFlag, RequestedDBInfoReady, ProcessWordCommand };
    public enum ServerEvents { Invalid, startup, Heartbeat, TimeEvent, InformationCommingFromServer, InformationCommingFromPlugin, WatchdogProcess, CurrentServerStatus, ShutDownPlugin, StartupInfo, StartupInitialize, StartupCompleted, IncedentFlag, RequestedDBInfoReady, ProcessWordCommand };
    public enum ServerPluginCommands { Invalid, Accepted, Rejected, PluginSpecific, GeneralConfigInformation, ErrorMessage, GetEncryptionCode, AddDevice, GeneralMessage, LocalErrorMessage, LocalGeneralMesssage, SendAllIncedentFlags, DontSendAllIncedentFlags, SendJustFlagChangeIncedentFlags, DontSendJustFlagChangeIncedentFlags, AddRoom, GetDataBaseInfo, ProcessWordCommandCompleted, ProcessWordFlagCompleted, ProcessWordDisplayCompleted, AddToConfigurationInfo, UpdateDevice, DeleteDevice, AddActionItem, DeleteActionItem, AddPassword, DeviceIsOffline, DeviceIsOnline };
    public enum PluginCommandsToPlugins { Invalid, PluginSpecific, RequestLink, LinkAccepted, LinkRejected, LinkedCommReady, CancelLink, ClearBufferAndProcessCommunication, WaitOnIncomingData, StopWaitOnIncomingData, StartTimedLoopForData, EndTimedLoopForData, TransactionComplete, TransactionFailed, SpontaniousDataReceived, DataLinkLost, DataLinkReestablished, GarbageData, ChangeIntervalLoopTime, DoLoopNow, ActionCompleted, ProcessCommunicationWOClearingBuffer, ProcessCommunicationAtTime, ProcessNext, PriorityProcessNow, TransactionFailedDueToPriorityCommand, ProcessCommandWords, DirectCommand, HTMLProcess, MaintanenceRequest};
    public enum PluginCommandsToPluginsHTMLSubCommands { StartHTMLSession }
    public enum PluginCommandsToPluginsAdendum { Invalid, None, TooManyRetries };
    public enum PluginEvents { ProcessWebpage };
    public enum ServerStatusCodes { Invalid, InStartup, Running, InShutdown, InError, Unknown };
    public enum CommunicationResultCode { Invalid, Successful, UnSuccessful, InvalidPlugin, UnableToLoadPlugin };
    public enum PluginIncedentFlags { NewDevice, NewRoom, FlagChange, DeleteDevice };

    public struct NewFlagStruct
    {
        public string FlagName;
        public string FlagSubType;
        public string FlagValue;
        public string UOM;
        public string FlagRawValue;
        public string UniqueID;
        public string RoomUniqueID;
        public string SourceUniqueID;
        public FlagActionCodes Operation;
        public FlagChangeCodes Type;
        public string LogCode;
        public int MaxHistoryToSave;
        public string ValidValues;
        public bool IsDeviceOffline;

    }

    public struct PendingFlagQueueStruct
    {
        public string FlagName;
        public string FlagSubType;
    }

    public struct PluginErrorMessage  //when an unexpected error is caught
    {
        public Exception ExceptionData;
        public DateTime DateTimeOfException;
        public string Comment;
    }

    public struct PluginStatusStruct
    {
        //PluginStatusStruct (PluginStatusStruct o) :this()
        //{
        //    this.GetFlag = o.GetFlag;

        //}


        public bool SetFlag;
        //public bool GetFlag;
        public bool ToServer;
        public bool ToPlugin;
        public bool UEErrors;
        public int SetFlagCount;
        //public int GetFlagCount;
        public int ToServerCount;
        public int ToPluginCount;
        public int NumberOfUEErrorsCount;
        public bool StartupInitializedFinished;
        public int StartupInitializedMessage;
        public string StartupInitializedMessageSuffix;
        public Exception StartupInitializedError;
        public int LastAliveSequence;

    }

    public class CommandStruct
    {
        public string DeviceUniqueID;
        public string DirectCommand;
        public string Command;
        public int StateNumber;
        public object CommandObject;

    }

    public class ServerFunctionsStruct
    {
        public delegate Tuple<string, string, string>[] ServerGetFlagsInListDelegate(string[] FlagList);
        public ServerGetFlagsInListDelegate GetFlags;

        public delegate string ServerGetFlagDelegate(string Flag);
        public ServerGetFlagDelegate GetSingleFlag;

        public delegate FlagDataStruct GetSingleFlagFromServerFullDelegate(string Flag);
        public GetSingleFlagFromServerFullDelegate GetSingleFlagFromServerFull;

        public delegate string GetMacroDelegate(string MacroName, string MacroType, string MacroOwner);
        public GetMacroDelegate GetMacro;

        public delegate bool RunDirectCommandDelegate(ref PluginCommunicationStruct PCS, DeviceStruct DS);
        public RunDirectCommandDelegate RunDirectCommand;

        public ServerFunctionsStruct DeepCopy()
        {
            ServerFunctionsStruct OD = new ServerFunctionsStruct();

            OD.GetFlags = this.GetFlags;
            OD.GetSingleFlag = this.GetSingleFlag;
            OD.GetSingleFlagFromServerFull = this.GetSingleFlagFromServerFull;
            OD.GetMacro = this.GetMacro;
            OD.RunDirectCommand = this.RunDirectCommand;

            return (OD);
        }

    }
}
