import { useState, useEffect, createContext, useContext } from 'react';
import { 
  FluentProvider, 
  webLightTheme,
  webDarkTheme,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './components/Layout';
import { WelcomePage } from './pages/WelcomePage';
import { DashboardPage } from './pages/DashboardPage';
import { CreatePage } from './pages/CreatePage';
import { CreateWizard } from './pages/Wizard/CreateWizard';
import { RenderPage } from './pages/RenderPage';
import { PublishPage } from './pages/PublishPage';
import { DownloadsPage } from './pages/DownloadsPage';
import { SettingsPage } from './pages/SettingsPage';
import { KeyboardShortcutsModal } from './components/KeyboardShortcutsModal';

const useStyles = makeStyles({
  root: {
    height: '100vh',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground1,
  },
});

interface ThemeContextType {
  isDarkMode: boolean;
  toggleTheme: () => void;
}

export const ThemeContext = createContext<ThemeContextType>({
  isDarkMode: false,
  toggleTheme: () => {},
});

export const useTheme = () => useContext(ThemeContext);

function App() {
  const styles = useStyles();
  const [isDarkMode, setIsDarkMode] = useState(() => {
    const saved = localStorage.getItem('darkMode');
    return saved ? JSON.parse(saved) : false;
  });
  const [showShortcuts, setShowShortcuts] = useState(false);

  useEffect(() => {
    localStorage.setItem('darkMode', JSON.stringify(isDarkMode));
  }, [isDarkMode]);

  // Global keyboard shortcut handler for Ctrl+K
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setShowShortcuts(true);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const toggleTheme = () => {
    setIsDarkMode(!isDarkMode);
  };

  return (
    <ThemeContext.Provider value={{ isDarkMode, toggleTheme }}>
      <FluentProvider theme={isDarkMode ? webDarkTheme : webLightTheme}>
        <div className={styles.root}>
          <BrowserRouter>
            <Layout>
              <Routes>
                <Route path="/" element={<WelcomePage />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/create" element={<CreateWizard />} />
                <Route path="/create/legacy" element={<CreatePage />} />
                <Route path="/render" element={<RenderPage />} />
                <Route path="/publish" element={<PublishPage />} />
                <Route path="/downloads" element={<DownloadsPage />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </Layout>
          </BrowserRouter>
          <KeyboardShortcutsModal 
            isOpen={showShortcuts} 
            onClose={() => setShowShortcuts(false)} 
          />
        </div>
      </FluentProvider>
    </ThemeContext.Provider>
  );
}

export default App;
