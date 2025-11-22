/**
 * StorageManager - Manages browser storage and application reset
 * 
 * Provides utilities to clear all browser storage (localStorage, sessionStorage,
 * IndexedDB, cookies, cache) and trigger full application resets in Electron.
 */

export class StorageManager {
  private static readonly STORAGE_PREFIX = 'aura_';
  
  /**
   * Clear all Aura-related storage
   */
  static clearAll(): void {
    console.log('[StorageManager] Clearing all storage...');
    
    // Clear localStorage
    const localStorageKeys = Object.keys(localStorage);
    localStorageKeys.forEach(key => {
      if (key.startsWith(this.STORAGE_PREFIX) || key.includes('aura')) {
        localStorage.removeItem(key);
      }
    });
    
    // Clear sessionStorage
    const sessionStorageKeys = Object.keys(sessionStorage);
    sessionStorageKeys.forEach(key => {
      if (key.startsWith(this.STORAGE_PREFIX) || key.includes('aura')) {
        sessionStorage.removeItem(key);
      }
    });
    
    // Clear IndexedDB
    this.clearIndexedDB();
    
    // Clear cookies
    this.clearCookies();
    
    // Clear cache storage
    this.clearCacheStorage();
    
    console.log('[StorageManager] Storage cleared');
  }
  
  /**
   * Clear IndexedDB databases
   */
  private static async clearIndexedDB(): Promise<void> {
    if ('indexedDB' in window) {
      try {
        const databases = await indexedDB.databases();
        for (const db of databases) {
          if (db.name && (db.name.startsWith(this.STORAGE_PREFIX) || db.name.includes('aura'))) {
            indexedDB.deleteDatabase(db.name);
            console.log(`[StorageManager] Deleted IndexedDB: ${db.name}`);
          }
        }
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        console.error('[StorageManager] Error clearing IndexedDB:', errorObj.message);
      }
    }
  }
  
  /**
   * Clear all cookies
   */
  private static clearCookies(): void {
    document.cookie.split(';').forEach(cookie => {
      const eqPos = cookie.indexOf('=');
      const name = eqPos > -1 ? cookie.substr(0, eqPos).trim() : cookie.trim();
      if (name.startsWith(this.STORAGE_PREFIX) || name.includes('aura')) {
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=${window.location.hostname}`;
      }
    });
  }
  
  /**
   * Clear cache storage
   */
  private static async clearCacheStorage(): Promise<void> {
    if ('caches' in window) {
      try {
        const cacheNames = await caches.keys();
        for (const cacheName of cacheNames) {
          if (cacheName.startsWith(this.STORAGE_PREFIX) || cacheName.includes('aura')) {
            await caches.delete(cacheName);
            console.log(`[StorageManager] Deleted cache: ${cacheName}`);
          }
        }
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        console.error('[StorageManager] Error clearing cache storage:', errorObj.message);
      }
    }
  }
  
  /**
   * Check if running in Electron
   */
  private static isElectron(): boolean {
    return !!(window as Window & { electron?: unknown }).electron;
  }
  
  /**
   * Request full application reset (Electron only)
   */
  static async resetApplication(): Promise<void> {
    if (this.isElectron()) {
      console.log('[StorageManager] Requesting application reset...');
      const electronWindow = window as Window & { 
        electron?: { 
          ipcRenderer?: { 
            invoke: (channel: string, ...args: unknown[]) => Promise<unknown> 
          } 
        } 
      };
      if (electronWindow.electron?.ipcRenderer) {
        await electronWindow.electron.ipcRenderer.invoke('reset-application');
      }
    } else {
      console.log('[StorageManager] Not in Electron, performing web-only reset...');
      this.clearAll();
      window.location.reload();
    }
  }
}
