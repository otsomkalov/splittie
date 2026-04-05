module Domain.Repos

open System.Threading.Tasks

type IReceiptRepo =
  abstract Save: Receipt -> Task<unit>
  abstract Get: ReceiptId -> Task<Receipt option>
  abstract QueueParsing: Receipt.New -> Task<unit>