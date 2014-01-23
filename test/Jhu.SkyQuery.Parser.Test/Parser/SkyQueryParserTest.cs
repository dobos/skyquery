﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.ParserLib;
using Jhu.SkyQuery.Parser;

namespace Jhu.SkyQuery.Parser.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class SkyQueryParserTest
    {
        protected QuerySpecification Parse(string query)
        {
            var p = new SkyQueryParser();
            return (QuerySpecification)((SelectStatement)p.Execute(query)).EnumerateQuerySpecifications().First();;
        }

        [TestMethod]
        public void XMatchTableHintTest()
        {
            var sql = "WITH(POINT(ra,dec), ERROR(1.0))";

            var p = new SkyQueryParser();
            var h = p.Execute(new XMatchHintClause(), sql);
        }

        [TestMethod]
        public void SimpleQueryTest()
        {
            var sql =
@"SELECT ra, dec FROM d:c";

            var qs = Parse(sql);
            Assert.IsNull(qs.FindDescendant<XMatchClause>());
        }

        [TestMethod]
        public void XMatchTableHintsTest()
        {
            var sql =
@"SELECT c1.ra, c1.dec, c2.ra, c2.dec
FROM d1:c1 WITH(POINT(c1.ra, c1.dec), ERROR(0.1)),
     d2:c2 WITH(POINT(c2.ra, c2.dec), ERROR(c2.err, 0.1, 0.5))
XMATCH BAYESFACTOR AS x
MUST EXIST c1
MUST EXIST c2
HAVING LIMIT 1e3
";

            var qs = Parse(sql);

            var ts = qs.EnumerateSourceTables(false).Cast<XMatchTableSource>().ToArray();

            Assert.AreEqual(2, ts.Length);

            Assert.AreEqual("POINT(c1.ra, c1.dec)", ts[0].Position.ToString());
            Assert.AreEqual("0.1", ts[0].ErrorExpression.ToString());
            Assert.IsTrue(ts[0].IsConstantError);

            Assert.AreEqual("POINT(c2.ra, c2.dec)", ts[1].Position.ToString());
            Assert.AreEqual("c2.err", ts[1].ErrorExpression.ToString());
            Assert.AreEqual("0.1", ts[1].MinErrorExpression.ToString());
            Assert.AreEqual("0.5", ts[1].MaxErrorExpression.ToString());
            Assert.IsFalse(ts[1].IsConstantError);
        }

        [TestMethod]
        public void TableValuedFunctionTest()
        {
            var sql = @"
SELECT htm.htmidstart, htm.htmidend
INTO SqlQueryTest_TableValuedFunctionJoinTest
FROM dbo.fHtmCoverCircleEq(100) AS htm
";

            var qs = Parse(sql);

            var ts = qs.EnumerateSourceTables(false).Cast<Jhu.Graywulf.SqlParser.FunctionTableSource>().ToArray();

            Assert.AreEqual(1, ts.Length);
        }

        [TestMethod]
        public void JoinedTableValuedFunctionTest()
        {
            var sql = @"
SELECT TOP 100 objid, ra, dec
INTO SqlQueryTest_TableValuedFunctionJoinTest
FROM dbo.fHtmCoverCircleEq (0, 0, 10) AS htm
INNER JOIN SDSSDR7:PhotoObj p
    ON p.htmid BETWEEN htm.htmidstart AND htm.htmidend";

            var qs = Parse(sql);

            var ts = qs.EnumerateSourceTables(false).ToArray();

            Assert.AreEqual(2, ts.Length);
        }

        [TestMethod]
        public void CrossJoinXMatchTestTest()
        {
            var sql = @"
SELECT m.ra, m.dec, x.ra, x.dec
INTO [$targettable]
FROM SDSSDR7:PhotoObjAll AS s WITH(POINT(s.ra, s.dec), ERROR(s.raErr, 0.05, 0.1))
CROSS JOIN MyCatalog m WITH(POINT(m.ra, m.dec), ERROR(0.2))
XMATCH BAYESFACTOR x
MUST EXIST s
MUST EXIST m
HAVING LIMIT 1e3
WHERE s.ra BETWEEN 0 AND 0.5 AND s.dec BETWEEN 0 AND 0.5";

            var qs = Parse(sql);

            var ts = qs.EnumerateSourceTables(false).ToArray();
            Assert.AreEqual(2, ts.Length);
        }

        [TestMethod]
        public void PartitionedQueryTest()
        {
            var sql =
        @"SELECT TOP 100 a.objid, a.ra, a.dec
INTO PartitionedSqlQueryTest_SimpleQueryTest
FROM SDSSDR7:PhotoObjAll a PARTITION ON a.objid
";

            var qs = Parse(sql);

            var ts = qs.EnumerateSourceTables(false).ToArray();
            Assert.AreEqual(1, ts.Length);
        }
    }
}
