/**
 * WorkspaceGallery Component
 * Grid/List view for displaying workspace cards
 */

import { makeStyles, tokens, SearchBox, ToggleButton } from '@fluentui/react-components';
import { Grid20Regular, List20Regular } from '@fluentui/react-icons';
import { useState, useMemo, type FC } from 'react';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import { WorkspaceCard } from './WorkspaceCard';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    width: '100%',
    height: '100%',
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'space-between',
  },
  searchContainer: {
    flex: 1,
    maxWidth: '400px',
  },
  viewToggle: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  gridView: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalL,
    overflowY: 'auto',
    padding: tokens.spacingVerticalS,
  },
  listView: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    overflowY: 'auto',
    padding: tokens.spacingVerticalS,
  },
  listItem: {
    display: 'grid',
    gridTemplateColumns: '200px 1fr',
    gap: tokens.spacingHorizontalM,
    paddingTop: tokens.spacingVerticalM,
    paddingBottom: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderTopWidth: '1px',
    borderRightWidth: '1px',
    borderBottomWidth: '1px',
    borderLeftWidth: '1px',
    borderTopStyle: 'solid',
    borderRightStyle: 'solid',
    borderBottomStyle: 'solid',
    borderLeftStyle: 'solid',
    borderTopColor: tokens.colorNeutralStroke1,
    borderRightColor: tokens.colorNeutralStroke1,
    borderBottomColor: tokens.colorNeutralStroke1,
    borderLeftColor: tokens.colorNeutralStroke1,
    cursor: 'pointer',
    transitionProperty: 'box-shadow',
    transitionDuration: '0.2s',
    ':hover': {
      boxShadow: tokens.shadow8,
    },
  },
  listItemActive: {
    borderTopColor: tokens.colorBrandStroke1,
    borderRightColor: tokens.colorBrandStroke1,
    borderBottomColor: tokens.colorBrandStroke1,
    borderLeftColor: tokens.colorBrandStroke1,
    borderTopWidth: '2px',
    borderRightWidth: '2px',
    borderBottomWidth: '2px',
    borderLeftWidth: '2px',
  },
  listThumbnail: {
    width: '200px',
    height: '112px',
  },
  listContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  listTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  listDescription: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
  },
  listActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    marginTop: 'auto',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
  },
});

export type ViewMode = 'grid' | 'list';

export interface WorkspaceGalleryProps {
  workspaces: WorkspaceLayout[];
  currentLayoutId?: string;
  defaultLayoutId?: string;
  onWorkspaceClick?: (workspace: WorkspaceLayout) => void;
  onSetDefault?: (workspace: WorkspaceLayout) => void;
  onDuplicate?: (workspace: WorkspaceLayout) => void;
  onExport?: (workspace: WorkspaceLayout) => void;
  onDelete?: (workspace: WorkspaceLayout) => void;
  canDeleteWorkspace?: (workspace: WorkspaceLayout) => boolean;
  initialViewMode?: ViewMode;
}

export const WorkspaceGallery: FC<WorkspaceGalleryProps> = ({
  workspaces,
  currentLayoutId,
  defaultLayoutId,
  onWorkspaceClick,
  onSetDefault,
  onDuplicate,
  onExport,
  onDelete,
  canDeleteWorkspace,
  initialViewMode = 'grid',
}) => {
  const styles = useStyles();
  const [viewMode, setViewMode] = useState<ViewMode>(initialViewMode);
  const [searchQuery, setSearchQuery] = useState('');

  // Filter workspaces based on search
  const filteredWorkspaces = useMemo(() => {
    if (!searchQuery.trim()) {
      return workspaces;
    }
    const query = searchQuery.toLowerCase();
    return workspaces.filter(
      (ws) => ws.name.toLowerCase().includes(query) || ws.description.toLowerCase().includes(query)
    );
  }, [workspaces, searchQuery]);

  if (workspaces.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.emptyState}>
          <p>No workspaces available</p>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.toolbar}>
        <div className={styles.searchContainer}>
          <SearchBox
            placeholder="Search workspaces..."
            value={searchQuery}
            onChange={(_, data) => setSearchQuery(data.value || '')}
          />
        </div>
        <div className={styles.viewToggle}>
          <ToggleButton
            appearance="subtle"
            icon={<Grid20Regular />}
            checked={viewMode === 'grid'}
            onClick={() => setViewMode('grid')}
            aria-label="Grid view"
          />
          <ToggleButton
            appearance="subtle"
            icon={<List20Regular />}
            checked={viewMode === 'list'}
            onClick={() => setViewMode('list')}
            aria-label="List view"
          />
        </div>
      </div>

      {filteredWorkspaces.length === 0 ? (
        <div className={styles.emptyState}>
          <p>No workspaces found matching &quot;{searchQuery}&quot;</p>
        </div>
      ) : viewMode === 'grid' ? (
        <div className={styles.gridView}>
          {filteredWorkspaces.map((workspace) => (
            <WorkspaceCard
              key={workspace.id}
              workspace={workspace}
              isActive={workspace.id === currentLayoutId}
              isDefault={workspace.id === defaultLayoutId}
              canDelete={canDeleteWorkspace ? canDeleteWorkspace(workspace) : true}
              onClick={() => onWorkspaceClick?.(workspace)}
              onSetDefault={() => onSetDefault?.(workspace)}
              onDuplicate={() => onDuplicate?.(workspace)}
              onExport={() => onExport?.(workspace)}
              onDelete={() => onDelete?.(workspace)}
            />
          ))}
        </div>
      ) : (
        <div className={styles.listView}>
          {filteredWorkspaces.map((workspace) => (
            <WorkspaceCard
              key={workspace.id}
              workspace={workspace}
              isActive={workspace.id === currentLayoutId}
              isDefault={workspace.id === defaultLayoutId}
              canDelete={canDeleteWorkspace ? canDeleteWorkspace(workspace) : true}
              onClick={() => onWorkspaceClick?.(workspace)}
              onSetDefault={() => onSetDefault?.(workspace)}
              onDuplicate={() => onDuplicate?.(workspace)}
              onExport={() => onExport?.(workspace)}
              onDelete={() => onDelete?.(workspace)}
            />
          ))}
        </div>
      )}
    </div>
  );
};
