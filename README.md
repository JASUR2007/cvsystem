# TalentHub CV Management System

ASP.NET Core 10, EF Core, PostgreSQL, React, TypeScript and Bootstrap. Recruiters maintain shared position templates and attributes; candidates keep master profile values and generate one CV per eligible position. CVs can be published after required values are filled.

## Quick start with Docker

Copy `.env.example` to `.env`, replace `POSTGRES_PASSWORD` and `JWT_KEY`, then run `docker compose up --build`. Open `http://localhost:8080`. PostgreSQL migrations and built-in attributes are applied automatically. To create the first administrator, set `BOOTSTRAP_ADMIN_EMAIL` and `BOOTSTRAP_ADMIN_PASSWORD` before startup. Normal registration creates a Candidate; an administrator can grant Recruiter access from the Users page.

If port 8080 is occupied, change `FRONTEND_PORT` and the port in `FRONTEND_BASE_URL` together.

The default Docker setup works without social sign-in or image storage. Their controls are shown when the corresponding services are configured.

## Local development

Use PostgreSQL and set `ConnectionStrings__Default`, `Jwt__Key` (at least 32 bytes), `Jwt__Issuer` and `Jwt__Audience`. Development defaults are in `backend/backend/appsettings.Development.json`; they are local-only values. Run `dotnet run --project backend/backend --launch-profile http`, then `npm ci` and `npm run dev` in `frontend`. The Vite dev server proxies `/api` to port 5015. Run `dotnet build backend/backend/backend.csproj`, `npm run build` and `npm run lint` to check both apps.

## Social sign-in

Set `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret`, or the corresponding `Authentication__GitHub__*` variables. Register these provider callback URLs:

- Google: `https://YOUR_BACKEND_HOST/signin-google`
- GitHub: `https://YOUR_BACKEND_HOST/signin-github`

For local Docker replace the host with `http://localhost:8080`. Set `Frontend__BaseUrl` to the public frontend origin. OAuth signs into a short-lived external cookie, then issues a one-time exchange code to the frontend; the API exchanges it for the same JWT used by password login. An existing account with the same email is not automatically linked to a social provider.

## External S3-compatible images

Set `S3__Endpoint`, `S3__AccessKey`, `S3__SecretKey`, `S3__Bucket` and `S3__PublicBaseUrl`. Set the frontend build variable `VITE_S3_PUBLIC_BASE_URL` to the same public base URL. The frontend asks the backend for a presigned URL and uploads JPEG, PNG or WebP directly to S3. Configure the bucket's CORS policy to allow `PUT` from the frontend origin and allow the public image origin for `GET`. No image bytes are stored in the API or PostgreSQL.

## Render deployment

Create a Render PostgreSQL database, a Docker web service using `backend/Dockerfile` with root directory `backend`, and a static site with root directory `frontend`, build command `npm ci && npm run build`, publish directory `dist`, and a rewrite from `/*` to `/index.html`. Add the backend URL as the static site's `VITE_API_BASE_URL` build variable. Add the backend environment variables `ConnectionStrings__Default`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Frontend__BaseUrl` and `Cors__Origins__0` (the exact frontend origin). Set `ASPNETCORE_URLS` to `http://+:10000` if the Render service requires port 10000. Add OAuth and S3 variables only when those services are configured. Use HTTPS public URLs for OAuth callbacks and origins.

If TLS terminates at a reverse proxy, configure ASP.NET Core to trust that proxy's forwarded headers so OAuth callback URLs use the public HTTPS scheme. `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` works in cloud setups with changing proxy IPs, but trusts forwarded headers from any source; restrict proxy IPs when the hosting topology permits it.

The API applies migrations at startup. Its health endpoint is `/api/health`. Do not use the development connection string or JWT key in production.
