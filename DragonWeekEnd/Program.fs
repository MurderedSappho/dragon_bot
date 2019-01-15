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

let fact = Domain.WeekDrinkFact.create [(LocalDate.FromYearMonthWeekAndDay(2019, 1, 3, IsoDayOfWeek.Friday), true)]
let first = Storage.create fact.Value
let all = Storage.getAll()

let aa = 2

type Updates = JsonProvider<""" {
    "ok ": true,
    "result ": [
        {
            "update_id ": 293423948,
            "message ": {
                "message_id ": 9,
                "from ": {
                    "id ": 436295526,
                    "is_bot ": false,
                    "first_name ": "Eyes in the ",
                    "last_name ": "Box ",
                    "username ": "eyesInTheBox ",
                    "language_code ": "en "
                },
                "chat ": {
                    "id ": 436295526,
                    "first_name ": "Eyes in the ",
                    "last_name ": "Box ",
                    "username ": "eyesInTheBox ",
                    "type ": "private "
                },
                "date ": 1547041301,
                "text ": "  u0430  u0432  u044b  u0430  u0432  u044b "
            }
        }
    ]
} """>

let scheduler = 
    let comp = async {
        let schedulerFactory = StdSchedulerFactory()
        let! schd = schedulerFactory.GetScheduler()
        let casted = schd :> IScheduler
        schd.Start() |> ignore
        return schd
    }

    Async.RunSynchronously comp


type TelegramUpdateMessage = 
    {
        Ok: bool;
        Result: list<TelegramUpdateResponseItem>;
    }
and TelegramUpdateResponseItem =
    {
        UpdateId: int;
        Message: TelegramUpdateMessageItem;

    }
and TelegramUpdateMessageItem =
    {
        [<JsonProperty("message_id")>]
        MessageId: int;
        Text: string;
    }

type SendMessageItem = 
    {
        [<JsonProperty("chat_id")>]
        ChatId: int;
        [<JsonProperty("text")>]
        Text: string
    }

let getTelegramString<'T> method = 
    async {
        let client = new HttpClient()
        let url = sprintf "https://api.telegram.org/bot678792687:AAHa9sbP9zT4hm8x8ZD10u1GzJSf6ZIJiJg/%s" method
        let! res =  client.GetStringAsync(url)
        let upd = Updates.Parse(res)

        return JsonConvert.DeserializeObject<'T>(res)
    }

Async.RunSynchronously (getTelegramString "getUpdates")

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
