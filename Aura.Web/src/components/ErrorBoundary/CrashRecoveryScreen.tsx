/**
 * Crash Recovery Screen
 * Shown when application detects repeated crashes
 */

import {
  Button,
  Card,
  makeStyles,
  MessageBar,
  MessageBarBody,
  shorthands,
  Text,
  Title3,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  Delete24Regular,
  ErrorCircle24Regular,
  Home24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { autoSaveService } from '../../services/autoSaveService';
import { crashRecoveryService } from '../../services/crashRecoveryService';
import { loggingService } from '../../services/loggingService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground1,
    ...shorthands.padding(tokens.spacingVerticalXXL),
  },
  card: {
    maxWidth: '700px',
    width: '100%',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalL),
    ...shorthands.padding(tokens.spacingVerticalXXL, tokens.spacingHorizontalXL),
  },
  header: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    textAlign: 'center',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  icon: {
    fontSize: '64px',
    color: tokens.colorPaletteRedForeground1,
  },
  suggestionsList: {
    marginTop: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalXL,
  },
  actions: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
  },
  actionRow: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    flexWrap: 'wrap',
  },
  dangerZone: {
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalM),
    backgroundColor: tokens.colorPaletteRedBackground1,
    ...shorthands.border('1px', 'solid', tokens.colorPaletteRedBorder1),
  },
});

interface CrashRecoveryScreenProps {
  onRecovered: () => void;
}

export function CrashRecoveryScreen({ onRecovered }: CrashRecoveryScreenProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [clearing, setClearing] = useState(false);

  const recoveryState = crashRecoveryService.getRecoveryState();
  const suggestions = crashRecoveryService.getRecoverySuggestions();

  const handleSafeMode = () => {
    loggingService.info('User entering safe mode', 'CrashRecoveryScreen', 'handleSafeMode');
    
    // Reset crash counter
    crashRecoveryService.resetCrashCounter();
    
    // Navigate to safe home page
    navigate('/', { replace: true });
    onRecovered();
  };

  const handleClearData = async () => {
    if (!confirm('This will clear all local data including auto-saves. Are you sure?')) {
      return;
    }

    setClearing(true);
    loggingService.warn('User clearing all local data', 'CrashRecoveryScreen', 'handleClearData');

    try {
      // Clear auto-save data
      autoSaveService.clearAll();

      // Clear crash recovery data
      crashRecoveryService.clearRecoveryData();

      // Clear other local storage (except settings)
      const keysToKeep = ['aura_user_settings', 'aura_api_keys'];
      const allKeys = Object.keys(localStorage);
      allKeys.forEach((key) => {
        if (!keysToKeep.includes(key)) {
          localStorage.removeItem(key);
        }
      });

      // Reload page
      window.location.reload();
    } catch (error) {
      loggingService.error(
        'Failed to clear data',
        error as Error,
        'CrashRecoveryScreen',
        'handleClearData'
      );
      setClearing(false);
    }
  };

  const handleRestoreAutosave = () => {
    loggingService.info(
      'User restoring from autosave',
      'CrashRecoveryScreen',
      'handleRestoreAutosave'
    );
    
    // Reset crash counter
    crashRecoveryService.resetCrashCounter();
    
    // Reload to trigger autosave recovery
    window.location.reload();
  };

  const hasAutosave = autoSaveService.hasRecoverableData();

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <div className={styles.header}>
            <ErrorCircle24Regular className={styles.icon} />
            <Title3>Application Recovery Mode</Title3>
            <Text style={{ color: tokens.colorNeutralForeground2 }}>
              Aura detected {recoveryState?.consecutiveCrashes || 0} consecutive crashes. We've
              entered recovery mode to help you get back on track.
            </Text>
          </div>

          <MessageBar intent="warning">
            <MessageBarBody>
              <Text weight="semibold" block>
                What happened?
              </Text>
              <Text>
                The application unexpectedly closed multiple times in a row. This could be due to a
                browser issue, corrupted data, or a bug.
              </Text>
            </MessageBarBody>
          </MessageBar>

          {suggestions.length > 0 && (
            <div>
              <Text weight="semibold" block>
                Recovery Suggestions:
              </Text>
              <ul className={styles.suggestionsList}>
                {suggestions.map((suggestion, index) => (
                  <li key={index}>
                    <Text>{suggestion}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <div className={styles.actions}>
            <Text weight="semibold">Recovery Options:</Text>

            <div className={styles.actionRow}>
              <Button
                appearance="primary"
                icon={<Home24Regular />}
                onClick={handleSafeMode}
                size="large"
              >
                Continue in Safe Mode
              </Button>

              {hasAutosave && (
                <Button
                  appearance="secondary"
                  icon={<ArrowClockwise24Regular />}
                  onClick={handleRestoreAutosave}
                  size="large"
                >
                  Restore Auto-save
                </Button>
              )}
            </div>

            <div className={styles.dangerZone}>
              <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalS }}>
                Last Resort Options:
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalS }}>
                Only use these if the above options don't work.
              </Text>
              <div className={styles.actionRow}>
                <Button
                  appearance="secondary"
                  icon={<Delete24Regular />}
                  onClick={handleClearData}
                  disabled={clearing}
                  size="small"
                >
                  {clearing ? 'Clearing...' : 'Clear All Local Data'}
                </Button>
              </div>
            </div>
          </div>

          {recoveryState?.lastCrash && (
            <details>
              <summary style={{ cursor: 'pointer' }}>
                <Text size={300}>Crash Details (for support)</Text>
              </summary>
              <Card
                style={{
                  marginTop: tokens.spacingVerticalS,
                  padding: tokens.spacingVerticalS,
                  backgroundColor: tokens.colorNeutralBackground3,
                }}
              >
                <Text
                  size={200}
                  style={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap', display: 'block' }}
                >
                  {JSON.stringify(recoveryState.lastCrash, null, 2)}
                </Text>
              </Card>
            </details>
          )}
        </div>
      </Card>
    </div>
  );
}
