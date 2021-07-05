// Entry point
//
// Copyright (c) 2021 Arvind Devarajan
// Licensed to you under the MIT License.
// See the LICENSE file in the project root for more information.
open System
open System.Reflection
open System.Threading
open Microsoft.Extensions.Configuration
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Exceptions
open Telegram.Bot.Extensions.Polling
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.InlineQueryResults
open Telegram.Bot.Types.InputFiles
open Telegram.Bot.Types.ReplyMarkups

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

    let botClient 
      = TelegramBotClient(config.GetValue("TelegramBot.Token"))

    let handleErrorAsync (bot:ITelegramBotClient) (err:Exception) (cts:CancellationToken) = 
      async {
        let errormsg = 
          match err with 
            | :? ApiRequestException as apiex -> $"Telegram API Error:\n[{apiex.ErrorCode}]\n{apiex.Message}"
            | _                               -> err.ToString()

        Console.WriteLine(errormsg)        
      }      

    let botOnMessageReceived (message:Message) = 
      Console.WriteLine($"Receive message type: {message.Type}");

      let sendInlineKeyboard = undefined
      let sendReplyKeyboard = undefined
      let removeKeyboard = undefined
      let sendFile = undefined
      let requestContactAndLocation = undefined
      let usage = undefined

      async {
        if message.Type <> MessageType.Text then 
          ()
        else
          let fn = 
            match message.Text.Split(' ').[0] with
              | "/inline"   -> sendInlineKeyboard
              | "/keyboard" -> sendReplyKeyboard
              | "/remove"   -> removeKeyboard
              | "/photo"    -> sendFile
              | "/request"  -> requestContactAndLocation
              | _           -> usage

          do! fn message
      }

    let unknownUpdateHandlerAsync (message:Message) =
      async {
        undefined
      }

    let handleUpdateAsync bot (update:Update) cts = 
      async {
        try
          let fn = 
            match update.Type with 
              | UpdateType.Message -> botOnMessageReceived
              | _                  -> unknownUpdateHandlerAsync

          do! fn update.Message
        with
          | _ as ex -> do! handleErrorAsync bot ex cts
      }
     
    async {
      let! me = botClient.GetMeAsync() |> Async.AwaitTask
      Console.Title = me.Username |> ignore
      printfn $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
      printfn $"Start listening for {me.Username}..."

      use cts = new CancellationTokenSource();
      
      // There is quite some bit of jugglery here, so requires some explanation:
      // DefaultUpdateHandler() requires two arguments, both of type Func<_,_,_,_>, and both
      // of which return a Task. Now, in order to be in the F# domain, we would like to 
      // have this Func<> defined as an F# function: and so we need to explicitely construct
      // Func<>s with by passing them an inner lambda function.
      // Now, the inner lambda function needs to return a Task, but we want to use F# async.
      // We therefore use a Async.StartAsTask to start an async computation and get back a Task.
      // The last ":>" is for upcasting Task<unit> (returned by the async computations) to their
      // base class Task to avoid and error that says "the expression expects a Task but we have a
      // Task<unit> here.". Since F# can already infer the base-type, we simply upcast to "_".
      // The good thing about doing all this is that handleUpdateAsync and handleErrorAsync are both
      // in the F# domain - so now can take all advantages of F#!
      botClient.StartReceiving(DefaultUpdateHandler(
                                Func<_,_,_,_>(fun b u t ->  Async.StartAsTask (handleUpdateAsync b u t) :> _),
                                Func<_,_,_,_>(fun b e t ->  Async.StartAsTask (handleErrorAsync b e t) :> _)),
                                cts.Token)
    } |> Async.Start

    printfn "Press any key to exit"
    Console.ReadKey() |> ignore
    0