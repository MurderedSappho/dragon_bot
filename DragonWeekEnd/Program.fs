// Learn more about F# at http://fsharp.org

open Newtonsoft.Json
open Quartz
open Quartz.Impl
open System.Net.Http
open System.Text
open FSharp.Data
open System
open NodaTime
open Domain
open Storage

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

type StatisicsMessage = string

type SendMessageItem = 
    {
        [<JsonProperty("chat_id")>]
        ChatId: int
        [<JsonProperty("text")>]
        Text: StatisicsMessage
    }

type DrinkTotalStatistics =
    { 
        Yes : int
        No : int
        NA: int
    }
    static member defaultValue = { Yes = 0; No = 0; NA = 0 }


let scheduler = 
    let comp = async {
        let schedulerFactory = StdSchedulerFactory()
        let! schd = schedulerFactory.GetScheduler()
        let casted = schd :> IScheduler
        schd.Start() |> ignore
        return schd
    }

    Async.RunSynchronously comp

let getChatUpdates offset = 
    async {
        use client = new HttpClient()
        let url = sprintf "https://api.telegram.org/bot678792687:AAHa9sbP9zT4hm8x8ZD10u1GzJSf6ZIJiJg/getUpdates?offset=%i" offset
        try
            let! res =  client.GetStringAsync(url)
            return Updates.Parse(res)
        with
        | _ -> return Updates.Parse("""{ "ok": true,"result": []}""")
    }

let parseTimestamp
    (timestamp: int)
    : LocalDate =
    Instant
        .FromUnixTimeSeconds(Convert.ToInt64 timestamp)
        .InUtc()
        .LocalDateTime
        .Date

let parseBool
    (value: string)
    : bool =
    let (_, parsed) = Boolean.TryParse(value)
    parsed

let result = Async.RunSynchronously (getChatUpdates 0)
let va = result.Ok

let getUpdatesLoop 
    : Async<unit> =
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

          let weekDrinkFacts =
              updates
              |> List.where (fun x -> x.Message.Chat.Id = 436295526)
              |> List.sortByDescending (fun x -> x.Message.Date)
              |> List.map (fun x ->
                  let date = parseTimestamp x.Message.Date
                  let value = parseBool x.Message.Text
                  (date, value))
              |> List.groupBy (fun (date, _) -> date)
              |> List.map (fun (key, coll) -> 
                  coll
                  |> List.first
                  |> function
                        | Some (_, value) -> (key, value)
                        | None -> (key, false))
              |> List.groupBy (fun (date, _) -> getWeekStart date)
              |> List.map (fun (_, coll) ->
                  WeekDrinkFact.create coll)

          do weekDrinkFacts
          |> List.map (fun x ->
                    match x with
                    | Some fact -> 
                        try
                            do Storage.createOrUpdate fact
                        with 
                        | ex -> ()
                    | None -> ())
          |> ignore

          do! Async.Sleep 10000
          
          return! loop lastUpdateId
        }
    loop 0
    
Async.Start getUpdatesLoop

let getDrinkStatisticts
    (weekFact: WeekDrinkFact option) 
    : DrinkTotalStatistics =
    match weekFact with
    | None -> DrinkTotalStatistics.defaultValue
    | Some weekFactValue -> 
    let days = weekFactValue.Days
    List.fold (fun stats value -> 
                match value with
                | (_, Yes) -> { stats with Yes = stats.Yes + 1 }
                | (_, No) -> { stats with Yes = stats.No + 1 }
                | (_, NA) -> { stats with Yes = stats.NA + 1 }) DrinkTotalStatistics.defaultValue days

let composeForWeek
    (weekFact: WeekDrinkFact option)
    : StatisicsMessage =
    let { Yes = yes; } = getDrinkStatisticts weekFact
    match yes with
    | 0 -> "Is it your dragon? He see no alcohol whole week!"
    | 1 -> "It's perfect result, only one alcohol consumption!"
    | 2 -> "Every day your dragon is better - two times in a week means almost nothing"
    | 3 -> "Blessed and approved by Zverj tree times in a week"
    | 4 -> "Something goes wrong, 4 times it too much"
    | days -> sprintf "%i times is unacceptable condition" days

let msg =
    result.Result
    |> List.ofArray
    |> List.map (fun x ->
        x.Message.Text)

let sendMessage 
    chatId 
    message =
    async {
        use client = new HttpClient()
        let url = "https://api.telegram.org/bot678792687:AAHa9sbP9zT4hm8x8ZD10u1GzJSf6ZIJiJg/sendMessage"
        let content = new StringContent(JsonConvert.SerializeObject({ ChatId= chatId; Text=message}), Encoding.UTF8, "application/json")
        try
            let! res =  client.PostAsync(url, content)
            ()
        with
        | _ -> ()
    }

type ZverSendJob() =
    interface IJob with
        member this.Execute(context: IJobExecutionContext) = 
            let zversChatId = 436295526
            
            let c = async {
                let now = 
                    SystemClock
                        .Instance
                        .GetCurrentInstant()
                        .InUtc()
                        .LocalDateTime
                        .Date

                let weekStart = getWeekStart now
                let previousWeekStart = weekStart.PlusWeeks(-1)
                
                let message = 
                    getAll()
                    |> List.where (fun x -> x.WeekStart = previousWeekStart)
                    |> List.first
                    |> composeForWeek

                let! res = sendMessage zversChatId message
                ()
            }

            Async.StartAsTask c :> _
            
let job =
    JobBuilder.Create<ZverSendJob>().WithIdentity("j1", "g1").Build()
           
let trigger =
    TriggerBuilder
        .Create()
        .WithIdentity("t1", "g1")
        .WithDailyTimeIntervalSchedule(fun x -> 
            let timezone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time")

            x.InTimeZone(timezone)
                .OnEveryDay()
                .WithIntervalInMinutes(1)
             //.StartingDailyAt(TimeOfDay.HourMinuteAndSecondOfDay(16, 25, 0))
             //.OnDaysOfTheWeek(DayOfWeek.Wednesday)
             .Build() |> ignore)
            .Build()

[<EntryPoint>]
let main argv =
    let scheduleJob = async {
        let! res = scheduler.ScheduleJob(job, trigger)
        res
    }

    Async.RunSynchronously scheduleJob
    Console.ReadKey true
        |> ignore
    printfn "Hello World from F#!"
    0 // return an integer exit code
