// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

namespace DotNet.Host.LogFilesMonitorArchiver.Config;

/// <summary>
/// Defines the monitoring mode
/// </summary>
public enum MonitoringMode
{
    /// <summary>
    /// Defines to check files only in the path.
    /// </summary>
    FilesOnly,
    /// <summary>
    /// Defines to check directories only in the path.
    /// </summary>
    SubdirectoriesOnly
}