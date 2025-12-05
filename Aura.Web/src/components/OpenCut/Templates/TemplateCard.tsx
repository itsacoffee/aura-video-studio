/**
 * TemplateCard Component
 *
 * Individual template preview card with thumbnail, info, and action buttons.
 * Supports click to select, double-click to apply, and explicit apply button.
 */

import {
  makeStyles,
  tokens,
  Text,
  mergeClasses,
  Tooltip,
  Button,
  Badge,
} from '@fluentui/react-components';
import { Play24Regular, Copy24Regular, Delete24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import type { Template } from '../../../stores/opencutTemplates';
import { openCutTokens } from '../../../styles/designTokens';

export interface TemplateCardProps {
  template: Template;
  isSelected?: boolean;
  onClick?: () => void;
  onApply?: () => void;
  onDuplicate?: () => void;
  onDelete?: () => void;
  showActions?: boolean;
}

const useStyles = makeStyles({
  card: {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.lg,
    cursor: 'pointer',
    transition: 'all 0.15s ease',
    border: `1px solid transparent`,
    overflow: 'hidden',
    position: 'relative',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
      transform: 'translateY(-2px)',
      boxShadow: openCutTokens.shadows.md,
    },
    ':active': {
      transform: 'translateY(0)',
    },
  },
  cardSelected: {
    border: `1px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3Hover,
  },
  thumbnail: {
    width: '100%',
    aspectRatio: '16/9',
    backgroundColor: tokens.colorNeutralBackground4,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
    overflow: 'hidden',
  },
  thumbnailImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  thumbnailPlaceholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground4,
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
  },
  aspectBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
  },
  content: {
    padding: openCutTokens.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  name: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    lineHeight: tokens.lineHeightBase300,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  description: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    lineHeight: tokens.lineHeightBase200,
    minHeight: '2.4em',
  },
  tags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXXS,
    marginTop: tokens.spacingVerticalXS,
  },
  tag: {
    fontSize: tokens.fontSizeBase100,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    padding: `0 ${openCutTokens.spacing.md} ${openCutTokens.spacing.md}`,
    opacity: 0,
    transition: 'opacity 0.15s ease',
  },
  actionsVisible: {
    opacity: 1,
  },
  cardHovered: {
    ':hover .template-actions': {
      opacity: 1,
    },
  },
  builtinBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    left: tokens.spacingHorizontalXS,
  },
  meta: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalXS,
  },
  metaItem: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
  },
});

const formatDuration = (seconds: number): string => {
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  if (remainingSeconds === 0) return `${minutes}m`;
  return `${minutes}m ${remainingSeconds}s`;
};

export const TemplateCard: FC<TemplateCardProps> = ({
  template,
  isSelected = false,
  onClick,
  onApply,
  onDuplicate,
  onDelete,
  showActions = true,
}) => {
  const styles = useStyles();

  const handleApplyClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onApply?.();
  };

  const handleDuplicateClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDuplicate?.();
  };

  const handleDeleteClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDelete?.();
  };

  const handleDoubleClick = () => {
    onApply?.();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      if (e.key === 'Enter') {
        onApply?.();
      } else {
        onClick?.();
      }
    }
  };

  return (
    <div
      className={mergeClasses(
        styles.card,
        isSelected && styles.cardSelected,
        showActions && styles.cardHovered
      )}
      onClick={onClick}
      onDoubleClick={handleDoubleClick}
      onKeyDown={handleKeyDown}
      role="button"
      tabIndex={0}
      aria-label={`${template.name} template`}
      aria-pressed={isSelected}
    >
      <div className={styles.thumbnail}>
        {template.thumbnail ? (
          <img
            src={template.thumbnail}
            alt={`${template.name} preview`}
            className={styles.thumbnailImage}
          />
        ) : (
          <div className={styles.thumbnailPlaceholder}>
            <Text size={200}>{template.aspectRatio}</Text>
            <Text size={100}>{template.data.tracks.length} tracks</Text>
          </div>
        )}

        {template.isBuiltin && (
          <Badge
            className={styles.builtinBadge}
            appearance="filled"
            color="informative"
            size="small"
          >
            Built-in
          </Badge>
        )}

        <Badge className={styles.aspectBadge} appearance="outline" size="small">
          {template.aspectRatio}
        </Badge>
      </div>

      <div className={styles.content}>
        <Text className={styles.name}>{template.name}</Text>
        <Text className={styles.description}>{template.description}</Text>

        <div className={styles.meta}>
          <Text className={styles.metaItem}>{template.data.tracks.length} tracks</Text>
          {template.duration > 0 && (
            <Text className={styles.metaItem}>{formatDuration(template.duration)}</Text>
          )}
        </div>

        {template.tags.length > 0 && (
          <div className={styles.tags}>
            {template.tags.slice(0, 3).map((tag) => (
              <span key={tag} className={styles.tag}>
                {tag}
              </span>
            ))}
            {template.tags.length > 3 && (
              <span className={styles.tag}>+{template.tags.length - 3}</span>
            )}
          </div>
        )}
      </div>

      {showActions && (
        <div className={mergeClasses(styles.actions, 'template-actions')}>
          <Tooltip content="Apply template" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Play24Regular />}
              onClick={handleApplyClick}
              aria-label="Apply template"
            />
          </Tooltip>

          <Tooltip content="Duplicate" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Copy24Regular />}
              onClick={handleDuplicateClick}
              aria-label="Duplicate template"
            />
          </Tooltip>

          {!template.isBuiltin && onDelete && (
            <Tooltip content="Delete" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<Delete24Regular />}
                onClick={handleDeleteClick}
                aria-label="Delete template"
              />
            </Tooltip>
          )}
        </div>
      )}
    </div>
  );
};

export default TemplateCard;
