#!/bin/bash

set -e

# === Input Arguments ===
FUNCTION_NAME=$1
GIT_REPO=$2
BRANCH=$3
DOTNET_FRAMEWORK=$4
REGION=$5
ROLE_ARN=$6
SECRET_NAME=$7

echo "$(date) - Build started"

echo "FUNCTION_NAME = $FUNCTION_NAME"
echo "GIT_REPO = $GIT_REPO"
echo "BRANCH = $BRANCH"
echo "DOTNET_FRAMEWORK = $DOTNET_FRAMEWORK"
echo "REGION = $REGION"

# === Config ===
CLONE_DIR="/tmp/lambda-deploy-$FUNCTION_NAME"
ZIP_NAME="$FUNCTION_NAME.zip"
RUNTIME="$DOTNET_FRAMEWORK"
TARGET_FRAMEWORK="${DOTNET_FRAMEWORK/dotnet/net}.0"
PROJECT_FILE="./${FUNCTION_NAME}Lambda.csproj"

echo "Building Lambda: $FUNCTION_NAME"
echo "Runtime: $RUNTIME"
echo "TargetFramework: $TARGET_FRAMEWORK"

# === Cleanup & Clone ===
rm -rf "$CLONE_DIR"

git clone -b "$BRANCH" "$GIT_REPO" "$CLONE_DIR" --depth=1

cd "$CLONE_DIR"

cleanup() {
  echo "Cleaning temporary files..."
  rm -rf "$CLONE_DIR"
  rm -f "$ZIP_PATH" 2>/dev/null || true
}

trap cleanup EXIT

# === Detect .csproj ===
ORIGINAL_CSPROJ=$(find . -name "*.csproj" | head -n 1)

PACKAGE_REFERENCES=""

if [[ -f "$ORIGINAL_CSPROJ" ]]; then
  echo "Using existing csproj: $ORIGINAL_CSPROJ"
else
  echo "No .csproj found. Creating new project..."

cat > "$PROJECT_FILE" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>$TARGET_FRAMEWORK</TargetFramework>
    <RootNamespace>$FUNCTION_NAME</RootNamespace>
    <AssemblyName>$FUNCTION_NAME</AssemblyName>
    <OutputType>Exe</OutputType>
    <AWSProjectType>Lambda</AWSProjectType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="2.4.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.4.0" />
  </ItemGroup>

</Project>
EOF

  ORIGINAL_CSPROJ=$PROJECT_FILE
fi

# === Restore & Build ===

dotnet nuget locals all --clear

dotnet clean "$ORIGINAL_CSPROJ"

dotnet restore "$ORIGINAL_CSPROJ"

dotnet build "$ORIGINAL_CSPROJ" -c Release

# === Publish ===

PUBLISH_DIR="./bin/Release/$TARGET_FRAMEWORK/linux-x64/publish"

echo "Publishing to $PUBLISH_DIR"

dotnet publish "$ORIGINAL_CSPROJ" \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o "$PUBLISH_DIR"

# === Verify runtimeconfig.json ===

RUNTIME_CONFIG_FILE=$(find "$PUBLISH_DIR" -name "*.runtimeconfig.json" | head -n 1)

if [[ -z "$RUNTIME_CONFIG_FILE" ]]; then
  echo "runtimeconfig.json missing"
  exit 1
fi

echo "Found runtimeconfig.json"

# === Create ZIP ===

cd "$PUBLISH_DIR"

zip -r "../$ZIP_NAME" . > /dev/null

cd -

ZIP_PATH="$PUBLISH_DIR/../$ZIP_NAME"

# === Extract Handler ===

DEFAULTS_FILE=$(find . -name "aws-lambda-tools-defaults.json" | head -n1)

if [[ -f "$DEFAULTS_FILE" ]]; then

  HANDLER=$(jq -r '.["function-handler"]' "$DEFAULTS_FILE")

  if [[ -z "$HANDLER" || "$HANDLER" == "null" ]]; then
    HANDLER=$(jq -r '.Information.FunctionHandler' "$DEFAULTS_FILE")
  fi

  echo "Handler: $HANDLER"

else
  echo "aws-lambda-tools-defaults.json not found"
  exit 1
fi

# === Deploy ===

echo "Deploying Lambda..."

if aws lambda get-function \
  --function-name "$FUNCTION_NAME" \
  --region "$REGION" >/dev/null 2>&1; then

  echo "Updating existing Lambda..."

  timeout 120 aws lambda update-function-code \
    --function-name "$FUNCTION_NAME" \
    --zip-file fileb://"$ZIP_PATH" \
    --region "$REGION"

  echo "Waiting for code update to complete..."

  aws lambda wait function-updated-v2 \
    --function-name "$FUNCTION_NAME" \
    --region "$REGION"

  echo "Updating Lambda configuration..."

  aws lambda update-function-configuration \
    --function-name "$FUNCTION_NAME" \
    --region "$REGION" \
    --environment "Variables={AWS_SECRET_NAME=$SECRET_NAME}"

else

  echo "Creating new Lambda..."

  timeout 120 aws lambda create-function \
    --function-name "$FUNCTION_NAME" \
    --runtime "$RUNTIME" \
    --role "$ROLE_ARN" \
    --handler "$HANDLER" \
    --zip-file fileb://"$ZIP_PATH" \
    --timeout 120 \
    --region "$REGION" \
    --environment "Variables={AWS_SECRET_NAME=$SECRET_NAME}"

fi

echo "Waiting for Lambda to become active..."

aws lambda wait function-active-v2 \
  --function-name "$FUNCTION_NAME" \
  --region "$REGION"

sleep 15

# === Function URL ===

EXISTING_URL=$(aws lambda list-function-url-configs \
  --function-name "$FUNCTION_NAME" \
  --region "$REGION" \
  --query 'FunctionUrlConfigs[0].FunctionUrl' \
  --output text 2>/dev/null)

echo "Existing URL: $EXISTING_URL"

if [ -z "$EXISTING_URL" ] || [ "$EXISTING_URL" == "None" ] || [ "$EXISTING_URL" == "null" ]; then

  echo "Creating Function URL..."

  URL=$(aws lambda create-function-url-config \
      --function-name "$FUNCTION_NAME" \
      --auth-type NONE \
      --region "$REGION" \
      --query 'FunctionUrl' \
      --output text)

else

  echo "Function URL already exists"

  aws lambda update-function-url-config \
      --function-name "$FUNCTION_NAME" \
      --auth-type NONE \
      --region "$REGION" >/dev/null

  URL=$EXISTING_URL
fi

echo "Function URL: $URL"

echo "$(date) - END"

echo "Lambda deployment completed successfully!"