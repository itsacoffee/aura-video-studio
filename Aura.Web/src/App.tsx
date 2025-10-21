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
import { TimelinePage } from './pages/TimelinePage';
import { RenderPage } from './pages/RenderPage';
import { PublishPage } from './pages/PublishPage';
import { DownloadsPage } from './pages/DownloadsPage';
import { SettingsPage } from './pages/SettingsPage';
import { LogViewerPage } from './pages/LogViewerPage';
import { ProjectsPage } from './pages/Projects/ProjectsPage';
import { RecentJobsPage } from './pages/RecentJobsPage';
import { FirstRunWizard } from './pages/Onboarding/FirstRunWizard';
import { SetupWizard } from './pages/Setup/SetupWizard';
import { ProviderHealthDashboard } from './pages/Health/ProviderHealthDashboard';
import { KeyboardShortcutsModal } from './components/KeyboardShortcutsModal';
import { NotificationsToaster, useNotifications } from './components/Notifications/Toasts';
import { JobStatusBar } from './components/StatusBar/JobStatusBar';
import { JobProgressDrawer } from './components/JobProgressDrawer';
import { useJobState } from './state/jobState';

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
  const { toasterId } = useNotifications();
  
  // Job state for status bar
  const { currentJobId, status, progress, message } = useJobState();
  const [showDrawer, setShowDrawer] = useState(false);

  useEffect(() => {
    localStorage.setItem('darkMode', JSON.stringify(isDarkMode));
  }, [isDarkMode]);
  
  // Poll job progress when a job is active
  useEffect(() => {
    if (!currentJobId || status === 'completed' || status === 'failed') {
      return;
    }

    const pollInterval = setInterval(async () => {
      try {
        const response = await fetch(`/api/jobs/${currentJobId}/progress`);
        if (response.ok) {
          const data = await response.json();
          useJobState.getState().updateProgress(data.progress, data.currentStage);
          
          if (data.status === 'completed') {
            useJobState.getState().setStatus('completed');
            useJobState.getState().updateProgress(100, 'Video generation complete!');
          } else if (data.status === 'failed') {
            useJobState.getState().setStatus('failed');
            useJobState.getState().updateProgress(data.progress, 'Generation failed');
          }
        }
      } catch (error) {
        console.error('Error polling job progress:', error);
      }
    }, 1000);

    return () => clearInterval(pollInterval);
  }, [currentJobId, status]);

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
            {/* Status bar for job progress */}
            <JobStatusBar
              status={status}
              progress={progress}
              message={message}
              onViewDetails={() => setShowDrawer(true)}
            />
            <Layout>
              <Routes>
                <Route path="/" element={<WelcomePage />} />
                <Route path="/setup" element={<SetupWizard />} />
                <Route path="/onboarding" element={<FirstRunWizard />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/create" element={<CreateWizard />} />
                <Route path="/create/legacy" element={<CreatePage />} />
                <Route path="/timeline" element={<TimelinePage />} />
                <Route path="/render" element={<RenderPage />} />
                <Route path="/publish" element={<PublishPage />} />
                <Route path="/projects" element={<ProjectsPage />} />
                <Route path="/jobs" element={<RecentJobsPage />} />
                <Route path="/downloads" element={<DownloadsPage />} />
                <Route path="/health" element={<ProviderHealthDashboard />} />
                <Route path="/logs" element={<LogViewerPage />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </Layout>
          </BrowserRouter>
          <KeyboardShortcutsModal 
            isOpen={showShortcuts} 
            onClose={() => setShowShortcuts(false)} 
          />
          <NotificationsToaster toasterId={toasterId} />
          
          {/* Job progress drawer */}
          <JobProgressDrawer
            isOpen={showDrawer}
            onClose={() => setShowDrawer(false)}
            jobId={currentJobId || ''}
          />
        </div>
      </FluentProvider>
    </ThemeContext.Provider>
  );
}

export default App;
