export VAULT_TOKEN="dev-root"
export VAULT_ADDR='http://0.0.0.0:8200'

echo "Enabling KV secrets engine at path 'aspireshop'"
vault secrets enable -path='aspireshop' -version=2 kv

echo "Writing stripe keys"
vault kv put -mount=aspireshop stripe public_key="" secret_key=""

#vault secrets list
exit 0


# curl -H "X-Vault-Token: ${VAULT_TOKEN}"  -X GET ${VAULT_ADDR}/v1/aspireshop/config
# curl -H "X-Vault-Token: ${VAULT_TOKEN}"  -X GET ${VAULT_ADDR}/v1/aspireshop/metadata/stripe