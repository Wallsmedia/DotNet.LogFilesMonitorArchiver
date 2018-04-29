// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using DotNet.LogFilesMonitorArchiver;
using Microsoft.Extensions.Configuration;
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, FilesArchiveHostedService>(
                (p) =>
                {
                    var c = p.GetService<IConfiguration>();
                    var a = new ArchiveProcessorConfigWrapper(c);
                    return new FilesArchiveHostedService(a);
                }
                ));
        }

        /// <summary>
        /// Adds the Log Files Monitor Archiver to <see cref="ServiceCollection"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configurationSection">The configuration section name.</param>
        public static void AddLogFilesMonitorArchiver(this IServiceCollection services, string configurationSection)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, FilesArchiveHostedService>(
                (p) =>
                {
                    var c = p.GetService<IConfiguration>();
                    var a = new ArchiveProcessorConfigWrapper(c, configurationSection);
                    return new FilesArchiveHostedService(a);
                }
                ));
        }
    }
}
