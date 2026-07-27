# Multi-stage build: React SPA → ASP.NET Core host serving API + static assets (doc 18 §2).

# --- Stage 1: frontend ---
FROM node:22-alpine AS frontend
WORKDIR /app
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci --no-fund --no-audit
COPY src/frontend/ ./
RUN npm run build

# --- Stage 2: backend ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend
WORKDIR /src
COPY global.json ./
COPY src/backend/ ./backend/
RUN dotnet publish backend/Lif.Api/Lif.Api.csproj -c Release -o /out

# --- Stage 3: runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=backend /out ./
COPY --from=frontend /app/dist ./wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lif.Api.dll"]
