// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Test - Log files monitor and archiver

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DotNet.Host.LogFilesMonitorArchiver.DependencyInjection;
using DotNet.LogFilesMonitorArchiver;
using System.Linq;

namespace LogFilesMonitorArchiver.Test
{
    [TestClass]
    public class TestHostArchiveFiles
    {
        public static IConfiguration GetConfiguration(string name)
        {
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();

            string path = TestInternals.GetBasePath();
            path = Path.Combine(path, name);
            JsonConfigurationSource jsonConfigurationSource = new JsonConfigurationSource()
            {
                FileProvider = null,
                Optional = false,
                Path = path
            };
            jsonConfigurationSource.ResolveFileProvider();
            configBuilder.Add(jsonConfigurationSource);
            return configBuilder.Build();
        }

        [TestMethod]
        public void Test_HostingOfLogFilesMonitorArchiver()
        {

            string name = "TestConfiguration0.json";
            IConfiguration configuration = GetConfiguration(name);
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(configuration);

            serviceCollection.AddLogFilesMonitorArchiver();

            IServiceProvider Services = serviceCollection.BuildServiceProvider();

            var _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            // Setup proper test configuration 
            ArchiveProcessorConfig config = ((FilesArchiveProcessor)_hostedServices.First()).Configuration;
            config.AutoTimerIntervalEnabled = true;
            config.DelayArchiveInSecondsOnstartUp = 1;
            config.AutoTimerArchiveIntervalMin = 1000;

            var path = TestInternals.GetBasePath();
            foreach (var rule in config.ArchiveRules)
            {
                rule.SourcePath = Path.Combine(path, rule.SourcePath);
                rule.ArchivePath = Path.Combine(path, rule.ArchivePath);
            }

            config.AutoTimerIntervalEnabled = true;
            config.DelayArchiveInSecondsOnstartUp = 1;
            config.AutoTimerArchiveIntervalMin = 1000;

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            TestInternals.FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            TestInternals.FillTestFiles(config, "archive-able_1{0}.xml", config.ArchiveRules[0].MoveToArchiveAfterReachingFiles, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            TestInternals.FillTestFiles(config, "archive-able_2{0}.xml", config.ArchiveRules[0].DeleteFromArchiveAfterReachingFiles, time);

            time = time - TimeSpan.FromDays(1);
            TestInternals.FillTestFiles(config, "archive-able_3{0}.xml", 2, time);


            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
            TestInternals.FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            TestInternals.FillTestFiles(config, "archive-able_5{0}.xml", 2, time);

            // Start service via host services interface 

            foreach (var hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                hostedService.StartAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Thread.Sleep(TimeSpan.FromSeconds(66));
            TestInternals.VerifySourceOlderByDate(config, markerTime);
            TestInternals.VerifySourceByNumber(config);
            TestInternals.VerifyArchiveByNumber(config);
            TestInternals.VerifySourceDeletedOlderByDate(config, markerTime);

        }

    }
}

