import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { FileDropzone } from '../FileDropzone';

describe('FileDropzone', () => {
  describe('File Size Validation', () => {
    it('should display 100GB maximum file size limit in UI', () => {
      const onFileSelected = vi.fn();
      render(<FileDropzone onFileSelected={onFileSelected} />);

      expect(screen.getByText(/maximum file size: 100 gb/i)).toBeInTheDocument();
    });

    it('should display supported video formats in UI', () => {
      const onFileSelected = vi.fn();
      render(<FileDropzone onFileSelected={onFileSelected} />);

      expect(screen.getByText(/supported formats:.+mp4.+mov.+avi.+mkv.+webm/i)).toBeInTheDocument();
    });

    it('should have file input that accepts correct formats', () => {
      const onFileSelected = vi.fn();
      const { container } = render(<FileDropzone onFileSelected={onFileSelected} />);

      const input = container.querySelector('input[type="file"]') as HTMLInputElement;

      expect(input).toBeTruthy();
      expect(input.accept).toContain('.mp4');
      expect(input.accept).toContain('.mov');
      expect(input.accept).toContain('.avi');
      expect(input.accept).toContain('.mp3');
      expect(input.accept).toContain('.wav');
    });
  });
});
