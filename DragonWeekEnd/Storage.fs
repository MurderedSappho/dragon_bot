module Storage

    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    open NodaTime
    open Domain
    open MBrace.FsPickler.Json
    open MongoDB.Bson

    [<CLIMutable>]
    type MongoDto =
        {
            [<BsonId>]
            Id: BsonObjectId;
            Value: string
        }

    let jsonSerializer = FsPickler.CreateJsonSerializer()
    let connectionString = "mongodb://vavava:aiusdan3HAk2@ds149984.mlab.com:49984/appharbor_srt410vj"
    let url = new MongoUrl(connectionString)
    let client = new MongoClient(url)
    let db = client.GetDatabase("appharbor_srt410vj")
    let coll = db.GetCollection<MongoDto>("MongoDto")

    let getAll 
        () 
        : WeekDrinkFact list =
        let results =
            coll
                .Find(fun x -> true)
                .ToList()
            |> List.ofSeq
            |> List.map (fun s -> 
            jsonSerializer.UnPickleOfString<WeekDrinkFact> s.Value)
        
        results

    let create 
        (fact: WeekDrinkFact) =
        let serialized = jsonSerializer.PickleToString fact
        let dto = { Id = BsonObjectId.Empty; Value = serialized; }
        do coll.InsertOne(dto)