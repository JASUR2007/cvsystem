# TalentHub — CV Management System

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61dafb?style=flat&logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178c6?style=flat&logo=typescript)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169e1?style=flat&logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ed?style=flat&logo=docker)](https://www.docker.com/)

**TalentHub** is an enterprise-grade CV and applicant tracking management system designed for modern recruiting teams and job seekers. It features dynamic candidate skill-matrix evaluation, customizable position attributes, interactive candidate comparison, discussions, and multi-tenant role-based access control.

---

##  Live Deployment

- **Frontend Application**: [https://cvsystem-frontend.onrender.com](https://cvsystem-frontend.onrender.com)
- **Default Administrator Account**:
  - **Email**: `makej1318@gmail.com`
  - **Password**: `Password123!`

---

##  Key Features

### 1. Multi-Role Authorization & Security (RBAC)
- **Administrator**: Full system management, user role assignments, moderation, audit logging.
- **Recruiter**: Create and manage job openings, configure dynamic attribute schemas, inspect applicant comparison matrices, evaluate candidate CVs.
- **Candidate**: Browse positions, build structured CVs matching position attributes, submit applications, participate in discussion threads.
- **Secure Authentication**: ASP.NET Core Identity with JWT bearer tokens, refresh tokens, Google OAuth2, and GitHub OAuth2 integration.

### 2. Candidate Evaluation Matrix & Dynamic Attributes
- Define custom attributes per position (Text, Number, Boolean, Select).
- Real-time tabular candidate comparison matrix with filtering (Equals, GreaterThan, LessThan, Contains).
- Embedded CV overview directly inside position detail tabs with quick-access full-screen mode.

### 3. Modern User Experience
- **Responsive Adaptive Design**: Optimized for desktops, tablets, and mobile phones with custom bottom navigation bar and mobile card lists.
- **Light & Dark Theme**: Full theme switching with automatic system preference detection and accessible contrast ratios.
- **Internationalization (i18n)**: Multi-language support (English, Russian, Latvian).
- **Rich Media & Cloud Storage**: Cloudflare R2 / AWS S3 integration for avatar and attachment uploads.
- **Discussion Boards**: Markdown-enabled discussion feeds for position Q&A.

---

##  Architecture & Tech Stack

```text
┌────────────────────────────────────────────────────────┐
│                   TalentHub Frontend                   │
│      React 19 + TypeScript + Vite + Bootstrap 5        │
└───────────────────────────┬────────────────────────────┘
                            │ HTTPS / REST API / JWT
┌───────────────────────────▼────────────────────────────┐
│                    TalentHub Backend                   │
│             ASP.NET Core Web API (.NET 10)             │
│   EF Core, ASP.NET Identity, AWSSDK.S3, OAuth2         │
└───────────────────┬────────────────────────┬───────────┘
                    │                        │
┌───────────────────▼────────────┐  ┌────────▼───────────┐
│           PostgreSQL           │  │   Cloudflare R2    │
│  Relational Data & Migrations  │  │ S3 Object Storage  │
└────────────────────────────────┘  └────────────────────┘
```

### Backend
- **Framework**: ASP.NET Core (.NET 10)
- **Database ORM**: Entity Framework Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Authentication**: JWT Bearer, ASP.NET Core Identity, Google & GitHub OAuth
- **Storage**: Amazon S3 Compatible Client (`AWSSDK.S3`) connected to Cloudflare R2
- **Documentation**: Swagger / OpenAPI

### Frontend
- **Framework**: React 19, TypeScript, Vite
- **Styling**: Bootstrap 5 + Custom Scoped CSS Design System
- **Icons**: Monochrome SVG Vector Icons
- **State & Data**: Custom React hooks with optimistic updates and caching

---

##  Getting Started

### Prerequisites
- [Docker](https://www.docker.com/) and [Docker Compose](https://docs.docker.com/compose/)
- Alternatively, for manual local execution:
  - [.NET 10 SDK](https://dotnet.microsoft.com/download)
  - [Node.js 20+](https://nodejs.org/) & [pnpm](https://pnpm.io/)
  - [PostgreSQL 15+](https://www.postgresql.org/)

---

### Running with Docker Compose (Recommended)

1. Clone the repository:
   ```bash
   git clone https://github.com/JASUR2007/cvsystem.git
   cd cvsystem
   ```

2. Configure environment variables:
   ```bash
   cp .env.example .env
   ```
   *(Ensure `POSTGRES_PASSWORD` and `JWT_KEY` are configured in `.env`)*

3. Start all services:
   ```bash
   docker compose up --build -d
   ```

4. Access the applications:
   - **Frontend UI**: [http://localhost:8080](http://localhost:8080)
   - **API Backend**: [http://localhost:5000](http://localhost:5000)
   - **Swagger Docs**: [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

### Running Manually

#### 1. Database Setup
Ensure PostgreSQL is running, then create the database:
```sql
CREATE DATABASE cvsystem;
```

#### 2. Backend Setup
```bash
cd backend/backend
dotnet restore
dotnet run
```
The backend automatically executes EF Core database migrations and bootstraps the default administrator account on first boot.

#### 3. Frontend Setup
```bash
cd frontend
pnpm install
pnpm dev
```
Navigate to `http://localhost:5173`.

---

## ⚙️Environment Variables Reference

| Variable | Description | Default / Example |
| :--- | :--- | :--- |
| `POSTGRES_PASSWORD` | PostgreSQL superuser password | `SuperSecretPassword!` |
| `JWT_KEY` | Secret key for JWT token generation | *(At least 32 characters)* |
| `FRONTEND_BASE_URL` | Allowed frontend origin for CORS | `http://localhost:8080` |
| `BOOTSTRAP_ADMIN_EMAIL` | Initial admin account email | `makej1318@gmail.com` |
| `BOOTSTRAP_ADMIN_PASSWORD` | Initial admin account password | `Password123!` |
| `S3_ENDPOINT` | Cloudflare R2 / AWS S3 endpoint | `https://<account-id>.r2.cloudflarestorage.com` |
| `S3_ACCESS_KEY` | S3 Access Key ID | `your-access-key-id` |
| `S3_SECRET_KEY` | S3 Secret Access Key | `your-secret-access-key` |
| `S3_BUCKET` | S3 bucket name | `cvsystem` |
| `S3_PUBLIC_BASE_URL` | Public CDN URL for serving uploads | `https://pub-<id>.r2.dev` |

---

##  License
This project is developed as part of advanced web engineering curriculum. All rights reserved.
