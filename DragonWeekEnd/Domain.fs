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

    let getDayFact
        (facts: DayDrinkFact list)
        (weekDay: WeekDay)
        : DayDrinkFact =
            facts 
                |> List.where (fun (day, _) -> day = weekDay)
                |> List.first
                |> function
                    | Some drinkFact -> drinkFact
                    | None -> DayDrinkFact(weekDay, NA)
    
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

    type WeekDrinkFact = private {
        WeekStart : LocalDate
        Days: DayDrinkFact list }

    module WeekDrinkFact =

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

            let daysFromMonday = 
                firstDay.DayOfWeek
                |> LanguagePrimitives.EnumToValue
                |> (*) -1

            let previousSunday = firstDay.PlusDays(daysFromMonday)

            let weekDays = 
                [1..7]
                |> List.map (fun n -> previousSunday.PlusDays(n))
            
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
            
            let weekStart = previousSunday.PlusDays(1)
            
            Some { 
                WeekStart = weekStart;
                Days = inWeekDays }

        type WeekDrinkFact with
            member this.Monday = getDayFact this.Days Monday
            member this.Tuesday = getDayFact this.Days Tuesday
            member this.Wednesday = getDayFact this.Days Wednesday
            member this.Thursday = getDayFact this.Days Thursday
            member this.Friday = getDayFact this.Days Friday
            member this.Saturday = getDayFact this.Days Saturday
            member this.Sunday = getDayFact this.Days Sunday

