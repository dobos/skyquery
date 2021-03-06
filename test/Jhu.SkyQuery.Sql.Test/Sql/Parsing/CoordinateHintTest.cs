﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.Sql.Parsing;

namespace Jhu.SkyQuery.Sql.Parsing
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CoordinateHintTest : SkyQueryTestBase
    {
        [TestMethod]
        public void SimpleCoordinateHintTest()
        {
            var sql =
@"SELECT ra, dec
FROM dummytable WITH(POINT(ra,dec), ERROR(1.0))
REGION 'CIRCLE J2000 10 10 0'";

            var ss = Parse(sql);
        }

        [TestMethod]
        public void XMatchCoordinateHintsTest()
        {
            var sql =
@"SELECT c1.ra, c1.dec, c2.ra, c2.dec
FROM 
    XMATCH
        (MUST EXIST IN d1:c1 WITH(POINT(c1.ra, c1.dec), ERROR(0.1)),
         MUST EXIST IN d2:c2 WITH(POINT(c2.ra, c2.dec), ERROR(c2.err, 0.1, 0.5)),
         LIMIT BAYESFACTOR TO 1000) AS x";

            var qs = Parse(sql);
        }

        [TestMethod]
        public void CoordinatesWithHtmIdTest()
        {
            var sql =
@"SELECT c1.ra, c1.dec, c2.ra, c2.dec
FROM 
    XMATCH
        (MUST EXIST IN d1:c1 WITH(POINT(c1.ra, c1.dec), ERROR(0.1), HTMID(c1.htmID)),
         MUST EXIST IN d2:c2 WITH(POINT(c2.ra, c2.dec), ERROR(c2.err, 0.1, 0.5), HTMID(c2.htmID)),
         LIMIT BAYESFACTOR TO 1000) AS x";

            var exp = Parse(sql);
        }

        [TestMethod]
        public void CoordinatesWithZoneIdTest()
        {

            var sql =
    @"SELECT c1.ra, c1.dec, c2.ra, c2.dec
FROM 
    XMATCH
        (MUST EXIST IN d1:c1 WITH(POINT(c1.ra, c1.dec), ERROR(0.1), ZONEID(c1.zoneID)),
         MUST EXIST IN d2:c2 WITH(POINT(c2.ra, c2.dec), ERROR(c2.err, 0.1, 0.5), ZONEID(c2.zoneID)),
         LIMIT BAYESFACTOR TO 1000) AS x";

            var qs = Parse(sql);
        }

        [TestMethod]
        public void SimpleRegionQueryTest()
        {
            var sql =
        @"SELECT TOP 100 a.objid, a.ra, a.dec
INTO PartitionedSqlQueryTest_SimpleQueryTest
FROM SDSSDR7:PhotoObjAll a WITH(POINT(ra, dec))
REGION CIRCLE(10, 20, 30)
";
            var qs = Parse(sql);
        }
    }
}
