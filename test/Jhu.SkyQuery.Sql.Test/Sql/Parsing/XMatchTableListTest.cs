﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jhu.SkyQuery.Sql.Parsing
{
    [TestClass]
    public class XMatchTableListTest
    {
        protected XMatchTableList Parse(string query)
        {
            var p = new SkyQueryParser();
            var xt = new XMatchTableList();
            return (XMatchTableList)p.Execute(xt, query);
        }

        [TestMethod]
        public void SingleTableTest()
        {
            Parse("MUST EXIST IN SDSSDR7:PhotoObjAll");
            Parse("MAY EXIST IN SDSSDR7:PhotoObjAll");
            Parse("NOT EXIST IN SDSSDR7:PhotoObjAll");
            Parse("MUST EXIST IN SDSSDR7:PhotoObjAll a");
            Parse("MUST EXIST IN SDSSDR7:PhotoObjAll AS a");
            Parse("MUST EXIST IN SDSSDR7:PhotoObjAll a WITH(POINT(ra, dec))");
        }

        [TestMethod]
        public void MultipleTablesTest()
        {
            Parse(@"MUST EXIST IN SDSSDR7:PhotoObjAll a WITH(POINT(ra, dec)),
                    MUST EXIST IN d2:c2 WITH(POINT(c2.ra, c2.dec), ERROR(c2.err, 0.1, 0.5))");
        }
    }
}
