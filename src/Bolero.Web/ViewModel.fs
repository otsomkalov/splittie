namespace Bolero.Web

open System
open Bolero.Web.Models

[<RequireQualifiedAccess>]
module ViewModel =

  [<RequireQualifiedAccess>]
  module ReceiptGridState =
    type Person = { Id: string; Name: string }

    type PersonValue =
      { PersonId: string
        Share: int
        Price: decimal }

    type ItemRow =
      { Id: ItemId
        Name: string
        Values: PersonValue list
        TotalAmount: decimal
        IsWarning: bool
        IsSuccess: bool }

    type FeeRow =
      { Id: FeeId
        Type: string
        Values: PersonValue list
        TotalAmount: decimal }

    type ItemsSubtotalRow =
      { Values: PersonValue list
        TotalAmount: decimal }

    type FeesSubtotalRow =
      { Values: PersonValue list
        TotalAmount: decimal }

    type TotalRow =
      { Values: PersonValue list
        TotalAmount: decimal }

    type State =
      { People: Person list
        Items: ItemRow list
        ItemsSubtotal: ItemsSubtotalRow
        Fees: FeeRow list
        FeesSubtotal: FeesSubtotalRow
        Total: TotalRow }

    let from (receipt: Receipt.Parsed) (people: Person list) (shares: Map<ItemId * string, int>) =
      let mapItem =
        fun (item: Receipt.Item) ->
          let totalShares =
            people
            |> List.sumBy (fun p -> shares.TryFind(item.Id, p.Id) |> Option.defaultValue 0)

          let values =
            people
            |> List.map (fun p ->
              let share = shares.TryFind(item.Id, p.Id) |> Option.defaultValue 0

              let price =
                if totalShares = 0 then
                  0.0M
                else
                  (item.Amount / decimal totalShares) * decimal share

              { PersonId = p.Id
                Share = share
                Price = price })

          { Id = item.Id
            Name = item.Name
            Values = values
            TotalAmount = item.Amount
            IsWarning = totalShares = 0
            IsSuccess = totalShares <> 0 && values |> List.forall (fun v -> v.Share <> 0) }

      let itemRows = receipt.Items |> List.map mapItem

      let totalItemsAmount = receipt.Items |> List.sumBy _.Amount

      let itemsSubtotalValues =
        people
        |> List.map (fun p ->
          let personValues =
            itemRows
            |> List.map (fun row -> row.Values |> List.find (fun v -> v.PersonId = p.Id))

          let shareTotal = personValues |> List.sumBy _.Share
          let priceTotal = personValues |> List.sumBy _.Price

          { PersonId = p.Id
            Share = shareTotal
            Price = priceTotal })

      let feeRows =
        receipt.Fees
        |> List.map (fun fee ->
          let values =
            people
            |> List.map (fun p ->
              let itemPriceTotal =
                (itemsSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Price

              let feePrice =
                if totalItemsAmount = 0.0M then
                  0.0M
                else
                  (itemPriceTotal / totalItemsAmount) * fee.Amount

              { PersonId = p.Id
                Share = 0
                Price = feePrice })

          { Id = fee.Id
            Type = fee.Type
            Values = values
            TotalAmount = fee.Amount })

      let feesSubtotalValues =
        people
        |> List.map (fun p ->
          let priceTotal =
            feeRows
            |> List.sumBy (fun row -> (row.Values |> List.find (fun v -> v.PersonId = p.Id)).Price)

          { PersonId = p.Id
            Share = 0
            Price = priceTotal })

      let totalValues =
        people
        |> List.map (fun p ->
          let itemPriceTotal =
            (itemsSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Price

          let feesPriceTotal =
            (feesSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Price

          let shareTotal =
            (itemsSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Share

          { PersonId = p.Id
            Share = shareTotal
            Price = itemPriceTotal + feesPriceTotal })

      { People = people
        Items = itemRows
        ItemsSubtotal =
          { Values = itemsSubtotalValues
            TotalAmount = totalItemsAmount }
        Fees = feeRows
        FeesSubtotal =
          { Values = feesSubtotalValues
            TotalAmount = receipt.Fees |> List.sumBy _.Amount }
        Total =
          { Values = totalValues
            TotalAmount = receipt.Total } }

    let private recalculate (state: State) =
      let totalItemsAmount = state.ItemsSubtotal.TotalAmount

      let itemRows =
        state.Items
        |> List.map (fun row ->
          let totalShares = row.Values |> List.sumBy _.Share

          let values =
            row.Values
            |> List.map (fun v ->
              let price =
                if totalShares = 0 then
                  0.0M
                else
                  (row.TotalAmount / decimal totalShares) * decimal v.Share

              { v with Price = price })

          { row with
              Values = values
              IsWarning = totalShares = 0
              IsSuccess = totalShares <> 0 && values |> List.forall (fun v -> v.Share <> 0) })

      let itemsSubtotalValues =
        state.People
        |> List.map (fun p ->
          let personValues =
            itemRows
            |> List.map (fun row -> row.Values |> List.find (fun v -> v.PersonId = p.Id))

          let shareTotal = personValues |> List.sumBy _.Share
          let priceTotal = personValues |> List.sumBy _.Price

          { PersonId = p.Id
            Share = shareTotal
            Price = priceTotal })

      let feeRows =
        state.Fees
        |> List.map (fun fee ->
          let values =
            state.People
            |> List.map (fun p ->
              let itemPriceTotal =
                (itemsSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Price

              let feePrice =
                if totalItemsAmount = 0.0M then
                  0.0M
                else
                  (itemPriceTotal / totalItemsAmount) * fee.TotalAmount

              { PersonId = p.Id
                Share = 0
                Price = feePrice })

          { fee with Values = values })

      let feesSubtotalValues =
        state.People
        |> List.map (fun p ->
          let priceTotal =
            feeRows
            |> List.sumBy (fun row -> (row.Values |> List.find (fun v -> v.PersonId = p.Id)).Price)

          { PersonId = p.Id
            Share = 0
            Price = priceTotal })

      let totalValues =
        state.People
        |> List.map (fun p ->
          let itemPriceTotal =
            (itemsSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Price

          let feesPriceTotal =
            (feesSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Price

          let shareTotal =
            (itemsSubtotalValues |> List.find (fun v -> v.PersonId = p.Id)).Share

          { PersonId = p.Id
            Share = shareTotal
            Price = itemPriceTotal + feesPriceTotal })

      { state with
          Items = itemRows
          ItemsSubtotal.Values = itemsSubtotalValues
          Fees = feeRows
          FeesSubtotal.Values = feesSubtotalValues
          Total.Values = totalValues }

    let updateShare (itemId: ItemId) (personId: string) (share: int) (state: State) =
      let newItems =
        state.Items
        |> List.map (fun row ->
          if row.Id = itemId then
            let newValues =
              row.Values
              |> List.map (fun v ->
                if v.PersonId = personId then
                  { v with Share = share }
                else
                  v)

            { row with Values = newValues }
          else
            row)

      { state with Items = newItems } |> recalculate

    let updatePersonName (personId: string) (name: string) (state: State) =
      let newPeople =
        state.People
        |> List.map (fun p -> if p.Id = personId then { p with Name = name } else p)

      { state with People = newPeople }

    let addPerson (person: Person) (state: State) =
      let newPeople = state.People @ [ person ]

      let mapValues =
        fun (values: PersonValue list) ->
          values
          @ [ { PersonId = person.Id
                Share = 0
                Price = 0.0M } ]

      { state with
          People = newPeople
          Items =
            state.Items
            |> List.map (fun row ->
              { row with
                  Values = mapValues row.Values })
          Fees =
            state.Fees
            |> List.map (fun row ->
              { row with
                  Values = mapValues row.Values })
          ItemsSubtotal.Values = mapValues state.ItemsSubtotal.Values
          FeesSubtotal.Values = mapValues state.FeesSubtotal.Values
          Total.Values = mapValues state.Total.Values }
      |> recalculate

    let addItem (state: State) =
      let newId = ItemId(Guid.NewGuid().ToString())

      let newItem =
        { Id = newId
          Name = "New Item"
          Values =
            state.People
            |> List.map (fun p ->
              { PersonId = p.Id
                Share = 0
                Price = 0.0M })
          TotalAmount = 0.0M
          IsWarning = true
          IsSuccess = false }

      { state with
          Items = state.Items @ [ newItem ] }
      |> recalculate

    let removeItem (itemId: ItemId) (state: State) =
      { state with
          Items = state.Items |> List.filter (fun i -> i.Id <> itemId) }
      |> recalculate

    let updateItem (itemId: ItemId) (name: string) (amount: decimal) (state: State) =
      let newItems =
        state.Items
        |> List.map (fun i ->
          if i.Id = itemId then
            { i with
                Name = name
                TotalAmount = amount }
          else
            i)

      let newItemsSubtotalAmount = newItems |> List.sumBy _.TotalAmount

      { state with
          Items = newItems
          ItemsSubtotal.TotalAmount = newItemsSubtotalAmount
          Total.TotalAmount = newItemsSubtotalAmount + state.FeesSubtotal.TotalAmount }
      |> recalculate

    let addFee (state: State) =
      let newId = FeeId(Guid.NewGuid().ToString())

      let newFee =
        { Id = newId
          Type = "New Fee"
          Values =
            state.People
            |> List.map (fun p ->
              { PersonId = p.Id
                Share = 0
                Price = 0.0M })
          TotalAmount = 0.0M }

      { state with
          Fees = state.Fees @ [ newFee ] }
      |> recalculate

    let removeFee (feeId: FeeId) (state: State) =
      let newFees = state.Fees |> List.filter (fun f -> f.Id <> feeId)
      let newFeesSubtotalAmount = newFees |> List.sumBy _.TotalAmount

      { state with
          Fees = newFees
          FeesSubtotal.TotalAmount = newFeesSubtotalAmount
          Total.TotalAmount = state.ItemsSubtotal.TotalAmount + newFeesSubtotalAmount }
      |> recalculate

    let updateFee (feeId: FeeId) (newType: string) (amount: decimal) (state: State) =
      let newFees =
        state.Fees
        |> List.map (fun f ->
          if f.Id = feeId then
            { f with
                Type = newType
                TotalAmount = amount }
          else
            f)

      let newFeesSubtotalAmount = newFees |> List.sumBy _.TotalAmount

      { state with
          Fees = newFees
          FeesSubtotal.TotalAmount = newFeesSubtotalAmount
          Total.TotalAmount = state.ItemsSubtotal.TotalAmount + newFeesSubtotalAmount }
      |> recalculate