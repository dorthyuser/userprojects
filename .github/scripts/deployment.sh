#!/bin/bash
set -euo pipefail

# =============================
# Azure .NET Function Build + Deploy (Production)
# Function name = Project name (auto-detected)
# =============================

# === Input Arguments ===
GIT_REPO=$1
BRANCH=$2
RESOURCE_GROUP=$3
LOCATION=$4
STORAGE_ACCOUNT=$5
DOTNET_VERSION=$6

echo "-------------------------------------------------------"
echo "GIT_REPO        = $GIT_REPO"
echo "BRANCH          = $BRANCH"
echo "RESOURCE_GROUP  = $RESOURCE_GROUP"
echo "LOCATION        = $LOCATION"
echo "STORAGE_ACCOUNT = $STORAGE_ACCOUNT"
echo "DOTNET_VERSION  = $DOTNET_VERSION"
echo "-------------------------------------------------------"

# === Preflight Checks ===
echo "Checking dependencies..."
for cmd in az git zip dotnet; do
  command -v $cmd >/dev/null || {
    echo "ERROR: $cmd not found"
    exit 1
  }
done

# === Parse .NET version ===
CLEANED="$(echo "$DOTNET_VERSION" | tr -cd '0-9.')"
DOTNET_MAJOR_MINOR="$CLEANED"

if [[ "$DOTNET_MAJOR_MINOR" =~ ^[0-9]+$ ]]; then
  DOTNET_MAJOR_MINOR="$DOTNET_MAJOR_MINOR.0"
fi

# === Step 1: Clone Repo to temp ===
TEMP_DIR="/tmp/azure-temp-$(date +%s)"

echo "Cloning repository..."
rm -rf "$TEMP_DIR"

git clone -b "$BRANCH" "$GIT_REPO" "$TEMP_DIR" --depth=1

cd "$TEMP_DIR"

# === Step 2: Detect correct csproj ===

REPO_NAME=$(basename "$GIT_REPO" .git)

echo "Detecting project file..."

echo "Detecting project file..."

# Find real Azure Function project (exclude build artifacts)
CSPROJ_PATH=$(
find . -type f -name "*.csproj" \
  ! -path "*/obj/*" \
  ! -path "*/bin/*" \
  ! -iname "*workerextensions.csproj" \
  ! -iname "*test*.csproj" \
  ! -iname "*tests*.csproj" \
  | head -n 1
)

if [[ -z "$CSPROJ_PATH" ]]; then
  echo "ERROR: No valid Azure Function project found"
  exit 1
fi

PROJECT_NAME=$(basename "$CSPROJ_PATH" .csproj)
FUNCTION_APP_NAME=$(echo "${PROJECT_NAME}-xyz")

echo "Project detected: $PROJECT_NAME"
echo "Function App Name: $FUNCTION_APP_NAME"
echo "Project path: $CSPROJ_PATH"

BUILD_DIR="/tmp/azure-build-$FUNCTION_APP_NAME"
ZIP_NAME="$FUNCTION_APP_NAME.zip"

echo "Project detected: $PROJECT_NAME"
echo "Function App Name: $FUNCTION_APP_NAME"

# Move repo to final build location
rm -rf "$BUILD_DIR"
mv "$TEMP_DIR" "$BUILD_DIR"

cd "$BUILD_DIR"

# === Extract routes (ROBUST) ===
echo "Extracting routes..."

ROUTE_MAP=$(
awk '
BEGIN {
  fn=""
}

# Capture Function name
/\[Function\(/ {
  match($0, /\[Function\("([^"]+)"\)/, f)
  if (f[1]) {
    fn = f[1]
  }
}

# Capture HttpTrigger (can be on same OR different line)
/HttpTrigger/ {
  match($0, /"([^"]+)"/, m)
  match($0, /Route *= *"([^"]+)"/, r)

  if (fn != "" && m[1] && r[1]) {
    printf "%s,%s,%s\n", fn, m[1], r[1]
  }
}
' $(find . -name "*.cs")
)

if [[ -z "$ROUTE_MAP" ]]; then
  echo "ERROR: No functions found"
  exit 1
fi

echo "$ROUTE_MAP"

# === Step 4: Load Postgres connection ===

LOCAL_SETTINGS_FILE="local.settings.json"

if [[ ! -f "$LOCAL_SETTINGS_FILE" ]]; then
  echo "ERROR: local.settings.json not found"
  exit 1
fi

POSTGRES_CONNECTION=$(
awk -F': *"' '
/PostgresConnectionString/ {
  gsub(/",?$/, "", $2);
  print $2
}
' "$LOCAL_SETTINGS_FILE"
)

# === Step 5: Detect Azure Functions runtime ===

AZURE_FUNCTIONS_VERSION=$(
awk -F'[<>]' '
/AzureFunctionsVersion/ {
  gsub(/v/, "", $3);
  print $3
}
' "$CSPROJ_PATH"
)

echo "Functions runtime: v$AZURE_FUNCTIONS_VERSION"

# === Step 6: Build & Publish ===

echo "Publishing project..."

dotnet restore "$CSPROJ_PATH"

dotnet publish "$CSPROJ_PATH" \
  -c Release \
  -o "$BUILD_DIR/publish"
  
# Verify metadata exists (supports .NET 6/7/8 isolated)

if [[ -f "$BUILD_DIR/publish/functions.metadata" ]]; then
  echo "Detected .NET isolated metadata (functions.metadata)"
elif find "$BUILD_DIR/publish" -name "function.json" | grep -q .; then
  echo "Detected legacy metadata (function.json)"
else
  echo "ERROR: No Azure Functions metadata found"
  exit 1
fi

# === Step 7: Zip ===

cd "$BUILD_DIR/publish"

ls -l "$BUILD_DIR/publish"

zip -r "$BUILD_DIR/$ZIP_NAME" . >/dev/null

cd "$BUILD_DIR"

# === Step 8: Ensure Azure resources ===

echo "Ensuring Azure resources..."

az account show >/dev/null

az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" -o none

# Normalize storage name
STORAGE_ACCOUNT=$(echo "$STORAGE_ACCOUNT" \
  | tr '[:upper:]' '[:lower:]' \
  | tr -cd 'a-z0-9' \
  | cut -c1-23)

# Create storage if missing
STORAGE_EXISTS=$(az storage account show \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --query "name" -o tsv 2>/dev/null || echo "")

if [[ -z "$STORAGE_EXISTS" ]]; then

  echo "Creating storage account..."

  az storage account create \
    --name "$STORAGE_ACCOUNT" \
    --location "$LOCATION" \
    --resource-group "$RESOURCE_GROUP" \
    --sku Standard_LRS -o none
else
  echo "Storage exists."
fi

# Create function app if missing
APP_EXISTS=$(az functionapp show \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "name" -o tsv 2>/dev/null || echo "")

if [[ -z "$APP_EXISTS" ]]; then

  echo "Creating Function App..."

  az functionapp create \
    --name "$FUNCTION_APP_NAME" \
    --storage-account "$STORAGE_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --consumption-plan-location "$LOCATION" \
    --functions-version "$AZURE_FUNCTIONS_VERSION" \
    --runtime dotnet-isolated \
    --runtime-version "$DOTNET_MAJOR_MINOR" \
    --os-type Linux -o none
else
  echo "Function App exists."
fi

# Apply config
az functionapp config set \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --linux-fx-version "DOTNET-ISOLATED|$DOTNET_MAJOR_MINOR" -o none

az functionapp config appsettings set \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
  FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
  WEBSITE_RUN_FROM_PACKAGE=1 \
  PostgresConnectionString="$POSTGRES_CONNECTION" -o none

# === Step 9: Deploy ===

echo "Deploying..."

az functionapp deployment source config-zip \
  --src "$BUILD_DIR/$ZIP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" -o none

# Restart to ensure visibility
az functionapp restart \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP"

sleep 5

# === Step 10: Get URL ===

APP_URL=$(az functionapp show \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query defaultHostName -o tsv)

HOST_KEY=$(az functionapp keys list \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query functionKeys.default -o tsv)

echo "-------------------------------------------------------"
echo "Deployment complete"
echo "-------------------------------------------------------"

while IFS=',' read -r FN METHOD ROUTE; do
  echo "$METHOD https://$APP_URL/api/$ROUTE?code=$HOST_KEY"
done <<< "$ROUTE_MAP"

echo "-------------------------------------------------------"