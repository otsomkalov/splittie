namespace Parser.OpenAI

open System
open System.Text.Json
open System.IO
open Domain
open Microsoft.Extensions.Logging
open OpenAI.Chat

[<RequireQualifiedAccess>]
module JSON =
  let private options =
    JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)

  let deserialize<'T> (json: string) : 'T =
    JsonSerializer.Deserialize<'T>(json, options)

[<CLIMutable>]
type OpenAISettings =
  { Endpoint: string
    Key: string
    Model: string }

  static member SectionName = "OpenAI"

type OpenAIReceiptParser(chatClient: ChatClient, logger: ILogger<OpenAIReceiptParser>) =
  [<Literal>]
  let Prompt = "Analyze this image and return structured JSON."

  interface IReceiptParser with
    member this.Parse(imageUri) =
      task {
        let! schemaBytes = File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "schema.json"))

        let responseFormat =
          ChatResponseFormat.CreateJsonSchemaFormat("response_schema", BinaryData.FromBytes(schemaBytes), jsonSchemaIsStrict = true)

        let message: ChatMessage =
          UserChatMessage(
            [ ChatMessageContentPart.CreateTextPart(Prompt)
              ChatMessageContentPart.CreateImagePart(imageUri) ]
          )

        let completionOptions = ChatCompletionOptions(ResponseFormat = responseFormat)

        logger.LogInformation("Sending receipt image for analysis")

        try
          let! response = chatClient.CompleteChatAsync([ message ], completionOptions)

          let responseContent = response.Value.Content[0].Text

          logger.LogInformation("Receipt parsed successfully")

          let receipt = JSON.deserialize<ParsingResult> responseContent

          return Ok receipt
        with e ->
          logger.LogError(e, "Error during parsing receipt image")

          return Error e.Message
      }