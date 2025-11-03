import { useState, useEffect } from 'react';
import { settingsService } from '../services/settingsService';

/**
 * Hook to access and manage Advanced Mode state
 * @returns Current advanced mode state and setter function
 */
export function useAdvancedMode(): [boolean, (enabled: boolean) => Promise<void>] {
  const [advancedMode, setAdvancedModeState] = useState(false);

  useEffect(() => {
    const loadAdvancedMode = async () => {
      try {
        const settings = await settingsService.loadSettings();
        setAdvancedModeState(settings.general.advancedModeEnabled);
      } catch (error) {
        console.error('Failed to load advanced mode setting:', error);
        setAdvancedModeState(false);
      }
    };

    loadAdvancedMode();
  }, []);

  const setAdvancedMode = async (enabled: boolean) => {
    try {
      const settings = await settingsService.loadSettings();
      settings.general.advancedModeEnabled = enabled;
      await settingsService.saveSettings(settings);
      setAdvancedModeState(enabled);
    } catch (error) {
      console.error('Failed to save advanced mode setting:', error);
      throw error;
    }
  };

  return [advancedMode, setAdvancedMode];
}
