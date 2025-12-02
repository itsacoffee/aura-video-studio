/**
 * Mica Context Provider
 * Provides Windows 11 Mica state to the application
 */

import React, { createContext, useContext, useEffect, ReactNode } from 'react';
import { useWindowsMica } from '../hooks/useWindowsMica';
import { useGraphics } from './GraphicsContext';

interface MicaContextType {
  isSupported: boolean;
  isEnabled: boolean;
  accentColor: string | null;
  scaleFactor: number;
  isDarkMode: boolean;
}

const MicaContext = createContext<MicaContextType>({
  isSupported: false,
  isEnabled: false,
  accentColor: null,
  scaleFactor: 1,
  isDarkMode: false,
});

interface MicaProviderProps {
  children: ReactNode;
}

export function MicaProvider({ children }: MicaProviderProps): React.JSX.Element {
  const mica = useWindowsMica();
  const { settings } = useGraphics();

  // Sync Mica with graphics settings
  useEffect(() => {
    if (mica.isElectron && mica.isSupported) {
      mica.syncWithGraphicsSettings({
        transparency: settings.effects.transparency,
        blurEffects: settings.effects.blurEffects,
      });
    }
  }, [
    mica.isElectron,
    mica.isSupported,
    mica.syncWithGraphicsSettings,
    settings.effects.transparency,
    settings.effects.blurEffects,
  ]);

  // Apply system accent color as CSS variable
  useEffect(() => {
    if (mica.accentColor) {
      document.documentElement.style.setProperty('--system-accent-color', mica.accentColor);
    }
  }, [mica.accentColor]);

  // Apply mica-enabled class to root
  useEffect(() => {
    const root = document.documentElement;
    const isEnabled = mica.isSupported && settings.effects.transparency;

    if (isEnabled) {
      root.classList.add('mica-enabled');
      root.classList.remove('mica-fallback');
    } else {
      root.classList.remove('mica-enabled');
      root.classList.add('mica-fallback');
    }
  }, [mica.isSupported, settings.effects.transparency]);

  const value: MicaContextType = {
    isSupported: mica.isSupported,
    isEnabled: mica.isSupported && settings.effects.transparency,
    accentColor: mica.accentColor,
    scaleFactor: mica.scaleFactor,
    isDarkMode: mica.isDarkMode,
  };

  return <MicaContext.Provider value={value}>{children}</MicaContext.Provider>;
}

export function useMica(): MicaContextType {
  return useContext(MicaContext);
}
