/**
 * Navigation Context
 * Provides navigation service throughout the application
 */

import { createContext, useContext, useEffect, type FC, type ReactNode } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { navigationService } from '../services/navigationService';

interface NavigationContextValue {
  push: typeof navigationService.push;
  replace: typeof navigationService.replace;
  goBack: typeof navigationService.goBack;
  goForward: typeof navigationService.goForward;
  getCurrentPath: typeof navigationService.getCurrentPath;
  getCurrentRouteMeta: typeof navigationService.getCurrentRouteMeta;
  getRouteMeta: typeof navigationService.getRouteMeta;
}

const NavigationContext = createContext<NavigationContextValue | null>(null);

interface NavigationProviderProps {
  children: ReactNode;
}

/**
 * Navigation Provider
 * Must be rendered inside Router context
 */
export const NavigationProvider: FC<NavigationProviderProps> = ({ children }) => {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    navigationService.setNavigate(navigate);
  }, [navigate]);

  useEffect(() => {
    navigationService.updateCurrentPath(location.pathname);
  }, [location.pathname]);

  const value: NavigationContextValue = {
    push: navigationService.push.bind(navigationService),
    replace: navigationService.replace.bind(navigationService),
    goBack: navigationService.goBack.bind(navigationService),
    goForward: navigationService.goForward.bind(navigationService),
    getCurrentPath: navigationService.getCurrentPath.bind(navigationService),
    getCurrentRouteMeta: navigationService.getCurrentRouteMeta.bind(navigationService),
    getRouteMeta: navigationService.getRouteMeta.bind(navigationService),
  };

  return <NavigationContext.Provider value={value}>{children}</NavigationContext.Provider>;
};

/**
 * Hook to access navigation service
 */
export function useNavigation(): NavigationContextValue {
  const context = useContext(NavigationContext);
  if (!context) {
    throw new Error('useNavigation must be used within NavigationProvider');
  }
  return context;
}
