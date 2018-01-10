// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Test - Log files monitor and archiver

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNet.LogFilesMonitorArchiver;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogFilesMonitorArchiver.Test
{

    [TestClass]
    public class TestArchiveFiles
    {

        [TestMethod]
        public void Test_MoveToAchiveByDate()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            TestInternals.FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            TestInternals.FillTestFiles(config, "archive-able_1{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_1{0}.xml", 2, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            TestInternals.FillTestFiles(config, "archive-able_2{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_2{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            TestInternals.FillTestFiles(config, "archive-able_3{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_3{0}.xml", 2, time);

            var task = farp.LaunchArchiveFilesAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);
            TestInternals.VerifySourceOlderByDate(config, markerTime);
            TestInternals.VerifyArchiveOlderByDate(config, markerTime);

            // test stop sync
            task = farp.StopAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

        }

        [TestMethod]
        public void Test_MoveToAchiveByDateManualStart()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config, false);

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            TestInternals.FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            TestInternals.FillTestFiles(config, "archive-able_1{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_1{0}.xml", 2, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            TestInternals.FillTestFiles(config, "archive-able_2{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_2{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            TestInternals.FillTestFiles(config, "archive-able_3{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_3{0}.xml", 2, time);


            var task = farp.StartAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            task = farp.LaunchArchiveFilesAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);
            TestInternals.VerifySourceOlderByDate(config, markerTime);
            TestInternals.VerifyArchiveOlderByDate(config, markerTime);

            task = farp.StopAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

        }


        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDate()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            TestInternals.FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            TestInternals.FillTestFiles(config, "archive-able_1{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_1{0}.xml", 2, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            TestInternals.FillTestFiles(config, "archive-able_2{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_2{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            TestInternals.FillTestFiles(config, "archive-able_3{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_3{0}.xml", 2, time);


            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
            TestInternals.FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_4{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            TestInternals.FillTestFiles(config, "archive-able_5{0}.xml", 2, time);
            TestInternals.FillTestFiles(config, "ignored-able_5{0}.xml", 2, time);

            var task = farp.LaunchArchiveFilesAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);
            TestInternals.VerifySourceOlderByDate(config, markerTime);
            TestInternals.VerifyArchiveOlderByDate(config, markerTime);

            task = farp.LaunchDeleteFromArchiveFilesAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            TestInternals.VerifySourceDeletedOlderByDate(config, markerTime);

        }

        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDateAndNumber()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);

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

            var task = farp.LaunchArchiveFilesAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            TestInternals.VerifySourceOlderByDate(config, markerTime);


            task = farp.LaunchDeleteFromArchiveFilesAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            TestInternals.VerifySourceByNumber(config);
            TestInternals.VerifyArchiveByNumber(config);
            TestInternals.VerifySourceDeletedOlderByDate(config, markerTime);

        }


        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDateAndNumberAutoOneMin()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
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

            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);
            Thread.Sleep(TimeSpan.FromSeconds(66));
            TestInternals.VerifySourceOlderByDate(config, markerTime);
            TestInternals.VerifySourceByNumber(config);
            TestInternals.VerifyArchiveByNumber(config);
            TestInternals.VerifySourceDeletedOlderByDate(config, markerTime);

        }

        [TestMethod]
        public void Test_NegativeExceptions()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;

            FilesArchiveProcessor farp = new FilesArchiveProcessor(config, false);

            // Should throw exception until it has been started
            Assert.ThrowsException<InvalidOperationException>(() => farp.LaunchArchiveFilesAsync());
            Assert.ThrowsException<InvalidOperationException>(() => farp.LaunchDeleteFromArchiveFilesAsync());

            var task = farp.StartAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            Assert.ThrowsException<InvalidOperationException>(() => farp.StartAsync());

            task = farp.StopAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            // Should throw exception until it has been started
            Assert.ThrowsException<InvalidOperationException>(() => farp.LaunchArchiveFilesAsync());
            Assert.ThrowsException<InvalidOperationException>(() => farp.LaunchDeleteFromArchiveFilesAsync());

            task = farp.StartAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            farp.Dispose();
            // Should throw exception until it has been started
            Assert.ThrowsException<InvalidOperationException>(() => farp.LaunchArchiveFilesAsync());
            Assert.ThrowsException<InvalidOperationException>(() => farp.LaunchDeleteFromArchiveFilesAsync());
        }

        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDateAndNumberAutoOneMinManual()
        {
            ArchiveProcessorConfig config = TestInternals.LoadConfiguration("TestConfiguration0.json");
            TestInternals.CreateClearTestInputOutput(config);
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

            FilesArchiveProcessor farp = new FilesArchiveProcessor(config, false);
            var task = farp.StartAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            Thread.Sleep(TimeSpan.FromSeconds(66));
            TestInternals.VerifySourceOlderByDate(config, markerTime);
            TestInternals.VerifySourceByNumber(config);
            TestInternals.VerifyArchiveByNumber(config);
            TestInternals.VerifySourceDeletedOlderByDate(config, markerTime);

            task = farp.StopAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

        }


    }
}
