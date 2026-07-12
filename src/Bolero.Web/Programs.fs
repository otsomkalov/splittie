module Bolero.Web.Programs

open System
open BlazorBootstrap
open Bolero
open Bolero.Html
open Bolero.Web.Models
open Bolero.Web.Repos
open Bolero.Web.Shared
open Bolero.Web.Util
open Domain
open Elmish
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Forms

type PersonId =
  | PersonId of string

  member this.Value = let (PersonId id) = this in id

type Person = { Id: PersonId; Name: string }

[<RequireQualifiedAccess>]
module Receipt =

  [<RequireQualifiedAccess>]
  module New =
    type Model = { File: IBrowserFile option }

    type Message =
      | FileSelected of IBrowserFile
      | UploadReceipt
      | ReceiptUploaded of ReceiptId

    let init = fun _ -> { File = None }, Cmd.none

    let update (navManager: NavigationManager) (env: #IUploadReceipt & #IShowNotification) =
      fun msg model ->
        match msg, model with
        | FileSelected file, _ -> { model with File = Some file }, Cmd.none
        | UploadReceipt, { File = Some file } -> model, Cmd.OfTask.perform env.UploadReceipt file ReceiptUploaded
        | ReceiptUploaded id, _ ->
          env.ShowNotification(ToastMessage(ToastType.Success, "Receipt uploaded successfully"))
          navManager.NavigateTo($"/receipts/{id.Value}")

          model, Cmd.none
        | _ -> model, Cmd.none

    let view (model: Model) dispatch = div {
      attr.``class`` "d-flex flex-row gap-2"

      comp<InputFile> {
        attr.``class`` "form-control"
        attr.accept "image/*"

        attr.callback "OnChange" (fun (e: InputFileChangeEventArgs) -> e.File |> FileSelected |> dispatch)
      }

      button {
        attr.``class`` "btn btn-primary"
        attr.disabled model.File.IsNone
        on.click (fun _ -> UploadReceipt |> dispatch)

        "Upload"
      }
    }

  [<RequireQualifiedAccess>]
  module Details =
    type Model =
      { People: Person list
        Grid: ViewModel.ReceiptGridState.State option
        IsEditMode: bool }

    module Item =
      type RowModel =
        { Row: ViewModel.ReceiptGridState.ItemRow
          IsEditMode: bool }

      type Message =
        | Add
        | Remove of ItemId
        | NameUpdate of ItemId * name: string * amount: decimal
        | SetShare of ItemId * PersonId * int

      let internal update msg model =
        match msg with
        | Add ->
          let grid = model.Grid |> Option.map ViewModel.ReceiptGridState.addItem
          { model with Grid = grid }, Cmd.none
        | Remove itemId ->
          let grid = model.Grid |> Option.map (ViewModel.ReceiptGridState.removeItem itemId)
          { model with Grid = grid }, Cmd.none
        | NameUpdate(itemId, name, amount) ->
          let grid =
            model.Grid
            |> Option.map (ViewModel.ReceiptGridState.updateItem itemId name amount)

          { model with Grid = grid }, Cmd.none
        | SetShare(itemId, personId, share) ->
          let grid =
            model.Grid
            |> Option.map (ViewModel.ReceiptGridState.updateShare itemId personId.Value share)

          { model with Grid = grid }, Cmd.none

      let internal render (row: ViewModel.ReceiptGridState.ItemRow) isEditMode (dispatch: Message -> unit) = tr {
        attr.``class`` (
          [ "align-middle"
            if row.IsWarning then
              "table-warning"
            elif row.IsSuccess then
              "table-success" ]
          |> String.concat " "
        )

        td {
          cond isEditMode <| function
            | true ->
              input {
                attr.``class`` "form-control"
                attr.``type`` "text"
                attr.value row.Name
                on.change (fun e -> NameUpdate(row.Id, string e.Value, row.TotalAmount) |> dispatch)
              }
            | false -> text row.Name
        }

        forEach row.Values <| fun value ->
          concat {
            td {
              attr.``class`` (getCellClass value.Price)

              input {
                attr.``class`` "form-control"
                attr.``type`` "number"
                attr.min 0

                attr.value (value.Share |> string)

                on.change (fun e -> SetShare(row.Id, PersonId value.PersonId, int (string e.Value)) |> dispatch)
              }
            }

            td {
              attr.``class`` (getCellClass value.Price)

              sprintf "%.2f" value.Price
            }
          }

        td {
          cond isEditMode <| function
            | true ->
              input {
                attr.``class`` "form-control"
                attr.``type`` "number"
                attr.step "0.01"
                attr.value (row.TotalAmount |> string)
                on.change (fun e -> NameUpdate(row.Id, row.Name, decimal (string e.Value)) |> dispatch)
              }
            | false -> textf "%.2f" row.TotalAmount
        }

        cond isEditMode <| function
          | true ->
            td {
              button {
                attr.``class`` "btn btn-danger"
                on.click (fun _ -> Remove row.Id |> dispatch)
                "Remove"
              }
            }
          | false -> empty()
      }

      type RowComponent() =
        inherit ElmishComponent<RowModel, Message>()

        override _.View model dispatch = render model.Row model.IsEditMode dispatch

    module Fee =
      type RowModel =
        { Row: ViewModel.ReceiptGridState.FeeRow
          IsEditMode: bool }

      type Message =
        | Add
        | Remove of FeeId
        | TypeUpdate of FeeId * newType: string * amount: decimal

      let update msg model =
        match msg with
        | Add ->
          let grid = model.Grid |> Option.map ViewModel.ReceiptGridState.addFee
          { model with Grid = grid }, Cmd.none
        | Remove feeId ->
          let grid = model.Grid |> Option.map (ViewModel.ReceiptGridState.removeFee feeId)
          { model with Grid = grid }, Cmd.none
        | TypeUpdate(feeId, newType, amount) ->
          let grid =
            model.Grid
            |> Option.map (ViewModel.ReceiptGridState.updateFee feeId newType amount)

          { model with Grid = grid }, Cmd.none

      let internal render (row: ViewModel.ReceiptGridState.FeeRow) isEditMode (dispatch: Message -> unit) = tr {
        attr.``class`` "align-middle"

        td {
          cond isEditMode <| function
            | true ->
              input {
                attr.``class`` "form-control"
                attr.``type`` "text"
                attr.value row.Type
                on.change (fun e -> TypeUpdate(row.Id, string e.Value, row.TotalAmount) |> dispatch)
              }
            | false -> text row.Type
        }

        forEach row.Values <| fun value ->
          td {
            attr.colspan 2
            attr.``class`` (getCellClass value.Price)

            sprintf "%.2f" value.Price
          }

        td {
          cond isEditMode <| function
            | true ->
              input {
                attr.``class`` "form-control"
                attr.``type`` "number"
                attr.step "0.01"
                attr.value (row.TotalAmount |> string)
                on.change (fun e -> TypeUpdate(row.Id, row.Type, decimal (string e.Value)) |> dispatch)
              }
            | false -> textf "%.2f" row.TotalAmount
        }

        cond isEditMode <| function
          | true ->
            td {
              button {
                attr.``class`` "btn btn-danger"
                on.click (fun _ -> Remove row.Id |> dispatch)
                "Remove"
              }
            }
          | false -> empty()
      }

      type RowComponent() =
        inherit ElmishComponent<RowModel, Message>()

        override _.View model dispatch = render model.Row model.IsEditMode dispatch

    module Person =
      type Message =
        | UpdateName of PersonId * string
        | Add

      let internal update msg model =
        match msg with
        | UpdateName(personId, name) ->
          let newPeople =
            model.People
            |> List.map (fun p -> if p.Id = personId then { p with Name = name } else p)

          let grid =
            model.Grid
            |> Option.map (ViewModel.ReceiptGridState.updatePersonName personId.Value name)

          { model with
              People = newPeople
              Grid = grid },
          Cmd.none
        | Add ->
          let newId = Guid.NewGuid().ToString() |> PersonId
          let person = { Id = newId; Name = "Person" }
          let newPeople = model.People @ [ person ]

          let grid =
            model.Grid
            |> Option.map (
              ViewModel.ReceiptGridState.addPerson
                { ViewModel.ReceiptGridState.Person.Id = person.Id.Value
                  ViewModel.ReceiptGridState.Person.Name = person.Name }
            )

          { model with
              People = newPeople
              Grid = grid },
          Cmd.none

    module Receipt =
      type Message =
        | Load of string
        | Receipt of AsyncOp<Receipt option>

      let update (env: #IGetReceipt) wrap msg model =
        match msg with
        | Load receiptId -> model, Cmd.OfTask.perform env.GetReceipt receiptId (Finished >> Receipt >> wrap)
        | Receipt(Finished(Some(Receipt.Parsed receipt))) ->
          let people =
            model.People
            |> List.map (fun p ->
              { ViewModel.ReceiptGridState.Person.Id = p.Id.Value
                ViewModel.ReceiptGridState.Person.Name = p.Name })

          let grid = ViewModel.ReceiptGridState.from receipt people Map.empty

          { model with Grid = Some grid }, Cmd.none
        | Receipt(Finished(Some(Receipt.Unparsed _))) -> { model with Grid = None }, Cmd.none
        | _ -> { model with Grid = None }, Cmd.none

    type Message =
      | ToggleEditMode
      | ItemMsg of Item.Message
      | FeeMsg of Fee.Message
      | PersonMsg of Person.Message
      | ReceiptMsg of Receipt.Message

    let init receiptId =
      fun _ ->
        { People = [ { Id = PersonId "1"; Name = "Me" } ]
          Grid = None
          IsEditMode = false },
        Cmd.batch [ Cmd.ofMsg (ReceiptMsg(Receipt.Message.Load receiptId)) ]

    let update (env: #IGetReceipt) msg model =
      match msg with
      | ToggleEditMode ->
        { model with
            IsEditMode = not model.IsEditMode },
        Cmd.none
      | ItemMsg msg -> Item.update msg model
      | FeeMsg msg -> Fee.update msg model
      | PersonMsg msg -> Person.update msg model
      | ReceiptMsg msg -> Receipt.update env ReceiptMsg msg model

    let private renderHeader (grid: ViewModel.ReceiptGridState.State) isEditMode dispatch =
      let defaultColumnStyle = attr.style "min-width: 100px"

      thead {
        tr {
          th {
            attr.rowspan 2
            "Name"
          }

          forEach grid.People <| fun person ->
            th {
              attr.colspan 2

              cond isEditMode <| function
                | true ->
                  input {
                    attr.``class`` "form-control d-inline-block w-full"
                    attr.``type`` "text"
                    attr.value person.Name

                    on.change (fun e ->
                      PersonMsg(Person.Message.UpdateName(PersonId person.Id, string e.Value))
                      |> dispatch)
                  }
                | false -> text person.Name
            }

          th {
            attr.rowspan 2

            attr.``class`` "item-price-column"

            "Price"
          }

          cond isEditMode <| function
            | true ->
              th {
                attr.rowspan 2
                "Action"
              }
            | false -> empty()
        }

        tr {
          forEach grid.People <| fun _ ->
            concat {
              th {
                attr.``class`` "quantity-column"

                defaultColumnStyle
                "Quantity"
              }

              th {
                attr.``class`` "user-share-column"

                defaultColumnStyle
                "Price"
              }
            }
        }
      }

    let private renderFeesSubtotalRow (row: ViewModel.ReceiptGridState.FeesSubtotalRow) isEditMode =

      tr {
        td { strong { "Fees Subtotal" } }

        forEach row.Values <| fun value ->
          td {
            attr.colspan 2
            attr.``class`` (getCellClass value.Price)

            strong { sprintf "%.2f" value.Price }
          }

        td { strong { sprintf "%.2f" row.TotalAmount } }

        cond isEditMode <| function
          | true -> td { "" }
          | false -> empty()
      }

    let private renderItemsSubtotalRow (row: ViewModel.ReceiptGridState.ItemsSubtotalRow) isEditMode = tr {
      td { strong { "Items Subtotal" } }

      forEach row.Values <| fun value ->
        concat {
          td {
            attr.``class`` (getCellClass value.Price)

            value.Share |> string
          }

          td {
            attr.``class`` (getCellClass value.Price)

            strong { sprintf "%.2f" value.Price }
          }
        }

      td { strong { sprintf "%.2f" row.TotalAmount } }

      cond isEditMode <| function
        | true -> td { "" }
        | false -> empty()
    }

    let private renderTotalRow (row: ViewModel.ReceiptGridState.TotalRow) isEditMode = tfoot {
      tr {
        td { strong { "Total" } }

        forEach row.Values <| fun value ->
          concat {
            td {
              attr.``class`` (getCellClass value.Price)

              value.Share |> string
            }

            td {
              attr.``class`` (getCellClass value.Price)

              strong { sprintf "%.2f" value.Price }
            }
          }

        td { strong { sprintf "%.2f" row.TotalAmount } }

        cond isEditMode <| function
          | true -> td { "" }
          | false -> empty()
      }
    }

    let view model dispatch =
      match model.Grid with
      | None -> Loading.render () dispatch
      | Some grid -> div {
          attr.``class`` "d-flex flex-column gap-1"
          attr.style "width: max-content"

          div {
            attr.``class`` "d-flex justify-content-between align-items-center"

            div {
              attr.``class`` "d-flex align-items-center gap-1"

              comp<Tooltip> {
                "Title" => "Add person"
                "Placement" => TooltipPlacement.Top
                "Class" => "d-inline-block"

                button {
                  attr.``class`` "btn btn-primary"

                  on.click (fun _ -> dispatch (PersonMsg Person.Message.Add))

                  i { attr.``class`` "bi bi-person-plus" }
                }
              }

              comp<Tooltip> {
                "Title" => "Export to Splitwise"

                button {
                  attr.``class`` "btn"

                  img { attr.src "https://secure.splitwise.com/favicon.ico" }
                }
              }

              cond model.IsEditMode <| function
                | true ->
                  button {
                    attr.``class`` "btn btn-outline-primary"
                    on.click (fun _ -> dispatch (ItemMsg Item.Message.Add))
                    "Add Item"
                  }
                | false -> empty()

              cond model.IsEditMode <| function
                | true ->
                  button {
                    attr.``class`` "btn btn-outline-primary"
                    on.click (fun _ -> dispatch (FeeMsg Fee.Message.Add))
                    "Add Fee"
                  }
                | false -> empty()
            }

            div {
              input {
                attr.``type`` "checkbox"
                attr.``class`` "btn-check"
                attr.id "edit-mode-toggle"
                attr.autocomplete "off"
                attr.``checked`` model.IsEditMode

                on.change (fun _ -> dispatch ToggleEditMode)
              }

              label {
                attr.``class`` "btn btn-outline-warning"
                attr.``for`` "edit-mode-toggle"

                i {
                  attr.``class`` "bi bi-pencil"
                  attr.style "color: black;"

                }
              }
            }
          }

          div {
            attr.``class`` "d-flex"

            table {
              attr.``class`` "table table-bordered w-auto"

              renderHeader grid model.IsEditMode dispatch

              tbody {
                forEach grid.Items <| fun itemRow ->
                  let itemRowModel: Item.RowModel =
                    { Row = itemRow
                      IsEditMode = model.IsEditMode }

                  ecomp<Item.RowComponent, Item.RowModel, Item.Message> itemRowModel (ItemMsg >> dispatch) {
                    attr.key itemRow.Id
                  }

                renderItemsSubtotalRow grid.ItemsSubtotal model.IsEditMode

                forEach grid.Fees <| fun feeRow ->
                  let feeRowModel: Fee.RowModel =
                    { Row = feeRow
                      IsEditMode = model.IsEditMode }

                  ecomp<Fee.RowComponent, Fee.RowModel, Fee.Message> feeRowModel (FeeMsg >> dispatch) {
                    attr.key feeRow.Id
                  }

                renderFeesSubtotalRow grid.FeesSubtotal model.IsEditMode
              }

              renderTotalRow grid.Total model.IsEditMode
            }
          }
        }