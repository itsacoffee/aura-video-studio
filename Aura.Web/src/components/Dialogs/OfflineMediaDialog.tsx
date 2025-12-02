/**
 * Offline Media Dialog Component
 * Displays when project contains offline/missing media files
 * Provides options to locate, relink, or skip missing files
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Badge,
  Text,
  Tooltip,
  Spinner,
  makeStyles,
  tokens,
  DataGrid,
  DataGridHeader,
  DataGridRow,
  DataGridHeaderCell,
  DataGridBody,
  DataGridCell,
  TableColumnDefinition,
  createTableColumn,
  DataGridCellFocusMode,
} from '@fluentui/react-components';
import {
  Warning24Regular,
  FolderOpen24Regular,
  Dismiss24Regular,
  DocumentSearch24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { assetManager } from '../../services/assetManager';
import { loggingService } from '../../services/loggingService';
import type { AssetReference } from '../../types/asset';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '800px',
    minWidth: '600px',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  warningBanner: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
    flexShrink: 0,
  },
  warningText: {
    flex: 1,
  },
  dataGrid: {
    maxHeight: '300px',
    overflowY: 'auto',
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'space-between',
  },
  primaryActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  resultSummary: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  errorIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  pathText: {
    maxWidth: '200px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
});

interface OfflineMediaDialogProps {
  isOpen: boolean;
  onClose: () => void;
  offlineAssets: AssetReference[];
  onAssetsRelinked: () => void;
}

interface RelinkStatus {
  assetId: string;
  status: 'pending' | 'found' | 'not-found' | 'error';
  newPath?: string;
  error?: string;
}

/**
 * Dialog component for handling offline/missing media files
 */
export const OfflineMediaDialog: FC<OfflineMediaDialogProps> = ({
  isOpen,
  onClose,
  offlineAssets,
  onAssetsRelinked,
}) => {
  const styles = useStyles();
  const logger = loggingService.createLogger('OfflineMediaDialog');

  const [isSearching, setIsSearching] = useState(false);
  const [relinkStatuses, setRelinkStatuses] = useState<Map<string, RelinkStatus>>(new Map());
  const [searchComplete, setSearchComplete] = useState(false);

  /**
   * Handle locating all missing files by searching a directory
   * Note: The File System Access API in browsers has security limitations.
   * It only provides file/folder names, not full paths. In a desktop app
   * context (Electron), this would be handled through IPC to the main process
   * which has full file system access. The backend API should accept the
   * directory handle or resolve paths appropriately.
   */
  const handleLocateAll = useCallback(async () => {
    setIsSearching(true);
    setSearchComplete(false);

    try {
      // Check if the File System Access API is available
      if ('showDirectoryPicker' in window) {
        const directoryHandle = await (
          window as unknown as { showDirectoryPicker: () => Promise<FileSystemDirectoryHandle> }
        ).showDirectoryPicker();

        logger.info('search started', 'locateAll', { directory: directoryHandle.name });

        // Initialize pending statuses
        const initialStatuses = new Map<string, RelinkStatus>();
        offlineAssets.forEach((asset) => {
          initialStatuses.set(asset.id, {
            assetId: asset.id,
            status: 'pending',
          });
        });
        setRelinkStatuses(initialStatuses);

        // In a desktop app, this would use Electron IPC to get the full path.
        // For web, we pass the directory name and rely on the backend to
        // search in configured/known locations or user-specified base paths.
        const result = await assetManager.bulkRelink({
          searchDirectory: directoryHandle.name,
          recursive: true,
          matchByName: true,
          matchByHash: true,
        });

        // Update statuses based on results
        const updatedStatuses = new Map<string, RelinkStatus>();

        result.relinked.forEach((relinkResult) => {
          updatedStatuses.set(relinkResult.assetId, {
            assetId: relinkResult.assetId,
            status: relinkResult.success ? 'found' : 'error',
            newPath: relinkResult.newPath,
            error: relinkResult.error,
          });
        });

        result.stillMissing.forEach((assetId) => {
          updatedStatuses.set(assetId, {
            assetId,
            status: 'not-found',
          });
        });

        setRelinkStatuses(updatedStatuses);
        setSearchComplete(true);

        if (result.found > 0) {
          onAssetsRelinked();
          logger.info('relink complete', 'locateAll', {
            found: result.found,
            notFound: result.notFound,
          });
        }

        // Close dialog if all files were found
        if (result.stillMissing.length === 0) {
          onClose();
        }
      } else {
        // Fallback for browsers without File System Access API
        logger.warn('File System Access API not available', 'locateAll');

        // Use file input as fallback
        const input = document.createElement('input');
        input.type = 'file';
        input.webkitdirectory = true;
        input.click();
      }
    } catch (error: unknown) {
      if (error instanceof Error && error.name === 'AbortError') {
        // User cancelled the directory picker
        logger.info('Directory selection cancelled', 'locateAll');
      } else {
        logger.error(
          'Failed to locate files',
          error instanceof Error ? error : new Error(String(error)),
          'locateAll'
        );
      }
    } finally {
      setIsSearching(false);
    }
  }, [offlineAssets, onAssetsRelinked, onClose, logger]);

  /**
   * Handle locating a single missing file
   * Note: The File System Access API in browsers has security limitations.
   * It only provides file names, not full paths. In a desktop app context
   * (Electron), this would be handled through IPC to get the actual file path.
   * The backend should be configured to resolve file names to full paths
   * based on project settings or configured media directories.
   */
  const handleLocateSingle = useCallback(
    async (assetId: string) => {
      const asset = offlineAssets.find((a) => a.id === assetId);
      if (!asset) return;

      try {
        // Check if File System Access API is available
        if ('showOpenFilePicker' in window) {
          const [fileHandle] = await (
            window as unknown as {
              showOpenFilePicker: (options?: unknown) => Promise<FileSystemFileHandle[]>;
            }
          ).showOpenFilePicker({
            types: [
              {
                description: 'Media files',
                accept: {
                  'video/*': ['.mp4', '.mov', '.avi', '.mkv', '.webm'],
                  'audio/*': ['.mp3', '.wav', '.aac', '.ogg', '.flac'],
                  'image/*': ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp'],
                },
              },
            ],
          });

          const file = await fileHandle.getFile();

          // Update status to pending
          setRelinkStatuses((prev) => {
            const updated = new Map(prev);
            updated.set(assetId, { assetId, status: 'pending' });
            return updated;
          });

          // In a desktop app, this would use Electron IPC to get the full path.
          // For web, we pass the file name and rely on the backend to resolve it.
          const result = await assetManager.relinkAsset({
            assetId,
            newPath: file.name,
            verifyHash: true,
          });

          // Update status
          setRelinkStatuses((prev) => {
            const updated = new Map(prev);
            updated.set(assetId, {
              assetId,
              status: result.success ? 'found' : 'error',
              newPath: result.newPath,
              error: result.error,
            });
            return updated;
          });

          if (result.success) {
            onAssetsRelinked();
            logger.info('Single file relinked', 'locateSingle', { assetId });
          }
        } else {
          // Fallback for browsers without File System Access API
          const input = document.createElement('input');
          input.type = 'file';

          input.onchange = async (e) => {
            const file = (e.target as HTMLInputElement).files?.[0];
            if (file) {
              const result = await assetManager.relinkAsset({
                assetId,
                newPath: file.name,
                verifyHash: true,
              });

              if (result.success) {
                onAssetsRelinked();
                setRelinkStatuses((prev) => {
                  const updated = new Map(prev);
                  updated.set(assetId, {
                    assetId,
                    status: 'found',
                    newPath: result.newPath,
                  });
                  return updated;
                });
              }
            }
          };

          input.click();
        }
      } catch (error: unknown) {
        if (error instanceof Error && error.name === 'AbortError') {
          // User cancelled
          return;
        }

        logger.error(
          'Failed to locate file',
          error instanceof Error ? error : new Error(String(error)),
          'locateSingle'
        );

        setRelinkStatuses((prev) => {
          const updated = new Map(prev);
          updated.set(assetId, {
            assetId,
            status: 'error',
            error: error instanceof Error ? error.message : 'Unknown error',
          });
          return updated;
        });
      }
    },
    [offlineAssets, onAssetsRelinked, logger]
  );

  /**
   * Handle skipping all missing files
   */
  const handleSkipAll = useCallback(() => {
    logger.info('User skipped all missing files', 'skipAll', {
      count: offlineAssets.length,
    });
    onClose();
  }, [offlineAssets.length, onClose, logger]);

  /**
   * Get status badge for an asset
   */
  const getStatusBadge = useCallback(
    (assetId: string) => {
      const status = relinkStatuses.get(assetId);

      if (!status || status.status === 'pending') {
        return (
          <Badge appearance="outline" color="warning">
            Missing
          </Badge>
        );
      }

      switch (status.status) {
        case 'found':
          return (
            <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
              Found
            </Badge>
          );
        case 'not-found':
          return (
            <Badge appearance="outline" color="danger">
              Not Found
            </Badge>
          );
        case 'error':
          return (
            <Tooltip content={status.error || 'Error'} relationship="label">
              <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
                Error
              </Badge>
            </Tooltip>
          );
        default:
          return null;
      }
    },
    [relinkStatuses]
  );

  /**
   * Calculate summary counts
   */
  const summary = useMemo(() => {
    let found = 0;
    let notFound = 0;
    let errors = 0;

    relinkStatuses.forEach((status) => {
      switch (status.status) {
        case 'found':
          found++;
          break;
        case 'not-found':
          notFound++;
          break;
        case 'error':
          errors++;
          break;
      }
    });

    return { found, notFound, errors };
  }, [relinkStatuses]);

  /**
   * Define columns for the data grid
   */
  const columns: TableColumnDefinition<AssetReference>[] = useMemo(
    () => [
      createTableColumn<AssetReference>({
        columnId: 'name',
        compare: (a, b) => a.name.localeCompare(b.name),
        renderHeaderCell: () => 'File Name',
        renderCell: (item) => (
          <Tooltip content={item.name} relationship="label">
            <Text className={styles.pathText}>{item.name}</Text>
          </Tooltip>
        ),
      }),
      createTableColumn<AssetReference>({
        columnId: 'type',
        compare: (a, b) => a.type.localeCompare(b.type),
        renderHeaderCell: () => 'Type',
        renderCell: (item) => <Badge appearance="outline">{item.type}</Badge>,
      }),
      createTableColumn<AssetReference>({
        columnId: 'path',
        compare: (a, b) => a.originalPath.localeCompare(b.originalPath),
        renderHeaderCell: () => 'Original Path',
        renderCell: (item) => (
          <Tooltip content={item.originalPath} relationship="label">
            <Text className={styles.pathText}>{item.originalPath}</Text>
          </Tooltip>
        ),
      }),
      createTableColumn<AssetReference>({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => <div className={styles.statusBadge}>{getStatusBadge(item.id)}</div>,
      }),
      createTableColumn<AssetReference>({
        columnId: 'actions',
        renderHeaderCell: () => 'Actions',
        renderCell: (item) => {
          const status = relinkStatuses.get(item.id);
          const isFound = status?.status === 'found';

          return (
            <div className={styles.actionButtons}>
              <Tooltip content="Locate file" relationship="label">
                <Button
                  size="small"
                  icon={<DocumentSearch24Regular />}
                  appearance="subtle"
                  disabled={isSearching || isFound}
                  onClick={() => handleLocateSingle(item.id)}
                />
              </Tooltip>
            </div>
          );
        },
      }),
    ],
    [styles, getStatusBadge, relinkStatuses, isSearching, handleLocateSingle]
  );

  const getCellFocusMode = useCallback((columnId: string | number): DataGridCellFocusMode => {
    return columnId === 'actions' ? 'group' : 'cell';
  }, []);

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle>Missing Media Files</DialogTitle>
          <DialogContent className={styles.content}>
            {/* Warning Banner */}
            <div className={styles.warningBanner}>
              <Warning24Regular className={styles.warningIcon} />
              <div className={styles.warningText}>
                <Text weight="semibold">
                  {offlineAssets.length} file{offlineAssets.length !== 1 ? 's' : ''} could not be
                  found.
                </Text>
                <Text>
                  The media files may have been moved, renamed, or deleted. You can locate them
                  manually or search a folder.
                </Text>
              </div>
            </div>

            {/* Search Result Summary */}
            {searchComplete && (
              <div className={styles.resultSummary}>
                {summary.found > 0 && (
                  <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
                    {summary.found} found
                  </Badge>
                )}
                {summary.notFound > 0 && (
                  <Badge appearance="outline" color="danger">
                    {summary.notFound} not found
                  </Badge>
                )}
                {summary.errors > 0 && (
                  <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
                    {summary.errors} errors
                  </Badge>
                )}
              </div>
            )}

            {/* Data Grid */}
            <div className={styles.dataGrid}>
              <DataGrid
                items={offlineAssets}
                columns={columns}
                sortable
                getRowId={(item) => item.id}
                focusMode="composite"
              >
                <DataGridHeader>
                  <DataGridRow>
                    {({ renderHeaderCell }) => (
                      <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                    )}
                  </DataGridRow>
                </DataGridHeader>
                <DataGridBody<AssetReference>>
                  {({ item, rowId }) => (
                    <DataGridRow<AssetReference> key={rowId}>
                      {({ renderCell, columnId }) => (
                        <DataGridCell focusMode={getCellFocusMode(columnId)}>
                          {renderCell(item)}
                        </DataGridCell>
                      )}
                    </DataGridRow>
                  )}
                </DataGridBody>
              </DataGrid>
            </div>
          </DialogContent>
          <DialogActions className={styles.actions}>
            <Button
              appearance="secondary"
              icon={<Dismiss24Regular />}
              onClick={handleSkipAll}
              disabled={isSearching}
            >
              Skip All
            </Button>
            <div className={styles.primaryActions}>
              <Button
                appearance="primary"
                icon={isSearching ? <Spinner size="tiny" /> : <FolderOpen24Regular />}
                onClick={handleLocateAll}
                disabled={isSearching}
              >
                {isSearching ? 'Searching...' : 'Search Folder'}
              </Button>
            </div>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default OfflineMediaDialog;
