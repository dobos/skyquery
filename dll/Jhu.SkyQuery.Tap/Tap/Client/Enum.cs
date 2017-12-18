﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jhu.SkyQuery.Tap.Client
{
    public enum TapQueryLanguage
    {
        Adql,
        Pql,
        Sql,
        SkyQuery
    }

    public enum TapResultsFormat
    {
        VOTable,
        VOTableBinary,
        VOTableBinary2,
        VOTableFits,
        Json,
        Csv,
        Text
    }
    // application/x-votable+xml;serialization=TABLEDATA

    public enum TapJobAction
    {
        Run,
        Abort
    }

    public enum TapJobPhase
    {
        Unknown,
        Queued,
        Executing,
        Completed,
        Aborted,
        Error,
    }
}