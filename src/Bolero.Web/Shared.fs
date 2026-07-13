module Bolero.Web.Shared

open BlazorBootstrap
open Bolero.Html

[<RequireQualifiedAccess>]
module Loading =
  let render () _ = div {
    attr.``class`` "d-flex flex-column align-items-center justify-content-center vh-100"

    comp<Spinner> {
      "Type" => SpinnerType.Border
      "Color" => SpinnerColor.Primary
      "VisuallyHiddenText" => "Loading..."
    }
  }

let getCellClass (value: decimal) =
  [ "text-end"
    if value > 0.0M then
      "table-success" ]
  |> String.concat " "