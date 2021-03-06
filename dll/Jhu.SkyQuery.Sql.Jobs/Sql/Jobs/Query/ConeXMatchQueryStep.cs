﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using gw = Jhu.Graywulf.Registry;
using Jhu.Graywulf.Sql.Schema;
using Jhu.SkyQuery.Sql.Parsing;

namespace Jhu.SkyQuery.Sql.Jobs.Query
{
    [Serializable]
	public class ConeXMatchQueryStep : XMatchQueryStep
	{
        public ConeXMatchQueryStep()
            : base()
        {
            InitializeMembers(new StreamingContext());
        }

        public ConeXMatchQueryStep(XMatchQueryPartition queryPartition, gw.RegistryContext context)
            : base(queryPartition, context)
        {
            InitializeMembers(new StreamingContext());
        }

        [OnDeserializing]
        private void InitializeMembers(StreamingContext context)
        {
        }
	}
}
