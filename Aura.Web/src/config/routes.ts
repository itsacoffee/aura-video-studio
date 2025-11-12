/**
 * Application routes configuration
 * Centralized route paths for type-safe navigation
 */

export const ROUTES = {
  HOME: '/',
  SETUP: '/setup',
  ONBOARDING: '/onboarding', // Legacy route, redirects to SETUP
  DASHBOARD: '/dashboard',
  IDEATION: '/ideation',
  TRENDING: '/trending',
  CREATE: '/create',
  CREATE_LEGACY: '/create/legacy',
  TIMELINE: '/timeline',
  EDITOR: '/editor/:jobId',
  EDITOR_BASE: '/editor',
  PACING: '/pacing',
  RENDER: '/render',

  PROJECTS: '/projects',
  ASSETS: '/assets',
  RAG: '/rag',
  JOBS: '/jobs',
  DOWNLOADS: '/downloads',
  HEALTH: '/health',
  LOGS: '/logs',
  SETTINGS: '/settings',
} as const;

/**
 * Menu navigation routes - subset of ROUTES that menu items can navigate to
 * CRITICAL: Every route in this object MUST exist in App.tsx Routes configuration
 */
export const MENU_ROUTES = {
  HOME: ROUTES.HOME,
  CREATE: ROUTES.CREATE,
  PROJECTS: ROUTES.PROJECTS,
  ASSETS: ROUTES.ASSETS,
  RAG: ROUTES.RAG,
  RENDER: ROUTES.RENDER,
  EDITOR: ROUTES.EDITOR_BASE,
  SETTINGS: ROUTES.SETTINGS,
  LOGS: ROUTES.LOGS,
  HEALTH: ROUTES.HEALTH,
} as const;

/**
 * Type representing all valid menu navigation routes
 */
export type MenuRoute = (typeof MENU_ROUTES)[keyof typeof MENU_ROUTES];

/**
 * Helper function to build editor route with job ID
 */
export function getEditorRoute(jobId: string): string {
  return `/editor/${jobId}`;
}

/**
 * Helper function to check if route is active
 */
export function isActiveRoute(currentPath: string, routePath: string): boolean {
  if (routePath === ROUTES.HOME) {
    return currentPath === routePath;
  }
  return currentPath.startsWith(routePath);
}

/**
 * Route metadata for navigation
 */
export interface RouteMetadata {
  path: string;
  title: string;
  description?: string;
  icon?: string;
  requiresAuth?: boolean;
}

export const ROUTE_METADATA: Record<string, RouteMetadata> = {
  HOME: {
    path: ROUTES.HOME,
    title: 'Welcome',
    description: 'Getting started with Aura Video Studio',
  },
  DASHBOARD: {
    path: ROUTES.DASHBOARD,
    title: 'Dashboard',
    description: 'Overview of your projects and recent activity',
  },
  IDEATION: {
    path: ROUTES.IDEATION,
    title: 'Ideation',
    description: 'AI-powered brainstorming and concept generation',
  },
  TRENDING: {
    path: ROUTES.TRENDING,
    title: 'Trending Topics',
    description: 'Explore trending topics and content ideas',
  },
  CREATE: {
    path: ROUTES.CREATE,
    title: 'Create',
    description: 'Create a new video project',
  },
  EDITOR: {
    path: ROUTES.EDITOR,
    title: 'Timeline Editor',
    description: 'Edit your video timeline',
  },
  PROJECTS: {
    path: ROUTES.PROJECTS,
    title: 'Projects',
    description: 'Manage your video projects',
  },
  ASSETS: {
    path: ROUTES.ASSETS,
    title: 'Asset Library',
    description: 'Manage media assets and stock content',
  },
  RAG: {
    path: ROUTES.RAG,
    title: 'Document Management',
    description: 'Manage RAG documents and knowledge base',
  },
  SETTINGS: {
    path: ROUTES.SETTINGS,
    title: 'Settings',
    description: 'Configure application settings',
  },
  LOGS: {
    path: ROUTES.LOGS,
    title: 'Logs',
    description: 'View application logs',
  },
  HEALTH: {
    path: ROUTES.HEALTH,
    title: 'System Health',
    description: 'View system health diagnostics',
  },
  RENDER: {
    path: ROUTES.RENDER,
    title: 'Render',
    description: 'Render and export videos',
  },
};

/**
 * Validates that a given route exists in the application's route configuration
 * Used at runtime to ensure menu navigation targets are valid
 */
export function validateRoute(route: string): boolean {
  const allRoutes = Object.values(ROUTES);
  // Check exact match
  if (allRoutes.includes(route)) {
    return true;
  }

  // Check if route matches a base path (for parameterized routes like /editor/:jobId)
  return allRoutes.some((r) => {
    // Remove parameter placeholders (e.g., :jobId) for comparison
    const basePath = r.replace(/\/:[^/]+$/, '');
    return route === basePath || r === route;
  });
}

/**
 * Gets all registered routes for validation
 */
export function getAllRoutes(): string[] {
  return Object.values(ROUTES);
}
