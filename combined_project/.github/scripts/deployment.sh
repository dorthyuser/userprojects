#!/bin/bash

set -euo pipefail

FUNCTION_NAME=${1:-combined_project}
GIT_REPO=${2:-}
BRANCH=${3:-main}
DOTNET_FRAMEWORK=${4:-dotnet8}
REGION=${5:-us-east-1}
ROLE_ARN=${6:-}
SECRET_NAME=${7:-}

if [[ -z "$GIT_REPO" || -z "$ROLE_ARN" || -z "$SECRET_NAME" ]]; then
  echo "Missing required arguments"
  exit 1
fi

CLONE_DIR="/tmp/lambda-deploy-$FUNCTION_NAME"
ZIP_NAME="$FUNCTION_NAME.zip"
TARGET_FRAMEWORK="net8.0"
PROJECT_FILE="./combined_project.csproj"

rm -rf "$CLONE_DIR"
git clone -b "$BRANCH" "$GIT_REPO" "$CLONE_DIR" --depth=1
cd "$CLONE_DIR"

cleanup() {
  rm -rf "$CLONE_DIR"
}
trap cleanup EXIT

if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "Project file not found: $PROJECT_FILE"
  exit 1
fi

dotnet nuget locals all --clear || true
dotnet restore "$PROJECT_FILE"
dotnet build "$PROJECT_FILE" -c Release

PUBLISH_DIR="./bin/Release/$TARGET_FRAMEWORK/linux-x64/publish"
dotnet publish "$PROJECT_FILE" -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"

RUNTIME_CONFIG_FILE=$(find "$PUBLISH_DIR" -name "*.runtimeconfig.json" | head -n 1)
if [[ -z "$RUNTIME_CONFIG_FILE" ]]; then
  echo "runtimeconfig.json missing"
  exit 1
fi

cd "$PUBLISH_DIR"
zip -r "../$ZIP_NAME" . > /dev/null
cd - >/dev/null

ZIP_PATH="$PUBLISH_DIR/../$ZIP_NAME"

if aws lambda get-function --function-name "$FUNCTION_NAME" --region "$REGION" >/dev/null 2>&1; then
  aws lambda update-function-code --function-name "$FUNCTION_NAME" --zip-file fileb://"$ZIP_PATH" --region "$REGION"
  aws lambda wait function-updated-v2 --function-name "$FUNCTION_NAME" --region "$REGION"
  aws lambda update-function-configuration --function-name "$FUNCTION_NAME" --region "$REGION" --environment "Variables={AWS_SECRET_NAME=$SECRET_NAME}"
else
  aws lambda create-function \
    --function-name "$FUNCTION_NAME" \
    --runtime "$DOTNET_FRAMEWORK" \
    --role "$ROLE_ARN" \
    --handler "combined_project::combined_project.Function::HandleAsync" \
    --zip-file fileb://"$ZIP_PATH" \
    --timeout 120 \
    --memory-size 512 \
    --region "$REGION" \
    --environment "Variables={AWS_SECRET_NAME=$SECRET_NAME}"
fi

aws lambda wait function-active-v2 --function-name "$FUNCTION_NAME" --region "$REGION"

EXISTING_URL=$(aws lambda list-function-url-configs --function-name "$FUNCTION_NAME" --region "$REGION" --query 'FunctionUrlConfigs[0].FunctionUrl' --output text 2>/dev/null || true)
if [[ -z "$EXISTING_URL" || "$EXISTING_URL" == "None" || "$EXISTING_URL" == "null" ]]; then
  URL=$(aws lambda create-function-url-config --function-name "$FUNCTION_NAME" --auth-type NONE --region "$REGION" --query 'FunctionUrl' --output text)
else
  aws lambda update-function-url-config --function-name "$FUNCTION_NAME" --auth-type NONE --region "$REGION" >/dev/null
  URL=$EXISTING_URL
fi

echo "Function URL: $URL"
