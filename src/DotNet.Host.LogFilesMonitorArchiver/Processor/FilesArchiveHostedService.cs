// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using DotNet.Host.LogFilesMonitorArchiver.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNet.Host.LogFilesMonitorArchiver.Processor;

/// <summary>
/// Processor to archive log files by the rules setup in the configuration.
/// </summary>
public class FilesArchiveHostedService : IHostedService, IDisposable
{
    private bool disposedValue = false; // To detect redundant calls
    private readonly ILogger<FilesArchiveHostedService> logger;
    private readonly List<string> allNames = new() { "*" };

    /// <summary>
    /// The interval timer for repeated actions.
    /// </summary>
    Timer ProcessorIntervalTimer { get; set; }

    /// <summary>
    /// The log file processor configuration <see cref="ArchiveProcessorConfig"/>
    /// </summary>
    public ArchiveProcessorConfig Configuration { get; }

    /// <summary>
    /// The action block cancellation token source.
    /// </summary>
    CancellationTokenSource CancellationTokenSource;
    CancellationToken CancellationToken;

    /// <summary>
    /// Constructs the class. It passes the configuration rules to the processor.
    /// It starts,if it is configured, the background timed monitoring. 
    /// </summary>
    /// <param name="logger">The hosted service logger.</param>
    /// <param name="configurationRoot">The archive processor configuration and archive rules.</param>
    public FilesArchiveHostedService(ILogger<FilesArchiveHostedService> logger,
        ArchiveProcessorConfig configurationRoot)
    {
        this.logger = logger;
        Configuration = configurationRoot;
    }

    /// <summary>
    /// Triggered to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (CancellationTokenSource != null)
        {
            logger.LogWarning("[FilesArchiveHostedService] - Already Started.");
        }

        logger.LogInformation("[FilesArchiveHostedService] - Started.");

        CancellationTokenSource = new CancellationTokenSource();
        CancellationToken = CancellationTokenSource.Token;
        // We are not completing the ActionBlock Task with Cancel exception

        logger.LogInformation($"[FilesArchiveHostedService] - Archiving interval: {Configuration.AutoTimerArchiveIntervalMin} min.");

        if (Configuration.ArchiveOnStartup)
        {
            logger.LogInformation("[FilesArchiveHostedService] - Started with mode: Archive On Startup.");
            ProcessorIntervalTimer = new Timer(ProcessLogTimerCallback, this, TimeSpan.FromSeconds(Configuration.DelayArchiveInSecondsOnStartUp),
                TimeSpan.FromMinutes(Configuration.AutoTimerArchiveIntervalMin));
        }
        else
        {
            ProcessorIntervalTimer = new Timer(ProcessLogTimerCallback, this,
                TimeSpan.FromMinutes(Configuration.AutoTimerArchiveIntervalMin),
                TimeSpan.FromMinutes(Configuration.AutoTimerArchiveIntervalMin));
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered to perform a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (CancellationTokenSource != null)
        {
            logger.LogInformation("[FilesArchiveHostedService] - Stopped.");
            CancellationTokenSource.Cancel();
            ProcessorIntervalTimer?.Dispose();
            CancellationTokenSource = null;
        }
        return Task.CompletedTask;
    }

    private void ProcessLogTimerCallback(object state)
    {
        ArchiveProcessorAction(ArchiveCommand.MoveToArchive);
        ArchiveProcessorAction(ArchiveCommand.DeleteFromArchive);
    }

    private void ArchiveProcessorAction(ArchiveCommand actionMessage)
    {
        if (CancellationToken.IsCancellationRequested)
        {
            actionMessage.MarkCanceled();
            return;
        }

        try
        {
            if (actionMessage.Action == ArchiveCommand.ArchiveAction.MoveToArchive)
            {
                foreach (var archiveRule in Configuration.ArchiveRules)
                {
                    DateTime markerTime = archiveRule.UseUtcTime ? DateTime.UtcNow : DateTime.Now;
                    string inputDirectory = archiveRule.SourcePath;
                    string archiveDirectory = archiveRule.ArchivePath;
                    var monitoringNames = archiveRule.MonitoringNames;

                    if (monitoringNames.Count == 0)
                    {
                        monitoringNames = allNames;
                    }

                    foreach (var monitoringName in monitoringNames)
                    {
                        string searchTemplate = monitoringName;

                        if (archiveRule.MonitoringMode == MonitoringMode.FilesOnly)
                        {
                            List<(string, string)> fileTuples = GetFilesOlderThanDays(archiveRule.UseUtcTime, markerTime, archiveRule.MoveToArchiveOlderThanDays, inputDirectory, searchTemplate);
                            MoveFiles(fileTuples, archiveDirectory);
                            fileTuples = GetFilesAboveTheNumber(archiveRule.MoveToArchiveAfterReachingNumber, inputDirectory, searchTemplate);
                            MoveFiles(fileTuples, archiveDirectory);
                        }
                        else if (archiveRule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
                        {
                            List<(string, string)> dirTuples = GetDirectoriesOlderThanDays(archiveRule.UseUtcTime, markerTime, archiveRule.MoveToArchiveOlderThanDays, inputDirectory, searchTemplate);
                            MoveDirectories(dirTuples, archiveDirectory);
                            dirTuples = GetDirectoriesAboveTheNumber(archiveRule.MoveToArchiveAfterReachingNumber, inputDirectory, searchTemplate);
                            MoveDirectories(dirTuples, archiveDirectory);
                        }
                    }
                }
            }
            else if (actionMessage.Action == ArchiveCommand.ArchiveAction.DeleteFromArchive)
            {
                foreach (var archiveRule in Configuration.ArchiveRules)
                {
                    DateTime markerTime = archiveRule.UseUtcTime ? DateTime.UtcNow : DateTime.Now;
                    string archiveDirectory = archiveRule.ArchivePath;
                    var monitoringNames = archiveRule.MonitoringNames;

                    if (monitoringNames.Count == 0)
                    {
                        monitoringNames = allNames;
                    }

                    foreach (var monitoringName in monitoringNames)
                    {
                        if (archiveRule.MonitoringMode == MonitoringMode.FilesOnly)
                        {
                            string searchTemplate = monitoringName;
                            List<(string, string)> fileTuples = GetFilesOlderThanDays(archiveRule.UseUtcTime, markerTime, archiveRule.DeleteFromArchiveOlderThanDays, archiveDirectory, searchTemplate);
                            RemoveFiles(fileTuples);
                            fileTuples = GetFilesAboveTheNumber(archiveRule.DeleteFromArchiveAfterReachingNumber, archiveDirectory, searchTemplate);
                            RemoveFiles(fileTuples);
                        }
                        else if (archiveRule.MonitoringMode == MonitoringMode.SubdirectoriesOnly)
                        {
                            string searchTemplate = monitoringName;
                            List<(string, string)> dirTuples = GetDirectoriesOlderThanDays(archiveRule.UseUtcTime, markerTime, archiveRule.DeleteFromArchiveOlderThanDays, archiveDirectory, searchTemplate);
                            RemoveDirectories(dirTuples);
                            dirTuples = GetDirectoriesAboveTheNumber(archiveRule.DeleteFromArchiveAfterReachingNumber, archiveDirectory, searchTemplate);
                            RemoveDirectories(dirTuples);
                        }
                    }
                }

            }
            actionMessage.MarkComplete();
        }
        catch (Exception ex)
        {
            actionMessage.MarkException(ex);
        }
    }

    /// <summary>
    /// Returns the list of files that older than exact date.
    /// </summary>
    /// <param name="useUtcTime">Use UTC time.</param>
    /// <param name="markerTime">The mark time to count from.</param>
    /// <param name="ageInDays">The number of days older than.</param>
    /// <param name="path">The directory to scan.</param>
    /// <param name="searchPattern">The filename template, search pattern.</param>
    /// <returns></returns>
    List<(string, string)> GetFilesOlderThanDays(bool useUtcTime, DateTime markerTime, int ageInDays, string path, string searchPattern)
    {
        List<(string, string)> result = new List<(string, string)>();
        try
        {
            DateTime latestDateTime = markerTime - TimeSpan.FromDays(ageInDays);
            if (Directory.Exists(path))
            {
                var dirInfo = Directory.CreateDirectory(path);
                FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var time = useUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                    if (time <= latestDateTime)
                    {
                        result.Add((path, file.Name));
                    }
                }
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Returns the list of directories that older than exact date.
    /// </summary>
    /// <param name="useUtcTime">Use UTC time.</param>
    /// <param name="markerTime">The mark time to count from.</param>
    /// <param name="ageInDays">The number of days older than.</param>
    /// <param name="path">The directory to scan.</param>
    /// <param name="searchPattern">The filename template, search pattern.</param>
    /// <returns></returns>
    List<(string, string)> GetDirectoriesOlderThanDays(bool useUtcTime, DateTime markerTime, int ageInDays, string path, string searchPattern)
    {
        List<(string, string)> result = new List<(string, string)>();
        try
        {
            DateTime latestDateTime = markerTime - TimeSpan.FromDays(ageInDays);
            if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(path);
                DirectoryInfo[] directories = dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                foreach (var directory in directories)
                {
                    var time = useUtcTime ? directory.LastWriteTimeUtc : directory.LastWriteTime;
                    if (time <= latestDateTime)
                    {
                        result.Add((path, directory.Name));
                    }
                }
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Returns the list of files which is sequence number is greater than maxTotal.
    /// </summary>
    /// <param name="maxTotal">The limit number of files in the directory.</param>
    /// <param name="path">The directory to scan.</param>
    /// <param name="searchPattern">The filename template, search pattern.</param>
    /// <returns></returns>
    List<(string, string)> GetFilesAboveTheNumber(int maxTotal, string path, string searchPattern)
    {
        List<(string, string)> result = new List<(string, string)>();
        try
        {
            if (Directory.Exists(path))
            {
                var dirInfo = Directory.CreateDirectory(path);
                FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                files = files.OrderByDescending(file => file.LastWriteTime).ToArray();
                for (int i = maxTotal; i < files.Length; i++)
                {
                    result.Add((path, files[i].Name));
                }
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Returns the list of directories which is sequence number is greater than maxTotal.
    /// </summary>
    /// <param name="maxTotal">The limit number of files in the directory.</param>
    /// <param name="path">The directory to scan.</param>
    /// <param name="searchPattern">The filename template, search pattern.</param>
    /// <returns></returns>
    List<(string, string)> GetDirectoriesAboveTheNumber(int maxTotal, string path, string searchPattern)
    {
        List<(string, string)> result = new List<(string, string)>();
        try
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(path);
                DirectoryInfo[] directories = dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                directories = directories.OrderByDescending(file => file.LastWriteTime).ToArray();
                for (int i = maxTotal; i < directories.Length; i++)
                {
                    result.Add((path, directories[i].Name));
                }
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Removes the files by the tuple list.
    /// </summary>
    /// <param name="filesToRemove">The list files to remove.</param>
    void RemoveFiles(List<(string, string)> filesToRemove)
    {
        foreach (var tfile in filesToRemove)
        {
            try
            {
                File.Delete(Path.Combine(tfile.Item1, tfile.Item2));
            }
            catch { }
        }
    }


    /// <summary>
    /// Moves directories in the source list to the destination folder.
    /// </summary>
    /// <param name="directoriesToMove">The list of directories to move.</param>
    /// <param name="destinationPath">The destination path.</param>
    void MoveDirectories(List<(string, string)> directoriesToMove, string destinationPath)
    {
        foreach (var tDir in directoriesToMove)
        {
            try
            {
                var source = Path.Combine(tDir.Item1, tDir.Item2);
                var target = Path.Combine(destinationPath, tDir.Item2);
                MoveDirectory(source, target);
            }
            catch { }
        }
    }

    /// <summary>
    /// Removes directories in the source list.
    /// </summary>
    /// <param name="directoriesToMove">The list of directories.</param>
    void RemoveDirectories(List<(string, string)> directoriesToMove)
    {
        foreach (var tDir in directoriesToMove)
        {
            try
            {
                var source = Path.Combine(tDir.Item1, tDir.Item2);
                RemoveDirectory(source);
            }
            catch { }
        }
    }
    record Folder(string Source, string Target);


    /// <summary>
    /// Removes directory and all files in it.
    /// </summary>
    /// <param name="source">The source path.</param>
    public void RemoveDirectory(string source)
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

    /// <summary>
    /// Moves directory and all files in it.
    /// </summary>
    /// <param name="source">The source path.</param>
    /// <param name="target">The target path.</param>
    public void MoveDirectory(string source, string target)
    {
        var stack = new Stack<Folder>();
        stack.Push(new Folder(source, target));
        while (stack.Count > 0)
        {
            var currentFolder = stack.Pop();
            Directory.CreateDirectory(currentFolder.Target);
            foreach (var file in Directory.GetFiles(currentFolder.Source, "*.*"))
            {
                string targetFile = Path.Combine(currentFolder.Target, Path.GetFileName(file));
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }

                File.Move(file, targetFile);
            }
            foreach (var directory in Directory.GetDirectories(currentFolder.Source))
            {
                stack.Push(new Folder(directory, Path.Combine(currentFolder.Target, Path.GetFileName(directory))));
            }
        }
        Directory.Delete(source, true);
    }

    /// <summary>
    /// Moves files in the source list to the destination folder.
    /// </summary>
    /// <param name="filesToMove">The list of files to move.</param>
    /// <param name="destinationPath">The destination path.</param>
    void MoveFiles(List<(string, string)> filesToMove, string destinationPath)
    {
        try
        {
            CreateDirectory(destinationPath);
            foreach (var tFile in filesToMove)
            {
                try
                {
                    string newFileName = tFile.Item2;
                    string newFileNameWithoutExtension = Path.GetFileNameWithoutExtension(newFileName);
                    string extension = Path.GetExtension(newFileName);
                    int maxFileCopies = 100000;
                    for (int i = 1; i < maxFileCopies; ++i)
                    {
                        var fi = new FileInfo(Path.Combine(destinationPath, newFileName));
                        if (!fi.Exists)
                        {
                            break;
                        }
                        if (newFileNameWithoutExtension.Contains("."))
                        {
                            newFileNameWithoutExtension = Path.GetFileNameWithoutExtension(newFileNameWithoutExtension);
                        }
                        newFileName = $"{newFileNameWithoutExtension}.{i}{extension}";
                    }
                    File.Move(Path.Combine(tFile.Item1, tFile.Item2), Path.Combine(destinationPath, newFileName));
                }
                catch { }
            }
        }
        catch { }
    }

    void CreateDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    #region IDisposable Support

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (CancellationTokenSource != null && disposing)
            {
                // dispose managed state (managed objects).
                CancellationTokenSource.Cancel();
                ProcessorIntervalTimer?.Dispose();
                CancellationTokenSource = null;
            }
            disposedValue = true;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    #endregion

}
