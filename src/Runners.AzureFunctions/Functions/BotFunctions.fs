namespace Runners.AzureFunctions.Functions

open Bot
open Bot.Handlers
open Microsoft.AspNetCore.Http
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open otsom.fs.Bot
open otsom.fs.Bot.Telegram.Mappings

type BotFunctions(storageClient, imageSettings, tgBot, buildBotService: BuildBotService, chatRepo: IChatRepo, logger: ILogger<BotFunctions>)
  =
  [<Function("HandleBotUpdate")>]
  member this.HandleBotUpdate([<HttpTrigger("post", Route = "update")>] update: Telegram.Bot.Types.Update, request: HttpRequest) = task {
    try
      match mapUpdate update with
      | Some(chatId, upd) ->
        let botSvc = buildBotService chatId
        let chat: Chat = raise <| System.NotImplementedException()

        match! mainHandler storageClient imageSettings tgBot botSvc chat upd with
        | Some() -> return ()
        | None ->
          logger.LogWarning("Update was not handled")

          return ()
      | None -> logger.LogWarning("Unsupported update type: {UpdateType}", string update.Type)
    with e ->
      logger.LogError(e, "Error during execution:")

      return ()
  }