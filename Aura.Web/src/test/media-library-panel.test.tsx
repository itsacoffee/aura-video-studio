import { describe, it, expect } from 'vitest';
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
    expect(screen.getByText('Browse and manage your media assets')).toBeDefined();
  });

  it('should render dual view by default', () => {
    const { container } = renderWithProvider(<MediaLibraryPanel />);
    const tabList = container.querySelector('[role="tablist"]');
    expect(tabList).toBeTruthy();
    const tabs = container.querySelectorAll('[role="tab"]');
    expect(tabs.length).toBe(2);
  });

  it('should show empty state when no clips are present', () => {
    renderWithProvider(<MediaLibraryPanel />);
    expect(screen.getByText('No media in this collection')).toBeDefined();
    expect(screen.getByText('Add files to get started')).toBeDefined();
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

  it('should render file browser in dual view', () => {
    renderWithProvider(<MediaLibraryPanel />);
    expect(screen.getByText('File Browser')).toBeDefined();
  });

  it('should accept multiple file types', () => {
    const { container } = renderWithProvider(<MediaLibraryPanel />);
    
    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    expect(fileInput).toBeTruthy();
    expect(fileInput?.getAttribute('accept')).toBe('video/*,audio/*,image/*');
    expect(fileInput?.hasAttribute('multiple')).toBe(true);
  });
});
