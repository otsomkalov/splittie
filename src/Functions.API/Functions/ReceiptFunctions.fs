namespace Functions.API.Functions

open System.Threading.Tasks
open Azure.Storage.Blobs
open Azure.Storage.Blobs.Models
open Azure.Storage.Sas
open Domain
open Domain.Repos
open Domain.Settings
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Functions.API.Shared

type ReceiptFunctions
  (
    storageClient: BlobServiceClient,
    imageOptions: IOptions<ImageSettings>,
    storageOptions: IOptions<StorageSettings>,
    authService: IAuthenticationService,
    receiptRepo: IReceiptRepo,
    parser: IReceiptParser,
    logger: ILogger<ReceiptFunctions>
  ) =
  let imageSettings = imageOptions.Value
  let storageSettings = storageOptions.Value

  [<Function("CreateReceipt")>]
  member this.CreateReceipt([<HttpTrigger("post", Route = "receipts")>] request: HttpRequest) : Task<IActionResult> =

    let handler: TokenUser -> Task<Result<Receipt.New, RequestError<string>>> =
      fun user -> task {
        let receiptFile = request.Form.Files.GetFile("receipt") |> Option.ofObj

        match receiptFile with
        | Some receiptFile' when imageSettings.SupportedMimeTypes |> Seq.contains receiptFile'.ContentType ->

          let containerClient =
            storageClient.GetBlobContainerClient(storageSettings.Container)

          let id = System.Guid.NewGuid() |> string

          let extension = MimeTypes.MimeTypeMap.GetExtension(receiptFile'.ContentType)
          let blobName = sprintf "%s%s" id extension

          use imageStream = receiptFile'.OpenReadStream()

          let blob = containerClient.GetBlobClient blobName

          let blobHeaders = BlobHttpHeaders(ContentType = receiptFile'.ContentType)

          do! blob.UploadAsync(imageStream, blobHeaders) |> Task.ignore

          let newReceipt: Receipt.New =
            { Id = ReceiptId(id)
              FileName = blobName
              UserId = user.UserId
              Date = System.DateTime.UtcNow }

          do! receiptRepo.Save(New newReceipt)

          do! receiptRepo.QueueParsing newReceipt

          return Ok newReceipt
        | _ -> return "Bad mime type" |> OperationError |> Error
      }

    validateUser authService request
    |> TaskResult.bind handler
    |> Task.map (function
      | Ok receipt -> OkObjectResult receipt
      | Error RequestError.Unauthorized -> UnauthorizedResult()
      | Error(RequestError.OperationError e) -> BadRequestObjectResult(e)
      | Error(RequestError.Validation errors) -> BadRequestObjectResult(errors))

  [<Function("GetReceipt")>]
  member this.GetReceipt([<HttpTrigger("get", Route = "receipts/{id}")>] request: HttpRequest, id: string) : Task<IActionResult> =

    let handler: TokenUser -> Task<Result<Receipt, RequestError<unit>>> =
      fun _ -> task {
        let! receipt = receiptRepo.Get(ReceiptId id)

        match receipt with
        | Some receipt' -> return Ok receipt'
        | None -> return () |> OperationError |> Error
      }

    validateUser authService request
    |> TaskResult.bind handler
    |> Task.map (function
      | Ok receipt -> OkObjectResult receipt
      | Error RequestError.Unauthorized -> UnauthorizedResult()
      | Error(RequestError.OperationError()) -> NotFoundResult()
      | Error(RequestError.Validation errors) -> BadRequestObjectResult(errors))

  [<Function("ParseReceipt")>]
  member this.ParseReceipt([<QueueTrigger("%Storage:Queue%")>] request: {| Id: string |}, _: FunctionContext) : Task<unit> = task {

    let container = storageClient.GetBlobContainerClient(storageSettings.Container)

    let! receipt = receiptRepo.Get(ReceiptId request.Id)

    match receipt with
    | Some(New newReceipt) ->
      let blob = container.GetBlobClient newReceipt.FileName

      let sasUri =
        blob.GenerateSasUri(BlobSasPermissions.Read, System.DateTimeOffset.UtcNow.AddMinutes 5.0)

      let! result = parser.Parse(sasUri)

      match result with
      | Ok parseResult ->
        let parsedReceipt: Receipt.Parsed =
          { Id = newReceipt.Id
            Store = parseResult.Store
            UserId = newReceipt.UserId
            Date = parseResult.Date
            FileName = newReceipt.FileName
            Items = parseResult.Items
            Fees = parseResult.Fees }

        do! receiptRepo.Save(Parsed parsedReceipt)

        return ()
      | Error _ ->
        let unparsedReceipt: Receipt.Unparsed =
          { Id = newReceipt.Id
            FileName = newReceipt.FileName
            UserId = newReceipt.UserId
            Date = newReceipt.Date }

        do! receiptRepo.Save(Unparsed unparsedReceipt)

        return ()
    | _ ->
      logger.LogWarning("Receipt is not in the proper state for parsing or not found: {ReceiptId}", request.Id)
      return ()
  }