import { makeStyles, tokens } from '@fluentui/react-components';
import React, { ReactNode, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { useTheme } from '../App';
import { useSwipeGesture } from '../hooks/useSwipeGesture';
import { gaps, pageLayout, spacing } from '../themes/layout';
import { SkipLinks } from './Accessibility/SkipLinks';
import { Breadcrumbs } from './Breadcrumbs';
import { NotificationCenter } from './dashboard/NotificationCenter';
import { ErrorBoundary } from './ErrorBoundary';
import { ProviderHealthIndicator } from './Header';
import { GlobalLlmSelector } from './LLMMenu/GlobalLlmSelector';
import { ZoomControls } from './ZoomControls';
import { MobileBottomNav } from './MobileBottomNav';
import { MobileFAB } from './MobileFAB';
import { ResultsTray } from './ResultsTray';
import { Sidebar } from './Sidebar';
import { UndoRedoButtons } from './UndoRedo/UndoRedoButtons';

/**
 * Standard toolbar height meeting Apple HIG touch target minimum (44pt).
 * Provides comfortable interaction and visual hierarchy.
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
    minWidth: 0,
  },
  topBar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    height: TOOLBAR_HEIGHT,
    minHeight: TOOLBAR_HEIGHT,
    paddingLeft: pageLayout.pagePadding,
    paddingRight: pageLayout.pagePadding,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: '0 1px 2px rgba(0, 0, 0, 0.04)',
    flexShrink: 0,
    '@media (max-width: 768px)': {
      paddingLeft: spacing.md,
      paddingRight: spacing.md,
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
    minHeight: 0,
    padding: pageLayout.pagePadding,
    backgroundColor: tokens.colorNeutralBackground1,
    '@media (max-width: 768px)': {
      padding: pageLayout.pagePaddingMobile,
      paddingBottom: '64px',
    },
  },
  /** Content area without padding for full-bleed editors */
  contentFullBleedWrapper: {
    flex: 1,
    overflow: 'hidden',
    minHeight: 0,
    minWidth: 0,
    padding: 0,
    backgroundColor: tokens.colorNeutralBackground1,
    display: 'flex',
    flexDirection: 'column',
  },
  /** Inner content wrapper for max-width constraint */
  contentInner: {
    maxWidth: pageLayout.maxContentWidth,
    marginLeft: 'auto',
    marginRight: 'auto',
    width: '100%',
  },
  /** Full bleed content - fills all available space without max-width constraint */
  contentFullBleed: {
    width: '100%',
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    minHeight: 0,
    minWidth: 0,
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
  /** When true, content fills available space without max-width constraint (for full-screen editors like OpenCut) */
  fullBleed?: boolean;
}

export function Layout({
  children,
  showBreadcrumbs = true,
  statusBadge,
  fullBleed = false,
}: LayoutProps) {
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
        {showBreadcrumbs && !fullBleed && <Breadcrumbs statusBadge={statusBadge} />}
        {!fullBleed && (
          <div className={styles.topBar} role="banner" aria-label="Top bar">
            <UndoRedoButtons />
            <div className={styles.topBarActions}>
              <ZoomControls />
              <ProviderHealthIndicator />
              <GlobalLlmSelector />
              <NotificationCenter />
              <ResultsTray />
            </div>
          </div>
        )}
        <main
          id="main-content"
          className={fullBleed ? styles.contentFullBleedWrapper : styles.content}
          tabIndex={-1}
          aria-label="Main content"
        >
          <div className={fullBleed ? styles.contentFullBleed : styles.contentInner}>
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

/**
 * FullBleedLayout - A layout variant without max-width constraints for full-screen editors
 * Used for pages like OpenCut that need to fill all available space
 */
export function FullBleedLayout({ children }: { children?: ReactNode }) {
  return <Layout fullBleed>{children}</Layout>;
}
