﻿using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Cryptography;

using CHMPluginAPI;
using CHMPluginAPICommon;

//Required Parameters
///  UpdateInterval (In Miliseconds, default is 2500)

namespace CHMModules
{
    public class VantagePro
    {

        internal static _PluginCommonFunctions PluginCommonFunctions;
        internal static string LinkPlugin;
        internal static string LinkPluginReferenceIdentifier;
        internal static string LinkPluginSecureCommunicationIDCode;
//        internal static string LastDateSummaryArchived;
        private static bool FirstHeartbeat = true;
        private static bool StartupCompleteAndLinked = false;
        private static int HowManyWeatherValues;
        private static bool ArchivingRunning = false;
        private static _PluginDatabaseAccess PluginDatabaseAccess = new _PluginDatabaseAccess(Path.GetFileNameWithoutExtension((System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)));
        internal static uint ReceiveDelay = 500;


        internal struct VantageProDevices
        {
            internal DeviceStruct Devices;
            internal bool HasValidDevice;
            internal bool HasReceivedValidData;
            internal string CurrentValue;
            internal string CurrentRaw;
            internal string PreviousValue;
            internal string Room;
            internal DateTime LastChangeTime;
        }
        internal static VantageProDevices[] _VantageProDevices;

        /// <summary>
        /// PluginInitialize
        /// </summary>
        /// <param name="UniqueID"></param>
        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Vantage Pro Weather Station Console";
            ServerAccessFunctions.PluginSerialNumber = "00001-00010";
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
            return;
        }

        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}

       

        /// <summary>
        /// PluginStartupCompleted
        /// </summary>
        /// <param name="WhichEvent"></param>
        /// <param name="Value"></param>
        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            int index = 0;

            try
            {
                  _VantageProDevices = new VantageProDevices[_PluginCommonFunctions.LocalDevicesByUnique.Count];
                foreach (KeyValuePair<string, DeviceStruct > SN in _PluginCommonFunctions.LocalDevicesByUnique)
                {
                    _VantageProDevices[(int)index].Devices = SN.Value;
                    _VantageProDevices[(int)index].HasValidDevice = true;
                    _VantageProDevices[(int)index].HasReceivedValidData = false;
                    _VantageProDevices[(int)index].Room = PluginCommonFunctions.GetRoomFromUniqueID(SN.Value.RoomUniqueID);
                    index++;
                }
                //String[] Values, FieldNames;
                //PluginDatabaseAccess.FindDatedRecord("VantageProArchive", _PluginCommonFunctions.Interfaces[0].InterfaceUniqueID, "", "", out Values, out FieldNames, _PluginDatabaseAccess.PluginDataLocationType.Newest);
                //if (Values == null || Values.Length == 0)
                //    LastDateSummaryArchived = "";
                //else
                //    LastDateSummaryArchived = Values[0];

            }
            catch (Exception CHMAPIEx)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            if (StartupCompleteAndLinked)
            {
                if ((HeartbeatTimeCode)Value.HeartBeatTC == HeartbeatTimeCode.NewDay || FirstHeartbeat)
                {
                    ThreadedDataProcessing TDP = new ThreadedDataProcessing();

                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    PCS.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                    PCS.DestinationPlugin = LinkPlugin;
                    PCS.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    PCS.OutgoingDS= new OutgoingDataStruct();
                    PCS.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[3];

                    PCS.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'\n' };
                    PCS.OutgoingDS.CommDataControlInfo[0].ReceiveDelayMiliseconds = ReceiveDelay;
                    PCS.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r' };
                    PCS.OutgoingDS.CommDataControlInfo[1].CharactersToSend = new Byte[] { (Byte)'S', (Byte)'E', (Byte)'T', (Byte)'T', (Byte)'I', (Byte)'M', (Byte)'E', (Byte)'\n' };
                    PCS.OutgoingDS.CommDataControlInfo[1].ReceiveDelayMiliseconds = ReceiveDelay;
                    PCS.OutgoingDS.CommDataControlInfo[1].ResponseToWaitFor = new Byte[] { (Byte)'\x06' };
                    DateTime CT = _PluginCommonFunctions.CurrentTime;
                    Byte[] M = new Byte[6];
                    M[0] = (Byte)CT.Second;
                    M[1] = (Byte)CT.Minute;
                    M[2] = (Byte)CT.Hour;
                    M[3] = (Byte)CT.Day;
                    M[4] = (Byte)CT.Month;
                    M[5] = (Byte)(CT.Year - 1900);

                    PCS.OutgoingDS.CommDataControlInfo[2].CharactersToSend = TDP.CalculateCRC(M);
                    PCS.OutgoingDS.CommDataControlInfo[2].ResponseToWaitFor = new Byte[] { (Byte)'\x06' };
                    PCS.OutgoingDS.CommDataControlInfo[2].ReceiveDelayMiliseconds = ReceiveDelay;
                    PCS.OutgoingDS.MaxMilisecondsToWaitForIncommingData = 5000;
                    PCS.OutgoingDS.LocalIDTag = "SetTime";
 //                   _PCF.QueuePluginInformationToPlugin(PCS);

                }

                if (FirstHeartbeat)
                {
                    //PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    //PCS.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                    //PCS.DestinationPlugin = LinkPlugin;
                    //PCS.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    //PCS.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    //PCS.OutgoingDS = new OutgoingDataStruct();
                    //PCS.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[3];

                    //PCS.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'\n' };
                    //PCS.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r' };
                    //PCS.OutgoingDS.CommDataControlInfo[0].ReceiveDelayMiliseconds = ReceiveDelay;
                    //PCS.OutgoingDS.CommDataControlInfo[1].CharactersToSend = new Byte[] { (Byte)'E', (Byte)'E', (Byte)'B', (Byte)'R', (Byte)'D', (Byte)' ', (Byte)'2', (Byte)'D', (Byte)' ', (Byte)'1', (Byte)'\n' };
                    //PCS.OutgoingDS.CommDataControlInfo[1].ResponseToWaitFor = new Byte[] { (Byte)'\x06' };
                    //PCS.OutgoingDS.CommDataControlInfo[1].ReceiveDelayMiliseconds = ReceiveDelay;

                    //PCS.OutgoingDS.CommDataControlInfo[2].ReceiveDelayMiliseconds = ReceiveDelay;
                    //PCS.OutgoingDS.CommDataControlInfo[2].ReponseSizeToWaitFor = 1;
                    //PCS.OutgoingDS.MaxMilisecondsToWaitForIncommingData = 5000;
                    //PCS.OutgoingDS.LocalIDTag = "GetArchiveTime";
                    //_PCF.QueuePluginInformationToPlugin(PCS);

                    
                    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();

                    PCS2.Command = PluginCommandsToPlugins.StartTimedLoopForData;
                    PCS2.DestinationPlugin = LinkPlugin;
                    PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                    PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                    PCS2.OutgoingDS = new OutgoingDataStruct();
                    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[3];

                    PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'\n' };
                    PCS2.OutgoingDS.CommDataControlInfo[0].ReceiveDelayMiliseconds = ReceiveDelay;
                    PCS2.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r' };
                    PCS2.OutgoingDS.CommDataControlInfo[1].CharactersToSend = new Byte[] { (Byte)'L', (Byte)'O', (Byte)'O', (Byte)'P', (Byte)' ', (Byte)'1', (Byte)'\n' };
                    PCS2.OutgoingDS.MaxMilisecondsToWaitForIncommingData = 5000;
                    PCS2.OutgoingDS.CommDataControlInfo[1].ResponseToWaitFor = new Byte[] { (Byte)'\x06' };
                    PCS2.OutgoingDS.CommDataControlInfo[1].ReceiveDelayMiliseconds = ReceiveDelay;
                    PCS2.OutgoingDS.CommDataControlInfo[2].ReponseSizeToWaitFor = 99;
                    PCS2.OutgoingDS.CommDataControlInfo[2].ReceiveDelayMiliseconds = ReceiveDelay;
                    PCS2.OutgoingDS.LocalIDTag = "LOOP";
                    _PCF.QueuePluginInformationToPlugin(PCS2);
                }

                FirstHeartbeat = false;
            }


        }

        private static void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void InformationCommingFromServerServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }


        private static void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent)
        {
            PluginEventArgs Value;

            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();

            while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
            {

                OutgoingDataStruct ODS = Value.PluginData.OutgoingDS;

                if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionComplete)
                {
                    ThreadedDataProcessing TDP = new ThreadedDataProcessing();
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

                    if (ODS.LocalIDTag == "LOOP") //Loop Data
                    {
                        int index = ODS.CommDataControlInfo[2].ActualResponseReceived.Length - 2;
                        Byte[] C = new Byte[index];
                        Array.Copy(ODS.CommDataControlInfo[2].ActualResponseReceived, C, index);
                        Byte[] CRC = TDP.CalculateCRC(C);
                        int f;
                        if (CRC[index] != ODS.CommDataControlInfo[2].ActualResponseReceived[index] || CRC[index + 1] != ODS.CommDataControlInfo[2].ActualResponseReceived[index + 1])
                        {
                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(2000004, "", "");
                            continue;
                        }
                        for (int i = 0; i < _VantageProDevices.Length; i++)
                        {
                            try
                            {
                                if (int.TryParse(_VantageProDevices[i].Devices.DeviceIdentifier.Substring(3, 5), out f))
                                    TDP.ProcessFlags(ref _VantageProDevices[i], ODS.CommDataControlInfo[2].ActualResponseReceived, "VPD", true);
                                else
                                {
                                    if (_VantageProDevices[i].Devices.DeviceIdentifier.Substring(0, 9).ToUpper() == "VPDWCHILL") //Wind Chill
                                    {
                                        string UNProcessedFieldValue, ProcessedFieldValueString;
                                        double Temp, Wind;
                                        double d;
                                        VantageProDevices VPD = new VantageProDevices();
                                        VPD.Devices = new DeviceStruct();

                                        string[] WCParts = _VantageProDevices[i].Devices.DeviceIdentifier.Split(' ');
                                        VPD.Devices.DeviceIdentifier = WCParts[1];
                                        TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Temp);
                                        VPD.Devices.DeviceIdentifier = WCParts[2];
                                        TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Wind);

                                        if (TDP.CalculateWindChill(Temp, Wind, out d))
                                        {
                                            _VantageProDevices[i].PreviousValue = _VantageProDevices[i].CurrentValue;
                                            _VantageProDevices[i].CurrentValue = d.ToString();
                                            _VantageProDevices[i].HasReceivedValidData = true;
                                            _VantageProDevices[i].LastChangeTime = _PluginCommonFunctions.CurrentTime;
                                            TDP.CreateFlag(_VantageProDevices[i], Math.Round(d, 0).ToString(), d.ToString());
                                        }

                                    }


                                    if (_VantageProDevices[i].Devices.DeviceIdentifier.Substring(0, 9).ToUpper() == "VPDHINDEX") //Heat Index
                                    {
                                        string UNProcessedFieldValue, ProcessedFieldValueString;
                                        double Temp, Wind;
                                        double d;
                                        VantageProDevices VPD = new VantageProDevices();
                                        VPD.Devices = new DeviceStruct();

                                        string[] WCParts = _VantageProDevices[i].Devices.DeviceIdentifier.Split(' ');
                                        VPD.Devices.DeviceIdentifier = WCParts[1];
                                        TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Temp);
                                        VPD.Devices.DeviceIdentifier = WCParts[2];
                                        TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Wind);

                                        if (TDP.CalculateHeatIndex(Temp, Wind, out d))
                                        {
                                            _VantageProDevices[i].PreviousValue = _VantageProDevices[i].CurrentValue;
                                            _VantageProDevices[i].CurrentValue = d.ToString();
                                            _VantageProDevices[i].HasReceivedValidData = true;
                                            _VantageProDevices[i].LastChangeTime = _PluginCommonFunctions.CurrentTime;
                                            TDP.CreateFlag(_VantageProDevices[i], Math.Round(d, 0).ToString(), d.ToString());
                                        }

                                    }


                                    if (_VantageProDevices[i].Devices.DeviceIdentifier.Substring(0, 8).ToUpper() == "VPDDEWPT") //Dew Point
                                    {
                                        string UNProcessedFieldValue, ProcessedFieldValueString;
                                        double Temp, Humid;
                                        double d;
                                        VantageProDevices VPD = new VantageProDevices();
                                        VPD.Devices = new DeviceStruct();

                                        string[] WCParts = _VantageProDevices[i].Devices.DeviceIdentifier.Split(' ');
                                        VPD.Devices.DeviceIdentifier = WCParts[1];
                                        TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Temp);
                                        VPD.Devices.DeviceIdentifier = WCParts[2];
                                        TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Humid);

                                        if (TDP.CalculateDewPoint(Temp, Humid, out d))
                                        {
                                            _VantageProDevices[i].PreviousValue = _VantageProDevices[i].CurrentValue;
                                            _VantageProDevices[i].CurrentValue = d.ToString();
                                            _VantageProDevices[i].HasReceivedValidData = true;
                                            _VantageProDevices[i].LastChangeTime = _PluginCommonFunctions.CurrentTime;
                                            TDP.CreateFlag(_VantageProDevices[i], Math.Round(d, 0).ToString(), Math.Round(d, 4).ToString());
                                        }

                                    }

                                }
                            }
                            catch (Exception CHMAPIEx)
                            {
                                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                            }
                        }
                        //Now Check for Status Data
                        try
                        {
                            string StatMessage, StatMessageLog, Status;
                            if (_PluginCommonFunctions.LookupStatusDictionary("Battery", out StatMessage, out StatMessageLog, out Status))
                            {
                                string UNProcessedFieldValue, ProcessedFieldValueString;
                                double Battery;
                                VantageProDevices VPD = new VantageProDevices();
                                VPD.Devices = new DeviceStruct();
                                VPD.Devices.DeviceIdentifier = Status;
                                TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Battery);
                                double Voltage = ((Battery * 300) / 512) / 100.0;
                                Tuple<string, string, string, Tuple<int, string>[]> SU;
                                _PluginCommonFunctions.UOM.TryGetValue(72, out SU);
                                TDP.CreateFlagStatus(_PluginCommonFunctions.Interfaces[0], StatMessage, Voltage.ToString(), Battery.ToString(), SU.Item2);
                            }
                            if (_PluginCommonFunctions.LookupStatusDictionary("Sunrise", out StatMessage, out StatMessageLog, out Status))
                            {
                                string UNProcessedFieldValue, ProcessedFieldValueString;
                                double Sunrise;
                                VantageProDevices VPD = new VantageProDevices();
                                VPD.Devices = new DeviceStruct();
                                VPD.Devices.DeviceIdentifier = Status;
                                TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Sunrise);
                                int hour = (int)Sunrise / (int)100;
                                int minute = (int)Sunrise - (hour * 100);
                                TDP.CreateFlagStatus(_PluginCommonFunctions.Interfaces[0], StatMessage, hour.ToString("D2") + ":" + minute.ToString("D2") + ":00", Sunrise.ToString(),"");
                            }
                            if (_PluginCommonFunctions.LookupStatusDictionary("Sunset", out StatMessage, out StatMessageLog, out Status))
                            {
                                string UNProcessedFieldValue, ProcessedFieldValueString;
                                double Sunset;
                                VantageProDevices VPD = new VantageProDevices();
                                VPD.Devices = new DeviceStruct();
                                VPD.Devices.DeviceIdentifier = Status;
                                TDP.ProcessFieldValue(ODS.CommDataControlInfo[2].ActualResponseReceived, ref VPD, out UNProcessedFieldValue, out ProcessedFieldValueString, out Sunset);
                                int hour = (int)Sunset / (int)100;
                                int minute = (int)Sunset - (hour * 100);
                                TDP.CreateFlagStatus(_PluginCommonFunctions.Interfaces[0], StatMessage, hour.ToString("D2") + ":" + minute.ToString("D2") + ":00", Sunset.ToString(), "");
                            }
                        }
                        catch (Exception CHMAPIEx)
                        {
                            _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                        }
                        continue;
                    }


                //    if (ODS.LocalIDTag == "ArchiveRecord")  //Archive Loop Time
                //    {
                //        TDP.ArchiveTimerRoutine(ODS);
                //        continue;
                //    }

                //    if (ODS.LocalIDTag == "GetArchiveTime")  //Archive Loop Time
                //    {
                //        int ConsoleArchiveTimeset = ODS.CommDataControlInfo[2].ActualResponseReceived[0];
                //        int ConsoleArchiveInterval=PluginCommonFunctions.GetStartupField("ConsoleArchiveInterval", 5);

                //        int[] ValidValues = new int[] { 1, 5, 10, 15, 30, 60, 120 };

                //        if (ConsoleArchiveTimeset != ConsoleArchiveInterval && Array.IndexOf(ValidValues, ConsoleArchiveInterval) != -1)
                //        {
                //            ConsoleArchiveTimeset = ConsoleArchiveInterval;
                //            PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                //            PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                //            PCS2.DestinationPlugin = LinkPlugin;
                //            PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                //            PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                //            PCS2.OutgoingDS = new OutgoingDataStruct();
                //            PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[2];

                //            PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'\n' };
                //            PCS2.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r' };
                //            PCS2.OutgoingDS.CommDataControlInfo[0].ReceiveDelayMiliseconds = ReceiveDelay;
                //            string S = "SETPER " + ConsoleArchiveInterval.ToString() + " \n";
                //            PCS2.OutgoingDS.CommDataControlInfo[1].CharactersToSend =  Encoding.ASCII.GetBytes(S);
                //            PCS2.OutgoingDS.CommDataControlInfo[1].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r', (Byte)'O', (Byte)'K', (Byte)'\n', (Byte)'\r' };
                //            PCS2.OutgoingDS.CommDataControlInfo[1].ReceiveDelayMiliseconds = ReceiveDelay;
                //            PCS2.OutgoingDS.MaxMilisecondsToWaitForIncommingData = 10000;
                //            PCS2.OutgoingDS.LocalIDTag = "SetArchiveTime";
                //            _PCF.QueuePluginInformationToPlugin(PCS2);
                //            continue;
                //        }
                //        else
                //        {
                //            ODS.LocalIDTag = "SetArchiveTime";
                //        }
                //    }


                //    if (ODS.LocalIDTag == "SetArchiveTime")  //Archive Loop Time
                //    {
                //        PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
                //        PCS2.Command = PluginCommandsToPlugins.ClearBufferAndProcessCommunication;
                //        PCS2.DestinationPlugin = LinkPlugin;
                //        PCS2.PluginReferenceIdentifier = LinkPluginReferenceIdentifier;
                //        PCS2.SecureCommunicationIDCode = LinkPluginSecureCommunicationIDCode;

                //        PCS2.OutgoingDS = new OutgoingDataStruct();
                //        PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[2];

                //        PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'\n' };
                //        PCS2.OutgoingDS.CommDataControlInfo[0].ReceiveDelayMiliseconds = ReceiveDelay;
                //        PCS2.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r' };
                //        PCS2.OutgoingDS.CommDataControlInfo[1].CharactersToSend = new Byte[] { (Byte)'S', (Byte)'T', (Byte)'A', (Byte)'R', (Byte)'T', (Byte)'\n' };
                //        PCS2.OutgoingDS.CommDataControlInfo[1].ReceiveDelayMiliseconds = ReceiveDelay;

                //        PCS2.OutgoingDS.MaxMilisecondsToWaitForIncommingData = 5000;
                //        PCS2.OutgoingDS.LocalIDTag = "StartArchive";
                //        _PCF.QueuePluginInformationToPlugin(PCS2);
                //        ArchivingRunning = true;

                //        TDP.ArchiveTimerRoutine(PCS2.OutgoingDS);
                //        continue;
                //    }
                }

                if (Value.PluginData.Command == PluginCommandsToPlugins.GarbageData)
                {
                    continue;
                }

                if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionFailed)
                {
                    continue;
                }

                if (Value.PluginData.Command == PluginCommandsToPlugins.LinkedCommReady)
                {
                    StartupCompleteAndLinked = true;
                    continue;
                }

                if (Value.PluginData.Command == PluginCommandsToPlugins.RequestLink)
                {
                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

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

                if (Value.PluginData.Command == PluginCommandsToPlugins.CancelLink)
                {
                    //End Process Timer based on Interval

                    PluginCommunicationStruct PCS = new PluginCommunicationStruct();
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();


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

    class ThreadedDataProcessing
    {
        bool ArchiveStartup = false;

        
        int[] crc_table= new int[]  
        {
            0x0000,  0x1021,  0x2042,  0x3063,  0x4084,  0x50a5,  0x60c6,  0x70e7,  // 0x00
            0x8108,  0x9129,  0xa14a,  0xb16b,  0xc18c,  0xd1ad,  0xe1ce,  0xf1ef,  // 0x08  
            0x1231,  0x0210,  0x3273,  0x2252,  0x52b5,  0x4294,  0x72f7,  0x62d6,  // 0x10
            0x9339,  0x8318,  0xb37b,  0xa35a,  0xd3bd,  0xc39c,  0xf3ff,  0xe3de,  // 0x18
            0x2462,  0x3443,  0x0420,  0x1401,  0x64e6,  0x74c7,  0x44a4,  0x5485,  // 0x20
            0xa56a,  0xb54b,  0x8528,  0x9509,  0xe5ee,  0xf5cf,  0xc5ac,  0xd58d,  // 0x28
            0x3653,  0x2672,  0x1611,  0x0630,  0x76d7,  0x66f6,  0x5695,  0x46b4,  // 0x30
            0xb75b,  0xa77a,  0x9719,  0x8738,  0xf7df,  0xe7fe,  0xd79d,  0xc7bc,  // 0x38
            0x48c4,  0x58e5,  0x6886,  0x78a7,  0x0840,  0x1861,  0x2802,  0x3823,  // 0x40
            0xc9cc,  0xd9ed,  0xe98e,  0xf9af,  0x8948,  0x9969,  0xa90a,  0xb92b,  // 0x48
            0x5af5,  0x4ad4,  0x7ab7,  0x6a96,  0x1a71,  0x0a50,  0x3a33,  0x2a12,  // 0x50
            0xdbfd,  0xcbdc,  0xfbbf,  0xeb9e,  0x9b79,  0x8b58,  0xbb3b,  0xab1a,  // 0x58
            0x6ca6,  0x7c87,  0x4ce4,  0x5cc5,  0x2c22,  0x3c03,  0x0c60,  0x1c41,  // 0x60
            0xedae,  0xfd8f,  0xcdec,  0xddcd,  0xad2a,  0xbd0b,  0x8d68,  0x9d49,  // 0x68
            0x7e97,  0x6eb6,  0x5ed5,  0x4ef4,  0x3e13,  0x2e32,  0x1e51,  0x0e70,  // 0x70
            0xff9f,  0xefbe,  0xdfdd,  0xcffc,  0xbf1b,  0xaf3a,  0x9f59,  0x8f78,  // 0x78
            0x9188,  0x81a9,  0xb1ca,  0xa1eb,  0xd10c,  0xc12d,  0xf14e,  0xe16f,  // 0x80
            0x1080,  0x00a1,  0x30c2,  0x20e3,  0x5004,  0x4025,  0x7046,  0x6067,  // 0x88
            0x83b9,  0x9398,  0xa3fb,  0xb3da,  0xc33d,  0xd31c,  0xe37f,  0xf35e,  // 0x90
            0x02b1,  0x1290,  0x22f3,  0x32d2,  0x4235,  0x5214,  0x6277,  0x7256,  // 0x98
            0xb5ea,  0xa5cb,  0x95a8,  0x8589,  0xf56e,  0xe54f,  0xd52c,  0xc50d,  // 0xA0
            0x34e2,  0x24c3,  0x14a0,  0x0481,  0x7466,  0x6447,  0x5424,  0x4405,  // 0xA8
            0xa7db,  0xb7fa,  0x8799,  0x97b8,  0xe75f,  0xf77e,  0xc71d,  0xd73c,  // 0xB0
            0x26d3,  0x36f2,  0x0691,  0x16b0,  0x6657,  0x7676,  0x4615,  0x5634,  // 0xB8
            0xd94c,  0xc96d,  0xf90e,  0xe92f,  0x99c8,  0x89e9,  0xb98a,  0xa9ab,  // 0xC0
            0x5844,  0x4865,  0x7806,  0x6827,  0x18c0,  0x08e1,  0x3882,  0x28a3,  // 0xC8
            0xcb7d,  0xdb5c,  0xeb3f,  0xfb1e,  0x8bf9,  0x9bd8,  0xabbb,  0xbb9a,  // 0xD0
            0x4a75,  0x5a54,  0x6a37,  0x7a16,  0x0af1,  0x1ad0,  0x2ab3,  0x3a92,  // 0xD8
            0xfd2e,  0xed0f,  0xdd6c,  0xcd4d,  0xbdaa,  0xad8b,  0x9de8,  0x8dc9,  // 0xE0
            0x7c26,  0x6c07,  0x5c64,  0x4c45,  0x3ca2,  0x2c83,  0x1ce0,  0x0cc1,  // 0xE8
            0xef1f,  0xff3e,  0xcf5d,  0xdf7c,  0xaf9b,  0xbfba,  0x8fd9,  0x9ff8,  // 0xF0
            0x6e17,  0x7e36,  0x4e55,  0x5e74,  0x2e93,  0x3eb2,  0x0ed1,  0x1ef0,  // 0xF8
        };


        static double[][] HeatIndexTable = new double[][] {
         
        new double[] {-57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57, -57},    // -57f
        new double[] {-56.1, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56, -56},    // -56f
        new double[] {-55.1, -55.1, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55, -55},    // -55f
        new double[] {-54.1, -54.1, -54.1, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54, -54},    // -54f
        new double[] {-53.1, -53.1, -53.1, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53, -53},    // -53f
        new double[] {-52.1, -52.1, -52.1, -52.1, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52, -52},    // -52f
        new double[] {-51.1, -51.1, -51.1, -51.1, -51.1, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51, -51},    // -51f
        new double[] {-50.1, -50.1, -50.1, -50.1, -50.1, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50, -50},    // -50f
        new double[] {-49.1, -49.1, -49.1, -49.1, -49.1, -49.1, -49, -49, -49, -49, -49, -49, -49, -49, -49, -49, -49, -49, -49, -49, -49},    // -49f
        new double[] {-48.1, -48.1, -48.1, -48.1, -48.1, -48.1, -48, -48, -48, -48, -48, -48, -48, -48, -48, -48, -48, -48, -48, -48, -48},    // -48f
        new double[] {-47.1, -47.1, -47.1, -47.1, -47.1, -47.1, -47.1, -47, -47, -47, -47, -47, -47, -47, -47, -47, -47, -47, -47, -47, -47},    // -47f
        new double[] {-46.1, -46.1, -46.1, -46.1, -46.1, -46.1, -46.1, -46.1, -46, -46, -46, -46, -46, -46, -46, -46, -46, -46, -46, -46, -46},    // -46f
        new double[] {-45.1, -45.1, -45.1, -45.1, -45.1, -45.1, -45.1, -45.1, -45, -45, -45, -45, -45, -45, -45, -45, -45, -45, -45, -45, -45},    // -45f
        new double[] {-44.1, -44.1, -44.1, -44.1, -44.1, -44.1, -44.1, -44.1, -44.1, -44, -44, -44, -44, -44, -44, -44, -44, -44, -44, -44, -44},    // -44f
        new double[] {-43.1, -43.1, -43.1, -43.1, -43.1, -43.1, -43.1, -43.1, -43.1, -43, -43, -43, -43, -43, -43, -43, -43, -43, -43, -43, -43},    // -43f
        new double[] {-42.1, -42.1, -42.1, -42.1, -42.1, -42.1, -42.1, -42.1, -42.1, -42, -42, -42, -42, -42, -42, -42, -42, -42, -42, -42, -42},    // -42f
        new double[] {-41.1, -41.1, -41.1, -41.1, -41.1, -41.1, -41.1, -41.1, -41.1, -41.1, -41, -41, -41, -41, -41, -41, -41, -41, -41, -41, -41},    // -41f
        new double[] {-40.1, -40.1, -40.1, -40.1, -40.1, -40.1, -40.1, -40.1, -40.1, -40.1, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40},    // -40f
        new double[] {-39.1, -39.1, -39.1, -39.1, -39.1, -39.1, -39.1, -39.1, -39.1, -39.1, -39.1, -39, -39, -39, -39, -39, -39, -39, -39, -39, -39},    // -39f
        new double[] {-38.1, -38.1, -38.1, -38.1, -38.1, -38.1, -38.1, -38.1, -38.1, -38.1, -38.1, -38, -38, -38, -38, -38, -38, -38, -38, -38, -38},    // -38f
        new double[] {-37.1, -37.1, -37.1, -37.1, -37.1, -37.1, -37.1, -37.1, -37.1, -37.1, -37.1, -37, -37, -37, -37, -37, -37, -37, -37, -37, -37},    // -37f
        new double[] {-36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36.1, -36, -36, -36, -36, -36, -36, -36, -36, -36},    // -36f
        new double[] {-35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35.1, -35, -35, -35, -35, -35, -35, -35, -35, -35},    // -35f
        new double[] {-34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34.1, -34, -34, -34, -34, -34, -34, -34, -34},    // -34f
        new double[] {-33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33.1, -33, -33, -33, -33, -33, -33, -33, -33},    // -33f
        new double[] {-32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32.1, -32, -32, -32, -32, -32, -32, -32, -32},    // -32f
        new double[] {-31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31.1, -31, -31, -31, -31, -31, -31, -31, -31},    // -31f
        new double[] {-30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30.1, -30, -30, -30, -30, -30, -30, -30},    // -30f
        new double[] {-29.2, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29.1, -29, -29, -29, -29, -29, -29, -29},    // -29f
        new double[] {-28.2, -28.2, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28.1, -28, -28, -28, -28, -28, -28, -28},    // -28f
        new double[] {-27.2, -27.2, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27.1, -27, -27, -27, -27, -27, -27, -27},    // -27f
        new double[] {-26.2, -26.2, -26.2, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26.1, -26, -26, -26, -26, -26, -26},    // -26f
        new double[] {-25.2, -25.2, -25.2, -25.2, -25.1, -25.1, -25.1, -25.1, -25.1, -25.1, -25.1, -25.1, -25.1, -25.1, -25.1, -25, -25, -25, -25, -25, -25},    // -25f
        new double[] {-24.2, -24.2, -24.2, -24.2, -24.2, -24.1, -24.1, -24.1, -24.1, -24.1, -24.1, -24.1, -24.1, -24.1, -24.1, -24, -24, -24, -24, -24, -24},    // -24f
        new double[] {-23.2, -23.2, -23.2, -23.2, -23.2, -23.1, -23.1, -23.1, -23.1, -23.1, -23.1, -23.1, -23.1, -23.1, -23.1, -23, -23, -23, -23, -23, -23},    // -23f
        new double[] {-22.2, -22.2, -22.2, -22.2, -22.2, -22.2, -22.1, -22.1, -22.1, -22.1, -22.1, -22.1, -22.1, -22.1, -22.1, -22.1, -22, -22, -22, -22, -22},    // -22f
        new double[] {-21.2, -21.2, -21.2, -21.2, -21.2, -21.2, -21.1, -21.1, -21.1, -21.1, -21.1, -21.1, -21.1, -21.1, -21.1, -21.1, -21, -21, -21, -21, -21},    // -21f
        new double[] {-20.2, -20.2, -20.2, -20.2, -20.2, -20.2, -20.2, -20.1, -20.1, -20.1, -20.1, -20.1, -20.1, -20.1, -20.1, -20.1, -20, -20, -20, -20, -20},    // -20f
        new double[] {-19.2, -19.2, -19.2, -19.2, -19.2, -19.2, -19.2, -19.1, -19.1, -19.1, -19.1, -19.1, -19.1, -19.1, -19.1, -19.1, -19, -19, -19, -19, -19},    // -19f
        new double[] {-18.2, -18.2, -18.2, -18.2, -18.2, -18.2, -18.2, -18.2, -18.1, -18.1, -18.1, -18.1, -18.1, -18.1, -18.1, -18.1, -18, -18, -18, -18, -18},    // -18f
        new double[] {-17.2, -17.2, -17.2, -17.2, -17.2, -17.2, -17.2, -17.2, -17.1, -17.1, -17.1, -17.1, -17.1, -17.1, -17.1, -17.1, -17, -17, -17, -17, -17},    // -17f
        new double[] {-16.3, -16.2, -16.2, -16.2, -16.2, -16.2, -16.2, -16.2, -16.2, -16.1, -16.1, -16.1, -16.1, -16.1, -16.1, -16.1, -16.1, -16, -16, -16, -16},    // -16f
        new double[] {-15.3, -15.3, -15.2, -15.2, -15.2, -15.2, -15.2, -15.2, -15.2, -15.1, -15.1, -15.1, -15.1, -15.1, -15.1, -15.1, -15.1, -15, -15, -15, -15},    // -15f
        new double[] {-14.3, -14.3, -14.3, -14.2, -14.2, -14.2, -14.2, -14.2, -14.2, -14.2, -14.1, -14.1, -14.1, -14.1, -14.1, -14.1, -14.1, -14, -14, -14, -14},    // -14f
        new double[] {-13.3, -13.3, -13.3, -13.2, -13.2, -13.2, -13.2, -13.2, -13.2, -13.2, -13.1, -13.1, -13.1, -13.1, -13.1, -13.1, -13.1, -13, -13, -13, -13},    // -13f
        new double[] {-12.3, -12.3, -12.3, -12.3, -12.2, -12.2, -12.2, -12.2, -12.2, -12.2, -12.2, -12.1, -12.1, -12.1, -12.1, -12.1, -12.1, -12, -12, -12, -12},    // -12f
        new double[] {-11.3, -11.3, -11.3, -11.3, -11.3, -11.2, -11.2, -11.2, -11.2, -11.2, -11.2, -11.1, -11.1, -11.1, -11.1, -11.1, -11.1, -11, -11, -11, -11},    // -11f
        new double[] {-10.3, -10.3, -10.3, -10.3, -10.3, -10.2, -10.2, -10.2, -10.2, -10.2, -10.2, -10.1, -10.1, -10.1, -10.1, -10.1, -10.1, -10, -10, -10, -10},    // -10f
        new double[] {-9.3, -9.3, -9.3, -9.3, -9.3, -9.3, -9.2, -9.2, -9.2, -9.2, -9.2, -9.2, -9.1, -9.1, -9.1, -9.1, -9.1, -9.1, -9, -9, -9},    // -9f
        new double[] {-8.4, -8.3, -8.3, -8.3, -8.3, -8.3, -8.3, -8.2, -8.2, -8.2, -8.2, -8.2, -8.1, -8.1, -8.1, -8.1, -8.1, -8.1, -8, -8, -8},    // -8f
        new double[] {-7.4, -7.4, -7.3, -7.3, -7.3, -7.3, -7.3, -7.2, -7.2, -7.2, -7.2, -7.2, -7.1, -7.1, -7.1, -7.1, -7.1, -7.1, -7, -7, -7},    // -7f
        new double[] {-6.4, -6.4, -6.4, -6.3, -6.3, -6.3, -6.3, -6.3, -6.2, -6.2, -6.2, -6.2, -6.2, -6.1, -6.1, -6.1, -6.1, -6.1, -6, -6, -6},    // -6f
        new double[] {-5.4, -5.4, -5.4, -5.3, -5.3, -5.3, -5.3, -5.3, -5.2, -5.2, -5.2, -5.2, -5.2, -5.1, -5.1, -5.1, -5.1, -5.1, -5, -5, -5},    // -5f
        new double[] {-4.4, -4.4, -4.4, -4.4, -4.3, -4.3, -4.3, -4.3, -4.3, -4.2, -4.2, -4.2, -4.2, -4.1, -4.1, -4.1, -4.1, -4.1, -4, -4, -4},    // -4f
        new double[] {-3.4, -3.4, -3.4, -3.4, -3.4, -3.3, -3.3, -3.3, -3.3, -3.2, -3.2, -3.2, -3.2, -3.2, -3.1, -3.1, -3.1, -3.1, -3, -3, -3},    // -3f
        new double[] {-2.5, -2.4, -2.4, -2.4, -2.4, -2.3, -2.3, -2.3, -2.3, -2.3, -2.2, -2.2, -2.2, -2.2, -2.1, -2.1, -2.1, -2.1, -2, -2, -2},    // -2f
        new double[] {-1.5, -1.5, -1.4, -1.4, -1.4, -1.4, -1.3, -1.3, -1.3, -1.3, -1.2, -1.2, -1.2, -1.2, -1.1, -1.1, -1.1, -1.1, -1, -1, -1},    // -1f
        new double[] {-0.5, -0.5, -0.4, -0.4, -0.4, -0.4, -0.3, -0.3, -0.3, -0.3, -0.2, -0.2, -0.2, -0.2, -0.1, -0.1, -0.1, -0.1, 0, 0, 0},    // 0f
        new double[] {0.5, 0.5, 0.5, 0.6, 0.6, 0.6, 0.6, 0.7, 0.7, 0.7, 0.7, 0.8, 0.8, 0.8, 0.8, 0.9, 0.9, 0.9, 0.9, 1, 1},    // 1f
        new double[] {1.5, 1.5, 1.5, 1.5, 1.6, 1.6, 1.6, 1.7, 1.7, 1.7, 1.7, 1.8, 1.8, 1.8, 1.8, 1.9, 1.9, 1.9, 1.9, 2, 2},    // 2f
        new double[] {2.4, 2.5, 2.5, 2.5, 2.6, 2.6, 2.6, 2.6, 2.7, 2.7, 2.7, 2.7, 2.8, 2.8, 2.8, 2.9, 2.9, 2.9, 2.9, 3, 3},    // 3f
        new double[] {3.4, 3.4, 3.5, 3.5, 3.5, 3.6, 3.6, 3.6, 3.7, 3.7, 3.7, 3.7, 3.8, 3.8, 3.8, 3.9, 3.9, 3.9, 3.9, 4, 4},    // 4f
        new double[] {4.4, 4.4, 4.5, 4.5, 4.5, 4.5, 4.6, 4.6, 4.6, 4.7, 4.7, 4.7, 4.8, 4.8, 4.8, 4.8, 4.9, 4.9, 4.9, 5, 5},    // 5f
        new double[] {5.4, 5.4, 5.4, 5.5, 5.5, 5.5, 5.6, 5.6, 5.6, 5.7, 5.7, 5.7, 5.7, 5.8, 5.8, 5.8, 5.9, 5.9, 5.9, 6, 6},    // 6f
        new double[] {6.3, 6.4, 6.4, 6.4, 6.5, 6.5, 6.5, 6.6, 6.6, 6.6, 6.7, 6.7, 6.7, 6.8, 6.8, 6.8, 6.9, 6.9, 6.9, 7, 7},    // 7f
        new double[] {7.3, 7.3, 7.4, 7.4, 7.5, 7.5, 7.5, 7.6, 7.6, 7.6, 7.7, 7.7, 7.7, 7.8, 7.8, 7.8, 7.9, 7.9, 7.9, 8, 8},    // 8f
        new double[] {8.3, 8.3, 8.4, 8.4, 8.4, 8.5, 8.5, 8.5, 8.6, 8.6, 8.6, 8.7, 8.7, 8.8, 8.8, 8.8, 8.9, 8.9, 8.9, 9, 9},    // 9f
        new double[] {9.3, 9.3, 9.3, 9.4, 9.4, 9.4, 9.5, 9.5, 9.6, 9.6, 9.6, 9.7, 9.7, 9.7, 9.8, 9.8, 9.9, 9.9, 9.9, 10, 10},    // 10f
        new double[] {10.2, 10.3, 10.3, 10.3, 10.4, 10.4, 10.5, 10.5, 10.5, 10.6, 10.6, 10.7, 10.7, 10.7, 10.8, 10.8, 10.8, 10.9, 10.9, 11, 11},    // 11f
        new double[] {11.2, 11.2, 11.3, 11.3, 11.4, 11.4, 11.4, 11.5, 11.5, 11.6, 11.6, 11.6, 11.7, 11.7, 11.8, 11.8, 11.8, 11.9, 11.9, 12, 12},    // 12f
        new double[] {12.2, 12.2, 12.2, 12.3, 12.3, 12.4, 12.4, 12.5, 12.5, 12.5, 12.6, 12.6, 12.7, 12.7, 12.7, 12.8, 12.8, 12.9, 12.9, 13, 13},    // 13f
        new double[] {13.1, 13.2, 13.2, 13.3, 13.3, 13.3, 13.4, 13.4, 13.5, 13.5, 13.6, 13.6, 13.7, 13.7, 13.7, 13.8, 13.8, 13.9, 13.9, 14, 14},    // 14f
        new double[] {14.1, 14.1, 14.2, 14.2, 14.3, 14.3, 14.4, 14.4, 14.5, 14.5, 14.5, 14.6, 14.6, 14.7, 14.7, 14.8, 14.8, 14.9, 14.9, 15, 15},    // 15f
        new double[] {15.1, 15.1, 15.1, 15.2, 15.2, 15.3, 15.3, 15.4, 15.4, 15.5, 15.5, 15.6, 15.6, 15.7, 15.7, 15.8, 15.8, 15.9, 15.9, 16, 16},    // 16f
        new double[] {16, 16.1, 16.1, 16.2, 16.2, 16.3, 16.3, 16.4, 16.4, 16.5, 16.5, 16.6, 16.6, 16.7, 16.7, 16.8, 16.8, 16.9, 16.9, 17, 17},    // 17f
        new double[] {17, 17, 17.1, 17.1, 17.2, 17.2, 17.3, 17.3, 17.4, 17.4, 17.5, 17.5, 17.6, 17.6, 17.7, 17.7, 17.8, 17.8, 17.9, 17.9, 18},    // 18f
        new double[] {17.9, 18, 18, 18.1, 18.1, 18.2, 18.3, 18.3, 18.4, 18.4, 18.5, 18.5, 18.6, 18.6, 18.7, 18.7, 18.8, 18.8, 18.9, 18.9, 19},    // 19f
        new double[] {18.9, 18.9, 19, 19.1, 19.1, 19.2, 19.2, 19.3, 19.3, 19.4, 19.4, 19.5, 19.6, 19.6, 19.7, 19.7, 19.8, 19.8, 19.9, 19.9, 20},    // 20f
        new double[] {19.8, 19.9, 20, 20, 20.1, 20.1, 20.2, 20.2, 20.3, 20.4, 20.4, 20.5, 20.5, 20.6, 20.7, 20.7, 20.8, 20.8, 20.9, 20.9, 21},    // 21f
        new double[] {20.8, 20.9, 20.9, 21, 21, 21.1, 21.2, 21.2, 21.3, 21.3, 21.4, 21.5, 21.5, 21.6, 21.6, 21.7, 21.8, 21.8, 21.9, 21.9, 22},    // 22f
        new double[] {21.7, 21.8, 21.9, 21.9, 22, 22.1, 22.1, 22.2, 22.2, 22.3, 22.4, 22.4, 22.5, 22.6, 22.6, 22.7, 22.7, 22.8, 22.9, 22.9, 23},    // 23f
        new double[] {22.7, 22.8, 22.8, 22.9, 23, 23, 23.1, 23.1, 23.2, 23.3, 23.3, 23.4, 23.5, 23.5, 23.6, 23.7, 23.7, 23.8, 23.9, 23.9, 24},    // 24f
        new double[] {23.6, 23.7, 23.8, 23.8, 23.9, 24, 24, 24.1, 24.2, 24.3, 24.3, 24.4, 24.5, 24.5, 24.6, 24.7, 24.7, 24.8, 24.9, 24.9, 25},    // 25f
        new double[] {24.6, 24.7, 24.7, 24.8, 24.9, 24.9, 25, 25.1, 25.1, 25.2, 25.3, 25.4, 25.4, 25.5, 25.6, 25.6, 25.7, 25.8, 25.9, 25.9, 26},    // 26f
        new double[] {25.5, 25.6, 25.7, 25.7, 25.8, 25.9, 26, 26, 26.1, 26.2, 26.3, 26.3, 26.4, 26.5, 26.6, 26.6, 26.7, 26.8, 26.9, 26.9, 27},    // 27f
        new double[] {26.5, 26.5, 26.6, 26.7, 26.8, 26.8, 26.9, 27, 27.1, 27.2, 27.2, 27.3, 27.4, 27.5, 27.5, 27.6, 27.7, 27.8, 27.8, 27.9, 28},    // 28f
        new double[] {27.4, 27.5, 27.6, 27.6, 27.7, 27.8, 27.9, 28, 28, 28.1, 28.2, 28.3, 28.4, 28.4, 28.5, 28.6, 28.7, 28.8, 28.8, 28.9, 29},    // 29f
        new double[] {28.3, 28.4, 28.5, 28.6, 28.7, 28.8, 28.8, 28.9, 29, 29.1, 29.2, 29.3, 29.3, 29.4, 29.5, 29.6, 29.7, 29.8, 29.8, 29.9, 30},    // 30f
        new double[] {29.3, 29.4, 29.5, 29.5, 29.6, 29.7, 29.8, 29.9, 30, 30.1, 30.1, 30.2, 30.3, 30.4, 30.5, 30.6, 30.7, 30.8, 30.8, 30.9, 31},    // 31f
        new double[] {30.2, 30.3, 30.4, 30.5, 30.6, 30.7, 30.8, 30.9, 31, 31, 31.1, 31.2, 31.3, 31.4, 31.5, 31.6, 31.7, 31.8, 31.9, 31.9, 32},    // 32f
        new double[] {31.2, 31.2, 31.3, 31.4, 31.5, 31.6, 31.7, 31.8, 31.9, 32, 32.1, 32.2, 32.3, 32.4, 32.5, 32.6, 32.7, 32.8, 32.8, 32.9, 33},    // 33f
        new double[] {32.1, 32.2, 32.3, 32.4, 32.5, 32.6, 32.7, 32.8, 32.9, 33, 33.1, 33.2, 33.3, 33.4, 33.5, 33.5, 33.6, 33.7, 33.8, 33.9, 34},    // 34f
        new double[] {33, 33.1, 33.2, 33.3, 33.4, 33.5, 33.7, 33.8, 33.9, 34, 34.1, 34.2, 34.3, 34.4, 34.5, 34.6, 34.7, 34.8, 34.9, 35, 35.1},    // 35f
        new double[] {34, 34.1, 34.2, 34.3, 34.4, 34.5, 34.6, 34.7, 34.8, 34.9, 35, 35.1, 35.2, 35.3, 35.4, 35.6, 35.7, 35.8, 35.9, 36, 36.1},    // 36f
        new double[] {34.9, 35, 35.1, 35.2, 35.3, 35.4, 35.6, 35.7, 35.8, 35.9, 36, 36.1, 36.2, 36.3, 36.4, 36.6, 36.7, 36.8, 36.9, 37, 37.1},    // 37f
        new double[] {35.8, 35.9, 36.1, 36.2, 36.3, 36.4, 36.5, 36.6, 36.8, 36.9, 37, 37.1, 37.2, 37.3, 37.4, 37.6, 37.7, 37.8, 37.9, 38, 38.1},    // 38f
        new double[] {36.8, 36.9, 37, 37.1, 37.2, 37.4, 37.5, 37.6, 37.7, 37.8, 38, 38.1, 38.2, 38.3, 38.4, 38.6, 38.7, 38.8, 38.9, 39, 39.2},    // 39f
        new double[] {37.7, 37.8, 37.9, 38, 38.2, 38.3, 38.4, 38.5, 38.7, 38.8, 38.9, 39, 39.2, 39.3, 39.4, 39.5, 39.7, 39.8, 39.9, 40.1, 40.2},    // 40f
        new double[] {38.6, 38.7, 38.9, 39, 39.1, 39.3, 39.4, 39.5, 39.6, 39.8, 39.9, 40, 40.2, 40.3, 40.4, 40.6, 40.7, 40.8, 40.9, 41.1, 41.2},    // 41f
        new double[] {39.5, 39.7, 39.8, 39.9, 40.1, 40.2, 40.3, 40.5, 40.6, 40.8, 40.9, 41, 41.2, 41.3, 41.4, 41.6, 41.7, 41.8, 42, 42.1, 42.2},    // 42f
        new double[] {40.5, 40.6, 40.7, 40.9, 41, 41.2, 41.3, 41.4, 41.6, 41.7, 41.9, 42, 42.2, 42.3, 42.4, 42.6, 42.7, 42.9, 43, 43.1, 43.3},    // 43f
        new double[] {41.4, 41.6, 41.7, 41.9, 42, 42.1, 42.3, 42.4, 42.6, 42.7, 42.9, 43, 43.2, 43.3, 43.5, 43.6, 43.8, 43.9, 44.1, 44.2, 44.4},    // 44f
        new double[] {42.3, 42.5, 42.6, 42.8, 42.9, 43.1, 43.3, 43.4, 43.6, 43.7, 43.9, 44, 44.2, 44.3, 44.5, 44.6, 44.8, 44.9, 45.1, 45.2, 45.4},    // 45f
        new double[] {43.3, 43.4, 43.6, 43.7, 43.9, 44.1, 44.2, 44.4, 44.5, 44.7, 44.9, 45, 45.2, 45.3, 45.5, 45.6, 45.8, 46, 46.1, 46.3, 46.4},    // 46f
        new double[] {44.2, 44.3, 44.5, 44.7, 44.8, 45, 45.2, 45.3, 45.5, 45.7, 45.8, 46, 46.2, 46.3, 46.5, 46.7, 46.8, 47, 47.2, 47.3, 47.5},    // 47f
        new double[] {45.1, 45.3, 45.4, 45.6, 45.8, 46, 46.1, 46.3, 46.5, 46.7, 46.8, 47, 47.2, 47.3, 47.5, 47.7, 47.9, 48, 48.2, 48.4, 48.6},    // 48f
        new double[] {46, 46.2, 46.4, 46.6, 46.8, 46.9, 47.1, 47.3, 47.5, 47.7, 47.8, 48, 48.2, 48.4, 48.6, 48.7, 48.9, 49.1, 49.3, 49.5, 49.6},    // 49f
        new double[] {47, 47.2, 47.3, 47.5, 47.7, 47.9, 48.1, 48.3, 48.5, 48.7, 48.8, 49, 49.2, 49.4, 49.6, 49.8, 50, 50.1, 50.3, 50.5, 50.7},    // 50f
        new double[] {47.6, 47.8, 48, 48.2, 48.4, 48.6, 48.9, 49.1, 49.3, 49.5, 49.7, 49.9, 50.1, 50.3, 50.5, 50.7, 50.9, 51, 51.2, 51.4, 51.6},    // 51f
        new double[] {48.3, 48.5, 48.7, 48.9, 49.1, 49.3, 49.6, 49.9, 50.1, 50.3, 50.5, 50.8, 51, 51.2, 51.4, 51.6, 51.8, 52, 52.2, 52.3, 52.5},    // 52f
        new double[] {49, 49.2, 49.4, 49.6, 49.9, 50.1, 50.4, 50.7, 50.9, 51.2, 51.4, 51.6, 51.9, 52.1, 52.3, 52.5, 52.7, 52.9, 53.1, 53.3, 53.4},    // 53f
        new double[] {49.7, 49.9, 50.1, 50.4, 50.6, 50.9, 51.2, 51.5, 51.8, 52, 52.3, 52.6, 52.8, 53, 53.3, 53.5, 53.7, 53.9, 54.1, 54.2, 54.4},    // 54f
        new double[] {50.4, 50.6, 50.9, 51.1, 51.4, 51.6, 52.1, 52.4, 52.7, 52.9, 53.2, 53.5, 53.7, 54, 54.2, 54.5, 54.7, 54.9, 55.1, 55.2, 55.4},    // 55f
        new double[] {51.2, 51.3, 51.6, 51.9, 52.2, 52.4, 52.9, 53.2, 53.5, 53.8, 54.1, 54.4, 54.7, 55, 55.2, 55.4, 55.7, 55.9, 56.1, 56.2, 56.4},    // 56f
        new double[] {51.9, 52.1, 52.4, 52.7, 53, 53.2, 53.8, 54.1, 54.5, 54.8, 55.1, 55.4, 55.7, 56, 56.2, 56.5, 56.7, 56.9, 57.1, 57.2, 57.4},    // 57f
        new double[] {52.7, 52.9, 53.2, 53.5, 53.8, 54.1, 54.7, 55, 55.4, 55.7, 56, 56.4, 56.7, 57, 57.2, 57.5, 57.8, 57.9, 58.1, 58.3, 58.4},    // 58f
        new double[] {53.4, 53.6, 54, 54.3, 54.6, 54.9, 55.6, 55.9, 56.3, 56.7, 57, 57.4, 57.7, 58, 58.3, 58.5, 58.8, 59, 59.2, 59.4, 59.5},    // 59f
        new double[] {54.2, 54.4, 54.8, 55.1, 55.5, 55.8, 56.5, 56.9, 57.3, 57.7, 58, 58.4, 58.7, 59.1, 59.4, 59.6, 59.9, 60.1, 60.3, 60.4, 60.6},    // 60f
        new double[] {55.1, 55.3, 55.7, 56, 56.3, 56.7, 57.4, 57.8, 58.3, 58.7, 59, 59.4, 59.8, 60.2, 60.4, 60.7, 61, 61.2, 61.4, 61.5, 61.7},    // 61f
        new double[] {55.9, 56.1, 56.5, 56.9, 57.2, 57.6, 58.4, 58.8, 59.3, 59.7, 60.1, 60.5, 60.8, 61.3, 61.5, 61.8, 62.2, 62.3, 62.5, 62.7, 62.8},    // 62f
        new double[] {56.7, 56.9, 57.4, 57.7, 58.1, 58.5, 59.3, 59.8, 60.3, 60.7, 61.1, 61.6, 61.9, 62.4, 62.7, 62.9, 63.3, 63.5, 63.7, 63.8, 64},    // 63f
        new double[] {57.6, 57.8, 58.3, 58.6, 59, 59.4, 60.3, 60.8, 61.3, 61.8, 62.2, 62.7, 63.1, 63.5, 63.8, 64.1, 64.5, 64.6, 64.8, 65, 65.1},    // 64f
        new double[] {58.5, 58.7, 59.2, 59.6, 60, 60.4, 61.3, 61.8, 62.4, 62.8, 63.3, 63.8, 64.2, 64.7, 65, 65.3, 65.7, 65.8, 66, 66.2, 66.3},    // 65f
        new double[] {59.4, 59.6, 60.1, 60.5, 60.9, 61.3, 62.4, 62.9, 63.4, 63.9, 64.4, 64.9, 65.3, 65.8, 66.2, 66.5, 66.9, 67, 67.3, 67.4, 67.5},    // 66f
        new double[] {60.3, 60.5, 61.1, 61.5, 61.9, 62.3, 63.4, 63.9, 64.5, 65.1, 65.6, 66.1, 66.5, 67, 67.4, 67.7, 68.1, 68.3, 68.5, 68.6, 68.8},    // 67f
        new double[] {61.2, 61.5, 62, 62.6, 63.1, 63.8, 64.4, 65, 65.6, 66.2, 66.7, 67.2, 67.7, 68.2, 68.6, 68.9, 69.3, 69.5, 69.7, 69.9, 70},    // 68f
        new double[] {62.4, 62.9, 63.3, 63.8, 64.3, 64.8, 65.4, 66, 66.6, 67.1, 67.6, 68, 68.4, 68.9, 69.4, 70, 70.5, 70.8, 71, 71.3, 71.9},    // 69f
        new double[] {64, 64.1, 64.5, 65, 65.5, 65.9, 66.4, 66.9, 67.3, 67.8, 68.3, 68.7, 69.2, 69.7, 70.1, 70.6, 71.1, 71.5, 72, 72.5, 73.5},    // 70f
        new double[] {65.4, 65.5, 65.9, 66.4, 66.8, 67.3, 67.7, 68.2, 68.6, 69.1, 69.6, 70, 70.5, 70.9, 71.4, 71.8, 72.3, 72.8, 73.2, 73.7, 74.7},    // 71f
        new double[] {66.7, 66.8, 67.2, 67.6, 68.1, 68.6, 69.1, 69.6, 70.1, 70.6, 71.1, 71.5, 71.9, 72.3, 72.7, 73, 73.4, 73.8, 74.2, 74.8, 75.6},    // 72f
        new double[] {68, 68.1, 68.6, 69.2, 69.7, 70.2, 70.7, 71.2, 71.7, 72.1, 72.5, 73, 73.4, 73.7, 74.1, 74.5, 74.8, 75.1, 75.5, 75.8, 76.6},    // 73f
        new double[] {69.2, 69.5, 69.8, 70.3, 71, 71.7, 72.3, 72.7, 73.1, 73.4, 73.7, 74.2, 74.7, 75.2, 75.6, 75.9, 76, 76.2, 76.4, 77, 77.6},    // 74f
        new double[] {70.4, 71.2, 71.7, 72.1, 72.5, 72.9, 73.3, 73.8, 74.2, 74.7, 75.1, 75.5, 75.9, 76.3, 76.7, 77.1, 77.5, 78, 78.4, 78.7, 78.8},    // 75f
        new double[] {71.5, 72.5, 73.1, 73.6, 74.1, 74.3, 74.5, 74.8, 75.1, 75.6, 76, 76.3, 76.7, 77.2, 77.8, 78.4, 78.9, 79.2, 79.5, 80, 80.3},    // 76f
        new double[] {72.6, 73.7, 74.6, 75.2, 75.5, 75.7, 75.8, 76, 76.2, 76.5, 76.8, 77.3, 77.8, 78.3, 78.9, 79.5, 80, 80.6, 81, 81.5, 82.2},    // 77f
        new double[] {73.6, 73.8, 74.6, 75.4, 75.9, 76.2, 76.4, 76.7, 77.1, 77.6, 78, 78.5, 78.9, 79.4, 80, 80.6, 81.4, 82.1, 82.8, 83.4, 84.4},    // 78f
        new double[] {74.6, 74.7, 75.1, 75.6, 76.1, 76.7, 77.2, 77.8, 78.2, 78.7, 79.2, 79.6, 80.1, 80.7, 81.3, 82, 82.8, 83.7, 84.7, 85.8, 86.8},    // 79f
        new double[] {75.6, 75.7, 76.1, 76.5, 77, 77.5, 78, 78.5, 79, 79.6, 80.1, 80.7, 81.3, 82, 82.7, 83.6, 84.5, 85.6, 86.8, 88.1, 89.6},    // 80f
        new double[] {76.5, 76.6, 77, 77.5, 77.9, 78.4, 79, 79.5, 80, 80.6, 81.2, 81.9, 82.7, 83.5, 84.4, 85.4, 86.5, 87.8, 89.2, 90.7, 92.5},    // 81f
        new double[] {77.4, 77.5, 77.9, 78.4, 78.9, 79.5, 80.1, 80.7, 81.3, 81.9, 82.6, 83.4, 84.2, 85.2, 86.3, 87.5, 88.9, 90.4, 92.1, 93.9, 95.7},    // 82f
        new double[] {78.3, 78.5, 79.1, 79.6, 80, 80.4, 81, 81.8, 82.7, 83.5, 84.2, 84.9, 85.7, 86.9, 88.4, 90.1, 91.7, 93.1, 94.8, 97.3, 99.2},    // 83f
        new double[] {79.1, 79.9, 80.6, 80.8, 81, 81.5, 82.3, 83.3, 84.3, 85.1, 85.8, 86.6, 87.7, 89.2, 90.9, 92.7, 94.5, 96.1, 98, 100.5, 103.1},    // 84f
        new double[] {80, 80.9, 81.6, 81.7, 81.8, 82.4, 83.2, 84.2, 85.1, 86, 86.8, 87.9, 89.4, 91.1, 92.9, 94.6, 96.4, 98.4, 100.9, 104.1, 107.6},    // 85f
        new double[] {80.8, 81.8, 82.5, 82.6, 82.8, 83.4, 84.3, 85.3, 86.2, 87.1, 88.2, 89.7, 91.5, 93.4, 95.3, 97, 98.8, 101.2, 104.5, 108.6, 113.2},    // 86f
        new double[] {81.6, 82.6, 83.3, 83.5, 83.7, 84.3, 85.2, 86.2, 87.4, 88.6, 90.1, 91.8, 93.6, 95.6, 97.5, 99.6, 102.1, 105.4, 109.8, 114.9, 120.5},    // 87f
        new double[] {82.4, 83.3, 83.7, 84, 84.4, 84.9, 85.7, 86.8, 88.1, 89.7, 91.4, 93.1, 94.9, 96.8, 99, 101.4, 104.4, 108.5, 114.2, 121.9, 130.4},    // 88f
        new double[] {83.1, 83.6, 83.7, 84.4, 85.2, 86, 87, 88.3, 90, 91.9, 93.7, 95.4, 97.3, 99.6, 102.3, 105.5, 109.2, 113.8, 120.9, 132, 144.2},    // 89f
        new double[] {83.9, 84.2, 85.5, 86.2, 86.6, 87.4, 89, 89.9, 91.2, 93.3, 95.1, 97.4, 99.8, 102.8, 105.8, 109.4, 113.3, 120.3, 127.1, 142.9, 163.4},    // 90f
 
        new double[] {84.7, 85.1, 86.4, 87.2, 87.6, 88.6, 90.2, 91.2, 92.9, 95.1, 97.1, 99.7, 102.3, 105.6, 109, 113.4, 118.2, 126.5, 135.1, 156.6, 190.2},    // 91f
        new double[] {85.4, 86, 87.3, 88.2, 88.6, 89.8, 91.4, 92.6, 94.5, 96.9, 99.1, 102.1, 104.9, 108.6, 112.3, 117.8, 123.6, 133.7, 144.3, 173.2, 227.3},    // 92f
        new double[] {86.2, 86.9, 88.2, 89.2, 89.7, 91, 92.7, 94, 96.3, 98.8, 101.2, 104.6, 107.7, 111.7, 115.7, 122.7, 129.7, 141.7, 155.1, 193.3, 277.7},    // 93f
        new double[] {86.9, 87.7, 89.1, 90.2, 90.7, 92.3, 94, 95.4, 98.1, 100.8, 103.4, 107.2, 110.7, 114.9, 119.4, 128, 136.6, 150.8, 167.6, 217.4, 345.5},    // 94f
        new double[] {87.6, 88.6, 90, 91.3, 91.8, 93.6, 95.3, 97, 100, 102.9, 105.7, 110, 113.8, 118.4, 123.4, 134, 144.4, 161.1, 182.2, 246, 435.4},    // 95f
        new double[] {88.4, 89.5, 90.9, 92.3, 93, 95, 96.6, 98.6, 102, 105.1, 108.1, 112.9, 117.1, 122.1, 127.5, 140.5, 153.1, 172.7, 199, 279.9, 552.9},    // 96f
        new double[] {89.1, 90.4, 91.8, 93.3, 94.1, 96.3, 98, 100.3, 104, 107.3, 110.6, 116, 120.6, 125.9, 131.9, 147.7, 162.8, 185.6, 218.5, 319.6, 704.8},    // 97f
        new double[] {89.8, 91.2, 92.7, 94.4, 95.3, 97.8, 99.5, 102.1, 106.1, 109.6, 113.2, 119.2, 124.3, 130, 136.6, 155.6, 173.7, 200, 241, 365.8, 898.6},    // 98f
        new double[] {90.6, 92.1, 93.7, 95.5, 96.5, 99.2, 101, 103.9, 108.3, 112, 115.9, 122.6, 128.2, 134.3, 141.6, 164.3, 185.9, 216, 266.9, 419.2, 0},    // 99f
        new double[] {91.3, 93, 94.6, 96.6, 97.8, 100.7, 102.6, 105.9, 110.6, 114.6, 118.8, 126.2, 132.3, 138.9, 146.8, 173.8, 199.3, 233.8, 296.5, 480.6, 0},    // 100f
        new double[] {92.1, 93.9, 95.5, 97.6, 99.1, 102.2, 104.2, 108, 113, 117.2, 121.7, 130, 136.6, 143.7, 152.4, 184.2, 214.2, 253.3, 330.5, 550.7, 0},    // 101f
        new double[] {92.8, 94.8, 96.4, 98.8, 100.4, 103.7, 105.9, 110.2, 115.5, 119.9, 124.8, 134, 141.2, 148.8, 158.3, 195.6, 230.7, 274.8, 369.1, 630.3, 0},    // 102f
        new double[] {93.5, 95.6, 97.4, 99.9, 101.8, 105.3, 107.7, 112.6, 118, 122.7, 128.1, 138.1, 146.1, 154.2, 164.6, 208, 248.8, 298.4, 413.1, 720.2, 0},    // 103f
        new double[] {94.3, 96.5, 98.3, 101, 103.2, 107, 109.5, 115, 120.7, 125.7, 131.4, 142.5, 151.2, 159.9, 171.3, 221.5, 268.6, 324.1, 462.9, 821.3, 0},    // 104f
        new double[] {95, 97.4, 99.3, 102.2, 104.6, 108.6, 111.4, 117.6, 123.5, 128.7, 134.9, 147.1, 156.6, 165.9, 178.4, 236.1, 290.2, 352.2, 519.2, 934.4, 0},    // 105f
        new double[] {95.8, 98.3, 100.3, 103.4, 106, 110.3, 113.4, 120.3, 126.4, 131.9, 138.6, 152, 162.3, 172.3, 186, 251.9, 313.8, 382.6, 582.6, 0, 0},    // 106f
        new double[] {96.5, 99.2, 101.3, 104.6, 107.5, 112.1, 115.6, 123.1, 129.4, 135.2, 142.4, 157.1, 168.4, 179.1, 194, 269.1, 339.4, 415.5, 653.8, 0, 0},    // 107f
        new double[] {97.3, 100.1, 102.3, 105.8, 109, 113.9, 117.8, 126.1, 132.5, 138.7, 146.4, 162.5, 174.8, 186.3, 202.6, 287.5, 367.2, 451.1, 733.6, 0, 0},    // 108f
        new double[] {98.1, 101.1, 103.3, 107, 110.6, 115.8, 120.1, 129.3, 135.7, 142.3, 150.6, 168.2, 181.6, 193.9, 211.7, 307.5, 397.1, 489.4, 822.7, 0, 0},    // 109f
        new double[] {98.8, 102, 104.3, 108.3, 112.2, 117.7, 122.5, 132.6, 139.1, 146, 155, 174.3, 188.8, 202, 221.4, 328.9, 429.4, 530.5, 922, 0, 0},    // 110f
        new double[] {99.6, 102.9, 105.4, 109.6, 113.8, 119.7, 125, 136.1, 142.6, 149.9, 159.5, 180.6, 196.4, 210.5, 231.8, 351.8, 464.1, 574.5, 0, 0, 0},    // 111f
        new double[] {100.4, 103.8, 106.5, 110.9, 115.4, 121.7, 127.7, 139.7, 146.3, 154, 164.3, 187.3, 204.4, 219.6, 242.8, 376.4, 501.3, 621.6, 0, 0, 0},    // 112f
        new double[] {101.2, 104.8, 107.6, 112.2, 117.1, 123.8, 130.5, 143.6, 150.1, 158.2, 169.3, 194.3, 213, 229.3, 254.6, 402.7, 541.1, 671.8, 0, 0, 0},    // 113f
        new double[] {102, 105.7, 108.7, 113.6, 118.8, 125.9, 133.4, 147.6, 154, 162.6, 174.5, 201.8, 222.1, 239.5, 267.1, 430.8, 583.6, 725.2, 0, 0, 0},    // 114f
        new double[] {102.8, 106.7, 109.9, 115, 120.6, 128.1, 136.5, 151.7, 158.2, 167.2, 179.9, 209.7, 231.7, 250.4, 280.6, 460.8, 628.8, 782, 0, 0, 0},    // 115f
        new double[] {103.6, 107.6, 111, 116.4, 122.4, 130.4, 139.7, 156.1, 162.5, 172, 185.6, 218, 241.9, 262, 294.9, 492.6, 676.9, 842.2, 0, 0, 0},    // 116f
        new double[] {104.4, 108.6, 112.2, 117.9, 124.2, 132.7, 143, 160.7, 166.9, 176.9, 191.6, 226.7, 252.8, 274.4, 310.3, 526.5, 727.9, 905.8, 0, 0, 0},    // 117f
        new double[] {105.3, 109.6, 113.5, 119.4, 126, 135.1, 146.6, 165.5, 171.6, 182.1, 197.8, 236, 264.3, 287.5, 326.7, 562.4, 781.9, 973.1, 0, 0, 0},    // 118f
        new double[] {106.1, 110.6, 114.7, 120.9, 127.9, 137.6, 150.3, 170.5, 176.4, 187.6, 204.3, 245.9, 276.6, 301.5, 344.3, 600.5, 839.1, 0, 0, 0, 0},    // 119f
        new double[] {106.9, 111.6, 116, 122.4, 129.9, 140.1, 154.2, 175.7, 181.5, 193.2, 211.1, 256.3, 289.7, 316.4, 363.1, 640.8, 899.4, 0, 0, 0, 0},    // 120f
        new double[] {107.8, 112.6, 117.3, 124, 131.9, 142.8, 158.2, 181.1, 186.8, 199.1, 218.3, 267.3, 303.6, 332.2, 383.2, 683.3, 962.9, 0, 0, 0, 0},    // 121f
        new double[] {108.6, 113.6, 118.7, 125.6, 133.9, 145.5, 162.5, 186.7, 192.2, 205.3, 225.8, 278.9, 318.4, 349.2, 404.9, 728.3, 0, 0, 0, 0, 0},    // 122f
        new double[] {109.5, 114.6, 120, 127.3, 136, 148.3, 167, 192.6, 198, 211.7, 233.7, 291.3, 334.1, 367.2, 428.1, 775.6, 0, 0, 0, 0, 0},    // 123f
        new double[] {110.4, 115.6, 121.4, 129, 138.1, 151.2, 171.6, 198.7, 203.9, 218.4, 241.9, 304.3, 350.9, 386.5, 453, 825.5, 0, 0, 0, 0, 0},    // 124f
        new double[] {111.3, 116.6, 122.9, 130.7, 140.3, 154.2, 176.5, 205.1, 210.2, 225.4, 250.5, 318.2, 368.8, 407.1, 479.8, 877.9, 0, 0, 0, 0, 0},    // 125f
        new double[] {112.1, 117.7, 124.4, 132.5, 142.6, 157.3, 181.6, 211.7, 216.7, 232.7, 259.6, 332.9, 387.9, 429.1, 508.5, 932.9, 0, 0, 0, 0, 0},    // 126f
        new double[] {113, 118.8, 125.9, 134.3, 144.9, 160.5, 187, 218.6, 223.4, 240.4, 269.1, 348.5, 408.3, 452.7, 539.5, 990.7, 0, 0, 0, 0, 0},    // 127f
        new double[] {113.9, 119.8, 127.5, 136.1, 147.3, 163.9, 192.6, 225.7, 230.5, 248.4, 279.1, 365.1, 430.1, 477.8, 572.8, 0, 0, 0, 0, 0, 0},    // 128f
        new double[] {114.8, 120.9, 129.1, 138, 149.8, 167.3, 198.5, 233.1, 237.9, 256.7, 289.5, 382.7, 453.3, 504.8, 608.7, 0, 0, 0, 0, 0, 0},    // 129f
        new double[] {115.8, 122, 130.7, 139.9, 152.3, 170.9, 204.6, 240.8, 245.6, 265.4, 300.5, 401.4, 478.2, 533.7, 647.4, 0, 0, 0, 0, 0, 0},    // 130f
        new double[] {116.7, 123.1, 132.4, 141.9, 155, 174.6, 211, 248.7, 253.7, 274.6, 312.1, 421.3, 504.8, 564.6, 689.1, 0, 0, 0, 0, 0, 0},    // 131f
        new double[] {117.6, 124.2, 134.1, 143.9, 157.7, 178.4, 217.7, 257, 262.1, 284.1, 324.2, 442.5, 533.3, 597.8, 734.1, 0, 0, 0, 0, 0, 0},    // 132f
        new double[] {118.5, 125.3, 135.9, 146, 160.5, 182.4, 224.6, 265.5, 270.9, 294.1, 337, 465, 563.8, 633.4, 782.7, 0, 0, 0, 0, 0, 0},    // 133f
        new double[] {119.5, 126.5, 137.7, 148.1, 163.5, 186.6, 231.9, 274.3, 280.1, 304.6, 350.4, 488.9, 596.4, 671.7, 835.2, 0, 0, 0, 0, 0, 0},    // 134f
        new double[] {120.4, 127.6, 139.6, 150.3, 166.6, 190.9, 239.5, 283.4, 289.7, 315.5, 364.5, 514.4, 631.5, 712.7, 892, 0, 0, 0, 0, 0, 0},    // 135f
        new double[] {121.4, 128.8, 141.5, 152.5, 169.8, 195.4, 247.4, 292.8, 299.7, 327, 379.3, 541.6, 669, 756.9, 953.4, 0, 0, 0, 0, 0, 0},    // 136f
        new double[] {122.4, 129.9, 143.5, 154.7, 173.1, 200, 255.6, 302.5, 310.2, 339, 394.9, 570.6, 709.4, 804.3, 0, 0, 0, 0, 0, 0, 0},    // 137f
        new double[] {123.3, 131.1, 145.5, 157, 176.6, 204.9, 264.2, 312.6, 321.2, 351.6, 411.3, 601.6, 752.7, 855.4, 0, 0, 0, 0, 0, 0, 0},    // 138f
        new double[] {124.3, 132.3, 147.5, 159.4, 180.2, 210, 273.1, 323, 332.8, 364.7, 428.6, 634.6, 799.2, 910.4, 0, 0, 0, 0, 0, 0, 0},    // 139f
        new double[] {125.3, 133.5, 149.7, 161.8, 184.1, 215.3, 282.4, 333.7, 344.8, 378.6, 446.8, 669.8, 849.2, 969.6, 0, 0, 0, 0, 0, 0, 0},    // 140f
 
        new double[] {126.3, 134.7, 151.8, 164.3, 188.1, 220.9, 292, 344.7, 357.4, 393, 466, 707.5, 903, 0, 0, 0, 0, 0, 0, 0, 0},    // 141f
        new double[] {127.3, 135.9, 154.1, 166.8, 192.3, 226.6, 302.1, 356.1, 370.6, 408.2, 486.2, 747.7, 960.9, 0, 0, 0, 0, 0, 0, 0, 0},    // 142f
        new double[] {128.3, 137.2, 156.3, 169.4, 196.7, 232.7, 312.5, 367.8, 384.5, 424.2, 507.6, 790.7, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 143f
        new double[] {129.3, 138.4, 158.7, 172, 201.3, 239.1, 323.4, 379.9, 399, 440.9, 530, 836.7, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 144f
        new double[] {130.3, 139.7, 161.1, 174.7, 206.2, 245.7, 334.6, 392.3, 414.2, 458.5, 553.8, 886, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 145f
        new double[] {131.4, 141, 163.5, 177.4, 211.4, 252.8, 346.3, 405.1, 430.1, 476.9, 578.8, 938.7, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 146f
        new double[] {132.4, 142.3, 166.1, 180.2, 216.8, 260.1, 358.5, 418.2, 446.9, 496.3, 605.3, 995.2, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 147f
        new double[] {133.4, 143.6, 168.6, 183.1, 222.6, 267.9, 371.1, 431.8, 464.4, 516.6, 633.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 148f
        new double[] {134.4, 144.9, 171.3, 186, 228.6, 276, 384.2, 445.7, 482.8, 538, 662.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 149f
        new double[] {135.5, 146.3, 174, 189, 235, 284.7, 397.7, 460, 502.1, 560.5, 693.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 150f
        new double[] {136.6, 147.6, 176.8, 192.1, 241.8, 293.8, 411.8, 474.7, 522.4, 584.1, 726.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 151f
        new double[] {137.6, 149, 179.6, 195.2, 248.9, 303.4, 426.3, 489.8, 543.7, 609, 761.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 152f
        new double[] {138.7, 150.4, 182.5, 198.4, 256.4, 313.7, 441.4, 505.3, 566.1, 635.1, 798.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 153f
        new double[] {139.8, 151.8, 185.5, 201.6, 264.4, 324.5, 457, 521.2, 589.6, 662.6, 837.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 154f
        new double[] {140.9, 153.2, 188.6, 205, 272.8, 336.1, 473.1, 537.5, 614.3, 691.6, 878.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 155f
        new double[] {142, 154.6, 191.7, 208.4, 281.7, 348.4, 489.8, 554.2, 640.3, 722.1, 922.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 156f
        new double[] {143, 156.1, 194.9, 211.8, 291.1, 361.5, 507.1, 571.4, 667.7, 754.3, 969.1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 157f
        new double[] {144.2, 157.5, 198.2, 215.3, 301, 375.6, 524.9, 589, 696.4, 788.1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 158f
        new double[] {145.2, 159, 201.5, 218.9, 311.5, 390.7, 543.4, 607, 726.7, 823.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 159f
        new double[] {146.4, 160.5, 204.9, 222.6, 322.6, 406.9, 562.4, 625.5, 758.5, 861.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 160f
        new double[] {147.5, 162, 208.5, 226.4, 334.3, 424.4, 582.1, 644.4, 792.1, 901.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 161f
        new double[] {148.6, 163.5, 212, 230.2, 346.7, 443.4, 602.4, 663.7, 827.4, 943, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 162f
        new double[] {149.7, 165, 215.7, 234.1, 359.8, 463.9, 623.4, 683.6, 864.6, 987.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 163f
        new double[] {150.8, 166.6, 219.4, 238, 373.6, 486.2, 645.1, 703.9, 903.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 164f
        new double[] {151.8, 168.2, 222.6, 242.1, 378, 494.1, 667.4, 732.3, 945.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 165f
        new double[] {152.9, 169.7, 225.8, 246.2, 382.4, 502.2, 690.4, 762.1, 988.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 166f
        new double[] {153.9, 171.3, 229.1, 250.4, 386.9, 510.5, 714.2, 793.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 167f
        new double[] {155, 173, 232.4, 254.7, 391.5, 519.1, 738.7, 826.1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 168f
        new double[] {156, 174.6, 235.7, 259.1, 396.2, 527.9, 763.9, 860.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 169f
        new double[] {157.1, 176.3, 239.1, 263.5, 401, 537, 789.9, 896.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 170f
        new double[] {158.1, 177.9, 242.6, 268.1, 405.9, 546.3, 816.7, 934.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 171f
        new double[] {159.2, 179.6, 246.1, 272.7, 410.9, 555.9, 844.2, 974.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 172f
        new double[] {160.3, 181.3, 249.6, 277.4, 415.9, 565.8, 872.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 173f
        new double[] {161.3, 183, 253.2, 282.2, 421.1, 576, 901.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 174f
        new double[] {162.4, 184.8, 256.8, 287, 426.4, 586.5, 931.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 175f
        new double[] {163.4, 186.5, 260.5, 292, 431.8, 597.3, 962.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 176f
        new double[] {164.5, 188.3, 264.3, 297.1, 437.4, 608.4, 994.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 177f
        new double[] {165.6, 190.1, 268.1, 302.2, 443, 619.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 178f
        new double[] {166.6, 191.9, 271.9, 307.4, 448.8, 631.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 179f
        new double[] {167.7, 193.7, 275.8, 312.8, 454.6, 643.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 180f
        new double[] {168.7, 195.6, 279.7, 318.2, 460.6, 656.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 181f
        new double[] {169.8, 197.5, 283.7, 323.7, 466.7, 669.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 182f
        new double[] {170.9, 199.4, 287.8, 329.3, 473, 682.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 183f
        new double[] {171.9, 201.3, 291.8, 335, 479.4, 696.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 184f
        new double[] {173, 203.2, 296, 340.8, 485.9, 710.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 185f
        new double[] {174.1, 205.1, 300.2, 346.7, 492.5, 724.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 186f
        new double[] {175.1, 207.1, 304.4, 352.7, 499.3, 739.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 187f
        new double[] {176.2, 209.1, 308.7, 358.8, 506.3, 755.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 188f
        new double[] {177.2, 211.1, 313, 365, 513.3, 771.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 189f
        new double[] {178.3, 213.1, 317.4, 371.3, 520.6, 787.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 190f
 
        new double[] {179.4, 215.1, 321.8, 377.7, 528, 804.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 191f
        new double[] {180.4, 217.2, 326.3, 384.3, 535.5, 822.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 192f
        new double[] {181.5, 219.3, 330.8, 390.9, 543.2, 840.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 193f
        new double[] {182.5, 221.4, 335.4, 397.6, 551.1, 858.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 194f
        new double[] {183.6, 223.5, 340, 404.4, 559.1, 878.1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 195f
        new double[] {184.6, 225.6, 344.6, 411.4, 567.3, 897.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 196f
        new double[] {185.7, 227.8, 349.3, 418.4, 575.7, 918.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 197f
        new double[] {186.7, 230, 354.1, 425.6, 584.3, 939.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 198f
        new double[] {187.8, 232.2, 358.9, 432.8, 593, 961.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 199f
        new double[] {188.8, 234.4, 363.8, 440.2, 602, 983.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 200f
        new double[] {189.9, 236.7, 368.7, 447.7, 611.1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 201f
        new double[] {190.9, 238.9, 373.6, 455.3, 620.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 202f
        new double[] {191.9, 241.2, 378.6, 463, 630, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 203f
        new double[] {193, 243.5, 383.6, 470.9, 639.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 204f
        new double[] {194, 245.9, 388.7, 478.8, 649.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 205f
        new double[] {195, 248.2, 393.8, 486.9, 659.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 206f
        new double[] {196.1, 250.6, 399, 495.1, 670.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 207f
        new double[] {197.1, 253, 404.2, 503.4, 680.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 208f
        new double[] {198.1, 255.4, 409.5, 511.8, 691.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 209f
        new double[] {199.1, 257.8, 414.8, 520.4, 702.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 210f
        new double[] {200.2, 260.3, 420.2, 529, 714.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 211f
        new double[] {201.2, 262.8, 425.6, 537.8, 725.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 212f
        new double[] {202.2, 265.3, 431, 546.8, 737.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 213f
        new double[] {203.2, 267.8, 436.5, 555.8, 749.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 214f
        new double[] {204.2, 270.4, 442, 565, 762.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 215f
        new double[] {205.2, 273, 447.6, 574.3, 775, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 216f
        new double[] {206.2, 275.6, 453.2, 583.7, 787.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 217f
        new double[] {207.2, 278.2, 458.9, 593.3, 801.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 218f
        new double[] {208.2, 280.8, 464.6, 603, 814.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 219f
        new double[] {209.2, 283.5, 470.4, 612.8, 828.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 220f
        new double[] {209.9, 286.2, 476.2, 622.8, 842.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 221f
        new double[] {211.2, 288.9, 482, 632.9, 857.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 222f
        new double[] {212.2, 291.6, 487.9, 643.1, 872.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 223f
        new double[] {213.2, 294.4, 493.8, 653.5, 887.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 224f
        new double[] {214.2, 297.2, 499.8, 664, 902.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 225f
        new double[] {215.1, 300, 505.8, 674.6, 918.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 226f
        new double[] {216.1, 302.8, 511.8, 685.4, 935, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 227f
        new double[] {217.1, 305.7, 517.9, 696.3, 951.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 228f
        new double[] {218, 308.6, 524.1, 707.4, 968.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 229f
        new double[] {219, 311.5, 530.2, 718.6, 986, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 230f
        new double[] {220, 314.4, 536.5, 729.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 231f
        new double[] {220.9, 317.4, 542.7, 741.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 232f
        new double[] {221.9, 320.3, 549, 753, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 233f
        new double[] {222.8, 323.4, 555.4, 764.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 234f
        new double[] {223.8, 326.4, 561.7, 776.7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 235f
        new double[] {224.7, 329.4, 568.1, 788.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 236f
        new double[] {225.6, 332.5, 574.6, 801, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 237f
        new double[] {226.6, 335.6, 581.1, 813.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 238f
        new double[] {227.5, 338.8, 587.6, 825.9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 239f
        new double[] {228.4, 341.9, 594.2, 838.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 240f
        new double[] {229.3, 345.1, 600.8, 851.4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 241f
        new double[] {230.2, 348.3, 607.5, 864.3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 242f
        new double[] {231.2, 351.5, 614.2, 877.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 243f
        new double[] {232.1, 354.8, 620.9, 890.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 244f
        new double[] {233, 358.1, 627.7, 904.2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 245f
        new double[] {233.9, 361.4, 634.5, 917.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 246f
        new double[] {234.8, 364.8, 641.3, 931.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 247f
        new double[] {235.6, 368.1, 648.2, 945.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 248f
        new double[] {236.5, 371.5, 655.1, 959.6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},    // 249f
        new double[] {237.4, 374.9, 662.1, 973.8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}    // 250f
 
        };


        internal bool CalculateHeatIndex(Double Temp, Double Humidity, out double HeatIndex)
        {
            int TempIndex = Convert.ToInt32(Math.Round(Temp)) + 57;
            int HumidityIndex = ((int) Humidity / 5);
            if ((TempIndex < 0) || (TempIndex > HeatIndexTable.Length))
            {
                HeatIndex = 0;
                return (false);
            }
            HeatIndex = HeatIndexTable[TempIndex][HumidityIndex];
            return (true);
        }

        internal Byte[] CalculateCRC( Byte[] Data)
         {
            Int32 CRCValue = 0;
            Int32 Temp, Temp1, Temp2;
            
            foreach (Byte C in Data)
            {
                Temp = CRCValue/(256) ^ C;
                Temp1 = crc_table[Temp];
                Temp2 = CRCValue - ((CRCValue / 256) * 256);
                Temp2 = Temp2 * 256;
                CRCValue = Temp2 ^ Temp1;
            }
            Byte[] RValue = new Byte[Data.Length+2];
            for (int i = 0; i < Data.Length;i++)
                RValue[i] = Data[i];
            RValue[Data.Length + 0] = (Byte)(CRCValue / 256);
            RValue[Data.Length + 1] = (Byte)(CRCValue - ((CRCValue / 256) * 256));
            return (RValue);

         }

        public bool ProcessFlags(ref VantagePro.VantageProDevices _VantageProDevices, Byte[] Value, string Which, bool SendFlag)
         {
            double D;
            string Raw, Val;

//            char[] Value=System.Text.Encoding.UTF32.GetString(ByteValue).ToCharArray();
            
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            if (_VantageProDevices.Devices.DeviceIdentifier.Substring(0, 3).ToUpper() != Which.ToUpper())
                return(false);

                _VantageProDevices.PreviousValue = _VantageProDevices.CurrentValue;

                if (!ProcessFieldValue(Value, ref _VantageProDevices, out Raw, out Val, out D))
                    return (false);

             
                if (_VantageProDevices.Devices.DeviceIdentifier.Length>=10)
                {
                    switch(_VantageProDevices.Devices.DeviceIdentifier.Substring(9, 1).ToUpper())
                    {

                        case "W":
                            string[] arr = new string[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
                            int wd = (int)(((double)D / 22.5) + .5);
                            Val = arr[wd % 16];
                            break;

                        case "T":
                            if (D == 65535)
                            {
                                Val = "";
                            }
                            else
                            {
                                byte[] array = BitConverter.GetBytes((Int32)D);
                                BitArray bits = new BitArray(BitConverter.GetBytes((Int32)D));
                                int month = 0;
                                for (int i = 12; i <= 15; i++)
                                {
                                    if (bits[i])
                                        month += Convert.ToInt16(Math.Pow(2, i - 12));
                                }
                                int day = 0;
                                for (int i = 7; i <= 11; i++)
                                {
                                    if (bits[i])
                                        day += Convert.ToInt16(Math.Pow(2, i - 7));
                                }

                                int year = 2000;
                                for (int i = 0; i <= 6; i++)
                                {
                                    if (bits[i])
                                        year += Convert.ToInt16(Math.Pow(2, i));
                                }
                                Val = year.ToString() + "-" + month.ToString("00") + "-" + day.ToString("00");
                                Raw = year.ToString() + month.ToString("00") + day.ToString("00");
                            }
                            break;

                        case "V":
                        try
                        {
                            D = D / 10;
                            if (D <= 0)
                                D = 0;
                            Val = D.ToString();
                            if (_VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes.Count >= 12)
                            {
                                int UVQ = (int)(D + (double).51);
                                if (UVQ >= 11)
                                    Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[11];
                                else
                                    Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[UVQ];
                            }
                        }
                        catch
                        {
                            Val = "";
                        }
                        break;

                        case "B": 
                            Val = D.ToString();

                            if (_VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes.Count>=5)
                            {
                                switch ((int)D)
                                {
                                    case -60:
                                    case 196:
                                        Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[0];
                                        break;
                                    case -20:
                                    case 236:
                                        Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[1];
                                        break;
                                    case 0:
                                        Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[2];
                                        break;
                                    case 20:
                                        Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[3];
                                        break;
                                    case 60:
                                        Val = _VantageProDevices.Devices.StoredDeviceData.Local_StatesFlagAttributes[4];
                                        break;
                                    default:
                                        break;
                                }
                            }
                        break;

                        case "A":
                            if (_VantageProDevices.Devices.DeviceIdentifier.Length >= 11)
                            {
                                if (_VantageProDevices.Devices.DeviceIdentifier.Substring(10, 1).ToUpper()=="D")
                                {
                                    byte[] array = BitConverter.GetBytes((Int32)D);
                                    BitArray bits = new BitArray(BitConverter.GetBytes((Int32)D));
                                    int month = 0;
                                    for (int i = 5; i <= 8; i++)
                                    {
                                        if (bits[i])
                                            month += Convert.ToInt16(Math.Pow(2, i - 5));
                                    }
                                    int day = 0;
                                    for (int i = 0; i <= 4; i++)
                                    {
                                        if (bits[i])
                                            day += Convert.ToInt16(Math.Pow(2, i));
                                    }

                                    int year = 2000;
                                    for (int i = 9; i <= 15; i++)
                                    {
                                        if (bits[i])
                                            year += Convert.ToInt16(Math.Pow(2, i-9));
                                    }
                                    Val = year.ToString() + "-" + month.ToString("00") + "-" + day.ToString("00");

                                }

                                if (_VantageProDevices.Devices.DeviceIdentifier.Substring(10, 1).ToUpper() == "T")
                                {
                                    int hour = (int)D / (int)100;
                                    int minute = (int)D - (hour * 100);
                                    Val=hour.ToString("D2") + ":" + minute.ToString("D2") + ":00";
   
                                }
                            }
                            
                        break;
                    }
                    
                }
                _VantageProDevices.CurrentValue = Val;
                _VantageProDevices.CurrentRaw = Raw;
                _VantageProDevices.LastChangeTime = _PluginCommonFunctions.CurrentTime;
                if (SendFlag)
                    CreateFlag(_VantageProDevices, Val, Raw);
                _VantageProDevices.HasReceivedValidData = true;
                return (true);
            }

         internal bool ProcessFieldValue(Byte[] ByteValue, ref VantagePro.VantageProDevices _VantageProDevices, out string UNProcessedFieldValue, out string ProcessedFieldValueString, out double ProcessedFieldValuedouble)
        {
            int Vs, Dv;
            uint RawUnsignedValue;

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            char[] Value = _PCF.ConvertByteArrayToCharArray(ByteValue);

            bool B = _PCF.ConvertCharsToInt(Value,
                _VantageProDevices.Devices.DeviceIdentifier.Substring(3, 3),
                _VantageProDevices.Devices.DeviceIdentifier.Substring(6, 2),
                out RawUnsignedValue, false);
            if (!B)
            {
                UNProcessedFieldValue = "";
                ProcessedFieldValueString = "";
                ProcessedFieldValuedouble = 0;
                return (false);
            }
            if (_VantageProDevices.Devices.DeviceIdentifier.Substring(8, 1).ToUpper() == "S")
            {
                Vs = (int)RawUnsignedValue;
                ProcessedFieldValuedouble = Vs;
            }
            else
                ProcessedFieldValuedouble = RawUnsignedValue;
            UNProcessedFieldValue = ProcessedFieldValuedouble.ToString();
            ProcessedFieldValueString = UNProcessedFieldValue;
            if (_VantageProDevices.Devices.DeviceIdentifier.Length >= 10)
            {

                if (_VantageProDevices.Devices.DeviceIdentifier.Substring(9, 1) == "/")
                {
                    if (_VantageProDevices.Devices.DeviceIdentifier.Length >= 11)
                    {
                        Dv = _PCF.ConvertToInt32(_VantageProDevices.Devices.DeviceIdentifier.Substring(10));
                        if (Dv != 0)
                            ProcessedFieldValuedouble = ProcessedFieldValuedouble / Dv;
                        ProcessedFieldValueString = ProcessedFieldValuedouble.ToString();
                    }
                }

            }
            return (true);
        }


        internal void CreateFlagStatus(InterfaceStruct Interface, string StatusMessage, string FlagValue, string FlagRaw, string UOM)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            _PCF.AddFlagForTransferToServer(
                Interface.InterfaceName,
                StatusMessage,
                FlagValue,
                FlagRaw,
                Interface.RoomUniqueID,
                Interface.InterfaceUniqueID,
                FlagChangeCodes.OwnerOnly,
                FlagActionCodes.addorupdate,
                UOM);
        }

        internal void CreateFlag(VantagePro.VantageProDevices _VantageProDevices, string FlagValue, string RawValue)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            Tuple<string, string, string, Tuple<int, string>[]> SU;

            if (_VantageProDevices.CurrentValue != _VantageProDevices.PreviousValue || !_VantageProDevices.HasReceivedValidData)
            {
                _PluginCommonFunctions.UOM.TryGetValue(_PCF.ConvertToInt32(_VantageProDevices.Devices.UOMCode), out SU);

                if (string.IsNullOrEmpty(FlagValue))
                {
                    _PCF.AddFlagForTransferToServer(
                    _VantageProDevices.Room,
                    _VantageProDevices.Devices.DeviceName,
                    FlagValue,
                    RawValue,
                    _VantageProDevices.Devices.RoomUniqueID,
                    _VantageProDevices.Devices.DeviceUniqueID,
                    FlagChangeCodes.OwnerOnly,
                    FlagActionCodes.delete,
                    SU.Item2);
                }
                else
                {
                    _PCF.AddFlagForTransferToServer(
                    _VantageProDevices.Room,
                    _VantageProDevices.Devices.DeviceName,
                    FlagValue,
                    RawValue,
                    _VantageProDevices.Devices.RoomUniqueID,
                    _VantageProDevices.Devices.DeviceUniqueID,
                    FlagChangeCodes.OwnerOnly,
                    FlagActionCodes.addorupdate,
                    SU.Item2);
                }
            }
        }
    
        internal bool CalculateDewPoint(Double Temp, Double Humidity, out double Dewpoint)
        {
            double TempCelcius = 5.0 / 9.0 * (Temp - 32.0);
            double VapourPressureValue = Humidity * 0.01 * 6.112 * Math.Exp((17.62 * TempCelcius) / (TempCelcius + 243.12));
            double Numerator = 243.12 * Math.Log(VapourPressureValue) - 440.1;
            double Denominator = 19.43 - (Math.Log(VapourPressureValue));
            double DewPointCelcius = Numerator / Denominator;
            Dewpoint = (9.0 / 5.0) * DewPointCelcius + 32.0;
            return (true);
        }
        
        
        internal bool  CalculateWindChill(Double Temp, Double Wind,  out double WindChill)
        {
            try
            {
                WindChill = (Wind <=3 ) ? Temp : (Temp > 50) ? Temp : (35.74 + (0.6215 * Temp) - 35.75 * (Math.Pow(Wind, 0.16)) + (0.4275 * Temp) * (Math.Pow(Wind, 0.16)));
                 return (true);
            }
            catch
            {
                WindChill = 0;
                return (false);
            }
        }

        //internal void ArchiveTimerRoutine(OutgoingDataStruct ODS)
        //{
        //    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
        //    string RecordDate, RecordTime;
        //    DateTime EventTime;

        //    RecordDate = "";
        //    RecordTime = "";

        //    if (ODS.LocalIDTag == "ArchiveRecord")  //Archive Loop Time
        //    {
        //        int f;
        //        if (ODS.CommDataControlInfo[2].ActualResponseReceived.Length != 7 || ODS.CommDataControlInfo[3].ActualResponseReceived.Length != 267)
        //        {
        //            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(2000006, "", ODS.CommDataControlInfo[2].ActualResponseReceived.Length.ToString() + "-" + ODS.CommDataControlInfo[3].ActualResponseReceived.Length.ToString());
        //            return;
        //        }

        //        for (int Record = ODS.CommDataControlInfo[2].ActualResponseReceived[3]; Record < 5; Record++)
        //        {
        //            Byte[] ArcRecord = new Byte[52];
        //            Array.Copy(ODS.CommDataControlInfo[3].ActualResponseReceived, Record * 52 + 1, ArcRecord, 0, 52);
        //            for (int i = 0; i < VantagePro._VantageProDevices.Length; i++)
        //            {
        //                try
        //                {
        //                    if (int.TryParse(VantagePro._VantageProDevices[i].Devices.DeviceIdentifier.Substring(3, 5), out f))
        //                    {
        //                        if (ProcessFlags(ref VantagePro._VantageProDevices[i], ArcRecord, "VPA", false))
        //                        {
        //                            VantagePro._VantageProDevices[i].HasReceivedValidData = true;
        //                            if (VantagePro._VantageProDevices[i].Devices.DeviceIdentifier.Substring(VantagePro._VantageProDevices[i].Devices.DeviceIdentifier.Length - 2).ToUpper() == "AD")
        //                                RecordDate = VantagePro._VantageProDevices[i].CurrentValue;
        //                            if (VantagePro._VantageProDevices[i].Devices.DeviceIdentifier.Substring(VantagePro._VantageProDevices[i].Devices.DeviceIdentifier.Length - 2).ToUpper() == "AT")
        //                                RecordTime = VantagePro._VantageProDevices[i].CurrentValue;
        //                        }
        //                        else
        //                            VantagePro._VantageProDevices[i].HasReceivedValidData = false;

        //                    }

        //                }
        //                catch
        //                {

        //                }
        //            }
        //            try
        //            {
        //                if (string.IsNullOrEmpty(RecordDate) || string.IsNullOrEmpty(RecordTime))
        //                    continue;
        //            }
        //            catch (Exception e)
        //            {
        //                _PluginCommonFunctions.GenerateErrorRecordLocalMessage(2000005, RecordDate + " " + RecordTime, e.Message);
        //                continue; //Date Error
        //            }


        //            EventTime = new DateTime(
        //                int.Parse(RecordDate.Substring(0, 4)),
        //                int.Parse(RecordDate.Substring(5, 2)),
        //                int.Parse(RecordDate.Substring(8, 2)),
        //                int.Parse(RecordTime.Substring(0, 2)),
        //                int.Parse(RecordTime.Substring(3, 2)),
        //                int.Parse(RecordTime.Substring(6, 2)));
        //            for (int i = 0; i < VantagePro._VantageProDevices.Length; i++)
        //            {
        //                try
        //                {
        //                    if (VantagePro._VantageProDevices[i].HasReceivedValidData && VantagePro._VantageProDevices[i].Devices.DeviceIdentifier.Substring(0, 3).ToUpper() == "VPA")
        //                    {
        //                        _PCF.NamedSaveLogs("VantageProArchive", VantagePro._VantageProDevices[i].Devices.DeviceName, "", VantagePro._VantageProDevices[i].CurrentValue, VantagePro._VantageProDevices[i].CurrentRaw, VantagePro._VantageProDevices[i].Devices, EventTime);
        //                        VantagePro._VantageProDevices[i].HasReceivedValidData = false;

        //                    }
        //                }
        //                catch
        //                {
        //                }
        //            }
        //            VantagePro.LastDateSummaryArchived = _PCF.SaveLogsDateFormat(EventTime);
        //        }

        //    }

        //    PluginCommunicationStruct PCS2 = new PluginCommunicationStruct();
        //    PCS2.Command = PluginCommandsToPlugins.ProcessCommunicationAtTime;
        //    PCS2.DestinationPlugin = VantagePro.LinkPlugin;
        //    PCS2.PluginReferenceIdentifier = VantagePro.LinkPluginReferenceIdentifier;
        //    PCS2.SecureCommunicationIDCode = VantagePro.LinkPluginSecureCommunicationIDCode;

        //    PCS2.OutgoingDS = new OutgoingDataStruct();
        //    PCS2.OutgoingDS.NumberOfTimesToProcessCommunicationAtTime = 1;
        //    if (ODS.LocalIDTag == "ArchiveRecord")
        //        PCS2.OutgoingDS.SecondsBetweenProcessCommunicationAtTime = _PCF.GetStartupField("ConsoleArchiveInterval", 5) * 60;
        //    else
        //        PCS2.OutgoingDS.SecondsBetweenProcessCommunicationAtTime = 15; //First Time in Plugin

            
        //    PCS2.OutgoingDS.CommDataControlInfo = new CommDataControlInfoStruct[5];

        //    PCS2.OutgoingDS.LocalIDTag = "ArchiveRecord";
        //    PCS2.OutgoingDS.CommDataControlInfo[0].CharactersToSend = new Byte[] { (Byte)'\n' };
        //    PCS2.OutgoingDS.CommDataControlInfo[0].ResponseToWaitFor = new Byte[] { (Byte)'\n', (Byte)'\r' };
        //    PCS2.OutgoingDS.CommDataControlInfo[0].ReceiveDelayMiliseconds = VantagePro.ReceiveDelay;

        //    PCS2.OutgoingDS.CommDataControlInfo[1].CharactersToSend = new Byte[] { (Byte)'D', (Byte)'M', (Byte)'P', (Byte)'A', (Byte)'F', (Byte)'T', (Byte)'\n' };
        //    PCS2.OutgoingDS.CommDataControlInfo[1].ResponseToWaitFor = new Byte[] { (Byte)'\x06' };
        //    PCS2.OutgoingDS.CommDataControlInfo[1].ReceiveDelayMiliseconds = VantagePro.ReceiveDelay;

        //    if (string.IsNullOrEmpty(VantagePro.LastDateSummaryArchived))
        //    {
        //        //June 6, 2003 9:30am
        //        int VantageDateStamp = 6 + (6 * 32) + ((2003 - 2000) * 512);
        //        int VantageTimeStamp = (100 * 9) + 30;

        //        Byte[] M = new Byte[4];
        //        M[1] = (Byte)(VantageDateStamp / 256);
        //        M[0] = (Byte)(VantageDateStamp - (M[1] * 256));
        //        M[3] = (Byte)(VantageTimeStamp / 256);
        //        M[2] = (Byte)(VantageTimeStamp - (M[3] * 256));
        //        PCS2.OutgoingDS.CommDataControlInfo[2].CharactersToSend = CalculateCRC(M);
        //    }
        //    else
        //    {
        //        string S = VantagePro.LastDateSummaryArchived.Substring(8, 2) +
        //            VantagePro.LastDateSummaryArchived.Substring(5, 2) +
        //            VantagePro.LastDateSummaryArchived.Substring(0, 4);

        //        String T = VantagePro.LastDateSummaryArchived.Substring(11, 2) +
        //            VantagePro.LastDateSummaryArchived.Substring(14, 2);

        //        int VantageDateStamp = _PCF.ConvertToInt32(VantagePro.LastDateSummaryArchived.Substring(8, 2)) +
        //            (_PCF.ConvertToInt32(VantagePro.LastDateSummaryArchived.Substring(5, 2)) * 32) +
        //            ((_PCF.ConvertToInt32(VantagePro.LastDateSummaryArchived.Substring(0, 4)) - 2000) * 512);
        //        int VantageTimeStamp = (100 * _PCF.ConvertToInt32(VantagePro.LastDateSummaryArchived.Substring(11, 2))) +
        //            _PCF.ConvertToInt32(VantagePro.LastDateSummaryArchived.Substring(14, 2));

        //        Byte[] M = new Byte[4];
        //        M[1] = (Byte)(VantageDateStamp / 256);
        //        M[0] = (Byte)(VantageDateStamp - (M[1] * 256));
        //        M[3] = (Byte)(VantageTimeStamp / 256);
        //        M[2] = (Byte)(VantageTimeStamp - (M[3] * 256));
        //        PCS2.OutgoingDS.CommDataControlInfo[2].CharactersToSend = CalculateCRC(M);
        //    }
        //    PCS2.OutgoingDS.CommDataControlInfo[2].ReponseSizeToWaitFor = 7;
        //    PCS2.OutgoingDS.CommDataControlInfo[2].ReceiveDelayMiliseconds = VantagePro.ReceiveDelay;
        //    PCS2.OutgoingDS.CommDataControlInfo[3].CharactersToSend = new Byte[] { (Byte)'\x06' };
        //    PCS2.OutgoingDS.CommDataControlInfo[3].ReponseSizeToWaitFor = 267;
        //    PCS2.OutgoingDS.CommDataControlInfo[3].ReceiveDelayMiliseconds = VantagePro.ReceiveDelay;
        //    PCS2.OutgoingDS.CommDataControlInfo[4].CharactersToSend = new Byte[] { (Byte)'\x1B' };
        //    PCS2.OutgoingDS.CommDataControlInfo[4].ReceiveDelayMiliseconds = VantagePro.ReceiveDelay;

        //    PCS2.OutgoingDS.MaxMilisecondsToWaitForIncommingData = 30000;
        //    _PCF.QueuePluginInformationToPlugin(PCS2);
        //}
    }
}

