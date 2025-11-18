import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FileDropzone } from '../FileDropzone';

describe('FileDropzone', () => {
  let onFileSelected: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    onFileSelected = vi.fn();
    vi.clearAllMocks();
  });

  describe('File Size Validation', () => {
    it('should display 100GB maximum file size limit in UI', () => {
      render(<FileDropzone onFileSelected={onFileSelected} />);

      expect(screen.getByText(/maximum file size: 100 gb/i)).toBeInTheDocument();
    });

    it('should display supported video formats in UI', () => {
      render(<FileDropzone onFileSelected={onFileSelected} />);

      expect(screen.getByText(/supported formats:.+mp4.+mov.+avi.+mkv.+webm/i)).toBeInTheDocument();
    });

    it('should have file input that accepts correct formats', () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);

      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      expect(input).toBeTruthy();
      expect(input.accept).toContain('.mp4');
      expect(input.accept).toContain('.mov');
      expect(input.accept).toContain('.avi');
      expect(input.accept).toContain('.mp3');
      expect(input.accept).toContain('.wav');
    });

    it('should display all format extensions in UI', () => {
      render(<FileDropzone onFileSelected={onFileSelected} />);
      const formatText = screen.getByText(/supported formats:/i);

      expect(formatText.textContent).toMatch(/MP4/i);
      expect(formatText.textContent).toMatch(/MOV/i);
      expect(formatText.textContent).toMatch(/AVI/i);
      expect(formatText.textContent).toMatch(/MKV/i);
      expect(formatText.textContent).toMatch(/WebM/i);
      expect(formatText.textContent).toMatch(/MP3/i);
      expect(formatText.textContent).toMatch(/WAV/i);
      expect(formatText.textContent).toMatch(/AAC/i);
      expect(formatText.textContent).toMatch(/FLAC/i);
    });
  });

  describe('Drag and Drop State', () => {
    it('should change UI when drag is active', () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;

      fireEvent.dragOver(dropzone);

      expect(screen.getByText('Drop file here')).toBeInTheDocument();
    });

    it('should reset UI when drag leaves', () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;

      fireEvent.dragOver(dropzone);
      fireEvent.dragLeave(dropzone);

      expect(screen.getByText('Drag and drop a video or audio file here')).toBeInTheDocument();
    });

    it('should not activate drag state when disabled', () => {
      const { container } = render(
        <FileDropzone onFileSelected={onFileSelected} disabled={true} />
      );
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;

      fireEvent.dragOver(dropzone);

      expect(screen.queryByText('Drop file here')).not.toBeInTheDocument();
    });
  });

  describe('Keyboard Accessibility', () => {
    it('should have correct accessibility attributes', () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;

      expect(dropzone).toHaveAttribute('tabIndex', '0');
      expect(dropzone).toHaveAttribute('role', 'button');
    });

    it('should handle Enter key press', () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;
      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      const clickSpy = vi.spyOn(input, 'click');
      fireEvent.keyDown(dropzone, { key: 'Enter' });

      expect(clickSpy).toHaveBeenCalled();
      clickSpy.mockRestore();
    });

    it('should handle Space key press', () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;
      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      const clickSpy = vi.spyOn(input, 'click');
      fireEvent.keyDown(dropzone, { key: ' ' });

      expect(clickSpy).toHaveBeenCalled();
      clickSpy.mockRestore();
    });
  });

  describe('Disabled State', () => {
    it('should not allow file input clicks when disabled', () => {
      const { container } = render(
        <FileDropzone onFileSelected={onFileSelected} disabled={true} />
      );
      const dropzone = container.querySelector('[role="button"][tabindex="0"]') as HTMLElement;
      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      const clickSpy = vi.spyOn(input, 'click');
      fireEvent.click(dropzone);

      expect(clickSpy).not.toHaveBeenCalled();
      clickSpy.mockRestore();
    });

    it('should disable file input element when disabled prop is true', () => {
      const { container } = render(
        <FileDropzone onFileSelected={onFileSelected} disabled={true} />
      );
      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      expect(input).toBeDisabled();
    });
  });

  describe('Single File Selection', () => {
    it('should display processing state after file selection', async () => {
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);
      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      const file = new File(['test content'], 'test.mp4', { type: 'video/mp4' });

      fireEvent.change(input, { target: { files: [file] } });

      await waitFor(() => {
        expect(screen.getByText('Processing file...')).toBeInTheDocument();
      });
    });
  });
});
