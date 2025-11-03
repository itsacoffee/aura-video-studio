import { makeStyles, tokens, Title3, Button, Tooltip } from '@fluentui/react-components';
import { WeatherMoon24Regular, WeatherSunny24Regular } from '@fluentui/react-icons';
import { ReactNode } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useTheme } from '../App';
import { navItems } from '../navigation';
import { ResultsTray } from './ResultsTray';
import { UndoRedoButtons } from './UndoRedo/UndoRedoButtons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'row',
    height: '100vh',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  sidebar: {
    width: '240px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingVerticalM,
    boxShadow: '2px 0 8px rgba(0, 0, 0, 0.1)',
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
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
  mainContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  topBar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.06)',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalXXL,
    backgroundColor: tokens.colorNeutralBackground1,
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
  footer: {
    marginTop: 'auto',
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
});

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const location = useLocation();
  const { isDarkMode, toggleTheme } = useTheme();

  return (
    <div className={styles.container}>
      <nav className={styles.sidebar}>
        <div className={styles.header}>
          <Title3>🎬 Aura Studio</Title3>
        </div>
        <div className={styles.nav}>
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            return (
              <Tooltip key={item.key} content={item.name} relationship="label">
                <Button
                  appearance={isActive ? 'primary' : 'subtle'}
                  className={styles.navButton}
                  icon={<Icon />}
                  onClick={() => navigate(item.path)}
                >
                  {item.name}
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
              onClick={toggleTheme}
            >
              {isDarkMode ? 'Light Mode' : 'Dark Mode'}
            </Button>
          </Tooltip>
        </div>
      </nav>
      <div className={styles.mainContainer}>
        <div className={styles.topBar}>
          <UndoRedoButtons />
          <ResultsTray />
        </div>
        <main className={styles.content}>{children}</main>
      </div>
    </div>
  );
}
