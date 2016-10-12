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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CHMPluginAPICommon;



#region User Namespaces
using System.Collections;
#endregion

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
        public static string Replace(this string s, string oldValue, string newValue,
            StringComparison comparisonType)
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
namespace CHMModules
{
    using Extensions;

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
        public static string Replace(this string s, string oldValue, string newValue,
            StringComparison comparisonType)
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
    
    
    
    public class NetworkGatewayInterface_IH
    {

        internal Exception LastError; 
        internal string _IPAddress;
        internal string _Port;
        HttpWebRequest request;

        public bool NGWI_StillConnectedToServer()
        {
            return (true);

        }

        public bool NGWI_InitializePlugin(string IPAddress, string Port)
        {
            try
            {
                JObject DoThisToPreventAnErrorAndForceNewtonsoftLoad = new JObject();
                _IPAddress = IPAddress;
                _Port = Port;
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
                return (true);
            }
            catch (Exception e)
            {
                LastError = e;
                return (false);
            }

        }

        public bool NGWI_IsConnectedToDevice()
        {
            LastError = new Exception();
            return (true);
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
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream Answer = response.GetResponseStream();
                StreamReader _Answer = new StreamReader(Answer);
                string RTE = _Answer.ReadToEnd();
                OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CookiesReturned = new CookieCollection();
                string tokens = request.Headers["Cookie"];
                IncomingData.AddRange(Encoding.ASCII.GetBytes(RTE));
                if (!string.IsNullOrEmpty(tokens))
                {
                    string[] tokenlist = tokens.Split(';');
                    if (tokenlist.Length > 0)
                    {
                        int c = 0;
                        foreach (string t in tokenlist)
                        {
                            int q = t.IndexOf("=");
                            if (q > 0)
                            {
                                OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CookiesReturned.Add(new Cookie(t.Substring(0, q).Trim(), t.Substring(q + 1)));
                                c++;
                            }
                        }
                    }
                }
                else
                {
                    foreach (Cookie CKX in response.Cookies)
                    {
                        OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CookiesReturned.Add(CKX);
                    }
                }
                //Now We Get Any Replaceable Data


                if (OutgoingData.ReplaceableFieldValues != null && OutgoingData.ReplaceableFieldValues.Count > 0)
                {
                    for (int index = 0; index < OutgoingData.ReplaceableFieldValues.Count; index++)
                    {
                        OutgoingDataStruct.ReplaceFieldValues RFV;
                        RFV = OutgoingData.ReplaceableFieldValues[index];
                        if (RFV.WhereToFindDataValue == ReplaceFieldValues_DataFieldValueLocation.Json)
                        {
                            try
                            {
                                JObject root = JObject.Parse(RTE);
                                JToken Value = root[RFV.DataFieldValueName];
                                if (Value != null)
                                {
                                    RFV.ReplaceFieldValueValue = Value.ToString();
                                    OutgoingData.ReplaceableFieldValues[index] = RFV;
                                }

                            }
                            catch (Exception e)
                            {

                            }
                        }

                    }
                }
    
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
                
                String SB = System.Text.Encoding.ASCII.GetString(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CharactersToSend);
                //Debug.WriteLine(SB);

                if (SB.IndexOf("$$IPAddress")>=0)
                {
                    if (string.IsNullOrEmpty(_IPAddress))
                        return (-1);
                    string S = _IPAddress;

                    if (!string.IsNullOrEmpty(_Port))
                        S = S + ":" + _Port;
                    SB = SB.Replace("$$IPAddress", S, StringComparison.OrdinalIgnoreCase);
                }

                if (OutgoingData.ReplaceableFieldValues!=null && OutgoingData.ReplaceableFieldValues.Count > 0)
                {
                    
                    foreach (OutgoingDataStruct.ReplaceFieldValues RFV in OutgoingData.ReplaceableFieldValues)
                    {
                        if(RFV.HowToReplace==ReplaceFieldValues_ReplaceFieldValuesType.DirectReplace)
                        {
                            SB = SB.Replace(RFV.ReplaceFieldValueName, RFV.ReplaceFieldValueValue, StringComparison.OrdinalIgnoreCase);
                        }

                        if (RFV.HowToReplace == ReplaceFieldValues_ReplaceFieldValuesType.BracketedByChars)
                        {
                            int index = -1, start = -1, end = -1;
                            index = SB.IndexOf(RFV.ReplaceFieldValueName, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                start = SB.IndexOf(RFV.ReplaceStartingChar, index);
                                if (start >= 0)
                                {
                                    end = SB.IndexOf(RFV.ReplaceEndingChar, start + 1);
                                }
                                if (start >= 0 && end > 0)
                                {
                                    SB = SB.Substring(0, start + 1) + RFV.ReplaceFieldValueValue + SB.Substring(end);

                                }

                            }
                        }
                    }
                }

                
                if (SB.Length == 0)
                    return(-1);
                request = (HttpWebRequest)WebRequest.Create(SB);
                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Method))
                    request.Method = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Method;
                else
                    request.Method = "Get";

                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].HeadersToSend == null)
                {
                    request.Headers = new WebHeaderCollection();
                }
                else
                {
                    request.Headers = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].HeadersToSend;
                }
                request.CookieContainer = new CookieContainer();
                request.KeepAlive = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].KeepAlive;
                request.UseDefaultCredentials = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].UseDefaultCredentials;

                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Host))
                    request.Host = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Host;

                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Referer))
                    request.Referer = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Referer;

                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ContentType))
                    request.ContentType = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].ContentType;

                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Referer))
                    request.Referer = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Referer;

                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].UserAgent))
                    request.UserAgent = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].UserAgent;

                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Timeout > 0)
                    request.Timeout = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].Timeout;

                if (OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CookiesToSend != null)
                {
                    foreach (Cookie C in OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].CookiesToSend)
                    {
                        if (string.IsNullOrEmpty(C.Domain))
                        {
                            Uri Dom = new Uri(SB.ToString());
                            C.Domain = Dom.Host;
                        }
                        request.CookieContainer.Add(C);
                    }
                }

                if (!string.IsNullOrEmpty(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].BodyData))
                {
                    request.ContentLength = OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].BodyData.Length;
                    // Get the request stream.
                    StreamWriter dataStream = new StreamWriter(request.GetRequestStream());
                    // Write the data to the request stream.
                    dataStream.Write(OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].BodyData);
                    // Close the Stream object.
                    dataStream.Close();
                }
                else
                {
                    request.ContentLength = 0;
                }
                OutgoingData.CommDataControlInfo[CurrentCommDataControlInfoIndex].WaitForType = CommDataControlInfoStruct_WhatToWaitFor.Anything;
                return ((int)request.ContentLength);
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
            }
            catch 
            {
            }

            try
            {
            }
            catch 
            {
            }



        }

        public bool NGWI_ClearIncommingStream()
        {
            
            try
            {
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
