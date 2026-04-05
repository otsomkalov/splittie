module Bolero.Web.Util

type AsyncOp<'r> =
  | Loading
  | Finished of 'r