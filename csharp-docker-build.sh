#!/bin/bash
set -e

APP_NAME=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3
IMAGE_TAG=$4
DOTNET_VERSION=$5

IMAGE_NAME="$DOCKER_USERNAME/$APP_NAME:$IMAGE_TAG"

echo "Building .NET app..."

# Restore & publish
dotnet restore
dotnet publish -c Release -o publish

# Find DLL
DLL_NAME=$(find publish -name "*.dll" | head -n 1 | xargs basename)
echo "DLL: $DLL_NAME"

# Create Dockerfile
cat > Dockerfile <<EOF
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET_VERSION
WORKDIR /app
COPY ./publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "$DLL_NAME"]
EOF

# Build image
echo "Building Docker image..."
docker build -t "$IMAGE_NAME" .

# Login & push
echo "Logging into Docker..."
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin

echo "Pushing image..."
docker push "$IMAGE_NAME"

echo "Done: $IMAGE_NAME"
