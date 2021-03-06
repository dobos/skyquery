﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.Sql.Parsing;
using Jhu.Graywulf.Sql.NameResolution;
using Jhu.Graywulf.Sql.Schema;
using Jhu.Graywulf.Sql.Schema.SqlServer;

namespace Jhu.SkyQuery.Sql.Parsing
{
    [TestClass]
    public class SkyQueryNameResolverTest : SkyQueryTestBase
    {
        private SchemaManager CreateSchemaManager()
        {
            return new SqlServerSchemaManager();
        }

        protected new QuerySpecification Parse(string query)
        {
            var script = new SkyQueryParser().Execute<StatementBlock>(query);
            var statement = script.FindDescendantRecursive<Statement>();
            var select = statement.FindDescendant<SelectStatement>();
            var qs = select.QueryExpression.EnumerateQuerySpecifications().FirstOrDefault();

            var nr = new SkyQueryNameResolver()
            {
                Options = new Graywulf.Sql.Extensions.NameResolution.GraywulfSqlNameResolverOptions()
                {
                    DefaultTableDatasetName = Jhu.Graywulf.Test.Constants.TestDatasetName,
                    DefaultFunctionDatasetName = Jhu.Graywulf.Test.Constants.CodeDatasetName,
                    DefaultDataTypeDatasetName = Jhu.Graywulf.Test.Constants.CodeDatasetName,
                }
            };
            nr.SchemaManager = CreateSchemaManager();
            nr.Execute(script);

            return qs;
        }

        private string RenderQuery(QuerySpecification qs)
        {
            var qr = new Jhu.Graywulf.Sql.QueryRendering.SqlServer.SqlServerQueryRenderer()
            {
                Options = new Graywulf.Sql.QueryRendering.QueryRendererOptions()
                {
                    TableNameRendering = Graywulf.Sql.QueryRendering.NameRendering.FullyQualified,
                    TableAliasRendering = Graywulf.Sql.QueryRendering.AliasRendering.Default,
                    ColumnNameRendering = Graywulf.Sql.QueryRendering.NameRendering.FullyQualified,
                    ColumnAliasRendering = Graywulf.Sql.QueryRendering.AliasRendering.Always,
                    DataTypeNameRendering = Graywulf.Sql.QueryRendering.NameRendering.FullyQualified,
                }
            };

            var sw = new StringWriter();
            qr.Execute(sw, qs);
            return sw.ToString();
        }

        [TestMethod]
        public void SimpleQueryTest()
        {
            var sql = "SELECT objId, ra, dec FROM CatalogA";

            var qs = Parse(sql);
            var ts = qs.SourceTableReferences.Values.ToArray();

            Assert.AreEqual("CatalogA", ts[0].DatabaseObjectName);
        }

        [TestMethod]
        public void XMatchQueryTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1000) AS x";

            var qs = Parse(sql);
            var ts = qs.SourceTableReferences.Values.ToArray();
            var xm = qs.FindDescendantRecursive<BayesFactorXMatchTableSourceSpecification>();
            var xts = xm.EnumerateXMatchTableSpecifications().ToArray();

            Assert.AreEqual(3, ts.Length);

            Assert.AreEqual("[x]", ts[0].ToString());
            Assert.IsTrue(ts[0].IsComputed);

            Assert.AreEqual("[a]", ts[1].ToString());
            Assert.AreEqual("[b]", ts[2].ToString());
            Assert.AreEqual("[a]", xts[0].TableReference.ToString());
            Assert.AreEqual("[b]", xts[1].TableReference.ToString());
        }

        [TestMethod]
        public void XMatchQueryWithValidTableHintsTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(cx, cy, cz), HTMID(htmID)),
     MUST EXIST IN CatalogB b WITH(POINT(cx, cy, cz), HTMID(htmID)),
     LIMIT BAYESFACTOR TO 1000) AS x";

            var qs = Parse(sql);
            var ts = qs.SourceTableReferences.Values.ToArray();
            var xm = qs.FindDescendantRecursive<BayesFactorXMatchTableSourceSpecification>();
            var xts = xm.EnumerateXMatchTableSpecifications().ToArray();

            Assert.AreEqual("[a].[cx]", QueryRenderer.Execute(xts[0].Coordinates.XHintExpression));
            Assert.AreEqual("[b].[cx]", QueryRenderer.Execute(xts[1].Coordinates.XHintExpression));
            Assert.AreEqual("[a].[htmId]", QueryRenderer.Execute(xts[0].Coordinates.HtmIdHintExpression));
            Assert.AreEqual("[b].[htmId]", QueryRenderer.Execute(xts[1].Coordinates.HtmIdHintExpression));
        }

        // TODO: add test for zoneID, but need to modify catalog schema first

        [TestMethod]
        public void XMatchQueryWithInvalidTableHintsTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(b.cx, b.cy, b.cz)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1000) AS x";

            try
            {
                var qs = Parse(sql);
                Assert.Fail();
            }
            catch (NameResolverException)
            {
            }
        }


        [TestMethod]
        public void XMatchQueryWithHtmIdHintTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(cx, cy, cz), HTMID(htmID)),
     MUST EXIST IN CatalogB b WITH(POINT(cx, cy, cz), HTMID(htmID)),
     LIMIT BAYESFACTOR TO 1000) AS x";

            var qs = Parse(sql);
            var ts = qs.SourceTableReferences.Values.ToArray();
            var xm = qs.FindDescendantRecursive<BayesFactorXMatchTableSourceSpecification>();
            var xts = xm.EnumerateXMatchTableSpecifications().ToArray();

            Assert.AreEqual("[a].[cx]", QueryRenderer.Execute(xts[0].Coordinates.XHintExpression));
            Assert.AreEqual("[b].[cx]", QueryRenderer.Execute(xts[1].Coordinates.XHintExpression));
            Assert.AreEqual("[a].[htmId]", QueryRenderer.Execute(xts[0].Coordinates.HtmIdHintExpression));
            Assert.AreEqual("[b].[htmId]", QueryRenderer.Execute(xts[1].Coordinates.HtmIdHintExpression));
        }

        [TestMethod]
        public void XMatchQueryWithZoneIdHintTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(cx, cy, cz), ZONEID(zoneID)),
     MUST EXIST IN CatalogB b WITH(POINT(cx, cy, cz), ZONEID(zoneID)),
     LIMIT BAYESFACTOR TO 1000) AS x";

            var qs = Parse(sql);
            var ts = qs.SourceTableReferences.Values.ToArray();
            var xm = qs.FindDescendantRecursive<BayesFactorXMatchTableSourceSpecification>();
            var xts = xm.EnumerateXMatchTableSpecifications().ToArray();

            Assert.AreEqual("[a].[cx]", QueryRenderer.Execute(xts[0].Coordinates.XHintExpression));
            Assert.AreEqual("[b].[cx]", QueryRenderer.Execute(xts[1].Coordinates.XHintExpression));
            Assert.AreEqual("[a].[zoneId]", QueryRenderer.Execute(xts[0].Coordinates.ZoneIdHintExpression));
            Assert.AreEqual("[b].[zoneId]", QueryRenderer.Execute(xts[1].Coordinates.ZoneIdHintExpression));
        }

        [TestMethod]
        public void InclusionMethodTest()
        {
            var sql =
@"SELECT x.*
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     MAY EXIST IN CatalogC c WITH(POINT(c.cx, c.cy, c.cz)),
     NOT EXIST IN CatalogD d WITH(POINT(d.cx, d.cy, d.cz)),
     LIMIT BAYESFACTOR TO 1e3) AS x";

            var qs = Parse(sql);
            var ts = qs.SourceTableReferences.Values.ToArray();
            var xm = qs.FindDescendantRecursive<BayesFactorXMatchTableSourceSpecification>();
            var xts = xm.EnumerateXMatchTableSpecifications().ToArray();

            Assert.AreEqual(XMatchInclusionMethod.Must, xts[0].InclusionMethod);
            Assert.AreEqual(XMatchInclusionMethod.Must, xts[1].InclusionMethod);
            Assert.AreEqual(XMatchInclusionMethod.May, xts[2].InclusionMethod);
            Assert.AreEqual(XMatchInclusionMethod.Drop, xts[3].InclusionMethod);
        }

        [TestMethod]
        public void SelectStarTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.*
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1e3) AS x";

            var gt =
@"SELECT [a].[objId] AS [a_objId], [a].[ra] AS [a_ra], [a].[dec] AS [a_dec],
         [b].[objId] AS [b_objId], [b].[ra] AS [b_ra], [b].[dec] AS [b_dec],
         [x].[MatchID] AS [x_MatchID], [x].[RA] AS [x_RA], [x].[Dec] AS [x_Dec], [x].[Cx] AS [x_Cx], [x].[Cy] AS [x_Cy], [x].[Cz] AS [x_Cz], [x].[N] AS [x_N], [x].[A] AS [x_A], [x].[L] AS [x_L], [x].[Q] AS [x_Q], [x].[LogBF] AS [x_LogBF]
FROM XMATCH
    (MUST EXIST IN [SkyNode_Test].[dbo].[CatalogA] [a] WITH(POINT([a].[cx], [a].[cy], [a].[cz])),
     MUST EXIST IN [SkyNode_Test].[dbo].[CatalogB] [b] WITH(POINT([b].[cx], [b].[cy], [b].[cz])),
     LIMIT BAYESFACTOR TO 1e3) AS [x]";

            var qs = Parse(sql);

            var res = RenderQuery(qs);

            Assert.AreEqual(gt, res);
        }

    }
}
