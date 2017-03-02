/// Contains helper methods for getting Suave working nicely with Application Insights.
module Suave.Azure.ApplicationInsights

open Microsoft.ApplicationInsights
open Microsoft.ApplicationInsights.DataContracts
open Microsoft.ApplicationInsights.DependencyCollector
open Microsoft.ApplicationInsights.Extensibility
open Microsoft.ApplicationInsights.Extensibility.Implementation
open Suave
open System
open System.Diagnostics
open System.Configuration

/// The global Telemetry Client that handles all AI requests.
let telemetryClient = TelemetryClient()

/// Builds an operation name from a typical "/api/" endpoint.
let buildApiOperationName (uri:Uri) (meth:HttpMethod) =
    let meth = meth.ToString()
    if uri.AbsolutePath.StartsWith "/api/" && uri.Segments.Length > 2
    then meth + " /api/" + uri.Segments.[2]
    else meth + " " + uri.AbsolutePath

/// Tracks a web part request with App Insights.
let withRequestTracking buildOperationName (webPart:WebPart) context =
    // Start recording a new operation.
    let operation = telemetryClient.StartOperation<RequestTelemetry>(buildOperationName context.request.url context.request.``method``)

    // Set basic AI details
    operation.Telemetry.Url <- context.request.url

    async {                
        try
            try
                // Execute the webpart
                let! context = webPart context
            
                // Set response code + success
                context
                |> Option.iter(fun context ->
                    operation.Telemetry.ResponseCode <- context.response.status.code.ToString()
                    operation.Telemetry.Success <- Nullable (int context.response.status.code < 400))
            
                return context
            with ex ->
                // Hoppla! Fail the request
                operation.Telemetry.ResponseCode <- "500"
                operation.Telemetry.Success <- Nullable false

                // log the error
                let telemetry = ExceptionTelemetry(ex)
                telemetryClient.TrackException telemetry

                raise ex
                return None
        finally
            telemetryClient.StopOperation operation
    }

/// Configuration for setting up App Insights
type AIConfiguration =
    { /// The Application Insights key
      AppInsightsKey : string
      /// Whether to use Developer Mode with AI - will send more frequent messages at cost of higher CPU etc.
      DeveloperMode : bool
      /// Track external dependencies e.g. SQL, HTTP etc. etc.
      TrackDependencies : bool }

/// Turn on dependency tracking
let startMonitoring configuration =
    TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode <- Nullable configuration.DeveloperMode
    TelemetryConfiguration.Active.InstrumentationKey <- configuration.AppInsightsKey
    
    if configuration.TrackDependencies then
        let dependencyTracking = new DependencyTrackingTelemetryModule()
        dependencyTracking.Initialize TelemetryConfiguration.Active