﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SkyQuery.Parser
{
    public class Constants
    {
        public const string AlgorithmBayesFactor = "BAYESFACTOR";
        public const string InclusionMethodMust = "MUST";
        public const string InclusionMethodMay = "MAY";
        public const string InclusionMethodNot = "NOT";

        public const string PointHintIdentifier = "POINT";
        public const string HtmIdHintIdentifier = "HTMID";
        public const string ZoneIdHintIdentifier = "ZONEID";
        public const string ErrorHintIdentifier = "ERROR";

        public const string DefaultCodeDatasetFunctionPrefix = "[SkyQuery_Code].[dbo]";
    }
}
