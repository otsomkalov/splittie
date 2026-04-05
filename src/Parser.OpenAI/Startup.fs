module Parser.OpenAI.Startup

#nowarn "20"

open System
open System.ClientModel
open Domain
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open OpenAI
open OpenAI.Chat

let private buildChatClient (serviceProvider: IServiceProvider) =
  let settings = serviceProvider.GetRequiredService<IOptions<OpenAISettings>>().Value

  ChatClient(settings.Model, ApiKeyCredential settings.Key, OpenAIClientOptions(Endpoint = Uri(settings.Endpoint)))

let addOpenAIParser (cfg: IConfiguration) (services: IServiceCollection) =
  services.Configure<OpenAISettings>(cfg.GetRequiredSection(OpenAISettings.SectionName))

  services.AddSingleton<ChatClient>(buildChatClient)

  services.AddSingleton<IReceiptParser, OpenAIReceiptParser>()