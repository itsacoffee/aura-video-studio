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
  Checkbox,
  Label,
  Input,
  Field,
} from '@fluentui/react-components';
import { useState, useEffect } from 'react';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import {
  exportWorkspaceToJSON,
  exportWorkspacesAsBundle,
  downloadWorkspaceFile,
  sanitizeFilename,
} from '../../utils/workspaceImportExport';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  checkboxGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
});

interface ExportWorkspaceDialogProps {
  open: boolean;
  onClose: () => void;
  workspace?: WorkspaceLayout | null;
  workspaces?: WorkspaceLayout[];
  mode: 'single' | 'all';
}

export function ExportWorkspaceDialog({
  open,
  onClose,
  workspace,
  workspaces,
  mode,
}: ExportWorkspaceDialogProps) {
  const styles = useStyles();
  const [includeMetadata, setIncludeMetadata] = useState(true);
  const [authorName, setAuthorName] = useState('');
  const [isExporting, setIsExporting] = useState(false);

  useEffect(() => {
    if (open) {
      const storedAuthor = localStorage.getItem('aura-workspace-author');
      if (storedAuthor) {
        setAuthorName(storedAuthor);
      }
    }
  }, [open]);

  const handleExport = () => {
    setIsExporting(true);

    try {
      if (mode === 'single' && workspace) {
        const author = includeMetadata && authorName ? authorName : undefined;
        const jsonContent = exportWorkspaceToJSON(workspace, author);
        const filename = `${sanitizeFilename(workspace.name)}.workspace`;
        downloadWorkspaceFile(jsonContent, filename);
      } else if (mode === 'all' && workspaces && workspaces.length > 0) {
        const author = includeMetadata && authorName ? authorName : undefined;
        const jsonContent = exportWorkspacesAsBundle(workspaces, author);
        const filename = `aura-workspaces-${new Date().toISOString().split('T')[0]}.workspace-bundle`;
        downloadWorkspaceFile(jsonContent, filename);
      }

      if (includeMetadata && authorName) {
        localStorage.setItem('aura-workspace-author', authorName);
      }

      onClose();
    } catch (error) {
      console.error('Error exporting workspace:', error);
    } finally {
      setIsExporting(false);
    }
  };

  const getTitle = () => {
    if (mode === 'single') {
      return workspace ? `Export "${workspace.name}"` : 'Export Workspace';
    }
    return 'Export All Workspaces';
  };

  const getDescription = () => {
    if (mode === 'single') {
      return 'Export this workspace configuration as a JSON file for backup or sharing.';
    }
    return `Export all ${workspaces?.length || 0} workspaces as a bundle for backup or sharing.`;
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{getTitle()}</DialogTitle>
          <DialogContent className={styles.content}>
            <p>{getDescription()}</p>

            <div className={styles.section}>
              <Label>Export Options</Label>
              <div className={styles.checkboxGroup}>
                <Checkbox
                  checked={includeMetadata}
                  onChange={(_, data) => setIncludeMetadata(data.checked === true)}
                  label="Include metadata (author, creation date)"
                />
              </div>
            </div>

            {includeMetadata && (
              <Field label="Author Name (optional)">
                <Input
                  value={authorName}
                  onChange={(_, data) => setAuthorName(data.value)}
                  placeholder="Your name or username"
                />
              </Field>
            )}

            <div className={styles.section}>
              <Label>File Format</Label>
              <p
                style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 }}
              >
                {mode === 'single'
                  ? 'File will be saved as .workspace (JSON format)'
                  : 'File will be saved as .workspace-bundle (JSON format)'}
              </p>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose} disabled={isExporting}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={handleExport} disabled={isExporting}>
              {isExporting ? 'Exporting...' : 'Export'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
