// \\     |/\  /||
//  \\ \\ |/ \/ ||
//   \//\\/|  \ || 
// Copyright © Alexander Paskhin 2013-2017. All rights reserved.
// Wallsmedia LTD 2015-2017:{Alexander Paskhin}
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
    public class FilesArchiveProcessor
    {
        /// <summary>
        /// The log file processor configuration <see cref="ArchiveProcessorConfig"/>
        /// </summary>
        public ArchiveProcessorConfig Configuration { get; }

        /// <summary>
        /// The action block cancellation token source.
        /// </summary>
        CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        CancellationToken CancellationToken { get; }

        CancellationTokenRegistration CancellationTokenRegistration { get; }

        /// <summary>
        /// Stops the archive processor.
        /// </summary>
        public void Stop()
        {
            CancellationTokenSource.Cancel();
        }
        /// <summary>
        /// The monitor processor mover and archiver.
        /// </summary>
        ActionBlock<ArchiveCommand> AchiveProcessor { get; }

        Timer ProcessorIntervalTimer { get; set; }

        /// <summary>
        /// Constructs the class. It passes the configuration rules to the processor.
        /// It starts,if it is configured, the background timed monitoring. 
        /// </summary>
        /// <param name="configurationRoot">The archive processor configuration and archive rules.</param>
        public FilesArchiveProcessor(ArchiveProcessorConfig configurationRoot)
        {
            Configuration = configurationRoot;
            CancellationToken = CancellationTokenSource.Token;
            CancellationTokenRegistration = CancellationToken.Register(CancellationRequested);
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The completion Task.</returns>
        public Task LunchArchiveFilesAsync()
        {
            if (CancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("FilesArchiveProcessor has been stopped.");
            }
            var cmd = ArchiveCommand.MoveToArchive;
            AchiveProcessor.Post(cmd);
            return cmd.Complete;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The completion Task.</returns>
        public Task LunchDeleteFromArchiveFilesAsync()
        {
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
            try
            {
                if (actionMessage.Action == ArchiveCommand.ArchiveAction.MoveToArchive)
                {
                    foreach (var archiveRule in Configuration.ArchiveRules)
                    {
                        string inputDirectory = archiveRule.SourcePath;
                        string archiveDirectory = archiveRule.ArchivePath;
                        foreach (var monitoringName in archiveRule.MonitoringNames)
                        {
                            string seachTemplate = monitoringName;
                            List<Tuple<string, string>> fileTuples = GetFilesOlderThanDays(DateTime.UtcNow, archiveRule.MoveToArchiveOlderThanDays, inputDirectory, seachTemplate);
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
                        string archiveDirectory = archiveRule.ArchivePath;
                        foreach (var monitoringName in archiveRule.MonitoringNames)
                        {
                            string seachTemplate = monitoringName;
                            List<Tuple<string, string>> fileTuples = GetFilesOlderThanDays(DateTime.UtcNow, archiveRule.DeleteFromArchiveOlderThanDays, archiveDirectory, seachTemplate);
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
        /// <param name="markerTime">The mark time to count from.</param>
        /// <param name="ageInDays">The number of days older than.</param>
        /// <param name="path">The directory to scan.</param>
        /// <param name="searchPattern">The filename template, search pattern.</param>
        /// <returns></returns>
        List<Tuple<string, string>> GetFilesOlderThanDays(DateTime markerTime, int ageInDays, string path, string searchPattern)
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            try
            {
                DateTime lastestDateTime = markerTime - TimeSpan.FromDays(ageInDays);
                if (Directory.Exists(path))
                {
                    var dirInfo = Directory.CreateDirectory(path);
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        if (file.LastWriteTime <= lastestDateTime)
                        {
                            result.Add(new Tuple<string, string>(path, file.Name));
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
        List<Tuple<string, string>> GetFilesAboveTheNumber(int maxTotal, string path, string searchPattern)
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            try
            {
                if (Directory.Exists(path))
                {
                    var dirInfo = Directory.CreateDirectory(path);
                    FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                    files = files.OrderByDescending(file => file.LastWriteTime).ToArray();
                    for (int i = maxTotal; i < files.Length; i++)
                    {
                        result.Add(new Tuple<string, string>(path, files[i].Name));
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filesToRemove"></param>
        void RemoveFiles(List<Tuple<string, string>> filesToRemove)
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
        /// <param name="filesToMove"></param>
        /// <param name="destinatioPath"></param>
        void MoveFiles(List<Tuple<string, string>> filesToMove, string destinatioPath)
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

        protected void CancellationRequested()
        {
            ProcessorIntervalTimer.Dispose();
            AchiveProcessor.Complete();
        }

        void CreateDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

    }
}
