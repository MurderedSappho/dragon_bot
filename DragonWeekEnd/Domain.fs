module Domain

    open NodaTime

    type Fact =
        | Yes
        | No
        | NA

    type WeekDay = 
        | Monday
        | Tuesday
        | Wednesday
        | Thursday
        | Friday
        | Saturday
        | Sunday

    type DayDrinkFact =
        WeekDay * Fact

    let getFact
        (facts: DayDrinkFact list)
        (weekDay: WeekDay)
        : Fact =
            facts 
                |> List.where (fun (day, _) -> day = weekDay)
                |> List.first
                |> function
                    | Some (_, fact) -> fact
                    | None -> NA
    
    let dayOfWeekToWeekDay 
        (dayOfWeek: IsoDayOfWeek)
        : WeekDay option =
        match dayOfWeek with
        | IsoDayOfWeek.Monday -> Some Monday
        | IsoDayOfWeek.Tuesday -> Some Tuesday
        | IsoDayOfWeek.Wednesday -> Some Wednesday
        | IsoDayOfWeek.Thursday -> Some Thursday
        | IsoDayOfWeek.Friday -> Some Friday
        | IsoDayOfWeek.Saturday -> Some Saturday
        | IsoDayOfWeek.Sunday -> Some Sunday
        | _ -> None
    
    let getWeekDay 
        (date: LocalDate)
        : WeekDay option = dayOfWeekToWeekDay date.DayOfWeek

    let getWeekStart
        (date: LocalDate)
        : LocalDate =
        let daysFromMonday = 
                date.DayOfWeek
                |> LanguagePrimitives.EnumToValue
                |> (*) -1

        date.PlusDays(daysFromMonday)

    type WeekDrinkFact = private {
        weekStart : LocalDate
        days: DayDrinkFact list }
        with
        member this.Monday = getFact this.Days Monday
        member this.Tuesday = getFact this.Days Tuesday
        member this.Wednesday = getFact this.Days Wednesday
        member this.Thursday = getFact this.Days Thursday
        member this.Friday = getFact this.Days Friday
        member this.Saturday = getFact this.Days Saturday
        member this.Sunday = getFact this.Days Sunday
        member this.WeekStart = this.weekStart
        member this.Days = this.days

    module WeekDrinkFact =
        open YoLo

        let create
            (days: (LocalDate*bool) list)
            : WeekDrinkFact option =
            let sortedDays = 
                days
                |> List.sortBy fst
            
            let firstDayDrink = List.first sortedDays
            match firstDayDrink with
            | None -> None
            | Some (firstDay, _) ->

            let weekStart = getWeekStart firstDay

            let weekDays = 
                [0..6]
                |> List.map (fun n -> weekStart.PlusDays(n))
            
            let inWeekDays = 
                weekDays
                |> List.map (fun day ->
                        let weekDay = 
                            getWeekDay(day)
                            |> function
                                | Some weekDay -> weekDay
                                | None -> failwith "Bad day"

                        let boolToFact bool =
                            match bool with
                            | true -> Fact.Yes
                            | false -> Fact.No

                        let resultsForDay =
                            days
                            |> List.where (fun (d, _) -> d = day)
                            |> List.first
                            |> function
                                | Some (_, result) -> DayDrinkFact(weekDay, boolToFact result)
                                | None -> DayDrinkFact(weekDay, NA)

                        resultsForDay)

            Some { 
                weekStart = weekStart;
                days = inWeekDays }

        let optimisticMerge
            (first: WeekDrinkFact)
            (second: WeekDrinkFact)
            : WeekDrinkFact option  =
            if (first.WeekStart <> second.WeekStart)
            then None
            else
            let firstDays = 
                first.Days
                |> List.sortBy (fun (date, _) -> date)
            let secondDays = 
                second.Days
                |> List.sortBy (fun (date, _) -> date)
            
            let mergedFacts = 
                List.map2 (fun (firstDay, firstFact) (_, secondFact) -> 
                            match (firstFact, secondFact) with
                            | (Yes, _) -> DayDrinkFact(firstDay, Yes)
                            | (No, _) -> DayDrinkFact(firstDay, No)
                            | (_, Yes) -> DayDrinkFact(firstDay, Yes)
                            | (_, No) -> DayDrinkFact(firstDay, No)
                            | (_, _) -> DayDrinkFact(firstDay, NA)) firstDays secondDays

            Some {
                weekStart = first.WeekStart;
                days = mergedFacts }

