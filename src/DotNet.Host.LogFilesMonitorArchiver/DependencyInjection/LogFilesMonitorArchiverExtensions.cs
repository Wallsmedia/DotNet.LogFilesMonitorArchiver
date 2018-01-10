// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using DotNet.LogFilesMonitorArchiver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DotNet.Host.LogFilesMonitorArchiver.DependencyInjection
{
    /// <summary>
    /// Extension methods for the Log Files Monitor Archiver to <see cref="ServiceCollection"/>.
    /// </summary>
    public static class LogFilesMonitorArchiverExtensions
    {

        /// <summary>
        /// Adds the Log Files Monitor Archiver to <see cref="ServiceCollection"/>.
        /// </summary>
        /// <param name="services"></param>
        public static void AddLogFilesMonitorArchiver(this IServiceCollection services)
        {
            services.TryAddSingleton<FilesArchiveHostedService>();
            services.TryAddSingleton<ArchiveProcessorConfig,ArchiveProcessorConfigWrapper>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, FilesArchiveHostedService>((p)=> p.GetService<FilesArchiveHostedService>()) );
        }
    }
}
