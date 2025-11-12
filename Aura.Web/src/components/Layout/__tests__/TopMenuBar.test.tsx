/**
 * TopMenuBar Tests - Verify menu navigation handlers
 */

import { render, screen } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TopMenuBar } from '../TopMenuBar';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockProps = {
  onImportMedia: vi.fn(),
  onExportVideo: vi.fn(),
  onSaveProject: vi.fn(),
  onShowKeyboardShortcuts: vi.fn(),
  getCurrentPanelSizes: vi.fn(),
};

describe('TopMenuBar Navigation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders all menu buttons', () => {
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    expect(screen.getByText('File')).toBeInTheDocument();
    expect(screen.getByText('Edit')).toBeInTheDocument();
    expect(screen.getByText('View')).toBeInTheDocument();
    expect(screen.getByText('Window')).toBeInTheDocument();
    expect(screen.getByText('Help')).toBeInTheDocument();
  });

  it('File menu - New Project navigates to /create', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const fileButton = screen.getByText('File');
    await user.click(fileButton);

    const newProjectItem = screen.getByText('New Project...');
    await user.click(newProjectItem);

    expect(mockNavigate).toHaveBeenCalledWith('/create');
  });

  it('File menu - Open Project navigates to /projects', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const fileButton = screen.getByText('File');
    await user.click(fileButton);

    const openProjectItem = screen.getByText('Open Project...');
    await user.click(openProjectItem);

    expect(mockNavigate).toHaveBeenCalledWith('/projects');
  });

  it('File menu - Export History navigates to /export-history', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const fileButton = screen.getByText('File');
    await user.click(fileButton);

    const exportHistoryItem = screen.getByText('Export History');
    await user.click(exportHistoryItem);

    expect(mockNavigate).toHaveBeenCalledWith('/export-history');
  });

  it('Help menu - System Health navigates to /health', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const helpButton = screen.getByText('Help');
    await user.click(helpButton);

    const healthItem = screen.getByText('System Health');
    await user.click(healthItem);

    expect(mockNavigate).toHaveBeenCalledWith('/health');
  });

  it('Help menu - Tutorials navigates to /learning', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const helpButton = screen.getByText('Help');
    await user.click(helpButton);

    const tutorialsItem = screen.getByText('Tutorials');
    await user.click(tutorialsItem);

    expect(mockNavigate).toHaveBeenCalledWith('/learning');
  });

  it('Help menu - Diagnostics navigates to /diagnostics', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const helpButton = screen.getByText('Help');
    await user.click(helpButton);

    const diagnosticsItem = screen.getByText('Diagnostics');
    await user.click(diagnosticsItem);

    expect(mockNavigate).toHaveBeenCalledWith('/diagnostics');
  });

  it('Edit menu - Preferences navigates to /settings', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const editButton = screen.getByText('Edit');
    await user.click(editButton);

    const preferencesItem = screen.getByText('Preferences...');
    await user.click(preferencesItem);

    expect(mockNavigate).toHaveBeenCalledWith('/settings');
  });

  it('Help menu - Keyboard Shortcuts calls callback', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const helpButton = screen.getByText('Help');
    await user.click(helpButton);

    const shortcutsItem = screen.getAllByText('Keyboard Shortcuts')[0];
    await user.click(shortcutsItem);

    expect(mockProps.onShowKeyboardShortcuts).toHaveBeenCalled();
  });

  it('Help menu - Documentation opens external link', async () => {
    const windowOpenSpy = vi.spyOn(window, 'open').mockImplementation(() => null);
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <TopMenuBar {...mockProps} />
      </BrowserRouter>
    );

    const helpButton = screen.getByText('Help');
    await user.click(helpButton);

    const docItem = screen.getByText('Documentation');
    await user.click(docItem);

    expect(windowOpenSpy).toHaveBeenCalledWith(
      'https://github.com/Saiyan9001/aura-video-studio',
      '_blank'
    );

    windowOpenSpy.mockRestore();
  });
});
