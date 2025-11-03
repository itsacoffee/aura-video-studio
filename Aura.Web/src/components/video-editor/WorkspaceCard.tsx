/**
 * WorkspaceCard Component
 * Card display for workspace with thumbnail, name, and actions
 */

import {
  Card,
  CardHeader,
  CardPreview,
  Button,
  Text,
  makeStyles,
  tokens,
  Tooltip,
} from '@fluentui/react-components';
import {
  Copy20Regular,
  ArrowExport20Regular,
  Delete20Regular,
  Star20Regular,
  Star20Filled,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import { WorkspaceThumbnail } from './WorkspaceThumbnail';

const useStyles = makeStyles({
  card: {
    width: '100%',
    cursor: 'pointer',
    transitionProperty: 'transform, box-shadow',
    transitionDuration: '0.2s',
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow16,
    },
  },
  cardActive: {
    borderTopColor: tokens.colorBrandStroke1,
    borderRightColor: tokens.colorBrandStroke1,
    borderBottomColor: tokens.colorBrandStroke1,
    borderLeftColor: tokens.colorBrandStroke1,
    borderTopWidth: '2px',
    borderRightWidth: '2px',
    borderBottomWidth: '2px',
    borderLeftWidth: '2px',
    borderTopStyle: 'solid',
    borderRightStyle: 'solid',
    borderBottomStyle: 'solid',
    borderLeftStyle: 'solid',
  },
  thumbnailContainer: {
    width: '100%',
    height: '180px',
    position: 'relative',
  },
  header: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  titleRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    display: '-webkit-box',
    WebkitLineClamp: '2',
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
  },
  actionsRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalS,
  },
  badge: {
    display: 'inline-block',
    padding: '2px 8px',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    backgroundColor: tokens.colorNeutralBackground3,
    color: tokens.colorNeutralForeground3,
  },
  activeBadge: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundInverted,
  },
});

export interface WorkspaceCardProps {
  workspace: WorkspaceLayout;
  isActive?: boolean;
  isDefault?: boolean;
  canDelete?: boolean;
  onClick?: () => void;
  onSetDefault?: () => void;
  onDuplicate?: () => void;
  onExport?: () => void;
  onDelete?: () => void;
}

export const WorkspaceCard: FC<WorkspaceCardProps> = ({
  workspace,
  isActive = false,
  isDefault = false,
  canDelete = true,
  onClick,
  onSetDefault,
  onDuplicate,
  onExport,
  onDelete,
}) => {
  const styles = useStyles();

  const handleCardClick = (e: React.MouseEvent) => {
    // Don't trigger if clicking on action buttons
    if ((e.target as HTMLElement).closest('button')) {
      return;
    }
    onClick?.();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onClick?.();
    }
  };

  return (
    <Card
      className={`${styles.card} ${isActive ? styles.cardActive : ''}`}
      onClick={handleCardClick}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="button"
    >
      <CardPreview>
        <div className={styles.thumbnailContainer}>
          <WorkspaceThumbnail workspace={workspace} />
        </div>
      </CardPreview>

      <CardHeader
        header={
          <div className={styles.header}>
            <div className={styles.titleRow}>
              {isDefault && (
                <Tooltip content="Default workspace" relationship="label">
                  <Star20Filled style={{ color: tokens.colorBrandForeground1 }} />
                </Tooltip>
              )}
              <Text className={styles.title}>{workspace.name}</Text>
              {isActive && <span className={`${styles.badge} ${styles.activeBadge}`}>Active</span>}
            </div>
            <Text className={styles.description}>{workspace.description}</Text>
          </div>
        }
        action={
          <div
            className={styles.actionsRow}
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => {
              if (e.key === 'Escape') {
                e.stopPropagation();
              }
            }}
            role="toolbar"
            aria-label="Workspace actions"
          >
            {!isDefault && onSetDefault && (
              <Tooltip content="Set as default" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Star20Regular />}
                  onClick={(e) => {
                    e.stopPropagation();
                    onSetDefault();
                  }}
                  aria-label="Set as default"
                />
              </Tooltip>
            )}
            {onDuplicate && (
              <Tooltip content="Duplicate" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Copy20Regular />}
                  onClick={(e) => {
                    e.stopPropagation();
                    onDuplicate();
                  }}
                  aria-label="Duplicate workspace"
                />
              </Tooltip>
            )}
            {onExport && (
              <Tooltip content="Export" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<ArrowExport20Regular />}
                  onClick={(e) => {
                    e.stopPropagation();
                    onExport();
                  }}
                  aria-label="Export workspace"
                />
              </Tooltip>
            )}
            {canDelete && onDelete && (
              <Tooltip content="Delete" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Delete20Regular />}
                  onClick={(e) => {
                    e.stopPropagation();
                    onDelete();
                  }}
                  aria-label="Delete workspace"
                />
              </Tooltip>
            )}
          </div>
        }
      />
    </Card>
  );
};
