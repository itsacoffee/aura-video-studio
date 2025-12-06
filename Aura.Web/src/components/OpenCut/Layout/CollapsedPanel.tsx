/**
 * CollapsedPanel Component
 *
 * A collapsed panel that shows only icons with tooltips.
 * Used when a panel is minimized to save screen space.
 */

import { makeStyles, tokens, Tooltip, Button } from '@fluentui/react-components';
import {
  Folder24Regular,
  Settings24Regular,
  Video24Regular,
  MusicNote224Regular,
  Sparkle24Regular,
  TextT24Regular,
  SlideTransition24Regular,
  Grid24Regular,
  DocumentCopy24Regular,
  ClosedCaption24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { LAYOUT_CONSTANTS } from '../../../stores/opencutLayout';
import { openCutTokens } from '../../../styles/designTokens';

export type CollapsedPanelType = 'media' | 'properties';

export interface CollapsedPanelProps {
  type: CollapsedPanelType;
  onExpand: () => void;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    width: `${LAYOUT_CONSTANTS.leftPanel.collapsedWidth}px`,
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    paddingTop: openCutTokens.spacing.md,
    gap: openCutTokens.spacing.xs,
    transition: `width ${LAYOUT_CONSTANTS.animation.collapseDuration}ms ${LAYOUT_CONSTANTS.animation.collapseEasing}`,
  },
  containerRight: {
    borderRight: 'none',
    borderLeft: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  iconButton: {
    minWidth: '36px',
    minHeight: '36px',
    color: tokens.colorNeutralForeground3,
    ':hover': {
      color: tokens.colorNeutralForeground1,
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  iconButtonActive: {
    color: tokens.colorBrandForeground1,
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  expandButton: {
    marginTop: 'auto',
    marginBottom: openCutTokens.spacing.md,
    minWidth: '36px',
    minHeight: '36px',
  },
  divider: {
    width: '24px',
    height: '1px',
    backgroundColor: tokens.colorNeutralStroke3,
    margin: `${openCutTokens.spacing.xs} 0`,
  },
});

const MEDIA_PANEL_ICONS = [
  { id: 'media', icon: <Folder24Regular />, label: 'Media' },
  { id: 'effects', icon: <Sparkle24Regular />, label: 'Effects' },
  { id: 'transitions', icon: <SlideTransition24Regular />, label: 'Transitions' },
  { id: 'graphics', icon: <Grid24Regular />, label: 'Graphics' },
  { id: 'templates', icon: <DocumentCopy24Regular />, label: 'Templates' },
  { id: 'captions', icon: <ClosedCaption24Regular />, label: 'Captions' },
];

const PROPERTIES_PANEL_ICONS = [
  { id: 'transform', icon: <Video24Regular />, label: 'Transform' },
  { id: 'audio', icon: <MusicNote224Regular />, label: 'Audio' },
  { id: 'text', icon: <TextT24Regular />, label: 'Text' },
  { id: 'settings', icon: <Settings24Regular />, label: 'Properties' },
];

export const CollapsedPanel: FC<CollapsedPanelProps> = ({ type, onExpand, className }) => {
  const styles = useStyles();
  const icons = type === 'media' ? MEDIA_PANEL_ICONS : PROPERTIES_PANEL_ICONS;

  return (
    <div
      className={`${styles.container} ${type === 'properties' ? styles.containerRight : ''} ${className || ''}`}
    >
      {icons.map((item) => (
        <Tooltip key={item.id} content={item.label} relationship="label" positioning="after">
          <Button
            appearance="subtle"
            size="small"
            className={styles.iconButton}
            icon={item.icon}
            onClick={onExpand}
            aria-label={item.label}
          />
        </Tooltip>
      ))}

      <div className={styles.divider} />

      <Tooltip content={`Expand ${type} panel`} relationship="label" positioning="after">
        <Button
          appearance="subtle"
          size="small"
          className={styles.expandButton}
          icon={type === 'media' ? <Folder24Regular /> : <Settings24Regular />}
          onClick={onExpand}
          aria-label={`Expand ${type} panel`}
        />
      </Tooltip>
    </div>
  );
};

export default CollapsedPanel;
