# Enterprise Dynamic Workflow Engine

An enterprise-grade, standalone, microservice-ready **Dynamic Workflow Management System** built with **.NET 10 (ASP.NET Core Web API)** and **React + Material UI (MUI)**. It operates independently from any specific business application, receiving workflow execution parameters via REST API and driving approvals through a visual workflow and form builder.

---

## 🏗️ Architectural Overview

This system follows **Clean Architecture** and **SOLID principles**, divided into distinct layers:

```
├── WorkflowEngine.Domain         # Core Entities, Enums, and Audit Base Classes
├── WorkflowEngine.Application    # Use Cases, DTOs, Service Interfaces, and Workflow Rules Engine
├── WorkflowEngine.Infrastructure # EF Core Persistence, DbContext, Repositories, Background Jobs, and JWT
├── WorkflowEngine.API            # Web API Controllers, Custom Exception/Audit Middleware
├── WorkflowEngine.Tests          # xUnit Test Suite for Condition Evaluators
└── workflow-engine-ui            # React + Vite + Material UI Portal (Dashboard, Designer, Form Builder, Inbox)
```

---

## 🔑 Demo Credentials (Password: `Password123!`)

To help test and evaluate approvals, delegations, and conditional skip rules:

| Username | Role | Department | Description |
| :--- | :--- | :--- | :--- |
| `admin` | Admin | IT | System Administrator (accesses designer, form builder, can seed data) |
| `ceo` | CEO | CEO Office | Final Approver (evaluated for large expense amounts/leave requests) |
| `it_hod` | Manager | IT | Department Head (receives first level HOD approvals) |
| `employee` | Employee | IT | Standard Requester (submits leave, purchases, and expenses) |
| `hr_mgr` | HR | HR | Evaluates leave requests at Step 2 |
| `fin_mgr` | Finance | Finance | Evaluates purchase and expense approvals at Step 2 |
| `proc_mgr` | Procurement| Procurement| Evaluates purchase orders at Step 3 |

---

## ⚡ Quick Start with Docker Compose

Ensure Docker and Docker Compose are installed on your machine. From the root directory containing `docker-compose.yml`, execute:

```bash
docker-compose up --build
```

This launches three interconnected containers:
1. **sqlserver**: A SQL Server 2022 instance running on port `1433`.
2. **workflow-api**: The .NET Core API running on port `5000` (exposes Swagger UI on `http://localhost:5000/swagger`).
3. **workflow-ui**: The React Nginx production build running on port `3000` (`http://localhost:3000`).

Once the containers are online, navigate to `http://localhost:3000` or `http://localhost:5000/swagger` and trigger the **Seed Data** button to automatically stand up the initial configuration, demo users, and active workflows.

---

## 🚀 Manual Local Development Setup

### 1. Database Mappings & Migrations (.NET Core Backend)
Before running, update the database connection string in `WorkflowEngine.API/appsettings.json` if not using Docker. Then run migrations:

```bash
# From workspace root
dotnet ef database update --project WorkflowEngine.Infrastructure --startup-project WorkflowEngine.API
```

Run the backend Web API:
```bash
dotnet run --project WorkflowEngine.API
```

### 2. Frontend React Installation
Navigate to the UI project, restore npm dependencies, and start the development server:

```bash
cd workflow-engine-ui
npm install
npm run dev
```
The client portal will start on `http://localhost:5173`.

---

## 🔌 API Integration Examples

### 1. Start a Workflow Instance
External systems submit request payloads containing dynamic variables. The engine loads published schemas, evaluates skip conditions, and maps tasks.

* **Endpoint**: `POST /api/workflowexecution/start`
* **Headers**: `Authorization: Bearer <jwt-token>`
* **Payload**:
```json
{
  "workflowCode": "AnnualLeave",
  "inputData": {
    "LeaveType": "Annual",
    "LeaveDays": "6",
    "FromDate": "2026-08-10",
    "ToDate": "2026-08-16",
    "Reason": "Summer Vacation"
  }
}
```

### 2. Approve a Step
* **Endpoint**: `POST /api/workflowexecution/approve`
* **Payload**:
```json
{
  "approvalId": "8f8f2b77-3e81-42ab-b199-a9a38f325d2c",
  "comment": "Approved - covered by proxy team",
  "digitalSignature": "DIGITALLY_SIGNED_BY_USER"
}
```

---

## ⚙️ Features Visualized

1. **Analytical Dashboard**: Monitored volume gauges, SLA violations counters, and department trends utilizing lightweight inline SVG graphs.
2. **Visual Step Designer**: Vertical flow visualizer linking start to end. Configures step numbers, roles/users, and custom SLA timeouts.
3. **Dynamic Form Builder**: Drag-and-drop or select inputs to set names, labels, options (dropdowns), and mark fields as required or read-only.
4. **Interactive Inbox**: Processes approvals, rejections, returns (clarifications), and assignments to proxy managers.
