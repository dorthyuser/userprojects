# Odata4

Java 17 Spring Boot AWS Lambda migration of the MuleSoft OData v4 API.

## Environment variables

The application reads these environment variables in `config/Settings.java`:

- `DS_DB_HOST`
- `DS_DB_PORT`
- `DS_DB_USER`
- `DS_DB_PASSWORD`
- `DS_DB_DATABASE`
- `SERVICE_ORDERS_PAGE_SIZE`

## OData service

Base path: `/api`

Entity sets:

- `Customers`
- `Orders`

Endpoints served by the OData runtime:

- `GET /api/`
- `GET /api/$metadata`
- `GET /api/Customers`
- `GET /api/Customers({CustomerID})`
- `POST /api/Customers`
- `PATCH /api/Customers({CustomerID})`
- `PUT /api/Customers({CustomerID})`
- `DELETE /api/Customers({CustomerID})`
- `GET /api/Customers({CustomerID})/Orders`
- `GET /api/Orders`
- `GET /api/Orders(OrderID={OrderID},ShipName={ShipName})`
- `POST /api/Orders`
- `PATCH /api/Orders(OrderID={OrderID},ShipName={ShipName})`
- `PUT /api/Orders(OrderID={OrderID},ShipName={ShipName})`
- `DELETE /api/Orders(OrderID={OrderID},ShipName={ShipName})`

## Build


./gradlew buildZip


The distributable is created at:

- `build/distributions/Odata4-1.0.0.zip`

## Local run

Set the environment variables above, then run:


./gradlew bootRun


The application listens on `http://localhost:8080/api`.

## Deploy

Deploy the ZIP to AWS Lambda with:

- handler: `com.ai2dev.odata4.StreamLambdaHandler::handleRequest`
- runtime: `java17.al2023`
- memory: at least `1024 MB`
- API Gateway in front of the Lambda
- SnapStart enabled on a published version with alias `live`
