module Bolero.Web.Repos

open System.Threading.Tasks
open BlazorBootstrap
open Domain
open Microsoft.AspNetCore.Components.Forms

type IListReceipts =
  abstract ListReceipts: unit -> Task<Receipt.Parsed list>

type IGetReceipt =
  abstract GetReceipt: string -> Task<Receipt option>

type IUploadReceipt =
  abstract UploadReceipt: IBrowserFile -> Task<ReceiptId>

type IShowNotification =
  abstract ShowNotification: ToastMessage -> unit

type IEnv =
  inherit IGetReceipt
  inherit IUploadReceipt
  inherit IShowNotification