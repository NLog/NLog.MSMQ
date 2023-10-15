# NLog MSMQ Target

NLog MSMQ Target writes log messages to the Microsoft Message Queuing (MSMQ).

See the [NLog Wiki](https://github.com/NLog/NLog/wiki/MSMQ-target) for available options and examples.

## Register Extension

NLog will only recognize type-alias `msmq` when loading from `NLog.config`-file, if having added extension to `NLog.config`-file:

```xml
<extensions>
    <add assembly="NLog.MSMQ"/>
</extensions>
```

Alternative register from code using [fluent configuration API](https://github.com/NLog/NLog/wiki/Fluent-Configuration-API):

```csharp
LogManager.Setup().SetupExtensions(ext => ext.RegisterTarget<NLog.Targets.MessageQueueTarget>());
```
