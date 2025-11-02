import {
  Button,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Dismiss16Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { providerRecommendationService } from '../../services/providers/providerRecommendationService';

const useStyles = makeStyles({
  link: {
    color: tokens.colorBrandForeground1,
    textDecoration: 'underline',
    cursor: 'pointer',
    fontSize: tokens.fontSizeBase200,
    border: 'none',
    background: 'none',
    padding: 0,
    '&:hover': {
      color: tokens.colorBrandForeground2,
    },
  },
});

interface QuickDisableRecommendationsProps {
  onDisabled?: () => void;
}

/**
 * Quick disable link for provider recommendations
 * Shows a confirmation dialog before disabling
 */
export const QuickDisableRecommendations: FC<QuickDisableRecommendationsProps> = ({
  onDisabled,
}) => {
  const styles = useStyles();
  const [showDialog, setShowDialog] = useState(false);
  const [disabling, setDisabling] = useState(false);

  const handleDisable = async () => {
    try {
      setDisabling(true);
      await providerRecommendationService.updatePreferences({
        enableRecommendations: false,
        assistanceLevel: 'Off',
      });
      setShowDialog(false);
      onDisabled?.();
    } catch (error: unknown) {
      console.error('Failed to disable recommendations:', error);
    } finally {
      setDisabling(false);
    }
  };

  return (
    <>
      <button type="button" className={styles.link} onClick={() => setShowDialog(true)}>
        Disable recommendations
      </button>

      <Dialog open={showDialog} onOpenChange={(_, data) => setShowDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Disable Provider Recommendations?</DialogTitle>
            <DialogContent>
              <Text>
                This will turn off all recommendation features. You&apos;ll have complete manual
                control over provider selection with no automation or suggestions.
              </Text>
              <Text style={{ marginTop: tokens.spacingVerticalM }}>
                You can re-enable recommendations anytime in Settings → Recommendations.
              </Text>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => setShowDialog(false)}
                disabled={disabling}
              >
                Cancel
              </Button>
              <Button
                appearance="primary"
                icon={<Dismiss16Regular />}
                onClick={handleDisable}
                disabled={disabling}
              >
                {disabling ? 'Disabling...' : 'Disable Recommendations'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  );
};
