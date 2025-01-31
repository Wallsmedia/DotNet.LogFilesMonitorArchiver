﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using System.Collections.Generic;

namespace DotNet.Host.LogFilesMonitorArchiver.Config;

/// <summary>
/// The monitor archiver processor configuration.
/// </summary>
public class ArchiveProcessorConfig
{

    /// <summary>
    /// Sets or gets the flag to run archive on the startup.
    /// It is a log archive processor construction feature.
    /// </summary>
    public bool ArchiveOnStartup { get; set; }

    /// <summary>
    /// Sets or gets the delay time to start in seconds after the construction of the instance.
    /// It is a log archive processor construction feature.
    /// </summary>
    public int DelayArchiveInSecondsOnStartUp { get; set; } = 30;

    /// <summary>
    /// Sets or gets the repetition of archive processing.
    /// It is a log archive processor construction feature.
    /// </summary>
    public int AutoTimerArchiveIntervalMin { get; set; } = 60 * 24;

    /// <summary>
    /// Gets or sets the key pair values or input + output archive directories.
    /// The processor doesn't create the input directories, it creates the output, archive directories.
    /// </summary>
    public List<ArchiveRule> ArchiveRules { get; set; } = new List<ArchiveRule>();

}