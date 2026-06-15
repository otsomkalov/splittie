variable "jwt-audience" {
  type = string
}

variable "jwt-authority" {
  type = string
}

variable "jwt-issuer" {
  type = string
}

variable "supported-image-types" {
  type = list(string)
}

variable "model-name" {
  type = string
}

variable "model-version" {
  type = string
}

variable "model-capacity" {
  type = number
}

variable "database-connection-string" {
  type = string
}

variable "web-url" {
  type = string
}

variable "env" {
  type    = string
  default = "dev"
}

