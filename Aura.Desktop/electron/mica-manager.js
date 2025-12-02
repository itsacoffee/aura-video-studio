/**
 * Mica Material Manager
 * Enables Windows 11 Mica/Acrylic effects on the main window
 */

const { nativeTheme, systemPreferences } = require('electron');
const os = require('os');

class MicaManager {
  constructor(logger) {
    this.logger = logger;
    this.isSupported = this._checkSupport();
    this.currentEffect = 'none';
    this._themeChangeListeners = [];
  }

  /**
   * Check if Windows 11 Mica is supported
   */
  _checkSupport() {
    if (process.platform !== 'win32') {
      this.logger?.info('[Mica] Not on Windows, Mica not supported');
      return false;
    }

    const release = os.release();
    const [major, , build] = release.split('.').map(Number);

    // Windows 11 is 10.0.22000 or higher
    const isWindows11 = major >= 10 && build >= 22000;

    this.logger?.info('[Mica] Windows version check', {
      major,
      build,
      isWindows11,
    });

    return isWindows11;
  }

  /**
   * Apply Mica effect to a BrowserWindow
   * @param {BrowserWindow} window - The window to apply effect to
   * @param {string} effect - 'mica' | 'acrylic' | 'tabbed' | 'none'
   */
  applyEffect(window, effect = 'mica') {
    if (!this.isSupported) {
      this.logger?.warn('[Mica] Effect not applied - not supported on this system');
      return false;
    }

    if (!window || window.isDestroyed()) {
      this.logger?.warn('[Mica] Invalid window');
      return false;
    }

    try {
      // Use Electron's setBackgroundMaterial (Electron 22+)
      if (typeof window.setBackgroundMaterial === 'function') {
        switch (effect) {
          case 'mica':
            window.setBackgroundMaterial('mica');
            break;
          case 'acrylic':
            window.setBackgroundMaterial('acrylic');
            break;
          case 'tabbed':
            window.setBackgroundMaterial('tabbed');
            break;
          case 'none':
          default:
            window.setBackgroundMaterial('none');
            break;
        }

        this.currentEffect = effect;
        this.logger?.info('[Mica] Applied effect', { effect });
        return true;
      } else {
        this.logger?.warn('[Mica] setBackgroundMaterial not available');
        return false;
      }
    } catch (error) {
      this.logger?.error('[Mica] Failed to apply effect', { error: error.message });
      return false;
    }
  }

  /**
   * Get current effect
   */
  getCurrentEffect() {
    return this.currentEffect;
  }

  /**
   * Check if dark mode is enabled
   */
  isDarkMode() {
    return nativeTheme.shouldUseDarkColors;
  }

  /**
   * Listen for theme changes
   * @param {function} callback - Function to call when theme changes
   * @returns {function} Cleanup function to remove the listener
   */
  onThemeChange(callback) {
    const listener = () => {
      callback(this.isDarkMode());
    };
    nativeTheme.on('updated', listener);
    this._themeChangeListeners.push(listener);

    // Return cleanup function
    return () => {
      nativeTheme.removeListener('updated', listener);
      const index = this._themeChangeListeners.indexOf(listener);
      if (index > -1) {
        this._themeChangeListeners.splice(index, 1);
      }
    };
  }

  /**
   * Remove all theme change listeners (for cleanup)
   */
  removeAllListeners() {
    this._themeChangeListeners.forEach((listener) => {
      nativeTheme.removeListener('updated', listener);
    });
    this._themeChangeListeners = [];
  }

  /**
   * Get system accent color
   */
  getAccentColor() {
    if (process.platform === 'win32') {
      try {
        return systemPreferences.getAccentColor();
      } catch {
        return null;
      }
    }
    return null;
  }
}

module.exports = { MicaManager };
