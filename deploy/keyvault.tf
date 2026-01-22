data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv-glovo-split" {
  resource_group_name = azurerm_resource_group.rg-glovo-split.name
  location            = azurerm_resource_group.rg-glovo-split.location

  name                       = "kv-glovo-split-${var.env}"
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = false
  soft_delete_retention_days = 7

  tags = local.tags
}

resource "azurerm_key_vault_access_policy" "kvap-terraform" {
  key_vault_id = azurerm_key_vault.kv-glovo-split.id
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
  key_vault_id = azurerm_key_vault.kv-glovo-split.id
  name         = "Authentication--Schemes--Bearer--Authority"
  value        = var.jwt-authority

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-audience" {
  key_vault_id = azurerm_key_vault.kv-glovo-split.id
  name         = "Authentication--Schemes--Bearer--ValidAudience"
  value        = var.jwt-audience

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-issuer" {
  key_vault_id = azurerm_key_vault.kv-glovo-split.id
  name         = "Authentication--Schemes--Bearer--ValidIssuer"
  value        = var.jwt-issuer

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_access_policy" "kvap-func" {
  key_vault_id = azurerm_key_vault.kv-glovo-split.id
  tenant_id    = azurerm_linux_function_app.func-glovo-split.identity[0].tenant_id
  object_id    = azurerm_linux_function_app.func-glovo-split.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}
