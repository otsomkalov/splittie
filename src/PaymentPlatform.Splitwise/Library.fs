namespace PaymentPlatform.Splitwise

open FsToolkit.ErrorHandling
open PaymentPlatform
open Splitwise.Clients.Interfaces

type SplitwisePaymentPlatform(client: ISplitwiseClient) =
  interface IPaymentPlatform with
    member this.ListFriends() =
      client.Friend.ListAsync()
      |> Task.map (
        Seq.map (fun f ->
          { Id = string f.Id
            Name = sprintf "%s %s" f.FirstName f.LastName })
        >> List.ofSeq
      )