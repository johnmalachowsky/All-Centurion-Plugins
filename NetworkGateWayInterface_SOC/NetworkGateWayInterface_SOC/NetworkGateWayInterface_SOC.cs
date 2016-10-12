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
using CHMPluginAPICommon;
using WebSocketSharp;


#region User Namespaces
using System.Collections;
#endregion



namespace CHMModules
{

    public class NetworkGatewayInterface_SOC
    {
        internal string _IPAddress;
        internal string _Port;
        internal Exception LastError;
        internal WebSocket client;
        internal bool Connected = false;
        internal List<Byte> SavedIncomingData;


        public bool NGWI_StillConnectedToServer()
        {
            return (true);

        }



        public bool NGWI_InitializePlugin(string IPAddress, string Port)
        {
            try
            {
                _IPAddress = IPAddress;
                _Port = Port;
                LastError = new Exception();
                SavedIncomingData = new List<byte>();
                 return (true);
            }
            catch (Exception e)
            {
                LastError = e;
                return (false);
            }
        }

        public bool NGWI_ConnectToDevice(int ReceiveTimeout, int TransmitTimeout)
        {

            try
            {
                LastError = new Exception();
                client = new WebSocket("ws://" + _IPAddress + ":" + _Port);
                client.OnMessage += (sender, e) => WebSocketSharp_Notify(e);
                client.OnError += (sender, e) => WebSocketSharp_ErrorNotify(e);
                //               client.OnOpen+= (sender, e) => WebSocketSharp_Open();
                client.Connect();
                
                return (true);
            }
            catch (Exception e)
            {
                LastError = e;
                return (false);
            }

       }

        void WebSocketSharp_ErrorNotify(WebSocketSharp.ErrorEventArgs e)
        {

        }

        void WebSocketSharp_Notify(MessageEventArgs message)
        {
            SavedIncomingData.AddRange(message.RawData);
        }

        void WebSocketSharp_Open()
        {
            Connected = true;
        }


        public bool NGWI_IsConnectedToDevice()
        {
            LastError = new Exception();
            return (Connected);
        }


    /// <summary>
    /// Read Characters
    /// </summary>
    /// <param name="OutgoingData"></param>
    /// <param name="CharsToRead"></param>
    /// <param name="MaxToRead"></param>
    /// <param name="CurrentCommDataControlInfoIndex"></param>
    /// <returns -1=Error
    ///           0=Process Normally upon Return
    ///           1=Immediatly Return with "Transaction Succeeded"
    ///           2=Immediatly Return with "Transaction Failed"
    ///           3=Go to Next CurrentCommDataControlInfoIndex
    /// </returns>           
    ///  
    public int NGWI_ReadChars(ref OutgoingDataStruct OutgoingData, ref List<Byte> IncomingData, int MaxToRead, int CurrentCommDataControlInfoIndex)
        {
            try
            {
                IncomingData.AddRange(SavedIncomingData.ToArray());
                SavedIncomingData.Clear();

                LastError = new Exception();
                return (0);
            }
            catch (Exception e)
            {
                LastError = e;
                return (-1);
            }

        }

 /// <summary>
 /// 
 /// </summary>
 /// <param name="OutgoingData"></param>
 /// <param name="CurrentCommDataControlInfoIndex"></param>
 /// <returnsTotal Number of Characters Sent, -1 means error
 /// </returns>
        public int NGWI_WriteChars(ref OutgoingDataStruct OutgoingData, int CurrentCommDataControlInfoIndex)
        {

            try
            {
                client.Send(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CharactersToSend);
                OutgoingData.LastDataSent = DateTime.Now;
                LastError = new Exception();
                return (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CharactersToSend.Length);
            }
            catch (Exception e)
            {
                LastError = e;
                return (-1);
            }
        }

        public void NGWI_Close()
        {
            try
            {
                client.Close();
            }
            catch 
            {
            }

        }

        public bool NGWI_ClearIncommingStream()
        {
            try
            {
                SavedIncomingData.Clear();
                LastError = new Exception();
                return (true);
            }
            catch (Exception e)
            {
                LastError = e;
                return (false);
            }

        }

        public Exception NGWI_GetLastError()
        {
            return (LastError);
        }
    }
}
