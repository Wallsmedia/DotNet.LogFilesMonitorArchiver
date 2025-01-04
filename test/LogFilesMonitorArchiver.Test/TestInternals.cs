// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Test - Log files monitor and archiver

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;
using DotNet.Host.LogFilesMonitorArchiver.Config;

namespace LogFilesMonitorArchiver.Test;

public static class TestInternals
{
    public static void VerifySourceDeletedOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
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
                    var time = rule.UseUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                    Assert.IsTrue(time > lastestDateTime, $"File was not archived : {file.Name}");
                }
            }
        }
    }

    public static void VerifySourceOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
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
                    var time = rule.UseUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                    Assert.IsTrue(time > lastestDateTime, $"File was not archived : {file.Name}");
                }
            }
        }
    }

    public static void VerifySourceByNumber(ArchiveProcessorConfig config)
    {
        foreach (var rule in config.ArchiveRules)
        {
            var dirInfo = Directory.CreateDirectory(rule.SourcePath);
            foreach (var searchPattern in rule.MonitoringNames)
            {
                FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                Assert.IsTrue(files.Length <= rule.MoveToArchiveAfterReachingFiles, $"File number breach the limit {files.Length} : {rule.MoveToArchiveAfterReachingFiles}");
            }
        }
    }

    public static void VerifyArchiveByNumber(ArchiveProcessorConfig config)
    {
        foreach (var rule in config.ArchiveRules)
        {
            var dirInfo = Directory.CreateDirectory(rule.ArchivePath);
            foreach (var searchPattern in rule.MonitoringNames)
            {
                FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                Assert.IsTrue(files.Length <= rule.DeleteFromArchiveAfterReachingFiles, $"File number breach the limit {files.Length} : {rule.DeleteFromArchiveAfterReachingFiles}");
            }
        }
    }

    public static void FillTestFiles(ArchiveProcessorConfig config, string pattern, int number, DateTime time)
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
                if (rule.UseUtcTime)
                {
                    file.LastWriteTimeUtc = time;
                }
                else
                {
                    file.LastWriteTime = time;
                }
            }
        }
    }

    public static void CreateClearTestInputOutput(ArchiveProcessorConfig config)
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
    public static ArchiveProcessorConfig LoadConfiguration(string name)
    {
        ArchiveProcessorConfig config = null;
        string path = GetBasePath();
        path = Path.Combine(path, name);
        var build = new ConfigurationBuilder().AddJsonFile(path, false);
        var cfg = build.Build();
        var achiveConfig = cfg.GetSection(nameof(ArchiveProcessorConfig));
        config = achiveConfig.Get<ArchiveProcessorConfig>();

        path = GetBasePath();
        foreach (var rule in config.ArchiveRules)
        {
            rule.SourcePath = Path.Combine(path, rule.SourcePath);
            rule.ArchivePath = Path.Combine(path, rule.ArchivePath);
        }
        return config;
    }

    public static void VerifyArchiveOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
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
                    var time = rule.UseUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                    Assert.IsTrue(time <= lastestDateTime, $"File should not be archived : {file.Name}");
                }
            }
        }
    }



    public static string GetBasePath()
    {
        var assembly = typeof(TestArchiveFiles).GetTypeInfo().Assembly;
        return Path.GetDirectoryName(assembly.Location);
    }

}
