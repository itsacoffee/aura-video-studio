import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FFmpegCard } from '../FFmpegCard';
import { ffmpegClient } from '@/services/api/ffmpegClient';

vi.mock('../../Notifications/Toasts', () => ({
  useNotifications: () => ({
    showSuccessToast: vi.fn(),
    showFailureToast: vi.fn(),
  }),
}));

vi.mock('@/services/api/ffmpegClient', () => ({
  ffmpegClient: {
    getStatusExtended: vi.fn(),
    install: vi.fn(),
    rescan: vi.fn(),
    useExisting: vi.fn(),
    directCheck: vi.fn(),
  },
}));

vi.mock('../ManualInstallationModal', () => ({
  ManualInstallationModal: () => <div>Manual Installation Modal</div>,
}));

describe('FFmpegCard', () => {
  const baseStatus = {
    installed: true,
    valid: true,
    version: '6.0',
    path: '/usr/bin/ffmpeg',
    source: 'Managed',
    error: null,
    errorCode: null,
    errorMessage: null,
    attemptedPaths: [],
    versionMeetsRequirement: true,
    minimumVersion: '4.0',
    hardwareAcceleration: {
      nvencSupported: true,
      amfSupported: false,
      quickSyncSupported: false,
      videoToolboxSupported: false,
      availableEncoders: ['h264_nvenc'],
    },
    correlationId: 'status-123',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(ffmpegClient.getStatusExtended).mockResolvedValue(baseStatus);
  });

  it('renders loading state initially', () => {
    vi.mocked(ffmpegClient.getStatusExtended).mockReturnValue(new Promise(() => {}));
    render(<FFmpegCard />);
    expect(screen.getByText(/Loading FFmpeg status/i)).toBeInTheDocument();
  });

  it('shows installed badge when status is ready', async () => {
    render(<FFmpegCard />);
    await waitFor(() => expect(screen.getByText('Installed')).toBeInTheDocument());
    expect(ffmpegClient.getStatusExtended).toHaveBeenCalled();
  });

  it('shows outdated badge when version is below requirement', async () => {
    vi.mocked(ffmpegClient.getStatusExtended).mockResolvedValue({
      ...baseStatus,
      versionMeetsRequirement: false,
    });

    render(<FFmpegCard />);
    await waitFor(() => expect(screen.getByText(/Outdated/i)).toBeInTheDocument());
    expect(screen.getByText(/Requires 4\.0\+/i)).toBeInTheDocument();
  });

  it('renders error banner when status call fails', async () => {
    vi.mocked(ffmpegClient.getStatusExtended).mockRejectedValue(new Error('boom'));

    render(<FFmpegCard />);

    await waitFor(() => expect(screen.getByText(/boom/i)).toBeInTheDocument());
  });

  it('loads technical details when toggled', async () => {
    const mockDirectCheck = {
      candidates: [
        {
          label: 'EnvVar',
          path: '/custom/ffmpeg',
          exists: true,
          executionAttempted: true,
          exitCode: 0,
          timedOut: false,
          rawVersionOutput: 'ffmpeg version 6.0',
          versionParsed: '6.0',
          valid: true,
          error: null,
        },
      ],
      overall: {
        installed: true,
        valid: true,
        source: 'EnvVar',
        chosenPath: '/custom/ffmpeg',
        version: '6.0',
      },
      correlationId: 'diag-123',
    };

    vi.mocked(ffmpegClient.directCheck).mockResolvedValue(mockDirectCheck);

    render(<FFmpegCard />);
    await waitFor(() => expect(screen.getByText('Installed')).toBeInTheDocument());

    await userEvent.click(screen.getByRole('button', { name: /Show Technical Details/i }));

    await waitFor(() => expect(screen.getByText(/Candidate Diagnostics/i)).toBeInTheDocument());
    expect(screen.getAllByText(/EnvVar/).length).toBeGreaterThan(0);
    expect(ffmpegClient.directCheck).toHaveBeenCalled();
  });
});
