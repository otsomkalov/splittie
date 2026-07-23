module Functions.API.Startup

#nowarn "20"

open System
open System.Net.Http
open System.Reflection
open System.Security.Claims
open System.Text.Json
open System.Text.Json.Serialization
open Azure.Identity
open Domain.Settings
open Infra
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Microsoft.Extensions.Options
open Parser.OpenAI
open Shared
open otsom.fs.Extensions.DependencyInjection

[<RequireQualifiedAccess>]
module KeyVault =
  [<Literal>]
  let KeyVaultName = "KeyVaultName"

let configureAppConfiguration (builder: FunctionsApplicationBuilder) =
  builder.Configuration.AddAzureKeyVault(
    Uri($"https://{builder.Configuration[KeyVault.KeyVaultName]}.vault.azure.net/"),
    DefaultAzureCredential()
  )

  builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true)

  builder

let configureLogging (builder: FunctionsApplicationBuilder) =
  builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  builder

let private configureFunctionsWebApp (builder: FunctionsApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts -> JSON.FsharpOptions.AddToJsonSerializerOptions opts)

  builder

let private configureServices (builder: FunctionsApplicationBuilder) =
  let cfg, services = builder.Configuration, builder.Services

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  services
    .AddAuthentication()
    .AddJwtBearer(fun opts ->
      opts.TokenValidationParameters.NameClaimType <- ClaimTypes.NameIdentifier

      ())

  services |> Startup.addInfra cfg |> Startup.addOpenAIParser cfg

  services.Configure<ImageSettings>(cfg.GetRequiredSection ImageSettings.SectionName)
  services.Configure<KeycloakSettings>(cfg.GetRequiredSection KeycloakSettings.SectionName)

  services.AddHttpClient(
    nameof SpltwisePaymentPlatformFactory,
    fun (sp: IServiceProvider) (client: HttpClient) ->
      let keycloakSettings = sp.GetRequiredService<IOptions<KeycloakSettings>>().Value

      client.BaseAddress <- (Uri keycloakSettings.Domain)

      ()
  )

  services.BuildSingleton<_, _, _>(spltwisePaymentPlatformFactory)

  services.AddMvcCore().AddJsonOptions(fun opts -> JSON.FsharpOptions.AddToJsonSerializerOptions opts.JsonSerializerOptions)

  builder

let builder =
  FunctionsApplication.CreateBuilder(Environment.GetCommandLineArgs() |> Array.tail).ConfigureFunctionsWebApplication()
  |> configureAppConfiguration
  |> configureFunctionsWebApp
  |> configureLogging
  |> configureServices

builder.Build().Run()