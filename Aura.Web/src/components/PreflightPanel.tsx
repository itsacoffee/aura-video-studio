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
} from '@fluentui/react-icons';
import type { PreflightReport, StageCheck, CheckStatus } from '../state/providers';

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
});

interface PreflightPanelProps {
  profile: string;
  report: PreflightReport | null;
  isRunning: boolean;
  onRunPreflight: () => void;
}

export function PreflightPanel({ profile, report, isRunning, onRunPreflight }: PreflightPanelProps) {
  const styles = useStyles();

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
        return <Badge appearance="filled" color="success">Pass</Badge>;
      case 'warn':
        return <Badge appearance="filled" color="warning">Warning</Badge>;
      case 'fail':
        return <Badge appearance="filled" color="danger">Failed</Badge>;
    }
  };

  const renderStageCheck = (stage: StageCheck) => (
    <Card key={stage.stage} className={styles.stageItem}>
      <div className={styles.stageIcon}>
        {getStatusIcon(stage.status)}
      </div>
      <div className={styles.stageContent}>
        <div>
          <Text className={styles.stageName}>{stage.stage}</Text>
          {' - '}
          <Text>{stage.provider}</Text>
        </div>
        <Text>{stage.message}</Text>
        {stage.hint && (
          <Text className={styles.hint}>💡 {stage.hint}</Text>
        )}
      </div>
      {getStatusBadge(stage.status)}
    </Card>
  );

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
          <div className={styles.stageList}>
            {report.stages.map(renderStageCheck)}
          </div>

          <div className={styles.summary}>
            {report.ok ? (
              <Badge appearance="filled" color="success" size="large">
                ✓ All systems ready
              </Badge>
            ) : (
              <Badge appearance="filled" color="warning" size="large">
                ⚠ Some checks failed - review above
              </Badge>
            )}
          </div>
        </>
      )}

      {!report && !isRunning && (
        <Card>
          <Text>
            Run a preflight check to verify that all providers for the <strong>{profile}</strong> profile are configured and reachable.
          </Text>
        </Card>
      )}
    </div>
  );
}
