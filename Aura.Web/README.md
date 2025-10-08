# Aura.Web - Frontend User Interface

## Overview

Aura.Web is the React-based frontend for Aura Video Studio. It provides a modern, responsive web interface built with TypeScript, Vite, and Fluent UI React components.

## Technology Stack

- **React 18** - UI library
- **TypeScript** - Type-safe JavaScript
- **Vite** - Fast build tool and dev server
- **Fluent UI React** - Microsoft's design system for Windows 11
- **ESLint** - Code linting (optional)

## Quick Start

### Prerequisites
- Node.js 20.x or later
- npm or yarn

### Installation

```bash
# Install dependencies
npm install

# Or with yarn
yarn install
```

### Development

```bash
# Start development server
npm run dev

# The app will be available at http://localhost:5173
```

### Build for Production

```bash
# Build optimized production bundle
npm run build

# Output will be in dist/
```

### Preview Production Build

```bash
# Serve the production build locally
npm run preview
```

## Project Structure

```
Aura.Web/
├── index.html          # HTML entry point
├── package.json        # npm dependencies and scripts
├── tsconfig.json       # TypeScript configuration
├── tsconfig.node.json  # TypeScript config for Vite
├── vite.config.ts      # Vite configuration
└── src/
    ├── main.tsx        # React entry point
    ├── App.tsx         # Root component
    ├── App.css         # App styles
    └── index.css       # Global styles
```

## Configuration

### Vite Configuration (vite.config.ts)

The Vite config includes:
- React plugin for JSX support
- Dev server on port 5173
- API proxy to forward `/api/*` requests to `http://127.0.0.1:5005`

```typescript
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5005',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, '')
      }
    }
  }
})
```

This allows the frontend to call `/api/healthz` which proxies to `http://127.0.0.1:5005/healthz`.

### TypeScript Configuration

Two TypeScript configs:
1. `tsconfig.json` - Main app configuration
2. `tsconfig.node.json` - Configuration for Vite config file

Strict mode is enabled for type safety.

## Features

### Current Features
- ✅ Health check integration with Aura.Api
- ✅ Fluent UI React theming
- ✅ TypeScript type safety
- ✅ API proxy for development

### Planned Features (Per Spec)
- [ ] **Create Wizard** (6 steps)
  - Brief input
  - Duration & pacing
  - Voice & music selection
  - Visual style
  - Provider configuration
  - Confirmation
- [ ] **Storyboard View**
  - Timeline editor
  - Scene management
  - Visual preview
- [ ] **Render Queue**
  - Job status
  - Progress tracking
  - Log viewer
- [ ] **Settings**
  - Provider configuration
  - API keys (encrypted)
  - Hardware overrides
  - Theme selection
- [ ] **Download Center**
  - Component installation
  - SHA-256 verification
  - Repair functionality

## Development Guidelines

### Adding a New Component

```typescript
// src/components/MyComponent.tsx
import { Button, Text } from '@fluentui/react-components'

export function MyComponent() {
  return (
    <div>
      <Text>Hello World</Text>
      <Button appearance="primary">Click Me</Button>
    </div>
  )
}
```

### Calling the API

```typescript
// Fetch capabilities from API
const response = await fetch('/api/capabilities')
const data = await response.json()
console.log(data)
```

The `/api` prefix is automatically proxied to the backend during development.

### Using Fluent UI Components

```typescript
import {
  FluentProvider,
  webLightTheme,
  webDarkTheme,
  Button,
  Card,
  Text
} from '@fluentui/react-components'

function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <Card>
        <Text>Welcome to Aura</Text>
        <Button appearance="primary">Get Started</Button>
      </Card>
    </FluentProvider>
  )
}
```

## Styling

### Global Styles (index.css)
- Sets font family to Segoe UI (Windows default)
- Basic reset and layout styles

### Component Styles
- Use CSS modules or inline styles with Fluent UI's `makeStyles`
- Follow Fluent UI design guidelines

### Theming
Fluent UI provides built-in themes:
- `webLightTheme` - Light mode
- `webDarkTheme` - Dark mode
- `teamsLightTheme` - Teams-style light
- Custom themes can be created

## API Integration

### Example: Fetching Hardware Capabilities

```typescript
import { useEffect, useState } from 'react'

interface Capabilities {
  tier: string
  cpu: { cores: number; threads: number }
  ram: { gb: number }
  // ...
}

function HardwareStatus() {
  const [caps, setCaps] = useState<Capabilities | null>(null)

  useEffect(() => {
    fetch('/api/capabilities')
      .then(res => res.json())
      .then(data => setCaps(data))
      .catch(err => console.error(err))
  }, [])

  return (
    <div>
      {caps ? (
        <Text>Tier: {caps.tier}, CPU: {caps.cpu.cores} cores</Text>
      ) : (
        <Text>Loading...</Text>
      )}
    </div>
  )
}
```

## Production Deployment

### Building for Production

```bash
npm run build
```

This creates an optimized build in `dist/` with:
- Minified JavaScript and CSS
- Code splitting for faster loading
- Asset optimization

### Serving from Aura.Api

In production, Aura.Api can serve the static files from `dist/`:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
});
```

Copy the `dist/` contents to the API's `wwwroot/` folder.

### Hosting in Windows Shell

The WinUI 3 or WPF shells will:
1. Start Aura.Api as a child process
2. Wait for API to be ready (`/healthz`)
3. Navigate WebView2 to `http://127.0.0.1:5005`
4. Aura.Api serves the static UI files

## Troubleshooting

### Port Already in Use
Change the dev server port in `vite.config.ts`:

```typescript
server: {
  port: 5174  // Use a different port
}
```

### API Not Responding
Ensure Aura.Api is running on `http://127.0.0.1:5005`:

```bash
cd ../Aura.Api
dotnet run
```

### Module Not Found Errors
```bash
# Clear node_modules and reinstall
rm -rf node_modules
npm install
```

### TypeScript Errors
```bash
# Check TypeScript compilation
npx tsc --noEmit
```

## Testing

### Unit Tests (Future)
```bash
# Install testing libraries
npm install --save-dev @testing-library/react @testing-library/jest-dom vitest

# Run tests
npm test
```

### E2E Tests (Future)
Use Playwright for end-to-end testing:

```bash
# Install Playwright
npm install --save-dev @playwright/test

# Run E2E tests
npx playwright test
```

## Contributing

When adding new features:
1. Follow the existing folder structure
2. Use TypeScript types for all props and state
3. Follow Fluent UI design patterns
4. Test API integration thoroughly
5. Update this README with new features

## Resources

- [React Documentation](https://react.dev/)
- [Vite Documentation](https://vitejs.dev/)
- [Fluent UI React](https://react.fluentui.dev/)
- [TypeScript Documentation](https://www.typescriptlang.org/)

## License

See LICENSE in the root of the repository.
