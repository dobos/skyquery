﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jhu.Graywulf.ParserLib;
using Jhu.Graywulf.SqlParser;
using Jhu.Graywulf.Schema;
using Jhu.Graywulf.Schema.SqlServer;
using Jhu.Graywulf.SqlCodeGen.SqlServer;

namespace Jhu.SkyQuery.Parser
{
    // Implements function to get coordinates and coordinate
    // errors applying to a table from the query or from the metadata
    public class TableCoordinates
    {
        #region Private member varibles

        private SimpleTableSource table;

        private TableHint pointHint;
        private Expression[] pointHintArguments;
        private TableHint htmIdHint;
        private Expression[] htmIdHintArguments;
        private TableHint zoneIdHint;
        private Expression[] zoneIdHintArguments;
        private TableHint errorHint;
        private Expression[] errorHintArguments;

        private bool isEqSpecified;
        private bool isCartesianSpecified;
        private bool isHtmIdSpecified;
        private bool isZoneIdSpecified;
        private bool isErrorSpecified;
        private bool isErrorLimitsSpecified;

        private int eqIndex;
        private int cartesianIndex;

        #endregion
        #region Properties

        public SimpleTableSource Table
        {
            get { return table; }
        }

        /// <summary>
        /// Placeholder for opting out from region constraint
        /// </summary>
        public bool IsNoRegion
        {
            get
            {
                return !isCartesianSpecified && !isEqSpecified && !isHtmIdSpecified &&
                  !IsEqColumnsAvailable && !IsCartesianColumnsAvailable;
            }
        }

        public bool IsEqColumnsAvailable
        {
            get
            {
                return 
                    table.TableReference != null &&
                    table.TableReference.TableOrView != null &&
                    table.TableReference.TableOrView.Columns.ContainsKey("ra") &&
                    table.TableReference.TableOrView.Columns.ContainsKey("dec");
            }
        }

        public bool IsEqHintSpecified
        {
            get { return isEqSpecified; }
        }

        public bool IsCartesianColumnsAvailable
        {
            get
            {
                return
                    table.TableReference != null &&
                    table.TableReference.TableOrView != null &&
                    table.TableReference.TableOrView.Columns.ContainsKey("cx") &&
                    table.TableReference.TableOrView.Columns.ContainsKey("cy") &&
                    table.TableReference.TableOrView.Columns.ContainsKey("cz");
            }
        }

        public bool IsCartesianHintSpecified
        {
            get { return isCartesianSpecified; }
        }

        public Expression RAHintExpression
        {
            get
            {
                return pointHintArguments[eqIndex];
            }
        }

        public Expression RAColumnExpression
        {
            get
            {
                return CreateColumnExpression("ra");
            }
        }

        public Expression DecHintExpression
        {
            get
            {
                return pointHintArguments[eqIndex + 1];
            }
        }

        public Expression DecColumnExpression
        {
            get
            {
                return CreateColumnExpression("dec");
            }
        }

        public Expression XHintExpression
        {
            get
            {
                return pointHintArguments[cartesianIndex];
            }
        }

        public Expression XColumnExpression
        {
            get
            {
                return CreateColumnExpression("cx");
            }
        }

        public Expression YHintExpression
        {
            get
            {
                return pointHintArguments[cartesianIndex + 1];
            }
        }

        public Expression YColumnExpression
        {
            get
            {
                return CreateColumnExpression("cy");
            }
        }

        public Expression ZHintExpression
        {
            get
            {
                return pointHintArguments[cartesianIndex + 2];
            }
        }

        public Expression ZColumnExpression
        {
            get
            {
                return CreateColumnExpression("cz");
            }
        }

        public bool IsHtmIdColumnAvailable
        {
            get
            {
                return
                    table.TableReference != null &&
                    table.TableReference.TableOrView != null &&
                    table.TableReference.TableOrView.Columns.ContainsKey("htmid");
            }
        }

        public bool IsHtmIdHintSpecified
        {
            get { return isHtmIdSpecified; }
        }

        public Expression HtmIdHintExpression
        {
            get
            {
                return htmIdHintArguments[0];
            }
        }

        public Expression HtmIdColumnExpression
        {
            get
            {
                return CreateColumnExpression("htmid");
            }
        }

        public ColumnReference HtmIdColumnReference
        {
            get
            {
                return HtmIdHintExpression.FindDescendant<AnyVariable>().FindDescendant<ColumnIdentifier>().ColumnReference;
            }
        }

        public bool IsZoneIdColumnAvailable
        {
            get
            {
                return
                    table.TableReference != null &&
                    table.TableReference.TableOrView != null &&
                    table.TableReference.TableOrView.Columns.ContainsKey("zoneid");
            }
        }

        public bool IsZoneIdHintSpecified
        {
            get { return isZoneIdSpecified; }
        }

        public Expression ZoneIdHintExpression
        {
            get
            {
                return zoneIdHintArguments[0];
            }
        }

        public Expression ZoneIDColumnExpression
        {
            get
            {
                return CreateColumnExpression("zoneid");
            }
        }

        public ColumnReference ZoneIdColumnReference
        {
            get
            {
                return zoneIdHintArguments[0].FindDescendant<AnyVariable>().FindDescendant<ColumnIdentifier>().ColumnReference;
            }
        }

        public bool IsErrorHintSpecified
        {
            get { return isErrorSpecified; }
        }

        public bool IsConstantError
        {
            get
            {
                return !IsErrorHintSpecified || errorHintArguments.Length == 1 && errorHintArguments[0].IsConstantNumber;
            }
        }


        public bool IsErrorLimitsHintSpecified
        {
            get { return isErrorLimitsSpecified; }
        }

        public Expression ErrorHintExpression
        {
            get
            {
                return errorHintArguments[0];
            }
        }

        public Expression ErrorHintMinExpression
        {
            get
            {
                return errorHintArguments[1];
            }
        }

        public Expression ErrorHintMaxExpression
        {
            get
            {
                return errorHintArguments[2];
            }
        }

        #endregion
        #region Constructors and initializers

        public TableCoordinates(SimpleTableSource table)
        {
            InitializeMembers();

            this.table = table;
            InterpretTableHints();
        }

        private void InitializeMembers()
        {
            this.table = null;

            this.pointHint = null;
            this.pointHintArguments = null;
            this.htmIdHint = null;
            this.htmIdHintArguments = null;
            this.zoneIdHint = null;
            this.zoneIdHintArguments = null;
            this.errorHint = null;
            this.errorHintArguments = null;

            this.isEqSpecified = false;
            this.isCartesianSpecified = false;
            this.isHtmIdSpecified = false;
            this.isZoneIdSpecified = false;
            this.isErrorSpecified = false;
            this.isErrorLimitsSpecified = false;

            this.eqIndex = -1;
            this.cartesianIndex = -1;
        }

        #endregion
        #region Hint interpretation functions

        private void InterpretTableHints()
        {
            var hints = table.FindDescendant<TableHintClause>();

            if (hints != null)
            {
                var hintlist = hints.FindDescendant<TableHintList>();

                foreach (var hint in hintlist.EnumerateDescendantsRecursive<TableHint>())
                {
                    switch (hint.Identifier.Value.ToUpperInvariant())
                    {
                        case Constants.PointHintIdentifier:
                            InterpretPointHint(hint);
                            break;
                        case Constants.HtmIdHintIdentifier:
                            InterpretHtmIdHint(hint);
                            break;
                        case Constants.ZoneIdHintIdentifier:
                            InterpretZoneIdHint(hint);
                            break;
                        case Constants.ErrorHintIdentifier:
                            InterpretErrorHint(hint);
                            break;
                        default:
                            // Nothing to do with unknown hints
                            break;
                    }
                }
            }
        }

        private void InterpretPointHint(TableHint hint)
        {
            pointHint = hint;
            pointHintArguments = GetHintArguments(hint);

            if (pointHintArguments.Length == 2)
            {
                isEqSpecified = true;
                isCartesianSpecified = false;
                eqIndex = 0;
            }
            else if (pointHintArguments.Length == 3)
            {
                isEqSpecified = false;
                isCartesianSpecified = true;
                cartesianIndex = 0;
            }
            else if (pointHintArguments.Length == 5)
            {
                isEqSpecified = true;
                isCartesianSpecified = true;
                eqIndex = 0;
                cartesianIndex = 2;
            }
            else
            {
                throw CreateException(ExceptionMessages.InvalidPointFormat);
            }
        }

        private void InterpretHtmIdHint(TableHint hint)
        {
            htmIdHint = hint;
            htmIdHintArguments = GetHintArguments(hint);

            if (htmIdHintArguments.Length != 1)
            {
                throw CreateException(ExceptionMessages.InvalidHtmIdFormat);
            }

            isHtmIdSpecified = true;

            if (!htmIdHintArguments[0].IsSingleColumn)
            {
                throw CreateException(ExceptionMessages.HtmIdIsNotSingleColumn);
            }
        }

        private void InterpretZoneIdHint(TableHint hint)
        {
            zoneIdHint = hint;
            zoneIdHintArguments = GetHintArguments(hint);

            if (zoneIdHintArguments.Length != 1)
            {
                throw CreateException(ExceptionMessages.InvalidZoneIdFormat);
            }

            isZoneIdSpecified = true;

            if (!ZoneIdHintExpression.IsSingleColumn)
            {
                throw CreateException(ExceptionMessages.ZoneIdIsNotSingleColumn);
            }
        }

        private void InterpretErrorHint(TableHint hint)
        {
            errorHint = hint;
            errorHintArguments = GetHintArguments(hint);

            if (errorHintArguments.Length == 1)
            {
                isErrorSpecified = true;
                isErrorLimitsSpecified = false;
            }
            else if (errorHintArguments.Length == 3)
            {
                isErrorSpecified = true;
                isErrorLimitsSpecified = true;
            }
            else
            {
                throw CreateException(ExceptionMessages.InvalidErrorFormat);
            }
        }

        private Expression[] GetHintArguments(TableHint hint)
        {
            return hint.FindDescendant<FunctionArguments>()
                       .FindDescendant<ArgumentList>()
                       .EnumerateDescendants<Argument>()
                       .Select(a => a.Expression)
                       .ToArray();
        }

        #endregion
        #region IndexSelectorFunctions

        /// <summary>
        /// Attempt to find an index that has an HTM ID in is as the first key column
        /// </summary>
        /// <returns></returns>
        public Index FindHtmIndex()
        {
            return FindIndexWithFirstKey(HtmIdColumnReference.ColumnName);
        }

        public Index FindZoneIndex()
        {
            return FindIndexWithFirstKey(ZoneIdColumnReference.ColumnName);
        }

        private Index FindIndexWithFirstKey(string columnName)
        {
            if (table.TableReference.DatabaseObject == null)
            {
                throw new InvalidOperationException(ExceptionMessages.QueryNamesNotResolved);
            }

            var t = (TableOrView)table.TableReference.DatabaseObject;

            foreach (var idx in t.Indexes.Values)
            {
                // TODO: modify this once columns are also stored by ordinal index and not just by name
                var col = idx.Columns.Values.Where(c => !c.IsIncluded).OrderBy(c => c.KeyOrdinal).FirstOrDefault();

                if (SqlServerSchemaManager.Comparer.Compare(columnName, col.Name) == 0)
                {
                    return idx;
                }
            }

            return null;
        }

        #endregion

        private Expression CreateColumnExpression(string column)
        {
            var cr = new ColumnReference(table.TableReference, table.TableReference.TableOrView.Columns[column]);
            return Expression.Create(cr);
        }

        private ValidatorException CreateException(string message)
        {
            return new ValidatorException(message);
        }
    }
}
