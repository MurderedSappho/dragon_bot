module Storage

    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
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

    let createOrUpdate
        (fact: WeekDrinkFact) =
        let toUpdate =
            coll
                .Find(fun x -> true)
                .ToList()
            |> List.ofSeq
            |> List.map (fun s ->
                let value = jsonSerializer.UnPickleOfString<WeekDrinkFact> s.Value
                let id = s.Id
                (id, value))
            |> List.filter (fun (_, value) -> value.WeekStart = fact.WeekStart)
            |> List.first
        
        match toUpdate with
        | Some (id, existingValue) ->
            let mergedValue = WeekDrinkFact.optimisticMerge fact existingValue
            match mergedValue with
            | None -> ()
            | Some _ ->
            let serialized =  jsonSerializer.PickleToString mergedValue
            let record = { Id = BsonObjectId.Create(id); Value = serialized }
            do coll.ReplaceOne((fun x -> x.Id = id), record) |> ignore
        | _ ->
            let serialized =  jsonSerializer.PickleToString fact
            let record = { Id = BsonObjectId.Empty; Value = serialized }
            do coll.InsertOne(record)
    
    