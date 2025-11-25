import { makeStyles, tokens } from '@fluentui/react-components';
import React, { ReactNode, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { useTheme } from '../App';
import { useSwipeGesture } from '../hooks/useSwipeGesture';
import { pageLayout, spacing, gaps } from '../themes/layout';
import { SkipLinks } from './Accessibility/SkipLinks';
import { Breadcrumbs } from './Breadcrumbs';
import { NotificationCenter } from './dashboard/NotificationCenter';
import { ErrorBoundary } from './ErrorBoundary';
import { MobileBottomNav } from './MobileBottomNav';
import { MobileFAB } from './MobileFAB';
import { ResultsTray } from './ResultsTray';
import { Sidebar } from './Sidebar';
import { SystemStatusIndicator } from './SystemStatus';
import { UndoRedoButtons } from './UndoRedo/UndoRedoButtons';

/**
 * Standard toolbar height for consistent vertical rhythm.
 */
const TOOLBAR_HEIGHT = '48px';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'row',
    height: '100vh',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    position: 'relative',
  },
  mainContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    minWidth: 0, // Prevents flex item from overflowing
  },
  topBar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    height: TOOLBAR_HEIGHT,
    minHeight: TOOLBAR_HEIGHT,
    paddingLeft: spacing.xl,
    paddingRight: spacing.xl,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.06)',
    flexShrink: 0,
    '@media (max-width: 768px)': {
      paddingLeft: spacing.lg,
      paddingRight: spacing.lg,
    },
  },
  topBarActions: {
    display: 'flex',
    alignItems: 'center',
    gap: gaps.standard,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    minHeight: 0, // Critical: Allows flex child to shrink and enable scrolling
    padding: pageLayout.pagePadding,
    backgroundColor: tokens.colorNeutralBackground1,
    '@media (max-width: 768px)': {
      padding: pageLayout.pagePaddingMobile,
      paddingBottom: '80px', // Space for mobile nav
    },
  },
  /** Inner content wrapper for max-width constraint */
  contentInner: {
    maxWidth: pageLayout.maxContentWidth,
    marginLeft: 'auto',
    marginRight: 'auto',
    width: '100%',
  },
  sidebarOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    zIndex: 999,
    display: 'none',
    '@media (max-width: 768px)': {
      display: 'block',
    },
  },
  sidebarMobileOpen: {
    '@media (max-width: 768px)': {
      position: 'fixed',
      left: 0,
      top: 0,
      bottom: 0,
      zIndex: 1000,
    },
  },
});

interface LayoutProps {
  children?: ReactNode;
  showBreadcrumbs?: boolean;
  statusBadge?: {
    text: string;
    appearance?: 'filled' | 'outline' | 'tint' | 'ghost';
    color?:
      | 'brand'
      | 'danger'
      | 'important'
      | 'informative'
      | 'severe'
      | 'subtle'
      | 'success'
      | 'warning';
  };
}

export function Layout({ children, showBreadcrumbs = true, statusBadge }: LayoutProps) {
  const styles = useStyles();
  const { isDarkMode, toggleTheme } = useTheme();
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);

  // Swipe gesture support
  const swipeRef = useSwipeGesture({
    onSwipeRight: () => {
      if (window.innerWidth <= 768) {
        setIsMobileSidebarOpen(true);
      }
    },
    onSwipeLeft: () => {
      if (window.innerWidth <= 768) {
        setIsMobileSidebarOpen(false);
      }
    },
  });

  return (
    <div className={styles.container} ref={swipeRef as React.RefObject<HTMLDivElement>}>
      {/* Skip links for accessibility */}
      <SkipLinks />

      {/* Mobile sidebar overlay */}
      {isMobileSidebarOpen && (
        <div
          className={styles.sidebarOverlay}
          onClick={() => setIsMobileSidebarOpen(false)}
          onKeyDown={(e) => {
            if (e.key === 'Escape') {
              setIsMobileSidebarOpen(false);
            }
          }}
          role="button"
          tabIndex={0}
          aria-label="Close sidebar overlay"
        />
      )}

      {/* Sidebar navigation */}
      <nav
        id="main-navigation"
        aria-label="Main navigation"
        className={isMobileSidebarOpen ? styles.sidebarMobileOpen : undefined}
      >
        <Sidebar
          isDarkMode={isDarkMode}
          onToggleTheme={toggleTheme}
          isMobileOpen={isMobileSidebarOpen}
          onMobileClose={() => setIsMobileSidebarOpen(false)}
        />
      </nav>

      <div className={styles.mainContainer}>
        {showBreadcrumbs && <Breadcrumbs statusBadge={statusBadge} />}
        <div className={styles.topBar} role="banner" aria-label="Top bar">
          <UndoRedoButtons />
          <div className={styles.topBarActions}>
            <SystemStatusIndicator />
            <NotificationCenter />
            <ResultsTray />
          </div>
        </div>
        <main id="main-content" className={styles.content} tabIndex={-1} aria-label="Main content">
          <div className={styles.contentInner}>
            <ErrorBoundary>{children || <Outlet />}</ErrorBoundary>
          </div>
        </main>
      </div>

      {/* Mobile-specific components */}
      <MobileBottomNav />
      <MobileFAB />
    </div>
  );
}
