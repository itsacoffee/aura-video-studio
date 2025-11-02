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
} from '@fluentui/react-components';
import {
  LayoutColumnTwoSplitLeft20Regular,
  ArrowReset20Regular,
  Checkmark20Regular,
  Delete20Regular,
} from '@fluentui/react-icons';
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
});

export function WorkspaceLayoutSwitcher() {
  const styles = useStyles();
  const { currentLayoutId, setCurrentLayout, resetLayout } = useWorkspaceLayoutStore();
  const layouts = getWorkspaceLayouts();

  const handleLayoutSelect = (layoutId: string) => {
    setCurrentLayout(layoutId);
  };

  const handleResetLayout = () => {
    resetLayout();
  };

  const handleDeleteLayout = (layoutId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (confirm('Are you sure you want to delete this custom workspace?')) {
      deleteWorkspaceLayout(layoutId);
      if (currentLayoutId === layoutId) {
        resetLayout();
      }
      window.location.reload();
    }
  };

  const isPreset = (layoutId: string) => {
    return !!PRESET_LAYOUTS[layoutId];
  };

  return (
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
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
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
                      onClick={(e) => handleDeleteLayout(layout.id, e)}
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
  );
}
