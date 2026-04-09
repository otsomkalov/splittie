[<RequireQualifiedAccess>]
module Shared.JSON

open System.Text.Json
open System.Text.Json.Serialization

let private encoding =
  JsonUnionEncoding.NamedFields
  ||| JsonUnionEncoding.InternalTag
  ||| JsonUnionEncoding.UnwrapRecordCases
  ||| JsonUnionEncoding.UnwrapOption
  ||| JsonUnionEncoding.UnwrapSingleCaseUnions

let FsharpOptions =
  JsonFSharpOptions(unionEncoding = encoding, unionTagName = "$type")

let SerializerOptions =
  JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

FsharpOptions.AddToJsonSerializerOptions SerializerOptions

let serialize value =
  JsonSerializer.Serialize(value, SerializerOptions)