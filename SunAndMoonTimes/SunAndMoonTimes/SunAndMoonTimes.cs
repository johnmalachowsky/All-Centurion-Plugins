using System;
using System.Collections.Specialized;
using CHMPluginAPI;
using CHMPluginAPICommon;



namespace CHMModules
{

    public class SunAndMoonTimes
    {

#region Required User Stuff


        static private _PluginCommonFunctions PluginCommonFunctions;

        public void PluginInitialize(int UniqueID)
        {
            ServerAccessFunctions.PluginDescription = "Sunrise, Sunset, Dawn, Dusk, Moonrise, Moonset Times";
            ServerAccessFunctions.PluginSerialNumber = "00001-00003";
            ServerAccessFunctions.PluginVersion = "1.0.0";
            
            PluginCommonFunctions = new _PluginCommonFunctions();
            PluginCommonFunctions.HeartbeatTimeCodeToInvoke = HeartbeatTimeCode.NewMinute;



            ServerAccessFunctions._HeartbeatServerEvent += HeartbeatServerEventHandler;
            ServerAccessFunctions._TimeEventServerEvent += TimeEventServerEventHandler;
            ServerAccessFunctions._InformationCommingFromServerServerEvent += InformationCommingFromServerServerEventHandler;
            ServerAccessFunctions._InformationCommingFromPluginServerEvent += InformationCommingFromPluginEventHandler;
            ServerAccessFunctions._WatchdogProcess += WatchdogProcessEventHandler;
            ServerAccessFunctions._ShutDownPlugin += ShutDownPluginEventHandler;
            ServerAccessFunctions._StartupInfoFromServer += StartupInfoEventHandler;
            ServerAccessFunctions._PluginStartupCompleted += PluginStartupCompleted;
            ServerAccessFunctions._IncedentFlag += IncedentFlagEventHandler;
            ServerAccessFunctions._PluginStartupInitialize += PluginStartupInitialize;	
            return;
        }


#endregion


        #region Optional User Stuff

        #region Optional User Globals

        static double Longitude = -9999, Latitude = -9999;
        static int Zone = -999;
        static bool DaylightSavingsTime = false, Running=false;
        static int DayNumber=-999;
        static int LastMinute = -999;
        static DateTime SunriseTime, SunsetTime;
        static DateTime dawnTime, duskTime;

        static internal DateTime MustRun = DateTime.MinValue;
        #endregion

        #region Optional User Routines

        private static void PluginStartupInitialize(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = false;

            ServerAccessFunctions.PluginStatus.StartupInitializedFinished = true;


        }

        private static void PluginStartupCompleted(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();

            Longitude = _PCF.ConvertToDouble(ServerAccessFunctions.GetSingleFlagFromServer("Longitude"));
            Latitude = _PCF.ConvertToDouble(ServerAccessFunctions.GetSingleFlagFromServer("Latitude"));
            Zone = _PCF.ConvertToInt32(ServerAccessFunctions.GetSingleFlagFromServer("UTCOffset"));
            MustRun = DateTime.MinValue;
            Running = true;


        }

        private static void HeartbeatServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
        {
            if (Running)
            {
                DateTime CurrentTime = _PluginCommonFunctions.CurrentTime;

                if (CurrentTime > MustRun || Value.DateValue.IsDaylightSavingTime() != DaylightSavingsTime || Value.DateValue.DayOfYear != DayNumber)
                {
                    if (Longitude == -9999 || Latitude == -9999 || Zone == -999)
                        return;
                    MustRun = _PluginCommonFunctions.CurrentTime.AddMinutes(1);

                    ProcessTimes(Longitude, Latitude, Zone);
                    MustRun = DateTime.MaxValue;
                    DaylightSavingsTime = Value.DateValue.IsDaylightSavingTime();
                    DayNumber=Value.DateValue.DayOfYear;

                }

                if(LastMinute!=CurrentTime.Minute)
                {
                    if (CurrentTime >= SunriseTime && CurrentTime < SunsetTime)
                    {
                        PreProcessAddEventForTransferToServer("Sunrise", "True", SunriseTime);
                        PreProcessAddEventForTransferToServer("Sunset", "False", SunsetTime);
                    }
                    else
                    {
                        PreProcessAddEventForTransferToServer("Sunrise", "False", SunriseTime);
                        PreProcessAddEventForTransferToServer("Sunset", "True", SunsetTime);
                    }

                    if (CurrentTime >= dawnTime && CurrentTime < duskTime)
                    {
                        PreProcessAddEventForTransferToServer("Dawn", "True", dawnTime);
                        PreProcessAddEventForTransferToServer("Dusk", "False", duskTime);
                    }
                    else
                    {
                        PreProcessAddEventForTransferToServer("Dawn", "False", dawnTime);
                        PreProcessAddEventForTransferToServer("Dusk", "True", duskTime);
                    }
                    LastMinute = CurrentTime.Minute;

                }
            }
        }


        private static void TimeEventServerEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
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

        private static void IncedentFlagEventHandler(ServerEvents WhichEvent, PluginEventArgs Value)
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

                }
            }
            catch (Exception CHMAPIEx)
            {
                _PCF.AddToUnexpectedErrorQueue(CHMAPIEx);
            }
            ServerAccessFunctions.InformationCommingFromServerSlim.Release();

        }
        private static void InformationCommingFromPluginEventHandler(ServerEvents WhichEvent)
        {
            _PluginCommonFunctions _PCF = new _PluginCommonFunctions();
            PluginEventArgs Value;


            ServerAccessFunctions.PluginInformationCommingFromPluginSlim.Wait();
            try
            {
                while (ServerAccessFunctions.PluginInformationCommingFromPluginQueue.TryDequeue(out Value))
                {

                    try
                    {
                        if (Value.PluginData.Command == PluginCommandsToPlugins.TransactionComplete)
                        {

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



        private static void ProcessTimes(double Longitude, double Latitude, int Zone)
        {
            DateTime CT = _PluginCommonFunctions.CurrentTime;
            DateTime CY = CT.AddDays(-1);
            CreateAndSetSunAndMoonFlags(Longitude, Latitude, CY, Zone, "Yesterday");
            DateTime CX = CT.AddDays(1);
            CreateAndSetSunAndMoonFlags(Longitude, Latitude, CX, Zone, "Tomorrow");
            CreateAndSetSunAndMoonFlags(Longitude, Latitude, CT, Zone, "Today");
            LastMinute = -1;
        }

        private static bool CreateAndSetSunAndMoonFlags(double Longitude, double Latitude, DateTime CurrentTime, int CurrentZone, string FlagModifier)
        {
            SunriseTime = CurrentTime;
            SunsetTime=CurrentTime;
            dawnTime = CurrentTime;
            duskTime = CurrentTime;
            DateTime MoonRiseTime = CurrentTime, MoonSetTime = CurrentTime;
            bool isSunrise = false, isSunset = false, isDawn = false, isDusk = false;
            LunarPhase LunaPhaseRoutine;
            SunTimes SunriseSunsetRoutines;
            Astronomy MoonRiseMoonsetRoutines;

            lock ("CreateAndSetSunAndMoonFlags")
            {

                SunriseSunsetRoutines = new SunTimes();
                SunriseSunsetRoutines.CalculateSunRiseSetTimes(Latitude, Longitude, CurrentTime, CurrentZone, ref SunriseTime, ref SunsetTime, ref isSunrise, ref isSunset, 0);
                SunriseSunsetRoutines.CalculateSunRiseSetTimes(Latitude, Longitude, CurrentTime, CurrentZone, ref  dawnTime, ref duskTime, ref isDawn, ref isDusk, 1);

                MoonRiseMoonsetRoutines = new Astronomy();
                MoonRiseMoonsetRoutines.moonRiseSet(Latitude, Longitude, CurrentTime, CurrentZone, ref MoonRiseTime, ref MoonSetTime);
                PreProcessAddFlagForTransferToServer("Moonrise Time " + FlagModifier, MoonRiseTime.ToString("HH:mm:ss:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);
                PreProcessAddFlagForTransferToServer("Moonset Time " + FlagModifier, MoonSetTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);

                LunaPhaseRoutine = new LunarPhase(CurrentTime);
                int ix = LunaPhaseRoutine.PhaseNum(CurrentTime);
                string PhaseName = LunarPhase.PhaseName[LunaPhaseRoutine.PhaseNum(CurrentTime)];
                PreProcessAddFlagForTransferToServer("MoonPhase " + FlagModifier, LunarPhase.PhaseName[LunaPhaseRoutine.PhaseNum(CurrentTime)], FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);

                if (isSunrise)
                {
                    PreProcessAddFlagForTransferToServer("Sunrise Time " + FlagModifier, SunriseTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);
                }
                else
                {
                    PreProcessAddFlagForTransferToServer("Sunrise Time " + FlagModifier, SunriseTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.delete);
                }

                if (isSunset)
                {
                    PreProcessAddFlagForTransferToServer("SunSet Time " + FlagModifier, SunsetTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);
                }
                else
                {
                    PreProcessAddFlagForTransferToServer("SunSet Time " + FlagModifier, SunsetTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.delete);
                }

                if (isDawn)
                {
                    PreProcessAddFlagForTransferToServer("Dawn Time " + FlagModifier, dawnTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);
                }
                else
                {
                    PreProcessAddFlagForTransferToServer("Dawn Time " + FlagModifier, dawnTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.delete);
                }

                if (isDusk)
                {
                    PreProcessAddFlagForTransferToServer("Dusk Time " + FlagModifier, duskTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.addorupdate);
                }
                else
                {
                    PreProcessAddFlagForTransferToServer("Dusk Time " + FlagModifier, duskTime.ToString("HH:mm:ss"), FlagChangeCodes.OwnerOnly, FlagActionCodes.delete);
                    
                }
            }
            return (true);
        }

        private static bool PreProcessAddFlagForTransferToServer(string Name, string Value, FlagChangeCodes ChangeCode, FlagActionCodes Operation)
        {
            StringDictionary SunMoonFlags = new StringDictionary();

            string V = SunMoonFlags[Name];
            if(!string.IsNullOrEmpty(V))
            {
                if (V == Value)
                    return (true);
                else
                    SunMoonFlags[Name] = Value;
            }
            else
            {
                SunMoonFlags.Add(Name, Value);
            }
            
            PluginCommonFunctions.AddFlagForTransferToServer(Name, Value, ChangeCode, Operation);
            return (true);
        }

        private static bool PreProcessAddEventForTransferToServer(string Name, string Value, DateTime EventTime)
        {
            PluginCommonFunctions.AddEventForTransferToServer(Name, Value, EventTime);
            return (true);
        }

        #endregion
    }
}

        #region Optional User Classes

    internal sealed class SunTimes
{
//////////////////////////////////////////////////////////////////////////////////////////////////////
//  
//  C# Singleton class and thread-safe class for calculating Sunrise and Sunset times.
//
// The algorithm was adapted from the JavaScript sample provided here)
//      http)//home.att.net/~srschmitt/script_sun_rise_set.html
//
//  NOTICE) this code is provided "as-is", without any warrenty, obligations or liability for it.
//          You may use this code freely for any use.
// 
//  Zacky Pickholz (zacky.pickholz@gmail.com)
//
/////////////////////////////////////////////////////////////////////////////////////////////////////   

    private const double mDR = Math.PI / 180;
    private const double mK1 = 15 * mDR * 1.0027379;

    private int[] mRiseTimeArr = new int[2] { 0, 0 };
    private int[] mSetTimeArr = new int[2] { 0, 0 };
    private double mRizeAzimuth = 0.0;
    private double mSetAzimuth = 0.0;

    private double[] mSunPositionInSkyArr = new double[2] { 0.0, 0.0 };
    private double[] mRightAscentionArr = new double[3] { 0.0, 0.0, 0.0 };
    private double[] mDecensionArr = new double[3] { 0.0, 0.0, 0.0 };
    private double[] mVHzArr = new double[3] { 0.0, 0.0, 0.0 };

    private bool mIsSunrise = false;
    private bool mIsSunset = false;

    internal abstract class Coords
    {
        internal protected int mDegrees = 0;
        internal protected int mMinutes = 0;
        internal protected int mSeconds = 0;

        public double ToDouble()
        {
            return Sign() * (mDegrees + ((double)mMinutes / 60) + ((double)mSeconds / 3600));
        }

        internal protected abstract int Sign();
    }

    public class LatitudeCoords : Coords
    {
        public enum Direction
        {
            North,
            South
        }
        internal protected Direction mDirection = Direction.North;

        public LatitudeCoords(int degrees, int minutes, int seconds, Direction direction)
        {
            mDegrees = degrees;
            mMinutes = minutes;
            mSeconds = seconds;
            mDirection = direction;
        }

        protected internal override int Sign()
        {
            return (mDirection == Direction.North ? 1 : -1);
        }
    }

    public class LongitudeCoords : Coords
    {
        public enum Direction
        {
            East,
            West
        }

        internal protected Direction mDirection = Direction.East;

        public LongitudeCoords(int degrees, int minutes, int seconds, Direction direction)
        {
            mDegrees = degrees;
            mMinutes = minutes;
            mSeconds = seconds;
            mDirection = direction;
        }

        protected internal override int Sign()
        {
            return (mDirection == Direction.East ? 1 : -1);
        }
    }

 
    /// <summary>
    /// Calculate sunrise and sunset times. Returns false if time zone and longitude are incompatible.
    /// </summary>
    /// <param name="lat">Latitude in decimal notation.</param>
    /// <param name="lon">Longitude in decimal notation.</param>
    /// <param name="date">Date for which to calculate.</param>
    /// <param name="riseTime">Sunrise time (output)</param>
    /// <param name="setTime">Sunset time (output)</param>
    /// <param name="isSunrise">Whether or not the sun rises at that day</param>
    /// <param name="isSunset">Whether or not the sun sets at that day</param>
    public bool CalculateSunRiseSetTimes(double lat, double lon, DateTime date, int SourceZone, 
                                            ref DateTime riseTime, ref DateTime setTime, 
                                            ref bool isSunrise, ref bool isSunset, int processloop)
    {

        double zone = -SourceZone;
        double jd = GetJulianDay(date) - 2451545;  // Julian day relative to Jan 1.5, 2000

        if ((Sign(zone) == Sign(lon)) && (zone != 0))
            return false;

        lon = lon / 360;
        double tz = zone / 24;
        double ct = jd / 36525 + 1;                                 // centuries since 1900.0
        double t0 = LocalSiderealTimeForTimeZone(lon, jd, tz);      // local sidereal time

        // get sun position at start of day
        jd += tz;
        CalculateSunPosition(jd, ct);
        double ra0 = mSunPositionInSkyArr[0];
        double dec0 = mSunPositionInSkyArr[1];

        // get sun position at end of day
        jd += 1;
        CalculateSunPosition(jd, ct);
        double ra1 = mSunPositionInSkyArr[0];
        double dec1 = mSunPositionInSkyArr[1];

        // make continuous 
        if (ra1 < ra0)
            ra1 += 2 * Math.PI;

        // initialize
        mIsSunrise = false;
        mIsSunset = false;

        mRightAscentionArr[0] = ra0;
        mDecensionArr[0] = dec0;

        // check each hour of this day
        for (int k = 0; k < 24; k++)
        {
            mRightAscentionArr[2] = ra0 + (k + 1) * (ra1 - ra0) / 24;
            mDecensionArr[2] = dec0 + (k + 1) * (dec1 - dec0) / 24;
            mVHzArr[2] = TestHour(k, zone, t0, lat, processloop);

            // advance to next hour
            mRightAscentionArr[0] = mRightAscentionArr[2];
            mDecensionArr[0] = mDecensionArr[2];
            mVHzArr[0] = mVHzArr[2];
        }

        riseTime = new DateTime(date.Year, date.Month, date.Day, mRiseTimeArr[0], mRiseTimeArr[1], 0);
        setTime = new DateTime(date.Year, date.Month, date.Day, mSetTimeArr[0], mSetTimeArr[1], 0);

        isSunset = true;
        isSunrise = true;
        if ((!mIsSunrise) && (!mIsSunset))
        {
            if (mVHzArr[2] < 0)
                isSunrise = false; // Sun down all day
            else
                isSunset = false; // Sun up all day
        }
        // sunrise or sunset
        else
        {
            if (!mIsSunrise)
                // No sunrise this date
                isSunrise = false;
            else if (!mIsSunset)
                // No sunset this date
                isSunset = false;
        }
        // neither sunrise nor sunset
          return true;
    }

    private int Sign(double value)
    {
        int rv = 0;

        if (value > 0.0) rv = 1;
        else if (value < 0.0) rv = -1;
        else rv = 0;

        return rv;
    }

    // Local Sidereal Time for zone
    private double LocalSiderealTimeForTimeZone(double lon, double jd, double z)
    {
        double s = 24110.5 + 8640184.812999999 * jd / 36525 + 86636.6 * z + 86400 * lon;
        s = s / 86400;
        s = s - Math.Floor(s);
        return s * 360 * mDR;
    }

    // determine Julian day from calendar date
    // (Jean Meeus, "Astronomical Algorithms", Willmann-Bell, 1991)
    private double GetJulianDay(DateTime date)
    {
        int month = date.Month;
        int day = date.Day;
        int year = date.Year;

        bool gregorian = (year < 1583) ? false : true;

        if ((month == 1) || (month == 2))
        {
            year = year - 1;
            month = month + 12;
        }

        double a = Math.Floor((double)year / 100);
        double b = 0;

        if (gregorian)
            b = 2 - a + Math.Floor(a / 4);
        else
            b = 0.0;

        double jd = Math.Floor(365.25 * (year + 4716))
                   + Math.Floor(30.6001 * (month + 1))
                   + day + b - 1524.5;

        return jd;
    }

    // sun's position using fundamental arguments 
    // (Van Flandern & Pulkkinen, 1979)
    private void CalculateSunPosition(double jd, double ct)
    {
        double g, lo, s, u, v, w;

        lo = 0.779072 + 0.00273790931 * jd;
        lo = lo - Math.Floor(lo);
        lo = lo * 2 * Math.PI;

        g = 0.993126 + 0.0027377785 * jd;
        g = g - Math.Floor(g);
        g = g * 2 * Math.PI;

        v = 0.39785 * Math.Sin(lo);
        v = v - 0.01 * Math.Sin(lo - g);
        v = v + 0.00333 * Math.Sin(lo + g);
        v = v - 0.00021 * ct * Math.Sin(lo);

        u = 1 - 0.03349 * Math.Cos(g);
        u = u - 0.00014 * Math.Cos(2 * lo);
        u = u + 0.00008 * Math.Cos(lo);

        w = -0.0001 - 0.04129 * Math.Sin(2 * lo);
        w = w + 0.03211 * Math.Sin(g);
        w = w + 0.00104 * Math.Sin(2 * lo - g);
        w = w - 0.00035 * Math.Sin(2 * lo + g);
        w = w - 0.00008 * ct * Math.Sin(g);

        // compute sun's right ascension
        s = w / Math.Sqrt(u - v * v);
        mSunPositionInSkyArr[0] = lo + Math.Atan(s / Math.Sqrt(1 - s * s));

        // ...and declination 
        s = v / Math.Sqrt(u);
        mSunPositionInSkyArr[1] = Math.Atan(s / Math.Sqrt(1 - s * s));
    }

    // test an hour for an event
    private double TestHour(int k, double zone, double t0, double lat, int processloop)
    {
        double[] ha = new double[3];
        double a, b, c, d, e, s, z;
        double time;
        int hr, min;
        double az, dz, hz, nz;

        ha[0] = t0 - mRightAscentionArr[0] + k * mK1;
        ha[2] = t0 - mRightAscentionArr[2] + k * mK1 + mK1;

        ha[1] = (ha[2] + ha[0]) / 2;    // hour angle at half hour
        mDecensionArr[1] = (mDecensionArr[2] + mDecensionArr[0]) / 2;  // declination at half hour

        s = Math.Sin(lat * mDR);
        c = Math.Cos(lat * mDR);
        z = 0;

        if (processloop==0)
            z = Math.Cos(90.833 * mDR);    // refraction + sun semidiameter at horizon (Sunrise/Sunset)
        if (processloop == 1)
            z = Math.Cos(96 * mDR);    // refraction + sun semidiameter at horizon (Dawn/Dusk)
    
        if (k <= 0)
            mVHzArr[0] = s * Math.Sin(mDecensionArr[0]) + c * Math.Cos(mDecensionArr[0]) * Math.Cos(ha[0]) - z;

        mVHzArr[2] = s * Math.Sin(mDecensionArr[2]) + c * Math.Cos(mDecensionArr[2]) * Math.Cos(ha[2]) - z;

        if (Sign(mVHzArr[0]) == Sign(mVHzArr[2]))
            return mVHzArr[2];  // no event this hour

        mVHzArr[1] = s * Math.Sin(mDecensionArr[1]) + c * Math.Cos(mDecensionArr[1]) * Math.Cos(ha[1]) - z;

        a = 2 * mVHzArr[0] - 4 * mVHzArr[1] + 2 * mVHzArr[2];
        b = -3 * mVHzArr[0] + 4 * mVHzArr[1] - mVHzArr[2];
        d = b * b - 4 * a * mVHzArr[0];

        if (d < 0)
            return mVHzArr[2];  // no event this hour

        d = Math.Sqrt(d);
        e = (-b + d) / (2 * a);

        if ((e > 1) || (e < 0))
            e = (-b - d) / (2 * a);

        time = (double)k + e + (double)1 / (double)120; // time of an event

        hr = (int)Math.Floor(time);
        min = (int)Math.Floor((time - hr) * 60);

        hz = ha[0] + e * (ha[2] - ha[0]);                 // azimuth of the sun at the event
        nz = -Math.Cos(mDecensionArr[1]) * Math.Sin(hz);
        dz = c * Math.Sin(mDecensionArr[1]) - s * Math.Cos(mDecensionArr[1]) * Math.Cos(hz);
        az = Math.Atan2(nz, dz) / mDR;
        if (az < 0) az = az + 360;

        if ((mVHzArr[0] < 0) && (mVHzArr[2] > 0))
        {
            mRiseTimeArr[0] = hr;
            mRiseTimeArr[1] = min;
            mRizeAzimuth = az;
            mIsSunrise = true;
        }

        if ((mVHzArr[0] > 0) && (mVHzArr[2] < 0))
        {
            mSetTimeArr[0] = hr;
            mSetTimeArr[1] = min;
            mSetAzimuth = az;
            mIsSunset = true;
        }

        return mVHzArr[2];
    }
}

    internal sealed class Astronomy
    {
        private string[] outmoon = new string[3]; //= ""
        private string[] outsun = new string[9]; //= ""
        private double rads = 0.0174532925;

        /// <summary>
        /// Gets the sun set time.
        /// </summary>
        /// <value>The sun set.</value>

        /// <summary>
        /// Gets the moon rise.
        /// </summary>
        /// <value>The moon rise.</value>
        public string MoonRise
        {
            get
            {
                return outmoon[1];
            }
            set
            {

            }
        }
        /// <summary>
        /// Gets the moon set.
        /// </summary>
        /// <value>The moon set.</value>
        public string MoonSet
        {
            get
            {
                return outmoon[2];
            }
            set
            {

            }
        }


        /// <summary>
        /// Julian Date
        ///  - Calculate a Julian date from data and time
        /// </summary>
        /// <param name="DateDay">The date day.</param>
        /// <returns></returns>
        public double JulianDate(DateTime DateDay)
        {
            double Year = 0;
            double Month = 0;
            double Day = 0;

            double Hour = 0;
            double Minutes = 0;
            double Seconds = 0;

            double a = 0;
            double b = 0;
            double c = 0;
            double d = 0;
            double j = 0;

            Hour = DateDay.Hour;
            Minutes = DateDay.Minute;
            Seconds = DateDay.Second;

            Hour = Hour + (Minutes / 60) + (Seconds / 3600);
            Hour /= 24;

            Year = DateDay.Year;
            Month = DateDay.Month;
            Day = DateDay.Day;
            if (Convert.ToInt32(Month) == 1 || Convert.ToInt32(Month) == 2)
            {
                Year -= 1;
                Month += 12;
            }

            if (Year < 1582 && Month < 10 && Day < 15)
            {
                a = 0;
                b = 0;
            }
            else
            {
                a = (double)Math.Floor((double)(Year / 100));
                b = 2 - a + (double)Math.Floor((double)(a / 4));
            }

            c = (int)Math.Floor((double)(365.25 * Year));
            d = (int)Math.Floor((double)(30.6001 * (Month + 1)));
            j = b + c + d + Day + 1720994.5 + Hour;
            return j;
        }

        /// <summary>
        /// Modifies julian date.
        ///  - Returns the Modify Julian Date
        /// </summary>
        /// <param name="DayTimeDate">The day time date.</param>
        /// <returns></returns>
        public double ModifyJulianDate(DateTime DayTimeDate)
        {

            //Takes the day, month, year and hours in the day and returns the
            //modified julian day number defined as mjd = jd - 2400000.5
            //checked OK for Greg era dates - 26th Dec 02
            //
            double a = 0;
            double b = 0;
            int Day = 0;
            int Month = 0;
            int Year = 0;
            int Hour = 0;

            Day = DayTimeDate.Day;
            Month = DayTimeDate.Month;
            Year = DayTimeDate.Year;
//			Hour = DayTimeDate.Hour;
            if (Month <= 2)
            {
                Month = Month + 12;
                Year = Year - 1;
            }

            a = 10000.0 * Year + 100.0 * Month + Day;
            if (a <= 15821004.1)
            {
                b = -2 *  Math.Floor((double)(Year + 4716) / 4) - 1179;

            }
            else
            {
                b = Math.Floor((double)Year / 400) - Math.Floor((double)Year / 100) + Math.Floor((double)Year / 4);
            }
            a = 365.0 * Year - 679004.0;
            return (a + b + Math.Floor(30.6001 * (Month + 1)) + Day + Hour / 24.0);

        }

        private double sin_alt(double iobj, double mjd0, double hour, double glong, double cglat, double sglat)
        {

            //	this rather mickey mouse function takes a lot of
            ////  arguments and then returns the sine of the altitude of
            ////  the double labelled by iobj. iobj = 1 is moon, iobj = 2 is sun

            double mjd = 0;
            double t = 0;
            double ra = 0;
            double dec = 0;
            double tau = 0;
            double salt = 0;

            double[] objpos;
            mjd = mjd0 + hour / 24.0;
            t = (mjd - 51544.5) / 36525.0;
            objpos = minimoon(t);
            ra = objpos[2];
            dec = objpos[1];
            //// hour angle of double
            tau = 15.0 * (lmst(mjd, glong) - ra);
            //// sin(alt) of double using the conversion formulas
            salt = sglat * Math.Sin(Convert.ToDouble(rads * dec)) + cglat * Math.Cos(Convert.ToDouble(rads * dec)) * Math.Cos(Convert.ToDouble(rads * tau));
            return salt;
        }

        private double[] minimoon(double t)
        {
            ////
            //// takes t and returns the geocentric ra and dec in an array mooneq
            //// claimed good to 5' (angle) in ra and 1' in dec
            //// tallies with another approximate method and with ICE for a couple of dates
            ////
            var p2 = 2 * Math.PI;
            var arc = 206264.8062;
            var coseps = 0.91748;
            var sineps = 0.39778;
            double L0 = 0;
            double L = 0;
            double LS = 0;
            double F = 0;
            double D = 0;
            double H = 0;
            double S = 0;
            double N = 0;
            double DL = 0;
            double CB = 0;
            double L_moon = 0;
            double B_moon = 0;
            double V = 0;
            double W = 0;
            double X = 0;
            double Y = 0;
            double Z = 0;
            double RHO = 0;
            double[] mooneq = new double[3];
            double ra = 0;
            double dec = 0;

            L0 = frac(0.606433 + 1336.855225 * t); //mean longitude of moon
            L = p2 * frac(0.374897 + 1325.55241 * t); //mean anomaly of Moon
            LS = p2 * frac(0.993133 + 99.997361 * t); //mean anomaly of Sun
            D = p2 * frac(0.827361 + 1236.853086 * t); //difference in longitude of moon and sun
            F = p2 * frac(0.259086 + 1342.227825 * t); //mean argument of latitude

            //// corrections to mean longitude in arcsec
            DL = 22640 * Math.Sin(Convert.ToDouble(L));
            DL += -4586 * Math.Sin(Convert.ToDouble(L - 2 * D));
            DL += +2370 * Math.Sin(2 * D);
            DL += +769 * Math.Sin(2 * L);
            DL += -668 * Math.Sin(Convert.ToDouble(LS));
            DL += -412 * Math.Sin(2 * F);
            DL += -212 * Math.Sin(2 * L - 2 * D);
            DL += -206 * Math.Sin(Convert.ToDouble(L + LS - 2 * D));
            DL += +192 * Math.Sin(Convert.ToDouble(L + 2 * D));
            DL += -165 * Math.Sin(Convert.ToDouble(LS - 2 * D));
            DL += -125 * Math.Sin(Convert.ToDouble(D));
            DL += -110 * Math.Sin(Convert.ToDouble(L + LS));
            DL += +148 * Math.Sin(Convert.ToDouble(L - LS));
            DL += -55 * Math.Sin(2 * F - 2 * D);

            //// simplified form of the latitude terms
            S = F + (DL + 412 * Math.Sin(2 * F) + 541 * Math.Sin(Convert.ToDouble(LS))) / arc;
            H = F - 2 * D;
            N = -526 * Math.Sin(Convert.ToDouble(H));
            N += +44 * Math.Sin(Convert.ToDouble(L + H));
            N += -31 * Math.Sin(Convert.ToDouble(-L + H));
            N += -23 * Math.Sin(Convert.ToDouble(LS + H));
            N += +11 * Math.Sin(Convert.ToDouble(-LS + H));
            N += -25 * Math.Sin(-2 * L + F);
            N += +21 * Math.Sin(Convert.ToDouble(-L + F));

            //// ecliptic long and lat of Moon in rads
            L_moon = p2 * frac(L0 + DL / 1296000);
            B_moon = (18520.0 * Math.Sin(Convert.ToDouble(S)) + N) / arc;

            //// equatorial coord conversion - note fixed obliquity
            CB = Math.Cos(Convert.ToDouble(B_moon));
            X = CB * Math.Cos(Convert.ToDouble(L_moon));
            V = CB * Math.Sin(Convert.ToDouble(L_moon));
            W = Math.Sin(Convert.ToDouble(B_moon));
            Y = coseps * V - sineps * W;
            Z = sineps * V + coseps * W;
            RHO = Math.Sqrt(1.0 - Z * Z);
            dec = (360.0 / p2) * Math.Atan(Convert.ToDouble(Z / RHO));
            ra = (48.0 / p2) * Math.Atan(Convert.ToDouble(Y / (X + RHO)));
            if (ra < 0)
            {
                ra += 24;
            }

            mooneq[1] = dec;
            mooneq[2] = ra;
            return mooneq;
        }

        private double frac(double x)
        {
            ////
            ////	returns the fractional part of x as used in minimoon and minisun
            ////
            double a = 0;
            a = x - Math.Floor(x);
            if (a < 0)
            {
                a += 1;
            }
            return a;

        }

        private double lmst(double mjd, double glong)
        {
            ////
            ////	Takes the mjd and the longitude (west negative) and then returns
            ////  the local sidereal time in hours. Im using Meeus formula 11.4
            ////  instead of messing about with UTo and so on
            ////
            double lst = 0;
            double t = 0;
            double d = 0;
            d = mjd - 51544.5;
            t = d / 36525.0;
            lst = range(280.46061837 + 360.98564736629 * d + 0.000387933 * t * t - t * t * t / 38710000);
            return (lst / 15.0 + glong / 15);
        }

        private double range(double x)
        {
            ////
            ////	returns an angle in degrees in the range 0 to 360
            ////
            double a = 0;
            double b = 0;
            b = x / 360;
            a = 360 * (b - ipart(b));
            if (a < 0)
            {
                a = a + 360;
            }
            return a;
        }

        private double ipart(double x)
        {
            ////
            ////	returns the integer part - like int() in basic
            ////
            double a = 0;
            if (x > 0)
            {
                a = Math.Floor(x);

            }
            else
            {
                a = Math.Round(x);
            }
            return a;
        }


        private double[] quad(double ym, double yz, double yp)
        {
            ////
            ////	finds the parabola throuh the three points (-1,ym), (0,yz), (1, yp)
            ////  and returns the coordinates of the max/min (if any) xe, ye
            ////  the values of x where the parabola crosses zero (roots of the quadratic)
            ////  and the number of roots (0, 1 or 2) within the interval [-1, 1]
            ////
            ////	well, this routine is producing sensible answers
            ////
            ////  results passed as array [nz, z1, z2, xe, ye]
            ////
            double nz = 0;
            double a = 0;
            double b = 0;
            double c = 0;
            double dis = 0;
            double dx = 0;
            double xe = 0;
            double ye = 0;
            double z1 = 0;
            double z2 = 0;
            double[] quadout = new double[5];

            nz = 0;
            a = 0.5 * (ym + yp) - yz;
            b = 0.5 * (yp - ym);
            c = yz;
            xe = -b / (2 * a);
            ye = (a * xe + b) * xe + c;
            dis = b * b - 4.0 * a * c;
            if (dis > 0)
            {
                dx = 0.5 * Math.Sqrt(Convert.ToDouble(dis)) / Math.Abs(a);
                z1 = xe - dx;
                z2 = xe + dx;
                if (Math.Abs(z1) <= 1.0)
                {
                    nz += 1;
                }

                if (Math.Abs(z2) <= 1.0)
                {
                    nz += 1;
                }

                if (z1 < -1.0)
                {
                    z1 = z2;
                }

            }
            quadout[0] = nz;
            quadout[1] = z1;
            quadout[2] = z2;
            quadout[3] = xe;
            quadout[4] = ye;
            return quadout;
        }


        /// <summary>
        /// Calculate a moon rise and moon set
        /// </summary>
        /// <param name="mjd">The Modify Julian Date input only date not time</param>
        /// <param name="tz">The Time Zone</param>
        /// <param name="glong">The longitude place in Decimal degrees </param>
        /// <param name="glat">The latitude place in Decimal degrees</param>
        /// <returns></returns>
        public void moonRiseSet(double Latitude, double Longitude,  DateTime CurrentTime, double CurrentZone, ref DateTime MoonRiseTime, ref DateTime MoonSetTime)
        {
            string[] Today = new string[2];
            string[] Yesterday = new string[2];
            string[] Tomorrow = new string[2];
            DateTime DayToProcess = new DateTime();

            DayToProcess = CurrentTime;
            Today = _moonRiseSet(DayToProcess, CurrentZone, Longitude, Latitude);
            if (Today[0].Length == 0)
            {
                DayToProcess = CurrentTime.AddDays(-1);
                Yesterday = _moonRiseSet(DayToProcess, CurrentZone, Longitude, Latitude);
                MoonRiseTime = new DateTime(DayToProcess.Year, DayToProcess.Month, DayToProcess.Day, Convert.ToInt16(Yesterday[0].Substring(0, 2)), Convert.ToInt16(Yesterday[0].Substring(2, 2)),0);
            }
            else
            {
                MoonRiseTime = new DateTime(DayToProcess.Year, DayToProcess.Month, DayToProcess.Day, Convert.ToInt16(Today[0].Substring(0, 2)), Convert.ToInt16(Today[0].Substring(2, 2)), 0);
            }

            DayToProcess = CurrentTime;
            if (Today[1].Length == 0)
            {
                DayToProcess = CurrentTime.AddDays(1);
                Tomorrow = _moonRiseSet(DayToProcess, CurrentZone, Longitude, Latitude);
                MoonSetTime = new DateTime(DayToProcess.Year, DayToProcess.Month, DayToProcess.Day, Convert.ToInt16(Tomorrow[1].Substring(0, 2)), Convert.ToInt16(Tomorrow[1].Substring(2, 2)), 0);
            }
            else
            {
                MoonSetTime = new DateTime(DayToProcess.Year, DayToProcess.Month, DayToProcess.Day, Convert.ToInt16(Today[1].Substring(0, 2)), Convert.ToInt16(Today[1].Substring(2, 2)), 0);
            }
        }
    
        private string[] _moonRiseSet(DateTime DayToProcess, double tz, double glong, double glat)
        {
            double sglat = 0;
            double data = 0;
            double ym = 0;
            double yz = 0;
            bool above = false;
            double utrise = 0;
            double utset = 0;
//			double j = 0;
            double yp = 0;
            double nz = 0;
            bool rise = false;
            bool sett = false;
            double hour = 0;
            double z1 = 0;
            double z2 = 0;
            double xe = 0;
            double ye = 0;
            var rads = 0.0174532925;
            double[] quadout;
            double sinho = 0;
            double cglat = 0;
            double mjd = ModifyJulianDate(DayToProcess);

            string[] outmoon=new string[2];
            outmoon[0] = "";
            outmoon[1] = "";

            sinho = Math.Sin(rads * 8 / 60.0); ////moonrise taken as centre of moon at +8 arcmin
            sglat = Math.Sin(rads * glat);
            cglat = Math.Cos(rads * glat);
            data = mjd - (double)((double)tz / (double)24.0);
            rise = false;
            sett = false;
            above = false;
            hour = 1.0;
            ym = sin_alt(1, data, hour - 1.0, glong, cglat, sglat) - sinho;
            if (ym > 0.0)
            {
                above = true;
            }


            while (hour < 25 && (Convert.ToBoolean(sett) == false || Convert.ToBoolean(rise) == false))
            {
                yz = sin_alt(1, data, hour, glong, cglat, sglat) - sinho;
                yp = sin_alt(1, data, hour + 1.0, glong, cglat, sglat) - sinho;
                quadout = quad(ym, yz, yp);
                nz = quadout[0];
                z1 = quadout[1];
                z2 = quadout[2];
                xe = quadout[3];
                ye = quadout[4];

                //// case when one event is found in the interval
                if (Convert.ToInt32(nz) == 1)
                {
                    if (ym < 0.0)
                    {
                        utrise = hour + z1;
                        rise = true;

                    }
                    else
                    {
                        utset = hour + z1;
                        sett = true;
                    }
                } //// end of nz = 1 case

                //// case where two events are found in this interval
                //// (rare but whole reason we are not using simple iteration)
                if (Convert.ToInt32(nz) == 2)
                {
                    if (ye < 0.0)
                    {
                        utrise = hour + z2;
                        utset = hour + z1;

                    }
                    else
                    {
                        utrise = hour + z1;
                        utset = hour + z2;
                    }
                }

                //// set up the next search interval
                ym = yp;
                hour += 2.0;

            }

            if (Convert.ToBoolean(rise) == true || Convert.ToBoolean(sett) == true)
            {
                if (Convert.ToBoolean(rise) == true)
                    outmoon[0] = hrsmin(utrise);
            else
                    outmoon[0] = "";
                if (Convert.ToBoolean(sett) == true)
                    outmoon[1] = hrsmin(utset);
                else
                    outmoon[1] = "";

            }
            else
            {
                if (Convert.ToBoolean(above) == true)
                {
                    outmoon[0] = ""; //+ always_up
                    outmoon[1] = ""; //+ always_up
                }
                else
                {
                    outmoon[0] = "";
                    outmoon[1] = "";
                }
            }
           
           
            return (outmoon);
        }

        private string hrsmin(double hours)
        {
            ////
            ////	takes decimal hours and returns a string in hhmm format
            ////
            double hrs = 0;
            double h = 0;
            double m = 0;
            double dum = 0;
            string Stime;

            hrs = Math.Floor(hours * 60 + 0.5) / 60.0;
            h = Math.Floor(hrs);
            m = Math.Floor(60 * (hrs - h) + 0.5);
            dum = h * 100 + m;

            Stime = dum.ToString();

            if (dum < 1000)
                Stime = "0" + Stime;
            if (dum < 100)
                Stime = "0" + Stime;
            if (dum < 10)
                Stime = "0" + Stime;

            return(Stime);
        }


    }

    internal sealed class LunarPhase
    {
        // Calculate lunar phase times; thanks to Martin Lewicki (from an alt.astrology.moderated posting) as well as the script at
        // http://www.fourmilab.ch/earthview/pacalc.html (by John Walker)

        
        public const string Copyright = "LunarPhase.cs, Copyright  2004-2005 by Robert Misiak";

        /// <summary>
        /// UT lunar phase time.
        /// </summary>
        public readonly DateTime NewMoon, FullMoon, FirstQuarter, LastQuarter, WaxingGibbous, WaningGibbous, WaxingCrescent, WaningCrescent;

        /// <summary>
        /// Julian lunar phase time.
        /// </summary>
        public readonly double JulianNewMoon, JulianFullMoon, JulianFirstQuarter, JulianLastQuarter, JulianWaxingGibbous, JulianWaningGibbous, JulianWaxingCrescent, JulianWaningCrescent;
        
        public enum Phase { PreNew, New, WaxingCrescent, FirstQuarter, WaxingGibbous, Full, WaningGibbous, LastQuarter, WaningCrescent };
        
        // LunarPhase.cs should be easily incorprated into any other application; however you will want to change
        // these values to the appropriate names.  The names used here are keys for Strings resources.
        public static string[] PhaseName = new string[] { "New", "WaxingCrescent", "FirstQuarter",
                                                                                                            "WaxingGibbous", "Full", "WaningGibbous",
                                                                                                            "LastQuarter", "WaningCrescent" };
        public readonly DateTime[] PhaseTimes = new DateTime[8];
        public readonly double[] JulianTimes = new double[8];

        // Used as base date for the algorithm, and later to convert from Julian.  Why not midnight/Julian 0.5?
        private const double baseJulian = 2415020.75933;
        private DateTime baseTime = new DateTime(1900, 1, 1, 6, 13, 26);

        /// <summary>
        /// Calculate lunar phase times.  Times stored are UT.
        /// </summary>
        /// <param name="searchTime">A DateTime containing a time for which to report the nearest lunar phases</param>
        public LunarPhase(DateTime searchTime)
        {
            double[] k = new double[8], t = new double[8];
            double ks = Math.Floor((DateAsDouble(searchTime) - 1900) * 12.3685);
            for (int i = 0; i < 8; i++)
            {
                k[i] = ks + 0.125 * i;
                t[i] = k[i] / 1236.85;
            }

            double[] m = new double[8], mprime = new double[8], f = new double[8];
            // First calculate mean phase times
            for (int i = 0; i < 8; i++)
            {
                JulianTimes[i] = baseJulian  // 2415020.75933 = Julian Date for 1-1-1900 6:13:26 UT
                    + 29.53058868 * k[i]  // 29.53058868 = Days in lunar cycle
                    + 0.0001178 * Math.Pow(t[i], 2)
                    - 0.000000155 * Math.Pow(t[i], 3)
                    + 0.00033 * DtrSin(166.56 + 132.87 * t[i] - 0.009173 * Math.Pow(t[i], 2));
                m[i] = 359.2242  // 359.2242 = Sun's mean anomaly
                    + 29.10535608 * k[i]
                    - 0.0000333 * Math.Pow(t[i], 2)
                    - 0.00000347 * Math.Pow(t[i], 3);
                mprime[i] = 306.0253  // 306.0253 = Moon's mean anomaly
                    + 385.81691806 * k[i]
                    + 0.0107306 * Math.Pow(t[i], 2)
                    + 0.00001236 * Math.Pow(t[i], 3);
                f[i] = 21.2964  // 21.2964 = Moon's argument of latitude
                    + 390.67050646 * k[i]
                    - 0.0016528 * Math.Pow(t[i], 2)
                    - 0.00000239 * Math.Pow(t[i], 3);
            }

            // Calculate actual full and new moon times
            for (int i = 0; i <= 4; i += 4)
            {
                JulianTimes[i] += (0.1734 - 0.000393 * t[i]) * DtrSin(m[i])
                    + 0.0021 * DtrSin(2 * m[i])
                    - 0.4068 * DtrSin(mprime[i])
                    + 0.0161 * DtrSin(2 * mprime[i])
                    - 0.0004 * DtrSin(3 * mprime[i])
                    + 0.0104 * DtrSin(2 * f[i])
                    - 0.0051 * DtrSin(m[i] + mprime[i])
                    - 0.0074 * DtrSin(m[i] - mprime[i])
                    + 0.0004 * DtrSin(2 * f[i] + m[i])
                    - 0.0004 * DtrSin(2 * f[i] - m[i])
                    - 0.0006 * DtrSin(2 * f[i] + mprime[i])
                    + 0.0010 * DtrSin(2 * f[i] - mprime[i])
                    + 0.0005 * DtrSin(m[i] + 2 * mprime[i]);
            }

            // Calculate first qtr, last quarter, and waxing/waning gibbous and crescent times
            for (int i = 1; i < 8; i++)
            {
                if (i == 4) // Full moon; already calculated
                    continue;
                JulianTimes[i] += (0.1721 - 0.0004 * t[i]) * DtrSin(m[i])
                    + 0.0021 * DtrSin(2 * m[i])
                    - 0.6280 * DtrSin(mprime[i])
                    + 0.0089 * DtrSin(2 * mprime[i])
                    - 0.0004 * DtrSin(3 * mprime[i])
                    + 0.0079 * DtrSin(2 * f[i])
                    - 0.0119 * DtrSin(m[i] + mprime[i])
                    - 0.0047 * DtrSin(m[i] - mprime[i])
                    + 0.0003 * DtrSin(2 * f[i] + m[i])
                    - 0.0004 * DtrSin(2 * f[i] - m[i])
                    - 0.0006 * DtrSin(2 * f[i] + mprime[i])
                    + 0.0021 * DtrSin(2 * f[i] - mprime[i])
                    + 0.0003 * DtrSin(m[i] + 2 * mprime[i])
                    + 0.0004 * DtrSin(m[i] - 2 * mprime[i])
                    - 0.0003 * DtrSin(2 * m[i] + mprime[i]);
                if (i < 4)  // Waxing
                    JulianTimes[i] += 0.0028 - 0.0004 * DtrCos(m[i]) + 0.0003 * DtrCos(mprime[i]);
                else if (i > 4) // Waning
                    JulianTimes[i] += -0.0028 + 0.0004 * DtrCos(m[i]) - 0.0003 * DtrCos(mprime[i]);
            }

            for (int i = 0; i < 8; i++)
                PhaseTimes[i] = JulianToDateTime(JulianTimes[i]);
            
            NewMoon = PhaseTimes[0];
            WaxingCrescent = PhaseTimes[1];
            FirstQuarter = PhaseTimes[2];
            WaxingGibbous = PhaseTimes[3];
            FullMoon = PhaseTimes[4];
            WaningGibbous = PhaseTimes[5];
            LastQuarter = PhaseTimes[6];
            WaningCrescent = PhaseTimes[7];
            
            JulianNewMoon = JulianTimes[0];
            JulianWaxingCrescent = JulianTimes[1];
            JulianFirstQuarter = JulianTimes[2];
            JulianWaxingGibbous = JulianTimes[3];
            JulianFullMoon = JulianTimes[4];
            JulianWaningGibbous = JulianTimes[5];
            JulianLastQuarter = JulianTimes[6];
            JulianWaningCrescent = JulianTimes[7];
        }
        
        /// <summary>
        /// Returns the current lunar phase.
        /// </summary>
        /// <param name="when">UT DateTime</param>
        /// <returns></returns>
        public Phase CurrentPhase(DateTime when)
        {
            if (when.CompareTo(NewMoon) < 0)
                return Phase.PreNew;
            else if (when.CompareTo(NewMoon) >= 0 && when.CompareTo(WaxingCrescent) < 0)
                return Phase.New;
            else if (when.CompareTo(WaxingCrescent) >= 0 && when.CompareTo(FirstQuarter) < 0)
                return Phase.WaxingCrescent;
            else if (when.CompareTo(FirstQuarter) >= 0 && when.CompareTo(WaxingGibbous) < 0)
                return Phase.FirstQuarter;
            else if (when.CompareTo(WaxingGibbous) >= 0 && when.CompareTo(FullMoon) < 0)
                return Phase.WaxingGibbous;
            else if (when.CompareTo(FullMoon) >= 0 && when.CompareTo(WaningGibbous) < 0)
                return Phase.Full;
            else if (when.CompareTo(WaningGibbous) >= 0 && when.CompareTo(LastQuarter) < 0)
                return Phase.WaningGibbous;
            else if (when.CompareTo(LastQuarter) >= 0 && when.CompareTo(WaningCrescent) < 0)
                return Phase.LastQuarter;
            else
                return Phase.WaningCrescent;
        }
        
        public int PhaseNum(DateTime when)
        {
            if (when.CompareTo(NewMoon) < 0)
                return 0;   //return -1 original value gives bad error
            else if (when.CompareTo(NewMoon) >= 0 && when.CompareTo(WaxingCrescent) < 0)
                return 0;
            else if (when.CompareTo(WaxingCrescent) >= 0 && when.CompareTo(FirstQuarter) < 0)
                return 1;
            else if (when.CompareTo(FirstQuarter) >= 0 && when.CompareTo(WaxingGibbous) < 0)
                return 2;
            else if (when.CompareTo(WaxingGibbous) >= 0 && when.CompareTo(FullMoon) < 0)
                return 3;
            else if (when.CompareTo(FullMoon) >= 0 && when.CompareTo(WaningGibbous) < 0)
                return 4;
            else if (when.CompareTo(WaningGibbous) >= 0 && when.CompareTo(LastQuarter) < 0)
                return 5;
            else if (when.CompareTo(LastQuarter) >= 0 && when.CompareTo(WaningCrescent) < 0)
                return 6;
            else
                return 7;
        }

        /// <summary>
        /// Returns the date represented by a floating point value (in which the year is whole)
        /// </summary>
        private double DateAsDouble(DateTime when)
        {
            double daysInYear;
            if (DateTime.IsLeapYear(when.Year))
                daysInYear = 366;
            else
                daysInYear = 365;
            return when.Year + ((1 / daysInYear) * when.DayOfYear);
        }

        /// <summary>
        /// Convert a Julian date to DateTime.
        /// </summary>
        private DateTime JulianToDateTime(double julian)
        {
            return baseTime.AddDays(julian - baseJulian);
        }

        private double DtrSin(double x)
        {
            return Math.Sin(DegreesToRadians(x));
        }

        private double DtrCos(double x)
        {
            return Math.Cos(DegreesToRadians(x));
        }

        // Redundant within ChronosXP; but we'll keep it here for easy incorporation elsewhere.
        private double DegreesToRadians(double x)
        {
            return (x * Math.PI) / 180;
        }
    }


        #endregion

        #endregion