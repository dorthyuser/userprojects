#!/bin/bash
set -e

APP_NAME=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3
IMAGE_TAG=$4
DOTNET_VERSION=$5

IMAGE_NAME="$DOCKER_USERNAME/$APP_NAME:$IMAGE_TAG"

echo "Building .NET app..."

dotnet restore
dotnet publish -c Release -o publish

DLL_NAME=$(ls publish/*.runtimeconfig.json | head -n 1 | xargs -n1 basename | sed 's/.runtimeconfig.json/.dll/')

if [ ! -f "publish/$DLL_NAME" ]; then
  echo "ERROR: Main DLL not found!"
  ls publish
  exit 1
fi

cat > Dockerfile <<EOF
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET_VERSION
WORKDIR /app
COPY ./publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "$DLL_NAME"]
EOF

echo "Building Docker image..."
docker build -t "$IMAGE_NAME" .

echo "Logging into Docker..."
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin

echo "Pushing image..."
docker push "$IMAGE_NAME"
echo "Done: $IMAGE_NAME"
