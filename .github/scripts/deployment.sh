name: Java Spring Boot Docker + Azure Container Instance

on:
  push:
    branches:
      - zoho-project-2

jobs:
  build-deploy:
    runs-on: ubuntu-latest

    steps:
      # 1. Checkout (only needed if scripts are in repo)
      - name: Checkout repository
        uses: actions/checkout@v4

      # 2. Azure Login
      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      # 3. Set permissions
      - name: Set execute permissions
        run: |
          chmod +x docker-build.sh
          chmod +x deployment.sh

      # 4. Build & Push Docker Image (Java)
      - name: Build and Push Docker Image
        run: |
          ./docker-build.sh \
            https://github.com/<your-org>/<your-repo>.git \
            zoho-project-2 \
            travelcard \
            ${{ secrets.DOCKER_USERNAME }} \
            ${{ secrets.DOCKER_PASSWORD }} \
            ${{ github.sha }} \
            17

      # Optional verification (recommended)
      - name: Verify image availability
        run: |
          docker pull ${{ secrets.DOCKER_USERNAME }}/travelcard:${{ github.sha }}

      - name: Wait for image availability
        run: sleep 15

      # 5. Deploy to Azure Container Instance
      - name: Deploy to ACI
        env:
          DOCKER_USERNAME: ${{ secrets.DOCKER_USERNAME }}
          DOCKER_PASSWORD: ${{ secrets.DOCKER_PASSWORD }}
          AZURE_KEY_VAULT: ${{ secrets.AZURE_KEY_VAULT }}
        run: |
          ./deployment.sh \
            travelcard-api \
            ${{ secrets.DOCKER_USERNAME }}/travelcard \
            ${{ github.sha }} \
            dev-luffy \
            8080