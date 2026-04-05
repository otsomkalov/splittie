namespace Domain

open System
open System.Threading.Tasks

type ReceiptId =
  | ReceiptId of string

  member this.Value = let (ReceiptId id) = this in id

type UserId =
  | UserId of string

  member this.Value = let (UserId id) = this in id

[<RequireQualifiedAccess>]
module Receipt =
  type Item =
    { Name: string
      Quantity: int
      Amount: decimal }

  type Fee = { Type: string; Amount: decimal }

  type New =
    { Id: ReceiptId
      FileName: string
      UserId: UserId
      Date: DateTime }

  type Parsed =
    { Id: ReceiptId
      FileName: string
      UserId: UserId
      Date: DateTime
      Items: Item list
      Fees: Fee list }

  type Unparsed =
    { Id: ReceiptId
      FileName: string
      UserId: UserId
      Date: DateTime }

  type ParsingError = ParsingError of string

type Receipt =
  | New of Receipt.New
  | Parsed of Receipt.Parsed
  | Unparsed of Receipt.Unparsed

  member this.Id =
    match this with
    | New r -> r.Id
    | Parsed r -> r.Id
    | Unparsed r -> r.Id

type GetError =
  | NotFound
  | Access

type ParsingResult =
  { Date: DateTime
    Items: Receipt.Item list
    Fees: Receipt.Fee list }

type IReceiptParser =
  abstract Parse: Uri -> Task<Result<ParsingResult, string>>