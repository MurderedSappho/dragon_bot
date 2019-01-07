// Learn more about F# at http://fsharp.org

open System
open System.Threading
open Suave



[<EntryPoint>]
let main argv =
    let cancellationTokenSource = new CancellationTokenSource()
    let config = { defaultConfig with cancellationToken = cancellationTokenSource.Token }
    let listening, server = startWebServerAsync defaultConfig (Successful.OK "")
    Async.Start server
    Console.ReadKey true
        |> ignore
    printfn "Hello World from F#!"
    0 // return an integer exit code
