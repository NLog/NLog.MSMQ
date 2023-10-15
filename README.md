# NLog.MSMQ
NLog [MSMQ Target](https://github.com/NLog/NLog/wiki/MSMQ-target) for writing to Microsoft Message Queue (MSMQ)

[![Version](https://badge.fury.io/nu/NLog.MSMQ.svg)](https://www.nuget.org/packages/NLog.MSMQ)
[![AppVeyor](https://img.shields.io/appveyor/ci/nlog/NLog-MSMQ/master.svg)](https://ci.appveyor.com/project/nlog/NLog-MSMQ/branch/master)


### How to install

1) Install the package

    `Install-Package NLog.MSMQ` or in your csproj:

    ```xml
    <PackageReference Include="NLog.MSMQ" Version="5.*" />
    ```

2) Add to your nlog.config:

    ```xml
    <extensions>
        <add assembly="NLog.MSMQ"/>
    </extensions>
    ```

   Alternative register from code using [fluent configuration API](https://github.com/NLog/NLog/wiki/Fluent-Configuration-API):

    ```csharp
    LogManager.Setup().SetupExtensions(ext => {
       ext.RegisterTarget<NLog.Targets.MessageQueueTarget>();
    });
    ```

### Example NLog.config file

Example of using the target "MSMQ" in `nlog.config`-file:

```xml
<nlog>
    <extensions>
        <add assembly="NLog.MSMQ"/>
    </extensions>
    <targets>
        <target name="msmq" xsi:type="MSMQ" layout="${message}"  />
    </targets>
    <rules>
        <logger minLevel="Info" writeTo="msmq" />
    </rules>
</nlog>
```
