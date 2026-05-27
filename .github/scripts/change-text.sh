#!/bin/bash
set -e

BRANCH=$1
APP_NAME=$2
ACI_NAME=$3
IMAGE_NAME=$4
IMAGE_TAG=$5
RESOURCE_GROUP=$6

FILE="main.yml"

if [ $# -lt 6 ]; then
  echo "Usage: $0 <branch> <app_name> <aci_name> <image_name> <image_tag> <resource_group>"
  exit 1
fi

echo "Updating YAML..."

# 1. Update branch
yq -i '.on.push.branches[0] = "'"$BRANCH"'"' $FILE

# 2. Update build script app name (travelcard)
yq -i '.jobs.build-deploy.steps[] |=
  (select(.name == "Build and Push Docker Image").run |= sub("travelcard"; "'"$APP_NAME"'"))' $FILE

# 3. Update docker image name
yq -i '.jobs.build-deploy.steps[] |=
  (select(.name == "Build and Push Docker Image").run |= sub("\\$\\{\\{ secrets.DOCKER_USERNAME \\}\\}/travelcard"; "'"$IMAGE_NAME"'"))' $FILE

# 4. Update deploy script values
yq -i '.jobs.build-deploy.steps[] |=
  (select(.name == "Deploy to ACI").run |= sub("travelcard-api"; "'"$ACI_NAME"'"))' $FILE

yq -i '.jobs.build-deploy.steps[] |=
  (select(.name == "Deploy to ACI").run |= sub("\\$\\{\\{ secrets.DOCKER_USERNAME \\}\\}/travelcard"; "'"$IMAGE_NAME"'"))' $FILE

# 5. Update resource group
yq -i '.jobs.build-deploy.steps[] |=
  (select(.name == "Deploy to ACI").run |= sub("dev-luffy"; "'"$RESOURCE_GROUP"'"))' $FILE

echo "YAML updated successfully"