module Infra.Settings

[<CLIMutable>]
type DatabaseSettings =
  { Name: string }

  static member SectionName = "Database"