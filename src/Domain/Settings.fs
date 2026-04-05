module Domain.Settings

[<CLIMutable>]
type ImageSettings =
  { SupportedMimeTypes: string seq }

  static member SectionName = "Image"

[<CLIMutable>]
type StorageSettings =
  { Container: string
    Queue: string }

  static member SectionName = "Storage"