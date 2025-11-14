import {
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  Button,
  makeStyles,
  tokens,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import {
  LayoutColumnTwoSplitLeft20Regular,
  ArrowReset20Regular,
  Checkmark20Regular,
  Delete20Regular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import {
  getWorkspaceLayouts,
  deleteWorkspaceLayout,
  PRESET_LAYOUTS,
} from '../../services/workspaceLayoutService';
import { useWorkspaceLayoutStore } from '../../state/workspaceLayout';

const useStyles = makeStyles({
  button: {
    minWidth: 'auto',
  },
  menuItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  checkmark: {
    width: '20px',
    visibility: 'hidden',
  },
  checkmarkVisible: {
    visibility: 'visible',
  },
  description: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginLeft: '28px',
  },
  layoutItemWithDelete: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    width: '100%',
  },
  deleteButton: {
    minWidth: 'auto',
    padding: '4px',
    marginLeft: tokens.spacingHorizontalS,
  },
  layoutContent: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
});

export function WorkspaceLayoutSwitcher() {
  const styles = useStyles();
  const { currentLayoutId, setCurrentLayout, resetLayout } = useWorkspaceLayoutStore();
  const [layouts, setLayouts] = useState(getWorkspaceLayouts());
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [layoutToDelete, setLayoutToDelete] = useState<string | null>(null);

  const handleLayoutSelect = (layoutId: string) => {
    setCurrentLayout(layoutId);
  };

  const handleResetLayout = () => {
    resetLayout();
  };

  const handleDeleteClick = (layoutId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setLayoutToDelete(layoutId);
    setDeleteDialogOpen(true);
  };

  const handleConfirmDelete = () => {
    if (layoutToDelete) {
      deleteWorkspaceLayout(layoutToDelete);
      if (currentLayoutId === layoutToDelete) {
        resetLayout();
      }
      setLayouts(getWorkspaceLayouts());
      setDeleteDialogOpen(false);
      setLayoutToDelete(null);
    }
  };

  const handleCancelDelete = () => {
    setDeleteDialogOpen(false);
    setLayoutToDelete(null);
  };

  const isPreset = (layoutId: string) => {
    return !!PRESET_LAYOUTS[layoutId];
  };

  const layoutToDeleteName = layoutToDelete
    ? layouts.find((l) => l.id === layoutToDelete)?.name
    : '';

  return (
    <>
      <Menu>
        <MenuTrigger disableButtonEnhancement>
          <Button
            appearance="subtle"
            icon={<LayoutColumnTwoSplitLeft20Regular />}
            className={styles.button}
            aria-label="Switch workspace layout"
          >
            Layout
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            {layouts.map((layout) => {
              const isActive = layout.id === currentLayoutId;
              const canDelete = !isPreset(layout.id);
              return (
                <MenuItem
                  key={layout.id}
                  onClick={() => handleLayoutSelect(layout.id)}
                  className={styles.menuItem}
                >
                  <div className={styles.layoutItemWithDelete}>
                    <div className={styles.layoutContent}>
                      <span
                        className={`${styles.checkmark} ${isActive ? styles.checkmarkVisible : ''}`}
                      >
                        {isActive && <Checkmark20Regular />}
                      </span>
                      <div>
                        <div>{layout.name}</div>
                        <div className={styles.description}>{layout.description}</div>
                      </div>
                    </div>
                    {canDelete && (
                      <Button
                        appearance="subtle"
                        size="small"
                        icon={<Delete20Regular />}
                        onClick={(e) => handleDeleteClick(layout.id, e)}
                        className={styles.deleteButton}
                        aria-label="Delete custom workspace"
                      />
                    )}
                  </div>
                </MenuItem>
              );
            })}
            <MenuDivider />
            <MenuItem onClick={handleResetLayout} icon={<ArrowReset20Regular />}>
              Reset to Default
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
      <DeleteWorkspaceConfirmDialog
        open={deleteDialogOpen}
        workspaceName={layoutToDeleteName || ''}
        onConfirm={handleConfirmDelete}
        onCancel={handleCancelDelete}
      />
    </>
  );
}

function DeleteWorkspaceConfirmDialog({
  open,
  workspaceName,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  workspaceName: string;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onCancel()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Delete Workspace</DialogTitle>
          <DialogContent>
            Are you sure you want to delete the workspace &quot;{workspaceName}&quot;? This action
            cannot be undone.
          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" onClick={onCancel}>
                Cancel
              </Button>
            </DialogTrigger>
            <Button appearance="primary" onClick={onConfirm}>
              Delete
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
