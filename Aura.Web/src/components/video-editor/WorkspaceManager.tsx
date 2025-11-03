import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  Button,
  makeStyles,
  tokens,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
  Tooltip,
} from '@fluentui/react-components';
import {
  Copy20Regular,
  ArrowExport20Regular,
  Delete20Regular,
  Star20Regular,
  Star20Filled,
  ArrowImport20Regular,
  ArrowDownload20Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import {
  getWorkspaceLayouts,
  deleteWorkspaceLayout,
  PRESET_LAYOUTS,
  duplicateWorkspaceLayout,
  getCurrentLayoutId,
} from '../../services/workspaceLayoutService';
import { useWorkspaceLayoutStore } from '../../state/workspaceLayout';
import { ExportWorkspaceDialog } from './ExportWorkspaceDialog';
import { ImportWorkspaceDialog } from './ImportWorkspaceDialog';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    minHeight: '400px',
    maxHeight: '600px',
  },
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
  tableContainer: {
    flex: 1,
    overflowY: 'auto',
  },
  actionsCell: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actionButton: {
    minWidth: 'auto',
  },
  defaultIndicator: {
    color: tokens.colorBrandForeground1,
  },
  presetBadge: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginLeft: tokens.spacingHorizontalXS,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface WorkspaceManagerProps {
  open: boolean;
  onClose: () => void;
}

export function WorkspaceManager({ open, onClose }: WorkspaceManagerProps) {
  const styles = useStyles();
  const [workspaces, setWorkspaces] = useState(getWorkspaceLayouts());
  const [defaultLayoutId, setDefaultLayoutId] = useState(getCurrentLayoutId());
  const [exportDialogOpen, setExportDialogOpen] = useState(false);
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [selectedWorkspace, setSelectedWorkspace] = useState<WorkspaceLayout | null>(null);
  const [exportMode, setExportMode] = useState<'single' | 'all'>('single');
  const { setCurrentLayout } = useWorkspaceLayoutStore();

  const refreshWorkspaces = useCallback(() => {
    setWorkspaces(getWorkspaceLayouts());
    setDefaultLayoutId(getCurrentLayoutId());
  }, []);

  const handleSetDefault = useCallback(
    (layoutId: string) => {
      setCurrentLayout(layoutId);
      setDefaultLayoutId(layoutId);
    },
    [setCurrentLayout]
  );

  const handleDuplicate = useCallback(
    (workspace: WorkspaceLayout) => {
      const duplicated = duplicateWorkspaceLayout(workspace.id);
      if (duplicated) {
        refreshWorkspaces();
      }
    },
    [refreshWorkspaces]
  );

  const handleDelete = useCallback(
    (layoutId: string) => {
      deleteWorkspaceLayout(layoutId);
      refreshWorkspaces();
    },
    [refreshWorkspaces]
  );

  const handleExportSingle = useCallback((workspace: WorkspaceLayout) => {
    setSelectedWorkspace(workspace);
    setExportMode('single');
    setExportDialogOpen(true);
  }, []);

  const handleExportAll = useCallback(() => {
    setExportMode('all');
    setExportDialogOpen(true);
  }, []);

  const handleImport = useCallback(() => {
    setImportDialogOpen(true);
  }, []);

  const handleImportComplete = useCallback(() => {
    refreshWorkspaces();
    setImportDialogOpen(false);
  }, [refreshWorkspaces]);

  const isPreset = (layoutId: string) => !!PRESET_LAYOUTS[layoutId];

  return (
    <>
      <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
        <DialogSurface style={{ maxWidth: '900px' }}>
          <DialogBody>
            <DialogTitle>Workspace Manager</DialogTitle>
            <DialogContent className={styles.content}>
              <div className={styles.toolbar}>
                <Button appearance="primary" icon={<ArrowImport20Regular />} onClick={handleImport}>
                  Import
                </Button>
                <Button
                  appearance="secondary"
                  icon={<ArrowExport20Regular />}
                  onClick={handleExportAll}
                >
                  Export All
                </Button>
                <Button
                  appearance="secondary"
                  icon={<ArrowDownload20Regular />}
                  onClick={() => {
                    /* Download templates functionality can be added here */
                  }}
                >
                  Get Templates
                </Button>
              </div>

              <div className={styles.tableContainer}>
                {workspaces.length === 0 ? (
                  <div className={styles.emptyState}>
                    <p>No workspaces found</p>
                    <Button appearance="primary" onClick={handleImport}>
                      Import Workspace
                    </Button>
                  </div>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHeaderCell>Name</TableHeaderCell>
                        <TableHeaderCell>Description</TableHeaderCell>
                        <TableHeaderCell>Type</TableHeaderCell>
                        <TableHeaderCell>Actions</TableHeaderCell>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {workspaces.map((workspace) => (
                        <TableRow key={workspace.id}>
                          <TableCell>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                              {defaultLayoutId === workspace.id ? (
                                <Tooltip content="Default workspace" relationship="label">
                                  <Star20Filled className={styles.defaultIndicator} />
                                </Tooltip>
                              ) : (
                                <Star20Regular style={{ visibility: 'hidden' }} />
                              )}
                              {workspace.name}
                            </div>
                          </TableCell>
                          <TableCell>{workspace.description}</TableCell>
                          <TableCell>
                            {isPreset(workspace.id) ? (
                              <span className={styles.presetBadge}>Built-in</span>
                            ) : (
                              <span>Custom</span>
                            )}
                          </TableCell>
                          <TableCell>
                            <div className={styles.actionsCell}>
                              {defaultLayoutId !== workspace.id && (
                                <Tooltip content="Set as default" relationship="label">
                                  <Button
                                    appearance="subtle"
                                    size="small"
                                    icon={<Star20Regular />}
                                    onClick={() => handleSetDefault(workspace.id)}
                                    className={styles.actionButton}
                                    aria-label="Set as default"
                                  />
                                </Tooltip>
                              )}
                              <Tooltip content="Duplicate" relationship="label">
                                <Button
                                  appearance="subtle"
                                  size="small"
                                  icon={<Copy20Regular />}
                                  onClick={() => handleDuplicate(workspace)}
                                  className={styles.actionButton}
                                  aria-label="Duplicate workspace"
                                />
                              </Tooltip>
                              <Tooltip content="Export" relationship="label">
                                <Button
                                  appearance="subtle"
                                  size="small"
                                  icon={<ArrowExport20Regular />}
                                  onClick={() => handleExportSingle(workspace)}
                                  className={styles.actionButton}
                                  aria-label="Export workspace"
                                />
                              </Tooltip>
                              {!isPreset(workspace.id) && (
                                <Tooltip content="Delete" relationship="label">
                                  <Button
                                    appearance="subtle"
                                    size="small"
                                    icon={<Delete20Regular />}
                                    onClick={() => handleDelete(workspace.id)}
                                    className={styles.actionButton}
                                    aria-label="Delete workspace"
                                  />
                                </Tooltip>
                              )}
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>
            </DialogContent>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <ExportWorkspaceDialog
        open={exportDialogOpen}
        onClose={() => setExportDialogOpen(false)}
        workspace={exportMode === 'single' ? selectedWorkspace : null}
        workspaces={exportMode === 'all' ? workspaces : undefined}
        mode={exportMode}
      />

      <ImportWorkspaceDialog
        open={importDialogOpen}
        onClose={() => setImportDialogOpen(false)}
        onImportComplete={handleImportComplete}
      />
    </>
  );
}
