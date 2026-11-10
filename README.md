
# TaskFlow

Full-stack task management application built with Angular, ASP.NET Core and PostgreSQL.

TaskFlow provides a modern, responsive dashboard for organizing and managing tasks, with a RESTful API and persistent database storage.

## Features

- Create, view, update and delete tasks
- Mark tasks as completed or pending
- Edit tasks using modal dialogs
- Search tasks by title or description
- Filter tasks by completion status
- Sort tasks by date or title
- Reactive dashboard statistics
- Confirmation dialogs and notifications
- Responsive UI with Angular Material
- Server-side input validation

## Tech Stack

### Frontend
- Angular 21
- TypeScript
- Angular Material
- SCSS
- Signals and RxJS

### Backend
- ASP.NET Core Web API (.NET 10)
- C#
- Entity Framework Core
- REST API
- Swagger / OpenAPI

### Database & Tools
- PostgreSQL 17
- Docker Compose
- Git & GitHub

## Architecture

```text
Angular Frontend
       |
       | HTTP / REST
       v
ASP.NET Core Web API
       |
       | Entity Framework Core
       v
PostgreSQL Database
```

## Getting Started

### Prerequisites

- Node.js and npm
- Angular CLI
- .NET 10 SDK
- Docker Desktop

### 1. Clone the repository

```bash
git clone https://github.com/EsanclemDev/taskflow.git
cd taskflow
```

### 2. Configure the database

Create a `.env` file in the project root using `.env.example` as a template.

Start PostgreSQL:

```bash
docker compose up -d
```

### 3. Configure the backend

Set the `ConnectionStrings__DefaultConnection` environment variable to your local PostgreSQL connection string.

From the backend directory:

```bash
cd backend/TaskManager.Api
dotnet ef database update
dotnet run
```

### 4. Start the frontend

In a separate terminal:

```bash
cd frontend/task-manager-ui
npm install
npm start
```

Open http://localhost:4200 in your browser.

The frontend expects the backend API at http://localhost:5000.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | Get all tasks |
| GET | `/api/tasks/{id}` | Get task by ID |
| POST | `/api/tasks` | Create a task |
| PUT | `/api/tasks/{id}` | Update a task |
| DELETE | `/api/tasks/{id}` | Delete a task |

## Screenshots

Screenshots of the dashboard and task management features will be added soon.

## Author

**Enrique Sanclemente**

Full Stack Developer | .NET · Angular · Java · SQL

GitHub: https://github.com/EsanclemDev
