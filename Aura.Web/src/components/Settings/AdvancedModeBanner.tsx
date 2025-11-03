import {
  makeStyles,
  tokens,
  Button,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Link,
} from '@fluentui/react-components';
import { Warning24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  banner: {
    marginBottom: tokens.spacingVerticalL,
  },
  buttonContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
});

interface AdvancedModeBannerProps {
  onRevert: () => void;
}

/**
 * Warning banner displayed when Advanced Mode is enabled
 */
export function AdvancedModeBanner({ onRevert }: AdvancedModeBannerProps) {
  const styles = useStyles();

  return (
    <MessageBar className={styles.banner} intent="warning" icon={<Warning24Regular />}>
      <MessageBarBody>
        <MessageBarTitle>Advanced Mode Active</MessageBarTitle>
        You have enabled Advanced Mode, which reveals expert features that may require technical
        knowledge. These include ML retraining controls, deep prompt customization, low-level render
        flags, chroma key compositing, motion graphics recipes, and expert provider tuning.{' '}
        <Link href="/docs/advanced-mode" target="_blank" rel="noopener noreferrer">
          Learn more about Advanced Mode
        </Link>
        <div className={styles.buttonContainer}>
          <Button appearance="primary" onClick={onRevert}>
            Revert to Simple Mode
          </Button>
        </div>
      </MessageBarBody>
    </MessageBar>
  );
}
