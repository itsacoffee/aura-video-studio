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
  Open24Regular,
  WrenchScrewdriver24Regular,
} from '@fluentui/react-icons';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useHealthDiagnostics } from '../state/healthDiagnostics';
import type { HealthCheckDetail, RemediationAction } from '../types/api-v1';
import { HealthCheckStatus, RemediationActionType } from '../types/api-v1';

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
  checkList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  checkItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
  },
  checkIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '24px',
    height: '24px',
  },
  checkContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  checkName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  checkCategory: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  hint: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
  },
  summary: {
    marginTop: tokens.spacingVerticalM,
  },
  remediationActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
    flexWrap: 'wrap',
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalL,
  },
  categoryTitle: {
    marginBottom: tokens.spacingVerticalM,
    fontWeight: tokens.fontWeightSemibold,
  },
});

interface HealthDiagnosticsPanelProps {
  showOptional?: boolean;
  onReady?: (isReady: boolean) => void;
}

export function HealthDiagnosticsPanel({
  showOptional = true,
  onReady,
}: HealthDiagnosticsPanelProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const { details, isLoading, error, fetchHealthDetails, isSystemReady, getRequiredFailedChecks } =
    useHealthDiagnostics();

  useEffect(() => {
    if (onReady && details) {
      onReady(isSystemReady());
    }
  }, [details, isSystemReady, onReady]);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case HealthCheckStatus.Pass:
        return <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case HealthCheckStatus.Warning:
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case HealthCheckStatus.Fail:
        return <Dismiss24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      default:
        return null;
    }
  };

  const getStatusBadge = (status: string, isRequired: boolean) => {
    switch (status) {
      case HealthCheckStatus.Pass:
        return (
          <Badge appearance="filled" color="success">
            Pass
          </Badge>
        );
      case HealthCheckStatus.Warning:
        return (
          <Badge appearance="filled" color="warning">
            {isRequired ? 'Warning' : 'Optional'}
          </Badge>
        );
      case HealthCheckStatus.Fail:
        return (
          <Badge appearance="filled" color="danger">
            {isRequired ? 'Failed' : 'Not Available'}
          </Badge>
        );
      default:
        return null;
    }
  };

  const getActionIcon = (actionType: string) => {
    switch (actionType) {
      case RemediationActionType.Install:
        return <ArrowDownload24Regular />;
      case RemediationActionType.OpenSettings:
      case RemediationActionType.Configure:
        return <Settings24Regular />;
      case RemediationActionType.Start:
        return <Play24Regular />;
      case RemediationActionType.OpenHelp:
        return <Open24Regular />;
      case RemediationActionType.SwitchProvider:
        return <WrenchScrewdriver24Regular />;
      default:
        return null;
    }
  };

  const handleRemediationAction = (action: RemediationAction) => {
    if (action.navigateTo) {
      navigate(action.navigateTo);
    } else if (action.externalUrl) {
      window.open(action.externalUrl, '_blank');
    }
  };

  const renderRemediationActions = (actions: RemediationAction[] | undefined) => {
    if (!actions || actions.length === 0) return null;

    return (
      <div className={styles.remediationActions}>
        {actions.map((action, index) => (
          <Button
            key={index}
            size="small"
            appearance="primary"
            icon={getActionIcon(action.type)}
            onClick={() => handleRemediationAction(action)}
            title={action.description}
          >
            {action.label}
          </Button>
        ))}
      </div>
    );
  };

  const renderCheckItem = (check: HealthCheckDetail) => {
    // Filter optional checks if not showing them
    if (!showOptional && !check.isRequired) {
      return null;
    }

    return (
      <Card key={check.id} className={styles.checkItem}>
        <div className={styles.checkIcon}>{getStatusIcon(check.status)}</div>
        <div className={styles.checkContent}>
          <div>
            <Text className={styles.checkName}>{check.name}</Text>
            <Text className={styles.checkCategory}> • {check.category}</Text>
          </div>
          {check.message && <Text>{check.message}</Text>}
          {check.remediationHint && <Text className={styles.hint}>💡 {check.remediationHint}</Text>}
          {renderRemediationActions(check.remediationActions)}
        </div>
        {getStatusBadge(check.status, check.isRequired)}
      </Card>
    );
  };

  const groupChecksByCategory = (checks: HealthCheckDetail[]) => {
    const grouped: Record<string, HealthCheckDetail[]> = {};
    checks.forEach((check) => {
      if (!grouped[check.category]) {
        grouped[check.category] = [];
      }
      grouped[check.category].push(check);
    });
    return grouped;
  };

  const renderChecks = () => {
    if (!details) return null;

    const filteredChecks = showOptional
      ? details.checks
      : details.checks.filter((c) => c.isRequired);

    const grouped = groupChecksByCategory(filteredChecks);

    return (
      <>
        {Object.entries(grouped).map(([category, checks]) => (
          <div key={category} className={styles.categorySection}>
            <Text className={styles.categoryTitle}>{category}</Text>
            <div className={styles.checkList}>{checks.map(renderCheckItem)}</div>
          </div>
        ))}
      </>
    );
  };

  const failedRequired = getRequiredFailedChecks();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>System Diagnostics</Title3>
        <Button
          appearance="primary"
          icon={isLoading ? <Spinner size="tiny" /> : <Play24Regular />}
          onClick={fetchHealthDetails}
          disabled={isLoading}
        >
          {isLoading ? 'Checking...' : 'Run Diagnostics'}
        </Button>
      </div>

      {error && (
        <Card>
          <Text style={{ color: tokens.colorPaletteRedForeground1 }}>Error: {error}</Text>
        </Card>
      )}

      {details && (
        <>
          {renderChecks()}

          <div className={styles.summary}>
            {details.isReady && failedRequired.length === 0 ? (
              <Badge appearance="filled" color="success" size="large">
                ✓ System ready for video generation
              </Badge>
            ) : (
              <Badge appearance="filled" color="warning" size="large">
                ⚠ {failedRequired.length} required check(s) failed - see above for fixes
              </Badge>
            )}
          </div>
        </>
      )}

      {!details && !isLoading && (
        <Card>
          <Text>
            Run diagnostics to verify that all system components are configured and available. This
            will check FFmpeg, providers, and other dependencies.
          </Text>
        </Card>
      )}
    </div>
  );
}
