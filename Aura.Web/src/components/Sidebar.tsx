import {
  makeStyles,
  tokens,
  Button,
  Tooltip,
  mergeClasses,
  Title3,
  Divider,
  Text,
} from '@fluentui/react-components';
import {
  PanelLeft24Regular,
  PanelLeftContract24Regular,
  WeatherMoon24Regular,
  WeatherSunny24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { navItems, type NavItem } from '../navigation';
import { panelLayout, spacing, gaps } from '../themes/layout';
import { Logo } from './Logo';

const useStyles = makeStyles({
  sidebar: {
    width: panelLayout.sidebarWidth,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    padding: spacing.md,
    gap: spacing.sm,
    boxShadow: '1px 0 4px rgba(0, 0, 0, 0.08)',
    transition: 'width 0.25s cubic-bezier(0.4, 0, 0.2, 1)',
    overflow: 'hidden',
    flexShrink: 0,
    '@media (max-width: 768px)': {
      width: panelLayout.sidebarWidthCollapsed,
      padding: spacing.xs,
    },
  },
  sidebarCollapsed: {
    width: panelLayout.sidebarWidthCollapsed,
    padding: spacing.xs,
  },
  header: {
    marginBottom: spacing.sm,
    paddingLeft: spacing.sm,
    display: 'flex',
    flexDirection: 'column',
    gap: spacing.xs,
    alignItems: 'flex-start',
  },
  headerCollapsed: {
    paddingLeft: '0',
    alignItems: 'center',
  },
  nav: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    flex: 1,
    overflowY: 'auto',
    overflowX: 'hidden',
    paddingRight: spacing.xs,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1px',
    marginBottom: spacing.sm,
  },
  sectionLabel: {
    paddingLeft: spacing.sm,
    paddingTop: spacing.xs,
    paddingBottom: '2px',
    fontSize: tokens.fontSizeBase100,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase',
    letterSpacing: '0.04em',
  },
  sectionLabelCollapsed: {
    paddingLeft: '0',
    textAlign: 'center',
  },
  navButton: {
    justifyContent: 'flex-start',
    width: '100%',
    paddingTop: spacing.xs,
    paddingBottom: spacing.xs,
    minHeight: '28px',
    fontSize: tokens.fontSizeBase200,
    borderRadius: tokens.borderRadiusSmall,
    transition: 'all 0.15s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      transform: 'translateX(2px)',
      boxShadow: tokens.shadow4,
    },
    ':active': {
      transform: 'translateX(1px)',
    },
  },
  navButtonCollapsed: {
    justifyContent: 'center',
    paddingLeft: '0',
    paddingRight: '0',
    ':hover': {
      transform: 'scale(1.05)',
    },
    ':active': {
      transform: 'scale(1.02)',
    },
  },
  footer: {
    marginTop: 'auto',
    paddingTop: spacing.sm,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  themeToggle: {
    width: '100%',
    justifyContent: 'center',
    fontSize: tokens.fontSizeBase200,
    borderRadius: tokens.borderRadiusSmall,
    transition: 'all 0.15s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      transform: 'scale(1.02)',
    },
  },
  toggleButton: {
    width: '100%',
    marginBottom: spacing.xs,
  },
  divider: {
    marginTop: '2px',
    marginBottom: '2px',
  },
  brandContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: gaps.tight,
  },
  brandLogo: {
    flexShrink: 0,
  },
});

interface SidebarProps {
  isDarkMode: boolean;
  onToggleTheme: () => void;
  isMobileOpen?: boolean;
  onMobileClose?: () => void;
}

export function Sidebar({
  isDarkMode,
  onToggleTheme,
  isMobileOpen: _isMobileOpen,
  onMobileClose: _onMobileClose,
}: SidebarProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const location = useLocation();
  const [isCollapsed, setIsCollapsed] = useState(() => {
    try {
      const stored = localStorage.getItem('aura-sidebar-collapsed');
      return stored ? JSON.parse(stored) : false;
    } catch {
      return false;
    }
  });

  // Auto-collapse on mobile screens
  useEffect(() => {
    const handleResize = () => {
      const isMobile = window.innerWidth <= 768;
      if (isMobile && !isCollapsed) {
        setIsCollapsed(true);
      }
    };

    handleResize();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [isCollapsed]);

  // Persist sidebar collapsed state
  useEffect(() => {
    try {
      localStorage.setItem('aura-sidebar-collapsed', JSON.stringify(isCollapsed));
    } catch {
      // Ignore localStorage errors
    }
  }, [isCollapsed]);

  // Organize navigation items into logical sections
  const homeItems = navItems.filter((item) => ['home', 'dashboard'].includes(item.key));

  const creationItems = navItems.filter((item) =>
    ['ideation', 'trending', 'create', 'templates', 'localization'].includes(item.key)
  );

  const editingItems = navItems.filter((item) =>
    ['editor', 'projects', 'assets', 'pacing', 'render'].includes(item.key)
  );

  const optimizationItems = navItems.filter((item) =>
    ['platform', 'quality', 'ai-editing', 'aesthetics'].includes(item.key)
  );

  const managementItems = navItems.filter((item) =>
    [
      'jobs',
      'downloads',
      'health',
      'models',
      'prompt-management',
      'rag',
      'voice-enhancement',
      'performance-analytics',
      'ml-lab',
      'quality-validation',
      'validation',
      'verification',
    ].includes(item.key)
  );

  const systemItems = navItems.filter((item) =>
    ['diagnostics', 'logs', 'settings'].includes(item.key)
  );

  const renderNavItems = (items: NavItem[], showDivider = false) => (
    <>
      {items.map((item) => {
        const Icon = item.icon;
        const isActive = location.pathname === item.path;
        const tooltipContent = item.advancedOnly ? `${item.name} (Advanced Mode)` : item.name;

        return (
          <Tooltip key={item.key} content={tooltipContent} relationship="label">
            <Button
              appearance={isActive ? 'primary' : 'subtle'}
              className={mergeClasses(styles.navButton, isCollapsed && styles.navButtonCollapsed)}
              icon={<Icon />}
              onClick={() => navigate(item.path)}
              aria-label={item.name}
            >
              {!isCollapsed && item.name}
            </Button>
          </Tooltip>
        );
      })}
      {showDivider && items.length > 0 && <Divider className={styles.divider} />}
    </>
  );

  const toggleSidebar = () => {
    setIsCollapsed(!isCollapsed);
  };

  return (
    <nav className={mergeClasses(styles.sidebar, isCollapsed && styles.sidebarCollapsed)}>
      <div className={mergeClasses(styles.header, isCollapsed && styles.headerCollapsed)}>
        <Tooltip content={isCollapsed ? 'Expand Sidebar' : 'Collapse Sidebar'} relationship="label">
          <Button
            appearance="subtle"
            className={styles.toggleButton}
            icon={isCollapsed ? <PanelLeft24Regular /> : <PanelLeftContract24Regular />}
            onClick={toggleSidebar}
            aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          />
        </Tooltip>
        {!isCollapsed && (
          <div className={styles.brandContainer}>
            <Logo size={32} className={styles.brandLogo} />
            <Title3>Aura Studio</Title3>
          </div>
        )}
        {isCollapsed && (
          <Tooltip content="Aura Studio" relationship="label">
            <Logo size={32} className={styles.brandLogo} />
          </Tooltip>
        )}
      </div>
      <div className={styles.nav}>
        {/* Home Section */}
        {renderNavItems(homeItems, true)}

        {/* Creation Tools */}
        {!isCollapsed && creationItems.length > 0 && (
          <Text className={styles.sectionLabel}>Creation</Text>
        )}
        {renderNavItems(creationItems, true)}

        {/* Editing Tools */}
        {!isCollapsed && editingItems.length > 0 && (
          <Text className={styles.sectionLabel}>Editing</Text>
        )}
        {renderNavItems(editingItems, true)}

        {/* Optimization Tools */}
        {!isCollapsed && optimizationItems.length > 0 && (
          <Text className={styles.sectionLabel}>Optimization</Text>
        )}
        {renderNavItems(optimizationItems, true)}

        {/* Management & Tools */}
        {!isCollapsed && managementItems.length > 0 && (
          <Text className={styles.sectionLabel}>Management</Text>
        )}
        {renderNavItems(managementItems, true)}

        {/* System */}
        {!isCollapsed && systemItems.length > 0 && (
          <Text className={styles.sectionLabel}>System</Text>
        )}
        {renderNavItems(systemItems, false)}
      </div>
      <div className={styles.footer}>
        <Tooltip
          content={isDarkMode ? 'Switch to Light Mode' : 'Switch to Dark Mode'}
          relationship="label"
        >
          <Button
            appearance="subtle"
            className={styles.themeToggle}
            icon={isDarkMode ? <WeatherSunny24Regular /> : <WeatherMoon24Regular />}
            onClick={onToggleTheme}
          >
            {!isCollapsed && (isDarkMode ? 'Light Mode' : 'Dark Mode')}
          </Button>
        </Tooltip>
      </div>
    </nav>
  );
}
