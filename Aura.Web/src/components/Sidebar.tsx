import {
  makeStyles,
  tokens,
  Button,
  Tooltip,
  mergeClasses,
  Title3,
} from '@fluentui/react-components';
import {
  Home24Regular,
  VideoClip24Regular,
  Folder24Regular,
  Book24Regular,
  Settings24Regular,
  PanelLeft24Regular,
  PanelLeftContract24Regular,
  WeatherMoon24Regular,
  WeatherSunny24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

const useStyles = makeStyles({
  sidebar: {
    width: '240px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingVerticalM,
    boxShadow: '2px 0 8px rgba(0, 0, 0, 0.1)',
    transition: 'width 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    overflow: 'hidden',
    '@media (max-width: 768px)': {
      width: '64px',
      padding: tokens.spacingVerticalS,
    },
  },
  sidebarCollapsed: {
    width: '64px',
    padding: tokens.spacingVerticalS,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    alignItems: 'flex-start',
  },
  headerCollapsed: {
    paddingLeft: '0',
    alignItems: 'center',
  },
  nav: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
  },
  navButton: {
    justifyContent: 'flex-start',
    width: '100%',
    paddingTop: tokens.spacingVerticalM,
    paddingBottom: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      transform: 'translateX(4px)',
      boxShadow: tokens.shadow8,
    },
    ':active': {
      transform: 'translateX(2px)',
    },
  },
  navButtonCollapsed: {
    justifyContent: 'center',
    paddingLeft: '0',
    paddingRight: '0',
    ':hover': {
      transform: 'scale(1.1)',
    },
    ':active': {
      transform: 'scale(1.05)',
    },
  },
  footer: {
    marginTop: 'auto',
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  themeToggle: {
    width: '100%',
    justifyContent: 'center',
    borderRadius: tokens.borderRadiusMedium,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      transform: 'scale(1.05)',
    },
  },
  toggleButton: {
    width: '100%',
    marginBottom: tokens.spacingVerticalS,
  },
});

interface NavItem {
  key: string;
  name: string;
  icon: React.ComponentType;
  path: string;
  tooltip?: string;
}

interface SidebarProps {
  isDarkMode: boolean;
  onToggleTheme: () => void;
}

export function Sidebar({ isDarkMode, onToggleTheme }: SidebarProps) {
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

  const coreNavItems: NavItem[] = [
    {
      key: 'home',
      name: 'Home',
      icon: Home24Regular,
      path: '/',
      tooltip: 'Dashboard - View your projects and recent activity',
    },
    {
      key: 'create',
      name: 'Create',
      icon: VideoClip24Regular,
      path: '/create',
      tooltip: 'Video Studio - Create and edit videos',
    },
    {
      key: 'library',
      name: 'Library',
      icon: Folder24Regular,
      path: '/projects',
      tooltip: 'Projects & Assets - Browse your library',
    },
    {
      key: 'learn',
      name: 'Learn',
      icon: Book24Regular,
      path: '/learning',
      tooltip: 'Tutorials & Docs - Get help and learn',
    },
    {
      key: 'settings',
      name: 'Settings',
      icon: Settings24Regular,
      path: '/settings',
      tooltip: 'Settings - Configure your workspace',
    },
  ];

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
        {!isCollapsed && <Title3>🎬 Aura Studio</Title3>}
      </div>
      <div className={styles.nav}>
        {coreNavItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;
          return (
            <Tooltip key={item.key} content={item.tooltip || item.name} relationship="label">
              <Button
                appearance={isActive ? 'primary' : 'subtle'}
                className={mergeClasses(styles.navButton, isCollapsed && styles.navButtonCollapsed)}
                icon={<Icon />}
                onClick={() => navigate(item.path)}
              >
                {!isCollapsed && item.name}
              </Button>
            </Tooltip>
          );
        })}
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
