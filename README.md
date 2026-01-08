# High-Load Ticket Sales System (Architecture Lab)

### 🎯 Project Overview
This repository serves as an **Engineering Laboratory** meant to simulate and solve high-load challenges in a real-world scenario: **"Ticket Sales for a Major Concert Event"** (e.g., Tarkan Concert).

The goal was not just to build an API, but to intentionally stress-test it, identify bottlenecks, and evolve the architecture from a **Monolith** to a **Resilient, Scalable Distributed System**.

---

### 🏗️ Architect's Perspective: The Evolution Path

This project follows a strict 4-phase architectural evolution to handle traffic spikes and ensure system reliability.

#### Phase 1: Containerized Infrastructure
* **Goal:** Establish a portable development environment.
* **Implementation:** Built a .NET 8 Web API connected to PostgreSQL, fully containerized with **Docker & Docker Compose**.
* **Key Achievement:** Service-to-service communication established via internal Docker DNS (`db` instead of `localhost`).

#### Phase 2: Performance & Caching Strategy
* **The Bottleneck:** Database read-heavy traffic (Checking "How many tickets left?") caused latency.
* **The Solution:** Implemented the **Cache-Aside Pattern** using **Redis**.
* **Resilience Check:** Tested **Failover** scenarios. If the Redis container stops, the system gracefully degrades and fetches data directly from the DB without crashing.

#### Phase 3: Asynchronous Event-Driven Architecture
* **The Bottleneck:** Synchronous "Buy Ticket" requests (Payment/Invoice processing) locked threads during traffic spikes.
* **The Solution:** Decoupled the write operations using **RabbitMQ** and **MassTransit**.
    * The API accepts the request and returns `202 Accepted`.
    * A background **Worker Service** processes the queue asynchronously (Peak Shaving).
* **Key Achievement:** Even if the Worker Service is down, orders accumulate in the queue and are processed once the service recovers (Zero Data Loss).

#### Phase 4: Load Testing & Horizontal Scaling
* **The Challenge:** Finding the breaking point.
* **Testing:** Used **k6** to simulate 100+ Virtual Users (VUs) per second.
* **The Fix:** Introduced **Nginx** as a Load Balancer and scaled the API to **3 Replicas**.

---

### 🛠️ Tech Stack & Tools

| Component | Technology | Role |
| :--- | :--- | :--- |
| **Backend** | **.NET 8 Web API** | Core Application Logic |
| **Worker** | **.NET Console App** | Background Queue Consumer |
| **Database** | **PostgreSQL** | Relational Data Storage |
| **Cache** | **Redis** | High-performance Caching |
| **Message Broker** | **RabbitMQ** | Asynchronous Messaging |
| **Orchestration** | **Docker Compose** | Infrastructure Management |
| **Load Balancer** | **Nginx** | Traffic Distribution |
| **Testing** | **k6** | Load & Stress Testing |

---

### 🚀 How to Run the Lab
1.  **Start Infrastructure:**
    ```bash
    docker-compose up -d
    ```
    *This spins up API (x3 replicas), Worker, Postgres, Redis, RabbitMQ, and Nginx.*

2.  **Run Load Tests:**
    ```bash
    k6 run load-tests/ticket-sales-spike.js
    ```

3.  **Observe Resilience:**
    * Stop the RabbitMQ consumer: `docker stop ticket-worker`
    * Send requests.
    * Restart consumer: `docker start ticket-worker`
    * Watch the queue drain and database update.

---
> *This project demonstrates the transition from synchronous blocking calls to a reactive, scalable architecture.*
