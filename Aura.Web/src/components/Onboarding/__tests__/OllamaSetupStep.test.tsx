import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { OllamaSetupStatus } from '../../../services/ollamaSetupService';
import { OllamaSetupStep } from '../OllamaSetupStep';

// Mock the ollama setup service
vi.mock('../../../services/ollamaSetupService', () => ({
  checkOllamaStatus: vi.fn(),
  getInstallGuide: vi.fn(() => ({
    platform: 'Windows',
    downloadUrl: 'https://ollama.com/download/windows',
    estimatedTime: '5-10 minutes',
    steps: [
      'Download the Ollama installer from ollama.com',
      'Run the installer (OllamaSetup.exe)',
      'Follow the installation wizard',
    ],
  })),
  startOllama: vi.fn(),
  getModelRecommendationsForSystem: vi.fn(() => [
    {
      name: 'llama3.2:3b',
      displayName: 'Llama 3.2 (3B)',
      size: '2.0 GB',
      sizeBytes: 2 * 1024 * 1024 * 1024,
      description: 'Fast and efficient. Best for systems with limited resources.',
      recommended: true,
    },
  ]),
}));

// Mock the notifications
vi.mock('../../Notifications/Toasts', () => ({
  useNotifications: () => ({
    showSuccessToast: vi.fn(),
    showFailureToast: vi.fn(),
  }),
}));

// Import after mock
import { checkOllamaStatus, startOllama } from '../../../services/ollamaSetupService';

function renderWithProvider(ui: React.ReactElement) {
  return render(<FluentProvider theme={webLightTheme}>{ui}</FluentProvider>);
}

describe('OllamaSetupStep', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should show loading state initially', () => {
    vi.mocked(checkOllamaStatus).mockImplementation(() => new Promise(() => {}));

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    expect(screen.getByText('Checking Ollama installation...')).toBeInTheDocument();
  });

  it('should show success message when Ollama is installed and running with models', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: true,
      running: true,
      version: '0.1.0',
      modelsInstalled: ['llama3.2:3b', 'mistral:7b'],
      recommendedModels: [],
      installationPath: '/usr/local/bin/ollama',
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Ollama Ready!')).toBeInTheDocument();
    });

    expect(
      screen.getByText(/Ollama is installed, running, and has 2 model\(s\) installed/)
    ).toBeInTheDocument();
    expect(screen.getByText('llama3.2:3b')).toBeInTheDocument();
    expect(screen.getByText('mistral:7b')).toBeInTheDocument();
  });

  it('should show start button when Ollama is installed but not running', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: true,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
      installationPath: '/usr/local/bin/ollama',
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Ollama Installed but Not Running')).toBeInTheDocument();
    });

    expect(screen.getByText('Start Ollama')).toBeInTheDocument();
  });

  it('should handle starting Ollama', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: true,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
      installationPath: '/usr/local/bin/ollama',
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);
    vi.mocked(startOllama).mockResolvedValue({
      success: true,
      message: 'Ollama started successfully',
    });

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Start Ollama')).toBeInTheDocument();
    });

    const startButton = screen.getByText('Start Ollama');
    fireEvent.click(startButton);

    await waitFor(() => {
      expect(startOllama).toHaveBeenCalled();
    });
  });

  it('should show installation guide when Ollama is not installed', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: false,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Ollama Not Installed')).toBeInTheDocument();
    });

    expect(screen.getByText(/Ollama is a free, open-source tool/)).toBeInTheDocument();
    expect(screen.getByText('Installation Guide for Windows')).toBeInTheDocument();
  });

  it('should show download button when not installed', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: false,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Download Ollama')).toBeInTheDocument();
    });
  });

  it('should show recommended models based on system specs', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: false,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Recommended Models for Your System')).toBeInTheDocument();
    });

    expect(screen.getByText('Llama 3.2 (3B)')).toBeInTheDocument();
  });

  it('should show Why Use Ollama section', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: false,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('Why Use Ollama?')).toBeInTheDocument();
    });

    expect(screen.getByText(/Free and Open Source/)).toBeInTheDocument();
    expect(screen.getByText(/Privacy/)).toBeInTheDocument();
    expect(screen.getByText(/No Internet Required/)).toBeInTheDocument();
  });

  it('should call onSetupComplete when Ollama is ready', async () => {
    const onSetupComplete = vi.fn();
    const mockStatus: OllamaSetupStatus = {
      installed: true,
      running: true,
      modelsInstalled: ['llama3.2:3b'],
      recommendedModels: [],
      installationPath: '/usr/local/bin/ollama',
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(
      <OllamaSetupStep
        availableMemoryGB={16}
        availableDiskGB={100}
        onSetupComplete={onSetupComplete}
      />
    );

    await waitFor(() => {
      expect(onSetupComplete).toHaveBeenCalled();
    });
  });

  it('should show no models warning when installed but no models', async () => {
    const mockStatus: OllamaSetupStatus = {
      installed: true,
      running: false,
      modelsInstalled: [],
      recommendedModels: [],
      installationPath: '/usr/local/bin/ollama',
    };

    vi.mocked(checkOllamaStatus).mockResolvedValue(mockStatus);

    renderWithProvider(<OllamaSetupStep availableMemoryGB={16} availableDiskGB={100} />);

    await waitFor(() => {
      expect(screen.getByText('No Models Installed')).toBeInTheDocument();
    });
  });
});
