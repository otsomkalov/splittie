data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv-splittie" {
  resource_group_name = azurerm_resource_group.rg-splittie.name
  location            = azurerm_resource_group.rg-splittie.location

  tenant_id = data.azurerm_client_config.current.tenant_id

  name                       = "kv-splittie-${var.env}"
  sku_name                   = "standard"
  purge_protection_enabled   = false
  soft_delete_retention_days = 7

  tags = local.tags
}

resource "azurerm_key_vault_access_policy" "kvap-terraform" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Purge",
    "Recover"
  ]
}

resource "azurerm_key_vault_secret" "kvs-authentication-authority" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  name         = "Authentication--Schemes--Bearer--Authority"
  value        = var.jwt-authority

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-audience" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  name         = "Authentication--Schemes--Bearer--ValidAudience"
  value        = var.jwt-audience

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-issuer" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  name         = "Authentication--Schemes--Bearer--ValidIssuer"
  value        = var.jwt-issuer

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-openai-key" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  name         = "OpenAI--Key"
  value        = azurerm_cognitive_account.ca-splittie.primary_access_key

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-database-connection-string" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  name         = "ConnectionStrings--Database"
  value        = var.database-connection-string

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-storage-connection-string" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  name         = "ConnectionStrings--Storage"
  value        = azurerm_storage_account.st-splittie.primary_connection_string

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_access_policy" "kvap-func" {
  key_vault_id = azurerm_key_vault.kv-splittie.id
  tenant_id    = azurerm_linux_function_app.func-splittie.identity[0].tenant_id
  object_id    = azurerm_linux_function_app.func-splittie.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}
