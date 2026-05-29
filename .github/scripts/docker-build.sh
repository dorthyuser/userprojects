#!/bin/bash
set -euo pipefail

# --- Input Args ---
APP_NAME=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3
IMAGE_TAG=$4
JDK_VERSION=$5

IMAGE_NAME="$DOCKER_USERNAME/$APP_NAME:$IMAGE_TAG"

echo "----------------------------------"
echo "🚀 Java Docker Build Started"
echo "App: $APP_NAME"
echo "Tag: $IMAGE_TAG"
echo "JDK: $JDK_VERSION"
echo "----------------------------------"

# --- Validate project ---
if [ ! -f "build.gradle" ] && [ ! -f "build.gradle.kts" ]; then
  echo " No Gradle project found"
  exit 1
fi

# --- Create Dockerfile (FULLY containerized build) ---
cat <<EOF > Dockerfile
# -------- Build Stage --------
FROM gradle:8.10.2-jdk${JDK_VERSION} AS build
WORKDIR /app
COPY . .

# Build inside container
RUN gradle clean bootJar -x test --no-daemon

# -------- Runtime Stage --------
FROM eclipse-temurin:${JDK_VERSION}-jre
WORKDIR /app

# Copy built jar
COPY --from=build /app/build/libs/*.jar app.jar

EXPOSE 8080
ENTRYPOINT ["java","-jar","app.jar"]
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