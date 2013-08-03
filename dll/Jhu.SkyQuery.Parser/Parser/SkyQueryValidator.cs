﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jhu.Graywulf.ParserLib;
using Jhu.Graywulf.SqlParser;

namespace Jhu.SkyQuery.Parser
{
    public class SkyQueryValidator : SqlValidator
    {

        public SkyQueryValidator()
        {
            InitializeMembers();
        }

        private void InitializeMembers()
        {
        }

        public override void Execute(Graywulf.SqlParser.SelectStatement selectStatement)
        {
            base.Execute(selectStatement);

            // Make sure subqueries are not xmatch queries
            CheckSubqueries(selectStatement);
        }

        /// <summary>
        /// Makes sure query does not contain xmatch subqueries
        /// </summary>
        /// <param name="selectStatement"></param>
        protected void CheckXMatchSubqueries(Graywulf.SqlParser.SelectStatement selectStatement)
        {
            foreach (var qs in selectStatement.EnumerateQuerySpecifications())
            {
                foreach (var sq in qs.EnumerateSubqueries())
                {
                    if (sq.FindDescendantRecursive<XMatchClause>() != null)
                    {
                        throw CreateException(ExceptionMessages.XMatchSubqueryNotAllowed, sq);
                    }
                }
            }
        }

        /// <summary>
        /// Makes sure query does not reference invalid table sources in the xmatch clause
        /// </summary>
        /// <param name="selectStatement"></param>
        protected void CheckSubqueries(Graywulf.SqlParser.SelectStatement selectStatement)
        {
            var xmc = selectStatement.FindDescendantRecursive<XMatchClause>();
            if (xmc != null)
            {
                foreach (var xmts in xmc.EnumerateXMatchTableSpecifications())
                {
                    if (xmts.TableReference.IsSubquery)
                    {
                        throw CreateException(ExceptionMessages.SubqueryInXMatchNotAllowed, xmts);
                    }
                    else if (xmts.TableReference.IsUdf)
                    {
                        throw CreateException(ExceptionMessages.FunctionInXMatchNotAllowed, xmts);
                    }
                }
            }
        }
    }
}
