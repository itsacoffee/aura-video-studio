import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ImportWorkspaceDialog } from '../../src/components/video-editor/ImportWorkspaceDialog';
import * as workspaceLayoutService from '../../src/services/workspaceLayoutService';
import * as workspaceImportExport from '../../src/utils/workspaceImportExport';

vi.mock('../../src/services/workspaceLayoutService', () => ({
  importWorkspaceLayout: vi.fn(),
}));

vi.mock('../../src/utils/workspaceImportExport', async () => {
  const actual = await vi.importActual('../../src/utils/workspaceImportExport');
  return {
    ...actual,
    readFileAsText: vi.fn(),
  };
});

describe('ImportWorkspaceDialog', () => {
  const mockOnClose = vi.fn();
  const mockOnImportComplete = vi.fn();

  const validWorkspaceContent = JSON.stringify({
    version: '1.0',
    name: 'Test Workspace',
    description: 'A test workspace',
    created: new Date().toISOString(),
    modified: new Date().toISOString(),
    layout: {
      mediaLibrary: { visible: true, width: '280px', collapsed: false },
      effectsLibrary: { visible: true, width: '280px', collapsed: false },
      preview: { visible: true, width: '60%', collapsed: false },
      properties: { visible: true, width: '320px', collapsed: false },
      timeline: { visible: true, height: '70%', collapsed: false },
      history: { visible: true, width: '320px', collapsed: false },
    },
    shortcuts: {},
  });

  const validBundleContent = JSON.stringify({
    version: '1.0',
    created: new Date().toISOString(),
    workspaces: [
      {
        version: '1.0',
        name: 'Workspace 1',
        description: 'First workspace',
        created: new Date().toISOString(),
        modified: new Date().toISOString(),
        layout: {
          mediaLibrary: { visible: true, width: '280px', collapsed: false },
          effectsLibrary: { visible: true, width: '280px', collapsed: false },
          preview: { visible: true, width: '60%', collapsed: false },
          properties: { visible: true, width: '320px', collapsed: false },
          timeline: { visible: true, height: '70%', collapsed: false },
          history: { visible: true, width: '320px', collapsed: false },
        },
        shortcuts: {},
      },
      {
        version: '1.0',
        name: 'Workspace 2',
        description: 'Second workspace',
        created: new Date().toISOString(),
        modified: new Date().toISOString(),
        layout: {
          mediaLibrary: { visible: true, width: '280px', collapsed: false },
          effectsLibrary: { visible: true, width: '280px', collapsed: false },
          preview: { visible: true, width: '60%', collapsed: false },
          properties: { visible: true, width: '320px', collapsed: false },
          timeline: { visible: true, height: '70%', collapsed: false },
          history: { visible: true, width: '320px', collapsed: false },
        },
        shortcuts: {},
      },
    ],
  });

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Drag & Drop Handling', () => {
    it('shows drop zone when dialog is open', () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      expect(screen.getByText('Drag and drop a workspace file here')).toBeInTheDocument();
    });

    it('changes visual style when dragging over', () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByText('Drag and drop a workspace file here').parentElement;

      fireEvent.dragOver(dropZone!);
      expect(dropZone).toHaveStyle({ borderColor: expect.any(String) });
    });

    it('processes valid .workspace file on drop', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      mockReadFileAsText.mockResolvedValue(validWorkspaceContent);

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByText('Drag and drop a workspace file here').parentElement!;
      const file = new File([validWorkspaceContent], 'test.workspace', {
        type: 'application/json',
      });

      const dropEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      };

      fireEvent.drop(dropZone, dropEvent);

      expect(dropEvent.preventDefault).toHaveBeenCalled();
      expect(dropEvent.stopPropagation).toHaveBeenCalled();

      await waitFor(() => {
        expect(screen.getByText(/Test Workspace/)).toBeInTheDocument();
      });
    });

    it('processes valid .workspace-bundle file on drop', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      mockReadFileAsText.mockResolvedValue(validBundleContent);

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file = new File([validBundleContent], 'test.workspace-bundle', {
        type: 'application/json',
      });

      const dropEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      };

      fireEvent.drop(dropZone, dropEvent);

      expect(dropEvent.preventDefault).toHaveBeenCalled();
      expect(dropEvent.stopPropagation).toHaveBeenCalled();

      await waitFor(() => {
        expect(screen.getByText(/Workspace 1/)).toBeInTheDocument();
        expect(screen.getByText(/Workspace 2/)).toBeInTheDocument();
      });
    });

    it('shows error for invalid file type on drop', async () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file = new File(['content'], 'test.txt', { type: 'text/plain' });

      const dropEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      };

      fireEvent.drop(dropZone, dropEvent);

      await waitFor(() => {
        expect(
          screen.getByText(
            /Invalid file type. Please select a .workspace or .workspace-bundle file./
          )
        ).toBeInTheDocument();
      });
    });
  });

  describe('File Input Handling', () => {
    it('allows browsing for file via click', () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      fireEvent.click(dropZone);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      expect(fileInput).toBeDefined();
      expect(fileInput.accept).toBe('.workspace,.workspace-bundle');
    });

    it('allows browsing via keyboard (Enter key)', () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByText('Drag and drop a workspace file here').parentElement!;
      fireEvent.keyDown(dropZone, { key: 'Enter' });

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      expect(fileInput).toBeDefined();
    });

    it('allows browsing via keyboard (Space key)', () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByText('Drag and drop a workspace file here').parentElement!;
      const event = { key: ' ', preventDefault: vi.fn() };
      fireEvent.keyDown(dropZone, event);

      expect(event.preventDefault).toHaveBeenCalled();
    });
  });

  describe('File Parsing and Validation', () => {
    it('shows error for empty file', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      mockReadFileAsText.mockResolvedValue('');

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file = new File([''], 'empty.workspace', { type: 'application/json' });

      const dropEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      };

      fireEvent.drop(dropZone, dropEvent);

      await waitFor(() => {
        expect(screen.getByText(/The file appears to be empty or unreadable/)).toBeInTheDocument();
      });
    });

    it('shows error for corrupted JSON', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      mockReadFileAsText.mockResolvedValue('{ invalid json }');

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file = new File(['{ invalid json }'], 'corrupted.workspace', {
        type: 'application/json',
      });

      const dropEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      };

      fireEvent.drop(dropZone, dropEvent);

      await waitFor(() => {
        expect(
          screen.getByText(/Could not parse workspace file. Ensure the file is not corrupted./)
        ).toBeInTheDocument();
      });
    });

    it('shows error for file with zero size', async () => {
      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file = new File([], 'empty.workspace', { type: 'application/json' });
      Object.defineProperty(file, 'size', { value: 0 });

      const dropEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      };

      fireEvent.drop(dropZone, dropEvent);

      await waitFor(() => {
        expect(
          screen.getByText(/The selected file is empty. Please choose a valid workspace file./)
        ).toBeInTheDocument();
      });
    });
  });

  describe('State Reset', () => {
    it('resets state when choosing different file', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      mockReadFileAsText.mockResolvedValue(validWorkspaceContent);

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file1 = new File([validWorkspaceContent], 'test1.workspace', {
        type: 'application/json',
      });

      fireEvent.drop(dropZone, {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file1] },
      });

      await waitFor(() => {
        expect(screen.getByText(/Test Workspace/)).toBeInTheDocument();
      });

      const chooseDifferentButton = screen.getByText('Choose Different File');
      fireEvent.click(chooseDifferentButton);

      await waitFor(() => {
        expect(screen.getByText('Drag and drop a workspace file here')).toBeInTheDocument();
      });
    });

    it('resets state before processing new file', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);

      mockReadFileAsText.mockResolvedValueOnce('{ invalid }');

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByText('Drag and drop a workspace file here').parentElement!;
      const file1 = new File(['{ invalid }'], 'bad.workspace', {
        type: 'application/json',
      });

      fireEvent.drop(dropZone, {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file1] },
      });

      await waitFor(() => {
        expect(screen.getByText(/Could not parse workspace file/)).toBeInTheDocument();
      });

      mockReadFileAsText.mockResolvedValueOnce(validWorkspaceContent);

      const chooseDifferentButton = screen.getByText('Choose Different File');
      fireEvent.click(chooseDifferentButton);

      await waitFor(() => {
        expect(screen.queryByText(/Could not parse workspace file/)).not.toBeInTheDocument();
      });
    });
  });

  describe('Multiple Workspace Selection', () => {
    it('allows selecting and deselecting workspaces in bundle', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      mockReadFileAsText.mockResolvedValue(validBundleContent);

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByRole('button');
      const file = new File([validBundleContent], 'bundle.workspace-bundle', {
        type: 'application/json',
      });

      fireEvent.drop(dropZone, {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      });

      await waitFor(() => {
        expect(screen.getByText(/Select Workspaces to Import \(2 of 2\)/)).toBeInTheDocument();
      });

      const checkboxes = screen.getAllByRole('checkbox');
      expect(checkboxes).toHaveLength(2);
      expect(checkboxes[0]).toBeChecked();
      expect(checkboxes[1]).toBeChecked();

      fireEvent.click(checkboxes[0]);

      await waitFor(() => {
        expect(screen.getByText(/Select Workspaces to Import \(1 of 2\)/)).toBeInTheDocument();
      });
    });

    it('allows importing selected workspaces only', async () => {
      const mockReadFileAsText = vi.mocked(workspaceImportExport.readFileAsText);
      const mockImportWorkspaceLayout = vi.mocked(workspaceLayoutService.importWorkspaceLayout);
      mockReadFileAsText.mockResolvedValue(validBundleContent);

      render(
        <ImportWorkspaceDialog
          open={true}
          onClose={mockOnClose}
          onImportComplete={mockOnImportComplete}
        />
      );

      const dropZone = screen.getByText('Drag and drop a workspace file here').parentElement!;
      const file = new File([validBundleContent], 'bundle.workspace-bundle', {
        type: 'application/json',
      });

      fireEvent.drop(dropZone, {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        dataTransfer: { files: [file] },
      });

      await waitFor(() => {
        expect(screen.getByText(/Workspace 1/)).toBeInTheDocument();
      });

      const checkboxes = screen.getAllByRole('checkbox');
      fireEvent.click(checkboxes[1]);

      const importButton = screen.getByText('Import');
      fireEvent.click(importButton);

      await waitFor(() => {
        expect(mockImportWorkspaceLayout).toHaveBeenCalledTimes(1);
        expect(mockOnImportComplete).toHaveBeenCalled();
      });
    });
  });
});
