using System;
using System.Collections.Generic;
using System.Net.Sockets;
using CHMPluginAPICommon;


#region User Namespaces
using System.Collections;
using System.Net.NetworkInformation;
#endregion



namespace CHMModules
{

    public class NetworkGatewayInterface_IP
    {
        internal System.Net.Sockets.TcpClient clientSocket;
        internal NetworkStream serverStream;
        internal string _IPAddress;
        internal string _Port;
        internal Exception LastError;


        public bool NGWI_StillConnectedToServer()
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation c in tcpConnections)
            {
                TcpState stateOfConnection = c.State;

                if (c.LocalEndPoint.Equals(clientSocket.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(clientSocket.Client.RemoteEndPoint))
                {
                    if (stateOfConnection == TcpState.Established)
                    {
                        return (true);
                    }
                    else
                    {
                        return (false);
                    }

                }
            }
            return (false);
        }

        public bool NGWI_InitializePlugin(string IPAddress, string Port)
        {
            try
            {
                _IPAddress = IPAddress;
                _Port = Port;
                LastError = new Exception();
                clientSocket = new System.Net.Sockets.TcpClient();
                if (clientSocket == null)
                    return (false);
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
                clientSocket.Close();
            }
            catch
            {

            }
           clientSocket = new System.Net.Sockets.TcpClient();
           LingerOption lingerOption = new LingerOption(true, 0);
           clientSocket.LingerState = lingerOption;

            try
            {
                LastError = new Exception();
                if (!clientSocket.Connected)
                    clientSocket.Connect(_IPAddress, int.Parse(_Port));
                if (!clientSocket.Connected)
                    return (false);
                serverStream = clientSocket.GetStream();
                serverStream.ReadTimeout = ReceiveTimeout;
                serverStream.WriteTimeout =TransmitTimeout;
                return (true);
            }
            catch (Exception e)
            {
                clientSocket.Close();
                try
                {
                    clientSocket = new System.Net.Sockets.TcpClient();
                    lingerOption = new LingerOption(true, 0);
                    clientSocket.LingerState = lingerOption;
                    clientSocket.Connect(_IPAddress, int.Parse(_Port));
                    serverStream = clientSocket.GetStream();
                    serverStream.ReadTimeout = ReceiveTimeout;
                    serverStream.WriteTimeout = TransmitTimeout;

                    return (true);
                }
                catch (Exception ex)
                {
                    LastError = ex;
                    return (false);

                }
            }

       }

        public bool NGWI_IsConnectedToDevice()
        {
            LastError = new Exception();
            if (clientSocket== null)
                return (false);
            return (clientSocket.Connected);
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
            int CharRead;
            try
            {
                while (serverStream.DataAvailable && IncomingData.Count < MaxToRead)
                {
                    OutgoingData.LastDataReceived = DateTime.Now;
                    CharRead = serverStream.ReadByte();
                    if (CharRead != -1)
                    {
                        IncomingData.Add((Byte)CharRead);
                    }
                }
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
                int count = 0;
                for (int i = 0; i < OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CharactersToSend.Length; i++)
                {
                    serverStream.WriteByte(Convert.ToByte(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CharactersToSend[i]));
                    count++;
                }
                OutgoingData.LastDataSent = DateTime.Now;
                LastError = new Exception();
                return (count);
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
                LastError = new Exception();
                if (serverStream!=null)
                    serverStream.Close();
            }
            catch 
            {
            }

            try
            {
                if (clientSocket!=null && clientSocket.Connected)
                    clientSocket.Close();
            }
            catch 
            {
            }



        }

        public bool NGWI_ClearIncommingStream()
        {
            
            int CharRead;
            try
            {
                while (serverStream.DataAvailable)
                {
                    CharRead = serverStream.ReadByte();//Empties Stream Before First Process
                }
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
