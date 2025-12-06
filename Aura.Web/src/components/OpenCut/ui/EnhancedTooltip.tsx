/**
 * OpenCut Enhanced Tooltip
 *
 * Enhanced tooltip component with keyboard shortcut display,
 * rich content support, and delayed showing for stability.
 */

import { Tooltip, TooltipProps, makeStyles, tokens } from '@fluentui/react-components';
import { Keyboard20Regular } from '@fluentui/react-icons';
import type { FC, ReactNode, ReactElement } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export interface EnhancedTooltipProps {
  /** Content to display in the tooltip */
  content: ReactNode;
  /** Keyboard shortcut to display (e.g., "Ctrl+S") */
  shortcut?: string;
  /** Additional details text */
  details?: string;
  /** Positioning of the tooltip */
  positioning?: TooltipProps['positioning'];
  /** Show delay in milliseconds for stability */
  showDelay?: number;
  /** Hide delay in milliseconds */
  hideDelay?: number;
  /** The element that triggers the tooltip */
  children: ReactElement;
  /** Relationship to the trigger element */
  relationship?: TooltipProps['relationship'];
}

const useStyles = makeStyles({
  tooltipContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xs,
    maxWidth: '280px',
  },
  mainContent: {
    fontSize: openCutTokens.typography.fontSize.sm,
    color: tokens.colorNeutralForeground1,
    lineHeight: openCutTokens.typography.lineHeight.normal,
  },
  details: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: tokens.colorNeutralForeground2,
    opacity: 0.8,
    lineHeight: openCutTokens.typography.lineHeight.normal,
  },
  shortcutContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    marginTop: openCutTokens.spacing.xxs,
  },
  shortcutIcon: {
    width: '14px',
    height: '14px',
    color: tokens.colorNeutralForeground3,
  },
  shortcutKeys: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
  },
  shortcutKey: {
    fontFamily: openCutTokens.typography.fontFamily.mono,
    fontSize: openCutTokens.typography.fontSize.xs,
    fontWeight: openCutTokens.typography.fontWeight.medium,
    padding: `1px ${openCutTokens.spacing.xxs}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: openCutTokens.radius.xs,
    color: tokens.colorNeutralForeground2,
    lineHeight: 1.2,
  },
  separator: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: tokens.colorNeutralForeground3,
    padding: '0 1px',
  },
});

/**
 * Parse keyboard shortcut string into individual keys
 */
function parseShortcut(shortcut: string): string[] {
  return shortcut.split('+').map((key) => key.trim());
}

/**
 * Render keyboard shortcut keys
 */
const ShortcutKeys: FC<{ shortcut: string; className?: string }> = ({ shortcut }) => {
  const styles = useStyles();
  const keys = parseShortcut(shortcut);

  return (
    <div className={styles.shortcutKeys}>
      {keys.map((key, index) => (
        <span key={index}>
          {index > 0 && <span className={styles.separator}>+</span>}
          <span className={styles.shortcutKey}>{key}</span>
        </span>
      ))}
    </div>
  );
};

/**
 * Enhanced tooltip with keyboard shortcut and rich content support
 */
export const EnhancedTooltip: FC<EnhancedTooltipProps> = ({
  content,
  shortcut,
  details,
  positioning = 'above',
  showDelay = 400,
  hideDelay = 0,
  children,
  relationship = 'description',
}) => {
  const styles = useStyles();

  const tooltipContent = (
    <div className={styles.tooltipContent}>
      <div className={styles.mainContent}>{content}</div>

      {details && <div className={styles.details}>{details}</div>}

      {shortcut && (
        <div className={styles.shortcutContainer}>
          <Keyboard20Regular className={styles.shortcutIcon} />
          <ShortcutKeys shortcut={shortcut} />
        </div>
      )}
    </div>
  );

  return (
    <Tooltip
      content={tooltipContent}
      positioning={positioning}
      showDelay={showDelay}
      hideDelay={hideDelay}
      relationship={relationship}
    >
      {children}
    </Tooltip>
  );
};

export default EnhancedTooltip;
