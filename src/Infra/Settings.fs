module Infra.Settings

[<CLIMutable>]
type DatabaseSettings =
  { ConnectionString: string
    Name: string }

  static member SectionName = "Database"