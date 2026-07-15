[<RequireQualifiedAccess>]
module Bolero.Web.Layout

open BlazorBootstrap
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.Routing
open Microsoft.AspNetCore.Components.WebAssembly.Authentication

[<RequireQualifiedAccess>]
module internal HeaderLinks =
  let view () =
    ul {
      attr.``class`` "navbar-nav"

      li {
        attr.``class`` "nav-item"

        navLink NavLinkMatch.All {
          attr.``class`` "nav-link"
          attr.href "/"

          "Home"
        }
      }

      li {
        attr.``class`` "nav-item"

        navLink NavLinkMatch.All {
          attr.``class`` "nav-link"
          attr.href "receipts/new"

          "New Receipt"
        }
      }
    }

[<RequireQualifiedAccess>]
module internal HeaderLogin =
  let view () =
    li {
      attr.``class`` "nav-item"

      navLink NavLinkMatch.All {
        attr.``class`` "nav-link"
        attr.href "authentication/login"

        "Login"
      }
    }

[<RequireQualifiedAccess>]
module internal HeaderAuth =
  let view (navManager: NavigationManager) (state: AuthenticationState) =
    li {
      attr.``class`` "nav-item dropdown"

      navLink NavLinkMatch.All {
        attr.``class`` "nav-link dropdown-toggle"
        attr.href "#"
        "role" => "button"
        "data-bs-toggle" => "dropdown"
        "aria-expanded" => "false"

        string state.User.Identity.Name
      }

      ul {
        attr.``class`` "dropdown-menu"

        li {
          attr.``class`` "dropdown-item"

          navLink NavLinkMatch.All {
            attr.``class`` "nav-link"
            attr.href "profile"

            "Profile"
          }
        }

        li {
          attr.``class`` "dropdown-item"

          button {
            attr.``class`` "nav-link"
            on.click (fun _ -> navManager.NavigateToLogout("authentication/logout"))

            "Logout"
          }
        }
      }
    }

module internal Header =
  let render navManager =
    nav {
      attr.``class`` "navbar bg-primary navbar-expand-lg mb-2"
      "data-bs-theme" => "dark"

      div {
        attr.``class`` "container-fluid"

        a {
          attr.``class`` "navbar-brand"
          attr.href "/"
          "GlovoSplit"
        }

        button {
          attr.``class`` "navbar-toggler"
          attr.``type`` "button"
          "data-bs-toggle" => "collapse"
          "data-bs-target" => "#navbarNavDropdown"
          "aria-controls" => "navbarNavDropdown"
          "aria-expanded" => "false"
          "aria-label" => "Toggle navigation"

          span { attr.``class`` "navbar-toggler-icon" }
        }

        div {
          attr.``class`` "collapse navbar-collapse justify-content-between"
          attr.id "navbarNavDropdown"

          HeaderLinks.view ()

          ul {
            attr.``class`` "navbar-nav"

            comp<AuthorizeView> {
              attr.fragmentWith "Authorized" (fun (state: AuthenticationState) -> HeaderAuth.view navManager state)
              attr.fragmentWith "NotAuthorized" (fun (_: AuthenticationState) -> HeaderLogin.view ())
            }
          }
        }
      }
    }

[<RequireQualifiedAccess>]
module internal Layout =
  let internal render navManager (body: RenderFragment) =
    concat {
      comp<Toasts> {
        attr.``class`` "p-3"

        "AutoHide" => true
        "Delay" => 5000
        "Placement" => ToastsPlacement.TopRight
      }

      Header.render navManager

      div {
        attr.``class`` "container-fluid"

        body
      }
    }

type Layout() =
  inherit LayoutComponentBase()

  [<Inject>]
  member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set

  override this.BuildRenderTree(builder) =
    base.BuildRenderTree(builder)

    Layout.render this.NavigationManager this.Body
    |> _.Invoke(this, builder, 0)
    |> ignore