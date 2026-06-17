#!/bin/bash
set -euo pipefail

# --- Input Args ---
APP_NAME=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3
IMAGE_TAG=$4
PYTHON_VERSION=${5:-3.13}

IMAGE_NAME="$DOCKER_USERNAME/$APP_NAME:$IMAGE_TAG"

echo "----------------------------------"
echo "Python FastAPI Docker Build"
echo "App:    $APP_NAME"
echo "Tag:    $IMAGE_TAG"
echo "Python: $PYTHON_VERSION"
echo "----------------------------------"

# --- Validate project ---
if [ ! -f "requirements.txt" ]; then
  echo "No requirements.txt found"
  exit 1
fi

if [ ! -f "main.py" ] && [ ! -f "app/main.py" ]; then
  echo "No main.py or app/main.py found"
  exit 1
fi

# --- Create Dockerfile ---
cat <<EOF > Dockerfile
# -------- Build Stage --------
FROM python:${PYTHON_VERSION}-slim AS build
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*
COPY requirements.txt .
RUN pip install --upgrade pip && \
    pip install --no-cache-dir --prefix=/install -r requirements.txt

# -------- Runtime Stage --------
FROM python:${PYTHON_VERSION}-slim
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    libpq5 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /install /usr/local
COPY . .
EXPOSE 8080
ENV PYTHONUNBUFFERED=1
ENTRYPOINT ["python", "main.py"]
EOF

# --- Build Image ---
echo "Building Docker image..."
docker build -t "$IMAGE_NAME" .

# --- Docker Login ---
echo "Logging into Docker..."
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin

# --- Push Image ---
echo "Pushing image..."
docker push "$IMAGE_NAME"

echo "Done: $IMAGE_NAME"
