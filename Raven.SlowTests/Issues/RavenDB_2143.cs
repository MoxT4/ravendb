using System;
using System.Threading;

using Raven.Client.Extensions;
using Raven.Database.Config;
using Raven.Database.Config.Settings;
using Raven.Tests.Helpers;
using Raven.Tests.Helpers.Util;

using Xunit;

namespace Raven.SlowTests.Issues
{
    public class RavenDB_2143 : RavenTestBase
    {
        protected override void ModifyConfiguration(ConfigurationModification configuration)
        {
            configuration.Modify(x => x.Tenants.FrequencyToCheckForIdle,new TimeSetting(3, TimeUnit.Seconds), "3");
            configuration.Modify(x => x.Tenants.MaxIdleTime, new TimeSetting(1, TimeUnit.Seconds), "1");
        }

        [Fact]
        public void Tenant_not_in_memory_idle_should_be_unloaded()
        {
            using (var server = GetNewServer(runInMemory: false))
            using (var store = NewRemoteDocumentStore(ravenDbServer: server, runInMemory: false))
            {
                var tenantRemovedEvent = new ManualResetEventSlim();
                server.Options.DatabaseLandlord.CleanupOccured += databaseId =>
                {
                    if (databaseId.Equals("testDB", StringComparison.InvariantCultureIgnoreCase))
                        tenantRemovedEvent.Set();
                };

                store.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists("testDB");
                using (var session = store.OpenSession("testDB"))
                {
                    session.Store(new {Foo = "Bar"});
                    session.SaveChanges();
                }


                //DB that is stored on H.D should *not* be unloaded
                Assert.True(tenantRemovedEvent.Wait(TimeSpan.FromSeconds(15)));
            }
        }

        [Fact]
        public void Tenant_in_memory_idle_should_not_be_unloaded()
        {
            using( var server = GetNewServer(runInMemory:true))
            using (var store = NewRemoteDocumentStore(ravenDbServer: server,runInMemory:true))
            {
                var tenantRemovedEvent = new ManualResetEventSlim();
                server.Options.DatabaseLandlord.CleanupOccured += databaseId =>
                {
                    if(databaseId.Equals("testDB",StringComparison.InvariantCultureIgnoreCase))
                        tenantRemovedEvent.Set();
                };

                store.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists("testDB");
                using (var session = store.OpenSession("testDB"))
                {
                    session.Store(new { Foo = "Bar" });
                    session.SaveChanges();
                }
                

                //DB that is stored in-memory should* be unloaded
                Assert.False(tenantRemovedEvent.Wait(TimeSpan.FromSeconds(15)));
            }			
        }
    }
}