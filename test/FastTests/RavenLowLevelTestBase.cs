﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Raven.Abstractions;
using Raven.Server.Config;
using Raven.Server.Documents;
using Raven.Server.Documents.Indexes;
using Raven.Server.Utils;
using Raven.Server.Utils.Metrics;

using Sparrow.Collections;

using Xunit;

namespace FastTests
{
    public abstract class RavenLowLevelTestBase : IDisposable
    {
        private readonly ConcurrentSet<string> _pathsToDelete = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        protected static void WaitForIndexMap(Index index, long etag)
        {
            var timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(15);
            Assert.True(SpinWait.SpinUntil(() => index.GetLastMappedEtagsForDebug().Values.Min() == etag, timeout));
        }

        protected DocumentDatabase CreateDocumentDatabase([CallerMemberName] string caller = null, bool runInMemory = true, string dataDirectory = null)
        {
            var name = caller ?? Guid.NewGuid().ToString("N");

            if (string.IsNullOrEmpty(dataDirectory))
                dataDirectory = NewDataPath(name);

            _pathsToDelete.Add(dataDirectory);

            var configuration = new RavenConfiguration();
            configuration.Initialize();
            configuration.Core.RunInMemory = runInMemory;
            configuration.Core.DataDirectory = dataDirectory;

            var documentDatabase = new DocumentDatabase(name, configuration, new MetricsScheduler());
            documentDatabase.Initialize();

            return documentDatabase;
        }

        protected string NewDataPath([CallerMemberName]string prefix = null, bool forceCreateDir = false)
        {
            var path = RavenTestHelper.NewDataPath(prefix, 9999, forceCreateDir);

            _pathsToDelete.Add(path);
            return path;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            SystemTime.UtcDateTime = null;

            GC.Collect(2);
            GC.WaitForPendingFinalizers();

            var exceptionAggregator = new ExceptionAggregator("Could not dispose test");

            RavenTestHelper.DeletePaths(_pathsToDelete, exceptionAggregator);

            exceptionAggregator.ThrowIfNeeded();
        }
    }
}