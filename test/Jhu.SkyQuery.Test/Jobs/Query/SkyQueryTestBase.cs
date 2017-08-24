﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.Registry;
using Jhu.Graywulf.Test;
using Jhu.Graywulf.Jobs.Query;
using Jhu.Graywulf.Schema;
using Jhu.Graywulf.Schema.SqlServer;
using Jhu.SkyQuery.Jobs.Query;
using Jhu.SkyQuery.Parser;

namespace Jhu.SkyQuery.Jobs.Query.Test
{
    public class SkyQueryTestBase : SqlQueryTestBase
    {
        protected override UserDatabaseFactory CreateUserDatabaseFactory(FederationContext context)
        {
            return UserDatabaseFactory.Create(
                typeof(Jhu.Graywulf.CasJobs.CasJobsUserDatabaseFactory).AssemblyQualifiedName,
                context);
        }

        protected override QueryFactory CreateQueryFactory(RegistryContext context)
        {
            return QueryFactory.Create(
                typeof(SkyQueryQueryFactory).AssemblyQualifiedName,
                context);
        }

        protected SchemaManager CreateSchemaManager()
        {
            return new SqlServerSchemaManager();
        }

        protected XMatchSelectStatement Parse(string sql)
        {
            var p = new SkyQueryParser();
            var ss = p.Execute<XMatchSelectStatement>(sql);
            return ss;
        }

        protected void FinishQueryJob(Guid guid)
        {
            FinishQueryJob(guid, new TimeSpan(0, 5, 0));
        }

        protected void FinishQueryJob(Guid guid, TimeSpan timeout)
        {
            WaitJobComplete(guid, TimeSpan.FromSeconds(10), timeout);

            var ji = LoadJob(guid);

            if (ji.JobExecutionStatus == JobExecutionState.Failed)
            {
                throw new Exception(ji.ExceptionMessage);
            }

            Assert.AreEqual(JobExecutionState.Completed, ji.JobExecutionStatus);
        }
    }
}
