import { ReactNode } from 'react';
import {
  makeStyles,
  tokens,
  Title3,
  Button,
} from '@fluentui/react-components';
import { useNavigate, useLocation } from 'react-router-dom';
import { navItems } from '../navigation';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'row',
    height: '100vh',
  },
  sidebar: {
    width: '200px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingVerticalM,
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
    paddingLeft: tokens.spacingHorizontalM,
  },
  nav: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  navButton: {
    justifyContent: 'flex-start',
    width: '100%',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalXXL,
  },
});

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const location = useLocation();

  return (
    <div className={styles.container}>
      <nav className={styles.sidebar}>
        <div className={styles.header}>
          <Title3>Aura Studio</Title3>
        </div>
        <div className={styles.nav}>
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            return (
              <Button
                key={item.key}
                appearance={isActive ? 'primary' : 'subtle'}
                className={styles.navButton}
                icon={<Icon />}
                onClick={() => navigate(item.path)}
              >
                {item.name}
              </Button>
            );
          })}
        </div>
      </nav>
      <main className={styles.content}>{children}</main>
    </div>
  );
}
