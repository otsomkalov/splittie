module Bolero.Web.Startup

#nowarn "20"

open System
open System.Net
open System.Net.Http
open System.Net.Http.Json
open Domain
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Bolero.Web.Repos
open Shared

type Env(httpClientFactory: IHttpClientFactory, logger: ILogger<Env>) =
  let maxFileSize = 1L * 1_024L * 1_024L // 1 MB
  let client = httpClientFactory.CreateClient(nameof Env)

  interface IEnv with
    member this.GetReceipt(receiptId) = task {
      try
        let! result = client.GetFromJsonAsync<Receipt>(sprintf "receipts/%s" receiptId, JSON.SerializerOptions)

        return Some result
      with
      | :? HttpRequestException as requestException when requestException.StatusCode = HttpStatusCode.NotFound -> return None
      | e ->
        logger.LogError(e, "Error during getting receipt")

        return None
    }

type APIAuthorizationMessageHandler(accessTokenProvider: IAccessTokenProvider, navigationManager: NavigationManager, cfg: IConfiguration) =
  inherit AuthorizationMessageHandler(accessTokenProvider, navigationManager)

  do base.ConfigureHandler([ cfg["API:Url"] ]) |> ignore

let configureHttpClient (serviceProvider: IServiceProvider) (client: HttpClient) =
  let cfg = serviceProvider.GetRequiredService<IConfiguration>()

  client.BaseAddress <- Uri(cfg["API:Url"])

  client.DefaultRequestHeaders.Add("x-functions-key", cfg["API:Key"])

  ()

let builder = WebAssemblyHostBuilder.CreateDefault()

builder.Services.AddOidcAuthentication(fun options ->

  builder.Configuration.Bind("Oidc", options.ProviderOptions)

  options.ProviderOptions.AdditionalProviderParameters.Add("audience", builder.Configuration["Oidc:Audience"])

  ())

builder.Services.AddScoped<APIAuthorizationMessageHandler>()

builder.Services.AddScoped<IEnv, Env>()

builder.Services.AddHttpClient(nameof Env, configureHttpClient).AddHttpMessageHandler<APIAuthorizationMessageHandler>()

builder.Logging.SetMinimumLevel(LogLevel.Information)

builder.RootComponents.Add<Components.Root>("#root")

builder.Build().RunAsync()