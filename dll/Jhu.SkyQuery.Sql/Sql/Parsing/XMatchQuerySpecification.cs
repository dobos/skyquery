﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SkyQuery.Sql.Parsing
{
    public partial class XMatchQuerySpecification
    {

        public XMatchTableSourceSpecification XMatchTableSource
        {
            get { return this.FindDescendantRecursive<XMatchTableSourceSpecification>(); }
        }
    }
}