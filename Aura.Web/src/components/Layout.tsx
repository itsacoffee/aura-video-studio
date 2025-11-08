import { makeStyles, tokens } from '@fluentui/react-components';
import { ReactNode } from 'react';
import { useTheme } from '../App';
import { ResultsTray } from './ResultsTray';
import { Sidebar } from './Sidebar';
import { UndoRedoButtons } from './UndoRedo/UndoRedoButtons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'row',
    height: '100vh',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
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
});

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const styles = useStyles();
  const { isDarkMode, toggleTheme } = useTheme();

  return (
    <div className={styles.container}>
      <Sidebar isDarkMode={isDarkMode} onToggleTheme={toggleTheme} />
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
