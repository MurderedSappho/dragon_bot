// Learn more about F# at http://fsharp.org

open Newtonsoft.Json
open Quartz
open Quartz.Impl
open System.Net.Http

open System.Text
open FSharp.Data
open System
open YoLo
open NodaTime
open NodaTime
open NodaTime
open Suave.EventSource




//let fact = Domain.WeekDrinkFact.create [(LocalDate.FromYearMonthWeekAndDay(2019, 1, 3, IsoDayOfWeek.Friday), true)]
//let first = Storage.createOrUpdate fact.Value
//let all = Storage.getAll()
//


let aa = 2

type Updates = JsonProvider<"""{
    "ok": true,
    "result": [
        {
            "update_id": 293423949,
            "message": {
                "message_id": 14,
                "from": {
                    "id": 436295526,
                    "is_bot": false,
                    "first_name": "Eyes in the",
                    "last_name": "Box",
                    "username": "eyesInTheBox",
                    "language_code": "ru"
                },
                "chat": {
                    "id": 436295526,
                    "first_name": "Eyes in the",
                    "last_name": "Box",
                    "username": "eyesInTheBox",
                    "type": "private"
                },
                "date": 1547992297,
                "text": "fdsfdsfds"
            }
        }
    ]
}""">

let scheduler = 
    let comp = async {
        let schedulerFactory = StdSchedulerFactory()
        let! schd = schedulerFactory.GetScheduler()
        let casted = schd :> IScheduler
        schd.Start() |> ignore
        return schd
    }

    Async.RunSynchronously comp

type SendMessageItem = 
    {
        [<JsonProperty("chat_id")>]
        ChatId: int;
        [<JsonProperty("text")>]
        Text: string
    }

let getChatUpdates offset = 
    async {
        let client = new HttpClient()
        let url = sprintf "https://api.telegram.org/bot678792687:AAHa9sbP9zT4hm8x8ZD10u1GzJSf6ZIJiJg/getUpdates?offset=%i" offset
        let! res =  client.GetStringAsync(url)
        return Updates.Parse(res)
    }

let parseTimestamp
    (timestamp: int)
    : LocalDate =
    Instant
        .FromUnixTimeSeconds(Convert.ToInt64 timestamp)
        .InUtc()
        .LocalDateTime
        .Date


let result = Async.RunSynchronously (getChatUpdates 0)
let va = result.Ok

let getUpdatesLoop : Async<unit> =
    let rec loop updateId : Async<unit> =
        async {
          let! message = getChatUpdates updateId
         
          let updates =
              message
                  .Result
              |> List.ofArray
              
          let lastUpdateId =
              updates
              |> List.tryLast
              |> function
                 | Some item -> item.UpdateId + 1
                 | None -> updateId
          
          let messages =
              updates
              |> List.sortByDescending (fun x -> x.Message.Date)
              |> List.map (fun x ->
                  let date = parseTimestamp x.Message.Date
                  let message = x.Message.Text
                  (date, message))
              |> List.groupBy (fun (date, _) -> date)
              |> List.map (fun (_, coll) -> List.head coll)
          
          do! Async.Sleep 10000
          
          return! loop lastUpdateId
        }
    loop 0
    
Async.Start getUpdatesLoop

let msg =
    result.Result
    |> List.ofArray
    |> List.map (fun x ->
        x.Message.Text)

let os = ""

let sendMessage chatId message = 
    async {
        let client = new HttpClient()
        let url = "https://api.telegram.org/bot678792687:AAHa9sbP9zT4hm8x8ZD10u1GzJSf6ZIJiJg/sendMessage"
        let content = new StringContent(JsonConvert.SerializeObject({ ChatId= chatId; Text=message}), Encoding.UTF8, "application/json")
        let! res =  client.PostAsync(url, content)
        return res
    }

type TestJob() =
    interface IJob with
        member this.Execute(context: IJobExecutionContext) = 
            let c = async {
                let! va = (sendMessage 436295526 "vava")
                Console.WriteLine(DateTime.Now)
            }
            
            Async.StartAsTask c :> _
            
let job =
    JobBuilder.Create<TestJob>().WithIdentity("j1", "g1").Build()
           
let trigger =
    TriggerBuilder
        .Create()
        .WithIdentity("t1", "g1")
        .WithDailyTimeIntervalSchedule(fun x -> 
            let timezone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time")

            x.InTimeZone(timezone)
             .StartingDailyAt(TimeOfDay.HourMinuteAndSecondOfDay(16, 26, 0))
             .OnEveryDay()
             .Build() |> ignore)
            .Build()



[<EntryPoint>]
let main argv =
    //let cancellationTokenSource = new CancellationTokenSource()
    //let config = { defaultConfig with cancellationToken = cancellationTokenSource.Token }
    //let listening, server = startWebServerAsync defaultConfig (Successful.OK "")
    //let scheduleJob = async {
    //    let! res = scheduler.ScheduleJob(job, trigger)
    //    res
    //}

    //Async.RunSynchronously scheduleJob
    //Async.Start server
    Console.ReadKey true
        |> ignore
    printfn "Hello World from F#!"
    0 // return an integer exit code
