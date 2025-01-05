// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log elements monitor and archiver

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

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
    /// Defines the monitoring mode elements or directories
    /// </summary>
    public  MonitoringMode MonitoringMode { get; set; }

    /// <summary>
    /// The list of element name search patterns. 
    /// Ignored for when MonitoringMode == MonitoringMode.SubdirectoriesOnly.
    /// </summary>
    public List<string> MonitoringNames { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the age of the element in days before to move into archive.
    /// Elements sorted by name and grouped by date.
    /// UTC time has been used.
    /// </summary>
    public int MoveToArchiveOlderThanDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the number of elements. All above that will be moved  into archive.
    /// Elements sorted by name and grouped by date.
    /// </summary>
    public int MoveToArchiveAfterReachingNumber { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the age of the element in days before to delete archive.
    /// Files sorted by name and grouped by date.
    /// UTC time has been used.
    /// </summary>
    public int DeleteFromArchiveOlderThanDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of elements. All above that will be deleted  from archive.
    /// Files sorted by name and grouped by date.
    /// </summary>
    public int DeleteFromArchiveAfterReachingNumber { get; set; } = int.MaxValue;


    /// <summary>
    /// Gets or sets the number of files. All above that will be moved  into archive.
    /// Files sorted by name and grouped by date.
    /// </summary>
    [Obsolete("Use property MoveToArchiveAfterReachingNumber", true)]
    public int MoveToArchiveAfterReachingFiles { get => MoveToArchiveAfterReachingNumber; set => MoveToArchiveAfterReachingNumber = value; }


    /// <summary>
    /// Gets or sets the number of files. All above that will be deleted  from archive.
    /// Files sorted by name and grouped by date.
    /// </summary>
    [Obsolete("Use property DeleteFromArchiveAfterReachingNumber", true)]
    public int DeleteFromArchiveAfterReachingFiles { get => DeleteFromArchiveAfterReachingNumber; set => DeleteFromArchiveAfterReachingNumber = value; } 

}
