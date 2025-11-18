import {
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  MenuGroup,
  MenuGroupHeader,
} from '@fluentui/react-components';
import type { ReactElement, MouseEvent as ReactMouseEvent } from 'react';
import { useEffect, useState, useCallback, isValidElement } from 'react';

export interface ContextMenuItem {
  id: string;
  label: string;
  icon?: ReactElement;
  shortcut?: string;
  disabled?: boolean;
  divider?: boolean;
  submenu?: ContextMenuItem[];
  onClick?: () => void;
}

export interface ContextMenuGroup {
  header?: string;
  items: ContextMenuItem[];
}

export interface ContextMenuProps {
  /** Position of the context menu */
  position: { x: number; y: number } | null;
  /** Menu items or groups */
  items: (ContextMenuItem | ContextMenuGroup)[];
  /** Callback when menu closes */
  onClose: () => void;
  /** Whether the menu is controlled to be open */
  open?: boolean;
  /** Additional className for styling */
  className?: string;
}

/**
 * Windows-native context menu component
 * Follows Windows 11 design patterns with acrylic backdrop and proper animations
 */
export function ContextMenu({ position, items, onClose, open, className }: ContextMenuProps) {
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    if (position && open !== false) {
      setIsOpen(true);
    } else {
      setIsOpen(false);
    }
  }, [position, open]);

  const handleClose = useCallback(() => {
    setIsOpen(false);
    onClose();
  }, [onClose]);

  const handleItemClick = useCallback(
    (onClick?: () => void) => {
      if (onClick) {
        onClick();
      }
      handleClose();
    },
    [handleClose]
  );

  const renderMenuItem = (item: ContextMenuItem) => {
    if (item.divider) {
      return <MenuDivider key={`divider-${item.id}`} />;
    }

    if (item.submenu && item.submenu.length > 0) {
      return (
        <Menu key={item.id}>
          <MenuTrigger>
            <MenuItem
              {...(item.icon && isValidElement(item.icon) ? { icon: <>{item.icon}</> } : {})}
              disabled={item.disabled}
            >
              {item.label}
            </MenuItem>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>{item.submenu.map(renderMenuItem)}</MenuList>
          </MenuPopover>
        </Menu>
      );
    }

    return (
      <MenuItem
        key={item.id}
        {...(item.icon && isValidElement(item.icon) ? { icon: <>{item.icon}</> } : {})}
        disabled={item.disabled}
        onClick={() => handleItemClick(item.onClick)}
      >
        {item.label}
        {item.shortcut && (
          <span style={{ marginLeft: 'auto', opacity: 0.7, fontSize: '0.85em' }}>
            {item.shortcut}
          </span>
        )}
      </MenuItem>
    );
  };

  const renderGroup = (group: ContextMenuGroup, index: number) => {
    if (group.header) {
      return (
        <MenuGroup key={`group-${index}`}>
          <MenuGroupHeader>{group.header}</MenuGroupHeader>
          {group.items.map(renderMenuItem)}
        </MenuGroup>
      );
    }
    return <div key={`group-${index}`}>{group.items.map(renderMenuItem)}</div>;
  };

  const renderContent = () => {
    return items.map((item, index) => {
      if ('header' in item || 'items' in item) {
        return renderGroup(item as ContextMenuGroup, index);
      }
      return renderMenuItem(item as ContextMenuItem);
    });
  };

  if (!position || !isOpen) {
    return null;
  }

  return (
    <div
      className={`context-menu-container ${className || ''}`}
      style={{
        position: 'fixed',
        left: position.x,
        top: position.y,
        zIndex: 10000,
      }}
    >
      <Menu open={isOpen} onOpenChange={(_, data) => !data.open && handleClose()}>
        <MenuTrigger>
          <div style={{ width: 0, height: 0 }} />
        </MenuTrigger>
        <MenuPopover className="context-menu">{<MenuList>{renderContent()}</MenuList>}</MenuPopover>
      </Menu>
    </div>
  );
}

/**
 * Hook for managing context menu state
 */
// eslint-disable-next-line react-refresh/only-export-components
export function useContextMenu() {
  const [contextMenu, setContextMenu] = useState<{
    position: { x: number; y: number } | null;
  }>({
    position: null,
  });

  const showContextMenu = useCallback((event: ReactMouseEvent) => {
    event.preventDefault();
    event.stopPropagation();
    setContextMenu({
      position: { x: event.clientX, y: event.clientY },
    });
  }, []);

  const hideContextMenu = useCallback(() => {
    setContextMenu({
      position: null,
    });
  }, []);

  useEffect(() => {
    const handleClick = () => {
      if (contextMenu.position) {
        hideContextMenu();
      }
    };

    const handleContextMenu = (e: Event) => {
      if (contextMenu.position) {
        e.preventDefault();
        hideContextMenu();
      }
    };

    document.addEventListener('click', handleClick);
    document.addEventListener('contextmenu', handleContextMenu);

    return () => {
      document.removeEventListener('click', handleClick);
      document.removeEventListener('contextmenu', handleContextMenu);
    };
  }, [contextMenu.position, hideContextMenu]);

  return {
    position: contextMenu.position,
    showContextMenu,
    hideContextMenu,
  };
}
