terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=4.57.0"
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

resource "azurerm_user_assigned_identity" "ado-pipeline-identity" {
  location            = azurerm_resource_group.rg-splittie.location
  name                = "ado-pipeline-identity-splittie-${var.env}"
  resource_group_name = azurerm_resource_group.rg-splittie.name
}

resource "azurerm_storage_account" "st-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  name                     = "stsplittie${var.env}"
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = local.tags
}

resource "azurerm_role_assignment" "ado-pipeline-identity-blob-access" {
  scope                = azurerm_storage_account.st-splittie.id
  principal_id         = azurerm_user_assigned_identity.ado-pipeline-identity.principal_id
  role_definition_name = "Storage Blob Data Contributor"
}

resource "azurerm_storage_container" "stc-splittie-input" {
  storage_account_id = azurerm_storage_account.st-splittie.id

  name = "input"
}

resource "azurerm_service_plan" "asp-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  name     = "asp-splittie-${var.env}"
  os_type  = "Linux"
  sku_name = "Y1"

  tags = local.tags
}

resource "azurerm_linux_function_app" "func-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  storage_account_name       = azurerm_storage_account.st-splittie.name
  storage_account_access_key = azurerm_storage_account.st-splittie.primary_access_key
  service_plan_id            = azurerm_service_plan.asp-splittie.id

  name = "func-splittie-${var.env}"

  functions_extension_version = "~4"

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_key = azurerm_application_insights.appi-splittie.instrumentation_key
    app_scale_limit          = 10

    application_stack {
      dotnet_version              = "9.0"
      use_dotnet_isolated_runtime = true
    }
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
