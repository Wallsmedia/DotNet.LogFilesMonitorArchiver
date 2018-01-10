// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using DotNet.LogFilesMonitorArchiver;
using Microsoft.Extensions.Hosting;

namespace DotNet.Host.LogFilesMonitorArchiver
{
    /// <summary>
    /// Processor to archive log files by the rules setup in the configuration.
    /// </summary>
    public class FilesArchiveHostedService : FilesArchiveProcessor, IHostedService
    {

        /// <summary>
        ///  Hosted by <see cref="IHostedService"/>Processor to archive log files by the rules setup in the configuration.
        /// </summary>
        /// <param name="configurationRoot">The processor configuration <see cref="ArchiveProcessorConfig"/> </param>
        public FilesArchiveHostedService(ArchiveProcessorConfig configurationRoot) : base(configurationRoot, false) { }

    }
}
