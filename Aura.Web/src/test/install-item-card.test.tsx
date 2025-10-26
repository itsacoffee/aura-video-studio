import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { InstallItemCard } from '../components/Onboarding/InstallItemCard';
import type { InstallItem } from '../components/Onboarding/InstallItemCard';

describe('InstallItemCard', () => {
  const mockItem: InstallItem = {
    id: 'ffmpeg',
    name: 'FFmpeg (Video encoding)',
    required: true,
    installed: false,
    installing: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render install item with name and required badge', () => {
    const mockOnInstall = vi.fn();
    render(<InstallItemCard item={mockItem} onInstall={mockOnInstall} />);

    expect(screen.getByText('FFmpeg (Video encoding)')).toBeInTheDocument();
    expect(screen.getByText('Required')).toBeInTheDocument();
  });

  it('should show Install button when not installed', () => {
    const mockOnInstall = vi.fn();
    render(<InstallItemCard item={mockItem} onInstall={mockOnInstall} />);

    const installButton = screen.getByRole('button', { name: /install/i });
    expect(installButton).toBeInTheDocument();
  });

  it('should show Use Existing button when onAttachExisting is provided', () => {
    const mockOnInstall = vi.fn();
    const mockOnAttachExisting = vi.fn();

    render(
      <InstallItemCard
        item={mockItem}
        onInstall={mockOnInstall}
        onAttachExisting={mockOnAttachExisting}
      />
    );

    expect(screen.getByRole('button', { name: /use existing/i })).toBeInTheDocument();
  });

  it('should show Skip button for non-required items', () => {
    const mockOnInstall = vi.fn();
    const mockOnSkip = vi.fn();
    const optionalItem = { ...mockItem, required: false };

    render(<InstallItemCard item={optionalItem} onInstall={mockOnInstall} onSkip={mockOnSkip} />);

    expect(screen.getByRole('button', { name: /skip/i })).toBeInTheDocument();
  });

  it('should not show Skip button for required items', () => {
    const mockOnInstall = vi.fn();
    const mockOnSkip = vi.fn();

    render(<InstallItemCard item={mockItem} onInstall={mockOnInstall} onSkip={mockOnSkip} />);

    expect(screen.queryByRole('button', { name: /skip/i })).not.toBeInTheDocument();
  });

  it('should show checkmark when installed', () => {
    const mockOnInstall = vi.fn();
    const installedItem = { ...mockItem, installed: true };

    render(<InstallItemCard item={installedItem} onInstall={mockOnInstall} />);

    // Button should not be visible when installed
    expect(screen.queryByRole('button', { name: /install/i })).not.toBeInTheDocument();
  });

  it('should show spinner when installing', () => {
    const mockOnInstall = vi.fn();
    const installingItem = { ...mockItem, installing: true };

    render(<InstallItemCard item={installingItem} onInstall={mockOnInstall} />);

    // Button should not be visible when installing
    expect(screen.queryByRole('button', { name: /install/i })).not.toBeInTheDocument();
  });

  it('should call onInstall when Install button is clicked', () => {
    const mockOnInstall = vi.fn();
    render(<InstallItemCard item={mockItem} onInstall={mockOnInstall} />);

    const installButton = screen.getByRole('button', { name: /install/i });
    fireEvent.click(installButton);

    expect(mockOnInstall).toHaveBeenCalledTimes(1);
  });

  it('should call onSkip when Skip button is clicked', () => {
    const mockOnInstall = vi.fn();
    const mockOnSkip = vi.fn();
    const optionalItem = { ...mockItem, required: false };

    render(<InstallItemCard item={optionalItem} onInstall={mockOnInstall} onSkip={mockOnSkip} />);

    const skipButton = screen.getByRole('button', { name: /skip/i });
    fireEvent.click(skipButton);

    expect(mockOnSkip).toHaveBeenCalledTimes(1);
  });

  it('should open attach dialog when Use Existing button is clicked', async () => {
    const mockOnInstall = vi.fn();
    const mockOnAttachExisting = vi.fn();

    render(
      <InstallItemCard
        item={mockItem}
        onInstall={mockOnInstall}
        onAttachExisting={mockOnAttachExisting}
      />
    );

    const useExistingButton = screen.getByRole('button', { name: /use existing/i });
    fireEvent.click(useExistingButton);

    await waitFor(() => {
      expect(screen.getByText(/Use Existing FFmpeg \(Video encoding\)/i)).toBeInTheDocument();
    });
  });

  it('should require install path in attach dialog', async () => {
    const mockOnInstall = vi.fn();
    const mockOnAttachExisting = vi.fn();

    render(
      <InstallItemCard
        item={mockItem}
        onInstall={mockOnInstall}
        onAttachExisting={mockOnAttachExisting}
      />
    );

    // Open dialog
    const useExistingButton = screen.getByRole('button', { name: /use existing/i });
    fireEvent.click(useExistingButton);

    await waitFor(() => {
      expect(screen.getByText(/Use Existing FFmpeg \(Video encoding\)/i)).toBeInTheDocument();
    });

    // Try to submit without path
    const attachButton = screen.getByRole('button', { name: /attach & validate/i });
    expect(attachButton).toBeDisabled();
  });

  it('should call onAttachExisting with paths when submitted', async () => {
    const mockOnInstall = vi.fn();
    const mockOnAttachExisting = vi.fn().mockResolvedValue(undefined);

    render(
      <InstallItemCard
        item={mockItem}
        onInstall={mockOnInstall}
        onAttachExisting={mockOnAttachExisting}
      />
    );

    // Open dialog
    const useExistingButton = screen.getByRole('button', { name: /use existing/i });
    fireEvent.click(useExistingButton);

    await waitFor(() => {
      expect(screen.getByText(/Use Existing FFmpeg \(Video encoding\)/i)).toBeInTheDocument();
    });

    // Fill in path
    const installPathInput = screen.getByLabelText(/install path/i);
    fireEvent.change(installPathInput, { target: { value: 'C:\\Tools\\ffmpeg' } });

    // Fill in executable path
    const executablePathInput = screen.getByLabelText(/executable path/i);
    fireEvent.change(executablePathInput, {
      target: { value: 'C:\\Tools\\ffmpeg\\bin\\ffmpeg.exe' },
    });

    // Submit
    const attachButton = screen.getByRole('button', { name: /attach & validate/i });
    fireEvent.click(attachButton);

    await waitFor(() => {
      expect(mockOnAttachExisting).toHaveBeenCalledWith(
        'C:\\Tools\\ffmpeg',
        'C:\\Tools\\ffmpeg\\bin\\ffmpeg.exe'
      );
    });
  });
});
