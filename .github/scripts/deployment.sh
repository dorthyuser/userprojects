#!/bin/bash

set -e

# === Input Arguments ===
FUNCTION_NAME=$1
GIT_REPO=$2
BRANCH=$3
PYTHON_VERSION=$4    # e.g. python3.12, python3.13, python3.14
REGION=$5
ROLE_ARN=$6
SECRET_NAME=$7

echo "$(date) - Build started"

echo "FUNCTION_NAME  = $FUNCTION_NAME"
echo "GIT_REPO       = $GIT_REPO"
echo "BRANCH         = $BRANCH"
echo "PYTHON_VERSION = $PYTHON_VERSION"
echo "REGION         = $REGION"

# === Config ===
CLONE_DIR="/tmp/lambda-deploy-$FUNCTION_NAME"

# Normalise: "python3.13" → "3.13", "3.13" → "3.13", "313" → "3.13"
PYTHON_VERSION_CLEAN=$(echo "$PYTHON_VERSION" | grep -oP '\d+\.\d+' | head -1)
if [[ -z "$PYTHON_VERSION_CLEAN" ]]; then
  RAW=$(echo "$PYTHON_VERSION" | grep -oP '\d+' | head -1)
  PYTHON_VERSION_CLEAN="${RAW:0:1}.${RAW:1}"
fi

# Runtime matrix
case "$PYTHON_VERSION_CLEAN" in
  3.12) LAMBDA_RUNTIME="python3.12"; DOCKER_IMAGE="python:3.12-slim" ;;
  3.13) LAMBDA_RUNTIME="python3.13"; DOCKER_IMAGE="python:3.13-slim" ;;
  3.14) LAMBDA_RUNTIME="python3.14"; DOCKER_IMAGE="python:3.14-rc-slim" ;;
  *)
    echo "Unsupported Python version: $PYTHON_VERSION_CLEAN"
    exit 1
    ;;
esac

echo "Lambda runtime : $LAMBDA_RUNTIME"
echo "Docker image   : $DOCKER_IMAGE"

# === Cleanup & Clone ===
rm -rf "$CLONE_DIR"
git clone -b "$BRANCH" "$GIT_REPO" "$CLONE_DIR" --depth=1
cd "$CLONE_DIR"

cleanup() {
  echo "Cleaning temporary files..."
  rm -rf "$CLONE_DIR"
}
trap cleanup EXIT

# === Detect dependency file ===
DEP_FILE=""
DEP_INSTALL_CMD=""

if [[ -f "requirements.txt" ]]; then
  DEP_FILE="requirements.txt"
  DEP_INSTALL_CMD="pip install --no-warn-script-location -r requirements.txt -t python/"
elif [[ -f "pyproject.toml" ]]; then
  DEP_FILE="pyproject.toml"
  DEP_INSTALL_CMD="pip install --no-warn-script-location . -t python/"
elif [[ -f "setup.py" ]]; then
  DEP_FILE="setup.py"
  DEP_INSTALL_CMD="pip install --no-warn-script-location . -t python/"
else
  echo "No dependency file found — no pip install step"
fi

[[ -n "$DEP_FILE" ]] && echo "Dependency file: $DEP_FILE"

# === Detect Lambda handler ===
# Priority:
#   1. lambda_config.json  { "handler": "module.function" }
#   2. Scan .py for def lambda_handler
#   3. Scan .py for def handler
#   4. Default: lambda_function.lambda_handler
HANDLER=""

CONFIG_FILE=$(find . -maxdepth 2 -name "lambda_config.json" | head -1)
if [[ -f "$CONFIG_FILE" ]]; then
  HANDLER=$(python3 -c "import json; d=json.load(open('$CONFIG_FILE')); print(d.get('handler',''))" 2>/dev/null || true)
  [[ -n "$HANDLER" ]] && echo "Handler from lambda_config.json: $HANDLER"
fi

if [[ -z "$HANDLER" ]]; then
  HANDLER_FILE=$(grep -rl "def lambda_handler" . --include="*.py" 2>/dev/null | head -1)
  if [[ -n "$HANDLER_FILE" ]]; then
    MODULE=$(basename "$HANDLER_FILE" .py)
    HANDLER="${MODULE}.lambda_handler"
    echo "Handler detected (lambda_handler scan): $HANDLER"
  fi
fi

if [[ -z "$HANDLER" ]]; then
  HANDLER_FILE=$(grep -rl "^def handler" . --include="*.py" 2>/dev/null | head -1)
  if [[ -n "$HANDLER_FILE" ]]; then
    MODULE=$(basename "$HANDLER_FILE" .py)
    HANDLER="${MODULE}.handler"
    echo "Handler detected (handler scan): $HANDLER"
  fi
fi

if [[ -z "$HANDLER" ]]; then
  HANDLER_FILE=$(grep -rl "lambda_handler\s*=" . --include="*.py" 2>/dev/null | head -1)
  if [[ -n "$HANDLER_FILE" ]]; then
    MODULE=$(basename "$HANDLER_FILE" .py)
    HANDLER="${MODULE}.lambda_handler"
    echo "Handler detected (Mangum assignment scan): $HANDLER"
  fi
fi

if [[ -z "$HANDLER" ]]; then
  HANDLER="lambda_function.lambda_handler"
  echo "Handler not detected — using default: $HANDLER"
fi

echo "Final handler: $HANDLER"

# === Build ZIP using Docker ===
# Docker isolates the pip install to the correct Python version
# and produces a clean linux-compatible package set

echo "$(date) - Building deployment package..."

BUILD_DIR=$(mktemp -d /tmp/lambda-docker-build-XXXXXX)

# Copy source and dependency files into build context
cp -r . "$BUILD_DIR/src"

# Create cache-seed with only the dependency file for Docker layer caching
mkdir -p "$BUILD_DIR/cache-seed"
[[ -f "requirements.txt" ]] && cp requirements.txt "$BUILD_DIR/cache-seed/"
[[ -f "pyproject.toml" ]]   && cp pyproject.toml   "$BUILD_DIR/cache-seed/"
[[ -f "setup.py" ]]         && cp setup.py         "$BUILD_DIR/cache-seed/"

cat > "$BUILD_DIR/Dockerfile" << DOCKERFILE_EOF
FROM ${DOCKER_IMAGE}

RUN apt-get update && apt-get install -y --no-install-recommends zip \
    && rm -rf /var/lib/apt/lists/*

ENV HOME=/tmp
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /build

# LAYER 1: dependency install (cached when dep file unchanged)
COPY cache-seed/ ./
RUN pip install --quiet --no-warn-script-location --upgrade pip

DOCKERFILE_EOF

if [[ -n "$DEP_INSTALL_CMD" ]]; then
  cat >> "$BUILD_DIR/Dockerfile" << DOCKERFILE_EOF
RUN mkdir -p python && ${DEP_INSTALL_CMD}

DOCKERFILE_EOF
fi

cat >> "$BUILD_DIR/Dockerfile" << DOCKERFILE_EOF
# LAYER 2: full source
COPY src/ ./src/

# LAYER 3: assemble ZIP
RUN mkdir -p /build/artifact && \
    cp -r src/. /build/artifact/ && \
    find /build/artifact -type d -name "__pycache__" -exec rm -rf {} + 2>/dev/null || true && \
    find /build/artifact -name "*.pyc" -delete 2>/dev/null || true

WORKDIR /build/artifact
RUN if [ -d /build/python ]; then cp -r /build/python/. ./; fi && \
    zip -r /build/function.zip . \
      -x "*.git*" -x "*__pycache__*" -x "*.pyc" \
      -x "requirements.txt" -x "pyproject.toml" -x "setup.py" > /dev/null && \
    echo "ZIP size: \$(du -sh /build/function.zip | cut -f1)"
DOCKERFILE_EOF

IMAGE_TAG="lambda-builder-${FUNCTION_NAME}"
docker build --pull -t "$IMAGE_TAG" "$BUILD_DIR"

# Extract ZIP from container
CONTAINER_ID=$(docker create "$IMAGE_TAG")
docker cp "$CONTAINER_ID":/build/function.zip "$BUILD_DIR/function.zip"
docker rm -f "$CONTAINER_ID" > /dev/null
docker rmi "$IMAGE_TAG" --force 2>/dev/null || true

ZIP_PATH="$BUILD_DIR/function.zip"

if [[ ! -f "$ZIP_PATH" ]]; then
  echo "function.zip not found after Docker build"
  exit 1
fi

ZIP_SIZE=$(du -sh "$ZIP_PATH" | cut -f1)
echo "ZIP created: $ZIP_PATH ($ZIP_SIZE)"

# === Deploy to AWS ===
echo "$(date) - Deploying to AWS Lambda..."

if aws lambda get-function \
  --function-name "$FUNCTION_NAME" \
  --region "$REGION" >/dev/null 2>&1; then

  echo "Updating existing Lambda function..."

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
    --handler "$HANDLER" \
    --runtime "$LAMBDA_RUNTIME" \
    --environment "Variables={POSTGRESQL_SECRET=$SECRET_NAME}"

else

  echo "Creating new Lambda function..."

  timeout 120 aws lambda create-function \
    --function-name "$FUNCTION_NAME" \
    --runtime "$LAMBDA_RUNTIME" \
    --role "$ROLE_ARN" \
    --handler "$HANDLER" \
    --zip-file fileb://"$ZIP_PATH" \
    --timeout 120 \
    --memory-size 512 \
    --region "$REGION" \
    --environment "Variables={POSTGRESQL_SECRET=$SECRET_NAME}"

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

# Cleanup build dir
rm -rf "$BUILD_DIR"

echo "Function URL: $URL"
echo "$(date) - END"
echo "Lambda deployment completed successfully!"
