import {
  Badge,
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Spinner,
  Text,
} from '@fluentui/react-components';
import {
  ArrowUndo24Regular,
  Delete24Regular,
  Info24Regular,
  MoreVertical24Regular,
  Star24Filled,
  Star24Regular,
} from '@fluentui/react-icons';
import React, { useCallback, useEffect, useState } from 'react';
import { deleteVersion, getVersions, restoreVersion, updateVersion } from '@/api/versions';
import { useProjectVersionsStore } from '@/state/projectVersions';
import type { VersionResponse } from '@/types/api-v1';

interface VersionListProps {
  projectId: string;
  onCompare?: (version1Id: string, version2Id: string) => void;
  className?: string;
}

/**
 * Component that displays a list of project versions
 */
const VersionList: React.FC<VersionListProps> = ({ projectId, onCompare, className }) => {
  const {
    versions,
    loading,
    error,
    setVersions,
    setLoading,
    setError,
    removeVersion,
    updateVersion: updateVersionInStore,
  } = useProjectVersionsStore();
  const [selectedVersions, setSelectedVersions] = useState<string[]>([]);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [versionToDelete, setVersionToDelete] = useState<VersionResponse | null>(null);
  const [restoreDialogOpen, setRestoreDialogOpen] = useState(false);
  const [versionToRestore, setVersionToRestore] = useState<VersionResponse | null>(null);

  const loadVersions = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getVersions(projectId);
      setVersions(data);
    } catch (err) {
      const error = err as Error;
      setError(error.message || 'Failed to load versions');
    } finally {
      setLoading(false);
    }
  }, [projectId, setLoading, setError, setVersions]);

  useEffect(() => {
    void loadVersions();
  }, [loadVersions]);

  const handleDelete = async (version: VersionResponse) => {
    setVersionToDelete(version);
    setDeleteDialogOpen(true);
  };

  const confirmDelete = async () => {
    if (!versionToDelete) return;

    try {
      await deleteVersion(projectId, versionToDelete.id);
      removeVersion(versionToDelete.id);
      setDeleteDialogOpen(false);
      setVersionToDelete(null);
    } catch (err) {
      const error = err as Error;
      setError(error.message || 'Failed to delete version');
    }
  };

  const handleRestore = async (version: VersionResponse) => {
    setVersionToRestore(version);
    setRestoreDialogOpen(true);
  };

  const confirmRestore = async () => {
    if (!versionToRestore) return;

    try {
      await restoreVersion({
        projectId,
        versionId: versionToRestore.id,
      });
      setRestoreDialogOpen(false);
      setVersionToRestore(null);
      await loadVersions();
    } catch (err) {
      const error = err as Error;
      setError(error.message || 'Failed to restore version');
    }
  };

  const toggleImportant = async (version: VersionResponse) => {
    try {
      await updateVersion(projectId, version.id, {
        isMarkedImportant: !version.isMarkedImportant,
      });
      updateVersionInStore(version.id, {
        isMarkedImportant: !version.isMarkedImportant,
      });
    } catch (err) {
      const error = err as Error;
      setError(error.message || 'Failed to update version');
    }
  };

  const toggleVersionSelection = (versionId: string) => {
    setSelectedVersions((prev) => {
      if (prev.includes(versionId)) {
        return prev.filter((id) => id !== versionId);
      }
      if (prev.length >= 2) {
        return [prev[1], versionId];
      }
      return [...prev, versionId];
    });
  };

  const handleCompare = () => {
    if (selectedVersions.length === 2 && onCompare) {
      onCompare(selectedVersions[0], selectedVersions[1]);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (days === 0) {
      return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    }
    if (days < 7) {
      return `${days}d ago`;
    }
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  };

  const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const getVersionTypeBadge = (type: string) => {
    switch (type) {
      case 'Manual':
        return (
          <Badge appearance="filled" color="success">
            Manual
          </Badge>
        );
      case 'Autosave':
        return (
          <Badge appearance="tint" color="informative">
            Autosave
          </Badge>
        );
      case 'RestorePoint':
        return (
          <Badge appearance="filled" color="warning">
            Restore Point
          </Badge>
        );
      default:
        return <Badge>{type}</Badge>;
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <Spinner label="Loading versions..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 text-red-800 rounded-md">
        <Text weight="semibold">Error: {error}</Text>
      </div>
    );
  }

  return (
    <div className={`flex flex-col gap-2 ${className || ''}`}>
      <div className="flex items-center justify-between mb-4">
        <Text size={400} weight="semibold">
          Versions ({versions.length})
        </Text>
        {selectedVersions.length === 2 && (
          <Button appearance="primary" onClick={handleCompare}>
            Compare Selected
          </Button>
        )}
      </div>

      {versions.length === 0 ? (
        <div className="p-8 text-center text-gray-500">
          <Text>No versions yet. Create a snapshot to get started.</Text>
        </div>
      ) : (
        <div className="space-y-2">
          {versions.map((version) => (
            <div
              key={version.id}
              className={`flex items-center gap-3 p-3 rounded-md border transition-colors ${
                selectedVersions.includes(version.id)
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
              }`}
              onClick={() => toggleVersionSelection(version.id)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  toggleVersionSelection(version.id);
                }
              }}
              role="button"
              tabIndex={0}
              style={{ cursor: 'pointer' }}
            >
              <input
                type="checkbox"
                checked={selectedVersions.includes(version.id)}
                onChange={() => toggleVersionSelection(version.id)}
                className="h-4 w-4"
              />

              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <Text weight="semibold">v{version.versionNumber}</Text>
                  {getVersionTypeBadge(version.versionType)}
                  {version.isMarkedImportant && <Star24Filled className="text-yellow-500" />}
                  {version.name && (
                    <Text size={300} className="truncate">
                      {version.name}
                    </Text>
                  )}
                </div>
                <div className="flex items-center gap-3 text-gray-600 text-sm">
                  <Text size={200}>{formatDate(version.createdAt)}</Text>
                  <Text size={200}>{formatSize(version.storageSizeBytes)}</Text>
                  {version.trigger && (
                    <Text size={200} className="truncate">
                      {version.trigger}
                    </Text>
                  )}
                </div>
              </div>

              <Menu>
                <MenuTrigger>
                  <Button
                    appearance="subtle"
                    icon={<MoreVertical24Regular />}
                    onClick={(e) => e.stopPropagation()}
                  />
                </MenuTrigger>
                <MenuPopover>
                  <MenuList>
                    <MenuItem
                      icon={<ArrowUndo24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleRestore(version);
                      }}
                    >
                      Restore
                    </MenuItem>
                    <MenuItem
                      icon={version.isMarkedImportant ? <Star24Filled /> : <Star24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        toggleImportant(version);
                      }}
                    >
                      {version.isMarkedImportant ? 'Unmark Important' : 'Mark Important'}
                    </MenuItem>
                    <MenuItem
                      icon={<Info24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                      }}
                    >
                      View Details
                    </MenuItem>
                    <MenuItem
                      icon={<Delete24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDelete(version);
                      }}
                    >
                      Delete
                    </MenuItem>
                  </MenuList>
                </MenuPopover>
              </Menu>
            </div>
          ))}
        </div>
      )}

      <Dialog open={deleteDialogOpen} onOpenChange={(_, data) => setDeleteDialogOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Delete Version</DialogTitle>
            <DialogContent>
              <Text>
                Are you sure you want to delete version {versionToDelete?.versionNumber}? This
                action cannot be undone.
              </Text>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setDeleteDialogOpen(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={confirmDelete}>
                Delete
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={restoreDialogOpen} onOpenChange={(_, data) => setRestoreDialogOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Restore Version</DialogTitle>
            <DialogContent>
              <Text>
                Are you sure you want to restore to version {versionToRestore?.versionNumber}? Your
                current state will be saved as a new version before restoring.
              </Text>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setRestoreDialogOpen(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={confirmRestore}>
                Restore
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};

export default VersionList;
