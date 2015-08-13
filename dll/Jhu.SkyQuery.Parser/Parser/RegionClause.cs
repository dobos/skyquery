﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jhu.Graywulf.ParserLib;
using Jhu.Graywulf.SqlParser;

namespace Jhu.SkyQuery.Parser
{
    public partial class RegionClause : Jhu.Graywulf.ParserLib.Node, ICloneable
    {
        #region Properties

        public bool IsUri
        {
            get
            {
                return (FindDescendantRecursive<StringConstant>() != null) &&
                    Uri.IsWellFormedUriString(RegionString, UriKind.Absolute);
            }
        }

        public bool IsString
        {
            get
            {
                return (FindDescendantRecursive<StringConstant>() != null);
            }
        }

        public Uri RegionUri
        {
            get
            {
                return new Uri(RegionString, UriKind.Absolute);
            }
        }

        public string RegionString
        {
            get
            {
                return FindDescendant<StringConstant>().TrimmedValue;
            }
            set
            {
                FindDescendant<StringConstant>().TrimmedValue = value;
            }
        }

        #endregion
    }
}
