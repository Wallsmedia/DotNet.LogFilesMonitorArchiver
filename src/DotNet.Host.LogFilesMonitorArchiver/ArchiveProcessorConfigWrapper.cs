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
        private string ConfigurationSection { get; set; } = nameof(ArchiveProcessorConfig);

        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructs configuration wrapper.
        /// </summary>
        /// <param name="configuration">The instance of the configuration <see cref="IConfiguration"/></param>
        public ArchiveProcessorConfigWrapper(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadConfiguration(_configuration);
            ChangeToken.OnChange(() => _configuration.GetReloadToken(), changeTokenConsumer: (cfg) => LoadConfiguration(cfg), state: _configuration);
        }

        /// <summary>
        /// Constructs configuration wrapper.
        /// </summary>
        /// <param name="configurationSection">The configuration Section name.</param>
        /// <param name="configuration">The instance of the configuration <see cref="IConfiguration"/></param>
        public ArchiveProcessorConfigWrapper(IConfiguration configuration,string configurationSection):this(configuration)
        {
            ConfigurationSection = configurationSection;
        }
             
        void LoadConfiguration(IConfiguration configuration)
        {
            configuration.Bind(ConfigurationSection, this);
        }

    }

}
