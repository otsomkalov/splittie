namespace Bolero.Web.Tests

open System
open Domain
open Xunit
open Bolero.Web

type ReceiptGridState() =

  let people: ViewModel.ReceiptGridState.Person list =
    [ { Id = "person1"; Name = "Person 1" }; { Id = "person2"; Name = "Person 2" } ]

  let receipt: Receipt.Parsed =
    { Id = ReceiptId "receipt1"
      Store = "Store 1"
      FileName = "receipt1.jpg"
      UserId = Mocks.userId
      Date = DateTime.Now
      Items =
        [ { Name = "Item 1"
            Quantity = 1
            Amount = 10m }
          { Name = "Item 2"
            Quantity = 1
            Amount = 20m } ]
      Fees = [ { Type = "Service Fee"; Amount = 3m } ] }

  [<Fact>]
  member _.``from should initialize state correctly with no shares``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty

    Assert.Equal<ViewModel.ReceiptGridState.Person>(people, state.People)
    Assert.Equal(2, state.Items |> List.length)
    Assert.Equal(1, state.Fees |> List.length)

    // Item 1
    let item1 = state.Items |> List.head
    Assert.Equal(10m, item1.TotalAmount)
    Assert.True(item1.IsWarning)
    item1.Values |> List.iter (fun value -> Assert.Equal(0m, value.Price))

    // Totals should be 33 since receipt.Total is 33 and it's not recalculated to 0 if no shares
    Assert.Equal(33m, state.Total.TotalAmount)
    state.Total.Values |> List.iter (fun value -> Assert.Equal(0m, value.Price))

  [<Fact>]
  member _.``recalculate should compute correct prices based on shares``() =
    let initialState = ViewModel.ReceiptGridState.from receipt people Map.empty

    let item1Id = initialState.Items.[0].Id
    let item2Id = initialState.Items.[1].Id
    let fee1Id = initialState.Fees.[0].Id

    let state =
      initialState
      |> ViewModel.ReceiptGridState.updateShare item1Id "person1" 1
      |> ViewModel.ReceiptGridState.updateShare item2Id "person1" 1
      |> ViewModel.ReceiptGridState.updateShare item2Id "person2" 1

    // Item 1: 10m total, person1 has 1 share, person2 has 0. person1 pays 10m, person2 pays 0.
    let item1Row = state.Items |> List.find (fun item -> item.Id = item1Id)

    Assert.Equal(10m, (item1Row.Values |> List.find (fun v -> v.PersonId = "person1")).Price)

    Assert.Equal(0m, (item1Row.Values |> List.find (fun v -> v.PersonId = "person2")).Price)

    // Item 2: 20m total, person1 has 1 share, person2 has 1. Both pay 10m.
    let item2Row = state.Items |> List.find (fun item -> item.Id = item2Id)

    Assert.Equal(10m, (item2Row.Values |> List.find (fun v -> v.PersonId = "person1")).Price)

    Assert.Equal(10m, (item2Row.Values |> List.find (fun v -> v.PersonId = "person2")).Price)

    // Items Subtotal: person1 pays 20m, person2 pays 10m. Total 30m.
    Assert.Equal(30m, state.ItemsSubtotal.TotalAmount)

    Assert.Equal(20m, (state.ItemsSubtotal.Values |> List.find (fun v -> v.PersonId = "person1")).Price)

    Assert.Equal(10m, (state.ItemsSubtotal.Values |> List.find (fun v -> v.PersonId = "person2")).Price)

    // Fee: 3m total. Distributed proportional to item subtotals.
    // person1: 20/30 * 3 = 2m
    // person2: 10/30 * 3 = 1m
    let fee1Row = state.Fees |> List.find (fun fee -> fee.Id = fee1Id)

    Assert.Equal(2m, (fee1Row.Values |> List.find (fun v -> v.PersonId = "person1")).Price, 3)

    Assert.Equal(1m, (fee1Row.Values |> List.find (fun v -> v.PersonId = "person2")).Price, 3)

    // Total: person1 = 20 + 2 = 22m, person2 = 10 + 1 = 11m. Total 33m.
    Assert.Equal(33m, state.Total.TotalAmount)

    Assert.Equal(22m, (state.Total.Values |> List.find (fun v -> v.PersonId = "person1")).Price, 3)

    Assert.Equal(11m, (state.Total.Values |> List.find (fun v -> v.PersonId = "person2")).Price, 3)

  [<Fact>]
  member _.``updateShare should update share and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let item1Id = state.Items.[0].Id

    let newState = state |> ViewModel.ReceiptGridState.updateShare item1Id "person1" 1

    let item1Row = newState.Items |> List.find (fun item -> item.Id = item1Id)

    let person1_item1 =
      item1Row.Values |> List.find (fun value -> value.PersonId = "person1")

    Assert.Equal(1, person1_item1.Share)
    Assert.Equal(10m, person1_item1.Price)
    Assert.True(newState.Total.TotalAmount > 0m)

  [<Fact>]
  member _.``addItem should add new item with zero amount and shares``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let newState = state |> ViewModel.ReceiptGridState.addItem

    Assert.Equal(state.Items.Length + 1, newState.Items |> List.length)
    let newItem = newState.Items |> List.last
    Assert.Equal(0m, newItem.TotalAmount)
    newItem.Values |> List.iter (fun value -> Assert.Equal(0, value.Share))

  [<Fact>]
  member _.``removeItem should remove item and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let item1Id = state.Items.[0].Id

    let stateWithShare =
      state |> ViewModel.ReceiptGridState.updateShare item1Id "person1" 1

    let newState = stateWithShare |> ViewModel.ReceiptGridState.removeItem item1Id

    Assert.Equal(state.Items.Length - 1, newState.Items |> List.length)

    Assert.False(newState.Items |> List.exists (fun item -> item.Id = item1Id))

  [<Fact>]
  member _.``updateItem should update name and amount and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let item1Id = state.Items.[0].Id

    let stateWithShare =
      state |> ViewModel.ReceiptGridState.updateShare item1Id "person1" 1

    let newState =
      stateWithShare |> ViewModel.ReceiptGridState.updateItem item1Id "New Name" 50m

    let item1Row = newState.Items |> List.find (fun item -> item.Id = item1Id)
    Assert.Equal("New Name", item1Row.Name)
    Assert.Equal(50m, item1Row.TotalAmount)

    Assert.Equal(50m, (item1Row.Values |> List.find (fun value -> value.PersonId = "person1")).Price)

  [<Fact>]
  member _.``addPerson should add new person with zero shares``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty

    let newPerson =
      { ViewModel.ReceiptGridState.Person.Id = "person3"
        ViewModel.ReceiptGridState.Person.Name = "Person 3" }

    let newState = state |> ViewModel.ReceiptGridState.addPerson newPerson

    Assert.Equal(3, newState.People |> List.length)
    Assert.True(newState.People |> List.contains newPerson)

    newState.Items
    |> List.iter (fun item -> Assert.True(item.Values |> List.exists (fun value -> value.PersonId = "person3")))

  [<Fact>]
  member _.``addFee should add new fee with zero amount``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let newState = state |> ViewModel.ReceiptGridState.addFee

    Assert.Equal(state.Fees.Length + 1, newState.Fees |> List.length)
    let newFee = newState.Fees |> List.last
    Assert.Equal(0m, newFee.TotalAmount)

  [<Fact>]
  member _.``removeFee should remove fee and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let fee1Id = state.Fees.[0].Id

    let newState = state |> ViewModel.ReceiptGridState.removeFee fee1Id

    Assert.Equal(state.Fees.Length - 1, newState.Fees |> List.length)
    Assert.Equal(0m, newState.FeesSubtotal.TotalAmount)

  [<Fact>]
  member _.``updateFee should update type and amount and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let item1Id = state.Items.[0].Id
    let fee1Id = state.Fees.[0].Id

    let stateWithShare =
      state |> ViewModel.ReceiptGridState.updateShare item1Id "person1" 1

    let newState =
      stateWithShare |> ViewModel.ReceiptGridState.updateFee fee1Id "Tax" 10m

    let fee1Row = newState.Fees |> List.find (fun fee -> fee.Id = fee1Id)
    Assert.Equal("Tax", fee1Row.Type)
    Assert.Equal(10m, fee1Row.TotalAmount)
    Assert.Equal(10m, newState.FeesSubtotal.TotalAmount)