namespace Infra

open Azure.Storage.Queues
open Domain
open Domain.Repos
open Domain.Settings
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Options
open MongoDB.Driver
open Shared

type ReceiptRepo
  (collection: IMongoCollection<Entities.Receipt>, queueService: QueueServiceClient, storageOptions: IOptions<StorageSettings>) =
  let storageSettings = storageOptions.Value
  let queue = queueService.GetQueueClient(storageSettings.Queue)

  interface IReceiptRepo with
    member this.QueueParsing(receipt) = task {
      let msg: Entities.ParseReceiptRequest = { Id = receipt.Id.Value }

      do! queue.SendMessageAsync(JSON.serialize msg) |> Task.ignore
    }

    member this.Get(ReceiptId id) =
      let filter = Builders.Filter.Eq("_id", id)

      collection.Find(filter).FirstOrDefaultAsync()
      |> Task.map (Option.ofObj >> Option.map _.ToDomain())

    member this.Save(receipt) = task {
      let updateOptions = ReplaceOptions(IsUpsert = true)
      let filter = Builders.Filter.Eq("_id", receipt.Id.Value)

      do!
        collection.ReplaceOneAsync(filter, Entities.Receipt.FromDomain receipt, updateOptions)
        |> Task.ignore
    }