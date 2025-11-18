import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MediaUploadDialog } from '../../../src/pages/MediaLibrary/components/MediaUploadDialog';

// Mock the API
vi.mock('../../../src/api/mediaLibraryApi', () => ({
  mediaLibraryApi: {
    uploadMedia: vi.fn().mockResolvedValue({ id: 'test-id' }),
  },
}));

// Mock react-query
vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn((options) => ({
    mutate: vi.fn(),
    mutateAsync: vi.fn().mockImplementation((file: File) => {
      if (options.onSuccess) {
        options.onSuccess(null, file);
      }
      return Promise.resolve({ id: 'test-id' });
    }),
    isPending: false,
  })),
}));

describe('MediaUploadDialog', () => {
  const mockOnClose = vi.fn();
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the upload dialog', () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    expect(screen.getByText('Upload Media')).toBeInTheDocument();
    expect(screen.getByText('Drag & drop files here or click to browse')).toBeInTheDocument();
  });

  it('handles drag enter event correctly', () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const dropzone = screen.getByText('Drag & drop files here or click to browse').parentElement;
    expect(dropzone).toBeInTheDocument();

    if (dropzone) {
      fireEvent.dragEnter(dropzone, {
        dataTransfer: { files: [] },
      });

      // The dropzone should have the active class (check via className or data attribute)
      expect(dropzone.className).toContain('dropzoneActive');
    }
  });

  it('handles drag leave event correctly with depth tracking', () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const dropzone = screen.getByText('Drag & drop files here or click to browse').parentElement;
    expect(dropzone).toBeInTheDocument();

    if (dropzone) {
      // Enter once
      fireEvent.dragEnter(dropzone, {
        dataTransfer: { files: [] },
      });
      expect(dropzone.className).toContain('dropzoneActive');

      // Enter again (simulating entering a child element)
      fireEvent.dragEnter(dropzone, {
        dataTransfer: { files: [] },
      });
      expect(dropzone.className).toContain('dropzoneActive');

      // Leave once (should still be active due to depth tracking)
      fireEvent.dragLeave(dropzone, {
        dataTransfer: { files: [] },
      });
      expect(dropzone.className).toContain('dropzoneActive');

      // Leave again (now should be inactive)
      fireEvent.dragLeave(dropzone, {
        dataTransfer: { files: [] },
      });
      expect(dropzone.className).not.toContain('dropzoneActive');
    }
  });

  it('handles drop event and resets drag state', () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const dropzone = screen.getByText('Drag & drop files here or click to browse').parentElement;
    expect(dropzone).toBeInTheDocument();

    if (dropzone) {
      const file1 = new File(['content1'], 'test1.mp4', { type: 'video/mp4' });

      fireEvent.dragEnter(dropzone, {
        dataTransfer: { files: [] },
      });
      expect(dropzone.className).toContain('dropzoneActive');

      fireEvent.drop(dropzone, {
        dataTransfer: { files: [file1] },
      });

      // Drag state should be reset
      expect(dropzone.className).not.toContain('dropzoneActive');

      // File should be added
      waitFor(() => {
        expect(screen.getByText('test1.mp4')).toBeInTheDocument();
      });
    }
  });

  it('deduplicates files on drop', async () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const dropzone = screen.getByText('Drag & drop files here or click to browse').parentElement;
    expect(dropzone).toBeInTheDocument();

    if (dropzone) {
      const file1 = new File(['content1'], 'test.mp4', { 
        type: 'video/mp4',
        lastModified: 1000000 
      });
      const file2 = new File(['content1'], 'test.mp4', { 
        type: 'video/mp4',
        lastModified: 1000000 
      });
      
      // Change the lastModified after creation to match
      Object.defineProperty(file1, 'lastModified', { value: 1000000 });
      Object.defineProperty(file2, 'lastModified', { value: 1000000 });

      // Drop first file
      fireEvent.drop(dropzone, {
        dataTransfer: { files: [file1] },
      });

      await waitFor(() => {
        expect(screen.getByText('test.mp4')).toBeInTheDocument();
      });

      // Drop duplicate file
      fireEvent.drop(dropzone, {
        dataTransfer: { files: [file2] },
      });

      // Should still only show one file
      await waitFor(() => {
        const fileItems = screen.getAllByText('test.mp4');
        expect(fileItems.length).toBe(1);
      });
    }
  });

  it('opens file picker on dropzone click', () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const dropzone = screen.getByText('Drag & drop files here or click to browse').parentElement;
    expect(dropzone).toBeInTheDocument();

    if (dropzone) {
      const input = dropzone.querySelector('input[type="file"]');
      expect(input).toBeInTheDocument();

      const clickSpy = vi.spyOn(input as HTMLElement, 'click');
      fireEvent.click(dropzone);

      expect(clickSpy).toHaveBeenCalled();
    }
  });

  it('clears input value after file selection', async () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(input).toBeInTheDocument();

    const file = new File(['content'], 'test.mp4', { type: 'video/mp4' });
    Object.defineProperty(input, 'files', {
      value: [file],
      writable: false,
    });

    fireEvent.change(input);

    await waitFor(() => {
      expect(input.value).toBe('');
    });
  });

  it('shows Add more files button when files are selected', async () => {
    render(
      <MediaUploadDialog onClose={mockOnClose} onSuccess={mockOnSuccess} />
    );

    const dropzone = screen.getByText('Drag & drop files here or click to browse').parentElement;
    expect(dropzone).toBeInTheDocument();

    if (dropzone) {
      const file = new File(['content'], 'test.mp4', { type: 'video/mp4' });

      fireEvent.drop(dropzone, {
        dataTransfer: { files: [file] },
      });

      await waitFor(() => {
        expect(screen.getByText('Add more files')).toBeInTheDocument();
      });
    }
  });
});
