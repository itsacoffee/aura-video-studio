import { Tooltip, TooltipProps, makeStyles, tokens } from '@fluentui/react-components';
import { Keyboard24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  tooltipContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  description: {
    fontSize: tokens.fontSizeBase200,
  },
  shortcut: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXXS,
  },
  shortcutKey: {
    fontFamily: 'monospace',
    padding: `2px ${tokens.spacingHorizontalXXS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
  },
  learnMoreLink: {
    marginTop: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorBrandForeground1,
    cursor: 'pointer',
    textDecoration: 'none',
    ':hover': {
      textDecoration: 'underline',
    },
  },
});

interface EnhancedTooltipProps extends Omit<TooltipProps, 'content'> {
  /** Main description text */
  description: string;
  /** Keyboard shortcut (e.g., "Ctrl+S" or "Space") */
  shortcut?: string;
  /** Link to documentation or help page */
  learnMoreUrl?: string;
  /** Additional details shown below description */
  details?: string;
}

/**
 * Enhanced tooltip with keyboard shortcut hints and optional learn more link
 */
export function EnhancedTooltip({
  description,
  shortcut,
  learnMoreUrl,
  details,
  children,
  relationship = 'description',
  ...props
}: EnhancedTooltipProps) {
  const styles = useStyles();

  const content = (
    <div className={styles.tooltipContent}>
      <div className={styles.description}>{description}</div>
      {details && (
        <div className={styles.description} style={{ opacity: 0.8 }}>
          {details}
        </div>
      )}
      {shortcut && (
        <div className={styles.shortcut}>
          <Keyboard24Regular style={{ fontSize: '14px' }} />
          <span className={styles.shortcutKey}>{shortcut}</span>
        </div>
      )}
      {learnMoreUrl && (
        <a
          href={learnMoreUrl}
          target="_blank"
          rel="noopener noreferrer"
          className={styles.learnMoreLink}
          onClick={(e) => e.stopPropagation()}
        >
          Learn more →
        </a>
      )}
    </div>
  );

  return (
    <Tooltip content={content} relationship={relationship} {...props}>
      {children}
    </Tooltip>
  );
}

/**
 * Quick tooltip for keyboard shortcuts only
 */
export function ShortcutTooltip({
  shortcut,
  children,
  relationship = 'description',
  ...props
}: Omit<EnhancedTooltipProps, 'description'> & { shortcut: string }) {
  const styles = useStyles();

  const content = (
    <div className={styles.shortcut}>
      <Keyboard24Regular style={{ fontSize: '14px' }} />
      <span className={styles.shortcutKey}>{shortcut}</span>
    </div>
  );

  return (
    <Tooltip content={content} relationship={relationship} {...props}>
      {children}
    </Tooltip>
  );
}

/**
 * Help tooltip with question mark icon
 */
export function HelpTooltip({
  description,
  learnMoreUrl,
  details,
}: Omit<EnhancedTooltipProps, 'children'>) {
  const styles = useStyles();

  const content = (
    <div className={styles.tooltipContent}>
      <div className={styles.description}>{description}</div>
      {details && (
        <div className={styles.description} style={{ opacity: 0.8 }}>
          {details}
        </div>
      )}
      {learnMoreUrl && (
        <a
          href={learnMoreUrl}
          target="_blank"
          rel="noopener noreferrer"
          className={styles.learnMoreLink}
          onClick={(e) => e.stopPropagation()}
        >
          Learn more →
        </a>
      )}
    </div>
  );

  return (
    <Tooltip content={content} relationship="description">
      <span
        style={{
          cursor: 'help',
          fontSize: '14px',
          color: tokens.colorNeutralForeground3,
          display: 'inline-flex',
          alignItems: 'center',
          marginLeft: tokens.spacingHorizontalXXS,
        }}
      >
        ⓘ
      </span>
    </Tooltip>
  );
}
