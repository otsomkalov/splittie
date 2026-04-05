module Runners.AzureFunctions.Shared

open System.Threading.Tasks
open Domain
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http
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
    |> Task.map (Option.someIf _.Succeeded)
    |> Task.map (Option.bind (_.Principal >> Option.ofObj))
    |> Task.map (Option.bind (_.Identity >> Option.ofObj))
    |> Task.map (Option.bind (_.Name >> Option.ofObj))
    |> TaskOption.map (fun name -> name.Split "|" |> Array.last)
    |> TaskOption.map (fun userId -> { UserId = UserId userId })
    |> Task.map (Result.ofOption RequestError.Unauthorized)