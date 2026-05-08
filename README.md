# VectorFlow

> Lightweight issue tracking for small dev teams that move fast.

VectorFlow is a full-stack project management tool built with **ASP.NET Core 10** and **Blazor WebAssembly**. It gives small development teams a real-time Kanban board, a complete issue lifecycle, workspace collaboration, and a full audit trail — with zero enterprise overhead.

---

## Screenshots

> _Coming soon — Kanban board, issue detail, and workspace dashboard_

---

## Features

### Core

- **Real-time Kanban board** — drag issues across Backlog, Todo, In Progress, In Review, and Done. All moves broadcast instantly via SignalR
- **Issue lifecycle** — auto-generated keys (e.g. `VF-42`), priority, type, assignee, due date, labels, and position ordering
- **Activity log** — immutable, per-issue audit trail recording every field change with actor and timestamp
- **Comments** — markdown-powered comment threads per issue, delivered in real time
- **Labels** — custom labels with hex color, scoped per project

### Collaboration

- **Workspaces** — fully isolated environments with multiple projects per workspace
- **Invitation system** — Owners and Admins invite members by email; invitations expire after 7 days and can be cancelled
- **Role-based access control** — three roles (Owner, Admin, Member) with server-side enforcement on every request

### Auth

- JWT authentication via HttpOnly cookies (access token + refresh token)
- Refresh token rotation — stolen tokens are invalidated on next use
- Email verification on signup
- Forgot password / reset password with SHA-256 hashed reset tokens
- Silent token refresh via a `DelegatingHandler` in the Blazor client

---

## Tech Stack

| Layer         | Technology                          |
| ------------- | ----------------------------------- |
| Backend API   | ASP.NET Core 10                     |
| Frontend      | Blazor WebAssembly                  |
| Real-time     | SignalR — project-scoped hubs       |
| ORM           | Entity Framework Core               |
| Database      | PostgreSQL                          |
| Identity      | ASP.NET Core Identity + JWT         |
| UI components | MudBlazor                           |
| Email         | MailKit (dev) / Resend (production) |
| Environment   | DotNetEnv                           |

---

## Architecture

VectorFlow follows a clean, flat solution structure — deliberately avoiding over-engineering for a project of this scope:

```
VectorFlow.sln
├── VectorFlow.Api         # ASP.NET Core — controllers, services, hubs, EF Core, Identity
├── VectorFlow.Client      # Blazor WASM — SPA frontend
├── VectorFlow.Shared      # DTOs, enums, SignalR event contracts (referenced by both)
└── VectorFlow.Tests       #**** xUnit — service layer unit tests
```

### Key patterns

- **Layered services** — thin controllers delegate to scoped service classes; no business logic in controllers
- **EF Core configurations** — each entity has its own `IEntityTypeConfiguration<T>` class; `OnModelCreating` only calls `ApplyConfigurationsFromAssembly`
- **`IDesignTimeDbContextFactory`** — migrations work independently of `Program.cs` startup
- **SignalR broadcaster** — an `IProjectHubBroadcaster` abstraction decouples domain services from SignalR internals, keeping services testable
- **Shared DTOs** — `VectorFlow.Shared` eliminates type drift between the API and the Blazor client
- **HttpOnly cookie auth** — tokens never touch `localStorage`; a `DelegatingHandler` on the Blazor `HttpClient` handles silent refresh transparently

---

## API Reference

### Auth

| Method | Endpoint                        | Description                                          |
| ------ | ------------------------------- | ---------------------------------------------------- |
| `POST` | `/api/auth/register`            | Create account, send verification email              |
| `POST` | `/api/auth/login`               | Authenticate, set HttpOnly cookies                   |
| `GET`  | `/api/auth/me`                  | Restore auth state from cookie (used on page reload) |
| `GET`  | `/api/auth/verify-email?token=` | Verify email address                                 |
| `POST` | `/api/auth/resend-verification` | Resend verification email                            |
| `POST` | `/api/auth/forgot-password`     | Send password reset link                             |
| `POST` | `/api/auth/reset-password`      | Reset password, revoke all sessions                  |
| `POST` | `/api/auth/refresh`             | Rotate refresh token, issue new access token         |
| `POST` | `/api/auth/logout`              | Revoke refresh token, clear cookies                  |

### Workspaces

| Method   | Endpoint                                     | Description                   |
| -------- | -------------------------------------------- | ----------------------------- |
| `GET`    | `/api/workspaces`                            | List user's workspaces        |
| `GET`    | `/api/workspaces/{slug}`                     | Get workspace by slug         |
| `POST`   | `/api/workspaces`                            | Create workspace              |
| `PUT`    | `/api/workspaces/{id}`                       | Update name/description       |
| `DELETE` | `/api/workspaces/{id}`                       | Delete workspace (Owner only) |
| `GET`    | `/api/workspaces/{id}/members`               | List members                  |
| `PUT`    | `/api/workspaces/{id}/members/{userId}/role` | Change member role            |
| `DELETE` | `/api/workspaces/{id}/members/{userId}`      | Remove member                 |
| `DELETE` | `/api/workspaces/{id}/members/me`            | Leave workspace               |

### Invitations

| Method   | Endpoint                                          | Description                               |
| -------- | ------------------------------------------------- | ----------------------------------------- |
| `POST`   | `/api/workspaces/{id}/invitations`                | Send invitation                           |
| `GET`    | `/api/workspaces/{id}/invitations`                | List workspace invitations                |
| `GET`    | `/api/invitations/mine`                           | List pending invitations for current user |
| `POST`   | `/api/invitations/accept?token=`                  | Accept invitation                         |
| `POST`   | `/api/invitations/decline?token=`                 | Decline invitation                        |
| `DELETE` | `/api/workspaces/{id}/invitations/{invitationId}` | Cancel invitation                         |

### Projects

| Method   | Endpoint                                    | Description    |
| -------- | ------------------------------------------- | -------------- |
| `GET`    | `/api/workspaces/{id}/projects`             | List projects  |
| `GET`    | `/api/workspaces/{id}/projects/{projectId}` | Get project    |
| `POST`   | `/api/workspaces/{id}/projects`             | Create project |
| `PUT`    | `/api/workspaces/{id}/projects/{projectId}` | Update project |
| `DELETE` | `/api/workspaces/{id}/projects/{projectId}` | Delete project |

### Issues

| Method   | Endpoint                                       | Description                 |
| -------- | ---------------------------------------------- | --------------------------- |
| `GET`    | `/api/projects/{id}/issues`                    | List issues (filterable)    |
| `GET`    | `/api/projects/{id}/issues/{issueId}`          | Get issue detail            |
| `GET`    | `/api/projects/{id}/issues/{issueId}/activity` | Get activity log            |
| `POST`   | `/api/projects/{id}/issues`                    | Create issue                |
| `PUT`    | `/api/projects/{id}/issues/{issueId}`          | Update issue fields         |
| `PATCH`  | `/api/projects/{id}/issues/{issueId}/status`   | Update status (Kanban drag) |
| `PATCH`  | `/api/projects/{id}/issues/{issueId}/position` | Update card position        |
| `DELETE` | `/api/projects/{id}/issues/{issueId}`          | Delete issue                |

### Comments & Labels

| Method   | Endpoint                         | Description                |
| -------- | -------------------------------- | -------------------------- |
| `GET`    | `/api/issues/{issueId}/comments` | List comments              |
| `POST`   | `/api/issues/{issueId}/comments` | Add comment                |
| `PUT`    | `/api/comments/{commentId}`      | Edit comment (author only) |
| `DELETE` | `/api/comments/{commentId}`      | Delete comment             |
| `GET`    | `/api/projects/{id}/labels`      | List labels                |
| `POST`   | `/api/projects/{id}/labels`      | Create label               |
| `PUT`    | `/api/labels/{labelId}`          | Update label               |
| `DELETE` | `/api/labels/{labelId}`          | Delete label               |

### SignalR

**Hub endpoint:** `/hubs/project`

| Client → Server           | Description                      |
| ------------------------- | -------------------------------- |
| `JoinProject(projectId)`  | Join a project's real-time group |
| `LeaveProject(projectId)` | Leave the group                  |

| Server → Client        | Trigger                           |
| ---------------------- | --------------------------------- |
| `IssueCreated`         | New issue created                 |
| `IssueUpdated`         | Issue fields changed              |
| `IssueDeleted`         | Issue deleted                     |
| `IssueStatusChanged`   | Kanban card moved between columns |
| `IssuePositionChanged` | Card reordered within a column    |
| `CommentCreated`       | New comment posted                |
| `CommentUpdated`       | Comment edited                    |
| `CommentDeleted`       | Comment deleted                   |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com) (for PostgreSQL)

### 1. Clone the repo

```bash
git clone https://github.com/KOKUMUbooker/vector-flow.git
cd vector-flow
```

### 2. Configure environment

```bash
cp env.example .env
```

Generate a secure JWT key for use in the env:

```bash
dotnet run --project tools/GenerateKey
# or inline:
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
```

#### 3. Start PostgreSQL with Docker

```bash
docker compose -f docker-compose-db.yaml up -d
```

### 4. Run database migrations

```bash
cd VectorFlow.Api
dotnet ef migrations add InitialCreate # If no Migrations folder
dotnet ef database update
```

### 5. Start the API

```bash
cd VectorFlow.Api
dotnet run
```

API runs at `http://localhost:5186`. OpenAPI docs at `http://localhost:5186/openapi`.

### 6. Start the Blazor client

```bash
cd VectorFlow.Client
dotnet run
```

Client runs at `http://localhost:5131`.

---

## Role Permissions

| Action                  | Owner  | Admin | Member |
| ----------------------- | :----: | :---: | :----: |
| Delete workspace        |   ✅   |  ❌   |   ❌   |
| Promote member to Admin |   ✅   |  ❌   |   ❌   |
| Demote other Admins     |   ✅   |  ❌   |   ❌   |
| Send/cancel invitations |   ✅   |  ✅   |   ❌   |
| Remove members          |   ✅   | ✅\*  |   ❌   |
| Create/delete projects  |   ✅   |  ✅   |   ❌   |
| Manage labels           |   ✅   |  ✅   |   ❌   |
| Create issues           |   ✅   |  ✅   |   ✅   |
| Update issue status     |   ✅   |  ✅   |   ✅   |
| Edit/delete own issues  |   ✅   |  ✅   |   ✅   |
| Edit/delete any issue   |   ✅   |  ✅   |   ❌   |
| Comment on issues       |   ✅   |  ✅   |   ✅   |
| Delete any comment      |   ✅   |  ✅   |   ❌   |
| Leave workspace         | ❌\*\* |  ✅   |   ✅   |

_\* Admins cannot remove other Admins — only the Owner can._
_\*\* Owner must delete the workspace instead of leaving._

---

## Roadmap

- [x] Register page
- [ ] Workspace dashboard
- [ ] Kanban board (Blazor WASM + MudBlazor drag-and-drop)
- [ ] Issue detail page with real-time comments and activity log
- [ ] Project settings (labels management)
- [ ] Workspace settings (member management, invitations)
- [ ] Unit tests for service layer (xUnit + Moq)
- [ ] Docker Compose setup
- [ ] Deploy to a VPS

---

## Contributing

This is a portfolio project and not currently open to external contributions. Feel free to fork it and adapt it for your own use.

---

<p align="center">
  Built by <a href="https://github.com/KOKUMUbooker">@Booker</a> · Nairobi, Kenya
</p>
