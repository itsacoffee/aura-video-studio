import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  makeStyles,
  tokens,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Table,
  TableBody,
  TableCell,
  TableRow,
  Checkbox,
} from '@fluentui/react-components';
import { useState, useRef } from 'react';
import { importWorkspaceLayout } from '../../services/workspaceLayoutService';
import type { WorkspaceExportFormat } from '../../types/workspace.types';
import {
  readFileAsText,
  parseWorkspaceJSON,
  parseWorkspaceBundleJSON,
  validateWorkspaceFormat,
  validateWorkspaceBundle,
  exportFormatToWorkspace,
} from '../../utils/workspaceImportExport';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    minHeight: '300px',
  },
  dropZone: {
    border: `2px dashed ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center' as const,
    cursor: 'pointer',
    transition: 'all 0.2s',
  },
  fileInput: {
    display: 'none',
  },
  preview: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  workspaceList: {
    maxHeight: '300px',
    overflowY: 'auto',
  },
  errorList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
  },
  errorItem: {
    padding: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
  },
});

interface ImportWorkspaceDialogProps {
  open: boolean;
  onClose: () => void;
  onImportComplete: () => void;
}

export function ImportWorkspaceDialog({
  open,
  onClose,
  onImportComplete,
}: ImportWorkspaceDialogProps) {
  const styles = useStyles();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [parsedWorkspaces, setParsedWorkspaces] = useState<WorkspaceExportFormat[]>([]);
  const [selectedWorkspaces, setSelectedWorkspaces] = useState<Set<number>>(new Set());
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [validationWarnings, setValidationWarnings] = useState<string[]>([]);
  const [isImporting, setIsImporting] = useState(false);

  const handleReset = () => {
    setSelectedFile(null);
    setParsedWorkspaces([]);
    setSelectedWorkspaces(new Set());
    setValidationErrors([]);
    setValidationWarnings([]);
    setIsImporting(false);
  };

  const handleFileSelect = async (file: File) => {
    handleReset();
    setSelectedFile(file);

    try {
      const content = await readFileAsText(file);

      if (file.name.endsWith('.workspace-bundle')) {
        const bundle = parseWorkspaceBundleJSON(content);
        if (bundle) {
          const validation = validateWorkspaceBundle(bundle);
          setValidationErrors(validation.errors);
          setValidationWarnings(validation.warnings);

          if (validation.valid) {
            setParsedWorkspaces(bundle.workspaces);
            setSelectedWorkspaces(new Set(bundle.workspaces.map((_, i) => i)));
          }
        } else {
          setValidationErrors(['Failed to parse workspace bundle file']);
        }
      } else {
        const workspace = parseWorkspaceJSON(content);
        if (workspace) {
          const validation = validateWorkspaceFormat(workspace);
          setValidationErrors(validation.errors);
          setValidationWarnings(validation.warnings);

          if (validation.valid) {
            setParsedWorkspaces([workspace]);
            setSelectedWorkspaces(new Set([0]));
          }
        } else {
          setValidationErrors(['Failed to parse workspace file']);
        }
      }
    } catch (error) {
      console.error('Error reading file:', error);
      setValidationErrors([error instanceof Error ? error.message : 'Unknown error occurred']);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    const file = e.dataTransfer.files[0];
    if (file && (file.name.endsWith('.workspace') || file.name.endsWith('.workspace-bundle'))) {
      handleFileSelect(file);
    }
  };

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  };

  const handleBrowseClick = () => {
    fileInputRef.current?.click();
  };

  const toggleWorkspaceSelection = (index: number) => {
    const newSelection = new Set(selectedWorkspaces);
    if (newSelection.has(index)) {
      newSelection.delete(index);
    } else {
      newSelection.add(index);
    }
    setSelectedWorkspaces(newSelection);
  };

  const handleImport = async () => {
    setIsImporting(true);

    try {
      const workspacesToImport = parsedWorkspaces.filter((_, index) =>
        selectedWorkspaces.has(index)
      );

      for (const workspace of workspacesToImport) {
        const layout = exportFormatToWorkspace(workspace);
        importWorkspaceLayout(layout);
      }

      onImportComplete();
      handleReset();
    } catch (error) {
      console.error('Error importing workspaces:', error);
      setValidationErrors([error instanceof Error ? error.message : 'Failed to import workspaces']);
    } finally {
      setIsImporting(false);
    }
  };

  const canImport =
    parsedWorkspaces.length > 0 && selectedWorkspaces.size > 0 && validationErrors.length === 0;

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface style={{ maxWidth: '700px' }}>
        <DialogBody>
          <DialogTitle>Import Workspace</DialogTitle>
          <DialogContent className={styles.content}>
            {!selectedFile ? (
              <>
                <div
                  role="button"
                  tabIndex={0}
                  className={styles.dropZone}
                  style={
                    isDragging
                      ? {
                          borderColor: tokens.colorBrandStroke1,
                          backgroundColor: tokens.colorBrandBackground2,
                        }
                      : undefined
                  }
                  onDragOver={handleDragOver}
                  onDragLeave={handleDragLeave}
                  onDrop={handleDrop}
                  onClick={handleBrowseClick}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      handleBrowseClick();
                    }
                  }}
                >
                  <p>Drag and drop a workspace file here</p>
                  <p
                    style={{
                      fontSize: tokens.fontSizeBase200,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    or click to browse
                  </p>
                  <p
                    style={{
                      fontSize: tokens.fontSizeBase200,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Supported formats: .workspace, .workspace-bundle
                  </p>
                </div>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".workspace,.workspace-bundle"
                  onChange={handleFileInputChange}
                  className={styles.fileInput}
                />
              </>
            ) : (
              <div className={styles.preview}>
                <div>
                  <strong>Selected File:</strong> {selectedFile.name}
                </div>

                {validationErrors.length > 0 && (
                  <MessageBar intent="error">
                    <MessageBarBody>
                      <MessageBarTitle>Validation Errors</MessageBarTitle>
                      <ul className={styles.errorList}>
                        {validationErrors.map((error, i) => (
                          <li key={i} className={styles.errorItem}>
                            {error}
                          </li>
                        ))}
                      </ul>
                    </MessageBarBody>
                  </MessageBar>
                )}

                {validationWarnings.length > 0 && (
                  <MessageBar intent="warning">
                    <MessageBarBody>
                      <MessageBarTitle>Warnings</MessageBarTitle>
                      <ul className={styles.errorList}>
                        {validationWarnings.map((warning, i) => (
                          <li key={i} className={styles.errorItem}>
                            {warning}
                          </li>
                        ))}
                      </ul>
                    </MessageBarBody>
                  </MessageBar>
                )}

                {parsedWorkspaces.length > 0 && (
                  <div className={styles.workspaceList}>
                    <strong>
                      {parsedWorkspaces.length === 1
                        ? 'Workspace to Import:'
                        : `Select Workspaces to Import (${selectedWorkspaces.size} of ${parsedWorkspaces.length}):`}
                    </strong>
                    <Table>
                      <TableBody>
                        {parsedWorkspaces.map((workspace, index) => (
                          <TableRow key={index}>
                            <TableCell>
                              {parsedWorkspaces.length > 1 && (
                                <Checkbox
                                  checked={selectedWorkspaces.has(index)}
                                  onChange={() => toggleWorkspaceSelection(index)}
                                />
                              )}
                            </TableCell>
                            <TableCell>
                              <div>
                                <div>
                                  <strong>{workspace.name}</strong>
                                </div>
                                <div
                                  style={{
                                    fontSize: tokens.fontSizeBase200,
                                    color: tokens.colorNeutralForeground3,
                                  }}
                                >
                                  {workspace.description}
                                </div>
                                {workspace.author && (
                                  <div
                                    style={{
                                      fontSize: tokens.fontSizeBase200,
                                      color: tokens.colorNeutralForeground3,
                                    }}
                                  >
                                    Author: {workspace.author}
                                  </div>
                                )}
                              </div>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                )}

                <Button appearance="secondary" onClick={handleReset}>
                  Choose Different File
                </Button>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose} disabled={isImporting}>
              Cancel
            </Button>
            {selectedFile && (
              <Button
                appearance="primary"
                onClick={handleImport}
                disabled={!canImport || isImporting}
              >
                {isImporting ? 'Importing...' : 'Import'}
              </Button>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
