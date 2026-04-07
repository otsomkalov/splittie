terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=4.67.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

locals {
  tags = {
    env  = var.env
    name = "splittie"
  }
}

resource "azurerm_resource_group" "rg-splittie" {
  name     = "rg-splittie-${var.env}"
  location = "France Central"

  tags = local.tags
}

resource "azurerm_application_insights" "appi-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  name             = "appi-splittie-${var.env}"
  application_type = "web"
}

resource "azurerm_log_analytics_workspace" "appi-ws-splittie" {
  name                = "appi-ws-splittie-${var.env}"
  location            = azurerm_resource_group.rg-splittie.location
  resource_group_name = azurerm_resource_group.rg-splittie.name

  sku               = "PerGB2018"
  retention_in_days = 30

  tags = local.tags
}

# Identity

resource "azurerm_user_assigned_identity" "id-ado-pipeline-identity" {
  location            = azurerm_resource_group.rg-splittie.location
  resource_group_name = azurerm_resource_group.rg-splittie.name

  name = "id-ado-splittie-${var.env}"
}

resource "azurerm_storage_account" "st-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  name                     = "stsplittie${var.env}"
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = local.tags
}

resource "azurerm_role_assignment" "ra-id-ado-pipeline-identity-blob-access" {
  scope        = azurerm_storage_account.st-splittie.id
  principal_id = azurerm_user_assigned_identity.id-ado-pipeline-identity.principal_id

  role_definition_name = "Storage Blob Data Contributor"
}

resource "azurerm_storage_container" "stc-splittie-receipts" {
  storage_account_id = azurerm_storage_account.st-splittie.id

  name = "receipts"
}

resource "azurerm_storage_container" "stc-func-api-deployments" {
  storage_account_id = azurerm_storage_account.st-splittie.id

  name = "func-api-deployments"
}

resource "azurerm_storage_queue" "stq-splittie-receipts" {
  storage_account_id = azurerm_storage_account.st-splittie.id

  name = "receipts"
}

resource "azurerm_service_plan" "asp-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  name     = "asp-splittie-${var.env}"
  os_type  = "Linux"
  sku_name = "FC1"

  tags = local.tags
}

resource "azurerm_function_app_flex_consumption" "func-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  service_plan_id = azurerm_service_plan.asp-splittie.id

  name = "func-splittie-api-${var.env}"

  runtime_name    = "dotnet-isolated"
  runtime_version = "9.0"

  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.st-splittie.primary_access_key
  storage_container_endpoint  = "${azurerm_storage_account.st-splittie.primary_blob_endpoint}${azurerm_storage_container.stc-func-api-deployments.name}"
  storage_container_type      = "blobContainer"

  instance_memory_in_mb  = 512
  maximum_instance_count = 10

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.appi-splittie.connection_string
  }

  app_settings = {
    KeyValueName = azurerm_key_vault.kv-splittie.name

    Authentication__Schemes__Bearer__Authority     = var.jwt-authority
    Authentication__Schemes__Bearer__ValidAudience = var.jwt-audience
    Authentication__Schemes__Bearer__ValidIssuer   = var.jwt-issuer

    Image__SupportedMimeTypes__0 = "image/jpeg"

    OpenAI__Endpoint = azurerm_cognitive_account.ca-splittie.endpoint
    OpenAI__Model    = azurerm_cognitive_deployment.openai_model.name
  }

  tags = local.tags
}

resource "azurerm_storage_account_static_website" "st-sw-splittie" {
  storage_account_id = azurerm_storage_account.st-splittie.id

  error_404_document = "index.html"
  index_document     = "index.html"
}

resource "azurerm_cognitive_account" "ca-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  name = "ca-splittie-${var.env}"

  kind     = "OpenAI"
  sku_name = "S0"

  tags = local.tags
}

resource "azurerm_cognitive_deployment" "openai_model" {
  cognitive_account_id = azurerm_cognitive_account.ca-splittie.id

  name = "ca-cd-splittie-${var.model-name}-${var.env}"

  model {
    format  = "OpenAI"
    name    = var.model-name
    version = var.model-version
  }

  sku {
    name     = "GlobalStandard"
    capacity = var.model-capacity
  }
}
