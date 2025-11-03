import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import type { WorkspaceLayout } from '../../../services/workspaceLayoutService';
import { WorkspaceThumbnail } from '../WorkspaceThumbnail';

// Mock the hook
vi.mock('../../../hooks/useWorkspaceThumbnails', () => ({
  useWorkspaceThumbnail: vi.fn(() => ({
    thumbnailUrl: 'data:image/png;base64,test',
    isGenerating: false,
  })),
}));

describe('WorkspaceThumbnail', () => {
  const mockWorkspace: WorkspaceLayout = {
    id: 'test-workspace',
    name: 'Test Workspace',
    description: 'Test description',
    panelSizes: {
      propertiesWidth: 320,
      mediaLibraryWidth: 280,
      effectsLibraryWidth: 280,
      historyWidth: 320,
      previewHeight: 70,
    },
    visiblePanels: {
      properties: true,
      mediaLibrary: true,
      effects: true,
      history: true,
    },
  };

  it('should render thumbnail image when available', () => {
    render(<WorkspaceThumbnail workspace={mockWorkspace} />);

    const img = screen.getByRole('img');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('alt', 'Test Workspace');
  });

  it('should render custom badge for custom workspaces', () => {
    const customWorkspace = { ...mockWorkspace, id: 'custom-123' };
    render(<WorkspaceThumbnail workspace={customWorkspace} showCustomBadge={true} />);

    expect(screen.getByText('Custom')).toBeInTheDocument();
  });

  it('should not render custom badge for built-in workspaces', () => {
    const builtinWorkspace = { ...mockWorkspace, id: 'editing' };
    render(<WorkspaceThumbnail workspace={builtinWorkspace} showCustomBadge={true} />);

    expect(screen.queryByText('Custom')).not.toBeInTheDocument();
  });

  it('should use custom alt text when provided', () => {
    render(<WorkspaceThumbnail workspace={mockWorkspace} alt="Custom alt text" />);

    const img = screen.getByRole('img');
    expect(img).toHaveAttribute('alt', 'Custom alt text');
  });
});
