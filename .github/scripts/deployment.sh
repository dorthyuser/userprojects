#!/bin/bash
# Builds the function's .zip and creates or updates the AWS Lambda function (a Spring Boot project
# migrated from MuleSoft). API Gateway (REST API, Lambda proxy integration) invokes the function:
# no Function URL is created (it would send a different event format).
#   deployment.sh <function-name> <runtime e.g. java17> <region> <role-arn> [secret-name]
set -euo pipefail

FUNCTION_NAME=$1
RUNTIME=$2
REGION=$3
ROLE_ARN=$4
SECRET_NAME=${5:-}

echo "$(date) - Build started: $FUNCTION_NAME ($RUNTIME, $REGION)"

# === Build: the project's own Gradle wrapper, the .zip AWS Lambda runs (lib/ holds every jar) ===
./gradlew buildZip -x test --no-daemon
ZIP=$(find build/distributions -name '*.zip' | head -n 1)
if [[ -z "$ZIP" ]]; then
  echo "No .zip in build/distributions"
  exit 1
fi
echo "Package: $ZIP ($(du -h "$ZIP" | cut -f1))"

# === Handler: the project's StreamLambdaHandler ===
HANDLER_FILE=$(find src/main/java -name StreamLambdaHandler.java | head -n 1)
if [[ -z "$HANDLER_FILE" ]]; then
  echo "StreamLambdaHandler.java not found under src/main/java"
  exit 1
fi
HANDLER="$(echo "${HANDLER_FILE#src/main/java/}" | sed 's|/|.|g; s|\.java$||')::handleRequest"
echo "Handler: $HANDLER"

# === Deploy ===
if aws lambda get-function --function-name "$FUNCTION_NAME" --region "$REGION" >/dev/null 2>&1; then
  echo "Updating the function code..."
  aws lambda update-function-code --function-name "$FUNCTION_NAME" --zip-file "fileb://$ZIP" --region "$REGION" >/dev/null
  aws lambda wait function-updated-v2 --function-name "$FUNCTION_NAME" --region "$REGION"

  # The function's own environment variables stay; only AWS_SECRET_NAME is set (when given)
  ENVIRONMENT=$(aws lambda get-function-configuration --function-name "$FUNCTION_NAME" --region "$REGION" \
    --query 'Environment.Variables' --output json)
  if [[ "$ENVIRONMENT" == "null" ]]; then ENVIRONMENT='{}'; fi
  if [[ -n "$SECRET_NAME" ]]; then
    ENVIRONMENT=$(echo "$ENVIRONMENT" | jq --arg name "$SECRET_NAME" '. + {AWS_SECRET_NAME: $name}')
  fi
  # Memory and timeout stay as they are on the function (set at creation, tuned there afterwards)
  echo "Updating the function configuration..."
  aws lambda update-function-configuration --function-name "$FUNCTION_NAME" --region "$REGION" \
    --handler "$HANDLER" --runtime "$RUNTIME" --snap-start ApplyOn=PublishedVersions \
    --environment "$(jq -n --argjson vars "$ENVIRONMENT" '{Variables: $vars}')" >/dev/null
  aws lambda wait function-updated-v2 --function-name "$FUNCTION_NAME" --region "$REGION"
else
  echo "Creating the function..."
  if [[ -n "$SECRET_NAME" ]]; then
    ENVIRONMENT=$(jq -n --arg name "$SECRET_NAME" '{Variables: {AWS_SECRET_NAME: $name}}')
  else
    ENVIRONMENT='{"Variables":{}}'
  fi
  aws lambda create-function --function-name "$FUNCTION_NAME" --region "$REGION" \
    --runtime "$RUNTIME" --role "$ROLE_ARN" --handler "$HANDLER" --zip-file "fileb://$ZIP" \
    --memory-size 1024 --timeout 30 --snap-start ApplyOn=PublishedVersions --environment "$ENVIRONMENT" >/dev/null
  aws lambda wait function-active-v2 --function-name "$FUNCTION_NAME" --region "$REGION"
fi

# === SnapStart: a published version (Lambda starts it once and snapshots it), the alias "live" points at it ===
echo "Publishing a version (SnapStart takes its snapshot now)..."
VERSION=$(aws lambda publish-version --function-name "$FUNCTION_NAME" --region "$REGION" --query Version --output text)
aws lambda wait published-version-active --function-name "$FUNCTION_NAME" --qualifier "$VERSION" --region "$REGION"
if aws lambda get-alias --function-name "$FUNCTION_NAME" --name live --region "$REGION" >/dev/null 2>&1; then
  aws lambda update-alias --function-name "$FUNCTION_NAME" --name live --function-version "$VERSION" --region "$REGION" >/dev/null
else
  aws lambda create-alias --function-name "$FUNCTION_NAME" --name live --function-version "$VERSION" --region "$REGION" >/dev/null
fi
echo "Version $VERSION is live (alias live)"

# Older versions go (each keeps a copy of the code): the newest 3 stay, and never a version an alias points to
ALIASED=$(aws lambda list-aliases --function-name "$FUNCTION_NAME" --region "$REGION" --query 'Aliases[].FunctionVersion' --output text)
for OLD in $(aws lambda list-versions-by-function --function-name "$FUNCTION_NAME" --region "$REGION" \
    --query 'Versions[?Version!=`$LATEST`].Version' --output text | tr '\t' '\n' | sort -n | head -n -3); do
  if ! echo " $ALIASED " | tr '\t' ' ' | grep -q " $OLD "; then
    aws lambda delete-function --function-name "$FUNCTION_NAME" --qualifier "$OLD" --region "$REGION" >/dev/null || echo "Version $OLD kept (could not be deleted)"
  fi
done

ALIAS_ARN=$(aws lambda get-alias --function-name "$FUNCTION_NAME" --name live --region "$REGION" --query AliasArn --output text)
echo "$(date) - Deployed: $FUNCTION_NAME version $VERSION. API Gateway (REST API, Lambda proxy integration) calls the alias:"
echo "  $ALIAS_ARN"
echo "(the integration's function is \"$FUNCTION_NAME:live\"; API Gateway needs permission to invoke that alias)."
echo "Set the function's other environment variables (see the README) on the function, then deploy again:"
echo "the alias runs a published version, which gets the function's settings when it is published."
