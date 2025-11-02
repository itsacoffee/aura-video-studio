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
} from '@fluentui/react-icons';
import { getWorkspaceLayouts } from '../../services/workspaceLayoutService';
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
            return (
              <MenuItem
                key={layout.id}
                onClick={() => handleLayoutSelect(layout.id)}
                className={styles.menuItem}
              >
                <span className={`${styles.checkmark} ${isActive ? styles.checkmarkVisible : ''}`}>
                  {isActive && <Checkmark20Regular />}
                </span>
                <div>
                  <div>{layout.name}</div>
                  <div className={styles.description}>{layout.description}</div>
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
