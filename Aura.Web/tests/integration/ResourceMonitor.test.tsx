import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ResourceMonitor } from '../../src/components/StatusBar/ResourceMonitor';

// Mock the apiClient module
const mockGet = vi.fn();

vi.mock('../../src/services/api/apiClient', () => ({
  get: (...args: unknown[]) => mockGet(...args),
}));

describe('ResourceMonitor', () => {
  beforeEach(() => {
    mockGet.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  const validMetricsResponse = {
    timestamp: '2024-01-01T00:00:00Z',
    cpu: {
      overallUsagePercent: 45.5,
      perCoreUsagePercent: [40, 50],
      logicalCores: 8,
      physicalCores: 4,
      processUsagePercent: 10.5,
    },
    memory: {
      totalBytes: 16000000000,
      availableBytes: 8000000000,
      usedBytes: 8000000000,
      usagePercent: 50.0,
      processUsageBytes: 100000000,
      processPrivateBytes: 50000000,
      processWorkingSetBytes: 100000000,
    },
    gpu: {
      name: 'NVIDIA RTX 3080',
      vendor: 'NVIDIA',
      usagePercent: 30.0,
      totalMemoryBytes: 10000000000,
      usedMemoryBytes: 3000000000,
      availableMemoryBytes: 7000000000,
      memoryUsagePercent: 30.0,
      temperatureCelsius: 65.0,
    },
    disks: [
      {
        driveName: 'C:',
        driveLabel: 'System',
        totalBytes: 500000000000,
        availableBytes: 200000000000,
        usedBytes: 300000000000,
        usagePercent: 60.0,
        readBytesPerSecond: 1048576,
        writeBytesPerSecond: 524288,
      },
    ],
    network: {
      bytesSentPerSecond: 10000,
      bytesReceivedPerSecond: 50000,
      totalBytesSent: 1000000,
      totalBytesReceived: 5000000,
    },
  };

  describe('compact mode', () => {
    it('renders CPU, Memory, and GPU metrics in compact view', async () => {
      mockGet.mockResolvedValue(validMetricsResponse);

      render(<ResourceMonitor compact />);

      // Wait for metrics to be fetched and displayed
      await waitFor(
        () => {
          expect(screen.getByText('46%')).toBeInTheDocument(); // CPU rounded
        },
        { timeout: 3000 }
      );
      expect(screen.getByText('50%')).toBeInTheDocument(); // Memory
      expect(screen.getByText('30%')).toBeInTheDocument(); // GPU
    });

    it('shows N/A for GPU when GPU is not available', async () => {
      const metricsWithoutGpu = {
        ...validMetricsResponse,
        gpu: undefined,
      };
      mockGet.mockResolvedValue(metricsWithoutGpu);

      render(<ResourceMonitor compact />);

      await waitFor(
        () => {
          expect(screen.getByText('N/A')).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });
  });

  describe('full mode', () => {
    it('renders System Resources title', () => {
      mockGet.mockResolvedValue(validMetricsResponse);

      render(<ResourceMonitor />);

      expect(screen.getByText('System Resources')).toBeInTheDocument();
    });

    it('renders all resource labels', () => {
      mockGet.mockResolvedValue(validMetricsResponse);

      render(<ResourceMonitor />);

      expect(screen.getByText('CPU')).toBeInTheDocument();
      expect(screen.getByText('Memory')).toBeInTheDocument();
      expect(screen.getByText('GPU')).toBeInTheDocument();
      expect(screen.getByText('Disk I/O')).toBeInTheDocument();
    });
  });

  describe('defensive parsing', () => {
    it('handles null/undefined response gracefully', async () => {
      mockGet.mockResolvedValue(null);

      render(<ResourceMonitor compact />);

      // Should show 0% for CPU and Memory after parsing null response
      await waitFor(
        () => {
          // After fetching null, values should stay at initial 0%
          expect(screen.getAllByText('0%').length).toBeGreaterThan(0);
        },
        { timeout: 3000 }
      );
    });

    it('handles missing cpu property gracefully', async () => {
      const metricsWithoutCpu = {
        ...validMetricsResponse,
        cpu: undefined,
      };
      mockGet.mockResolvedValue(metricsWithoutCpu);

      render(<ResourceMonitor compact />);

      // Should not throw, component should still render
      await waitFor(
        () => {
          expect(screen.getByText('50%')).toBeInTheDocument(); // Memory still shows
        },
        { timeout: 3000 }
      );
    });

    it('handles missing memory property gracefully', async () => {
      const metricsWithoutMemory = {
        ...validMetricsResponse,
        memory: undefined,
      };
      mockGet.mockResolvedValue(metricsWithoutMemory);

      render(<ResourceMonitor compact />);

      // Should not throw, component should still render
      await waitFor(
        () => {
          expect(screen.getByText('46%')).toBeInTheDocument(); // CPU still shows
        },
        { timeout: 3000 }
      );
    });

    it('handles missing disks property gracefully', async () => {
      const metricsWithoutDisks = {
        ...validMetricsResponse,
        disks: undefined,
      };
      mockGet.mockResolvedValue(metricsWithoutDisks);

      render(<ResourceMonitor />);

      // Should not throw, Disk I/O should show 0
      await waitFor(
        () => {
          expect(screen.getByText('0.0 MB/s')).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });

    it('handles empty disks array gracefully', async () => {
      const metricsWithEmptyDisks = {
        ...validMetricsResponse,
        disks: [],
      };
      mockGet.mockResolvedValue(metricsWithEmptyDisks);

      render(<ResourceMonitor />);

      // Should not throw, Disk I/O should show 0
      await waitFor(
        () => {
          expect(screen.getByText('0.0 MB/s')).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });

    it('handles gpu object with undefined usagePercent', async () => {
      const metricsWithPartialGpu = {
        ...validMetricsResponse,
        gpu: {
          name: 'NVIDIA RTX 3080',
          vendor: 'NVIDIA',
          usagePercent: undefined,
          totalMemoryBytes: 10000000000,
        },
      };
      mockGet.mockResolvedValue(metricsWithPartialGpu);

      render(<ResourceMonitor compact />);

      // GPU should show N/A since usagePercent is undefined
      await waitFor(
        () => {
          expect(screen.getByText('N/A')).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });
  });

  describe('error handling', () => {
    it('handles API errors gracefully without crashing', async () => {
      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      mockGet.mockRejectedValue(new Error('Network error'));

      render(<ResourceMonitor compact />);

      // Wait for error to be logged
      await waitFor(
        () => {
          expect(consoleWarnSpy).toHaveBeenCalledWith(
            '[ResourceMonitor] Failed to fetch metrics:',
            'Network error'
          );
        },
        { timeout: 3000 }
      );

      consoleWarnSpy.mockRestore();
    });

    it('does not log abort errors', async () => {
      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      const abortError = new Error('Request aborted');
      abortError.name = 'AbortError';
      mockGet.mockRejectedValue(abortError);

      render(<ResourceMonitor compact />);

      // Wait a bit to ensure the error would have been logged if it was going to be
      await new Promise((resolve) => setTimeout(resolve, 500));

      expect(consoleWarnSpy).not.toHaveBeenCalled();

      consoleWarnSpy.mockRestore();
    });
  });

  describe('value clamping', () => {
    it('clamps CPU values above 100 to 100', async () => {
      const metricsWithHighCpu = {
        ...validMetricsResponse,
        cpu: { ...validMetricsResponse.cpu, overallUsagePercent: 150 },
      };
      mockGet.mockResolvedValue(metricsWithHighCpu);

      render(<ResourceMonitor compact />);

      await waitFor(
        () => {
          expect(screen.getByText('100%')).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });

    it('clamps negative values to 0', async () => {
      const metricsWithNegativeCpu = {
        ...validMetricsResponse,
        cpu: { ...validMetricsResponse.cpu, overallUsagePercent: -10 },
      };
      mockGet.mockResolvedValue(metricsWithNegativeCpu);

      render(<ResourceMonitor compact />);

      await waitFor(
        () => {
          expect(screen.getByText('0%')).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });
  });
});
