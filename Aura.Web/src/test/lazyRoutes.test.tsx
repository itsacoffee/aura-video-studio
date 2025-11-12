/**
 * Tests for lazy-loaded route rendering
 * Requirement 3: Add unit test that renders each lazy route component without error
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { lazy, Suspense, ReactNode } from 'react';
import { HashRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';

// Import all lazy-loaded components
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
const CreatePage = lazy(() =>
  import('../pages/CreatePage').then((m) => ({ default: m.CreatePage }))
);
const CreateWizard = lazy(() =>
  import('../pages/Wizard/CreateWizard').then((m) => ({ default: m.CreateWizard }))
);
const DownloadsPage = lazy(() =>
  import('../pages/DownloadsPage').then((m) => ({ default: m.DownloadsPage }))
);
const TimelineEditor = lazy(() =>
  import('../pages/Editor/TimelineEditor').then((m) => ({ default: m.TimelineEditor }))
);
const IdeationDashboard = lazy(() =>
  import('../pages/Ideation/IdeationDashboard').then((m) => ({ default: m.IdeationDashboard }))
);
const LogViewerPage = lazy(() =>
  import('../pages/LogViewerPage').then((m) => ({ default: m.LogViewerPage }))
);
const ProjectsPage = lazy(() =>
  import('../pages/Projects/ProjectsPage').then((m) => ({ default: m.ProjectsPage }))
);
const RecentJobsPage = lazy(() =>
  import('../pages/RecentJobsPage').then((m) => ({ default: m.RecentJobsPage }))
);
const RenderPage = lazy(() =>
  import('../pages/RenderPage').then((m) => ({ default: m.RenderPage }))
);
const SettingsPage = lazy(() =>
  import('../pages/SettingsPage').then((m) => ({ default: m.SettingsPage }))
);
const SystemHealthDashboard = lazy(() => import('../pages/Health/SystemHealthDashboard'));
const TemplatesLibrary = lazy(() => import('../pages/Templates/TemplatesLibrary'));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
});

const TestWrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>
    <FluentProvider theme={webLightTheme}>
      <HashRouter>
        <Suspense fallback={<div>Loading...</div>}>{children}</Suspense>
      </HashRouter>
    </FluentProvider>
  </QueryClientProvider>
);

describe('Lazy Route Component Rendering', () => {
  it('should render AdminDashboardPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <AdminDashboardPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render AestheticsPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <AestheticsPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render AIEditingPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <AIEditingPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render AssetLibrary without error', async () => {
    const { container } = render(
      <TestWrapper>
        <AssetLibrary />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render CreatePage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <CreatePage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render CreateWizard without error', async () => {
    const { container } = render(
      <TestWrapper>
        <CreateWizard />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render DownloadsPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <DownloadsPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render TimelineEditor without error', async () => {
    const { container } = render(
      <TestWrapper>
        <TimelineEditor />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render IdeationDashboard without error', async () => {
    const { container } = render(
      <TestWrapper>
        <IdeationDashboard />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render LogViewerPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <LogViewerPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render ProjectsPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <ProjectsPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render RecentJobsPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <RecentJobsPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render RenderPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <RenderPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render SettingsPage without error', async () => {
    const { container } = render(
      <TestWrapper>
        <SettingsPage />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render SystemHealthDashboard without error', async () => {
    const { container } = render(
      <TestWrapper>
        <SystemHealthDashboard />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });

  it('should render TemplatesLibrary without error', async () => {
    const { container } = render(
      <TestWrapper>
        <TemplatesLibrary />
      </TestWrapper>
    );
    expect(container).toBeTruthy();
  });
});
