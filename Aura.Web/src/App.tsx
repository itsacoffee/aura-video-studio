import { useState, useEffect, createContext, useContext } from 'react';
import {
  FluentProvider,
  webLightTheme,
  webDarkTheme,
  Spinner,
} from '@fluentui/react-components';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './components/Layout';
import { WelcomePage } from './pages/WelcomePage';
import { DashboardPage } from './pages/DashboardPage';
import { CreatePage } from './pages/CreatePage';
import { CreateWizard } from './pages/Wizard/CreateWizard';
import { TimelinePage } from './pages/TimelinePage';
import { TimelineEditor } from './pages/Editor/TimelineEditor';
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
import { AssetLibrary } from './pages/Assets/AssetLibrary';
import { KeyboardShortcutsModal } from './components/KeyboardShortcutsModal';
import { CommandPalette } from './components/CommandPalette';
import { NotificationsToaster } from './components/Notifications/Toasts';
import { JobStatusBar } from './components/StatusBar/JobStatusBar';
import { JobProgressDrawer } from './components/JobProgressDrawer';
import { useJobState } from './state/jobState';
import { IdeationDashboard } from './pages/Ideation/IdeationDashboard';
import { TrendingTopicsExplorer } from './pages/Ideation/TrendingTopicsExplorer';
import { PlatformDashboard } from './components/Platform';
import { QualityDashboard } from './components/dashboard';
import { ContentPlanningDashboard } from './components/contentPlanning/ContentPlanningDashboard';
import { VideoEditorPage } from './pages/VideoEditorPage';
import { PacingAnalyzerPage } from './pages/PacingAnalyzerPage';
import { hasCompletedFirstRun, migrateLegacyFirstRunStatus } from './services/firstRunService';
import { ActivityProvider } from './state/activityContext';
import { GlobalStatusFooter } from './components/GlobalStatusFooter';

interface ThemeContextType {
  isDarkMode: boolean;
  toggleTheme: () => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const ThemeContext = createContext<ThemeContextType>({
  isDarkMode: false,
  toggleTheme: () => {},
});

// eslint-disable-next-line react-refresh/only-export-components
export const useTheme = () => useContext(ThemeContext);

function App() {
  // Initialize dark mode - default to dark on first run, then use localStorage
  const [isDarkMode, setIsDarkMode] = useState(() => {
    const saved = localStorage.getItem('darkMode');
    if (saved !== null) {
      return JSON.parse(saved);
    }
    // Default to dark mode for first-run users (creative app standard)
    return true;
  });
  
  const [showShortcuts, setShowShortcuts] = useState(false);
  const [showCommandPalette, setShowCommandPalette] = useState(false);
  const toasterId = 'notifications-toaster'; // Hardcoded to avoid hook context issues

  // First-run detection state
  const [isCheckingFirstRun, setIsCheckingFirstRun] = useState(true);
  const [shouldShowOnboarding, setShouldShowOnboarding] = useState(false);

  // Job state for status bar
  const { currentJobId, status, progress, message } = useJobState();
  const [showDrawer, setShowDrawer] = useState(false);

  // Check first-run status on app mount
  useEffect(() => {
    async function checkFirstRun() {
      try {
        // Migrate legacy first-run flag if needed
        migrateLegacyFirstRunStatus();

        // Check if user has completed first-run wizard
        const completed = await hasCompletedFirstRun();
        setShouldShowOnboarding(!completed);
      } catch (error) {
        console.error('Error checking first-run status:', error);
        // On error, assume not first-run to avoid blocking access
        setShouldShowOnboarding(false);
      } finally {
        setIsCheckingFirstRun(false);
      }
    }

    checkFirstRun();
  }, []);

  // Apply dark mode class to document root and save preference
  useEffect(() => {
    const root = document.documentElement;
    if (isDarkMode) {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
    localStorage.setItem('darkMode', JSON.stringify(isDarkMode));
  }, [isDarkMode]);

  // Poll job progress when a job is active
  useEffect(() => {
    // Early exit if no active job
    if (!currentJobId || status === 'completed' || status === 'failed') {
      return;
    }

    let isActive = true; // Prevent state updates after unmount
    const pollInterval = setInterval(async () => {
      // Skip if component unmounted or job completed
      if (!isActive) return;
      
      try {
        const response = await fetch(`/api/jobs/${currentJobId}/progress`);
        if (response.ok && isActive) {
          const data = await response.json();
          
          // Update progress only if still active
          if (isActive) {
            useJobState.getState().updateProgress(data.progress, data.currentStage);

            if (data.status === 'completed') {
              useJobState.getState().setStatus('completed');
              useJobState.getState().updateProgress(100, 'Video generation complete!');
              clearInterval(pollInterval); // Stop polling on completion
            } else if (data.status === 'failed') {
              useJobState.getState().setStatus('failed');
              useJobState.getState().updateProgress(data.progress, 'Generation failed');
              clearInterval(pollInterval); // Stop polling on failure
            }
          }
        }
      } catch (error) {
        if (isActive) {
          console.error('Error polling job progress:', error);
        }
      }
    }, 1000);

    return () => {
      isActive = false;
      clearInterval(pollInterval);
    };
  }, [currentJobId, status]);

  // Global keyboard shortcut handler for Ctrl+K (command palette) and Ctrl+/ (shortcuts)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl+K or Cmd+K for command palette
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setShowCommandPalette(true);
      }
      // Ctrl+P or Cmd+P for command palette (alternative)
      if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
        e.preventDefault();
        setShowCommandPalette(true);
      }
      // Ctrl+/ or Cmd+/ for shortcuts modal
      if ((e.ctrlKey || e.metaKey) && e.key === '/') {
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

  // Show loading spinner while checking first-run status
  if (isCheckingFirstRun) {
    return (
      <ThemeContext.Provider value={{ isDarkMode, toggleTheme }}>
        <FluentProvider theme={isDarkMode ? webDarkTheme : webLightTheme}>
          <div style={{ height: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Spinner size="large" label="Loading..." />
          </div>
        </FluentProvider>
      </ThemeContext.Provider>
    );
  }

  return (
    <ThemeContext.Provider value={{ isDarkMode, toggleTheme }}>
      <FluentProvider theme={isDarkMode ? webDarkTheme : webLightTheme}>
        <ActivityProvider>
          <div style={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
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
                  {/* First-run onboarding route - highest priority */}
                  <Route path="/onboarding" element={<FirstRunWizard />} />
                  
                  {/* Redirect to onboarding if first run */}
                  <Route 
                    path="/" 
                    element={shouldShowOnboarding ? <Navigate to="/onboarding" replace /> : <WelcomePage />} 
                  />
                  
                  {/* All other routes */}
                  <Route path="/setup" element={<SetupWizard />} />
                  <Route path="/dashboard" element={<DashboardPage />} />
                  <Route path="/ideation" element={<IdeationDashboard />} />
                  <Route path="/trending" element={<TrendingTopicsExplorer />} />
                  <Route path="/content-planning" element={<ContentPlanningDashboard />} />
                  <Route path="/create" element={<CreateWizard />} />
                  <Route path="/create/legacy" element={<CreatePage />} />
                  <Route path="/timeline" element={<TimelinePage />} />
                  <Route path="/editor/:jobId" element={<TimelineEditor />} />
                  <Route path="/editor" element={<VideoEditorPage />} />
                  <Route path="/pacing" element={<PacingAnalyzerPage />} />
                  <Route path="/render" element={<RenderPage />} />
                  <Route path="/platform" element={<PlatformDashboard />} />
                  <Route path="/quality" element={<QualityDashboard />} />
                  <Route path="/publish" element={<PublishPage />} />
                  <Route path="/projects" element={<ProjectsPage />} />
                  <Route path="/assets" element={<AssetLibrary />} />
                  <Route path="/jobs" element={<RecentJobsPage />} />
                  <Route path="/downloads" element={<DownloadsPage />} />
                  <Route path="/health" element={<ProviderHealthDashboard />} />
                  <Route path="/logs" element={<LogViewerPage />} />
                  <Route path="/settings" element={<SettingsPage />} />
                  <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
              </Layout>
              
              {/* These components need to be inside BrowserRouter for navigation hooks */}
              <KeyboardShortcutsModal isOpen={showShortcuts} onClose={() => setShowShortcuts(false)} />
              <CommandPalette isOpen={showCommandPalette} onClose={() => setShowCommandPalette(false)} />
              <NotificationsToaster toasterId={toasterId} />

              {/* Job progress drawer */}
              <JobProgressDrawer
                isOpen={showDrawer}
                onClose={() => setShowDrawer(false)}
                jobId={currentJobId || ''}
              />

              {/* Global activity status footer */}
              <GlobalStatusFooter />
            </BrowserRouter>
          </div>
        </ActivityProvider>
      </FluentProvider>
    </ThemeContext.Provider>
  );
}

export default App;
