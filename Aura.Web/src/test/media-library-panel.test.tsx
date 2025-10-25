import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MediaLibraryPanel } from '../components/EditorLayout/MediaLibraryPanel';
import userEvent from '@testing-library/user-event';

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('MediaLibraryPanel', () => {
  it('should render the media library header', () => {
    renderWithProvider(<MediaLibraryPanel />);
    expect(screen.getByText('Media Library')).toBeDefined();
    expect(screen.getByText('Drag clips to timeline')).toBeDefined();
  });

  it('should render the upload area', () => {
    renderWithProvider(<MediaLibraryPanel />);
    expect(screen.getByText('Drop files here or click to upload')).toBeDefined();
    expect(screen.getByText('Supports video, audio, and image files')).toBeDefined();
  });

  it('should show empty state when no clips are present', () => {
    renderWithProvider(<MediaLibraryPanel />);
    expect(screen.getByText('No clips in library')).toBeDefined();
    expect(screen.getByText('Upload files to get started')).toBeDefined();
  });

  it('should handle file upload', async () => {
    const { container } = renderWithProvider(<MediaLibraryPanel />);

    // Find the file input
    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    expect(fileInput).toBeTruthy();

    const file = new File(['dummy content'], 'test-video.mp4', { type: 'video/mp4' });
    
    await userEvent.upload(fileInput, file);

    // Wait for the clip to be processed and rendered using a more deterministic approach
    const clipCards = await new Promise<NodeListOf<Element>>((resolve) => {
      const checkForClips = () => {
        const cards = container.querySelectorAll('[draggable="true"]');
        if (cards.length > 0) {
          resolve(cards);
        } else {
          setTimeout(checkForClips, 100);
        }
      };
      checkForClips();
      // Timeout after 2 seconds to prevent infinite wait
      setTimeout(() => resolve(container.querySelectorAll('[draggable="true"]')), 2000);
    });

    expect(clipCards.length).toBeGreaterThan(0);
  });

  it('should have proper accessibility attributes on upload area', () => {
    const { container } = renderWithProvider(<MediaLibraryPanel />);
    
    const uploadArea = container.querySelector('[aria-label="Upload media files"]');
    expect(uploadArea).toBeTruthy();
    expect(uploadArea?.getAttribute('role')).toBe('button');
    expect(uploadArea?.getAttribute('tabIndex')).toBe('0');
  });

  it('should accept multiple file types', () => {
    const { container } = renderWithProvider(<MediaLibraryPanel />);
    
    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    expect(fileInput).toBeTruthy();
    expect(fileInput?.getAttribute('accept')).toBe('video/*,audio/*,image/*');
    expect(fileInput?.hasAttribute('multiple')).toBe(true);
  });
});
