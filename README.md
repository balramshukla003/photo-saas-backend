# PhotoPrint — Backend API

![.NET](https://img.shields.io/badge/.NET_Core-8-512BD4?logo=dotnet&logoColor=white&style=flat-square)
![MySQL](https://img.shields.io/badge/MySQL-8-4479A1?logo=mysql&logoColor=white&style=flat-square)
![JWT](https://img.shields.io/badge/Auth-JWT-orange?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

The backend API for PhotoPrint. Handles login, license verification, and photo processing — removes the background and enhances the image before sending it back to the frontend.

---

## What this does

- Accepts email and password login — returns a JWT token
- Every token contains the user's license details (active/expired, expiry date)
- Checks license on every request — expired license gets blocked automatically
- Receives a photo, sends it to remove.bg to remove the background
- Enhances the image (brightness, contrast, sharpness) using ImageSharp
- Crops the photo to exact passport size: 35mm × 45mm at 300 DPI
- Returns the processed image as base64 to the frontend

---

## Before you start

| What | Where to get it |
|---|---|
| .NET 8 SDK | https://dotnet.microsoft.com/download/dotnet/8.0 |
| MySQL 8 or MariaDB | https://dev.mysql.com/downloads/installer |
| remove.bg API key | https://www.remove.bg/api (50 free/month) |
| Database set up | See [photoprint-database](../photoprint-database) repo — run schema first |

---

## Setup

**1. Clone this repo**
```bash
git clone https://github.com/balramshukla003/photo-saas-backend
cd photo-saas-backend
```

**2. Install all packages** (one command)
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0 && dotnet add package System.IdentityModel.Tokens.Jwt --version 7.3.1 && dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0 && dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0 && dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0 && dotnet add package BCrypt.Net-Next --version 4.0.3 && dotnet add package SixLabors.ImageSharp --version 3.1.3 && dotnet add package Swashbuckle.AspNetCore --version 6.6.2
```

**3. Update `appsettings.json`** with your own values:
```json
"DefaultConnection": "Server=localhost;Port=3306;Database=photoprint;User=root;Password=YOUR_DB_PASSWORD;CharSet=utf8mb4;"
"Secret": "change-this-to-a-long-random-string-in-production"
"ApiKey": "YOUR_REMOVE_BG_API_KEY"
```

**4. Install EF Core tool** (only needed once)
```bash
dotnet tool install --global dotnet-ef
```

**5. Scaffold database models** (run this after the database is created)
```bash
dotnet ef dbcontext scaffold "Server=localhost;Port=3306;Database=photoprint;User=root;Password=YOUR_DB_PASSWORD;CharSet=utf8mb4;AllowPublicKeyRetrieval=true;SslMode=None;" Pomelo.EntityFrameworkCore.MySql --output-dir Models --context-dir Data --context PhotoPrintDbContext --data-annotations --no-onconfiguring --force
```

**6. Build and run**
```bash
dotnet build
dotnet run
```

API runs at **http://localhost:5000**
Swagger UI at **http://localhost:5000/swagger**

---

## API endpoints

| Method | URL | Who can call it | What it does |
|---|---|---|---|
| POST | `/api/auth/login` | Anyone | Login and get JWT token |
| GET | `/api/user/profile` | Logged in users | Get profile and license info |
| POST | `/api/photo/process` | Licensed users only | Process a photo |

---

## How the license system works

- When a user logs in, their license details are embedded directly inside the JWT token
- On every request to a protected route, the backend checks if the license is still active and not expired
- If expired — the request is blocked with a 403 response
- No extra database calls needed for license checks — everything is in the token

---

## Photo processing steps

```
Upload photo
    ↓
Send to remove.bg API → background removed, white background added
    ↓
ImageSharp enhancement → brightness +8%, contrast +12%, sharpness +0.75
    ↓
Crop to passport size → 413 × 531 px (35mm × 45mm at 300 DPI)
    ↓
Return as base64 PNG to frontend
```

---

## Related repos

| Repo | Purpose |
|---|---|
| [photoprint-frontend](photo-saas-frontend) | React frontend |
| [photoprint-database](photo-saas-database) | MySQL schema and seed data |
| [photoprint-user-tools](photo-saas-python) | Scripts to add users and reset passwords |
