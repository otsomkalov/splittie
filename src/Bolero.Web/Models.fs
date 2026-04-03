module Bolero.Web.Models

open System

type ReceiptId =
  | ReceiptId of string

  member this.Value = let (ReceiptId id) = this in id

type ItemId =
  | ItemId of string

  member this.Value = let (ItemId id) = this in id

type FeeId =
  | FeeId of string

  member this.Value = let (FeeId id) = this in id

[<RequireQualifiedAccess>]
module Receipt =
  type Item =
    { Id: ItemId
      Name: string
      Quantity: decimal
      Amount: decimal }

  type Fee =
    { Id: FeeId
      Type: string
      Amount: decimal }

  type Parsed =
    { Id: ReceiptId
      Date: DateTime
      Items: Item list
      Fees: Fee list
      Total: decimal }

type Receipt = Parsed of Receipt.Parsed

let mockReceipt: Receipt.Parsed =
  { Id = ReceiptId "1"
    Date = DateTime.Now
    Items =
      [ { Id = ItemId "1"
          Name = "Item 1"
          Quantity = 1.0M
          Amount = 10.0M }
        { Id = ItemId "2"
          Name = "Item 2"
          Quantity = 2.0M
          Amount = 20.0M } ]
    Fees =
      [ { Id = FeeId "1"
          Type = "Fee 1"
          Amount = 10.0M } ]
    Total = 40.0M }