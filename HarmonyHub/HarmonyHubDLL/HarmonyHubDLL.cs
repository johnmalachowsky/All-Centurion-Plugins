using System;
using System.Net;
using CHMPluginAPI;
using CHMPluginAPICommon;
using System.Net.Sockets;
using System.Diagnostics;
using HarmonyHub;
using System.Threading.Tasks;
using System.Threading;
using HarmonyHub.Config;
using System.Collections.Generic;
using System.Text.RegularExpressions;

//Required Parameters
//  Previous IP Address
//  MiliSeconds Between IP Poll
//  Seconds Between Harmony CHeck


namespace CHMModules
{


    public class HarmonyHubDLL
    {

        #region Standard Functions
        private static _PluginCommonFunctions PluginCommonFunctions;
        internal static bool LinkedCommReady = false;
        internal static bool Connected = false;
        internal static string Hub_IP = "";
        internal enum Status{ Waiting, Initialized, LookingForIP, FoundIP, TryingToLinkToHub, LinkedToHub };
        static internal Status CurrentStatus = Status.Waiting;
        static internal System.Threading.Timer CommandLoopTimer;
        internal static SemaphoreSlim LockingSemaphore;
        internal static Client client;
        static DateTime LastTime = DateTime.MinValue;
        static int SecondsBetweenCheck = 5;
        static int MiliSecondsBetweenIPPoll = 2000;
        static HarmonyConfig config;
        static DeviceStruct HubDevice;


        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Harmony Hub";
            ServerAccessFunctions.PluginSerialNumber = "00001-00017";
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

            //Hub_IP = "172.30.23.25";
            //ConnectToHarmonyHub(Hub_IP, "john@usmalachowskys.com", "columbus");

            //Hub_IP = "172.30.23.26";
            //ConnectToHarmonyHub(Hub_IP, "john@usmalachowskys.com", "columbus");

            CurrentStatus = Status.Initialized;

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            TimerCallback ProcessTimerCallBack = new TimerCallback(ProcessTimer);
            CommandLoopTimer = new System.Threading.Timer(ProcessTimerCallBack, null, Timeout.Infinite, Timeout.Infinite);
            LockingSemaphore = new SemaphoreSlim(1);



        }

        private static void FlagCommingServerEventHandler(ServerEvents WhichEvent)
        {

        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            if(CurrentStatus >=Status.Initialized && (_PluginCommonFunctions.CurrentTime - LastTime).TotalSeconds > SecondsBetweenCheck)
            {
                CommandLoopTimer.Change(0, Timeout.Infinite);
                LastTime = _PluginCommonFunctions.CurrentTime;
            }
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
                            continue;
                        }

                        if (Value.PluginData.Command == PluginCommandsToPlugins.ProcessCommandWords || Value.PluginData.Command == PluginCommandsToPlugins.DirectCommand)
                        {
                            continue;
                        }

                        if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionFailed)
                        {
                            continue;
                        }

                        if (Value.PluginData.Command == PluginCommandsToPlugins.RequestLink)
                        {
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

        private static void ProcessTimer(object Item)
        {
            LockingSemaphore.Wait();

            switch (CurrentStatus)
            {

                case Status.Waiting:
                    break;

                case Status.Initialized:
                    CurrentStatus = Status.LookingForIP;
                    if (FindHarmonyHub())
                        CurrentStatus = Status.FoundIP;
                    else
                        CurrentStatus = Status.Initialized;
                    break;

                case Status.FoundIP:
                    ConnectToHarmonyHub(Hub_IP, "john@usmalachowskys.com", "columbus");
                    CurrentStatus = Status.LinkedToHub;
                    break;
            }



            LockingSemaphore.Release();
        }

        private static bool FindHarmonyHub()
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            TcpClient _client = null;
            NetworkStream _stream = null;
            int start, end;


            string RootIP;
            RootIP=_PCF.GetStartupFieldWithDefault("LastIPAddress", ServerAccessFunctions.GetSingleFlagFromServer("Machine IP Address"));
            int a = RootIP.LastIndexOf('.');
            start = _PCF.ConvertToInt32(RootIP.Substring(a+1));
            end = start;
            RootIP = RootIP.Substring(0, a)+".";
            Hub_IP = "";
            while (true)
            {
                for (int i = start; i <= end; i++)
                {
                    IPAddress ip = IPAddress.Parse(RootIP + i.ToString());
                    _client = new TcpClient();
                    var result = _client.BeginConnect(ip.ToString(), 5222, null, null);

                    bool success = result.AsyncWaitHandle.WaitOne(MiliSecondsBetweenIPPoll);
                    Debug.WriteLine(ip);
 

                    if (!success)
                    {
                        _client.Close();
                        continue;
                    }
                    else
                    {
                        try
                        {
                            _stream = _client.GetStream();

                        }

                        catch
                        {
                            if (_stream != null)
                                _stream.Close();
                            if (_client != null)
                                _client.Close();
                            continue;
                        }

                        if (_stream != null)
                            _stream.Close();
                        if (_client != null)
                            _client.Close();
                        Hub_IP = ip.ToString();
                        if(start != end)
                        {
                            _PCF.AddOrUpdateConfigurationInformation("LastIPAddress", "", Hub_IP,"P");
                        }
                        Debug.WriteLine("Hub Found");
                        return (true);
                    }

                }
                if (start == 0 && end == 255)
                    return (false);
                start = 0;
                end = 255;
            }
        }

        private static void ConnectToHarmonyHub(string hub_IP, string UserName, string Password)
        {
            Task.Run(async () => await MainAsync(hub_IP, UserName, Password)).Wait();
        }

        static async Task MainAsync(string hub_IP, string UserName, string Password)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            using (client = new Client(hub_IP, UserName, Password, true))
            {
                // Setup event handlers
                client.MessageSent += (o, e) => { Debug.WriteLine(e.Message); };
                client.MessageReceived += (o, e) => { Debug.WriteLine(e.Message); };
                client.Error += (o, e) => { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.GetException().Message); Console.ResetColor(); };

                try
                {
                    client.Connect();
                }
                catch(Exception err)
                {
                    return;
                }
                config = await client.GetConfigAsync();

                HubDevice = new DeviceStruct();
                if (!_PluginCommonFunctions.LocalDevicesByName.TryGetValue(client.FriendlyName.ToLower(), out HubDevice))
                {
                    HubDevice = new DeviceStruct();
                    foreach (Tuple<string, string, int> Room in _PluginCommonFunctions.RoomArray)
                    {
                        string R = client.FriendlyName.Trim();
                        string S = R.ToLower();
                        if (S.IndexOf(Room.Item2) == 0)
                        {
                            HubDevice.RoomUniqueID = Room.Item1;
                            HubDevice.DeviceName = R.Substring(Room.Item2.Length).Trim();
                             break;
                        }
                    }
                    HubDevice.DeviceUniqueID = _PCF.CreateDBUniqueID("D");
                    HubDevice.InterfaceUniqueID = _PluginCommonFunctions.LocalInterface.InterfaceUniqueID;
                    HubDevice.DeviceType = "HarmonyHub";
                    HubDevice.DeviceClassID = client.HubProfiles;
                    HubDevice.DeviceIdentifier = client.Identity;
                    HubDevice.NativeDeviceIdentifier = client.ProductID;
                    _PCF.AddNewDevice(HubDevice);
                }

                DeviceStruct DeviceDevice = new DeviceStruct();
                string HubRoom=_PCF.GetRoomFromUniqueID(HubDevice.RoomUniqueID).Trim();

                foreach (var device in config.Device)
                {
                    string Name = HubRoom + " " + device.Label.Trim();
                    if (!_PluginCommonFunctions.LocalDevicesByName.TryGetValue(Name.ToLower(), out DeviceDevice))
                    {
                        DeviceDevice = new DeviceStruct();
                        DeviceDevice.RoomUniqueID = HubDevice.RoomUniqueID;
                        DeviceDevice.DeviceName = device.Label.Trim();
                        DeviceDevice.DeviceUniqueID = _PCF.CreateDBUniqueID("D");
                        DeviceDevice.InterfaceUniqueID = _PluginCommonFunctions.LocalInterface.InterfaceUniqueID;
                        DeviceDevice.DeviceType = device.DeviceTypeDisplayName;
                        DeviceDevice.DeviceClassID = device.Model;
                        DeviceDevice.DeviceIdentifier = device.Id;
                        DeviceDevice.NativeDeviceIdentifier = device.Type;
                        DeviceDevice.XMLConfiguration = "<commands>\r";

                        foreach (HarmonyHub.Config.ControlGroupConfigElement Element in device.ControlGroup)
                        {
                            string Q = "";
                            foreach (HarmonyHub.Config.FunctionConfigElement FE in Element.Function)
                            {
                                string S1 = FE.Action.Replace(':', '=');
                                string S2 = S1.Replace("{", "");
                                string S = S2.Replace("}", "");
                                string[] ll = S.Split(',');
                                Q="<command state="+"\""+FE.Label+"\" ";
                                foreach (string x in ll)
                                {
                                    var regex = new Regex(Regex.Escape("\""));
                                    Q = Q + regex.Replace(x, "", 2) + " ";
                                }
                                DeviceDevice.XMLConfiguration = DeviceDevice.XMLConfiguration + Q + "> </command>\n";
                            }
                        }
                        DeviceDevice.XMLConfiguration = DeviceDevice.XMLConfiguration + "</commands>\r";
                        _PCF.AddNewDevice(DeviceDevice);


                        Debug.WriteLine($"Device: {device.Label} ({device.Manufacturer} {device.Model}) - {device.Id}");
                    }
                }

                //while (input != 'q')
                //{
                //    Console.ResetColor();
                //    Console.Clear();
                //    foreach (var device in config.Device)
                //    {
                //        Console.WriteLine($"Device: {device.Label} ({device.Manufacturer} {device.Model}) - {device.Id}");
                //    }

                //    Console.WriteLine();

                //    switch (input)
                //    {
                //        case 'a':
                //            var activityId = await client.GetCurrentActivityIdAsync();
                //            Console.ForegroundColor = ConsoleColor.Yellow;
                //            Console.Write("Current activity: ");
                //            Console.ResetColor();
                //            Console.WriteLine($"{config.Activity.First(x => x.Id == activityId.ToString()).Label} ({activityId})");
                //            break;
                //        case 'c':
                //            Console.WriteLine("EXECUTE COMMAND");
                //            Console.WriteLine("===============");

                //            Console.ForegroundColor = ConsoleColor.Cyan;
                //            Console.Write("Device name: ");
                //            Console.ResetColor();

                //            var targetDeviceName = Console.ReadLine();

                //            if (!config.Device.Any(x => x.Label == targetDeviceName))
                //            {
                //                Console.ForegroundColor = ConsoleColor.Red;
                //                Console.WriteLine($"Unknown device: {targetDeviceName}");
                //                Console.ResetColor();
                //                break;
                //            }

                //            Console.ForegroundColor = ConsoleColor.Yellow;
                //            Console.Write("Available control groups: ");
                //            Console.ResetColor();
                //            foreach (var controlGroup in config.Device.First(x => x.Label == targetDeviceName).ControlGroup)
                //            {
                //                Console.Write($"{controlGroup.Name}, ");
                //            }
                //            Console.WriteLine();

                //            Console.ForegroundColor = ConsoleColor.Cyan;
                //            Console.Write("Control group: ");
                //            Console.ResetColor();
                //            var targetControlGroup = Console.ReadLine();

                //            if (!config.Device.First(x => x.Label == targetDeviceName).ControlGroup.Any(x => x.Name == targetControlGroup))
                //            {
                //                Console.ForegroundColor = ConsoleColor.Red;
                //                Console.WriteLine($"Unknown control group: {targetControlGroup}");
                //                Console.ResetColor();
                //                break;
                //            }

                //            Console.ForegroundColor = ConsoleColor.Yellow;
                //            Console.Write("Available control functions: ");
                //            Console.ResetColor();
                //            foreach (var controlFunction in config.Device.First(x => x.Label == targetDeviceName).ControlGroup.First(x => x.Name == targetControlGroup).Function)
                //            {
                //                Console.Write($"{controlFunction.Label}, ");
                //            }
                //            Console.WriteLine();

                //            Console.ForegroundColor = ConsoleColor.Cyan;
                //            Console.Write("Control function: ");
                //            Console.ResetColor();
                //            var targetControlFunction = Console.ReadLine();

                //            if (!config.Device.First(x => x.Label == targetDeviceName).ControlGroup.First(x => x.Name == targetControlGroup).Function.Any(x => x.Label == targetControlFunction))
                //            {
                //                Console.ForegroundColor = ConsoleColor.Red;
                //                Console.WriteLine($"Unknown function: {targetControlFunction}");
                //                Console.ResetColor();
                //                break;
                //            }

                //            var commandAction = config.Device.First(x => x.Label == targetDeviceName).
                //                ControlGroup.First(x => x.Name == targetControlGroup).
                //                Function.First(x => x.Label == targetControlFunction).Action;
                //            await client.SendCommandAsync(commandAction);
                //            break;
                //    }

                //    Console.WriteLine();
                //    Console.WriteLine("=======================================================");
                //    Console.WriteLine("Enter choice: get current (a)ctivity, (c)ommand, (q)uit");
                //    input = Console.ReadKey().KeyChar;
                //}
            }
        }
        #endregion

    }
}

