/**
 * WorkspacePreview Component
 * Tooltip-style preview for workspace on hover
 */

import {
  makeStyles,
  tokens,
  Text,
  Popover,
  PopoverTrigger,
  PopoverSurface,
} from '@fluentui/react-components';
import type { FC } from 'react';
import * as React from 'react';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import { WorkspaceThumbnail } from './WorkspaceThumbnail';

const useStyles = makeStyles({
  previewSurface: {
    minWidth: '320px',
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  thumbnailContainer: {
    width: '100%',
    height: '180px',
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
  },
  infoSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  metadata: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
    marginTop: tokens.spacingVerticalXS,
  },
});

export interface WorkspacePreviewProps {
  workspace: WorkspaceLayout;
  children: React.ReactElement;
  showMetadata?: boolean;
}

export const WorkspacePreview: FC<WorkspacePreviewProps> = ({
  workspace,
  children,
  showMetadata = true,
}) => {
  const styles = useStyles();

  return (
    <Popover positioning="below" withArrow>
      <PopoverTrigger disableButtonEnhancement>{children}</PopoverTrigger>
      <PopoverSurface className={styles.previewSurface}>
        <div className={styles.thumbnailContainer}>
          <WorkspaceThumbnail workspace={workspace} showCustomBadge={false} />
        </div>
        <div className={styles.infoSection}>
          <Text className={styles.title}>{workspace.name}</Text>
          <Text className={styles.description}>{workspace.description}</Text>
          {showMetadata && (
            <div className={styles.metadata}>
              <div>Type: {workspace.id.startsWith('custom-') ? 'Custom' : 'Built-in'}</div>
              {workspace.id.startsWith('custom-') && (
                <div>ID: {workspace.id.substring(0, 20)}...</div>
              )}
            </div>
          )}
        </div>
      </PopoverSurface>
    </Popover>
  );
};
