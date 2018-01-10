// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using DotNet.LogFilesMonitorArchiver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace DotNet.Host.LogFilesMonitorArchiver
{
    /// <summary>
    /// Loads and binds the configuration from the root section of "IConfiguration" with class name "ArchiveProcessorConfig".
    /// </summary>
    public class ArchiveProcessorConfigWrapper : ArchiveProcessorConfig
    {
        
        /// <summary>
        /// Constructs configuration wrapper.
        /// </summary>
        /// <param name="configuration"></param>
        public ArchiveProcessorConfigWrapper(IConfiguration configuration)
        {
            LoadFromConfigiration(configuration);
            ChangeToken = configuration.GetReloadToken();
            ChangeToken?.RegisterChangeCallback((a) => LoadFromConfigiration(configuration), null);
        }

        public IChangeToken ChangeToken { get; }

        void LoadFromConfigiration(IConfiguration configuration)
        {
            configuration.Bind(nameof(ArchiveProcessorConfig), this);
        }

    }

}
