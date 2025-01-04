// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Test - Log files monitor and archiver

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;
using DotNet.Host.LogFilesMonitorArchiver.Config;
using System.Collections.Generic;

namespace LogFilesMonitorArchiver.Test;

public static class TestInternals
{
    public static void VerifySourceOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
    {
        foreach (var rule in config.ArchiveRules)
        {
            DateTime latestDateTime = markerTime - TimeSpan.FromDays(rule.DeleteFromArchiveOlderThanDays);
            var dirInfo = Directory.CreateDirectory(rule.SourcePath);
            var monitoringNames = rule.MonitoringNames;

            if (monitoringNames.Count == 0)
            {
                monitoringNames = new() { "*" };
            }

            if (rule.MonitoringMode == MonitoringMode.FilesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        var time = rule.UseUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                        Assert.IsTrue(time > latestDateTime, $"File was not archived : {file.Name}");
                    }
                }
            }
            else
            if (rule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    var dirs = dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var dir in dirs)
                    {
                        var time = rule.UseUtcTime ? dir.LastWriteTimeUtc : dir.LastWriteTime;
                        Assert.IsTrue(time > latestDateTime, $"Directory was not archived : {dir.Name}");
                    }
                }
            }
        }
    }

    public static void VerifySourceByNumber(ArchiveProcessorConfig config)
    {
        foreach (var rule in config.ArchiveRules)
        {
            var dirInfo = Directory.CreateDirectory(rule.SourcePath);

            var monitoringNames = rule.MonitoringNames;
            if (monitoringNames.Count == 0)
            {
                monitoringNames = new() { "*" };
            }

            if (rule.MonitoringMode == MonitoringMode.FilesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    Assert.IsTrue(files.Length <= rule.MoveToArchiveAfterReachingNumber, $"File number breach the limit {files.Length} : {rule.MoveToArchiveAfterReachingNumber}");
                }
            }
            else if (rule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    var dirs = dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                    Assert.IsTrue(dirs.Length <= rule.MoveToArchiveAfterReachingNumber, $"Directory number breach the limit {dirs.Length} : {rule.MoveToArchiveAfterReachingNumber}");
                }
            }
        }
    }

    public static void VerifyArchiveByNumber(ArchiveProcessorConfig config)
    {
        foreach (var rule in config.ArchiveRules)
        {
            var dirInfo = Directory.CreateDirectory(rule.ArchivePath);

            var monitoringNames = rule.MonitoringNames;
            if (monitoringNames.Count == 0)
            {
                monitoringNames = new() { "*" };
            }

            if (rule.MonitoringMode == MonitoringMode.FilesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    Assert.IsTrue(files.Length <= rule.DeleteFromArchiveAfterReachingNumber, $"File number breach the limit {files.Length} : {rule.DeleteFromArchiveAfterReachingNumber}");
                }
            }
            else if (rule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    var dirs = dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                    Assert.IsTrue(dirs.Length <= rule.DeleteFromArchiveAfterReachingNumber, $"Directory number breach the limit {dirs.Length} : {rule.MoveToArchiveAfterReachingNumber}");
                }
            }
        }
    }

    public static void VerifyArchiveOlderByDate(ArchiveProcessorConfig config, DateTime markerTime)
    {
        foreach (var rule in config.ArchiveRules)
        {
            DateTime latestDateTime = markerTime - TimeSpan.FromDays(rule.MoveToArchiveOlderThanDays);
            var dirInfo = Directory.CreateDirectory(rule.ArchivePath);

            var monitoringNames = rule.MonitoringNames;
            if (monitoringNames.Count == 0)
            {
                monitoringNames = new() { "*" };
            }

            if (rule.MonitoringMode == MonitoringMode.FilesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        var time = rule.UseUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                        Assert.IsTrue(time <= latestDateTime, $"File should not be archived : {file.Name}");
                    }
                }
            }
            else if (rule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
            {
                foreach (var searchPattern in monitoringNames)
                {
                    var dirs = dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var dir in dirs)
                    {
                        var time = rule.UseUtcTime ? dir.LastWriteTimeUtc : dir.LastWriteTime;
                        Assert.IsTrue(time <= latestDateTime, $"Directory should not be archived : {dir.Name}");
                    }
                }
            }
        }
    }

    public static void FillTestFiles(ArchiveProcessorConfig config, string pattern, int number, DateTime time)
    {
        foreach (var rule in config.ArchiveRules)
        {
            string sourcePath = rule.SourcePath;
            CreateDirectory(sourcePath);

            if (rule.MonitoringMode == MonitoringMode.FilesOnly)
            {
                GenerateFilesInFolder(pattern, number, time, rule, sourcePath);
            }
            else if (rule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
            {
                for (int i = 0; i < number; i++)
                {
                    string name = string.Format(pattern, i);
                    name = Path.GetFileNameWithoutExtension(name);
                    var subFolder = Path.Combine(sourcePath, name);
                    CreateDirectory(subFolder);
                    GenerateFilesInFolder(pattern, number, time, rule, subFolder);
                }
            }
        }
    }

    public static void CreateDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static void GenerateFilesInFolder(string pattern, int number, DateTime time, ArchiveRule rule, string sourcePath)
    {
        for (int i = 0; i < number; i++)
        {
            string name = string.Format(pattern, i);
            name = Path.Combine(sourcePath, name);

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

    public static void CreateClearTestInputOutput(ArchiveProcessorConfig config)
    {

        foreach (var rule in config.ArchiveRules)
        {
            string src = rule.SourcePath;
            string archive = rule.ArchivePath;
            if (Directory.Exists(src))
            {
                RemoveDirectory(src);
            }
            if (Directory.Exists(archive))
            {
                RemoveDirectory(archive);
            }
        }

        void RemoveDirectory(string source)
        {
            var stack = new Stack<string>();
            stack.Push(source);
            while (stack.Count > 0)
            {
                var currentFolder = stack.Pop();

                foreach (var file in Directory.GetFiles(currentFolder, "*.*"))
                {
                    File.Delete(file);
                }
                foreach (var directory in Directory.GetDirectories(currentFolder))
                {
                    stack.Push(Path.Combine(currentFolder, Path.GetFileName(directory)));
                }
            }
            Directory.Delete(source, true);
        }
    }
    public static ArchiveProcessorConfig LoadConfiguration(string name)
    {
        ArchiveProcessorConfig config = null;
        string path = GetBasePath();
        path = Path.Combine(path, name);
        var build = new ConfigurationBuilder().AddJsonFile(path, false);
        var cfg = build.Build();
        var archiveConfig = cfg.GetSection(nameof(ArchiveProcessorConfig));
        config = archiveConfig.Get<ArchiveProcessorConfig>();

        path = GetBasePath();
        foreach (var rule in config.ArchiveRules)
        {
            rule.SourcePath = Path.Combine(path, rule.SourcePath);
            rule.ArchivePath = Path.Combine(path, rule.ArchivePath);
        }
        return config;
    }





    public static string GetBasePath()
    {
        var assembly = typeof(TestArchiveFiles).GetTypeInfo().Assembly;
        return Path.GetDirectoryName(assembly.Location);
    }

}
