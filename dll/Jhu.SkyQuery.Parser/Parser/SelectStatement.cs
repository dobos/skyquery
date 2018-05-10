﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jhu.Graywulf.ParserLib;
using Jhu.Graywulf.SqlParser;

namespace Jhu.SkyQuery.Parser
{
    public partial class SelectStatement
    {
        public SelectStatement(Jhu.Graywulf.SqlParser.SelectStatement old)
            : base(old)
        {
        }

        public override Node Exchange()
        {
            // Look for descentant nodes in the parsing tree to determine
            // query type. An XMathTableSource means it's a cross-match query.
            // If only a RegionClause is present, it's a simpler region query.

            if (FindDescendantRecursive<XMatchTableSource>() != null)
            {
                var xms = new XMatchSelectStatement(this);
                return xms;
            }
            else if (FindDescendantRecursive<RegionClause>() != null)
            {
                var rs = new RegionSelectStatement(this);
                return rs;
            }
            else
            {
                return base.Exchange();
            }
        }
    }
}