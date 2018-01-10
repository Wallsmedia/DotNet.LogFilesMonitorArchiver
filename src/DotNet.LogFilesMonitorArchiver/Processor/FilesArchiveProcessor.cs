// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DotNet.LogFilesMonitorArchiver
{
    /// <summary>
    /// Processor to archive log files by the rules setup in the configuration.
    /// </summary>
    public class FilesArchiveProcessor : IDisposable
    {
        /// <summary>
        /// The monitor processor mover and archiver.
        /// </summary>
        ActionBlock<ArchiveCommand> AchiveProcessor { get; set; }

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
        /// Triggered to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (CancellationTokenSource != null)
            {
                throw new InvalidOperationException("Process has been already started");
            }

            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;
            // We are not completing the ActionBlock Task with Cancel exception
            AchiveProcessor = new ActionBlock<ArchiveCommand>((Action<ArchiveCommand>)ArchiveProcessorAction);

            if (Configuration.AutoTimerIntervalEnabled)
            {
                if (Configuration.ArchiveOnStartup)
                {
                    ProcessorIntervalTimer = new Timer(ProcessLogTimerCallback, this, TimeSpan.FromSeconds(Configuration.DelayArchiveInSecondsOnstartUp),
                        TimeSpan.FromMinutes(Configuration.AutoTimerArchiveIntervalMin));
                }
                else
                {
                    ProcessorIntervalTimer = new Timer(ProcessLogTimerCallback, this,
                        TimeSpan.FromMinutes(Configuration.AutoTimerArchiveIntervalMin),
                        TimeSpan.FromMinutes(Configuration.AutoTimerArchiveIntervalMin));
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered to perform a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                ProcessorIntervalTimer?.Dispose();
                AchiveProcessor.Complete();
                CancellationTokenSource = null;
                return AchiveProcessor.Completion;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Constructs the class. It passes the configuration rules to the processor.
        /// It starts,if it is configured, the background timed monitoring. 
        /// </summary>
        /// <param name="configurationRoot">The archive processor configuration and archive rules.</param>
        public FilesArchiveProcessor(ArchiveProcessorConfig configurationRoot) : this(configurationRoot, true) { }

        /// <summary>
        /// Constructs the class. It passes the configuration rules to the processor.
        /// It starts,if it is configured, the background timed monitoring. 
        /// </summary>
        /// <param name="configurationRoot">The archive processor configuration and archive rules.</param>
        /// <param name="startOnConstruct">For hosted version support.</param>
        public FilesArchiveProcessor(ArchiveProcessorConfig configurationRoot, bool startOnConstruct)
        {
            Configuration = configurationRoot;
            if (startOnConstruct)
            {
                StartAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Launch the asynchronous task to perform the "Archive" monitoring.
        /// </summary>
        /// <returns>The completion Task.</returns>
        public Task LaunchArchiveFilesAsync()
        {
            if (CancellationTokenSource == null)
            {
                throw new InvalidOperationException("Process has not been started");
            }

            if (CancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("FilesArchiveProcessor has been stopped.");
            }
            var cmd = ArchiveCommand.MoveToArchive;
            AchiveProcessor.Post(cmd);
            return cmd.Complete;
        }

        /// <summary>
        /// Launch the asynchronous task to perform the "Delete" monitoring.
        /// </summary>
        /// <returns>The completion Task.</returns>
        public Task LaunchDeleteFromArchiveFilesAsync()
        {
            if (CancellationTokenSource == null)
            {
                throw new InvalidOperationException("Process has not been started");
            }

            if (CancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("FilesArchiveProcessor has been stopped.");
            }
            var cmd = ArchiveCommand.DeleteFromArchive;
            AchiveProcessor.Post(cmd);
            return cmd.Complete;
        }

        private void ProcessLogTimerCallback(object state)
        {
            AchiveProcessor.Post(ArchiveCommand.MoveToArchive);
            AchiveProcessor.Post(ArchiveCommand.DeleteFromArchive);
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
                        foreach (var monitoringName in archiveRule.MonitoringNames)
                        {
                            string seachTemplate = monitoringName;

                            List<(string, string)> fileTuples = GetFilesOlderThanDays(archiveRule.UseUtcTime, markerTime, archiveRule.MoveToArchiveOlderThanDays, inputDirectory, seachTemplate);
                            MoveFiles(fileTuples, archiveDirectory);
                            fileTuples = GetFilesAboveTheNumber(archiveRule.MoveToArchiveAfterReachingFiles, inputDirectory, seachTemplate);
                            MoveFiles(fileTuples, archiveDirectory);
                        }
                    }
                }
                else if (actionMessage.Action == ArchiveCommand.ArchiveAction.DeleteFromArchive)
                {
                    foreach (var archiveRule in Configuration.ArchiveRules)
                    {
                        DateTime markerTime = archiveRule.UseUtcTime ? DateTime.UtcNow : DateTime.Now;
                        string archiveDirectory = archiveRule.ArchivePath;
                        foreach (var monitoringName in archiveRule.MonitoringNames)
                        {
                            string seachTemplate = monitoringName;
                            List<(string, string)> fileTuples = GetFilesOlderThanDays(archiveRule.UseUtcTime, markerTime, archiveRule.DeleteFromArchiveOlderThanDays, archiveDirectory, seachTemplate);
                            RemoveFiles(fileTuples);
                            fileTuples = GetFilesAboveTheNumber(archiveRule.DeleteFromArchiveAfterReachingFiles, archiveDirectory, seachTemplate);
                            RemoveFiles(fileTuples);
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
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(ageInDays);
                if (Directory.Exists(path))
                {
                    var dirInfo = Directory.CreateDirectory(path);
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        var time = useUtcTime ? file.LastWriteTimeUtc : file.LastWriteTime;
                        if (time <= lastestDateTime)
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
        /// Moves files in the source list to the destination folder.
        /// </summary>
        /// <param name="filesToMove">The list of files to move.</param>
        /// <param name="destinatioPath">The destination path.</param>
        void MoveFiles(List<(string, string)> filesToMove, string destinatioPath)
        {
            try
            {
                CreateDirectory(destinatioPath);
                foreach (var tfile in filesToMove)
                {
                    try
                    {
                        string newFileName = tfile.Item2;
                        string newFileNameWitoutExtension = Path.GetFileNameWithoutExtension(newFileName);
                        string extension = Path.GetExtension(newFileName);
                        int maxFileCopies = 100000;
                        for (int i = 1; i < maxFileCopies; ++i)
                        {
                            var fi = new FileInfo(Path.Combine(destinatioPath, newFileName));
                            if (!fi.Exists)
                            {
                                break;
                            }
                            if (newFileNameWitoutExtension.Contains("."))
                            {
                                newFileNameWitoutExtension = Path.GetFileNameWithoutExtension(newFileNameWitoutExtension);
                            }
                            newFileName = $"{newFileNameWitoutExtension}.{i}{extension}";
                        }
                        File.Move(Path.Combine(tfile.Item1, tfile.Item2), Path.Combine(destinatioPath, newFileName));
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
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if ((CancellationTokenSource != null) && (disposing))
                {
                    // dispose managed state (managed objects).
                    CancellationTokenSource.Cancel();
                    ProcessorIntervalTimer?.Dispose();
                    AchiveProcessor.Complete();
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
}
