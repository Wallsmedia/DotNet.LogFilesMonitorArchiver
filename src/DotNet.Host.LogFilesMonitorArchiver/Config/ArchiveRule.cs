// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using System.Collections.Generic;

namespace DotNet.Host.LogFilesMonitorArchiver.Config;


/// <summary>
/// The archive rule definition container.
/// </summary>
public class ArchiveRule
{
    /// <summary>
    /// Defines to use UTC time. Default value is true.
    /// </summary>
    public bool UseUtcTime { get; set; } = true;
    /// <summary>
    /// The source monitoring path.
    /// </summary>
    public string SourcePath { get; set; }

    /// <summary>
    /// The destination archive path.
    /// </summary>
    public string ArchivePath { get; set; }

    /// <summary>
    /// The list of file names search patterns.
    /// </summary>
    public List<string> MonitoringNames { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the age of the file in days before to move into archive.
    /// Files sorted by name and grouped by date.
    /// UTC time has been used.
    /// </summary>
    public int MoveToArchiveOlderThanDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the number of files. All above that will be moved  into archive.
    /// Files sorted by name and grouped by date.
    /// </summary>
    public int MoveToArchiveAfterReachingFiles { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the age of the file in days before to delete archive.
    /// Files sorted by name and grouped by date.
    /// UTC time has been used.
    /// </summary>
    public int DeleteFromArchiveOlderThanDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of files. All above that will be deleted  from archive.
    /// Files sorted by name and grouped by date.
    /// </summary>
    public int DeleteFromArchiveAfterReachingFiles { get; set; } = int.MaxValue;

}