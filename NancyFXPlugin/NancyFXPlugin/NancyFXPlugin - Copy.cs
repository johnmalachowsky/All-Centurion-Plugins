
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
using System.Diagnostics;
using System.Xml.Linq;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.ViewEngines;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Responses;
using System.Reflection;
using Nancy.Session;
using Nancy.ModelBinding;


namespace CHMModules
{


    public class NancyFXPlugin
    {

        private static _PluginCommonFunctions PluginCommonFunctions;
        internal static _PluginDatabaseAccess _PDBA;


        #region WebServer Specific Values
        internal static NancyHost nancyHost;
        #endregion



        public void PluginInitialize(int UniqueID)
        {

            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            ServerAccessFunctions.PluginDescription = "NancyFx HTML Plugin";
            ServerAccessFunctions.PluginSerialNumber = "00001-00002";
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
            ServerAccessFunctions._Command += CommandEvent;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;
            _PDBA = new _PluginDatabaseAccess(Path.GetFileNameWithoutExtension((Assembly.GetExecutingAssembly().GetName().Name)));


        }

        private static void CommandEvent(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        //private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        //{

        //}

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = false;

            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;


        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            string Port, Address;

 
            _PCF.GetStartupField("HTTPPort", out Port);
            _PCF.GetStartupField("HTTPAddress", out Address);

            HTMLRoutines.StartWebServer(Address, Port);
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
            PluginEventArgs Value;
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();

            try
            {
                while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
                {
                    try
                    {
                        if (Value.PluginData.Command == PluginCommandsToPlugins.HTMLProcess)
                        {
                            if(Value.PluginData.HTMLSubCommand==PluginCommandsToPluginsHTMLSubCommands.StartHTMLSession)
                            {
                                HTMLRoutines.HTMLSessionData LocalHTMLSessionData;
                                Guid g = Guid.NewGuid();
                                var SessionIDCode = Convert.ToBase64String(g.ToByteArray());
                                SessionIDCode = SessionIDCode.Replace("=", "");
                                SessionIDCode = SessionIDCode.Replace("+", "");
                                LocalHTMLSessionData = new HTMLRoutines.HTMLSessionData();
                                LocalHTMLSessionData.SessionID = SessionIDCode;
                                LocalHTMLSessionData.NeverLogOut = false;
                                LocalHTMLSessionData.LastPageAccessed = "";
                                LocalHTMLSessionData.PageAfterLogin = "";
                                DateTime SessionTime = _PluginCommonFunctions.CurrentTime;
                                LocalHTMLSessionData.TickAtLastActivity = SessionTime;
                                int HTTPSessionTimeout;
                                _PCF.GetStartupField("HTTPSessionTimeout", out HTTPSessionTimeout, 300);

                                LocalHTMLSessionData.LogoutTimeout = HTTPSessionTimeout;
                                LocalHTMLSessionData.LastUpdateIsAutoRefresh = false;
                                LocalHTMLSessionData.FirstDisplay = false;
                                LocalHTMLSessionData.SecurityLevel = Value.PluginData.PWStruct.PWLevel;
                                LocalHTMLSessionData.Loggedin = true;
                                HTMLRoutines.HTMLSessionTable.TryAdd(SessionIDCode, LocalHTMLSessionData);

                                string Port, Address, HomePage;
                                _PCF.GetStartupField("HTTPPort", out Port);
                                _PCF.GetStartupField("HTTPAddress", out Address);
                                _PCF.GetStartupField("HTMLHomePage", out HomePage);

                                System.Diagnostics.Process.Start(Address+":" + Port+"/"+ HomePage+"?ID="+ SessionIDCode);

                            }

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
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            ServerAccessFunctions.InformationCommingFromServerSlim.Wait();
            try
            {

                while (ServerAccessFunctions.InformationCommingFromServerQueue.TryDequeue(out Value))
                {
                    try
                    {
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
            ServerAccessFunctions.InformationCommingFromServerSlim.Release();

        }

        private static void ShutDownPluginEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            //Shut Down Web Server
            HTMLRoutines.StopWebServer();
        }

        private static void WatchdogProcessEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {

        }

        private static void StartupInfoEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
        }


        #region Plugin Routines 

 

        internal class HTMLRoutines
        {
            static private _PluginDatabaseAccess _PDBA= NancyFXPlugin._PDBA;
            //static private SemaphoreSlim ProcessHTMLSlim;

            static private int MaxHTMLPageSize, HTTPSessionTimeout;
            static private Dictionary<string, string> HTMLObjectTypes;
            static internal string HTTPSessionCookieName;
            static internal string HTMLLoginPage, HTMLLoginImage, HTMLHomePage, HTMLLogout, HTMLRefreshRequest, HTMLStartupInfo, HTMLCommonFiles;
            static private DateTime LastDeadSessionCheck;
            internal class HTMLSessionData
            {
                internal string SessionID;
                internal DateTime TickAtLastActivity;
                internal string LastPageAccessed;
                internal string CurrentPageRequested;
                internal string PageAfterLogin;
                internal bool Loggedin;
                internal bool NeverLogOut;
                internal string SecurityLevel;
                internal string User;
                internal string XMLConfig;
                internal int LogoutTimeout;
                internal bool LastUpdateIsAutoRefresh;
                internal string[] FlagValuesToDisplay;
                internal DynamicHTMLDataStruct[] FlagValuesToDisplayData;
                internal Dictionary<string, Tuple<string, string>> Actions;
                internal string TemplateName;
                internal string PasswordRequired;
                internal bool FirstDisplay;
            };

            internal static ConcurrentDictionary<string, HTMLSessionData> HTMLSessionTable;

            private bool SecurityCheckNoLoginRequired(string level)
            {
                if (string.IsNullOrEmpty(level))
                    return (false);
                if (level.ToLower() == "kiosk")
                    return (true);
                return (false);

            }

            private static string SecurityPasswordCheck(string UserAccount, string Password)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                if(string.IsNullOrEmpty(UserAccount) || string.IsNullOrEmpty(Password))
                {
                    return ("");
                }


                 PasswordStruct PW = new PasswordStruct();
                if(!_PCF.GetPasswordInfo(UserAccount, Password, ref PW))
                {
                    return ("");
                }
                return (PW.PWLevel);
            }

            internal static void StartWebServer(string Address, string Port)//Changable for each type of Webserver
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

                try
                {
                    _PluginCommonFunctions.GenerateLocalMessage(1, "", "");
                    HTMLObjectTypes = new Dictionary<string, string>();
                    HTMLSessionTable = new ConcurrentDictionary<string, HTMLRoutines.HTMLSessionData>();
                    //ProcessHTMLSlim = new SemaphoreSlim(1, 1);

                    string Version;
                    _PDBA.OpenSpecialPluginDB(ServerAccessFunctions.PluginDataDirectory, out Version, "CHMHTMLDatabase", _PluginCommonFunctions.DBPassword);
                    _PDBA.SpecialPluginDBLoadDictionarytable("HTMLTypes", "Extension", "ContentType", HTMLObjectTypes);

                    _PCF.GetStartupField("HTTPSessionTimeout", out HTTPSessionTimeout, 300);
                    HTMLLoginPage = _PCF.GetStartupFieldWithDefault("HTMLLoginPage", "login.html");
                    HTMLHomePage = _PCF.GetStartupFieldWithDefault("HTMLHomePage", "mainpage.html");
                    HTMLLoginImage = _PCF.GetStartupFieldWithDefault("HTMLLoginImage", "loginbackground.jpg");
                    HTMLLogout= _PCF.GetStartupFieldWithDefault("HTMLLogout", "logout");
                    HTMLStartupInfo = _PCF.GetStartupFieldWithDefault("HTMLStartupInfo", "startupinfo.html");
                    HTMLRefreshRequest = _PCF.GetStartupFieldWithDefault("HTMRefreshRequest", "refreshcheck.html");
                    _PCF.GetStartupField("HTMLCommonFiles", out HTMLCommonFiles);
                    int AddOn, MinPg;
                    _PCF.GetStartupField("HTMLPageAddon", out AddOn);
                    _PCF.GetStartupField("HTMLMinPageSize", out MinPg);
                    HTTPSessionCookieName = _PCF.GetStartupFieldWithDefault("HTTPSessionCookieName", "_CHMHOFSession");
                    MaxHTMLPageSize = Math.Max(_PDBA.SpecialPluginDBCalculateLargestField("HTMLPages", "Object") + AddOn, MinPg);
                    _PCF.GetStartupField("HTMLMinPageSize", out MinPg);
                    LastDeadSessionCheck = _PluginCommonFunctions.CurrentTime;


                    //WebHosting Type Dependent Information (Changable for each type of Webserver)
                    HostConfiguration hostConfigs = new HostConfiguration();
                    hostConfigs.UrlReservations.CreateAutomatically = true;
                    nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri(Address+":" + Port), new CustomBootstrapper(), hostConfigs);

                    // Process.Start(Host);
                    nancyHost.Start();
                    _PluginCommonFunctions.GenerateLocalMessage(2, "", "Address-"+Address+" Port-"+Port);
                }
                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);

                }
            }

            internal static void StopWebServer()
            {
                //WebHosting Type Dependent Information (Changable for each type of Webserver)
                nancyHost.Stop();

            }

            internal bool ProcessHTML(ref string SessionIDCode, string URL, string TransType, string OwnerModule, string URLPath, string Host, Tuple<string, string>[] Form, ref Tuple<string, string>[] cookies, Tuple<string, object>[] RequestHeadders, out string ResponseContentType, out Tuple<string, object>[] ResponseHeadders, out string ResponseReDirect, out string JavaUpdate, ref byte[] ResponseBody)
            {
                _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                //ProcessHTMLSlim.Wait();

                try
                {

                    HTMLSessionData LocalHTMLSessionData;
                    DateTime SessionTime = _PluginCommonFunctions.CurrentTime;
                    string FileExtension = Path.GetExtension(URLPath).ToLower();
                    if (string.IsNullOrEmpty(FileExtension))
                        FileExtension = ".html";
                    string FileName = Path.GetFileNameWithoutExtension(URLPath).ToLower();

                    if (SessionIDCode==null)
                        SessionIDCode = "";

                    try
                    {
                        Uri Returneduri = new Uri(URL);
                        if (!string.IsNullOrEmpty(Returneduri.Query))
                        {
                            string[] sid = Returneduri.Query.Split('=');
                            if (sid[0] == "?ID")
                                SessionIDCode = sid[1];
                            if (!HTMLSessionTable.TryGetValue(SessionIDCode, out LocalHTMLSessionData))
                                SessionIDCode = "";
                            else
                            {
                                HTMLSessionTable.TryRemove(SessionIDCode, out LocalHTMLSessionData);

                                Guid g = Guid.NewGuid();
                                SessionIDCode = Convert.ToBase64String(g.ToByteArray());
                                SessionIDCode = SessionIDCode.Replace("=", "");
                                SessionIDCode = SessionIDCode.Replace("+", "");
                                LocalHTMLSessionData.SessionID = SessionIDCode;
                                HTMLSessionTable.TryAdd(SessionIDCode, LocalHTMLSessionData);
                            }


                        }
                    }
                    catch
                    {

                    }


                    if (!HTMLSessionTable.TryGetValue(SessionIDCode, out LocalHTMLSessionData))
                        SessionIDCode = "";
                    else//Check for Timeout and Logout
                    {
                        if (FileName == HTMLLogout)
                        {
                            LocalHTMLSessionData.TickAtLastActivity = DateTime.MinValue;
                            _PluginCommonFunctions.GenerateLocalMessage(5, "", LocalHTMLSessionData.User);
                            HTMLSessionData sid;
                            HTMLSessionTable.TryRemove(SessionIDCode,out sid);
                            SessionIDCode = "";
                        }
                    }
                    TimeSpan LastDead = SessionTime - LastDeadSessionCheck;
                    if (LastDead.TotalSeconds > (HTTPSessionTimeout*2)) //Check for Dead Codes
                    {
                        bool deadflag = true;
                        while (deadflag)
                        {
                            deadflag = false;
                            foreach (KeyValuePair<string, HTMLSessionData> HSD in HTMLSessionTable)
                            {
                                TimeSpan ds = SessionTime - HSD.Value.TickAtLastActivity;
                                if (ds.TotalSeconds> HSD.Value.LogoutTimeout*2)
                                {
                                    HTMLSessionData sid;
                                    HTMLSessionTable.TryRemove(HSD.Key, out sid);
                                    deadflag = true;
                                    break;
                                }
                            }
                        }

                        LastDeadSessionCheck = SessionTime;
                    }




                    if (string.IsNullOrEmpty(SessionIDCode)) //New or timed out
                    {
                        Guid g = Guid.NewGuid();
                        SessionIDCode = Convert.ToBase64String(g.ToByteArray());
                        SessionIDCode = SessionIDCode.Replace("=", "");
                        SessionIDCode = SessionIDCode.Replace("+", "");
                        LocalHTMLSessionData = new HTMLSessionData();
                        LocalHTMLSessionData.SessionID = SessionIDCode;
                        LocalHTMLSessionData.Loggedin = false;
                        LocalHTMLSessionData.NeverLogOut = false;
                        LocalHTMLSessionData.LastPageAccessed = "";
                        LocalHTMLSessionData.PageAfterLogin = "";
                        LocalHTMLSessionData.TickAtLastActivity = SessionTime;
                        LocalHTMLSessionData.LogoutTimeout = HTTPSessionTimeout;
                        LocalHTMLSessionData.LastUpdateIsAutoRefresh = false;
                        LocalHTMLSessionData.FirstDisplay = false;
                        LocalHTMLSessionData.Actions = new Dictionary<string, Tuple<string, string>>();
                        HTMLSessionTable.TryAdd(SessionIDCode, LocalHTMLSessionData);
                        if(FileExtension != ".html" || string.IsNullOrEmpty(FileName))
                        {
                            ResponseContentType = "";
                            ResponseHeadders = null;
                            JavaUpdate = "";
                            //ProcessHTMLSlim.Release();
                            ResponseReDirect = "/" + HTMLLoginPage;
                            LocalHTMLSessionData.LastPageAccessed = ResponseReDirect;
                            return (true);

                        }
                        if(FileName + FileExtension == HTMLRefreshRequest) //Refresh after reboot or restart
                        {
                            string X = URL.Substring(0, URL.Length-(HTMLRefreshRequest.Length + 1));
                            FileName = Path.GetFileNameWithoutExtension(X).ToLower();
                        }

                        if (FileName + FileExtension == HTMLLoginPage.ToLower())
                            LocalHTMLSessionData.LastPageAccessed = FileName + FileExtension;

                        string[] KeyFields = new string[] { "ObjectName", "ObjectType", "OwnerDLL" };
                        string[] KeyValues = new string[] { FileName + FileExtension, FileExtension.Substring(1), OwnerModule };
                        string htmlbody = "";
                        Dictionary<string, string> OtherFields;
                        int f = _PDBA.GetObjectByFieldsIntoString("HTMLPages", KeyFields, KeyValues, "object", ref htmlbody, out OtherFields);
                        if(f==-1)//Not Found-Must Login
                        {
                            ResponseContentType = "";
                            ResponseHeadders = null;
                            JavaUpdate = "";
                            //ProcessHTMLSlim.Release();
                            ResponseReDirect = "/" + HTMLLoginPage;
                            LocalHTMLSessionData.LastPageAccessed = ResponseReDirect;
                            return (true);
                        }
                        OtherFields.TryGetValue("XMLConfigInfo", out LocalHTMLSessionData.XMLConfig);
                        bool Continue = false;
                        if (!string.IsNullOrWhiteSpace(LocalHTMLSessionData.XMLConfig))
                        {
                            try
                            {
                                XmlDocument XML = new XmlDocument();
                                XML.LoadXml(LocalHTMLSessionData.XMLConfig);
                                XmlNodeList List = XML.SelectNodes("/root/configuration/security");
                                foreach (XmlElement e in List)
                                {
                                    for (int i = 0; i < e.Attributes.Count; i++)
                                    {
                                        if(e.Attributes[i].Name.ToLower().Trim()== "level")
                                        {
                                            if (SecurityCheckNoLoginRequired(e.Attributes[i].Value))
                                            {
                                                LocalHTMLSessionData.NeverLogOut = true;
                                                LocalHTMLSessionData.Loggedin = false;
                                                LocalHTMLSessionData.CurrentPageRequested = FileName + FileExtension;
                                                LocalHTMLSessionData.SecurityLevel = e.Attributes[i].Value;
                                                Continue = true;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            catch (Exception CHMAPIEx)
                            {

                            }
                            if (!Continue)
                            {
                                ResponseContentType = "";
                                ResponseHeadders = null;
                                ResponseReDirect = "/" + HTMLLoginPage;
                                LocalHTMLSessionData.LastPageAccessed = ResponseReDirect;
                                JavaUpdate = "";
                                //ProcessHTMLSlim.Release();
                                return (true);
                            }
                        }
                    }

                    if (FileName + FileExtension == HTMLRefreshRequest) //Send any updated Data to Page
                    {
                        if(LocalHTMLSessionData.Loggedin==false  && !LocalHTMLSessionData.NeverLogOut)
                        {
                            ResponseReDirect = "/" + HTMLLoginPage;
                            ResponseContentType = "";
                            ResponseHeadders = null;
                            JavaUpdate = "";
                            //ProcessHTMLSlim.Release();
                            return (true);

                        }
                        LocalHTMLSessionData.LastUpdateIsAutoRefresh = true;
                        LocalHTMLSessionData.TickAtLastActivity = SessionTime;
                        ResponseContentType = "";
                        ResponseHeadders = null;
                        //ResponseReDirect = "/" + LocalHTMLSessionData.CurrentPageRequested;
                        ResponseReDirect = "";
                        JavaUpdate = "";
                        string NewColor;

                        if (LocalHTMLSessionData.FlagValuesToDisplay != null && LocalHTMLSessionData.FlagValuesToDisplay.Length > 0)
                        {
                            Tuple<string, string, string>[] FlagValues;
                            FlagValues = ServerAccessFunctions.GetFlagInListFromServer(LocalHTMLSessionData.FlagValuesToDisplay);
                            string Val = "";
                            for (int i=0;i< FlagValues.Length;i++)
                            {

//                                if (LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValue != FlagValues[i].Item1 || LocalHTMLSessionData.FirstDisplay)
                                LocalHTMLSessionData.FlagValuesToDisplayData[i].LastFlagValue = LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValue;
                                LocalHTMLSessionData.FlagValuesToDisplayData[i].LastFlagValueUpdateTime = LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValueUpdateTime;
                                LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValue = FlagValues[i].Item1;
                                if (!string.IsNullOrEmpty(FlagValues[i].Item3))
                                    LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValue +=FlagValues[i].Item3;
                                LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValueUpdateTime = SessionTime;
                                Val += " "+LocalHTMLSessionData.FlagValuesToDisplayData[i].Literal + LocalHTMLSessionData.FlagValuesToDisplayData[i].CurrentFlagValue;
                                if (i+1 >= FlagValues.Length || LocalHTMLSessionData.FlagValuesToDisplayData[i].Id != LocalHTMLSessionData.FlagValuesToDisplayData[i+1].Id)
                                {
                                    JavaUpdate = JavaUpdate + "<updates  name=\"" + LocalHTMLSessionData.FlagValuesToDisplayData[i].Id + "\" value=\"" + Val.Trim() + "\"> </updates>\r\n";
                                    Val = "";
                                }

                            }
                        }
                        //NewColor = "BackgroundColorDeviceOn";
                        //JavaUpdate = JavaUpdate + "<updates  name=\"" + "Dev01" + "\" color=\"" + NewColor.Trim() + "\"> </updates>\r\n";
                        LocalHTMLSessionData.FirstDisplay = true;
                        //ProcessHTMLSlim.Release();
                        return (true);
                    }

                    if (TransType=="Post")
                    {
                        if(LocalHTMLSessionData.LastPageAccessed== HTMLLoginPage || LocalHTMLSessionData.LastPageAccessed == "/"+ HTMLLoginPage)
                        {
                            LocalHTMLSessionData.SecurityLevel = SecurityPasswordCheck(Form[0].Item2, Form[1].Item2);
                            if (LocalHTMLSessionData.SecurityLevel=="")
                            {
                                ResponseReDirect = HTMLLoginPage;
                                ResponseContentType = "";
                                ResponseHeadders = null;
                                _PluginCommonFunctions.GenerateLocalMessage(3,"", Form[0].Item2);
                                JavaUpdate = "";
                                //ProcessHTMLSlim.Release();
                                return (true);

                            }
                            LocalHTMLSessionData.Loggedin = true;
                            LocalHTMLSessionData.User = Form[0].Item2;
                            LocalHTMLSessionData.TickAtLastActivity = SessionTime;

                            _PluginCommonFunctions.GenerateLocalMessage(4, "", Form[0].Item2);
                        }
                    }

                    if (URLPath.Substring(0,8)=="/pushed/")
                    {
                        Tuple<string, string> TVal;
                        if (LocalHTMLSessionData.Actions.TryGetValue(FileName, out TVal))
                        {
                            FlagDataStruct NFStruct= ServerAccessFunctions.GetSingleFlagFromServerFull(TVal.Item1);
                            if (!string.IsNullOrEmpty(NFStruct.Name) && !string.IsNullOrEmpty(TVal.Item2))
                            {
                                ServerAccessFunctions.ProcessButtonMacro(NFStruct, TVal.Item2);

                                ResponseContentType = "";
                                ResponseHeadders = null;
                                ResponseReDirect = "";
                                JavaUpdate = "";
                                return (true);
                            }
                        }
                    }
                    int flag;
                    if (string.IsNullOrEmpty(FileName))
                    {
                        flag = -1;
                    }
                    else
                    {
                        string[] KeyFields = new string[] { "ObjectName", "ObjectType", "OwnerDLL" };
                        string[] KeyValues = new string[] { FileName + FileExtension, FileExtension.Substring(1), OwnerModule };
                        Dictionary<string, string> OtherFields;
                        string S;

                        if ((FileExtension == ".html" || FileExtension == ".js") && FileName + FileExtension != HTMLLoginPage.ToLower())
                        {
                            if (HTMLCommonFiles.ToLower().Contains(FileName + FileExtension))
                                KeyValues[2] = "$";
                            string htmlbody = "";
                            flag = _PDBA.GetObjectByFieldsIntoString("HTMLPages", KeyFields, KeyValues, "object", ref htmlbody, out OtherFields);
                            if (flag >= 0)
                            {
                                LocalHTMLSessionData.LastPageAccessed = FileName + FileExtension;
                                if (OtherFields.TryGetValue("IsTemplate", out S))
                                {
                                    if (S.ToUpper() == "Y")
                                    {
                                        ResponseContentType = "";
                                        ResponseHeadders = null;
                                        JavaUpdate = "";
                                        ResponseReDirect = HTMLLoginPage;
                                        //ProcessHTMLSlim.Release();
                                        return (false);
                                    }
                                }


                                //Now Process Startup Commands
                                if (FileExtension == ".html")
                                {
                                    if (!SecurityCheckNoLoginRequired(LocalHTMLSessionData.SecurityLevel))
                                    {
                                        LocalHTMLSessionData.NeverLogOut = false;
                                    }

                                    OtherFields.TryGetValue("XMLConfigInfo", out LocalHTMLSessionData.XMLConfig);
                                    if (!string.IsNullOrWhiteSpace(LocalHTMLSessionData.XMLConfig))
                                    {
                                        try
                                        {
                                            XmlDocument xmlDoc = new XmlDocument();
                                            xmlDoc.LoadXml(LocalHTMLSessionData.XMLConfig);
                                            XmlNode XmlMainNode = xmlDoc.FirstChild;
                                            Queue<DynamicHTMLDataStruct> Data = new Queue<DynamicHTMLDataStruct>();
                                            foreach (XmlNode RootNode in XmlMainNode)
                                            {
                                                try
                                                {
                                                    if(RootNode.Name== "configuration")
                                                    {
                                                        foreach (XmlNode ConfigNode in RootNode)
                                                        {
                                                            switch (ConfigNode.Name)
                                                            {
                                                                case "template":
                                                                    LocalHTMLSessionData.TemplateName = ConfigNode.Attributes["name"].Value;
                                                                    break;

                                                                case "security":
                                                                    if (SecurityCheckNoLoginRequired(ConfigNode.Attributes["level"].Value))
                                                                    {
                                                                        LocalHTMLSessionData.NeverLogOut = true;
                                                                        LocalHTMLSessionData.Loggedin = false;
                                                                        LocalHTMLSessionData.CurrentPageRequested = FileName + FileExtension;
                                                                        LocalHTMLSessionData.SecurityLevel = ConfigNode.Attributes["level"].Value;
                                                                    }
                                                                    break;
                                                            }

                                                        }
                                                        continue;
                                                    }
                                                    if (RootNode.Name == "body")
                                                    {
                                                        DynamicHTMLDataStruct dnsflag = new DynamicHTMLDataStruct();
                                                        dnsflag.DisplayAttributes = new List<DynamicHTMLAttributes>();
                                                        foreach (XmlNode BodyNode in RootNode.SelectNodes("display"))
                                                        {
                                                            string ID = "", Literal = "", Flag = "";
                                                            string Case = "", Mode = "", Color = "", Text = "", TextColor = "", Id="";
                                                            foreach (XmlNode DisplayNode in BodyNode)
                                                            {
                                                                switch (DisplayNode.Name)
                                                                {
                                                                    case "id":
                                                                        ID = DisplayNode.InnerText;
                                                                        break;

                                                                    case "literal":
                                                                        if(!string.IsNullOrEmpty(Literal))
                                                                        {
                                                                            DynamicHTMLDataStruct dnslit = new DynamicHTMLDataStruct();
                                                                            dnslit.Id = ID;
                                                                            dnslit.Literal = Literal;
                                                                            dnslit.Flag = Flag;
                                                                            Data.Enqueue(dnslit);
                                                                            Flag = "";
                                                                        }
                                                                        Literal = DisplayNode.InnerText;
                                                                        break;

                                                                    case "flag":
                                                                        if (!string.IsNullOrEmpty(Flag))
                                                                        {
                                                                            dnsflag = new DynamicHTMLDataStruct();
                                                                            dnsflag.Id = ID;
                                                                            dnsflag.Literal = Literal;
                                                                            dnsflag.Flag = Flag;
                                                                            Data.Enqueue(dnsflag);
                                                                            Literal = "";
                                                                        }
                                                                        Flag = DisplayNode.InnerText;
                                                                        break;

                                                                    //case "state":
                                                                    //{
                                                                    //        DynamicHTMLAttributes DynamicHTMLAttr = new DynamicHTMLAttributes();
                                                                    //        foreach (XmlAttribute a in DisplayNode)
                                                                    //        {
                                                                    //            switch (a.Name)
                                                                    //            {
                                                                    //                case "case":
                                                                    //                    {
                                                                    //                        Case = a.Value;
                                                                    //                        break;
                                                                    //                    }
                                                                    //                case "mode":
                                                                    //                    {
                                                                    //                        Mode = a.Value;
                                                                    //                        break;
                                                                    //                    }
                                                                    //                case "id":
                                                                    //                    {
                                                                    //                        Id = a.Value;
                                                                    //                        break;
                                                                    //                    }
                                                                    //                case "color":
                                                                    //                    {
                                                                    //                        Color = a.Value;
                                                                    //                        break;
                                                                    //                    }
                                                                    //                case "text":
                                                                    //                    { 
                                                                    //                            Text = a.Value;
                                                                    //                            break;
                                                                    //                    }
                                                                    //                case "textcolor":
                                                                    //                    {
                                                                    //                        TextColor = a.Value;
                                                                    //                        break;
                                                                    //                    }
                                                                                
                                                                    //            }


                                                                    //        }
                                                                    //        DynamicHTMLAttr.Case = Case;
                                                                    //        DynamicHTMLAttr.Color = Color;
                                                                    //        DynamicHTMLAttr.ID = ID;
                                                                    //        DynamicHTMLAttr.Mode = Mode;
                                                                    //        DynamicHTMLAttr.Text = Text;
                                                                    //        DynamicHTMLAttr.TextColor = TextColor;
                                                                    //        dnsflag.DisplayAttributes.Add(DynamicHTMLAttr);
                                                                    //}
                                                                    //break;

                                                                }
                                                            }
                                                            DynamicHTMLDataStruct dns = new DynamicHTMLDataStruct();
                                                            dns.Id = ID;
                                                            dns.Literal = Literal;
                                                            dns.Flag = Flag;
                                                            Data.Enqueue(dnsflag);
                                                        }
                                                    }
                                                    if (RootNode.Name == "pushed")
                                                    {
                                                        foreach (XmlNode ActionNode in RootNode.SelectNodes("action"))
                                                        {
                                                            string ID = "", Flag = "", Command = "";
                                                            foreach (XmlNode DisplayNode in ActionNode)
                                                            {
                                                                switch (DisplayNode.Name)
                                                                {
                                                                    case "id":
                                                                        ID = DisplayNode.InnerText.ToLower();
                                                                        break;

                                                                    case "flag":
                                                                        Flag = DisplayNode.InnerText.ToLower();
                                                                        break;

                                                                    case "macro":
                                                                        Command = DisplayNode.InnerText;
                                                                        break;
                                                                }
                                                            }
                                                                                                                       
                                                            LocalHTMLSessionData.Actions.Add(ID, new Tuple<string, string>(Flag, Command));
                                                            
                                                        }

                                                    }

                                                    LocalHTMLSessionData.FlagValuesToDisplayData = Data.ToArray();
                                                    LocalHTMLSessionData.FlagValuesToDisplay = new string[LocalHTMLSessionData.FlagValuesToDisplayData.Count()];
                                                    for (int i = 0; i < LocalHTMLSessionData.FlagValuesToDisplayData.Count(); i++)
                                                    {
                                                        LocalHTMLSessionData.FlagValuesToDisplay[i] = LocalHTMLSessionData.FlagValuesToDisplayData[i].Flag;
                                                        if (!string.IsNullOrEmpty(LocalHTMLSessionData.FlagValuesToDisplayData[i].Literal) && !string.IsNullOrEmpty(LocalHTMLSessionData.FlagValuesToDisplay[i]))
                                                            LocalHTMLSessionData.FlagValuesToDisplayData[i].Literal = LocalHTMLSessionData.FlagValuesToDisplayData[i].Literal + " ";
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
                                        //If it requires a template-Reload
                                        if (!string.IsNullOrEmpty(LocalHTMLSessionData.TemplateName))
                                        {
                                            string FileExtension2 = Path.GetExtension(LocalHTMLSessionData.TemplateName).ToLower();
                                            if (string.IsNullOrEmpty(FileExtension2))
                                                FileExtension2 = ".html";
                                            string FileName2 = Path.GetFileNameWithoutExtension(LocalHTMLSessionData.TemplateName).ToLower();
                                            string OwnerModule2=Path.GetDirectoryName(LocalHTMLSessionData.TemplateName).Trim('\\');
                                            string[] KeyFields2 = new string[] { "ObjectName", "ObjectType", "OwnerDLL" };
                                            string[] KeyValues2 = new string[] { FileName2 + FileExtension2, FileExtension2.Substring(1), OwnerModule2 };
                                            Dictionary<string, string> OtherFields2;
                                            htmlbody = "";
                                            flag = _PDBA.GetObjectByFieldsIntoString("HTMLPages", KeyFields2, KeyValues2, "object", ref htmlbody, out OtherFields2);
                                            if(flag<0)
                                            {
                                                _PluginCommonFunctions.GenerateErrorRecordLocalMessage(1000, "(" + flag.ToString() + ") ", LocalHTMLSessionData.TemplateName);
                                                ResponseContentType = "";
                                                ResponseHeadders = null;
                                                JavaUpdate = "";
                                                ResponseReDirect = HTMLLoginPage;
                                                //ProcessHTMLSlim.Release();
                                                return (false);
                                            }
                                            LocalHTMLSessionData.LastPageAccessed = FileName + FileExtension;
                                        }



                                        //Process Config info



                                    }
                                    //Add Required javascript Includes to bottom HTML page




                                }
                                ResponseBody = _PCF.ConvertStringToByteArray(htmlbody);
                            }
                        }
                        else
                        {
                            flag = _PDBA.GetObjectByFieldsIntoBytes("HTMLPages", KeyFields, KeyValues, "object", ref ResponseBody, out OtherFields);
                        }

                        if (flag < 0)
                            _PluginCommonFunctions.GenerateErrorRecordLocalMessage(1000, "(" + flag.ToString() + ") ", URL);
                    }
                    if(flag<0|| (!LocalHTMLSessionData.Loggedin && FileName + FileExtension != HTMLLoginImage && FileName + FileExtension!= HTMLLoginPage))
                    {
                        if (!SecurityCheckNoLoginRequired(LocalHTMLSessionData.SecurityLevel))
                        {

                            if (LocalHTMLSessionData.Loggedin == false)
                                ResponseReDirect = "/" + HTMLLoginPage;
                            else
                                ResponseReDirect = "/" + HTMLHomePage;
                        }
                        else
                            ResponseReDirect ="";

                        ResponseContentType = "";
                        ResponseHeadders = null;
                        JavaUpdate = "";
                        LocalHTMLSessionData.LastPageAccessed = ResponseReDirect;
                        //ProcessHTMLSlim.Release();
                        return (true);
                    }
                    HTMLObjectTypes.TryGetValue(FileExtension.Substring(1).ToLower(),out ResponseContentType);
                    ResponseHeadders = null;
                    ResponseReDirect = "";
                    if (FileExtension == ".html" && FileName + FileExtension != HTMLLoginPage.ToLower())
                    {
                         if (HTMLSessionTable.TryGetValue(SessionIDCode, out LocalHTMLSessionData))
                        {
                            LocalHTMLSessionData.LastPageAccessed = LocalHTMLSessionData.CurrentPageRequested;
                            LocalHTMLSessionData.CurrentPageRequested = Path.GetFileName(URLPath).ToLower();
                            if(!LocalHTMLSessionData.LastUpdateIsAutoRefresh)
                                LocalHTMLSessionData.TickAtLastActivity = SessionTime;
                            LocalHTMLSessionData.LastUpdateIsAutoRefresh = false;
                        }
                    }
                    JavaUpdate = "";
                    //ProcessHTMLSlim.Release();
                    return (flag >= 0);
                }

                catch (Exception CHMAPIEx)
                {
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    ResponseContentType = "";
                    ResponseHeadders = null;
                    ResponseReDirect = HTMLHomePage;
                    JavaUpdate = "";
                    //ProcessHTMLSlim.Release();
                    return (false);
                }


            }

        }

        #endregion
    }





    #region Nancy Classes
    public static class Extensions
    {
        public static Response FromByteArray(this IResponseFormatter formatter, byte[] body, string contentType = null)
        {
            return new ByteArrayResponse(body, contentType);
        }
    }

    public class ByteArrayResponse : Response
    {
        /// <summary>
        /// Byte array response
        /// </summary>
        /// <param name="body">Byte array to be the body of the response</param>
        /// <param name="contentType">Content type to use</param>
        public ByteArrayResponse(byte[] body, string contentType = null)
        {
            this.ContentType = contentType ?? "text / plain";

            this.Contents = stream =>
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(body);
                }
            };
        }
    }

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        //protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        //{
        //    base.ConfigureApplicationContainer(container);
        //    ResourceViewLocationProvider.RootNamespaces.Add(Assembly.GetAssembly(typeof(MainModule)), "VSMDemo.Web.Views");
        //}

        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            CookieBasedSessions.Enable(pipelines);
        }

        //protected override void ConfigureConventions(NancyConventions conventions)
        //{
        //    base.ConfigureConventions(conventions);
        //    conventions.StaticContentsConventions.Add(AddStaticResourcePath("/content", Assembly.GetAssembly(typeof(MainModule)), "VSMDemo.Web.Views.content"));
        //}

        //protected override NancyInternalConfiguration InternalConfiguration
        //{
        //    get
        //    {
        //        return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
        //    }
        //}

        void OnConfigurationBuilder(NancyInternalConfiguration x)
        {
            x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
        }



        public static Func<NancyContext, string, Response> AddStaticResourcePath(string requestedPath, Assembly assembly, string namespacePrefix)
        {
            return (context, s) =>
            {
                var path = context.Request.Path;
                if (!path.StartsWith(requestedPath))
                {
                    return null;
                }

                string resourcePath;
                string name;

                var adjustedPath = path.Substring(requestedPath.Length + 1);
                if (adjustedPath.IndexOf('/') >= 0)
                {
                    name = Path.GetFileName(adjustedPath);
                    resourcePath = namespacePrefix + "." + adjustedPath.Substring(0, adjustedPath.Length - name.Length - 1).Replace('/', '.');
                }
                else
                {
                    name = adjustedPath;
                    resourcePath = namespacePrefix;
                }
                return new EmbeddedFileResponse(assembly, resourcePath, name);
            };
        }
    }

    public class MainModule : NancyModule
    {
        public MainModule()
        {
            CHMModules.NancyFXPlugin.HTMLRoutines _HTMLPlugin = new CHMModules.NancyFXPlugin.HTMLRoutines();

            Get("/", x =>
            {
                try
                {
                    Tuple<string, string>[] Cookies = new Tuple<string, string>[Session.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, object> S in this.Session)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S.Key, S.Value.ToString());
                        if (S.Key != CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName)
                            Cookies[i] = tuple;
                        i++;
                    }

                    Tuple<string, object>[] Headers = new Tuple<string, object>[Request.Headers.Count()];
                    i = 0;
                    foreach (KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> S in Request.Headers)
                    {
                        Tuple<string, object> tuple = Tuple.Create(S.Key, (object)S.Value);
                        Headers[i] = tuple;
                        i++;
                    }
                    string ResponseContentType, ResponseReDirect, JavaUpdate;
                    byte[] ResponseBody = null;
                    Tuple<string, object>[] ResponseHeadders;
                    string SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if (!_HTMLPlugin.ProcessHTML(ref SessionCode, Request.Url.ToString(), "Get", "/", Request.Path.ToString(), Request.Url.SiteBase, null, ref Cookies, Headers, out ResponseContentType, out ResponseHeadders, out ResponseReDirect, out JavaUpdate, ref ResponseBody))
                        return (404);
                    Session[NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName] = SessionCode;

                    if (string.IsNullOrEmpty(ResponseReDirect) && string.IsNullOrEmpty(JavaUpdate) && ResponseBody == null)
                        return (204);

                    foreach (Tuple<string, string> t in Cookies)
                    {
                        if (t == null)
                            continue;
                        if (string.IsNullOrEmpty(t.Item2))
                            Session.Delete(t.Item1);
                        else
                            Session[t.Item1] = t.Item2;
                    }

                    if (!string.IsNullOrEmpty(ResponseReDirect))
                    {
                        return (Response.AsRedirect(ResponseReDirect));
                    }

                    if (!string.IsNullOrEmpty(JavaUpdate))
                    {
                        return (JavaUpdate);
                    }
                    return (Response.FromByteArray(ResponseBody, ResponseContentType));
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return Nancy.HttpStatusCode.NotFound;
                }

            });

            Get("/{name}", x =>
            {
                try
                {
                    Tuple<string, string>[] Cookies = new Tuple<string, string>[Session.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, object> S in this.Session)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S.Key, S.Value.ToString());
                        if (S.Key != CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName)
                            Cookies[i] = tuple;
                        i++;
                    }

                    Tuple<string, object>[] Headers = new Tuple<string, object>[Request.Headers.Count()];
                    i = 0;
                    foreach (KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> S in Request.Headers)
                    {
                        Tuple<string, object> tuple = Tuple.Create(S.Key, (object)S.Value);
                        Headers[i] = tuple;
                        i++;

                    }
                    string ResponseContentType, ResponseReDirect, JavaUpdate;
                    byte[] ResponseBody = null;
                    Tuple<string, object>[] ResponseHeadders;
                    string SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if(!_HTMLPlugin.ProcessHTML(ref SessionCode, Request.Url.ToString(), "Get", "/", Request.Path.ToString(), Request.Url.SiteBase, null, ref Cookies, Headers, out ResponseContentType, out ResponseHeadders, out ResponseReDirect, out JavaUpdate, ref ResponseBody))
                    return (404);
                    Session[NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName] = SessionCode;

                    if (string.IsNullOrEmpty(ResponseReDirect) && string.IsNullOrEmpty(JavaUpdate) && ResponseBody == null)
                        return (204);

                    foreach (Tuple<string, string> t in Cookies)
                    {
                        if (t == null)
                            continue;

                        if (string.IsNullOrEmpty(t.Item2))
                            Session.Delete(t.Item1);
                        else
                            Session[t.Item1] = t.Item2;
                    }

                    if (!string.IsNullOrEmpty(JavaUpdate))
                    {
                        return (JavaUpdate);
                    }

                    if (!string.IsNullOrEmpty(ResponseReDirect))
                    {
                        return (Response.AsRedirect(ResponseReDirect));
                    }


                    return (Response.FromByteArray(ResponseBody, ResponseContentType));
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return Nancy.HttpStatusCode.NotFound;
                }

            });




            Get("/{name3}/{name2}/{name}", x =>
            {
                try
                {
                    Tuple<string, string>[] Cookies = new Tuple<string, string>[Session.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, object> S in this.Session)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S.Key, S.Value.ToString());
                        if (S.Key != CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName)
                            Cookies[i] = tuple;
                        i++;
                    }

                    Tuple<string, object>[] Headers = new Tuple<string, object>[Request.Headers.Count()];
                    i = 0;
                    foreach (KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> S in Request.Headers)
                    {
                        Tuple<string, object> tuple = Tuple.Create(S.Key, (object)S.Value);
                        Headers[i] = tuple;
                        i++;

                    }
                    string ResponseContentType, ResponseReDirect, JavaUpdate;
                    byte[] ResponseBody = null;
                    Tuple<string, object>[] ResponseHeadders;
                    string SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if(!_HTMLPlugin.ProcessHTML(ref SessionCode, Request.Url.ToString(), "Get", (string)x.name3, Request.Path.ToString(), Request.Url.SiteBase, null, ref Cookies, Headers, out ResponseContentType, out ResponseHeadders, out ResponseReDirect, out JavaUpdate, ref ResponseBody))
                        return (404);
                    Session[NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName] = SessionCode;

                    if (string.IsNullOrEmpty(ResponseReDirect) && string.IsNullOrEmpty(JavaUpdate) && ResponseBody == null)
                        return (204);

                    foreach (Tuple<string, string> t in Cookies)
                    {
                        if (t == null)
                            continue;

                        if (string.IsNullOrEmpty(t.Item2))
                            Session.Delete(t.Item1);
                        else
                            Session[t.Item1] = t.Item2;
                    }

                    if (!string.IsNullOrEmpty(JavaUpdate))
                    {
                        return (JavaUpdate);
                    }

                    if (!string.IsNullOrEmpty(ResponseReDirect))
                    {
                        return (Response.AsRedirect(ResponseReDirect));
                    }

                    return (Response.FromByteArray(ResponseBody, ResponseContentType));
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return Nancy.HttpStatusCode.NotFound;
                }
            });


            Get("/{name2}/{name}", x =>
            {
                try
                {
                    Tuple<string, string>[] Cookies = new Tuple<string, string>[Session.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, object> S in this.Session)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S.Key, S.Value.ToString());
                        if (S.Key != CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName)
                            Cookies[i] = tuple;
                        i++;
                    }

                    Tuple<string, object>[] Headers = new Tuple<string, object>[Request.Headers.Count()];
                    i = 0;
                    foreach (KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> S in Request.Headers)
                    {
                        Tuple<string, object> tuple = Tuple.Create(S.Key, (object)S.Value);
                        Headers[i] = tuple;
                        i++;

                    }
                    string ResponseContentType, ResponseReDirect, JavaUpdate;
                    byte[] ResponseBody = null;
                    Tuple<string, object>[] ResponseHeadders;
                    string SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if (!_HTMLPlugin.ProcessHTML(ref SessionCode, Request.Url.ToString(), "Get", (string)x.name2, Request.Path.ToString(), Request.Url.SiteBase, null, ref Cookies, Headers, out ResponseContentType, out ResponseHeadders, out ResponseReDirect, out JavaUpdate, ref ResponseBody))
                        return (404);
                    Session[NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName] = SessionCode;

                    if (string.IsNullOrEmpty(ResponseReDirect) && string.IsNullOrEmpty(JavaUpdate) && ResponseBody == null)
                        return (204);

                    foreach (Tuple<string, string> t in Cookies)
                    {
                        if (t == null)
                            continue;

                        if (string.IsNullOrEmpty(t.Item2))
                            Session.Delete(t.Item1);
                        else
                            Session[t.Item1] = t.Item2;
                    }

                    if (!string.IsNullOrEmpty(JavaUpdate))
                    {
                        return (JavaUpdate);
                    }

                    if (!string.IsNullOrEmpty(ResponseReDirect))
                    {
                        return (Response.AsRedirect(ResponseReDirect));
                    }

                    return (Response.FromByteArray(ResponseBody, ResponseContentType));
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return Nancy.HttpStatusCode.NotFound;
                }
            });

            Post("/{name}", x =>
            {
                try
                {
                    string SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if(string.IsNullOrEmpty(SessionCode))
                        return (Response.AsRedirect(NancyFXPlugin.HTMLRoutines.HTMLLoginPage));


                    Tuple<string, string>[] Cookies = new Tuple<string, string>[Session.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, object> S in this.Session)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S.Key, S.Value.ToString());
                        if (S.Key != CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName)
                            Cookies[i] = tuple;
                        i++;
                    }

                    Tuple<string, string>[] Form = new Tuple<string, string>[Request.Form.Count];
                    i = 0;
                    foreach (string S in this.Request.Form.Keys)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S, Request.Form[S].ToString());
                        Form[i] = tuple;
                        i++;
                    }
 
                    Tuple<string, object>[] Headers = new Tuple<string, object>[Request.Headers.Count()];
                    i = 0;
                    foreach (KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> S in Request.Headers)
                    {
                        Tuple<string, object> tuple = Tuple.Create(S.Key, (object)S.Value);
                        Headers[i] = tuple;
                        i++;
                    }
                    string ResponseContentType, ResponseReDirect, JavaUpdate;
                    byte[] ResponseBody = null;
                    Tuple<string, object>[] ResponseHeadders;
                    SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if(!_HTMLPlugin.ProcessHTML(ref SessionCode, Request.Url.ToString(), "Post", "/", Request.Path.ToString(), Request.Url.SiteBase, Form, ref Cookies, Headers, out ResponseContentType, out ResponseHeadders, out ResponseReDirect, out JavaUpdate, ref ResponseBody))
                        return (404);
                    Session[NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName] = SessionCode;

                    if (string.IsNullOrEmpty(ResponseReDirect) && string.IsNullOrEmpty(JavaUpdate) && ResponseBody == null)
                        return (204);

                    foreach (Tuple<string, string> t in Cookies)
                    {
                        if (t == null)
                            continue;

                        if (string.IsNullOrEmpty(t.Item2))
                            Session.Delete(t.Item1);
                        else
                            Session[t.Item1] = t.Item2;
                    }

                    if (!string.IsNullOrEmpty(JavaUpdate))
                    {
                        return (JavaUpdate);
                    }

                    if (!string.IsNullOrEmpty(ResponseReDirect))
                    {
                        return (Response.AsRedirect(ResponseReDirect));
                    }
                    return (Response.FromByteArray(ResponseBody, ResponseContentType));
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return Nancy.HttpStatusCode.NotFound;
                }

            });

            Post("/{name}/{name2}", x =>
            {

                try
                {
                    string SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if (string.IsNullOrEmpty(SessionCode))
                        return (Response.AsRedirect(NancyFXPlugin.HTMLRoutines.HTMLLoginPage));


                    Tuple<string, string>[] Cookies = new Tuple<string, string>[Session.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, object> S in this.Session)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S.Key, S.Value.ToString());
                        if (S.Key != CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName)
                            Cookies[i] = tuple;
                        i++;
                    }

                    Tuple<string, string>[] Form = new Tuple<string, string>[Request.Form.Count];
                    i = 0;
                    foreach (string S in this.Request.Form.Keys)
                    {
                        Tuple<string, string> tuple = Tuple.Create(S, Request.Form[S].ToString());
                        Form[i] = tuple;
                        i++;
                    }

                    Tuple<string, object>[] Headers = new Tuple<string, object>[Request.Headers.Count()];
                    i = 0;
                    foreach (KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> S in Request.Headers)
                    {
                        Tuple<string, object> tuple = Tuple.Create(S.Key, (object)S.Value);
                        Headers[i] = tuple;
                        i++;
                    }
                    string ResponseContentType, ResponseReDirect, JavaUpdate;
                    byte[] ResponseBody = null;
                    Tuple<string, object>[] ResponseHeadders;
                    SessionCode = "";
                    try
                    {
                        SessionCode = (string)Session[CHMModules.NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName];
                    }
                    catch
                    {

                    }
                    if(!_HTMLPlugin.ProcessHTML(ref SessionCode, Request.Url.ToString(), "Post", (string)x.name2, Request.Path.ToString(), Request.Url.SiteBase, Form, ref Cookies, Headers, out ResponseContentType, out ResponseHeadders, out ResponseReDirect, out JavaUpdate, ref ResponseBody))
                        return (404);
                    Session[NancyFXPlugin.HTMLRoutines.HTTPSessionCookieName] = SessionCode;

                    if (string.IsNullOrEmpty(ResponseReDirect) && string.IsNullOrEmpty(JavaUpdate) && ResponseBody == null)
                        return (204);

                    foreach (Tuple<string, string> t in Cookies)
                    {
                        if (t == null)
                            continue;

                        if (string.IsNullOrEmpty(t.Item2))
                            Session.Delete(t.Item1);
                        else
                            Session[t.Item1] = t.Item2;
                    }

                    if (!string.IsNullOrEmpty(JavaUpdate))
                    {
                        return (JavaUpdate);
                    }

                    if (!string.IsNullOrEmpty(ResponseReDirect))
                    {
                        return (Response.AsRedirect(ResponseReDirect));
                    }

                    return (Response.FromByteArray(ResponseBody, ResponseContentType));
                }
                catch (Exception CHMAPIEx)
                {
                    _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
                    _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
                    return Nancy.HttpStatusCode.NotFound;
                }

            });

            //Get("/test", _ =>
            //{
            //    var responseThing = new
            //    {
            //        this.Request.Headers,
            //        this.Request.Query,
            //        this.Request.Form,
            //        this.Request.Session,
            //        this.Request.Method,
            //        this.Request.Url,
            //        this.Request.Path
            //    };

            //    return Response.AsJson(responseThing);
            //});
        }
    }
    #endregion

}

