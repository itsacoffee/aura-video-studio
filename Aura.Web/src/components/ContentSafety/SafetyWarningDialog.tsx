import {
  makeStyles,
  tokens,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  Badge,
  Card,
  Divider,
} from '@fluentui/react-components';
import {
  Shield24Regular,
  Warning24Regular,
  Dismiss24Regular,
  CheckmarkCircle24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    minWidth: '500px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  violations: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  violation: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder1}`,
  },
  alternatives: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  alternative: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  warning: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
});

export interface SafetyViolation {
  id: string;
  category: string;
  severityScore: number;
  reason: string;
  matchedContent?: string;
  recommendedAction: string;
  suggestedFix?: string;
}

export interface SafetyWarningDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  violations: SafetyViolation[];
  alternatives?: string[];
  onUseAlternative?: (alternative: string) => void;
  onOverride?: () => void;
  onCancel?: () => void;
  canOverride: boolean;
  requiresAdvancedMode: boolean;
  policyName?: string;
}

export const SafetyWarningDialog: FC<SafetyWarningDialogProps> = ({
  open,
  onOpenChange,
  violations,
  alternatives = [],
  onUseAlternative,
  onOverride,
  onCancel,
  canOverride,
  requiresAdvancedMode,
  policyName,
}) => {
  const styles = useStyles();

  const handleAlternativeClick = (alternative: string) => {
    if (onUseAlternative) {
      onUseAlternative(alternative);
      onOpenChange(false);
    }
  };

  const handleOverride = () => {
    if (onOverride) {
      onOverride();
      onOpenChange(false);
    }
  };

  const handleCancel = () => {
    if (onCancel) {
      onCancel();
    }
    onOpenChange(false);
  };

  const getSeverityBadge = (score: number) => {
    if (score >= 8) {
      return <Badge appearance="filled" color="danger">Critical</Badge>;
    }
    if (score >= 5) {
      return <Badge appearance="filled" color="warning">High</Badge>;
    }
    return <Badge appearance="filled" color="informative">Medium</Badge>;
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>
            <div className={styles.header}>
              <Shield24Regular />
              <Text weight="semibold">Content Safety Check Failed</Text>
            </div>
          </DialogTitle>
          <DialogContent className={styles.content}>
            <Text>
              Your content was flagged by the <strong>{policyName || 'current'}</strong> safety policy.
            </Text>

            <Divider />

            <div className={styles.violations}>
              <Text weight="semibold">Issues Found:</Text>
              {violations.map((violation) => (
                <div key={violation.id} className={styles.violation}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: tokens.spacingVerticalS }}>
                    <Text weight="semibold">{violation.category}</Text>
                    {getSeverityBadge(violation.severityScore)}
                  </div>
                  <Text size={300}>{violation.reason}</Text>
                  {violation.matchedContent && (
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalXS, color: tokens.colorNeutralForeground3 }}>
                      Found: &quot;{violation.matchedContent}&quot;
                    </Text>
                  )}
                  {violation.suggestedFix && (
                    <div style={{ marginTop: tokens.spacingVerticalS, display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                      <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                      <Text size={300}>
                        <strong>Suggestion:</strong> {violation.suggestedFix}
                      </Text>
                    </div>
                  )}
                </div>
              ))}
            </div>

            {alternatives.length > 0 && (
              <div className={styles.alternatives}>
                <Text weight="semibold">Suggested Alternatives:</Text>
                {alternatives.map((alt, index) => (
                  <Card
                    key={index}
                    className={styles.alternative}
                    onClick={() => handleAlternativeClick(alt)}
                  >
                    <Text size={300}>{alt}</Text>
                  </Card>
                ))}
              </div>
            )}

            {canOverride && requiresAdvancedMode && (
              <div className={styles.warning}>
                <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1, flexShrink: 0 }} />
                <Text size={300}>
                  You can override this decision in Advanced Mode if you believe this is a false positive.
                  Overriding safety checks is your responsibility.
                </Text>
              </div>
            )}

            {!canOverride && (
              <div className={styles.warning}>
                <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1, flexShrink: 0 }} />
                <Text size={300}>
                  This policy does not allow overrides. Please modify your content to comply with safety guidelines.
                </Text>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <div className={styles.actions}>
              <Button
                appearance="secondary"
                icon={<Dismiss24Regular />}
                onClick={handleCancel}
              >
                Cancel
              </Button>
              {alternatives.length === 0 && (
                <Button
                  appearance="primary"
                  onClick={handleCancel}
                >
                  Edit Content
                </Button>
              )}
              {canOverride && requiresAdvancedMode && (
                <Button
                  appearance="primary"
                  onClick={handleOverride}
                >
                  Override (Advanced)
                </Button>
              )}
            </div>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default SafetyWarningDialog;
