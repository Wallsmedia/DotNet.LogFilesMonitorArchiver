// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using DotNet.Host.LogFilesMonitorArchiver.Config;
using DotNet.Host.LogFilesMonitorArchiver.Processor;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the Log Files Monitor Archiver to <see cref="ServiceCollection"/>.
/// </summary>
public static class LogFilesMonitorArchiverExtensions
{

    /// <summary>
    /// Adds the Log Files Monitor Archiver to <see cref="ServiceCollection"/>.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static void AddLogFilesMonitorArchiver(this IServiceCollection services)
    {
        services.AddHostedService<FilesArchiveHostedService>();
    }

    /// <summary>
    /// Adds the Log Files Monitor Archiver Configuration to <see cref="ServiceCollection"/>.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="sectionName">The configuration sub-section with the specified key.</param>
    public static void AddLogFilesMonitorArchiverConfiguration(this IServiceCollection services, IConfiguration configuration, string sectionName = nameof(ArchiveProcessorConfig))
    {
        ArchiveProcessorConfig archiveProcessorConfig = configuration.GetSection(sectionName).Get<ArchiveProcessorConfig>();
        services.AddSingleton(archiveProcessorConfig);
    }

}
