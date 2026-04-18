#!/bin/bash
set -e

APP_NAME=$1
IMAGE_NAME=$2
IMAGE_TAG=$3
RESOURCE_GROUP=$4
PORT=${5:-8080}

# Validate inputs
if [ $# -lt 4 ]; then
  echo "Usage: $0 <app_name> <image_name> <image_tag> <resource_group> [port]"
  exit 1
fi

# Validate Docker credentials
if [ -z "$DOCKER_USERNAME" ] || [ -z "$DOCKER_PASSWORD" ]; then
  echo "Error: DOCKER_USERNAME and DOCKER_PASSWORD must be set"
  exit 1
fi

# Validate Key Vault
if [ -z "$AZURE_KEY_VAULT" ]; then
  echo "Error: AZURE_KEY_VAULT must be set"
  exit 1
fi

# Get location
echo "Fetching location for resource group..."
LOCATION=$(az group show --name "$RESOURCE_GROUP" --query location -o tsv)

FULL_IMAGE="$IMAGE_NAME:$IMAGE_TAG"
DNS_NAME="${APP_NAME}-${RANDOM}"

echo "Deploying container..."

# Delete existing container (idempotent)
az container delete \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --yes || true

# Create container
az container create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --image "$FULL_IMAGE" \
  --registry-login-server docker.io \
  --registry-username "$DOCKER_USERNAME" \
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
    ASPNETCORE_URLS="http://+:$PORT"

# Get FQDN
echo "Fetching public URL..."
FQDN=$(az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --query ipAddress.fqdn \
  -o tsv)

echo "Deployment successful"
echo "URL: http://$FQDN:$PORT"
