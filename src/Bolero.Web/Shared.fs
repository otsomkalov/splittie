module Bolero.Web.Shared

open Bolero.Html

[<RequireQualifiedAccess>]
module Loading =
  let render () _ = div {
    attr.``class`` "d-flex flex-column align-items-center justify-content-center vh-100"

    div {
      attr.``class`` "spinner-border text-primary"
      "role" => "status"

      span {
        attr.``class`` "visually-hidden"
        text "Loading..."
      }
    }
  }

let getCellClass (value: decimal) =
  [ "text-end"
    if value > 0.0M then
      "table-success" ]
  |> String.concat " "