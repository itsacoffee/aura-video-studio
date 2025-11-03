/**
 * WorkspaceThumbnail Component
 * Displays a thumbnail image for a workspace with loading and error states
 */

import { makeStyles, tokens, Spinner } from '@fluentui/react-components';
import { Image20Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { useWorkspaceThumbnail } from '../../hooks/useWorkspaceThumbnails';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';

const useStyles = makeStyles({
  container: {
    position: 'relative',
    width: '100%',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  thumbnail: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  loadingState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
  },
  placeholderIcon: {
    fontSize: '32px',
    color: tokens.colorNeutralForeground4,
  },
  customBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundInverted,
    padding: '2px 6px',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
});

export interface WorkspaceThumbnailProps {
  workspace: WorkspaceLayout | null;
  showCustomBadge?: boolean;
  alt?: string;
}

export const WorkspaceThumbnail: FC<WorkspaceThumbnailProps> = ({
  workspace,
  showCustomBadge = true,
  alt,
}) => {
  const styles = useStyles();
  const { thumbnailUrl, isGenerating } = useWorkspaceThumbnail(workspace);

  const altText = alt || workspace?.name || 'Workspace thumbnail';

  if (!workspace) {
    return (
      <div className={styles.container}>
        <div className={styles.loadingState}>
          <Image20Regular className={styles.placeholderIcon} />
        </div>
      </div>
    );
  }

  if (isGenerating) {
    return (
      <div className={styles.container}>
        <div className={styles.loadingState}>
          <Spinner size="small" />
          <span style={{ fontSize: tokens.fontSizeBase200 }}>Generating...</span>
        </div>
      </div>
    );
  }

  if (!thumbnailUrl) {
    return (
      <div className={styles.container}>
        <div className={styles.loadingState}>
          <Image20Regular className={styles.placeholderIcon} />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <img src={thumbnailUrl} alt={altText} className={styles.thumbnail} />
      {showCustomBadge && workspace.id.startsWith('custom-') && (
        <div className={styles.customBadge}>Custom</div>
      )}
    </div>
  );
};
