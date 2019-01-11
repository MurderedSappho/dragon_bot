module Domain

    open System
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

    type WeekDrinkFact = private {   
                WeekStart : LocalDate
                Days: DayDrinkFact list }

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
        : WeekDay option =
        dayOfWeekToWeekDay date.DayOfWeek
        
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

            //let firstWeekDayOption = getWeekDay firstDay
            //match firstWeekDayOption with
            //| None -> None
            //| Some firstWeekDay ->
            
            let startOfWeek = firstDay.Previous(IsoDayOfWeek.Monday)
            let endOfWeek = firstDay.Previous(IsoDayOfWeek.Sunday)
            
            let inWeekDays = 
                days
                |> List.takeWhile (fun (day, _) -> day < endOfWeek)
                |> List.z

            failwith ""
            
            
            
            
        

        type WeekDrinkFact with
            member this.Monday = getDayFact this.Days Monday
            member this.Tuesday = getDayFact this.Days Tuesday
            member this.Wednesday = getDayFact this.Days Wednesday
            member this.Thursday = getDayFact this.Days Thursday
            member this.Friday = getDayFact this.Days Friday
            member this.Saturday = getDayFact this.Days Saturday
            member this.Sunday = getDayFact this.Days Sunday

