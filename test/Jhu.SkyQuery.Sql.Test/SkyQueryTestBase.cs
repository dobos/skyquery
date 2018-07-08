﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.Graywulf.Registry;
using Jhu.Graywulf.Test;
using Jhu.Graywulf.Sql.Jobs.Query;
using Jhu.Graywulf.Sql.Schema;
using Jhu.Graywulf.Sql.Parsing;
using Jhu.SkyQuery.Sql.Jobs.Query;
using Jhu.SkyQuery.Sql.Parsing;

namespace Jhu.SkyQuery
{
    public class SkyQueryTestBase : SqlQueryTestBase
    {
        private RegistryContext registryContext;
        private FederationContext federationContext;
        private GraywulfSchemaManager schemaManager;

        public User RegistryUser
        {
            get
            {
                return null;
            }
        }

        public RegistryContext RegistryContext
        {
            get
            {
                if (registryContext == null)
                {
                    registryContext = ContextManager.Instance.CreateReadOnlyContext();
                    registryContext.ClusterReference.Name = ContextManager.Configuration.ClusterName;
                    registryContext.DomainReference.Name = ContextManager.Configuration.DomainName;
                    registryContext.FederationReference.Name = ContextManager.Configuration.FederationName;
                }

                return registryContext;
            }
        }

        public FederationContext FederationContext
        {
            get
            {
                if (federationContext == null)
                {
                    federationContext = new FederationContext(RegistryContext, RegistryUser);
                }

                return federationContext;
            }
        }

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
        
        protected virtual SchemaManager CreateSchemaManager()
        {
            if (schemaManager == null)
            {
                schemaManager = new GraywulfSchemaManager(FederationContext);
            }

            return schemaManager;
        }

        protected virtual SkyQuery.Sql.Parsing.StatementBlock Parse(string sql)
        {
            var p = new SkyQueryParser();
            var ss = p.Execute<SkyQuery.Sql.Parsing.StatementBlock>(sql);
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