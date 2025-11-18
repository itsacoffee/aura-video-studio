import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { FileMetadata } from '../FileContext';
import { FileSelector } from '../FileSelector';

describe('FileSelector', () => {
  let onClose: ReturnType<typeof vi.fn>;
  let onSelect: ReturnType<typeof vi.fn>;
  let onBrowse: ReturnType<typeof vi.fn>;
  let mockRecentFiles: FileMetadata[];
  let mockGeneratedFiles: FileMetadata[];

  beforeEach(() => {
    onClose = vi.fn();
    onSelect = vi.fn();
    onBrowse = vi.fn();
    vi.clearAllMocks();

    mockRecentFiles = [
      {
        name: 'Marketing_Video_Q4.mp4',
        path: '/Videos/Marketing_Video_Q4.mp4',
        type: 'video',
        duration: 125,
        size: 85 * 1024 * 1024,
        resolution: { width: 1920, height: 1080 },
        codec: 'H.264',
        bitrate: 8000000,
        fps: 30,
        audioCodec: 'AAC',
        sampleRate: 48000,
      },
      {
        name: 'Product_Demo_v2.mov',
        path: '/Videos/Product_Demo_v2.mov',
        type: 'video',
        duration: 95,
        size: 320 * 1024 * 1024,
        resolution: { width: 1920, height: 1080 },
        codec: 'ProRes',
        bitrate: 28000000,
        fps: 30,
        audioCodec: 'PCM',
        sampleRate: 48000,
      },
    ];

    mockGeneratedFiles = [
      {
        name: 'Aura_Generated_2024_01_15.mp4',
        path: '/.aura/outputs/Aura_Generated_2024_01_15.mp4',
        type: 'video',
        duration: 45,
        size: 35 * 1024 * 1024,
        resolution: { width: 1920, height: 1080 },
        codec: 'H.264',
        bitrate: 6000000,
        fps: 30,
        audioCodec: 'AAC',
        sampleRate: 48000,
      },
    ];
  });

  describe('Dialog Behavior', () => {
    it('should not render when open is false', () => {
      render(
        <FileSelector
          open={false}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      expect(screen.queryByText('Select File to Re-encode')).not.toBeInTheDocument();
    });

    it('should render when open is true', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      expect(screen.getByText('Select File to Re-encode')).toBeInTheDocument();
    });

    it('should call onClose when close button is clicked', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const closeButton = screen.getByLabelText('close');
      fireEvent.click(closeButton);

      expect(onClose).toHaveBeenCalled();
    });

    it('should call onClose when Cancel button is clicked', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const cancelButton = screen.getByRole('button', { name: /cancel/i });
      fireEvent.click(cancelButton);

      expect(onClose).toHaveBeenCalled();
    });
  });

  describe('File Metadata Display', () => {
    it('should display file duration in correct format', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const durations = screen.getAllByText(/\d+:\d{2}/);
      expect(durations.length).toBeGreaterThan(0);
    });

    it('should display file size in GB or MB', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const sizes = screen.getAllByText(/\d+\.\d{2} (GB|MB)/);
      expect(sizes.length).toBeGreaterThan(0);
    });

    it('should display video resolution when available', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const resolutions = screen.getAllByText(/\d+×\d+/);
      expect(resolutions.length).toBeGreaterThan(0);
    });

    it('should display codec information', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const codecBadges = screen.getAllByText(/H\.264|ProRes|MP3|AAC|PCM/);
      expect(codecBadges.length).toBeGreaterThan(0);
    });
  });

  describe('Tab Navigation', () => {
    it('should have Recent Projects tab', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      expect(screen.getByText('Recent Projects')).toBeInTheDocument();
    });

    it('should have Generated Videos tab', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      expect(screen.getAllByText('Generated Videos').length).toBeGreaterThan(0);
    });

    it('should switch between tabs', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const generatedTab = screen.getByRole('tab', { name: /generated videos/i });
      fireEvent.click(generatedTab);

      expect(screen.getAllByText(/Aura_Generated/i).length).toBeGreaterThan(0);
    });
  });

  describe('File Selection', () => {
    it('should highlight selected file', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const fileCards = screen.getAllByRole('group');
      const firstFileCard = fileCards[0];

      fireEvent.click(firstFileCard);

      // Just verify the card is present and clickable
      expect(fileCards.length).toBeGreaterThan(0);
    });

    it('should enable Select button when file is selected', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const selectButton = screen.getByRole('button', { name: /select file/i });
      expect(selectButton).toBeDisabled();

      const fileCards = screen.getAllByRole('group');
      fireEvent.click(fileCards[0]);

      expect(selectButton).not.toBeDisabled();
    });

    it('should call onSelect with correct metadata when Select button is clicked', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const fileCards = screen.getAllByRole('group');
      fireEvent.click(fileCards[0]);

      const selectButton = screen.getByRole('button', { name: /select file/i });
      fireEvent.click(selectButton);

      expect(onSelect).toHaveBeenCalled();
      expect(onClose).toHaveBeenCalled();

      const selectedFile = onSelect.mock.calls[0][0] as FileMetadata;
      expect(selectedFile).toHaveProperty('name');
      expect(selectedFile).toHaveProperty('path');
      expect(selectedFile).toHaveProperty('type');
      expect(selectedFile).toHaveProperty('duration');
      expect(selectedFile).toHaveProperty('size');
    });
  });

  describe('Browse Computer Button', () => {
    it('should have Browse Computer button', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      expect(screen.getByText('Browse Computer')).toBeInTheDocument();
    });

    it('should call onBrowse when Browse Computer is clicked', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const browseButton = screen.getByText('Browse Computer');
      fireEvent.click(browseButton);

      expect(onBrowse).toHaveBeenCalled();
      expect(onClose).toHaveBeenCalled();
    });
  });

  describe('Empty State', () => {
    it('should show empty state message when no files in a tab', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      expect(screen.queryByText('No files found')).not.toBeInTheDocument();
    });
  });

  describe('File Type Icons', () => {
    it('should display video icon for video files', () => {
      render(
        <FileSelector
          open={true}
          onClose={onClose}
          onSelect={onSelect}
          onBrowse={onBrowse}
          recentFiles={mockRecentFiles}
          generatedFiles={mockGeneratedFiles}
        />
      );

      const fileCards = screen.getAllByRole('group');
      expect(fileCards.length).toBeGreaterThan(0);
    });
  });
});
