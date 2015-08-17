﻿using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.ParserLib;
using Jhu.Graywulf.SqlParser;
using Jhu.Graywulf.Schema;
using Jhu.Graywulf.Schema.SqlServer;
using Jhu.Graywulf.Registry;
using Jhu.SkyQuery.Jobs.Query;
using System.Reflection;

namespace Jhu.SkyQuery.Jobs.Query.Test
{
    [TestClass]
    public class BayesFactorXMatchQueryPartitionTest
    {
        private SchemaManager CreateSchemaManager()
        {
            return new SqlServerSchemaManager();
        }

        private QuerySpecification Parse(string query)
        {
            var p = new Jhu.SkyQuery.Parser.SkyQueryParser();
            var ss = (SelectStatement)p.Execute(query);

            var nr = new Jhu.SkyQuery.Parser.SkyQueryNameResolver();
            nr.DefaultTableDatasetName = Jhu.Graywulf.Test.Constants.TestDatasetName;
            nr.DefaultFunctionDatasetName = Jhu.Graywulf.Test.Constants.CodeDatasetName;
            nr.SchemaManager = CreateSchemaManager();
            nr.Execute(ss);

            return (QuerySpecification)ss.EnumerateQuerySpecifications().First(); ;
        }

        // --- GetPropagatedColumnList

        private string GetPropagatedColumnListTestHelper(string query, ColumnListInclude include)
        {
            using (var context = ContextManager.Instance.CreateContext(ConnectionMode.AutoOpen, TransactionMode.AutoCommit))
            {

                var sm = CreateSchemaManager();

                var xmq = new BayesFactorXMatchQuery(context);
                xmq.QueryString = query;
                xmq.QueryFactoryTypeName = Jhu.Graywulf.Util.TypeNameFormatter.ToUnversionedAssemblyQualifiedName(typeof(Jhu.SkyQuery.Jobs.Query.XMatchQueryFactory));
                xmq.DefaultDataset = (SqlServerDataset)sm.Datasets[Jhu.Graywulf.Test.Constants.TestDatasetName];
                xmq.TemporaryDataset = (SqlServerDataset)sm.Datasets[Jhu.Graywulf.Test.Constants.TestDatasetName];
                xmq.CodeDataset = (SqlServerDataset)sm.Datasets[Jhu.Graywulf.Test.Constants.TestDatasetName];

                var xmqp = new BayesFactorXMatchQueryPartition(xmq);
                xmqp.ID = 0;
                xmqp.InitializeQueryObject(context);

                var qs = xmqp.Query.SelectStatement.EnumerateQuerySpecifications().First();
                var fc = qs.FindDescendant<Jhu.Graywulf.SqlParser.FromClause>();
                var xmtables = qs.FindDescendantRecursive<Jhu.SkyQuery.Parser.XMatchTableSource>().EnumerateXMatchTableSpecifications().ToArray();
                xmqp.GenerateSteps(xmtables);

                var xmtstr = new List<TableReference>(xmtables.Select(ts => ts.TableReference));

                var m = xmqp.GetType().GetMethod("GetPropagatedColumnList", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(m);

                return (string)m.Invoke(xmqp, new object[] { xmtables[0], ColumnListType.ForSelectNoAlias, include, ColumnListNullType.Nothing, "tablealias" });

            }
        }

        [TestMethod]
        public void GetPropagatedColumnListTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1000) AS x";

            var res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.Referenced);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId], [tablealias].[_TEST_dbo_CatalogA_a_ra], [tablealias].[_TEST_dbo_CatalogA_a_dec], [tablealias].[_TEST_dbo_CatalogA_a_cx], [tablealias].[_TEST_dbo_CatalogA_a_cy], [tablealias].[_TEST_dbo_CatalogA_a_cz]", res);

            res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.PrimaryKey);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId]", res);

            res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.All);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId], [tablealias].[_TEST_dbo_CatalogA_a_ra], [tablealias].[_TEST_dbo_CatalogA_a_dec], [tablealias].[_TEST_dbo_CatalogA_a_cx], [tablealias].[_TEST_dbo_CatalogA_a_cy], [tablealias].[_TEST_dbo_CatalogA_a_cz]", res);
        }

        [TestMethod]
        public void GetPropagatedColumnListTest2()
        {
            var sql =
@"SELECT a.ra
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.ra, a.dec)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1e3) AS x";

            var res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.Referenced);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_ra], [tablealias].[_TEST_dbo_CatalogA_a_dec]", res);

            res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.PrimaryKey);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId]", res);

            res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.All);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId], [tablealias].[_TEST_dbo_CatalogA_a_ra], [tablealias].[_TEST_dbo_CatalogA_a_dec]", res);
        }

        [TestMethod]
        public void GetPropagatedColumnListWithAliasesTest()
        {
            var sql =
@"SELECT a.ra
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.ra, a.dec)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1e3) AS x";

            var res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.Referenced);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_ra], [tablealias].[_TEST_dbo_CatalogA_a_dec]", res);

            res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.PrimaryKey);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId]", res);

            res = GetPropagatedColumnListTestHelper(sql, ColumnListInclude.All);
            Assert.AreEqual("[tablealias].[_TEST_dbo_CatalogA_a_objId], [tablealias].[_TEST_dbo_CatalogA_a_ra], [tablealias].[_TEST_dbo_CatalogA_a_dec]", res);
        }

        // ---

        private string GetExecuteQueryTextTestHelper(string query)
        {
            var sm = CreateSchemaManager();

            var xmq = new BayesFactorXMatchQuery(null);
            xmq.QueryString = query;
            xmq.QueryFactoryTypeName = Jhu.Graywulf.Util.TypeNameFormatter.ToUnversionedAssemblyQualifiedName(typeof(Jhu.SkyQuery.Jobs.Query.XMatchQueryFactory));
            xmq.DefaultDataset = (SqlServerDataset)sm.Datasets[Jhu.Graywulf.Test.Constants.TestDatasetName];
            xmq.TemporaryDataset = (SqlServerDataset)sm.Datasets[Jhu.Graywulf.Test.Constants.TestDatasetName];
            xmq.CodeDataset = (SqlServerDataset)sm.Datasets[Jhu.Graywulf.Test.Constants.TestDatasetName];

            var xmqp = new BayesFactorXMatchQueryPartition(xmq);
            xmqp.ID = 0;
            xmqp.InitializeQueryObject(null);

            var qs = xmqp.Query.SelectStatement.EnumerateQuerySpecifications().First();
            var fc = qs.FindDescendant<Jhu.Graywulf.SqlParser.FromClause>();
            var xmtables = qs.FindDescendantRecursive<Jhu.SkyQuery.Parser.XMatchTableSource>().EnumerateXMatchTableSpecifications().ToArray();
            xmqp.GenerateSteps(xmtables);

            var xmtstr = new List<TableReference>(xmtables.Select(ts => ts.TableReference));

            var m = xmqp.GetType().GetMethod("GetExecuteQueryText", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m);

            return (string)m.Invoke(xmqp, null);
        }

        [TestMethod]
        public void GetOutputSelectQueryTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
b.objID, b.ra, b.dec,
x.ra, x.dec
FROM XMATCH
    (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
     MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
     LIMIT BAYESFACTOR TO 1e3) AS x";

            var res = GetExecuteQueryTextTestHelper(sql);

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogA_a_ra] AS [a_ra], [matchtable].[_TEST_dbo_CatalogA_a_dec] AS [a_dec],
[matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId], [matchtable].[_TEST_dbo_CatalogB_b_ra] AS [b_ra], [matchtable].[_TEST_dbo_CatalogB_b_dec] AS [b_dec],
[matchtable].[RA] AS [x_RA], [matchtable].[Dec] AS [x_Dec]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_1] AS [matchtable]", res);

        }

        [TestMethod]
        public void GetOutputSelectQuery_ThreeTablesTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         x.ra, x.dec
FROM XMATCH
     (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
      MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
      MUST EXIST IN CatalogC c WITH(POINT(c.cx, c.cy, c.cz)),
      LIMIT BAYESFACTOR TO 1e3) AS x";

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogA_a_ra] AS [a_ra], [matchtable].[_TEST_dbo_CatalogA_a_dec] AS [a_dec],
         [matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId], [matchtable].[_TEST_dbo_CatalogB_b_ra] AS [b_ra], [matchtable].[_TEST_dbo_CatalogB_b_dec] AS [b_dec],
         [matchtable].[RA] AS [x_RA], [matchtable].[Dec] AS [x_Dec]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_2] AS [matchtable]", GetExecuteQueryTextTestHelper(sql));
        }

        [TestMethod]
        public void GetOutputSelectQuery_CombinedJoinTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         c.objID, c.ra, c.dec,
         x.ra, x.dec
FROM XMATCH
     (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
      MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
      LIMIT BAYESFACTOR TO 1e3) AS x
CROSS JOIN CatalogC c";

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogA_a_ra] AS [a_ra], [matchtable].[_TEST_dbo_CatalogA_a_dec] AS [a_dec],
         [matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId], [matchtable].[_TEST_dbo_CatalogB_b_ra] AS [b_ra], [matchtable].[_TEST_dbo_CatalogB_b_dec] AS [b_dec],
         [c].[objId] AS [c_objId], [c].[ra] AS [c_ra], [c].[dec] AS [c_dec],
         [matchtable].[RA] AS [x_RA], [matchtable].[Dec] AS [x_Dec]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_1] AS [matchtable]
CROSS JOIN [SkyNode_Test].[dbo].[CatalogC] [c]", GetExecuteQueryTextTestHelper(sql));

        }

        [TestMethod]
        public void GetOutputSelectQuery_InnerJoinTest()
        {
            var sql =
@"SELECT a.objID, a.ra, a.dec,
         b.objID, b.ra, b.dec,
         c.objID, c.ra, c.dec,
         x.ra, x.dec
FROM XMATCH
     (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
      MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
      LIMIT BAYESFACTOR TO 1e3) AS x
INNER JOIN CatalogC c ON c.objId = a.objId";

            var res = GetExecuteQueryTextTestHelper(sql);

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogA_a_ra] AS [a_ra], [matchtable].[_TEST_dbo_CatalogA_a_dec] AS [a_dec],
         [matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId], [matchtable].[_TEST_dbo_CatalogB_b_ra] AS [b_ra], [matchtable].[_TEST_dbo_CatalogB_b_dec] AS [b_dec],
         [c].[objId] AS [c_objId], [c].[ra] AS [c_ra], [c].[dec] AS [c_dec],
         [matchtable].[RA] AS [x_RA], [matchtable].[Dec] AS [x_Dec]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_1] AS [matchtable]
INNER JOIN [SkyNode_Test].[dbo].[CatalogC] [c] ON [c].[objId] = [matchtable].[_TEST_dbo_CatalogA_a_objId]", res);
        }

        [TestMethod]
        public void GetOutputSelectQuery_WhereTest()
        {
            var sql =
@"SELECT a.objID, b.objID
FROM XMATCH
     (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
      MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
      LIMIT BAYESFACTOR TO 1e3) AS x
WHERE a.ra BETWEEN 1 AND 2";

            var res = GetExecuteQueryTextTestHelper(sql);

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_1] AS [matchtable]
WHERE [matchtable].[_TEST_dbo_CatalogA_a_ra] BETWEEN 1 AND 2", res);
        }

        [TestMethod]
        public void GetOutputSelectQuery_WhereTest2()
        {
            var sql =
@"SELECT a.objID, b.objID
FROM XMATCH
     (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
      MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
      LIMIT BAYESFACTOR TO 1e3) AS x,
     CatalogC c
WHERE c.ra BETWEEN 1 AND 2";

            var res = GetExecuteQueryTextTestHelper(sql);

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_1] AS [matchtable],
     [SkyNode_Test].[dbo].[CatalogC] [c]
WHERE [c].[ra] BETWEEN 1 AND 2", res);

        }

        [TestMethod]
        public void GetOutputSelectQuery_SubqueryTest()
        {
            var sql =
@"SELECT a.objID, b.objID, c.objID
FROM XMATCH
     (MUST EXIST IN CatalogA a WITH(POINT(a.cx, a.cy, a.cz)),
      MUST EXIST IN CatalogB b WITH(POINT(b.cx, b.cy, b.cz)),
      LIMIT BAYESFACTOR TO 1e3) AS x,
     (SELECT * FROM CatalogC) c
WHERE c.ra BETWEEN 1 AND 2";

            var res = GetExecuteQueryTextTestHelper(sql);

            Assert.AreEqual(
@"SELECT [matchtable].[_TEST_dbo_CatalogA_a_objId] AS [a_objId], [matchtable].[_TEST_dbo_CatalogB_b_objId] AS [b_objId], [c].[objId] AS [c_objId]
FROM [SkyNode_Test].[dbo].[skyquerytemp_0_Match_1] AS [matchtable],
     (SELECT [SkyNode_Test].[dbo].[CatalogC].[objId], [SkyNode_Test].[dbo].[CatalogC].[ra], [SkyNode_Test].[dbo].[CatalogC].[dec], [SkyNode_Test].[dbo].[CatalogC].[astroErr], [SkyNode_Test].[dbo].[CatalogC].[cx], [SkyNode_Test].[dbo].[CatalogC].[cy], [SkyNode_Test].[dbo].[CatalogC].[cz], [SkyNode_Test].[dbo].[CatalogC].[htmId], [SkyNode_Test].[dbo].[CatalogC].[zoneId], [SkyNode_Test].[dbo].[CatalogC].[mag_1], [SkyNode_Test].[dbo].[CatalogC].[mag_2], [SkyNode_Test].[dbo].[CatalogC].[mag_3] FROM [SkyNode_Test].[dbo].[CatalogC]) [c]
WHERE [c].[ra] BETWEEN 1 AND 2", res);
        }
    }
}
