import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { SystemRequirements } from '../../../services/systemRequirementsService';
import { SystemRequirementsCheck } from '../SystemRequirementsCheck';

// Mock the system requirements service
vi.mock('../../../services/systemRequirementsService', () => ({
  checkSystemRequirements: vi.fn(),
  getSystemRecommendations: vi.fn(() => []),
}));

// Import after mock
import {
  checkSystemRequirements,
  getSystemRecommendations,
} from '../../../services/systemRequirementsService';

const mockRequirements: SystemRequirements = {
  diskSpace: {
    available: 100,
    total: 500,
    percentage: 20,
    status: 'pass',
    warnings: [],
  },
  gpu: {
    detected: true,
    vendor: 'NVIDIA',
    model: 'GeForce RTX 3060',
    memory: 6144,
    capabilities: {
      hardwareAcceleration: true,
      videoEncoding: true,
      videoDecoding: true,
    },
    status: 'pass',
    recommendations: [],
  },
  memory: {
    total: 16,
    available: 10,
    percentage: 62.5,
    status: 'pass',
    warnings: [],
  },
  os: {
    platform: 'Windows',
    version: '10',
    architecture: 'x64',
    compatible: true,
  },
  overall: 'pass',
};

function renderWithProvider(ui: React.ReactElement) {
  return render(<FluentProvider theme={webLightTheme}>{ui}</FluentProvider>);
}

describe('SystemRequirementsCheck', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should show loading state initially', () => {
    vi.mocked(checkSystemRequirements).mockImplementation(() => new Promise(() => {}));

    renderWithProvider(<SystemRequirementsCheck />);

    expect(screen.getByText('Checking system requirements...')).toBeInTheDocument();
  });

  it('should display system requirements after loading', async () => {
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('System Requirements Check')).toBeInTheDocument();
    });

    expect(
      screen.getByText('Your system meets all requirements for video generation.')
    ).toBeInTheDocument();
  });

  it('should show disk space information', async () => {
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('Disk Space')).toBeInTheDocument();
    });

    expect(screen.getByText(/100\.00 GB \/ 500\.00 GB/)).toBeInTheDocument();
  });

  it('should show GPU information when detected', async () => {
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('Graphics Card (GPU)')).toBeInTheDocument();
    });

    expect(screen.getByText('NVIDIA - GeForce RTX 3060')).toBeInTheDocument();
  });

  it('should show memory information', async () => {
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('System Memory (RAM)')).toBeInTheDocument();
    });

    expect(screen.getByText(/16\.00 GB/)).toBeInTheDocument();
  });

  it('should show OS information', async () => {
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('Operating System')).toBeInTheDocument();
    });

    expect(screen.getByText('Windows')).toBeInTheDocument();
  });

  it('should show warnings for insufficient disk space', async () => {
    const warningRequirements: SystemRequirements = {
      ...mockRequirements,
      diskSpace: {
        available: 5,
        total: 100,
        percentage: 5,
        status: 'fail',
        warnings: ['Less than 10GB available. Video generation requires significant disk space.'],
      },
      overall: 'fail',
    };

    vi.mocked(checkSystemRequirements).mockResolvedValue(warningRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText(/Less than 10GB available/)).toBeInTheDocument();
    });
  });

  it('should show warnings when no GPU detected', async () => {
    const noGpuRequirements: SystemRequirements = {
      ...mockRequirements,
      gpu: {
        detected: false,
        capabilities: {
          hardwareAcceleration: false,
          videoEncoding: false,
          videoDecoding: false,
        },
        status: 'warning',
        recommendations: ['No dedicated GPU detected. Video encoding will use CPU (slower).'],
      },
      overall: 'warning',
    };

    vi.mocked(checkSystemRequirements).mockResolvedValue(noGpuRequirements);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('No dedicated GPU detected')).toBeInTheDocument();
    });
  });

  it('should show recommendations card when recommendations exist', async () => {
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);
    vi.mocked(getSystemRecommendations).mockReturnValue([
      'Enable NVENC hardware acceleration in Settings for optimal performance.',
    ]);

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('Recommendations')).toBeInTheDocument();
    });

    expect(screen.getByText(/Enable NVENC hardware acceleration/)).toBeInTheDocument();
  });

  it('should call onCheckComplete callback when requirements are loaded', async () => {
    const onCheckComplete = vi.fn();
    vi.mocked(checkSystemRequirements).mockResolvedValue(mockRequirements);

    renderWithProvider(<SystemRequirementsCheck onCheckComplete={onCheckComplete} />);

    await waitFor(() => {
      expect(onCheckComplete).toHaveBeenCalledWith(mockRequirements);
    });
  });

  it('should handle errors gracefully', async () => {
    vi.mocked(checkSystemRequirements).mockRejectedValue(new Error('Failed to check requirements'));

    renderWithProvider(<SystemRequirementsCheck />);

    await waitFor(() => {
      expect(screen.getByText('Error Checking Requirements')).toBeInTheDocument();
    });

    expect(screen.getByText('Failed to check requirements')).toBeInTheDocument();
  });
});
