open System
open System.Reflection
open Microsoft.Extensions.Configuration
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types

module TelegramBotFs =
   
  // Just to use for stub functions
  let undefined<'T> : 'T = failwith "Not implemented yet"

  [<EntryPoint>]
  let main argv =
    // Get our environment name so that we can have development-specific settings
    // kept differently. The environment we are working in must be passed in
    // the env variable DOTNETCORE_ENVIRONMENT. 
    // Appsettings for that environment must be in the file appsettings.<env>.json
    let environment = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")

    // Get a handle to our configurations so that we can read from that later
    // We also add User Secrets which will contain the telegram-bot token
    // The token must be configured as TelegramBot.Token.
    let config = ConfigurationBuilder()
                  .AddUserSecrets(Assembly.GetExecutingAssembly())
                  .AddJsonFile("appsettings.json", true, true)
                  .AddJsonFile($"appsettings.${environment}.json", true, true)
                  .Build()

    let botClient = TelegramBotClient(config.GetValue("TelegramBot.Token"))

    let me = botClient.GetMeAsync().Result
    printfn $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."

    let botOnMessage (args: MessageEventArgs) =
      if args.Message.Text <> null then
        Console.WriteLine($"Received a text message in chat {args.Message.Chat.Id}.");
        do Async.AwaitTask (botClient.SendTextMessageAsync(ChatId(args.Message.Chat.Id),"You said:\n" + args.Message.Text)) |> ignore
      else
        Console.WriteLine("No message received");

    botClient.OnMessage.Add(botOnMessage)

    botClient.StartReceiving()

    printfn "Press any key to exit"
    Console.ReadKey() |> ignore

    botClient.StopReceiving()
    0