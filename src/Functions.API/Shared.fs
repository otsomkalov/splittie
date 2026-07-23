module Functions.API.Shared

open Microsoft.Extensions.Logging
open PaymentPlatform
open PaymentPlatform.Splitwise
open Splitwise.Clients
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Domain
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Options
open otsom.fs.Extensions

type ValidationError = { Member: string; Error: string }

type RequestError<'a> =
  | Unauthorized
  | Validation of ValidationError list
  | OperationError of 'a

type TokenUser = { UserId: UserId }

let validateUser (authService: IAuthenticationService) : HttpRequest -> Task<Result<TokenUser, RequestError<_>>> =
  fun req ->
    authService.AuthenticateAsync(req.HttpContext, JwtBearerDefaults.AuthenticationScheme)
    |> Task.map (
      Option.someIf _.Succeeded
      >> Option.bind (_.Principal >> Option.ofObj)
      >> Option.bind (_.Identity >> Option.ofObj)
      >> Option.bind (_.Name >> Option.ofObj)
    )
    |> TaskOption.map (fun userId -> { UserId = UserId userId })
    |> Task.map (Result.ofOption RequestError.Unauthorized)

[<CLIMutable>]
type KeycloakSettings =
  { Domain: string
    Realm: string
    Broker: string }

  static member SectionName = "Keycloak"

[<CLIMutable>]
type TokenResponse =
  { [<JsonPropertyName "access_token">]
    AccessToken: string }

type SpltwisePaymentPlatformFactory
  (
    keycloakOptions: IOptions<KeycloakSettings>,
    httpClientFactory: IHttpClientFactory,
    logger: ILogger<SpltwisePaymentPlatformFactory>,
    request: HttpRequest
  ) =
  let keycloakSettings = keycloakOptions.Value

  let httpClient =
    httpClientFactory.CreateClient(nameof SpltwisePaymentPlatformFactory)

  interface IPaymentPlatformFactory with
    member this.Get _ =
      taskOption {
        let! accessToken =
          match request.Headers.Authorization |> string with
          | token when token.StartsWith "Bearer " -> Some(token.Substring 7)
          | _ -> None

        use splitwiseTokenRequest =
          new HttpRequestMessage(HttpMethod.Get, $"realms/{keycloakSettings.Realm}/broker/{keycloakSettings.Broker}/token")

        splitwiseTokenRequest.Headers.Authorization <- AuthenticationHeaderValue("Bearer", accessToken)

        try
          use! splitwiseTokenResponse = httpClient.SendAsync(splitwiseTokenRequest)

          let! splitwiseToken =
            splitwiseTokenResponse.Content.ReadFromJsonAsync<TokenResponse>()
            |> Task.map Option.ofObj

          let splitwiseClient = SplitwiseClient(splitwiseToken.AccessToken)

          return SplitwisePaymentPlatform splitwiseClient :> IPaymentPlatform
        with e ->
          logger.LogError(e, "Error during building Splitwise client using Keycloak")

          return! None
      }

let spltwisePaymentPlatformFactory keycloakOptions httpClientFactory logger =
  fun request -> SpltwisePaymentPlatformFactory(keycloakOptions, httpClientFactory, logger, request) :> IPaymentPlatformFactory