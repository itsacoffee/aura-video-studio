/**
 * Firewall Configuration Dialog
 * Helps users configure Windows Firewall for Aura Video Studio backend
 */

import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  makeStyles,
  shorthands,
  Text,
  tokens,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Spinner,
} from '@fluentui/react-components';
import { ShieldCheckmark24Regular, Info24Regular } from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import {
  checkFirewallRule,
  addFirewallRule,
  type FirewallConfigResult,
} from '../../services/api/firewallApi';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  infoSection: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    ...shorthands.padding(tokens.spacingVerticalS),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
  },
  orderedList: {
    marginLeft: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalXS,
  },
  code: {
    fontFamily: 'monospace',
    backgroundColor: tokens.colorNeutralBackground3,
    ...shorthands.padding(tokens.spacingHorizontalXS),
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
    fontSize: tokens.fontSizeBase200,
  },
});

export interface FirewallConfigDialogProps {
  /** Whether the dialog is open */
  open: boolean;

  /** Callback when dialog closes */
  onClose: () => void;

  /** Path to the backend executable */
  executablePath?: string;
}

export function FirewallConfigDialog({
  open,
  onClose,
  executablePath = 'C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe',
}: FirewallConfigDialogProps) {
  const styles = useStyles();
  const [configuring, setConfiguring] = useState(false);
  const [result, setResult] = useState<FirewallConfigResult | null>(null);
  const [ruleExists, setRuleExists] = useState<boolean | null>(null);

  const checkExistingRule = useCallback(async () => {
    try {
      const exists = await checkFirewallRule(executablePath);
      setRuleExists(exists);
    } catch (error: unknown) {
      console.error('Failed to check firewall rule:', error);
      setRuleExists(false);
    }
  }, [executablePath]);

  useEffect(() => {
    if (open) {
      checkExistingRule();
    }
  }, [open, checkExistingRule]);

  const handleAutoConfig = async () => {
    setConfiguring(true);
    setResult(null);

    const configResult = await addFirewallRule(executablePath, false);
    setResult(configResult);
    setConfiguring(false);

    if (configResult.success) {
      setTimeout(() => {
        onClose();
      }, 2000);
    }
  };

  const handleClose = () => {
    setResult(null);
    setRuleExists(null);
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && handleClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>
            <ShieldCheckmark24Regular style={{ marginRight: tokens.spacingHorizontalS }} />
            Windows Firewall Configuration
          </DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.infoSection}>
              <Info24Regular style={{ flexShrink: 0, marginTop: '2px' }} />
              <div>
                <Text>
                  Aura Video Studio runs a local web server for its backend. Windows Firewall may
                  block this server and prevent the application from working correctly.
                </Text>
              </div>
            </div>

            {ruleExists === true && (
              <MessageBar intent="success">
                <MessageBarBody>
                  <MessageBarTitle>Firewall Rule Exists</MessageBarTitle>
                  The Windows Firewall rule for Aura Video Studio is already configured.
                </MessageBarBody>
              </MessageBar>
            )}

            {result && (
              <MessageBar intent={result.success ? 'success' : 'error'}>
                <MessageBarBody>
                  <MessageBarTitle>{result.success ? 'Success' : 'Error'}</MessageBarTitle>
                  {result.message}
                </MessageBarBody>
              </MessageBar>
            )}

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Automatic Configuration (Recommended)
              </Text>
              <Text>
                Click below to automatically add a firewall exception. This requires administrator
                privileges and may show a UAC prompt.
              </Text>
              <Button
                appearance="primary"
                onClick={handleAutoConfig}
                disabled={configuring || ruleExists === true}
                icon={configuring ? <Spinner size="tiny" /> : undefined}
              >
                {configuring
                  ? 'Configuring...'
                  : ruleExists === true
                    ? 'Already Configured'
                    : 'Configure Firewall Automatically'}
              </Button>
            </div>

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Manual Configuration
              </Text>
              <Text>If automatic configuration fails, you can add the firewall rule manually:</Text>
              <ol className={styles.orderedList}>
                <li>
                  <Text>Open Windows Defender Firewall with Advanced Security</Text>
                </li>
                <li>
                  <Text>Click &ldquo;Inbound Rules&rdquo; → &ldquo;New Rule&rdquo;</Text>
                </li>
                <li>
                  <Text>Select &ldquo;Program&rdquo; and click Next</Text>
                </li>
                <li>
                  <Text>
                    Browse to: <span className={styles.code}>{executablePath}</span>
                  </Text>
                </li>
                <li>
                  <Text>Select &ldquo;Allow the connection&rdquo; and click Next</Text>
                </li>
                <li>
                  <Text>Apply to all profiles and finish</Text>
                </li>
              </ol>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={handleClose}>
              {ruleExists === true ? 'Close' : 'Skip (Configure Later)'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
