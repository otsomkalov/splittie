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
    name = "glovo-split"
  }
}

resource "azurerm_resource_group" "rg-glovo-split" {
  name     = "rg-glovo-split-${var.env}"
  location = "France Central"

  tags = local.tags
}

resource "azurerm_application_insights" "appi-glovo-split" {
  resource_group_name = azurerm_resource_group.rg-glovo-split.name
  location            = azurerm_resource_group.rg-glovo-split.location

  name             = "appi-glovo-split-${var.env}"
  application_type = "web"
}

resource "azurerm_storage_account" "st-glovo-split" {
  resource_group_name = azurerm_resource_group.rg-glovo-split.name
  location            = azurerm_resource_group.rg-glovo-split.location

  name                     = "stglovosplit${var.env}"
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = local.tags
}

resource "azurerm_storage_container" "stc-glovo-split-input" {
  storage_account_id = azurerm_storage_account.st-glovo-split.id

  name = "input"
}

resource "azurerm_service_plan" "asp-glovo-split" {
  resource_group_name = azurerm_resource_group.rg-glovo-split.name
  location            = azurerm_resource_group.rg-glovo-split.location

  name     = "asp-glovo-split-${var.env}"
  os_type  = "Linux"
  sku_name = "Y1"

  tags = local.tags
}

resource "azurerm_linux_function_app" "func-glovo-split" {
  resource_group_name = azurerm_resource_group.rg-glovo-split.name
  location            = azurerm_resource_group.rg-glovo-split.location

  storage_account_name       = azurerm_storage_account.st-glovo-split.name
  storage_account_access_key = azurerm_storage_account.st-glovo-split.primary_access_key
  service_plan_id            = azurerm_service_plan.asp-glovo-split.id

  name = "func-glovo-split-${var.env}"

  functions_extension_version = "~4"

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_key = azurerm_application_insights.appi-glovo-split.instrumentation_key
    app_scale_limit          = 10

    application_stack {
      dotnet_version              = "9.0"
      use_dotnet_isolated_runtime = true
    }
  }

  tags = local.tags
}

resource "azurerm_storage_account_static_website" "st-sw-glovo-split" {
  storage_account_id = azurerm_storage_account.st-glovo-split.id

  error_404_document = "index.html"
  index_document     = "index.html"
}
