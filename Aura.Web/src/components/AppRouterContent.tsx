/**
 * AppRouterContent Component
 * Wraps all content that requires React Router context
 * This component MUST be rendered inside MemoryRouter/BrowserRouter
 */

import { Spinner } from '@fluentui/react-components';
import { lazy, useEffect, useState, type FC } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { env } from '../config/env';
import { NavigationProvider } from '../contexts/NavigationContext';
import { useMenuCommandSystem } from '../hooks/useMenuCommandSystem';
import { DashboardPage } from '../pages/DashboardPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { FirstRunWizard } from '../pages/Onboarding/FirstRunWizard';
import { WelcomePage } from '../pages/WelcomePage';
import { hasCompletedFirstRun } from '../services/firstRunService';
import { useJobState, type JobStatus } from '../state/jobState';
import { KeyboardShortcutsCheatSheet } from './Accessibility/KeyboardShortcutsCheatSheet';
import { CommandPalette } from './CommandPalette';
import { ConfigurationGate } from './ConfigurationGate';
import { QualityDashboard } from './dashboard';
import { ErrorBoundary } from './ErrorBoundary';
import { GlobalStatusFooter } from './GlobalStatusFooter';
import { JobProgressDrawer } from './JobProgressDrawer';
import { KeyboardShortcutsPanel } from './KeyboardShortcuts/KeyboardShortcutsPanel';
import { KeyboardShortcutsModal } from './KeyboardShortcutsModal';
import { Layout } from './Layout';
import { LazyRoute } from './LazyRoute';
import { NotificationsToaster } from './Notifications/Toasts';
import { PlatformDashboard } from './Platform';
import { SafeModeBanner } from './SafeMode';
import { JobStatusBar } from './StatusBar/JobStatusBar';
import { ActionHistoryPanel } from './UndoRedo/ActionHistoryPanel';
import { VideoCreationWizard } from './VideoWizard/VideoCreationWizard';

// Lazy load non-critical pages
const AdminDashboardPage = lazy(() => import('../pages/Admin/AdminDashboardPage'));
const AestheticsPage = lazy(() =>
  import('../pages/Aesthetics/AestheticsPage').then((m) => ({ default: m.AestheticsPage }))
);
const AIEditingPage = lazy(() =>
  import('../pages/AIEditing/AIEditingPage').then((m) => ({ default: m.AIEditingPage }))
);
const AssetLibrary = lazy(() =>
  import('../pages/Assets/AssetLibrary').then((m) => ({ default: m.AssetLibrary }))
);
const AudienceManagementPage = lazy(() => import('../pages/Audience/AudienceManagementPage'));
const CreatePage = lazy(() =>
  import('../pages/CreatePage').then((m) => ({ default: m.CreatePage }))
);
const DownloadsPage = lazy(() =>
  import('../pages/DownloadsPage').then((m) => ({ default: m.DownloadsPage }))
);
const TimelineEditor = lazy(() =>
  import('../pages/Editor/TimelineEditor').then((m) => ({ default: m.TimelineEditor }))
);
const ExportHistoryPage = lazy(() =>
  import('../pages/Export/ExportHistoryPage').then((m) => ({ default: m.ExportHistoryPage }))
);
const ProviderHealthDashboard = lazy(() =>
  import('../pages/Health/ProviderHealthDashboard').then((m) => ({
    default: m.ProviderHealthDashboard,
  }))
);
const ProvidersPage = lazy(() =>
  import('../pages/ProvidersPage').then((m) => ({
    default: m.ProvidersPage,
  }))
);
const SystemHealthDashboard = lazy(() => import('../pages/Health/SystemHealthDashboard'));
const CostHistoryPage = lazy(() =>
  import('../pages/CostHistory/CostHistoryPage').then((m) => ({
    default: m.CostHistoryPage,
  }))
);
const IdeationDashboard = lazy(() =>
  import('../pages/Ideation/IdeationDashboard').then((m) => ({ default: m.IdeationDashboard }))
);
const TrendingTopicsExplorer = lazy(() =>
  import('../pages/Ideation/TrendingTopicsExplorer').then((m) => ({
    default: m.TrendingTopicsExplorer,
  }))
);
const ConceptExplorer = lazy(() =>
  import('../pages/Ideation/ConceptExplorer').then((m) => ({ default: m.default }))
);
const BriefBuilder = lazy(() =>
  import('../pages/Ideation/BriefBuilder').then((m) => ({ default: m.default }))
);
const StoryboardVisualizer = lazy(() =>
  import('../pages/Ideation/StoryboardVisualizer').then((m) => ({ default: m.default }))
);
const RunDetailsPage = lazy(() =>
  import('../pages/Jobs/RunDetailsPage').then((m) => ({ default: m.RunDetailsPage }))
);
const LearningPage = lazy(() => import('../pages/Learning/LearningPage'));
const TranslationPage = lazy(() =>
  import('../pages/Localization/TranslationPage').then((m) => ({ default: m.TranslationPage }))
);
const MLLabPage = lazy(() =>
  import('../pages/MLLab/MLLabPage').then((m) => ({ default: m.MLLabPage }))
);
const PacingAnalyzerPage = lazy(() =>
  import('../pages/PacingAnalyzerPage').then((m) => ({ default: m.PacingAnalyzerPage }))
);
const ABTestManagementPage = lazy(
  () => import('../pages/PerformanceAnalytics/ABTestManagementPage')
);
const PerformanceAnalyticsPage = lazy(
  () => import('../pages/PerformanceAnalytics/PerformanceAnalyticsPage')
);
const UsageAnalyticsPage = lazy(() =>
  import('../pages/Analytics/UsageAnalyticsPage').then((m) => ({ default: m.default }))
);
const ProjectsPage = lazy(() =>
  import('../pages/Projects/ProjectsPage').then((m) => ({ default: m.ProjectsPage }))
);
const PromptManagementPage = lazy(() =>
  import('../pages/PromptManagement/PromptManagementPage').then((m) => ({
    default: m.PromptManagementPage,
  }))
);
const QualityValidationPage = lazy(
  () => import('../pages/QualityValidation/QualityValidationPage')
);
const RagDocumentManager = lazy(() => import('../pages/RAG/RagDocumentManager'));
const RecentJobsPage = lazy(() =>
  import('../pages/RecentJobsPage').then((m) => ({ default: m.RecentJobsPage }))
);
const RenderPage = lazy(() =>
  import('../pages/RenderPage').then((m) => ({ default: m.RenderPage }))
);
const SettingsPage = lazy(() =>
  import('../pages/SettingsPage').then((m) => ({ default: m.SettingsPage }))
);
const AccessibilitySettingsPage = lazy(() =>
  import('../pages/AccessibilitySettingsPage').then((m) => ({
    default: m.AccessibilitySettingsPage,
  }))
);
const CustomTemplatesPage = lazy(() => import('../pages/Templates/CustomTemplatesPage'));
const TemplatesLibrary = lazy(() => import('../pages/Templates/TemplatesLibrary'));
const ValidationPage = lazy(() => import('../pages/Validation/ValidationPage'));
const VerificationPage = lazy(() => import('../pages/Verification/VerificationPage'));
const VideoEditorPage = lazy(() =>
  import('../pages/VideoEditorPage').then((m) => ({ default: m.VideoEditorPage }))
);
const VoiceEnhancementPage = lazy(() => import('../pages/VoiceEnhancement/VoiceEnhancementPage'));
const CreateWizard = lazy(() =>
  import('../pages/Wizard/CreateWizard').then((m) => ({ default: m.CreateWizard }))
);

// Development-only features
const LogViewerPage = lazy(() =>
  import('../pages/LogViewerPage').then((m) => ({ default: m.LogViewerPage }))
);
const DiagnosticDashboardPage = lazy(() =>
  import('../pages/DiagnosticDashboardPage').then((m) => ({ default: m.DiagnosticDashboardPage }))
);
const ActivityDemoPage = lazy(() =>
  import('../pages/ActivityDemoPage').then((m) => ({ default: m.ActivityDemoPage }))
);
const LayoutDemoPage = lazy(() =>
  import('../pages/LayoutDemoPage').then((m) => ({ default: m.LayoutDemoPage }))
);
const Windows11DemoPage = lazy(() =>
  import('../pages/Windows11DemoPage').then((m) => ({ default: m.Windows11DemoPage }))
);
const ErrorHandlingDemoPage = lazy(() =>
  import('../pages/ErrorHandlingDemoPage').then((m) => ({ default: m.ErrorHandlingDemoPage }))
);
const StreamingScriptDemo = lazy(() =>
  import('../pages/Demo/StreamingScriptDemo').then((m) => ({ default: m.StreamingScriptDemo }))
);
const ButtonDemoPage = lazy(() =>
  import('../pages/Demo/ButtonDemoPage').then((m) => ({ default: m.ButtonDemoPage }))
);

/**
 * ProtectedRoute Component
 * Checks first-run status and redirects to setup if not completed
 * CRITICAL FIX: Added error handling and localStorage check to prevent crashes during wizard-to-app transition
 */
const ProtectedRoute: FC<{ children: React.ReactNode }> = ({ children }) => {
  const [setupComplete, setSetupComplete] = useState<boolean | null>(null);

  useEffect(() => {
    // CRITICAL FIX: Check localStorage first for immediate check (synchronous)
    // This prevents redirect loops when wizard just completed
    const localStatus =
      localStorage.getItem('hasCompletedFirstRun') === 'true' ||
      localStorage.getItem('hasSeenOnboarding') === 'true';

    if (localStatus) {
      // If localStorage says complete, trust it immediately
      // This prevents redirect loops during wizard-to-app transition
      console.info('[ProtectedRoute] localStorage indicates setup complete, allowing access');
      setSetupComplete(true);
      return;
    }

    // If localStorage doesn't say complete, check async (backend/localStorage sync)
    hasCompletedFirstRun()
      .then((completed) => {
        console.info('[ProtectedRoute] First-run check result:', completed);
        setSetupComplete(completed);
      })
      .catch((error) => {
        console.error('[ProtectedRoute] Error checking first-run status:', error);
        // On error, check localStorage again as fallback
        const fallbackStatus =
          localStorage.getItem('hasCompletedFirstRun') === 'true' ||
          localStorage.getItem('hasSeenOnboarding') === 'true';
        setSetupComplete(fallbackStatus);
      });
  }, []);

  // Show loading spinner while checking
  if (setupComplete === null) {
    return (
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
    );
  }

  // Redirect to setup if not complete
  if (!setupComplete) {
    return <Navigate to="/setup" replace />;
  }

  // Render protected content
  return <>{children}</>;
};

interface AppRouterContentProps {
  showShortcuts: boolean;
  showShortcutsPanel: boolean;
  showShortcutsCheatSheet: boolean;
  showCommandPalette: boolean;
  setShowShortcuts: (show: boolean) => void;
  setShowShortcutsPanel: (show: boolean) => void;
  setShowShortcutsCheatSheet: (show: boolean) => void;
  setShowCommandPalette: (show: boolean) => void;
  toasterId: string;
  showDiagnostics: boolean;
  setShowDiagnostics: (show: boolean) => void;
}

/**
 * Component that wraps all router-dependent content
 * MUST be rendered inside MemoryRouter or BrowserRouter
 */
export const AppRouterContent: FC<AppRouterContentProps> = ({
  showShortcuts,
  showShortcutsPanel,
  showShortcutsCheatSheet,
  showCommandPalette,
  setShowShortcuts,
  setShowShortcutsPanel,
  setShowShortcutsCheatSheet,
  setShowCommandPalette,
  toasterId,
  showDiagnostics,
  setShowDiagnostics,
}) => {
  const { currentJobId, status, progress, message } = useJobState();
  const [showDrawer, setShowDrawer] = useState(false);

  return (
    <NavigationProvider>
      <AppRouterContentInner
        showShortcuts={showShortcuts}
        showShortcutsPanel={showShortcutsPanel}
        showShortcutsCheatSheet={showShortcutsCheatSheet}
        showCommandPalette={showCommandPalette}
        setShowShortcuts={setShowShortcuts}
        setShowShortcutsPanel={setShowShortcutsPanel}
        setShowShortcutsCheatSheet={setShowShortcutsCheatSheet}
        setShowCommandPalette={setShowCommandPalette}
        toasterId={toasterId}
        showDiagnostics={showDiagnostics}
        setShowDiagnostics={setShowDiagnostics}
        currentJobId={currentJobId}
        status={status}
        progress={progress}
        message={message}
        showDrawer={showDrawer}
        setShowDrawer={setShowDrawer}
      />
    </NavigationProvider>
  );
};

const AppRouterContentInner: FC<
  AppRouterContentProps & {
    currentJobId: string | null;
    status: JobStatus;
    progress: number;
    message: string;
    showDrawer: boolean;
    setShowDrawer: (show: boolean) => void;
  }
> = ({
  showShortcuts,
  showShortcutsPanel,
  showShortcutsCheatSheet,
  showCommandPalette,
  setShowShortcuts,
  setShowShortcutsPanel,
  setShowShortcutsCheatSheet,
  setShowCommandPalette,
  toasterId,
  showDiagnostics,
  setShowDiagnostics,
  currentJobId,
  status,
  progress,
  message,
  showDrawer,
  setShowDrawer,
}) => {
  // Register enhanced menu command system (wires File, Edit, View, Tools, Help menus)
  // This MUST be inside Router context because it uses useNavigate()
  // New system includes validation, correlation IDs, context awareness, and user feedback
  // CRITICAL FIX: Hook must be called unconditionally (Rules of Hooks)
  // Error handling is done inside the hook's useEffect, not here
  useMenuCommandSystem();

  // Black screen prevention: Ensure we always render something visible
  // This prevents the app from going completely black if Routes fails
  try {
    return (
      <>
        <SafeModeBanner onOpenDiagnostics={() => setShowDiagnostics(!showDiagnostics)} />

        {/* Status bar for job progress */}
        <JobStatusBar
          status={status}
          progress={progress}
          message={message}
          onViewDetails={() => setShowDrawer(true)}
        />

        <ErrorBoundary>
          <ConfigurationGate>
            <Routes>
              {/* Setup wizard - unified entry point for first-run and reconfiguration */}
              <Route path="/setup" element={<FirstRunWizard />} />
              {/* Legacy route redirect for backward compatibility */}
              <Route path="/onboarding" element={<Navigate to="/setup" replace />} />

              {/* Main routes with Layout wrapper - Protected by first-run check */}
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <Layout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<WelcomePage />} />
                <Route path="dashboard" element={<DashboardPage />} />
                <Route
                  path="ideation"
                  element={
                    <LazyRoute routePath="/ideation">
                      <IdeationDashboard />
                    </LazyRoute>
                  }
                />
                <Route
                  path="ideation/concept/:conceptId"
                  element={
                    <LazyRoute routePath="/ideation/concept/:conceptId">
                      <ConceptExplorer />
                    </LazyRoute>
                  }
                />
                <Route
                  path="ideation/brief-builder"
                  element={
                    <LazyRoute routePath="/ideation/brief-builder">
                      <BriefBuilder />
                    </LazyRoute>
                  }
                />
                <Route
                  path="ideation/storyboard/:conceptId"
                  element={
                    <LazyRoute routePath="/ideation/storyboard/:conceptId">
                      <StoryboardVisualizer />
                    </LazyRoute>
                  }
                />
                <Route
                  path="trending"
                  element={
                    <LazyRoute routePath="/trending">
                      <TrendingTopicsExplorer />
                    </LazyRoute>
                  }
                />
                <Route path="create" element={<VideoCreationWizard />} />
                <Route path="generate" element={<VideoCreationWizard />} />
                <Route
                  path="create/advanced"
                  element={
                    <LazyRoute routePath="/create/advanced">
                      <CreateWizard />
                    </LazyRoute>
                  }
                />
                <Route
                  path="create/legacy"
                  element={
                    <LazyRoute routePath="/create/legacy">
                      <CreatePage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="templates"
                  element={
                    <LazyRoute routePath="/templates">
                      <TemplatesLibrary />
                    </LazyRoute>
                  }
                />
                <Route
                  path="templates/custom"
                  element={
                    <LazyRoute routePath="/templates/custom">
                      <CustomTemplatesPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="editor/:jobId"
                  element={
                    <LazyRoute routePath="/editor/:jobId">
                      <TimelineEditor />
                    </LazyRoute>
                  }
                />
                <Route
                  path="editor"
                  element={
                    <LazyRoute routePath="/editor">
                      <VideoEditorPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="pacing"
                  element={
                    <LazyRoute routePath="/pacing">
                      <PacingAnalyzerPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="render"
                  element={
                    <LazyRoute routePath="/render">
                      <RenderPage />
                    </LazyRoute>
                  }
                />
                <Route path="platform" element={<PlatformDashboard />} />
                <Route path="quality" element={<QualityDashboard />} />

                <Route
                  path="projects"
                  element={
                    <LazyRoute routePath="/projects">
                      <ProjectsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="export-history"
                  element={
                    <LazyRoute routePath="/export-history">
                      <ExportHistoryPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="assets"
                  element={
                    <LazyRoute routePath="/assets">
                      <AssetLibrary />
                    </LazyRoute>
                  }
                />
                <Route
                  path="cost-history"
                  element={
                    <LazyRoute routePath="/cost-history">
                      <CostHistoryPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="jobs"
                  element={
                    <LazyRoute routePath="/jobs">
                      <RecentJobsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="jobs/:jobId/telemetry"
                  element={
                    <LazyRoute routePath="/jobs/:jobId/telemetry">
                      <RunDetailsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="downloads"
                  element={
                    <LazyRoute routePath="/downloads">
                      <DownloadsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="health"
                  element={
                    <LazyRoute routePath="/health">
                      <SystemHealthDashboard />
                    </LazyRoute>
                  }
                />
                <Route
                  path="health/providers"
                  element={
                    <LazyRoute routePath="/health/providers">
                      <ProviderHealthDashboard />
                    </LazyRoute>
                  }
                />
                <Route
                  path="providers"
                  element={
                    <LazyRoute routePath="/providers">
                      <ProvidersPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="ai-editing"
                  element={
                    <LazyRoute routePath="/ai-editing">
                      <AIEditingPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="aesthetics"
                  element={
                    <LazyRoute routePath="/aesthetics">
                      <AestheticsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="localization"
                  element={
                    <LazyRoute routePath="/localization">
                      <TranslationPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="prompt-management"
                  element={
                    <LazyRoute routePath="/prompt-management">
                      <PromptManagementPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="rag"
                  element={
                    <LazyRoute routePath="/rag">
                      <RagDocumentManager />
                    </LazyRoute>
                  }
                />
                <Route
                  path="voice-enhancement"
                  element={
                    <LazyRoute routePath="/voice-enhancement">
                      <VoiceEnhancementPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="performance-analytics"
                  element={
                    <LazyRoute routePath="/performance-analytics">
                      <PerformanceAnalyticsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="usage-analytics"
                  element={
                    <LazyRoute routePath="/usage-analytics">
                      <UsageAnalyticsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="ml-lab"
                  element={
                    <LazyRoute routePath="/ml-lab">
                      <MLLabPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="ab-tests"
                  element={
                    <LazyRoute routePath="/ab-tests">
                      <ABTestManagementPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="audience"
                  element={
                    <LazyRoute routePath="/audience">
                      <AudienceManagementPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="learning"
                  element={
                    <LazyRoute routePath="/learning">
                      <LearningPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="quality-validation"
                  element={
                    <LazyRoute routePath="/quality-validation">
                      <QualityValidationPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="validation"
                  element={
                    <LazyRoute routePath="/validation">
                      <ValidationPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="verification"
                  element={
                    <LazyRoute routePath="/verification">
                      <VerificationPage />
                    </LazyRoute>
                  }
                />

                {/* Diagnostics and system information */}
                <Route
                  path="diagnostics"
                  element={
                    <LazyRoute routePath="/diagnostics">
                      <DiagnosticDashboardPage />
                    </LazyRoute>
                  }
                />

                {/* Logs page - always available for diagnostics */}
                <Route
                  path="logs"
                  element={
                    <LazyRoute routePath="/logs">
                      <LogViewerPage />
                    </LazyRoute>
                  }
                />

                {/* Development-only routes - lazy loaded */}
                {env.enableDevTools && (
                  <>
                    <Route
                      path="streaming-demo"
                      element={
                        <LazyRoute routePath="/streaming-demo">
                          <StreamingScriptDemo />
                        </LazyRoute>
                      }
                    />
                    <Route
                      path="button-demo"
                      element={
                        <LazyRoute routePath="/button-demo">
                          <ButtonDemoPage />
                        </LazyRoute>
                      }
                    />
                    <Route
                      path="error-handling-demo"
                      element={
                        <LazyRoute routePath="/error-handling-demo">
                          <ErrorHandlingDemoPage />
                        </LazyRoute>
                      }
                    />
                    <Route
                      path="activity-demo"
                      element={
                        <LazyRoute routePath="/activity-demo">
                          <ActivityDemoPage />
                        </LazyRoute>
                      }
                    />
                    <Route
                      path="layout-demo"
                      element={
                        <LazyRoute routePath="/layout-demo">
                          <LayoutDemoPage />
                        </LazyRoute>
                      }
                    />
                    <Route
                      path="windows11-demo"
                      element={
                        <LazyRoute routePath="/windows11-demo">
                          <Windows11DemoPage />
                        </LazyRoute>
                      }
                    />
                  </>
                )}
                <Route
                  path="admin"
                  element={
                    <LazyRoute routePath="/admin">
                      <AdminDashboardPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="settings"
                  element={
                    <LazyRoute routePath="/settings">
                      <SettingsPage />
                    </LazyRoute>
                  }
                />
                <Route
                  path="settings/accessibility"
                  element={
                    <LazyRoute routePath="/settings/accessibility">
                      <AccessibilitySettingsPage />
                    </LazyRoute>
                  }
                />
                <Route path="models" element={<Navigate to="/settings" replace />} />
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Routes>
          </ConfigurationGate>
        </ErrorBoundary>

        {/* These components need to be inside Router for navigation hooks */}
        <KeyboardShortcutsModal isOpen={showShortcuts} onClose={() => setShowShortcuts(false)} />
        <KeyboardShortcutsPanel
          isOpen={showShortcutsPanel}
          onClose={() => setShowShortcutsPanel(false)}
        />
        <KeyboardShortcutsCheatSheet
          open={showShortcutsCheatSheet}
          onClose={() => setShowShortcutsCheatSheet(false)}
        />
        <CommandPalette isOpen={showCommandPalette} onClose={() => setShowCommandPalette(false)} />
        <NotificationsToaster toasterId={toasterId} />

        {/* Job progress drawer */}
        <JobProgressDrawer
          isOpen={showDrawer}
          onClose={() => setShowDrawer(false)}
          jobId={currentJobId || ''}
        />

        {/* Action history panel for undo/redo */}
        <ActionHistoryPanel />

        {/* Global activity status footer */}
        <footer id="global-footer">
          <GlobalStatusFooter />
        </footer>
      </>
    );
  } catch (error) {
    // Critical error: Render fallback UI to prevent black screen
    console.error('[AppRouterContent] Critical error rendering routes:', error);
    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '20px',
          backgroundColor: '#1e1e1e',
          color: '#ffffff',
        }}
      >
        <div style={{ maxWidth: '600px', textAlign: 'center' }}>
          <h1 style={{ fontSize: '24px', marginBottom: '16px' }}>Application Error</h1>
          <p style={{ marginBottom: '20px' }}>
            The application encountered a critical error. Please reload the page.
          </p>
          <button
            onClick={() => window.location.reload()}
            style={{
              padding: '10px 20px',
              backgroundColor: '#0078d4',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: 600,
            }}
          >
            Reload Page
          </button>
        </div>
      </div>
    );
  }
};
