﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections;
using Microsoft.SqlServer.Server;

namespace Jhu.SkyQuery.SqlClrLib
{
    public partial class UserDefinedFunctions
    {
        private struct ZoneDef
        {
            public int ZoneID;
            public double DecMin;
            public double DecMax;
        }

        [SqlFunction(
            Name = "skyquery.ZoneIDFromDec",
            IsDeterministic = true, IsPrecise = false)]
        public static SqlInt32 ZoneIDFromDec(SqlDouble dec, SqlDouble zoneHeight)
        {
            return new SqlInt32((int)Math.Floor((dec.Value + 90.0) / zoneHeight.Value));
        }

        [SqlFunction(
            Name = "skyquery.ZoneIDFromZ",
            IsDeterministic = true, IsPrecise = false)]
        public static SqlInt32 ZoneIDFromZ(SqlDouble cz, SqlDouble zoneHeight)
        {
            double z = cz.Value;
            double dec;

            if (z >= 1)
            {
                dec = 90;
            }
            else if (z <= -1)
            {
                dec = -90;
            }
            else
            {
                dec = Math.Asin(z) * Constants.Radian2Degree;
            }

            return new SqlInt32((int)Math.Floor((dec + 90.0) / zoneHeight.Value));
        }

        [SqlFunction(
            Name = "skyquery.GetZones",
            DataAccess = DataAccessKind.None, IsDeterministic = true, IsPrecise = false,
            SystemDataAccess = SystemDataAccessKind.None,
            FillRowMethodName = "CalculateZones_Fill",
            TableDefinition = "ZoneID int, DecMin float, DecMax float")]
        public static IEnumerable GetZones(double zoneHeight, double theta)
        {
            int minzone = 0;
            int maxzone = (int)Math.Floor(180.0 / zoneHeight);

            while (minzone <= maxzone)
            {
                double zonedec = minzone * zoneHeight - 90;

                ZoneDef zd = new ZoneDef();
                zd.ZoneID = minzone;
                zd.DecMin = zonedec;
                zd.DecMax = zonedec + zoneHeight;

                yield return zd;

                minzone++;
            }
        }

        public static void CalculateZones_Fill(object o, out int zoneID, out double decMin, out double decMax)
        {
            ZoneDef zd = (ZoneDef)o;

            zoneID = zd.ZoneID;
            decMin = zd.DecMin;
            decMax = zd.DecMax;
        }

        /// <summary>
        /// Calculates the RA range belonging to a circle with the
        /// radius of theta centered at Dec.
        /// </summary>
        /// <param name="theta">Search radius in degrees</param>
        /// <param name="decMin">Min declination in degrees</param>
        /// <param name="decMax">Max declination in degrees</param>
        /// <param name="zoneHeight">Zone height in degrees</param>
        /// <returns>
        /// See Eq. 19 in http://research.microsoft.com/pubs/64524/tr-2006-52.pdf
        /// </returns>
        [SqlFunction(Name = "skyquery.Alpha")]
        public static double Alpha(double theta, double decMin, double decMax, double zoneHeight)
        {
            double dec;

            // Find longer circle of the zone an buffer by 1 per cent
            if (Math.Abs(decMax) < Math.Abs(decMin))
            {
                dec = decMin - zoneHeight / 100;
            }
            else
            {
                dec = decMax + zoneHeight / 100;
            }

            if (Math.Abs(dec) + theta > 89.9)
            {
                return 180; // hack
            }
            else
            {
                var a = Math.Sin(Constants.Radians * theta);
                var b = Math.Abs(Math.Cos(Constants.Radians * (dec - theta)) * Math.Cos(Constants.Radians * (dec + theta)));

                return Constants.Degrees * Math.Abs(Math.Atan(a / Math.Sqrt(b)));
            }
        }
    }
}