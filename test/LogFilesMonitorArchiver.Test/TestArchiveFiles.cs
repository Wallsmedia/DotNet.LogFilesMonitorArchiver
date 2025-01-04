// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Test - Log files monitor and archiver

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using DotNet.Host.LogFilesMonitorArchiver.Config;
using DotNet.Host.LogFilesMonitorArchiver.Processor;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogFilesMonitorArchiver.Test;


[TestClass]
public class TestArchiveFiles
{

    [TestMethod]
    public void Test_MoveToAchieveAndDeleteByDateAndNumberAutoOneMin()
    {
        ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
        TestInternals.CreateClearTestInputOutput(config);
        config.DelayArchiveInSecondsOnStartUp = 1;
        config.AutoTimerArchiveIntervalMin = 1000;

        var markerTime = DateTime.UtcNow;
        var time = markerTime;

        TestInternals.FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
        time = time - TimeSpan.FromHours(12);
        TestInternals.FillTestFiles(config, "archive-able_1{0}.xml", config.ArchiveRules[0].MoveToArchiveAfterReachingNumber, time);

        time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
        TestInternals.FillTestFiles(config, "archive-able_2{0}.xml", config.ArchiveRules[0].DeleteFromArchiveAfterReachingNumber, time);

        time = time - TimeSpan.FromDays(1);
        TestInternals.FillTestFiles(config, "archive-able_3{0}.xml", 2, time);


        time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
        TestInternals.FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
        time = time - TimeSpan.FromDays(1);
        TestInternals.FillTestFiles(config, "archive-able_5{0}.xml", 2, time);

        FilesArchiveHostedService farp = new FilesArchiveHostedService(NullLogger<FilesArchiveHostedService>.Instance, config);
        var task = farp.StartAsync();
        var res = task.Wait(TimeSpan.FromSeconds(10));
        Thread.Sleep(TimeSpan.FromSeconds(66));
        TestInternals.VerifySourceOlderByDate(config, markerTime);
        TestInternals.VerifySourceByNumber(config);
        TestInternals.VerifyArchiveByNumber(config);
        TestInternals.VerifySourceOlderByDate(config, markerTime);
    }

    [TestMethod]
    public void Test_MoveToAchieveAndDeleteByDateAndNumberAutoOneMinManual()
    {
        ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
        TestInternals.CreateClearTestInputOutput(config);
        config.DelayArchiveInSecondsOnStartUp = 1;
        config.AutoTimerArchiveIntervalMin = 1000;

        var markerTime = DateTime.UtcNow;
        var time = markerTime;

        TestInternals.FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
        time = time - TimeSpan.FromHours(12);
        TestInternals.FillTestFiles(config, "archive-able_1{0}.xml", config.ArchiveRules[0].MoveToArchiveAfterReachingNumber, time);

        time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
        TestInternals.FillTestFiles(config, "archive-able_2{0}.xml", config.ArchiveRules[0].DeleteFromArchiveAfterReachingNumber, time);

        time = time - TimeSpan.FromDays(1);
        TestInternals.FillTestFiles(config, "archive-able_3{0}.xml", 2, time);


        time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
        TestInternals.FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
        time = time - TimeSpan.FromDays(1);
        TestInternals.FillTestFiles(config, "archive-able_5{0}.xml", 2, time);

        FilesArchiveHostedService farp = new FilesArchiveHostedService(NullLogger<FilesArchiveHostedService>.Instance, config);
        var task = farp.StartAsync();
        var res = task.Wait(TimeSpan.FromSeconds(10));
        Thread.Sleep(TimeSpan.FromSeconds(10));
        Assert.IsTrue(res);

        Thread.Sleep(TimeSpan.FromSeconds(66));
        TestInternals.VerifySourceOlderByDate(config, markerTime);
        TestInternals.VerifySourceByNumber(config);
        TestInternals.VerifyArchiveByNumber(config);
        TestInternals.VerifySourceOlderByDate(config, markerTime);

        task = farp.StopAsync();
        res = task.Wait(TimeSpan.FromSeconds(10));
        Assert.IsTrue(res);
    }
}
