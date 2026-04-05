module Bolero.Web.Models

type ItemId =
  | ItemId of string

  member this.Value = let (ItemId id) = this in id

type FeeId =
  | FeeId of string

  member this.Value = let (FeeId id) = this in id