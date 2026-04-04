module Bolero.Web.Repos

open System.Net.Http
open System.Threading.Tasks
open Bolero.Web.Models
open Microsoft.AspNetCore.Components.Forms

type IListReceipts =
  abstract ListReceipts: unit -> Task<Receipt.Parsed list>

type IGetReceipt =
  abstract GetReceipt: string -> Task<Receipt>

type IUploadReceipt =
  abstract UploadReceipt: IBrowserFile -> Task<unit>

type IEnv =
  inherit IGetReceipt