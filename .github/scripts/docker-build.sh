#!/bin/bash
set -e
APP_NAME=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3
IMAGE_TAG=$4
PYTHON_VERSION=${5:-3.13}

IMAGE_NAME="$DOCKER_USERNAME/$APP_NAME:$IMAGE_TAG"

if [ ! -f "requirements.txt" ]; then
  echo "ERROR: requirements.txt not found!"
  exit 1
fi

echo "Building Python $PYTHON_VERSION app: $APP_NAME"

cat > Dockerfile <<DOCKEREOF
FROM python:${PYTHON_VERSION}-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY . .
ENV PORT=8080
EXPOSE 8080
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8080"]
DOCKEREOF

echo "Building Docker image..."
docker build -t "$IMAGE_NAME" .

echo "Logging into Docker..."
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin

echo "Pushing image..."
docker push "$IMAGE_NAME"

echo "Done: $IMAGE_NAME"