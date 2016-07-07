module System.Configuration.Azure

open System
open System.Configuration
open System.Reflection

module private AzureConfiguration =
    let (|AppSetting|_|) (key:string, value:string) =
        let token = "APPSETTING_"
        if key.StartsWith token then Some (key.Substring token.Length, value)
        else None
    let (|ConnectionString|_|) (key:string, value:string) =
        let token = "CONNSTR_"
        if key.Contains token then
            let connType = key.Substring(0, key.IndexOf token)
            Some(connType, key.Substring (connType + token).Length, value)
        else None
    let (|DetailedConnectionString|_|) = function
        | ConnectionString ("SQL", name, connection)
        | ConnectionString ("SQLAZURE", name, connection) ->
            Some (DetailedConnectionString(name, connection, Some "System.Data.SqlClient"))
        | ConnectionString ("MYSQL", name, connection) ->
            Some (DetailedConnectionString(name, connection, Some "System.Data.MySqlClient"))
        | ConnectionString ("CUSTOM", name, connection) -> Some (DetailedConnectionString(name, connection, None))
        | _ -> None

    let getEnvironmentSettings() =
        seq {
            let enumerator = Environment.GetEnvironmentVariables().GetEnumerator()
            while enumerator.MoveNext() do
                yield (string enumerator.Key, string enumerator.Value) }

    let private allowAddingNewConnectionStrings() =
        typeof<ConfigurationElementCollection>
            .GetField("bReadOnly", BindingFlags.Instance ||| BindingFlags.NonPublic)
            .SetValue(ConfigurationManager.ConnectionStrings, false)

    let private makeConnectionStringWriteable (connectionStringSettings:ConnectionStringSettings) =
        typeof<ConfigurationElement>
            .GetField("_bReadOnly", BindingFlags.Instance ||| BindingFlags.NonPublic)
            .SetValue(connectionStringSettings, value = false)               

    let applyToConfigurationManager settings =
        allowAddingNewConnectionStrings()
        for setting in settings do
            match setting with
            | AppSetting(key, value) -> ConfigurationManager.AppSettings.[key] <- value
            | DetailedConnectionString (name, connection, provider) ->
                match ConfigurationManager.ConnectionStrings.[name] |> Option.ofObj with
                | None ->
                    let connectionStringSettings =
                        match provider with
                        | Some provider -> ConnectionStringSettings(name, connection, provider)
                        | None -> ConnectionStringSettings(name, connection)
                    ConfigurationManager.ConnectionStrings.Add connectionStringSettings
                | Some setting ->
                    makeConnectionStringWriteable setting
                    setting.ConnectionString <- connection                
                    provider |> Option.iter(fun provider -> setting.ProviderName <- provider)
            | _ -> ()

/// Applies Azure Application Settings stored as Environment variables into the Configuration Manager.
let applyAzureEnvironmentToConfigurationManager =
    AzureConfiguration.getEnvironmentSettings >> AzureConfiguration.applyToConfigurationManager