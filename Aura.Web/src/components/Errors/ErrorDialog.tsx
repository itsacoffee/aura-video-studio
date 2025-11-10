import {
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  makeStyles,
  tokens,
  Badge,
  Text,
} from '@fluentui/react-components';
import {
  ErrorCircle24Regular,
  Warning24Regular,
  Info24Regular,
  Dismiss24Regular,
  Copy24Regular,
  ArrowDownload24Regular,
  ArrowClockwise24Regular,
  DocumentBulletList24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '700px',
    width: '90vw',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  severityBadge: {
    marginLeft: 'auto',
  },
  section: {
    marginBottom: tokens.spacingVerticalL,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground1,
  },
  actionsList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
  },
  actionItem: {
    padding: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalM,
    borderLeft: `3px solid ${tokens.colorBrandStroke1}`,
    marginBottom: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  technicalDetails: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    overflowX: 'auto',
    maxHeight: '300px',
    overflowY: 'auto',
  },
  correlationId: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
  linksList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
  },
  linkItem: {
    padding: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalS,
  },
  troubleshootingStep: {
    marginBottom: tokens.spacingVerticalM,
  },
  stepNumber: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '24px',
    height: '24px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundInverted,
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    marginRight: tokens.spacingHorizontalS,
  },
});

export interface ErrorDialogProps {
  open: boolean;
  onClose: () => void;
  error: ErrorInfo;
  onRetry?: () => void;
  onExportDiagnostics?: () => void;
}

export interface ErrorInfo {
  title: string;
  message: string;
  errorCode?: string;
  correlationId?: string;
  severity: 'error' | 'warning' | 'info' | 'critical';
  suggestedActions?: string[];
  technicalDetails?: string;
  stackTrace?: string;
  troubleshootingSteps?: TroubleshootingStep[];
  documentationLinks?: DocumentationLink[];
  canRetry?: boolean;
  isTransient?: boolean;
  automatedRecovery?: AutomatedRecoveryOption;
}

export interface TroubleshootingStep {
  step: number;
  title: string;
  description: string;
  actions: string[];
}

export interface DocumentationLink {
  title: string;
  url: string;
  description: string;
}

export interface AutomatedRecoveryOption {
  name: string;
  description: string;
  estimatedTimeSeconds: number;
}

export function ErrorDialog({
  open,
  onClose,
  error,
  onRetry,
  onExportDiagnostics,
}: ErrorDialogProps) {
  const styles = useStyles();
  const [copySuccess, setCopySuccess] = useState(false);
  const [isRecovering, setIsRecovering] = useState(false);

  const getSeverityIcon = () => {
    switch (error.severity) {
      case 'critical':
      case 'error':
        return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      case 'warning':
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'info':
        return <Info24Regular style={{ color: tokens.colorBrandForeground1 }} />;
    }
  };

  const getSeverityColor = () => {
    switch (error.severity) {
      case 'critical':
        return 'danger';
      case 'error':
        return 'danger';
      case 'warning':
        return 'warning';
      case 'info':
        return 'informative';
      default:
        return 'important';
    }
  };

  const handleCopyDetails = async () => {
    const details = `
Error: ${error.title}
Message: ${error.message}
${error.errorCode ? `Error Code: ${error.errorCode}` : ''}
${error.correlationId ? `Correlation ID: ${error.correlationId}` : ''}
${error.technicalDetails ? `\nTechnical Details:\n${error.technicalDetails}` : ''}
${error.stackTrace ? `\nStack Trace:\n${error.stackTrace}` : ''}
    `.trim();

    try {
      await navigator.clipboard.writeText(details);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  const handleAttemptRecovery = async () => {
    if (!error.automatedRecovery) return;

    setIsRecovering(true);
    try {
      // Call recovery endpoint
      // This would be implemented in the actual component
      await new Promise(resolve => setTimeout(resolve, error.automatedRecovery.estimatedTimeSeconds * 1000));
      onRetry?.();
    } catch (err) {
      console.error('Recovery failed:', err);
    } finally {
      setIsRecovering(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle>
            <div className={styles.header}>
              {getSeverityIcon()}
              <Text weight="semibold">{error.title}</Text>
              <Badge className={styles.severityBadge} appearance="filled" color={getSeverityColor()}>
                {error.severity.toUpperCase()}
              </Badge>
            </div>
          </DialogTitle>
          <DialogContent>
            {/* Main Error Message */}
            <div className={styles.section}>
              <Text>{error.message}</Text>
              {error.correlationId && (
                <div className={styles.correlationId}>
                  Correlation ID: {error.correlationId}
                </div>
              )}
            </div>

            {/* Automated Recovery Option */}
            {error.automatedRecovery && (
              <div className={styles.section}>
                <div className={styles.sectionTitle}>Automated Recovery Available</div>
                <div className={styles.actionItem}>
                  <Text>{error.automatedRecovery.description}</Text>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3, marginTop: tokens.spacingVerticalXS }}>
                    Estimated time: {error.automatedRecovery.estimatedTimeSeconds} seconds
                  </Text>
                </div>
              </div>
            )}

            {/* Suggested Actions */}
            {error.suggestedActions && error.suggestedActions.length > 0 && (
              <div className={styles.section}>
                <div className={styles.sectionTitle}>Suggested Actions</div>
                <ul className={styles.actionsList}>
                  {error.suggestedActions.map((action, index) => (
                    <li key={index} className={styles.actionItem}>
                      {action}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* Troubleshooting Steps */}
            {error.troubleshootingSteps && error.troubleshootingSteps.length > 0 && (
              <div className={styles.section}>
                <div className={styles.sectionTitle}>Troubleshooting Steps</div>
                {error.troubleshootingSteps.map((step) => (
                  <div key={step.step} className={styles.troubleshootingStep}>
                    <div style={{ display: 'flex', alignItems: 'center', marginBottom: tokens.spacingVerticalS }}>
                      <span className={styles.stepNumber}>{step.step}</span>
                      <Text weight="semibold">{step.title}</Text>
                    </div>
                    <Text size={200} style={{ marginLeft: '36px', marginBottom: tokens.spacingVerticalS }}>
                      {step.description}
                    </Text>
                    <ul style={{ marginLeft: '36px' }}>
                      {step.actions.map((action, idx) => (
                        <li key={idx}><Text size={200}>{action}</Text></li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            )}

            {/* Documentation Links */}
            {error.documentationLinks && error.documentationLinks.length > 0 && (
              <div className={styles.section}>
                <div className={styles.sectionTitle}>Documentation & Help</div>
                <ul className={styles.linksList}>
                  {error.documentationLinks.map((link, index) => (
                    <li key={index} className={styles.linkItem}>
                      <a href={link.url} target="_blank" rel="noopener noreferrer" style={{ color: tokens.colorBrandForeground1 }}>
                        <Text weight="semibold">{link.title}</Text>
                      </a>
                      <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
                        {link.description}
                      </Text>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* Technical Details (Collapsible) */}
            {(error.technicalDetails || error.stackTrace) && (
              <Accordion collapsible>
                <AccordionItem value="technical">
                  <AccordionHeader>Technical Details</AccordionHeader>
                  <AccordionPanel>
                    {error.errorCode && (
                      <Text size={200} style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}>
                        <strong>Error Code:</strong> {error.errorCode}
                      </Text>
                    )}
                    {error.technicalDetails && (
                      <div className={styles.technicalDetails}>
                        {error.technicalDetails}
                      </div>
                    )}
                    {error.stackTrace && (
                      <>
                        <Text size={200} weight="semibold" style={{ display: 'block', margin: `${tokens.spacingVerticalM} 0 ${tokens.spacingVerticalS} 0` }}>
                          Stack Trace:
                        </Text>
                        <div className={styles.technicalDetails}>
                          {error.stackTrace}
                        </div>
                      </>
                    )}
                  </AccordionPanel>
                </AccordionItem>
              </Accordion>
            )}
          </DialogContent>
          <DialogActions>
            <Button
              appearance="subtle"
              icon={<Copy24Regular />}
              onClick={handleCopyDetails}
            >
              {copySuccess ? 'Copied!' : 'Copy Details'}
            </Button>
            {onExportDiagnostics && (
              <Button
                appearance="subtle"
                icon={<ArrowDownload24Regular />}
                onClick={onExportDiagnostics}
              >
                Export Diagnostics
              </Button>
            )}
            {error.automatedRecovery && (
              <Button
                appearance="secondary"
                icon={<ArrowClockwise24Regular />}
                onClick={handleAttemptRecovery}
                disabled={isRecovering}
              >
                {isRecovering ? 'Recovering...' : 'Attempt Recovery'}
              </Button>
            )}
            {onRetry && error.canRetry && (
              <Button
                appearance="primary"
                icon={<ArrowClockwise24Regular />}
                onClick={onRetry}
              >
                Retry
              </Button>
            )}
            <Button appearance="primary" onClick={onClose}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
