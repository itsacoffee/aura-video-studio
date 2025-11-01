import { FluentProvider, webLightTheme, webDarkTheme, Spinner } from '@fluentui/react-components';
import { useState, useEffect, createContext, useContext, lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { CommandPalette } from './components/CommandPalette';
import { ContentPlanningDashboard } from './components/contentPlanning/ContentPlanningDashboard';
import { QualityDashboard } from './components/dashboard';
import { ErrorBoundary } from './components/ErrorBoundary';
import { GlobalStatusFooter } from './components/GlobalStatusFooter';
import { JobProgressDrawer } from './components/JobProgressDrawer';
import { KeyboardShortcutsPanel } from './components/KeyboardShortcuts/KeyboardShortcutsPanel';
import { KeyboardShortcutsModal } from './components/KeyboardShortcutsModal';
import { Layout } from './components/Layout';
import { NotificationsToaster } from './components/Notifications/Toasts';
import { PlatformDashboard } from './components/Platform';
import { JobStatusBar } from './components/StatusBar/JobStatusBar';
import { env } from './config/env';
import { AestheticsPage } from './pages/Aesthetics/AestheticsPage';
import { AIEditingPage } from './pages/AIEditing/AIEditingPage';
import { AssetLibrary } from './pages/Assets/AssetLibrary';
import { CreatePage } from './pages/CreatePage';
import { DashboardPage } from './pages/DashboardPage';
import { DownloadsPage } from './pages/DownloadsPage';
import { TimelineEditor } from './pages/Editor/TimelineEditor';
import { ExportHistoryPage } from './pages/Export/ExportHistoryPage';
import { ProviderHealthDashboard } from './pages/Health/ProviderHealthDashboard';
import { IdeationDashboard } from './pages/Ideation/IdeationDashboard';
import { TrendingTopicsExplorer } from './pages/Ideation/TrendingTopicsExplorer';
import { LocalizationPage } from './pages/Localization/LocalizationPage';
import { ModelsManagementPage } from './pages/Models/ModelsManagementPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { FirstRunWizard } from './pages/Onboarding/FirstRunWizard';
import { PacingAnalyzerPage } from './pages/PacingAnalyzerPage';
import { ProjectsPage } from './pages/Projects/ProjectsPage';
import { PromptManagementPage } from './pages/PromptManagement/PromptManagementPage';
import { PublishPage } from './pages/PublishPage';
import { RecentJobsPage } from './pages/RecentJobsPage';
import { RenderPage } from './pages/RenderPage';
import { SettingsPage } from './pages/SettingsPage';
import { SetupWizard } from './pages/Setup/SetupWizard';
import TemplatesLibrary from './pages/Templates/TemplatesLibrary';
import { TimelinePage } from './pages/TimelinePage';
import { VideoEditorPage } from './pages/VideoEditorPage';
import { WelcomePage } from './pages/WelcomePage';
import { CreateWizard } from './pages/Wizard/CreateWizard';
import { errorReportingService } from './services/errorReportingService';
import { hasCompletedFirstRun, migrateLegacyFirstRunStatus } from './services/firstRunService';
import { healthMonitorService } from './services/healthMonitorService';
import { keyboardShortcutManager } from './services/keyboardShortcutManager';
import { loggingService } from './services/loggingService';
import { ActivityProvider } from './state/activityContext';
import { useJobState } from './state/jobState';

// Lazy load development-only features to reduce production bundle size
const LogViewerPage = lazy(() =>
  import('./pages/LogViewerPage').then((m) => ({ default: m.LogViewerPage }))
);
const ActivityDemoPage = lazy(() =>
  import('./pages/ActivityDemoPage').then((m) => ({ default: m.ActivityDemoPage }))
);
const LayoutDemoPage = lazy(() =>
  import('./pages/LayoutDemoPage').then((m) => ({ default: m.LayoutDemoPage }))
);

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
  const [showShortcutsPanel, setShowShortcutsPanel] = useState(false);
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

  // Global error handlers for uncaught errors and promise rejections
  useEffect(() => {
    const handleError = (event: ErrorEvent) => {
      event.preventDefault(); // Prevent default browser error handling
      loggingService.error('Uncaught error', event.error, 'window', 'error', {
        message: event.message,
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
      });
    };

    const handleUnhandledRejection = (event: PromiseRejectionEvent) => {
      event.preventDefault(); // Prevent default browser error handling
      const error = event.reason instanceof Error ? event.reason : new Error(String(event.reason));
      loggingService.error('Unhandled promise rejection', error, 'window', 'unhandledrejection', {
        reason: event.reason,
      });
    };

    window.addEventListener('error', handleError);
    window.addEventListener('unhandledrejection', handleUnhandledRejection);

    return () => {
      window.removeEventListener('error', handleError);
      window.removeEventListener('unhandledrejection', handleUnhandledRejection);
    };
  }, []);

  // Start health monitoring on app mount
  useEffect(() => {
    healthMonitorService.start();

    // Add listener for health warnings
    const handleHealthWarning = (warning: { message: string; suggestion?: string }) => {
      errorReportingService.warning(warning.message, warning.suggestion ?? '', { duration: 10000 });
    };

    healthMonitorService.addWarningListener(handleHealthWarning);

    return () => {
      healthMonitorService.removeWarningListener(handleHealthWarning);
      healthMonitorService.stop();
    };
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

  // Register global shortcuts
  useEffect(() => {
    // Register global shortcuts that are always available
    keyboardShortcutManager.registerMultiple([
      {
        id: 'open-settings',
        keys: 'Ctrl+,',
        description: 'Open Settings',
        context: 'global',
        handler: () => {
          window.location.href = '/settings';
        },
      },
      {
        id: 'new-project',
        keys: 'Ctrl+N',
        description: 'New Project',
        context: 'global',
        handler: () => {
          window.location.href = '/create';
        },
      },
      {
        id: 'open-project',
        keys: 'Ctrl+O',
        description: 'Open Project',
        context: 'global',
        handler: () => {
          window.location.href = '/projects';
        },
      },
      {
        id: 'save-project',
        keys: 'Ctrl+S',
        description: 'Save Project',
        context: 'global',
        handler: (e) => {
          e.preventDefault();
          // Placeholder for save functionality
        },
      },
    ]);

    // Clean up on unmount
    return () => {
      keyboardShortcutManager.unregisterContext('global');
    };
  }, []);

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

  // Global keyboard shortcut handler
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Let the keyboard shortcut manager handle registered shortcuts first
      const handled = keyboardShortcutManager.handleKeyEvent(e);
      if (handled) {
        return;
      }

      // Handle special global shortcuts not in the manager
      // Ctrl+Shift+L for log viewer
      if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'L') {
        e.preventDefault();
        window.location.href = '/logs';
      }
      // Ctrl+K or Cmd+K for shortcuts panel (preferred)
      else if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setShowShortcutsPanel(true);
      }
      // ? key for shortcuts panel (alternative)
      else if (e.key === '?' && !e.ctrlKey && !e.metaKey && !e.altKey) {
        // Don't trigger if user is typing in an input
        if (!(e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement)) {
          e.preventDefault();
          setShowShortcutsPanel(true);
        }
      }
      // Ctrl+P or Cmd+P for command palette
      else if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
        e.preventDefault();
        setShowCommandPalette(true);
      }
      // Ctrl+/ or Cmd+/ for old shortcuts modal
      else if ((e.ctrlKey || e.metaKey) && e.key === '/') {
        e.preventDefault();
        setShowShortcuts(true);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Handle unsaved changes warning before closing window
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      // Check if we're on the video editor page and have unsaved changes
      // This is a simple check - in a real implementation, you'd want to check the actual state
      const currentPath = window.location.pathname;
      const isEditorPage = currentPath === '/editor' || currentPath === '/timeline';

      if (isEditorPage) {
        // Check localStorage for unsaved project data
        const autosaveData = localStorage.getItem('aura-project-autosave');
        if (autosaveData) {
          e.preventDefault();
          e.returnValue = '';
        }
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, []);

  const toggleTheme = () => {
    setIsDarkMode(!isDarkMode);
  };

  // Show loading spinner while checking first-run status
  if (isCheckingFirstRun) {
    return (
      <ThemeContext.Provider value={{ isDarkMode, toggleTheme }}>
        <FluentProvider theme={isDarkMode ? webDarkTheme : webLightTheme}>
          <div
            style={{
              height: '100vh',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
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
                <ErrorBoundary>
                  <Routes>
                    {/* First-run onboarding route - highest priority */}
                    <Route path="/onboarding" element={<FirstRunWizard />} />

                    {/* Redirect to onboarding if first run */}
                    <Route
                      path="/"
                      element={
                        shouldShowOnboarding ? (
                          <Navigate to="/onboarding" replace />
                        ) : (
                          <WelcomePage />
                        )
                      }
                    />

                    {/* All other routes */}
                    <Route path="/setup" element={<SetupWizard />} />
                    <Route path="/dashboard" element={<DashboardPage />} />
                    <Route path="/ideation" element={<IdeationDashboard />} />
                    <Route path="/trending" element={<TrendingTopicsExplorer />} />
                    <Route path="/content-planning" element={<ContentPlanningDashboard />} />
                    <Route path="/create" element={<CreateWizard />} />
                    <Route path="/create/legacy" element={<CreatePage />} />
                    <Route path="/templates" element={<TemplatesLibrary />} />
                    <Route path="/timeline" element={<TimelinePage />} />
                    <Route path="/editor/:jobId" element={<TimelineEditor />} />
                    <Route path="/editor" element={<VideoEditorPage />} />
                    <Route path="/pacing" element={<PacingAnalyzerPage />} />
                    <Route path="/render" element={<RenderPage />} />
                    <Route path="/platform" element={<PlatformDashboard />} />
                    <Route path="/quality" element={<QualityDashboard />} />
                    <Route path="/publish" element={<PublishPage />} />
                    <Route path="/projects" element={<ProjectsPage />} />
                    <Route path="/export-history" element={<ExportHistoryPage />} />
                    <Route path="/assets" element={<AssetLibrary />} />
                    <Route path="/jobs" element={<RecentJobsPage />} />
                    <Route path="/downloads" element={<DownloadsPage />} />
                    <Route path="/health" element={<ProviderHealthDashboard />} />
                    <Route path="/ai-editing" element={<AIEditingPage />} />
                    <Route path="/aesthetics" element={<AestheticsPage />} />
                    <Route path="/models" element={<ModelsManagementPage />} />
                    <Route path="/localization" element={<LocalizationPage />} />
                    <Route path="/prompt-management" element={<PromptManagementPage />} />
                    {/* Development-only routes - lazy loaded */}
                    {env.enableDevTools && (
                      <>
                        <Route
                          path="/logs"
                          element={
                            <Suspense fallback={<Spinner label="Loading..." />}>
                              <LogViewerPage />
                            </Suspense>
                          }
                        />
                        <Route
                          path="/activity-demo"
                          element={
                            <Suspense fallback={<Spinner label="Loading..." />}>
                              <ActivityDemoPage />
                            </Suspense>
                          }
                        />
                        <Route
                          path="/layout-demo"
                          element={
                            <Suspense fallback={<Spinner label="Loading..." />}>
                              <LayoutDemoPage />
                            </Suspense>
                          }
                        />
                      </>
                    )}
                    <Route path="/settings" element={<SettingsPage />} />
                    <Route path="*" element={<NotFoundPage />} />
                  </Routes>
                </ErrorBoundary>
              </Layout>

              {/* These components need to be inside BrowserRouter for navigation hooks */}
              <KeyboardShortcutsModal
                isOpen={showShortcuts}
                onClose={() => setShowShortcuts(false)}
              />
              <KeyboardShortcutsPanel
                isOpen={showShortcutsPanel}
                onClose={() => setShowShortcutsPanel(false)}
              />
              <CommandPalette
                isOpen={showCommandPalette}
                onClose={() => setShowCommandPalette(false)}
              />
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
