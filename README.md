### .NET Core Logging Files Monitor and Archiver
Long running services can generate lots of logging and temporary files. 
They become outdated and should be deleted by the dedicated period of time, like 180 days. 
".NET Core Logging Files Monitor and Archiver" provides convenient and flexible features for log files archiving and removing. 

".NET Core Logging Files Monitor and Archiver" is a long running monitor which scans file directories, archives them and deletes the old.
It keeps file directories in tidy state by the rules.


#### Version 8.0.0
- Add support of .Net 8.x and netstandard 2.0/2.1


**CLI Application edition of ".NET Core Logging Files Monitor and Archiver":**
https://www.nuget.org/packages/DotNet.LogFilesMonitorArchiver/

**ASP.NET Core WebHost and Host edition of ".NET Core Logging Files Monitor and Archiver":**
https://www.nuget.org/packages/DotNet.Host.LogFilesMonitorArchiver/



## Archive scenarios

The "DotNet.LogFilesMonitorArchiver" package allows to implement  the following archive scenarios:

1. Monitoring the "**SourcePath**" directory by the list of search pattern "**MonitoringNames**" names.

2. For each matched file name in the "**SourcePath**" directory apply the rules:
- Move it into the archive directory "**ArchivePath**" if the file created date is older than configured "**MoveToArchiveOlderThanDays**" days (default value 7).
- Move it into the archive directory "**ArchivePath**" if the number of files is greater than configured "**MoveToArchiveAfterReachingFiles**" number S(default value is int.MaxValue).

3. For each matched file name in the "**ArchivePath**" directory apply the rules: 
- Delete it from the archive directory "**ArchivePath**" if the file created date is older than configured "**DeleteFromArchiveOlderThanDays**" days (default value 30).
- Delete it from the archive directory "**ArchivePath**" if the number of files is greater than configured "**DeleteFromArchiveAfterReachingFiles**" number (default value is int.MaxValue).

## JSON Configuration 

Here is example of the JSON configuration file that can be use to define configuration.


*Example of the JSON configuration file:*
``` json
{
 "ArchiveProcessorConfig": {
   "AutoTimerIntervalEnabled": "true",
   "ArchiveOnStartup": "true",
   "DelayArchiveInSecondsOnstartUp": 1, 
   "AutoTimerArchiveIntervalMin": 720,
   "ArchiveRules": [
    {
      "SourcePath": "\\Logs\\AppExampleLogSource\\Stat",
      "ArchivePath": "\\Logs\\AppExampleLogSource\\Stat\\Archive",
      "MonitoringNames": [ "archive-able_*.xml", "*.csv" ],
      "MoveToArchiveOlderThanDays": 14,
      "MoveToArchiveAfterReachingFiles": 28,
      "DeleteFromArchiveOlderThanDays": 180,
      "DeleteFromArchiveAfterReachingFiles": 1000
    },
    {
      "UseUtcTime":"false",
      "SourcePath": "\\Logs\\AppExampleLogSource\\FatalError",
      "ArchivePath": "\\Logs\\AppExampleLogSource\\FatalError\\Archive",
      "MonitoringNames": [ "archive-able_*.xml", "*.trace" ],
      "MoveToArchiveOlderThanDays": 7,
      "DeleteFromArchiveOlderThanDays": 360
    }
  ]
 }
}
```

# Console application Example

Use https://www.nuget.org/packages/DotNet.LogFilesMonitorArchiver/ package.

*Example of the code to load configuration  file:*
``` c#
 public ArchiveProcessorConfig LoadConfiguration(string name)
 {
     ArchiveProcessorConfig config = null;
     string path = GetBasePath();
     path = Path.Combine(path, name);
     var build = new ConfigurationBuilder().AddJsonFile(path, false);
     var cfg = build.Build();
     var achiveConfig = cfg.GetSection(nameof(ArchiveProcessorConfig));
     config = achiveConfig.Get<ArchiveProcessorConfig>();
     path = GetBasePath();
     return config;
 }
 private string GetBasePath()
 {
     var assembly = GetType().GetTypeInfo().Assembly;
     return Path.GetDirectoryName(assembly.Location);
 }

```

### Automatic mode

*Example of the code to activate the Archive Processor in Automatic mode:*
For automatic mode you have to ensure that in configuration has been setup:
"AutoTimerIntervalEnabled": "**true**",

``` c#
static FilesArchiveProcessor _filesArchiveProcessor;
...
ArchiveProcessorConfig config = LoadConfiguration("FilesArchiveProcessor.json");

// Ensure that Auto Timer Interval has been enabled
config.AutoTimerIntervalEnabled = true;

_filesArchiveProcessor = new FilesArchiveProcessor(config);
...

```

### Automatic mode with delayed start

*Example of the code to activate the Archive Processor in Automatic mode:*
For automatic mode you have to ensure that in configuration has been setup:
"AutoTimerIntervalEnabled": "**true**",

``` c#
static FilesArchiveProcessor _filesArchiveProcessor;
...
ArchiveProcessorConfig config = LoadConfiguration("FilesArchiveProcessor.json");

// Ensure that Auto Timer Interval has been enabled
config.AutoTimerIntervalEnabled = true;

_filesArchiveProcessor = new FilesArchiveProcessor(config,false);
...

// start later some where

var task = _filesArchiveProcessor.StartAsync();

```


### Manual mode

*Example of the code to activate the Archive Processor in Manual mode:*
``` c#
static FilesArchiveProcessor _filesArchiveProcessor;

...
ArchiveProcessorConfig config = LoadConfiguration("FilesArchiveProcessor.json");

// Ensure that Auto Timer Interval has been disabled
config.AutoTimerIntervalEnabled = false;

_filesArchiveProcessor = new FilesArchiveProcessor(config);
...

  var taskA = _filesArchiveProcessor.LaunchArchiveFilesAsync();
...

  var taskD = _filesArchiveProcessor.LaunchDeleteFromArchiveFilesAsync();
```

## Hosted by WebHost or Host 

Use https://www.nuget.org/packages/DotNet.Host.LogFilesMonitorArchiver/ package.

Create separate configuration file or add to existing **appsettings.config** the root configuration section with name "ArchiveProcessorConfig".

Example of the configuration section:
``` json
{

 "ArchiveProcessorConfig": {
   "AutoTimerIntervalEnabled": "true",
   "ArchiveOnStartup": "true",
   "DelayArchiveInSecondsOnstartUp": 1, 
   "AutoTimerArchiveIntervalMin": 720,
   "ArchiveRules": [
    {
      "SourcePath": "\\Logs\\AppExampleLogSource\\Stat",
      "ArchivePath": "\\Logs\\AppExampleLogSource\\Stat\\Archive",
      "MonitoringNames": [ "archive-able_*.xml", "*.csv" ],
      "MoveToArchiveOlderThanDays": 14,
      "MoveToArchiveAfterReachingFiles": 28,
      "DeleteFromArchiveOlderThanDays": 180,
      "DeleteFromArchiveAfterReachingFiles": 1000
    },
    {
      "UseUtcTime":"false",
      "SourcePath": "\\Logs\\AppExampleLogSource\\FatalError",
      "ArchivePath": "\\Logs\\AppExampleLogSource\\FatalError\\Archive",
      "MonitoringNames": [ "archive-able_*.xml", "*.trace" ],
      "MoveToArchiveOlderThanDays": 7,
      "DeleteFromArchiveOlderThanDays": 360
    }
  ]
 }

}
```


When configuring services add the following initialization code into:

``` c#

public  ... ConfigureServices(IServiceCollection services)
{
...

services.AddLogFilesMonitorArchiver();

...

}

```
or 
When configuring services add the following initialization code into when you want to set a configuration section name :

``` c#

public  ... ConfigureServices(IServiceCollection services)
{
...

services.AddLogFilesMonitorArchiver("SectionOfArchiveProcessorConfig");

...

}

```
### Host Automatic mode

For automatic mode you have to ensure that in configuration has been setup:
"AutoTimerIntervalEnabled": "**true**"

### Manual modes

Other modes can be achieved by using DI i.e. service provider to get instance of singleton of class **FilesArchiveHostedService**.




