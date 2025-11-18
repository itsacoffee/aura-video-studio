/**
 * Navigation utilities that work in both web and Electron environments
 * 
 * In Electron, we use hash-based routing with file:// protocol
 * In web, we can use standard navigation
 */

/**
 * Navigate to a route in a way that works in both Electron and web
 * @param path - The path to navigate to (e.g., '/setup', '/dashboard')
 */
export function navigateToRoute(path: string): void {
  // Check if running in Electron
  const isElectron = typeof window !== 'undefined' && (window.aura || window.electron);
  
  if (isElectron) {
    // In Electron, use hash-based routing
    const hashPath = path.startsWith('#') ? path : `#${path}`;
    window.location.hash = hashPath;
  } else {
    // In web, use standard navigation
    // This will work with React Router's BrowserRouter
    window.location.href = path;
  }
}

/**
 * Reload the current page
 */
export function reloadPage(): void {
  window.location.reload();
}

/**
 * Navigate to external URL (opens in default browser in Electron)
 * @param url - Full URL to navigate to
 */
export async function navigateToExternalUrl(url: string): Promise<void> {
  const electron = typeof window !== 'undefined' ? window.aura || window.electron : null;
  
  if (electron?.shell?.openExternal) {
    // In Electron, open in default browser
    await electron.shell.openExternal(url);
  } else {
    // In web, open in new tab
    window.open(url, '_blank', 'noopener,noreferrer');
  }
}
