### Built-in into the Application Auto log files monitoring and archiver background processor 

https://www.nuget.org/packages/DotNet.LogFilesMonitorArchiver/

Long running services generates lots of log files. 
They become outdated and should be deleted by the dedicated period of time, like 180 days. 
Unfortunately, It is hard to find the logger framework that provides convenient and flexible features for log files archiving and removing. 

The "DotNet.LogFilesMonitorArchiver" package allows to implement  the following archive scenarios:

1. Monitoring the "**SourcePath**" directory by the list of search pattern "**MonitoringNames**" names.

2. For each matched file name in the "**SourcePath**" directory apply the rules:
- Move it into the archive directory "**ArchivePath**" if the file created date is older than configured "**MoveToArchiveOlderThanDays**" days (default value 7).
- Move it into the archive directory "**ArchivePath**" if the number of files is greater than configured "**MoveToArchiveAfterReachingFiles**" number S(default value is int.MaxValue).

3. For each matched file name in the "**ArchivePath**" directory apply the rules: 
- Delete it from the archive directory "**ArchivePath**" if the file created date is older than configured "**DeleteFromArchiveOlderThanDays**" days (default value 30).
- Delete it from the archive directory "**ArchivePath**" if the number of files is greater than configured "**DeleteFromArchiveAfterReachingFiles**" number (default value is int.MaxValue).

Here is example of the JSON configuration file that can be use to define configuration.


*Example of the JSON configuration file:*
```
{
  "AutoTimerIntervalEnabled": "true",
  "ArchiveOnStartup": "true",
  "DelayArchiveInSecondsOnstartUp": 1, //  1 min
  "AutoTimerArchiveIntervalMin": 720,  // 12 hours
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
```

*Example of the code to load configuration  file:*
```
 public ArchiveProcessorConfig LoadConfiguration(string name)
 {
     ArchiveProcessorConfig config = null;
     string path = GetBasePath();
     path = Path.Combine(path, name);
     var build = new ConfigurationBuilder().AddJsonFile(path, false);
     var cfg = build.Build();
     config = cfg.Get<ArchiveProcessorConfig>();
     path = GetBasePath();
     return config;
 }
 private string GetBasePath()
 {
     var assembly = GetType().GetTypeInfo().Assembly;
     return Path.GetDirectoryName(assembly.Location);
 }

```

*Example of the code to activate the Archive Processor:*
```
static FilesArchiveProcessor _filesArchiveProcessor;
...
ArchiveProcessorConfig config = LoadConfiguration("FilesArchiveProcessor.json");
_filesArchiveProcessor = new FilesArchiveProcessor(config);
...

```

