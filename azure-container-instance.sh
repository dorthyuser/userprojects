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

if [ -z "$DOCKER_USERNAME" ] || [ -z "$DOCKER_PASSWORD" ]; then
  echo "Error: DOCKER_USERNAME and DOCKER_PASSWORD must be set"
  exit 1
fi

echo "Fetching location for resource group..."
LOCATION=$(az group show --name "$RESOURCE_GROUP" --query location -o tsv)

FULL_IMAGE="$IMAGE_NAME:$IMAGE_TAG"
DNS_NAME="${APP_NAME}-${RANDOM}"

echo "Deleting old container (if exists)..."
az container delete \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --yes || true

echo "Waiting for cleanup..."
sleep 10

echo "Deploying container with retry..."
MAX_RETRIES=3
RETRY_DELAY=15
SUCCESS=false

for i in $(seq 1 $MAX_RETRIES); do
  echo "Attempt $i..."
  if az container create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_NAME" \
    --image "$FULL_IMAGE" \
    --registry-login-server index.docker.io \
    --registry-username "$DOCKER_USERNAME" \
    --registry-password "$DOCKER_PASSWORD" \
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
    ZOHO_BASE_URL="ZOHO-BASE-URL" \
    ZOHO-CLIENT-ID="ZOHO-CLIENT-ID" \
    ZOHO-CLIENT-SECRET="ZOHO-CLIENT-SECRET" \
    ZOHO_TOKEN_URL="ZOHO-TOKEN-URL" \
    ZOHO-REFRESH-TOKEN="ZOHO-REFRESH-TOKEN"
  then
    echo "Deployment succeeded"
    SUCCESS=true
    break
  else
    echo "Deployment failed. Retrying in $RETRY_DELAY seconds..."
    sleep $RETRY_DELAY
  fi
done

if [ "$SUCCESS" = false ]; then
  echo "Deployment failed after $MAX_RETRIES attempts"
  exit 1
fi

echo "Fetching public URL..."
FQDN=$(az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --query ipAddress.fqdn \
  -o tsv)

echo "Deployment successful"
echo "URL: http://$FQDN:$PORT"
