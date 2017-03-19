// \\     |/\  /||
//  \\ \\ |/ \/ ||
//   \//\\/|  \ || 
// Copyright © Alexander Paskhin 2013-2017. All rights reserved.
// Wallsmedia LTD 2015-2017:{Alexander Paskhin}
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Test - Log files monitor and archiver

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNet.LogFilesMonitorArchiver;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;
using System.Threading;

namespace LogFilesMonitorArchiver.Test
{
    [TestClass]
    public class TestArchiveFiles
    {
        ArchiveProcessorConfig LoadConfiguration(string name)
        {
            ArchiveProcessorConfig config = null;
            string path = GetBasePath();
            path = Path.Combine(path, name);
            var build = new ConfigurationBuilder().AddJsonFile(path, false);
            var cfg = build.Build();
            config = cfg.Get<ArchiveProcessorConfig>();
            path = GetBasePath();
            foreach (var rule in config.ArchiveRules)
            {
                rule.SourcePath = Path.Combine(path, rule.SourcePath);
                rule.ArchivePath = Path.Combine(path, rule.ArchivePath);
            }
            return config;
        }

        private string GetBasePath()
        {
            var assembly = GetType().GetTypeInfo().Assembly;
            return Path.GetDirectoryName(assembly.Location);
        }

        [TestMethod]
        public void Test_MoveToAchiveByDate()
        {
            ArchiveProcessorConfig config = LoadConfiguration("TestConfiguration0.json");
            CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            FillTestFiles(config, "archive-able_1{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_1{0}.xml", 2, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_2{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_2{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_3{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_3{0}.xml", 2, time);

            var task = farp.LunchArchiveFilesAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);
            VerifySourceOlderByDate(config, markerTime);
            VerifyArchiveOlderByDate(config, markerTime);
        }

        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDate()
        {
            ArchiveProcessorConfig config = LoadConfiguration("TestConfiguration0.json");
            CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            FillTestFiles(config, "archive-able_1{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_1{0}.xml", 2, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_2{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_2{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_3{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_3{0}.xml", 2, time);


            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_4{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_5{0}.xml", 2, time);
            FillTestFiles(config, "ignored-able_5{0}.xml", 2, time);

            var task = farp.LunchArchiveFilesAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);
            VerifySourceOlderByDate(config, markerTime);
            VerifyArchiveOlderByDate(config, markerTime);

            task = farp.LunchDeleteFromArchiveFilesAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            VerifySourceDeletedOlderByDate(config, markerTime);

        }

        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDateAndNumber()
        {
            ArchiveProcessorConfig config = LoadConfiguration("TestConfiguration0.json");
            CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = false;
            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            FillTestFiles(config, "archive-able_1{0}.xml", config.ArchiveRules[0].MoveToArchiveAfterReachingFiles, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_2{0}.xml", config.ArchiveRules[0].DeleteFromArchiveAfterReachingFiles, time);

            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_3{0}.xml", 2, time);


            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_5{0}.xml", 2, time);

            var task = farp.LunchArchiveFilesAsync();
            var res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            VerifySourceOlderByDate(config, markerTime);


            task = farp.LunchDeleteFromArchiveFilesAsync();
            res = task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(res);

            VerifySourceByNumber(config, markerTime);
            VerifyArchiveByNumber(config, markerTime);
            VerifySourceDeletedOlderByDate(config, markerTime);

        }


        [TestMethod]
        public void Test_MoveToAchiveAndDeleteByDateAndNumberAutoOneMin()
        {
            ArchiveProcessorConfig config = LoadConfiguration("TestConfiguration0.json");
            CreateClearTestInputOutput(config);
            config.AutoTimerIntervalEnabled = true;
            config.DelayArchiveInSecondsOnstartUp = 1;
            config.AutoTimerArchiveIntervalMin = 1000;

            var markerTime = DateTime.UtcNow;
            var time = markerTime;

            FillTestFiles(config, "archive-able_0{0}.xml", 2, time);
            time = time - TimeSpan.FromHours(12);
            FillTestFiles(config, "archive-able_1{0}.xml", config.ArchiveRules[0].MoveToArchiveAfterReachingFiles, time);

            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].MoveToArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_2{0}.xml", config.ArchiveRules[0].DeleteFromArchiveAfterReachingFiles, time);

            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_3{0}.xml", 2, time);


            time = markerTime - TimeSpan.FromDays(config.ArchiveRules[0].DeleteFromArchiveOlderThanDays);
            FillTestFiles(config, "archive-able_4{0}.xml", 2, time);
            time = time - TimeSpan.FromDays(1);
            FillTestFiles(config, "archive-able_5{0}.xml", 2, time);

            FilesArchiveProcessor farp = new FilesArchiveProcessor(config);
            Thread.Sleep(TimeSpan.FromSeconds(66));
            VerifySourceOlderByDate(config, markerTime);
            VerifySourceByNumber(config, markerTime);
            VerifyArchiveByNumber(config, markerTime);
            VerifySourceDeletedOlderByDate(config, markerTime);

        }


        private static void VerifyArchiveOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
        {
            foreach (var rule in config.ArchiveRules)
            {
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(rule.MoveToArchiveOlderThanDays);
                var dirInfo = Directory.CreateDirectory(rule.ArchivePath);
                foreach (var searchPattern in rule.MonitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        Assert.IsTrue(file.LastWriteTime <= lastestDateTime, $"File should not be archived : {file.Name}");
                    }
                }
            }
        }

        private static void VerifySourceDeletedOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
        {
            foreach (var rule in config.ArchiveRules)
            {
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(rule.DeleteFromArchiveOlderThanDays);
                var dirInfo = Directory.CreateDirectory(rule.SourcePath);
                foreach (var searchPattern in rule.MonitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        Assert.IsTrue(file.LastWriteTime > lastestDateTime, $"File was not archived : {file.Name}");
                    }
                }
            }
        }

        private static void VerifySourceOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
        {
            foreach (var rule in config.ArchiveRules)
            {
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(rule.MoveToArchiveOlderThanDays);
                var dirInfo = Directory.CreateDirectory(rule.SourcePath);
                foreach (var searchPattern in rule.MonitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        Assert.IsTrue(file.LastWriteTime > lastestDateTime, $"File was not archived : {file.Name}");
                    }
                }
            }
        }

        private static void VerifySourceByNumber(ArchiveProcessorConfig config, DateTime markerTime)
        {
            foreach (var rule in config.ArchiveRules)
            {
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(rule.MoveToArchiveOlderThanDays);
                var dirInfo = Directory.CreateDirectory(rule.SourcePath);
                foreach (var searchPattern in rule.MonitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    Assert.IsTrue(files.Length <= rule.MoveToArchiveAfterReachingFiles, $"File number breach the limit {files.Length} : {rule.MoveToArchiveAfterReachingFiles}");
                }
            }
        }

        private static void VerifyArchiveByNumber(ArchiveProcessorConfig config, DateTime markerTime)
        {
            foreach (var rule in config.ArchiveRules)
            {
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(rule.MoveToArchiveOlderThanDays);
                var dirInfo = Directory.CreateDirectory(rule.ArchivePath);
                foreach (var searchPattern in rule.MonitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    Assert.IsTrue(files.Length <= rule.DeleteFromArchiveAfterReachingFiles, $"File number breach the limit {files.Length} : {rule.DeleteFromArchiveAfterReachingFiles}");
                }
            }
        }

        void FillTestFiles(ArchiveProcessorConfig config, string pattern, int number, DateTime time)
        {
            foreach (var rule in config.ArchiveRules)
            {
                string src = rule.SourcePath;
                if (!Directory.Exists(src))
                {
                    Directory.CreateDirectory(src);
                }
                for (int i = 0; i < number; i++)
                {
                    string name = string.Format(pattern, i);
                    name = Path.Combine(src, name);

                    using (var writer = File.CreateText(name))
                    {
                        writer.WriteLine($"Test file {name} : Date {time}");
                    }
                    FileInfo file = new FileInfo(name);
                    file.LastWriteTime = time;
                }
            }
        }


        private void CreateClearTestInputOutput(ArchiveProcessorConfig config)
        {

            foreach (var rule in config.ArchiveRules)
            {
                string src = rule.SourcePath;
                string archive = rule.ArchivePath;
                if (Directory.Exists(src))
                {
                    Directory.Delete(src, true);
                }
                if (Directory.Exists(archive))
                {
                    Directory.Delete(archive, true);
                }
            }
        }
    }
}
