[<RequireQualifiedAccess>]
module Infra.Entities

open System
open Domain
open MongoDB.Bson.Serialization.Attributes

[<CLIMutable>]
type Item =
  { Name: string
    Quantity: int
    Amount: decimal }

  static member FromDomain(item: Receipt.Item) =
    { Name = item.Name
      Quantity = item.Quantity
      Amount = item.Amount }

  member this.ToDomain() : Receipt.Item =
    { Name = this.Name
      Quantity = this.Quantity
      Amount = this.Amount }

[<CLIMutable>]
type Fee =
  { Name: string
    Amount: decimal }

  static member FromDomain(fee: Receipt.Fee) =
    { Name = fee.Type; Amount = fee.Amount }

  member this.ToDomain() : Receipt.Fee =
    { Type = this.Name
      Amount = this.Amount }

[<CLIMutable; BsonIgnoreExtraElements>]
type Receipt =
  { [<BsonId; BsonElement "_id">]
    Id: string
    UserId: string
    Date: DateTime
    FileName: string
    Items: Item seq | null
    Fees: Fee seq | null }

  static member FromDomain(receipt: Domain.Receipt) =
    let newFromDomain (receipt: Receipt.New) =
      { Id = receipt.Id.Value
        UserId = receipt.UserId.Value
        Date = receipt.Date
        FileName = receipt.FileName
        Items = null
        Fees = null }

    let parsedFromDomain (receipt: Receipt.Parsed) : Receipt =
      { Id = receipt.Id.Value
        UserId = receipt.UserId.Value
        Date = receipt.Date
        FileName = receipt.FileName
        Items = receipt.Items |> List.map Item.FromDomain
        Fees = receipt.Fees |> List.map Fee.FromDomain }

    let unparsedFromDomain (receipt: Receipt.Unparsed) =
      { Id = receipt.Id.Value
        UserId = receipt.UserId.Value
        Date = receipt.Date
        FileName = receipt.FileName
        Items = null
        Fees = null }

    match receipt with
    | New r -> newFromDomain r
    | Parsed r -> parsedFromDomain r
    | Unparsed unparsed -> unparsedFromDomain unparsed

  member this.ToDomain() : Domain.Receipt =
    match this with
    | { Items = items; Fees = fees } when not (isNull items || isNull fees) ->
      Parsed
        { Id = ReceiptId this.Id
          FileName = this.FileName
          UserId = UserId this.UserId
          Date = this.Date
          Items = items |> Seq.map _.ToDomain() |> List.ofSeq
          Fees = fees |> Seq.map _.ToDomain() |> List.ofSeq }
    | _ ->
      New
        { Id = ReceiptId this.Id
          FileName = this.FileName
          UserId = UserId this.UserId
          Date = this.Date }

[<CLIMutable>]
type ParseReceiptRequest = { Id: string }