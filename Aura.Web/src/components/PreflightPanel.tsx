import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Warning24Regular,
  Dismiss24Regular,
  Play24Regular,
  Settings24Regular,
  ArrowDownload24Regular,
  ArrowSwap24Regular,
  Open24Regular,
} from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';
import type { PreflightReport, StageCheck, CheckStatus, FixAction } from '../state/providers';
import { useNotifications } from './Notifications/Toasts';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  stageList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  stageItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
  },
  stageIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '24px',
    height: '24px',
  },
  stageContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  stageName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  hint: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  summary: {
    marginTop: tokens.spacingVerticalM,
  },
  fixActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
    flexWrap: 'wrap',
  },
});

interface PreflightPanelProps {
  profile: string;
  report: PreflightReport | null;
  isRunning: boolean;
  onRunPreflight: () => void;
  onApplySafeDefaults?: () => void;
}

export function PreflightPanel({
  profile,
  report,
  isRunning,
  onRunPreflight,
  onApplySafeDefaults,
}: PreflightPanelProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const { showFailureToast } = useNotifications();

  const getStatusIcon = (status: CheckStatus) => {
    switch (status) {
      case 'pass':
        return <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'warn':
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'fail':
        return <Dismiss24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
    }
  };

  const getStatusBadge = (status: CheckStatus) => {
    switch (status) {
      case 'pass':
        return (
          <Badge appearance="filled" color="success">
            Pass
          </Badge>
        );
      case 'warn':
        return (
          <Badge appearance="filled" color="warning">
            Warning
          </Badge>
        );
      case 'fail':
        return (
          <Badge appearance="filled" color="danger">
            Failed
          </Badge>
        );
    }
  };

  const getFixActionIcon = (actionType: string) => {
    switch (actionType) {
      case 'Install':
        return <ArrowDownload24Regular />;
      case 'Start':
        return <Play24Regular />;
      case 'OpenSettings':
        return <Settings24Regular />;
      case 'SwitchToFree':
        return <ArrowSwap24Regular />;
      case 'Help':
        return <Open24Regular />;
      default:
        return null;
    }
  };

  const handleFixAction = (action: FixAction) => {
    switch (action.type) {
      case 'Install':
        // Navigate to downloads page
        navigate('/downloads');
        break;
      case 'OpenSettings':
        // Navigate to settings with the specific tab
        navigate(`/settings${action.parameter ? `?tab=${action.parameter}` : ''}`);
        break;
      case 'Help':
        // Open external URL
        if (action.parameter) {
          window.open(action.parameter, '_blank');
        }
        break;
      case 'SwitchToFree':
        // Apply safe defaults if handler provided
        if (onApplySafeDefaults) {
          onApplySafeDefaults();
        }
        break;
      case 'Start':
        // For now, just show a message - actual start requires backend support
        showFailureToast({
          title: 'Manual Start Required',
          message: `Please start ${action.parameter} manually. Check the Downloads page for instructions.`,
        });
        break;
    }
  };

  const renderFixActions = (fixActions: FixAction[] | null | undefined) => {
    if (!fixActions || fixActions.length === 0) return null;

    return (
      <div className={styles.fixActions}>
        {fixActions.map((action, index) => (
          <Button
            key={index}
            size="small"
            appearance="primary"
            icon={getFixActionIcon(action.type)}
            onClick={() => handleFixAction(action)}
            title={action.description}
          >
            {action.label}
          </Button>
        ))}
      </div>
    );
  };

  const renderStageCheck = (stage: StageCheck) => (
    <Card key={stage.stage} className={styles.stageItem}>
      <div className={styles.stageIcon}>{getStatusIcon(stage.status)}</div>
      <div className={styles.stageContent}>
        <div>
          <Text className={styles.stageName}>{stage.stage}</Text>
          {' - '}
          <Text>{stage.provider}</Text>
        </div>
        <Text>{stage.message}</Text>
        {stage.hint && <Text className={styles.hint}>💡 {stage.hint}</Text>}
        {renderFixActions(stage.fixActions)}
      </div>
      {getStatusBadge(stage.status)}
    </Card>
  );

  const hasFailures = report && !report.ok;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>Preflight Check</Title3>
        <Button
          appearance="primary"
          icon={isRunning ? <Spinner size="tiny" /> : <Play24Regular />}
          onClick={onRunPreflight}
          disabled={isRunning}
        >
          {isRunning ? 'Checking...' : 'Run Preflight Check'}
        </Button>
      </div>

      {report && (
        <>
          <div className={styles.stageList}>{report.stages.map(renderStageCheck)}</div>

          <div className={styles.summary}>
            {report.ok ? (
              <Badge appearance="filled" color="success" size="large">
                ✓ All systems ready
              </Badge>
            ) : (
              <>
                <Badge appearance="filled" color="warning" size="large">
                  ⚠ Some checks failed - review above
                </Badge>
                {onApplySafeDefaults && hasFailures && (
                  <Button
                    appearance="secondary"
                    onClick={onApplySafeDefaults}
                    style={{ marginLeft: tokens.spacingHorizontalM }}
                  >
                    Use Safe Defaults (Free-Only)
                  </Button>
                )}
              </>
            )}
          </div>
        </>
      )}

      {!report && !isRunning && (
        <Card>
          <Text>
            Run a preflight check to verify that all providers for the <strong>{profile}</strong>{' '}
            profile are configured and reachable.
          </Text>
        </Card>
      )}
    </div>
  );
}
