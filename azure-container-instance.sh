#!/bin/bash
set -e

APP_NAME=$1
IMAGE_NAME=$2
IMAGE_TAG=$3
RESOURCE_GROUP=$4
PORT=${5:-8080}

if [ $# -lt 4 ]; then
  echo "Usage: $0 <app_name> <image_name> <image_tag> <resource_group> [port]"
  exit 1
fi

if [ -z "$AZURE_KEY_VAULT" ]; then
  echo "Error: AZURE_KEY_VAULT must be set"
  exit 1
fi

echo "Fetching location for resource group..."
LOCATION=$(az group show --name "$RESOURCE_GROUP" --query location -o tsv)

FULL_IMAGE="$IMAGE_NAME:$IMAGE_TAG"
DNS_NAME="${APP_NAME}-${RANDOM}"

echo "Deleting old container..."
az container delete \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --yes || true

echo "Deploying container..."

az container create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --image "$FULL_IMAGE" \
  --dns-name-label "$DNS_NAME" \
  --ports "$PORT" \
  --location "$LOCATION" \
  --os-type Linux \
  --cpu 1 \
  --memory 2 \
  --restart-policy Always \
  --assign-identity \
  --environment-variables \
    AZURE_KEY_VAULT="$AZURE_KEY_VAULT" \
    ASPNETCORE_URLS="http://+:$PORT" \
    ZOHO_BASE_URL="https://www.zohoapis.in" \
    ZOHO_TOKEN_URL="https://accounts.zoho.in/oauth/v2/token" \
    GRANT_TYPE="refresh_token"

echo "Fetching URL..."
FQDN=$(az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --query ipAddress.fqdn \
  -o tsv)

echo "Deployment successful"
echo "URL: http://$FQDN:$PORT"
