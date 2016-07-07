open System
open System.Configuration

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    Environment.SetEnvironmentVariable("SQLCONNSTR_TESTCONN", "TestConnectionString")
    Environment.SetEnvironmentVariable("APPSETTING_TESTAPP", "TestAppSetting")
    
    Azure.applyAzureEnvironmentToConfigurationManager()
    
    let connectionString = ConfigurationManager.ConnectionStrings.["TESTCONN"]
    let appSetting = ConfigurationManager.AppSettings.["TESTAPP"]
    
    0 // return an integer exit code
