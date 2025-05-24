# Orders Microservice

This repository contains the Orders microservice of the eCommerce solution. It exposes a REST API for creating, updating and querying orders while coordinating with the Users and Products services.

## Architecture

- **OrdersMicroservice.API** – ASP.NET Core Web API hosting the endpoints under `/api/Orders`.
- **BusinessLogicLayer** – Contains DTOs, validation rules and the `OrdersService` which orchestrates persistence and cross‑service calls.
- **DataAccessLayer** – Provides MongoDB repositories for persisting `Order` and `OrderItem` documents.
- **APIGateway** – Ocelot based gateway that forwards requests to the orders, users and products services.
- **RabbitMQ consumers** – Background services that listen for product update and delete events to keep the Redis cache in sync.
- **Kubernetes manifests** – Deployment resources can be found in the `k8s/` folder with subfolders per environment (`dev`, `qa`, `uat`, `staging`, `prod`).

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/) – for building or running directly
- Docker and Docker Compose – for local development
- MongoDB, Redis and RabbitMQ instances (provided via `docker-compose.yml`)

Environment variables such as `MONGODB_HOST`, `UsersMicroserviceName` and `ProductsMicroservicePort` are configured in `docker-compose.yml` and the Kubernetes config maps.

## Running locally

1. Build and start the services with Docker Compose:
   ```bash
   docker-compose up --build
   ```
   The Orders API will be available on `http://localhost:7000` (port `8080` inside the container). When using the API Gateway, access it via `http://localhost:4000/gateway/Orders`.

2. Alternatively, run the API directly using the .NET CLI:
   ```bash
   dotnet run --project OrdersMicroservice.API
   ```

Swagger UI is enabled and served from `/swagger` when running the service.

## Running tests

Execute the unit tests with:
```bash
dotnet test
```

## Key Endpoints

- `GET    /api/Orders` – retrieve all orders
- `GET    /api/Orders/search/orderid/{orderID}`
- `GET    /api/Orders/search/productid/{productID}`
- `GET    /api/Orders/search/orderDate/{orderDate}`
- `GET    /api/Orders/search/userid/{userID}`
- `POST   /api/Orders` – create a new order
- `PUT    /api/Orders/{orderID}` – update an existing order
- `DELETE /api/Orders/{orderID}` – remove an order

## Deployment

The `azure-pipelines.yml` file defines build, test and multi‑environment deployment stages. Kubernetes manifests under `k8s/` are patched with the built image tag and then applied to the target cluster.

---
This README gives a short overview of the microservice, its structure and how to run or test it locally.
