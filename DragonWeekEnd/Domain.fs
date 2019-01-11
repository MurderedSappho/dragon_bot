module Domain

    open System

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
                WeekStart : DateTime
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
        (dayOfWeek: DayOfWeek)
        : WeekDay option =
        match dayOfWeek with
        | DayOfWeek.Monday -> Some Monday
        | DayOfWeek.Tuesday -> Some Tuesday
        | DayOfWeek.Wednesday -> Some Wednesday
        | DayOfWeek.Thursday -> Some Thursday
        | DayOfWeek.Friday -> Some Friday
        | DayOfWeek.Saturday -> Some Saturday
        | DayOfWeek.Sunday -> Some Sunday
        | _ -> None
    
    let getWeekDay 
        (date: DateTime)
        : WeekDay option =
        dayOfWeekToWeekDay date.DayOfWeek
        
    module WeekDrinkFact =

        let create 
            (days: (DateTime*bool) list)
            : WeekDrinkFact option =
            let sortedDays = 
                days
                |> List.sortBy fst
            
            let firstDayDrink = List.first sortedDays
            match firstDayDrink with
            | None -> None
            | Some (firstDay, _) ->

            let firstWeekDayOption = getWeekDay firstDay
            match firstWeekDayOption with
            | None -> None
            | Some firstWeekDay ->

            let lastDayDrink = List.last sortedDays
            let lastDay, _ = lastDayDrink

            failwith ""
            
            
            
            
        

        type WeekDrinkFact with
            member this.Monday = getDayFact this.Days Monday
            member this.Tuesday = getDayFact this.Days Tuesday
            member this.Wednesday = getDayFact this.Days Wednesday
            member this.Thursday = getDayFact this.Days Thursday
            member this.Friday = getDayFact this.Days Friday
            member this.Saturday = getDayFact this.Days Saturday
            member this.Sunday = getDayFact this.Days Sunday

