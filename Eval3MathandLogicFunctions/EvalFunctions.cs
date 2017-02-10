using System;
using Eval3;
using System.Collections.Generic;
using CHMPluginAPICommon;


namespace CHMPluginAPI
{
    // <summary>
    // Summary description for EvalFunctions.
    // </summary>
    public class EvalFunctions : iVariableBag
    {
        private List<Tuple<string, string, char>> StoredVariables;

        public event Eval3.iEvalValue.ValueChangedEventHandler ValueChanged;

        public ServerFunctionsStruct ServerFunctions;
        public bool UseFlags;

        public EvalFunctions()
        {
            StoredVariables = new List<Tuple<string, string, char>>();
            UseFlags = false;
        }


        public string Description
        {
            get
            {
                return "This module contains all the common functions";
            }
        }

        public EvalType EvalType
        {
            get
            {
                return EvalType.Object;
            }
        }

        public string Name
        {
            get
            {
                return "EvalFunctions";
            }
        }


        public object Value
        {
            get
            {
                return this;
            }
        }



        public double Sin(double v)
        {
            try
            {
                return Math.Sin(v);
            }
            catch
            {
                return (0);
            }

        }

        public double Cos(double v)
        {
            try
            {
                return Math.Cos(v);
            }
 			catch
            {
                return (0);
            }
        }

        public DateTime Now()
        {
            try
            {
                return System.DateTime.Now;
            }
 			catch
            {
                return (new DateTime());
            }
        }



        public double Mod(double x, double y)
        {
            try
            {
                return (x % y);
            }
 			catch
            {
                return (0);
            }
        }


        public object If(bool cond, object TrueValue, object FalseValue)
        {
            try
            {
                if (cond)
            {
                return TrueValue;
            }
            else
            {
                return FalseValue;
            }
            }
 			catch
            {
                return FalseValue;
            }
        }


        public DateTime Date(int year, int month, int day)
        {
            try
            {
                return new DateTime(year, month, day);
            }
            catch
            {
                return new DateTime();
            }
        }

        public int Year(DateTime d)
        {
            try
            {
                return d.Year;
            }
 			catch
            {
                return (0);
            }
        }

        public int Month(DateTime d)
        {
            try
            {
                return d.Month;
            }
 			catch
            {
                return (0);
            }
        }

        public int Day(DateTime d)
        {
            try
            {
                return d.Day;
            }
 			catch
            {
                return (0);
            }
        }



        public double Abs(double val)
        {
            try
            {
                if ((val < 0))
            {
                return (val * -1);
            }
            else
            {
                return val;
            }
            }
 			catch
            {
                return (0);
            }
        }

        public int Int(object value)
        {
            try
            {
                return (int)(value);
            }
 			catch
            {
                return (0);
            }
        }

        public int Trunc(double value, int prec)
        {
            try
            {
                value = (value - (0.5 / Math.Pow(10, prec)));
            //Warning!!!Optional parameters not supported

            return (int)(Math.Round(value, prec));
            }
 			catch
            {
                return (0);
            }
        }

        public double Dec(object value)
        {
            try
            {
                return (double)(value);
            }
 			catch
            {
                return (0);
            }
        }

        public double Round(object value)
        {
            try
            {
                return Math.Round((double)(value));
            }
 			catch
            {
                return (0);
            }
        }



        public double Exp(double Base, double pexp)
        {
            try
            {
                return Math.Pow(Base, pexp);
            }
 			catch
            {
                return (0);
            }
        }



        public double Sqrt(double v)
        {
            try
            {
                return Math.Sqrt(v);
            }
 			catch
            {
                return (0);
            }
        }

        public double Power(double v, double e)
        {
            try
            {
                return Math.Pow(v, e);
            }
 			catch
            {
                return (0);
            }
        }

        public System.Type systemType
        {
            get
            {
                return this.GetType();
            }
        }


        public string Trim(string str)
        {
            try
            {
                return str.Trim();
            }
 			catch
            {
                return String.Empty;
            }
        }

        public string LeftTrim(string str)
        {
            try
            {
                return str.TrimStart();
            }
 			catch
            {
                return String.Empty;
            }
        }

        public string RightTrim(string str)
        {
            try
            {
                return str.TrimEnd();
            }
 			catch
            {
                return String.Empty;
            }
        }

        public string PadLeft(string str, int wantedlen, string addedchar)
        {
            try
            {
                while ((str.Length < wantedlen))
            {
                str = (addedchar + str);
                // Warning!!! Optional parameters not supported
            }
            }
 			catch
            {
                return String.Empty;
            }
            return str;
        }

        public string[] anArray
        {
            get
            {
                return "How I want a drink alcoholic of course after the heavy lectures involving quantum mechanics".Split(' ');
            }
        }

        public string Chr(int c)
        {
            return "" + (char)(c);
        }

        public string ChCR()
        {
            return "\r";
        }

        public string ChLF()
        {
            return "\n";
        }

        public string ChCRLF()
        {
            return "\r\n";
        }

        public System.Type SystemType
        {
            get
            {
                return this.GetType();
            }
        }

        public string[] Split(string s, string delimiter)
        {
            try
            {
                return s.Split(delimiter[0]);
            }
 			catch
            {
                string[] sx = new string[0];
                return sx;
            }
            // Warning!!! Optional parameters not supported
        }

        System.DBNull DbNull()
        {
            return System.DBNull.Value;
        }

        string Replace(string Base, string search, string repl)
        {
            try
            {
                return Base.Replace(search, repl);
            }
 			catch
            {
                return String.Empty;
            }
        }

        //public string Substr(string s, int from, int len)
        //{
        //    try
        //    {
        //        if ((s == null))
        //        {
        //            return String.Empty;
        //        }
        //        // Warning!!! Optional parameters not supported
        //        from--;
        //        if ((from < 1))
        //        {
        //            from = 0;
        //        }
        //        if ((from >= s.Length))
        //        {
        //            from = s.Length;
        //        }
        //        if ((from + len) > s.Length)
        //        {
        //            len = (s.Length - from);
        //        }
        //        return s.Substring(from, len);
        //    }
        //    catch
        //    {
        //        return String.Empty;
        //    }
        //}

        public int Len(string str)
        {
            try
            {
                return str.Length;
            }
            catch
            {
                return (0);
            }
        }

        public string Lower(string value)
        {
            try
            {
                return value.ToLower();
            }
            catch
            {
                return String.Empty;
            }
        }

        public string Upper(string value)
        {
            try
            {
                return value.ToUpper();
            }
            catch
            {
                return String.Empty;
            }
        }

        public string WCase(string value)
        {
            try
            {
                if ((value.Length == 0))
                {
                    return "";
                }
                return (value.Substring(0, 1).ToUpper() + value.Substring(1).ToLower());
            }
            catch
            {
                return String.Empty;
            }
        }


        //*****************CHM Created Functions*************************

        public int ToInt(string num)
        {
            try
            {
                int x;
                int.TryParse(num, out x);
                return (x);

            }
            catch
            {
                return (0);
            }

        }

        public  byte ToByte(string num)
        {
            try
            {
                byte x;
                Byte.TryParse(num, out x);
                return (x);
            }
            catch
            {
                return (0);
            }

        }

        public double ToDouble(string num)
        {
            try
            {
                double x;
                Double.TryParse(num, out x);
                return (x);

            }
            catch
            {
                return (0);
            }
        }

        public double max(double a, double b)  //CHM Created
        {
            try
            {
                return Math.Max(a, b);
            }
            catch
            {
                return (0);
            }


        }

        public double min(double a, double b)  //CHM Created
        {
            try
            {
                return Math.Min(a, b);
            }
            catch (Exception)
            {

              return(0);
            } 
        }
        public iEvalValue GetVariable(string varname)  //CHM Created
        {
            if(UseFlags)
            {
                string S = ServerFunctions.GetSingleFlag(varname);
                if (string.IsNullOrEmpty(S))
                    return (null);

                double dd;
                if(double.TryParse(S, out dd))
                    return new Eval3.EvalVariable(dd, typeof(double));

                return new Eval3.EvalVariable(S, typeof(string));
            }





            Tuple<string, string, char> V;
            V = StoredVariables.Find(c => c.Item1 == varname.ToLower());

            if (V == null)
                return null;
            else
            {
                if (V.Item3 == '#')
                {
                    double dd;
                    double.TryParse(V.Item2, out dd);
                    return new Eval3.EvalVariable(dd, typeof(double));
                }
                else
                    return new Eval3.EvalVariable(V.Item2, typeof(string));

            }
        }

        public string ClearString()
        {
            return String.Empty;
        }


        public string Substr(string s, int from, int len)
        {
            try
            {
                if ((s == null))
                {
                    return String.Empty;
                }
                return s.Substring(from, len);
            }
            catch
            {
                return String.Empty;
            }
        }

        public bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public iEvalValue AddLocalVariable(string Name, string Value, string VCode)  //CHM Created
        {
            Tuple<string, string, char> V = new Tuple<string, string, char>(Name.ToLower(), Value, (char)VCode[0]);
            int a = StoredVariables.FindIndex(c => c.Item1 == Name.ToLower());
            if (a == -1)
                StoredVariables.Add(V);
            else
                StoredVariables[a] = V;

            return new Eval3.EvalVariable(StoredVariables.Count, typeof(double));

        }

        public iEvalValue ClearLocalVariables()  //CHM Created
        {
            StoredVariables.Clear();


            return new Eval3.EvalVariable(true, typeof(bool));

        }
    }

}
