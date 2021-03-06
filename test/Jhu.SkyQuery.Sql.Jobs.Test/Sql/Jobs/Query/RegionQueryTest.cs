﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.Scheduler;
using Jhu.Graywulf.RemoteService;
using Jhu.Graywulf.Registry;

namespace Jhu.SkyQuery.Sql.Jobs.Query
{
    [TestClass]
    public class RegionQueryTest : SkyQueryTestBase
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            StartLogger();
            InitializeJobTests();
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            CleanupJobTests();
            StopLogger();
        }

        [TestMethod]
        [TestCategory("Query")]
        public void SimpleRegionQuery_NoHints_Test()
        {
            var sql = @"
SELECT objid, ra, dec INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll
REGION 'CIRCLE J2000 0 0 10'";

            RunQuery(sql);
        }

        [TestMethod]
        [TestCategory("Query")]
        public void SimpleRegionQuery_AllHints_Test()
        {
            var sql = @"
SELECT objid, ra, dec INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll WITH(POINT(ra, dec, cx, cy, cz), HTMID(htmid))
REGION 'CIRCLE J2000 0 0 10'";

            RunQuery(sql);
        }

        [TestMethod]
        [TestCategory("Query")]
        public void TableWithAliasTest()
        {
            var sql = @"
SELECT objid, ra, dec INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll a WITH (POINT(ra, dec), HTMID(htmid))
REGION 'CIRCLE J2000 0 0 10'";

            RunQuery(sql);
        }

        [TestMethod]
        [TestCategory("Query")]
        public void SingleTableWithRegioAndWhereTest()
        {
            var sql =
@"SELECT s.objid
INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll_NoZone AS s WITH(POINT(ra, dec), HTMID(htmid))
WHERE s.ra BETWEEN 0 AND 5 AND s.dec BETWEEN 0 AND 5
REGION 'CIRCLE J2000 0 0 60'";

            RunQuery(sql);
        }

        [TestMethod]
        [TestCategory("Query")]
        public void JoinedTableWithRegionQueryTest()
        {
            var sql =
@"SELECT s.objid, g.objid
INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll_NoZone AS s WITH(POINT(ra, dec), HTMID(htmid))
INNER JOIN TEST:SDSSDR7PhotoObjAll_NoZone AS g
    ON s.objID = g.objID
WHERE s.ra BETWEEN 0 AND 5 AND s.dec BETWEEN 0 AND 5
    AND g.ra BETWEEN 0 AND 5 AND g.dec BETWEEN 0 AND 5
REGION 'CIRCLE J2000 0 0 60'";

            RunQuery(sql);
        }

        [TestMethod]
        [TestCategory("Query")]
        public void NoCoordinatesQueryTest()
        {
            var sql = @"
SELECT objid, ra, dec INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll WITH (HTMID(htmid))
REGION 'CIRCLE J2000 0 0 10'";

            RunQuery(sql);
        }

        [TestMethod]
        [TestCategory("Query")]
        public void NoHtmQueryTest()
        {
            var sql = @"
SELECT objid, ra, dec INTO [$into]
FROM TEST:SDSSDR7PhotoObjAll WITH (POINT(ra, dec))
REGION 'CIRCLE J2000 0 0 10'";

            RunQuery(sql);
        }
    }
}
