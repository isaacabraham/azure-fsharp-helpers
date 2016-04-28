# azure-fsharp-helpers
A set of files which help provide better support for Azure with a number of F# projects.

## AppInsights on Suave
paket usage: ``github isaacabraham/azure-fsharp-helpers src/appinsights/suave-appinsights.fs``

This file adds a new ``Suave.Azure.ApplicationInsights`` module which supports: -
  * Request logging for any WebPart (timing, outcome, exception handling etc.)
  * Dependency tracking
  * Configuring the Telemetry Client with the appropriate AppInsights key
  * Provisioning of a Telemetry Client

Example usage: -

```fsharp
 choose
  [ GET >=> OK "HELLO WORLD"
    POST >=> OK "BYE BYE" ] // Basic Suave routes
|> ApplicationInsights.withRequestTracking ApplicationInsights.buildApiOperationName // App Insights into it
```

## Azure App Service Configuration settings
paket usage: ``github isaacabraham/azure-fsharp-helpers src/configuration.fs``

This file contains a single function ``applyAzureEnvironmentToConfigurationManager`` that lives in ``System.Configuration.Azure``. It migrates all Azure App Setting settings that exist as Environment settings into the Configuration Manager. This is useful when using e.g. a Suave application hosted in the Azure App Service as you can now see Application Settings through the portal as normal and have them show up in your application.

## Azure App Service Diagnostics
paket usage: ``github isaacabraham/azure-fsharp-helpers src/diagnostics.fs``

This file contains a single function ``addAzureAppServicesTraceListeners`` that lives in ``System.Diagnostics.Tracing.Azure``. It automatically tries to add Trace Listeners for Azure Drive, Table and Blob, which relates to the Logging settings you can activate within an Azure App Service; in other words, you can log to blob, tables or the local file system (as well as the live log streaming service).
