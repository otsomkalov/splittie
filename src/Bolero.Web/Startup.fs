module Bolero.Web.Startup

#nowarn "20"

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open BlazorBootstrap
open Domain
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Bolero.Web.Repos
open Shared

[<CLIMutable>]
type UploadReceiptResponse = { Id: string }

type Env(httpClientFactory: IHttpClientFactory, toastService: ToastService, logger: ILogger<Env>) =
  let maxFileSize = 2L * 1_024L * 1_024L // 2 MB
  let client = httpClientFactory.CreateClient(nameof Env)

  [<Literal>]
  let receiptsRoute = "receipts"

  interface IEnv with
    member this.GetReceipt(receiptId) = task {
      try
        let! result = client.GetFromJsonAsync<Receipt>($"{receiptsRoute}/{receiptId}", JSON.SerializerOptions)

        return Some result
      with
      | :? HttpRequestException as requestException when requestException.StatusCode = HttpStatusCode.NotFound -> return None
      | e ->
        logger.LogError(e, "Error during getting receipt")

        return None
    }

    member this.UploadReceipt(receiptImage) = task {
      use formData = new MultipartFormDataContent()
      use fileStream = receiptImage.OpenReadStream(maxFileSize)
      use streamContent = new StreamContent(fileStream)

      streamContent.Headers.ContentType <- MediaTypeHeaderValue.Parse receiptImage.ContentType

      formData.Add(streamContent, "receipt", receiptImage.Name)

      let! result = client.PostAsync(receiptsRoute, formData)

      logger.LogInformation("Receipt uploaded successfully, {Status}", result.StatusCode)

      let! response = result.Content.ReadFromJsonAsync<UploadReceiptResponse>(JSON.SerializerOptions)

      return ReceiptId(response.Id)
    }

    member this.ShowNotification(toast) = toastService.Notify(toast)


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

builder.Services.AddBlazorBootstrap()

builder.RootComponents.Add<Components.Root>("#root")

builder.Build().RunAsync()