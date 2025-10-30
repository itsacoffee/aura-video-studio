# First Run Guide - Aura Video Studio

This guide will help you get Aura Video Studio running for the first time.

## Quick Start (Windows 11)

### Prerequisites
- Node.js 18.0.0+ installed (18.18.0 recommended)
- .NET 8 SDK installed
- Git with long paths enabled

### Step-by-Step Setup

#### 1. Build the Frontend

The frontend MUST be built before running the backend. Open a terminal in the repository root:

```bash
cd Aura.Web
npm install
npm run build
```

This creates the `dist` folder with the compiled frontend application.

#### 2. Build the Backend

The backend build automatically copies the frontend to `wwwroot`:

```bash
cd ..
dotnet build Aura.sln --configuration Release
```

The build process will:
- Copy `Aura.Web/dist` → `Aura.Api/bin/Release/net8.0/wwwroot`
- Prepare the backend to serve the frontend

#### 3. Run the Application

```bash
cd Aura.Api
dotnet run --configuration Release
```

The application will start on `http://127.0.0.1:5005`

#### 4. Access the Application

Open your browser and navigate to:
```
http://127.0.0.1:5005
```

You should see the Aura Video Studio welcome screen.

## Development Mode

For development, you can run frontend and backend separately:

### Terminal 1 - Frontend Dev Server
```bash
cd Aura.Web
npm run dev
```
Frontend runs on `http://localhost:5173`

### Terminal 2 - Backend API
```bash
cd Aura.Api
dotnet run
```
Backend runs on `http://127.0.0.1:5005`

The frontend dev server proxies API requests to the backend automatically.

## Troubleshooting

### White Screen / "Application Failed to Initialize"

**Cause**: The frontend was not built or not copied to wwwroot.

**Solution**:
1. Build frontend: `cd Aura.Web && npm run build`
2. Rebuild backend: `cd .. && dotnet build Aura.Api --configuration Release`
3. Restart application

### "VITE_API_BASE_URL is not defined"

**Cause**: Missing environment configuration.

**Solution**:
Create `Aura.Web/.env.local`:
```env
VITE_API_BASE_URL=http://127.0.0.1:5005
```

### Backend Builds But Frontend Doesn't Load

**Cause**: The `dist` folder may not exist when backend builds.

**Solution**:
1. Always build frontend FIRST
2. Then build backend
3. Backend build will only copy frontend if `dist` exists

### Port Already in Use

**Cause**: Another process is using port 5005 or 5173.

**Solution**:
- Stop other Aura instances
- Or change ports in configuration files

## Build Order Matters!

**✅ CORRECT ORDER:**
1. `cd Aura.Web && npm run build` (Frontend first)
2. `cd .. && dotnet build` (Backend second - copies frontend)
3. `cd Aura.Api && dotnet run` (Run application)

**❌ WRONG ORDER:**
1. `dotnet build` (Backend built, but no frontend to copy)
2. `npm run build` (Frontend built, but backend already compiled)
3. Result: White screen because wwwroot is empty

## Complete Clean Build

If you encounter persistent issues, perform a clean build:

```bash
# Clean everything
cd Aura.Web
rm -rf node_modules dist
npm install
npm run build

# Clean and rebuild backend
cd ..
dotnet clean
dotnet build Aura.sln --configuration Release

# Run
cd Aura.Api
dotnet run --configuration Release
```

## Environment Files

The application uses different environment files for different scenarios:

- `.env.development` - Used during `npm run dev` (development server)
- `.env.production` - Used during `npm run build` (production build)
- `.env.local` - Local overrides (create this file, not tracked in git)

**Recommended `.env.local` for development:**
```env
VITE_API_BASE_URL=http://127.0.0.1:5005
VITE_APP_VERSION=1.0.0-dev
VITE_ENV=development
VITE_ENABLE_DEBUG=true
```

## Next Steps

Once the application is running:

1. Complete the onboarding wizard
2. Configure your preferred AI providers (optional)
3. Create your first video project

For more detailed information, see:
- `BUILD_GUIDE.md` - Complete build instructions
- `README.md` - Project overview
- `docs/` - Detailed documentation
