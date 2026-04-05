module Domain.Settings

[<CLIMutable>]
type ImageSettings =
  { SupportedMimeTypes: string seq }

  static member SectionName = "Image"