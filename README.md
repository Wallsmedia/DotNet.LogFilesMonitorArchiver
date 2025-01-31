### .NET Core Logging Files Monitor and Archiver
Long running services can generate lots of logging and temporary files. 
They become outdated and should be deleted by the dedicated period of time, like 180 days. 
".NET Core Logging Files Monitor and Archiver" provides convenient and flexible features for log files archiving and removing. 
".NET Core Logging Files Monitor and Archiver" is a long running monitor which scans file directories, archives them and deletes the old.
It keeps file directories in tidy state by the rules.


#### Version 9.0.0
- Add support of .Net 9.x
- Obsolete "MoveToArchiveAfterReachingFiles" replaced with MoveToArchiveAfterReachingNumber
- Obsolete "DeleteFromArchiveAfterReachingFiles" replaced with DeleteFromArchiveAfterReachingNumber
- Add MonitoringMode:
    - FilesOnly 
    - SubdirectoriesOnly
- Property AutoTimerIntervalEnabled is not supported, Auto Timer Interval always on.


**ASP.NET Core WebHost and Host edition of ".NET Core Logging Files Monitor and Archiver":**
https://www.nuget.org/packages/DotNet.Host.LogFilesMonitorArchiver/


## Archive scenarios

The "DotNet.LogFilesMonitorArchiver" package allows to implement  the following archive scenarios:

1. Monitoring the "**SourcePath**" directory by the list of search pattern "**MonitoringNames**" names.

2. For each matched file name in the "**SourcePath**" directory apply the rules:
- Move it into the archive directory "**ArchivePath**" if the file created date is older than configured "**MoveToArchiveOlderThanDays**" days (default value 7).
- Move it into the archive directory "**ArchivePath**" if the number of files is greater than configured "**MoveToArchiveAfterReachingNumber**" number S(default value is int.MaxValue).

3. For each matched file name in the "**ArchivePath**" directory apply the rules: 
- Delete it from the archive directory "**ArchivePath**" if the file created date is older than configured "**DeleteFromArchiveOlderThanDays**" days (default value 30).
- Delete it from the archive directory "**ArchivePath**" if the number of files is greater than configured "**DeleteFromArchiveAfterReachingNumber**" number (default value is int.MaxValue).


## Hosted via IHostedService

Use https://www.nuget.org/packages/DotNet.Host.LogFilesMonitorArchiver/ package.

Create separate configuration file or add to existing **appsettings.config** the root configuration section with name "ArchiveProcessorConfig".

Example of the configuration section:
``` JSON
{

 "ArchiveProcessorConfig": {
   "ArchiveOnStartup": "true",
   "DelayArchiveInSecondsOnstartUp": 1, 
   "AutoTimerArchiveIntervalMin": 720,
   "ArchiveRules": [
    {
      "SourcePath": "\\Logs\\AppExampleLogSource\\Stat",
      "ArchivePath": "\\Logs\\AppExampleLogSource\\Stat\\Archive",
      "MonitoringNames": [ "archive-able_*.xml", "*.csv" ],
      "MoveToArchiveOlderThanDays": 14,
      "MoveToArchiveAfterReachingNumber": 28,
      "DeleteFromArchiveOlderThanDays": 180,
      "DeleteFromArchiveAfterReachingNumber": 1000
    },
    {
      "UseUtcTime":"false",
      "SourcePath": "\\Logs\\AppExampleLogSource\\FatalError",
      "ArchivePath": "\\Logs\\AppExampleLogSource\\FatalError\\Archive",
      "MonitoringNames": [ "archive-able_*.xml", "*.trace" ],
      "MoveToArchiveOlderThanDays": 7,
      "DeleteFromArchiveOlderThanDays": 360
    },
    {
     "SourcePath": "LogDirs",
     "ArchivePath": "DirsArchive",
     "MonitoringNames": [],
     "MonitoringMode": 1,
     "MoveToArchiveOlderThanDays": 2,
     "MoveToArchiveAfterReachingNumber": 20,
     "DeleteFromArchiveOlderThanDays": 5,
     "DeleteFromArchiveAfterReachingNumber": 30
    }

  ]
 }
}
```


When configuring services add the following initialization code into:

``` CSHARP

var builder = WebApplication.CreateBuilder(args);

....

builder.Services.AddLogFilesMonitorArchiver();

builder.Services.AddLogFilesMonitorArchiverConfiguration(builder.Configuration);

```




