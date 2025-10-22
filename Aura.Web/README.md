# Aura.Web - Frontend User Interface

## Overview

Aura.Web is the React-based frontend for Aura Video Studio. It provides a modern, responsive web interface built with TypeScript, Vite, and Fluent UI React components.

## Technology Stack

### Core
- **React 18.3.1** - UI library with hooks and concurrent features
- **TypeScript 5.9.3** - Type-safe JavaScript with strict mode enabled
- **Vite 6.4.1** - Fast build tool and dev server with HMR
- **Fluent UI React 9.72.1** - Microsoft's design system for Windows 11

### State & Data
- **Zustand** - Lightweight state management
- **React Router 6** - Client-side routing
- **Axios** - HTTP client with interceptors
- **React Hook Form** - Form state management
- **Zod** - Schema validation

### Styling
- **Tailwind CSS 3** - Utility-first CSS framework
- **PostCSS** - CSS processing with autoprefixer

### Code Quality
- **ESLint** - Code linting with TypeScript and React rules
- **Prettier** - Code formatting
- **Vitest** - Unit testing framework
- **Playwright** - E2E testing

## Critical Dependencies

This section documents all critical npm packages used in the project.

### Production Dependencies

| Package | Version | Purpose | Documentation |
|---------|---------|---------|---------------|
| react | ^18.3.1 | Core UI library with hooks and concurrent features | [React Docs](https://react.dev/) |
| react-dom | ^18.3.1 | React renderer for web browsers | [React DOM Docs](https://react.dev/reference/react-dom) |
| react-router-dom | ^6.21.0 | Client-side routing and navigation | [React Router Docs](https://reactrouter.com/) |
| @fluentui/react-components | ^9.72.1 | Microsoft Fluent UI design system components | [Fluent UI Docs](https://react.fluentui.dev/) |
| @fluentui/react-icons | ^2.0.239 | Fluent UI icon library | [Fluent Icons](https://react.fluentui.dev/?path=/docs/icons-icons--page) |
| zustand | ^5.0.8 | Lightweight state management without boilerplate | [Zustand Docs](https://zustand.docs.pmnd.rs/) |
| axios | ^1.6.5 | Promise-based HTTP client with interceptors | [Axios Docs](https://axios-http.com/) |
| react-hook-form | ^7.49.3 | Performant form state management | [React Hook Form](https://react-hook-form.com/) |
| zod | ^3.22.4 | TypeScript-first schema validation | [Zod Docs](https://zod.dev/) |

### Development Dependencies

| Package | Version | Purpose | Documentation |
|---------|---------|---------|---------------|
| vite | ^6.4.1 | Fast build tool and dev server with HMR | [Vite Docs](https://vite.dev/) |
| typescript | ^5.9.3 | TypeScript compiler for type-safe JavaScript | [TypeScript Docs](https://www.typescriptlang.org/) |
| @vitejs/plugin-react | ^4.7.0 | Vite plugin for React Fast Refresh | [Plugin Docs](https://github.com/vitejs/vite-plugin-react) |
| @types/react | ^18.3.26 | TypeScript type definitions for React | [DefinitelyTyped](https://github.com/DefinitelyTyped/DefinitelyTyped) |
| @types/react-dom | ^18.3.7 | TypeScript type definitions for React DOM | [DefinitelyTyped](https://github.com/DefinitelyTyped/DefinitelyTyped) |
| tailwindcss | ^3.4.1 | Utility-first CSS framework | [Tailwind CSS](https://tailwindcss.com/) |
| postcss | ^8.4.33 | CSS processing tool | [PostCSS Docs](https://postcss.org/) |
| autoprefixer | ^10.4.16 | PostCSS plugin to add vendor prefixes | [Autoprefixer](https://github.com/postcss/autoprefixer) |
| eslint | ^8.57.1 | JavaScript/TypeScript linter | [ESLint Docs](https://eslint.org/) |
| @typescript-eslint/parser | ^6.18.1 | ESLint parser for TypeScript | [TypeScript ESLint](https://typescript-eslint.io/) |
| @typescript-eslint/eslint-plugin | ^6.18.1 | ESLint rules for TypeScript | [TypeScript ESLint](https://typescript-eslint.io/) |
| eslint-plugin-react | ^7.33.2 | React-specific linting rules | [eslint-plugin-react](https://github.com/jsx-eslint/eslint-plugin-react) |
| eslint-plugin-react-hooks | ^4.6.0 | ESLint rules for React Hooks | [Rules of Hooks](https://react.dev/warnings/invalid-hook-call-warning) |
| eslint-plugin-react-refresh | ^0.4.5 | ESLint plugin for React Fast Refresh | [Plugin Docs](https://github.com/ArnaudBarre/eslint-plugin-react-refresh) |
| eslint-plugin-jsx-a11y | ^6.8.0 | Accessibility linting for JSX | [jsx-a11y](https://github.com/jsx-eslint/eslint-plugin-jsx-a11y) |
| prettier | ^3.1.1 | Opinionated code formatter | [Prettier Docs](https://prettier.io/) |
| vitest | ^3.2.4 | Fast unit testing framework powered by Vite | [Vitest Docs](https://vitest.dev/) |
| @vitest/ui | ^3.2.4 | UI for Vitest test runner | [Vitest UI](https://vitest.dev/guide/ui.html) |
| @vitest/coverage-v8 | ^3.2.4 | Code coverage provider for Vitest | [Coverage Docs](https://vitest.dev/guide/coverage.html) |
| @testing-library/react | ^16.3.0 | React testing utilities | [Testing Library](https://testing-library.com/react) |
| @testing-library/jest-dom | ^6.9.1 | Custom Jest matchers for DOM | [jest-dom](https://github.com/testing-library/jest-dom) |
| @testing-library/user-event | ^14.6.1 | User interaction simulation for tests | [user-event](https://testing-library.com/docs/user-event/intro/) |
| @playwright/test | ^1.56.0 | End-to-end testing framework | [Playwright Docs](https://playwright.dev/) |
| jsdom | ^27.0.0 | JavaScript DOM implementation for testing | [jsdom](https://github.com/jsdom/jsdom) |

### Version Requirements

The following minimum versions are required for compatibility:
- **Node.js**: >= 18.x (LTS recommended)
- **npm**: >= 9.x
- **React**: >= 18.2.0
- **TypeScript**: >= 5.0.0
- **Vite**: >= 5.0.0

Current versions used:
- React: 18.3.1 ✅
- TypeScript: 5.9.3 ✅
- Vite: 6.4.1 ✅

### Security & Updates

- Run `npm audit` regularly to check for security vulnerabilities
- Run `npm audit fix` to automatically fix vulnerabilities when possible
- Keep dependencies up to date with `npm update` or `npm outdated`
- All dependencies are regularly reviewed for security and compatibility

Last security audit: Zero vulnerabilities ✅ (as of Vite 6.4.1 update)

## Quick Start

### Prerequisites
- Node.js 18.x or later (LTS recommended)
- npm 9.x or later
- Git for version control

### Installation

```bash
# Install dependencies
npm install

# Note: If you encounter issues, try:
npm ci  # Clean install from package-lock.json
```

### Development

```bash
# Start development server (opens browser automatically)
npm run dev

# The app will be available at http://localhost:5173
# Hot Module Replacement (HMR) is enabled for instant updates
```

### Build for Production

```bash
# Type check first
npm run type-check

# Build optimized production bundle
npm run build

# Output will be in dist/
# - Code splitting for better performance
# - Source maps enabled for debugging
# - Assets optimized and minified
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
    ├── main.tsx            # React entry point
    ├── App.tsx             # Root component with routing
    ├── App.css             # App styles
    ├── index.css           # Global styles
    ├── types.ts            # TypeScript type definitions
    ├── navigation.tsx      # Navigation configuration
    ├── components/         # Reusable UI components
    │   └── Layout.tsx      # Main layout with sidebar
    ├── pages/              # Page components (one per route)
    │   ├── WelcomePage.tsx
    │   ├── DashboardPage.tsx
    │   ├── CreatePage.tsx
    │   ├── RenderPage.tsx
    │   ├── PublishPage.tsx
    │   ├── DownloadsPage.tsx
    │   └── SettingsPage.tsx
    └── hooks/              # Custom React hooks (future)
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

### Environment Variables

Environment variables are managed through `.env` files. Create a `.env` file based on `.env.example`:

```bash
# Copy the example file
cp .env.example .env
```

Available environment variables:

- `VITE_API_BASE_URL` - Backend API URL (default: http://localhost:5272)
- `VITE_APP_VERSION` - Application version
- `VITE_APP_NAME` - Application name
- `VITE_ENV` - Environment (development/production)
- `VITE_ENABLE_ANALYTICS` - Enable analytics (true/false)
- `VITE_ENABLE_DEBUG` - Enable debug logging (true/false)

Access environment variables in code:

```typescript
import { env } from './config/env';

console.log(env.apiBaseUrl);  // Type-safe access
console.log(env.isDevelopment);
```

### Code Quality Tools

#### ESLint

```bash
# Run linter
npm run lint

# Auto-fix issues
npm run lint:fix
```

Configuration: `.eslintrc.cjs`
- TypeScript and React rules enabled
- Accessibility checks with jsx-a11y
- React Hooks rules
- Warnings for console statements (except warn/error)

#### Prettier

```bash
# Format code
npm run format

# Check formatting
npm run format:check
```

Configuration: `.prettierrc`
- Single quotes
- 100 character line width
- Trailing commas (ES5)
- 2 space indentation

#### Path Aliases

TypeScript and Vite are configured with `@` alias for `src/`:

```typescript
// Instead of:
import { Component } from '../../../components/Component';

// Use:
import { Component } from '@/components/Component';
```

### Tailwind CSS

Tailwind CSS is configured with a custom theme including:
- Custom color palette (primary, secondary, success, warning, error)
- Extended spacing scale (4px base unit)
- Custom animations (fade-in, slide-in)
- Dark mode support with class strategy

Use Tailwind utilities in your components:

```tsx
<div className="flex items-center gap-4 p-4 bg-primary-500 rounded-lg">
  <Button className="px-6 py-2 bg-white text-primary-700 hover:bg-gray-100">
    Click me
  </Button>
</div>
```

### VS Code Setup

Recommended VS Code extensions (`.vscode/extensions.json`):
- ESLint
- Prettier
- Tailwind CSS IntelliSense
- TypeScript and JavaScript Language Features
- Path Intellisense
- Playwright Test
- Vitest Explorer

Workspace settings (`.vscode/settings.json`):
- Format on save enabled
- ESLint auto-fix on save
- Prettier as default formatter

## Features

### Current Features
- ✅ Health check integration with Aura.Api
- ✅ Fluent UI React theming
- ✅ TypeScript type safety
- ✅ API proxy for development
- ✅ **React Router** - Client-side routing
- ✅ **Complete Navigation** - Sidebar with all pages
- ✅ **Welcome Page** - System status and hardware detection
- ✅ **Dashboard Page** - Project management
- ✅ **Create Wizard** - Multi-step video creation (3 steps)
- ✅ **Render Queue** - Job management and progress
- ✅ **Publish Page** - YouTube metadata
- ✅ **Downloads Page** - Dependency management
- ✅ **Settings Page** - Multi-tab configuration (System, Providers, API Keys, Privacy)

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
