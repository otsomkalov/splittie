namespace Functions.API.Functions

open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Functions.API.Shared
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open PaymentPlatform

type UserFunctions(authSvc: IAuthenticationService, paymentPlatformFactory: HttpRequest -> IPaymentPlatformFactory) =
  [<Function("ListFriends")>]
  member this.ListFriends([<HttpTrigger("GET", Route = "users/me/friends")>] request: HttpRequest) : Task<IActionResult> =
    let handler (user: TokenUser) =
      taskResult {
        let paymentPlatformFactory = paymentPlatformFactory request

        let! paymentPlatform =
          paymentPlatformFactory.Get(user.UserId.Value |> UserId)
          |> TaskResult.requireSome (RequestError.OperationError "Payment platform not found")

        return! paymentPlatform.ListFriends()
      }

    validateUser authSvc request
    |> TaskResult.bind handler
    |> Task.map (function
      | Ok receipt -> OkObjectResult receipt
      | Error RequestError.Unauthorized -> UnauthorizedResult()
      | Error(RequestError.OperationError e) -> BadRequestObjectResult(e)
      | Error(RequestError.Validation errors) -> BadRequestObjectResult(errors))