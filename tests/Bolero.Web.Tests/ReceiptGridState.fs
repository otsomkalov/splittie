namespace Bolero.Web.Tests

open Xunit
open FsUnit.Xunit
open Bolero.Web
open Bolero.Web.Models

type ReceiptGridState() =

  let people: ViewModel.ReceiptGridState.Person list =
    [ { Id = "person1"; Name = "Person 1" }; { Id = "person2"; Name = "Person 2" } ]

  let receipt: Receipt.Parsed =
    { Id = ReceiptId "receipt1"
      Date = System.DateTime.Now
      Items =
        [ { Id = ItemId "item1"
            Name = "Item 1"
            Quantity = 1m
            Amount = 10m }
          { Id = ItemId "item2"
            Name = "Item 2"
            Quantity = 1m
            Amount = 20m } ]
      Fees =
        [ { Id = FeeId "fee1"
            Type = "Service Fee"
            Amount = 3m } ]
      Total = 33m }

  [<Fact>]
  member _.``from should initialize state correctly with no shares``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty

    state.People |> should equal people
    state.Items |> List.length |> should equal 2
    state.Fees |> List.length |> should equal 1

    // Item 1
    let item1 = state.Items |> List.find (fun item -> item.Id = ItemId "item1")
    item1.TotalAmount |> should equal 10m
    item1.IsWarning |> should be True
    item1.Values |> List.iter (fun value -> value.Price |> should equal 0m)

    // Totals should be 33 since receipt.Total is 33 and it's not recalculated to 0 if no shares
    state.Total.TotalAmount |> should equal 33m
    state.Total.Values |> List.iter (fun value -> value.Price |> should equal 0m)

  [<Fact>]
  member _.``recalculate should compute correct prices based on shares``() =
    let shares =
      Map
        [ (ItemId "item1", "person1"), 1
          (ItemId "item2", "person1"), 1
          (ItemId "item2", "person2"), 1 ]

    let state = ViewModel.ReceiptGridState.from receipt people shares

    // Item 1: 10m total, person1 has 1 share, person2 has 0. person1 pays 10m, person2 pays 0.
    let item1Row = state.Items |> List.find (fun item -> item.Id = ItemId "item1")

    let person1_item1 =
      item1Row.Values |> List.find (fun value -> value.PersonId = "person1")

    person1_item1.Price |> should equal 10m

    let person2_item1 =
      item1Row.Values |> List.find (fun value -> value.PersonId = "person2")

    person2_item1.Price |> should equal 0m

    // Item 2: 20m total, person1 has 1 share, person2 has 1. Both pay 10m.
    let item2Row = state.Items |> List.find (fun item -> item.Id = ItemId "item2")

    let person1_item2 =
      item2Row.Values |> List.find (fun value -> value.PersonId = "person1")

    person1_item2.Price |> should equal 10m

    let person2_item2 =
      item2Row.Values |> List.find (fun value -> value.PersonId = "person2")

    person2_item2.Price |> should equal 10m

    // Items Subtotal: person1 pays 20m, person2 pays 10m. Total 30m.
    state.ItemsSubtotal.TotalAmount |> should equal 30m

    (state.ItemsSubtotal.Values
     |> List.find (fun value -> value.PersonId = "person1"))
      .Price
    |> should equal 20m

    (state.ItemsSubtotal.Values
     |> List.find (fun value -> value.PersonId = "person2"))
      .Price
    |> should equal 10m

    // Fee: 3m total. Distributed proportional to item subtotals.
    // person1: 20/30 * 3 = 2m
    // person2: 10/30 * 3 = 1m
    let fee1Row = state.Fees |> List.find (fun fee -> fee.Id = FeeId "fee1")

    (fee1Row.Values |> List.find (fun value -> value.PersonId = "person1")).Price
    |> should (equalWithin 0.001m) 2m

    (fee1Row.Values |> List.find (fun value -> value.PersonId = "person2")).Price
    |> should (equalWithin 0.001m) 1m

    // Total: person1 = 20 + 2 = 22m, person2 = 10 + 1 = 11m. Total 33m.
    state.Total.TotalAmount |> should equal 33m

    (state.Total.Values |> List.find (fun value -> value.PersonId = "person1")).Price
    |> should (equalWithin 0.001m) 22m

    (state.Total.Values |> List.find (fun value -> value.PersonId = "person2")).Price
    |> should (equalWithin 0.001m) 11m

  [<Fact>]
  member _.``updateShare should update share and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty

    let newState =
      state |> ViewModel.ReceiptGridState.updateShare (ItemId "item1") "person1" 1

    let item1Row = newState.Items |> List.find (fun item -> item.Id = ItemId "item1")

    let person1_item1 =
      item1Row.Values |> List.find (fun value -> value.PersonId = "person1")

    person1_item1.Share |> should equal 1
    person1_item1.Price |> should equal 10m
    newState.Total.TotalAmount |> should be (greaterThan 0m)

  [<Fact>]
  member _.``addItem should add new item with zero amount and shares``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let newState = state |> ViewModel.ReceiptGridState.addItem

    newState.Items |> List.length |> should equal (state.Items.Length + 1)
    let newItem = newState.Items |> List.last
    newItem.TotalAmount |> should equal 0m
    newItem.Values |> List.iter (fun value -> value.Share |> should equal 0)

  [<Fact>]
  member _.``removeItem should remove item and recalculate``() =
    let shares = Map [ (ItemId "item1", "person1"), 1 ]
    let state = ViewModel.ReceiptGridState.from receipt people shares
    let newState = state |> ViewModel.ReceiptGridState.removeItem (ItemId "item1")

    newState.Items |> List.length |> should equal 1

    newState.Items
    |> List.exists (fun item -> item.Id = ItemId "item1")
    |> should be False

    newState.Total.TotalAmount
    |> should equal (newState.ItemsSubtotal.TotalAmount + newState.FeesSubtotal.TotalAmount)

  [<Fact>]
  member _.``updateItem should update name and amount and recalculate``() =
    let shares = Map [ (ItemId "item1", "person1"), 1 ]
    let state = ViewModel.ReceiptGridState.from receipt people shares

    let newState =
      state |> ViewModel.ReceiptGridState.updateItem (ItemId "item1") "New Name" 50m

    let item1Row = newState.Items |> List.find (fun item -> item.Id = ItemId "item1")
    item1Row.Name |> should equal "New Name"
    item1Row.TotalAmount |> should equal 50m

    (item1Row.Values |> List.find (fun value -> value.PersonId = "person1")).Price
    |> should equal 50m

  [<Fact>]
  member _.``addPerson should add new person with zero shares``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty

    let newPerson =
      { ViewModel.ReceiptGridState.Person.Id = "person3"
        ViewModel.ReceiptGridState.Person.Name = "Person 3" }

    let newState = state |> ViewModel.ReceiptGridState.addPerson newPerson

    newState.People |> List.length |> should equal 3
    newState.People |> List.contains newPerson |> should be True

    newState.Items
    |> List.iter (fun item ->
      item.Values
      |> List.exists (fun value -> value.PersonId = "person3")
      |> should be True)

  [<Fact>]
  member _.``addFee should add new fee with zero amount``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let newState = state |> ViewModel.ReceiptGridState.addFee

    newState.Fees |> List.length |> should equal (state.Fees.Length + 1)
    let newFee = newState.Fees |> List.last
    newFee.TotalAmount |> should equal 0m

  [<Fact>]
  member _.``removeFee should remove fee and recalculate``() =
    let state = ViewModel.ReceiptGridState.from receipt people Map.empty
    let newState = state |> ViewModel.ReceiptGridState.removeFee (FeeId "fee1")

    newState.Fees |> List.length |> should equal 0
    newState.FeesSubtotal.TotalAmount |> should equal 0m

  [<Fact>]
  member _.``updateFee should update type and amount and recalculate``() =
    let shares = Map [ (ItemId "item1", "person1"), 1 ]
    let state = ViewModel.ReceiptGridState.from receipt people shares

    let newState =
      state |> ViewModel.ReceiptGridState.updateFee (FeeId "fee1") "Tax" 10m

    let fee1Row = newState.Fees |> List.find (fun fee -> fee.Id = FeeId "fee1")
    fee1Row.Type |> should equal "Tax"
    fee1Row.TotalAmount |> should equal 10m
    newState.FeesSubtotal.TotalAmount |> should equal 10m