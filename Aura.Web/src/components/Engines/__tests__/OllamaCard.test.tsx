import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { OllamaCard } from '../OllamaCard';
import { ollamaClient } from '@/services/api/ollamaClient';

// Mock the Ollama detection hook
vi.mock('../../../hooks/useOllamaDetection', () => ({
  useOllamaDetection: () => ({
    isDetected: false,
    isChecking: false,
    detect: vi.fn(),
  }),
}));

// Mock the Ollama client
vi.mock('@/services/api/ollamaClient', () => ({
  ollamaClient: {
    getStatus: vi.fn(),
    start: vi.fn(),
    stop: vi.fn(),
    getModels: vi.fn(),
    getRecommendedModels: vi.fn(),
    pullModel: vi.fn(),
    deleteModel: vi.fn(),
    install: vi.fn(),
    checkModelAvailable: vi.fn(),
  },
}));

describe('OllamaCard', () => {
  const baseStatus = {
    running: false,
    installed: false,
    pid: null,
    managedByApp: false,
    model: null,
    error: null,
    installPath: null,
    version: null,
  };

  const recommendedModels = [
    {
      name: 'llama3.2:3b',
      displayName: 'Llama 3.2 (3B)',
      description: 'Fast and efficient.',
      size: '2.0 GB',
      sizeBytes: 2 * 1024 * 1024 * 1024,
      isRecommended: true,
    },
    {
      name: 'llama3.1:8b',
      displayName: 'Llama 3.1 (8B)',
      description: 'Balanced performance.',
      size: '4.7 GB',
      sizeBytes: 4.7 * 1024 * 1024 * 1024,
      isRecommended: true,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(ollamaClient.getStatus).mockResolvedValue(baseStatus);
    vi.mocked(ollamaClient.getModels).mockResolvedValue({ models: [] });
    vi.mocked(ollamaClient.getRecommendedModels).mockResolvedValue({ models: recommendedModels });
  });

  it('renders the Ollama card with title', async () => {
    render(<OllamaCard />);
    await waitFor(() => expect(screen.getByText('Ollama (Local AI)')).toBeInTheDocument());
  });

  it('shows Optional badge', async () => {
    render(<OllamaCard />);
    await waitFor(() => expect(screen.getByText('Optional')).toBeInTheDocument());
  });

  it('shows Install button when not installed', async () => {
    render(<OllamaCard />);
    await waitFor(() => expect(screen.getByText('Install Ollama')).toBeInTheDocument());
  });

  it('shows Auto-Detect button', async () => {
    render(<OllamaCard />);
    await waitFor(() => expect(screen.getByText('Auto-Detect')).toBeInTheDocument());
  });

  it('calls getStatus on mount', async () => {
    render(<OllamaCard />);
    await waitFor(() => expect(ollamaClient.getStatus).toHaveBeenCalled());
  });

  it('calls getRecommendedModels on mount', async () => {
    render(<OllamaCard />);
    await waitFor(() => expect(ollamaClient.getRecommendedModels).toHaveBeenCalled());
  });

  it('shows Start Ollama button when installed but not running', async () => {
    vi.mocked(ollamaClient.getStatus).mockResolvedValue({
      ...baseStatus,
      installed: true,
      running: false,
    });

    render(<OllamaCard />);
    await waitFor(() => expect(screen.getByText('Start Ollama')).toBeInTheDocument());
  });

  it('renders description text', () => {
    render(<OllamaCard />);
    expect(
      screen.getAllByText(/Run AI models locally for script generation/i).length
    ).toBeGreaterThan(0);
  });
});
