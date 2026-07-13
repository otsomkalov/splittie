module Bolero.Web.Components

open BlazorBootstrap
open Bolero
open Bolero.Html
open Bolero.Web.Programs
open Bolero.Web.Repos
open Domain
open Elmish
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.Routing
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Components.WebAssembly.Authentication

[<Route("")>]
type Home() =
  inherit Component()

  override this.Render() = div { "Home" }

[<Authorize; Route("receipts/new")>]
type NewReceipt(env: IEnv, navManager: NavigationManager) =
  inherit ProgramComponent<Receipt.New.Model, Receipt.New.Message>()

  override this.Program =
    Program.mkProgram Receipt.New.init (Receipt.New.update navManager env) Receipt.New.view
    |> Program.withConsoleTrace

[<Authorize; Route("receipts/{receiptId}")>]
type ReceiptDetails(env: IEnv) =
  inherit ProgramComponent<Receipt.Details.Model, Receipt.Details.Message>()

  [<Parameter>]
  member val ReceiptId = Unchecked.defaultof<string> with get, set

  override this.CssScope = CssScopes.receipt

  override this.Program =
    Program.mkProgram (Receipt.Details.init (ReceiptId this.ReceiptId)) (Receipt.Details.update env) Receipt.Details.view
    |> Program.withConsoleTrace

[<Authorize; Route("profile")>]
type Profile() =
  inherit Component()

  override this.Render() = comp<AuthorizeView> {
    attr.fragmentWith "Authorized" (fun (state: AuthenticationState) -> div { sprintf "Hello %s" state.User.Identity.Name })
    attr.fragmentWith "NotAuthorized" (fun (_: AuthenticationState) -> p { "You are not authorized" })
  }

[<AllowAnonymous; Route("/authentication/{action}")>]
type Authentication() =
  inherit Component()

  [<Parameter>]
  member val Action: string | null = null with get, set

  override this.Render() = comp<RemoteAuthenticatorView> { "Action" => this.Action }

type RedirectToLogin(navManager: NavigationManager) =
  inherit Component()

  override this.OnInitialized() =
    navManager.NavigateToLogin("authentication/login")

    ()

  override this.Render() = empty ()

[<RequireQualifiedAccess>]
module NotFound =
  let view () = div {
    comp<PageTitle> { "Not found" }

    comp<LayoutView> {
      "Layout" => typeof<Layout.Layout>

      comp<Alert> {
        "Color" => AlertColor.Warning
        "Sorry, there's nothing at this address."
      }
    }
  }

type Root() =
  inherit Component()

  let unauthorizedView (authenticationState: AuthenticationState) =
    match
      authenticationState.User.Identity
      |> Option.ofObj
      |> Option.map _.IsAuthenticated
    with
    | Some true -> comp<Alert> {
        "Color" => AlertColor.Warning
        "You are not authorized to access this page."
      }
    | _ -> comp<RedirectToLogin> { attr.empty () }

  override this.Render() = comp<CascadingAuthenticationState> {
    comp<Router> {
      "AppAssembly" => typeof<Root>.Assembly

      attr.fragmentWith "Found" (fun (routeData: RouteData) -> concat {
        comp<AuthorizeRouteView> {
          "RouteData" => routeData
          "DefaultLayout" => typeof<Layout.Layout>

          attr.fragmentWith "NotAuthorized" unauthorizedView
        }

        comp<FocusOnNavigate> {
          "RouteData" => routeData
          "Selector" => "h1"
        }
      })

      attr.fragment "NotFound" (NotFound.view ())
    }
  }