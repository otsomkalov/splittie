[<RequireQualifiedAccess>]
module Infra.Startup

#nowarn "20"

open Azure.Storage.Blobs
open Azure.Storage.Queues
open Domain.Repos
open Infra.Settings
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open MongoDB.Driver
open otsom.fs.Extensions.DependencyInjection

let private configureMongoClient (cfg: IConfiguration) =
  new MongoClient(cfg.GetConnectionString "Database") :> IMongoClient

let private configureMongoDatabase (options: IOptions<DatabaseSettings>) (mongoClient: IMongoClient) =
  let settings = options.Value

  mongoClient.GetDatabase(settings.Name)

let private configureStorageClient (cfg: IConfiguration) =
  cfg.GetConnectionString("Storage") |> BlobServiceClient

let private configureQueueClient (cfg: IConfiguration) =
  cfg.GetConnectionString("Storage") |> QueueServiceClient

let addInfra (cfg: IConfiguration) (services: IServiceCollection) =
  services.Configure<DatabaseSettings>(cfg.GetRequiredSection DatabaseSettings.SectionName)
  services.Configure<Domain.Settings.StorageSettings>(cfg.GetRequiredSection Domain.Settings.StorageSettings.SectionName)

  services.BuildSingleton<IMongoClient, IConfiguration>(configureMongoClient)
  services.BuildSingleton<IMongoDatabase, IOptions<DatabaseSettings>, IMongoClient>(configureMongoDatabase)
  services.BuildSingleton<IMongoCollection<Entities.Receipt>, IMongoDatabase>(fun db -> db.GetCollection "receipts")
  services.BuildSingleton<BlobServiceClient, IConfiguration>(configureStorageClient)
  services.BuildSingleton<QueueServiceClient, IConfiguration>(configureQueueClient)

  services.AddSingleton<IReceiptRepo, ReceiptRepo>()