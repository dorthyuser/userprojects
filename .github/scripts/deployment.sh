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

# Function App and storage account names are globally unique across Azure.
# Derive them from repo + branch + .NET version: different projects never
# collide, and redeploys of the same branch reuse the same resources.
NAME_HASH=$(printf '%s|%s|%s' "$GIT_REPO" "$BRANCH" "$DOTNET_MAJOR_MINOR" | sha256sum | cut -c1-8)
FUNCTION_APP_NAME="${PROJECT_NAME}-${NAME_HASH}"

# "auto" (or empty) = derive; any other value is used as given
if [[ -z "$STORAGE_ACCOUNT" || "$STORAGE_ACCOUNT" == "auto" || "$STORAGE_ACCOUNT" == "mystorageaccount123" ]]; then
  PROJECT_SLUG=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9' | cut -c1-13)
  STORAGE_ACCOUNT="st${PROJECT_SLUG}${NAME_HASH}"
fi
echo "Storage Account: $STORAGE_ACCOUNT"

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

# The function project's folder: next to its .csproj (added by the migration)
LOCAL_SETTINGS_FILE="$(dirname "$CSPROJ_PATH")/local.settings.json"

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

# === The unit tests of the business rules (added by the migration): a failing test stops the deploy ===
for TEST_PROJECT in tests/*/*.Tests.csproj; do
  [[ -f "$TEST_PROJECT" ]] || continue
  echo "Running the unit tests: $TEST_PROJECT"
  if grep -q '"Microsoft.Testing.Platform"' global.json 2>/dev/null; then
    dotnet test --project "$TEST_PROJECT" -c Release || { echo "::error::The unit tests failed ($TEST_PROJECT) — not deployed"; exit 1; }
  else
    dotnet test "$TEST_PROJECT" -c Release || { echo "::error::The unit tests failed ($TEST_PROJECT) — not deployed"; exit 1; }
  fi
done

# === Step 6: Build & Publish ===

echo "Publishing project..."

dotnet restore "$CSPROJ_PATH"

dotnet publish "$CSPROJ_PATH" \
  -c Release \
  -o "$BUILD_DIR/publish"
  
# Verify metadata exists (.NET isolated worker)

if grep -q '"workerIndexing"[[:space:]]*:[[:space:]]*"true"' "$BUILD_DIR/publish/worker.config.json" 2>/dev/null; then
  echo "Detected worker indexing (worker.config.json): the worker lists its functions (added by the migration)"
elif [[ -f "$BUILD_DIR/publish/functions.metadata" ]]; then
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

# .NET 8 support on Azure Functions ends 2026-11-10 (added by the migration)
if [[ "${DOTNET_MAJOR_MINOR%%.*}" == "8" ]]; then
  echo "::warning::.NET 8 support on Azure Functions ends 2026-11-10. The app keeps running without security updates: migrate the project again with .NET 10 for an app you keep."
fi

# Create function app if missing
APP_EXISTS=$(az functionapp show \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "name" -o tsv 2>/dev/null || echo "")

if [[ -z "$APP_EXISTS" ]]; then

  echo "Creating Function App..."

  # Flex Consumption: the .NET version as this region lists it (added by the migration)
  FLEX_VERSIONS=$(az functionapp list-flexconsumption-runtimes --location "$LOCATION" --runtime dotnet-isolated \
    --query "[].version" -o tsv 2>/dev/null | tr -d '\r' || true)
  FLEX_VERSION=$(grep -E "^${DOTNET_MAJOR_MINOR%%.*}(\.|$)" <<< "$FLEX_VERSIONS" | head -n 1 || true)
  if [[ -z "$FLEX_VERSION" ]]; then
    echo "::error::Flex Consumption in $LOCATION has no dotnet-isolated $DOTNET_MAJOR_MINOR (it lists: $(echo ${FLEX_VERSIONS:-nothing})). Set the AZURE_REGION secret to a region from: az functionapp list-flexconsumption-locations"
    exit 1
  fi
  DOTNET_MAJOR_MINOR="$FLEX_VERSION"

  az functionapp create \
    --name "$FUNCTION_APP_NAME" \
    --storage-account "$STORAGE_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --flexconsumption-location "$LOCATION" \
    --runtime dotnet-isolated \
    --runtime-version "$DOTNET_MAJOR_MINOR" -o none
else
  echo "Function App exists."
  # Created earlier on the Linux Consumption plan: deployed there as before (added by the migration)
  if [[ "$(az functionapp show --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP" --query sku -o tsv 2>/dev/null || true)" == "Dynamic" ]]; then
    echo "::warning::$FUNCTION_APP_NAME runs on the Linux Consumption plan, which retires 2028-09-30. To move it to Flex Consumption, delete it (az functionapp delete --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP) and run this workflow again: it creates the app on Flex Consumption with the same name and a new key. Set again what was set only in Azure: app settings without a repo secret, the managed identity and its Key Vault role."
  fi
fi

az functionapp config appsettings set \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
  PostgresConnectionString="$POSTGRES_CONNECTION" -o none

# === App settings the code reads (added by the migration) ===
# The workflow passes each one as APPSETTING_<name> from the repo secret of the same name
# (the value may be a Key Vault reference); a setting without a secret keeps its value in Azure.
APP_SETTINGS=()
for VAR in $(compgen -v APPSETTING_ || true); do
  if [[ -n "${!VAR}" ]]; then
    APP_SETTINGS+=("${VAR#APPSETTING_}=${!VAR}")
  else
    echo "App setting ${VAR#APPSETTING_}: no repo secret with this name — keeps the value set in Azure"
  fi
done
if (( ${#APP_SETTINGS[@]} )); then
  echo "App settings from repo secrets: $(for S in "${APP_SETTINGS[@]}"; do printf '%s ' "${S%%=*}"; done)"
  az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings "${APP_SETTINGS[@]}" -o none
fi

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

# The route prefix in "$(dirname "$CSPROJ_PATH")/host.json" (the API base path) — "api" when it sets none (added by the migration)
ROUTE_PREFIX=api
if grep -q '"routePrefix"' "$(dirname "$CSPROJ_PATH")/host.json" 2>/dev/null; then
  ROUTE_PREFIX=$(grep -o '"routePrefix"[[:space:]]*:[[:space:]]*"[^"]*"' "$(dirname "$CSPROJ_PATH")/host.json" | sed 's/.*"\([^"]*\)"$/\1/' || true)
fi

while IFS=',' read -r FN METHOD ROUTE; do
  [[ "$ROUTE" == "{*"* || "$FN" == *MethodNotAllowed || "$FN" == ResourceNotFound ]] && continue
  echo "$METHOD https://$APP_URL/${ROUTE_PREFIX:+$ROUTE_PREFIX/}$ROUTE?code=$HOST_KEY"
done <<< "$ROUTE_MAP"

echo "-------------------------------------------------------"