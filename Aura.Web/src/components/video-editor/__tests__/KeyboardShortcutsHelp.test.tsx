import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { keyboardShortcutManager } from '../../../services/keyboardShortcutManager';
import { KeyboardShortcutsHelp } from '../KeyboardShortcutsHelp';

describe('KeyboardShortcutsHelp', () => {
  const mockOnClose = vi.fn();

  beforeEach(() => {
    keyboardShortcutManager.clear();
    mockOnClose.mockClear();
  });

  it('should render when open', () => {
    render(<KeyboardShortcutsHelp open={true} onClose={mockOnClose} />);

    expect(screen.getByText('Keyboard Shortcuts')).toBeInTheDocument();
  });

  it('should not render when closed', () => {
    const { container } = render(<KeyboardShortcutsHelp open={false} onClose={mockOnClose} />);

    expect(container.firstChild).toBeNull();
  });

  it('should display shortcuts from keyboard shortcut manager', () => {
    keyboardShortcutManager.register({
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'global',
      handler: vi.fn(),
    });

    render(<KeyboardShortcutsHelp open={true} onClose={mockOnClose} />);

    expect(screen.getByText('Test shortcut')).toBeInTheDocument();
    expect(screen.getByText('Ctrl')).toBeInTheDocument();
    expect(screen.getByText('T')).toBeInTheDocument();
  });

  it('should filter shortcuts based on search query', () => {
    keyboardShortcutManager.registerMultiple([
      {
        id: 'shortcut-1',
        keys: 'Ctrl+A',
        description: 'Select all',
        context: 'global',
        handler: vi.fn(),
      },
      {
        id: 'shortcut-2',
        keys: 'Ctrl+S',
        description: 'Save project',
        context: 'global',
        handler: vi.fn(),
      },
    ]);

    render(<KeyboardShortcutsHelp open={true} onClose={mockOnClose} />);

    const searchInput = screen.getByPlaceholderText('Search shortcuts...');
    fireEvent.change(searchInput, { target: { value: 'save' } });

    expect(screen.getByText('Save project')).toBeInTheDocument();
    expect(screen.queryByText('Select all')).not.toBeInTheDocument();
  });

  it('should display no results message when search has no matches', () => {
    keyboardShortcutManager.register({
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'global',
      handler: vi.fn(),
    });

    render(<KeyboardShortcutsHelp open={true} onClose={mockOnClose} />);

    const searchInput = screen.getByPlaceholderText('Search shortcuts...');
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });

    expect(screen.getByText(/No shortcuts found matching/)).toBeInTheDocument();
  });

  it('should group shortcuts by context', () => {
    keyboardShortcutManager.registerMultiple([
      {
        id: 'global-shortcut',
        keys: 'Ctrl+A',
        description: 'Global action',
        context: 'global',
        handler: vi.fn(),
      },
      {
        id: 'editor-shortcut',
        keys: 'Ctrl+E',
        description: 'Editor action',
        context: 'video-editor',
        handler: vi.fn(),
      },
    ]);

    render(<KeyboardShortcutsHelp open={true} onClose={mockOnClose} />);

    expect(screen.getByText('Global')).toBeInTheDocument();
    expect(screen.getByText('Video Editor')).toBeInTheDocument();
  });

  it('should call onClose when close button is clicked', () => {
    render(<KeyboardShortcutsHelp open={true} onClose={mockOnClose} />);

    const closeButton = screen.getByLabelText('Close');
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });
});
