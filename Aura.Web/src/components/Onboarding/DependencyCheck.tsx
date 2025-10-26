import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Button,
  Spinner,
  Badge,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Dismiss24Regular,
  Warning24Regular,
  ArrowDownload24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  dependenciesGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr',
    gap: tokens.spacingVerticalM,
  },
  dependencyCard: {
    padding: tokens.spacingVerticalL,
    transition: 'all 0.2s ease-in-out',
    ':hover': {
      boxShadow: tokens.shadow8,
    },
  },
  dependencyHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  dependencyInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flex: 1,
  },
  statusIcon: {
    fontSize: '32px',
  },
  dependencyDetails: {
    flex: 1,
  },
  dependencyName: {
    marginBottom: tokens.spacingVerticalXS,
  },
  dependencyDescription: {
    color: tokens.colorNeutralForeground3,
  },
  actionsContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  detailsSection: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  progressSection: {
    marginTop: tokens.spacingVerticalM,
  },
  summaryCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground3,
    marginTop: tokens.spacingVerticalL,
  },
  statusList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  statusItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
});

export interface Dependency {
  id: string;
  name: string;
  description: string;
  required: boolean;
  status: 'checking' | 'installed' | 'missing' | 'error';
  version?: string;
  installPath?: string;
  canAutoInstall: boolean;
  installing?: boolean;
  installProgress?: number;
  errorMessage?: string;
}

export interface DependencyCheckProps {
  dependencies: Dependency[];
  onAutoInstall: (dependencyId: string) => Promise<void>;
  onManualInstall: (dependencyId: string) => void;
  onSkip: (dependencyId: string) => void;
  onRescan?: () => Promise<void>;
  isScanning?: boolean;
}

export function DependencyCheck({
  dependencies,
  onAutoInstall,
  onManualInstall,
  onSkip,
  onRescan,
  isScanning = false,
}: DependencyCheckProps) {
  const styles = useStyles();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  useEffect(() => {
    // Auto-expand first missing required dependency
    const firstMissing = dependencies.find(
      (dep) => dep.required && dep.status === 'missing'
    );
    if (firstMissing && !expandedId) {
      setExpandedId(firstMissing.id);
    }
  }, [dependencies, expandedId]);

  const getStatusIcon = (status: Dependency['status']) => {
    switch (status) {
      case 'checking':
        return <Spinner size="medium" className={styles.statusIcon} />;
      case 'installed':
        return (
          <Checkmark24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteGreenForeground1 }}
          />
        );
      case 'missing':
        return (
          <Warning24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteYellowForeground1 }}
          />
        );
      case 'error':
        return (
          <Dismiss24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
    }
  };

  const getStatusBadge = (dep: Dependency) => {
    if (dep.installing) {
      return (
        <Badge appearance="filled" color="informative">
          Installing...
        </Badge>
      );
    }
    switch (dep.status) {
      case 'checking':
        return <Badge appearance="outline">Checking...</Badge>;
      case 'installed':
        return (
          <Badge appearance="filled" color="success">
            Installed
          </Badge>
        );
      case 'missing':
        return (
          <Badge appearance="filled" color="warning">
            Not Found
          </Badge>
        );
      case 'error':
        return (
          <Badge appearance="filled" color="danger">
            Error
          </Badge>
        );
    }
  };

  const installedCount = dependencies.filter((d) => d.status === 'installed').length;
  const requiredCount = dependencies.filter((d) => d.required).length;
  const requiredInstalled = dependencies.filter(
    (d) => d.required && d.status === 'installed'
  ).length;

  const allRequiredInstalled = requiredInstalled === requiredCount;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Dependency Validation</Title2>
        <Text>
          Checking for required components to ensure the best experience. We&apos;ll help you install
          anything that&apos;s missing.
        </Text>
      </div>

      {/* Overall Summary */}
      <Card className={styles.summaryCard}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <Title3>
              {allRequiredInstalled ? '✓ All Required Dependencies Installed' : 'Dependencies Status'}
            </Title3>
            <Text size={300} style={{ marginTop: tokens.spacingVerticalXS }}>
              {installedCount} of {dependencies.length} components installed
              {requiredCount > 0 && ` (${requiredInstalled}/${requiredCount} required)`}
            </Text>
          </div>
          {onRescan && (
            <Button
              appearance="secondary"
              onClick={onRescan}
              disabled={isScanning}
              icon={isScanning ? <Spinner size="tiny" /> : <Settings24Regular />}
            >
              {isScanning ? 'Scanning...' : 'Rescan'}
            </Button>
          )}
        </div>
      </Card>

      {/* Dependencies List */}
      <div className={styles.dependenciesGrid}>
        {dependencies.map((dep) => (
          <Card key={dep.id} className={styles.dependencyCard}>
            <div className={styles.dependencyHeader}>
              <div className={styles.dependencyInfo}>
                {getStatusIcon(dep.status)}
                <div className={styles.dependencyDetails}>
                  <Title3 className={styles.dependencyName}>
                    {dep.name}
                    {dep.required && (
                      <Badge
                        appearance="tint"
                        color="danger"
                        style={{ marginLeft: tokens.spacingHorizontalS }}
                      >
                        Required
                      </Badge>
                    )}
                  </Title3>
                  <Text className={styles.dependencyDescription} size={200}>
                    {dep.description}
                  </Text>
                </div>
              </div>
              <div className={styles.actionsContainer}>
                {getStatusBadge(dep)}
              </div>
            </div>

            {/* Installation Progress */}
            {dep.installing && dep.installProgress !== undefined && (
              <div className={styles.progressSection}>
                <ProgressBar value={dep.installProgress} max={100} />
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Installing... {dep.installProgress}%
                </Text>
              </div>
            )}

            {/* Details Section */}
            {expandedId === dep.id && dep.status !== 'checking' && (
              <div className={styles.detailsSection}>
                {dep.status === 'installed' && (
                  <>
                    <div className={styles.detailRow}>
                      <Text weight="semibold">Version:</Text>
                      <Text>{dep.version || 'Unknown'}</Text>
                    </div>
                    {dep.installPath && (
                      <div className={styles.detailRow}>
                        <Text weight="semibold">Location:</Text>
                        <Text size={200}>{dep.installPath}</Text>
                      </div>
                    )}
                  </>
                )}

                {dep.status === 'missing' && (
                  <>
                    <Text weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
                      Installation Options:
                    </Text>
                    <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, flexWrap: 'wrap' }}>
                      {dep.canAutoInstall && (
                        <Button
                          appearance="primary"
                          icon={<ArrowDownload24Regular />}
                          onClick={() => onAutoInstall(dep.id)}
                          disabled={dep.installing}
                        >
                          Auto Install
                        </Button>
                      )}
                      <Button
                        appearance="secondary"
                        onClick={() => onManualInstall(dep.id)}
                        disabled={dep.installing}
                      >
                        Manual Setup
                      </Button>
                      {!dep.required && (
                        <Button
                          appearance="subtle"
                          onClick={() => onSkip(dep.id)}
                          disabled={dep.installing}
                        >
                          Skip
                        </Button>
                      )}
                    </div>
                  </>
                )}

                {dep.status === 'error' && dep.errorMessage && (
                  <div>
                    <Text weight="semibold" style={{ color: tokens.colorPaletteRedForeground1 }}>
                      Error:
                    </Text>
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                      {dep.errorMessage}
                    </Text>
                  </div>
                )}
              </div>
            )}

            {/* Toggle Details Button */}
            {dep.status !== 'checking' && (
              <Button
                appearance="subtle"
                size="small"
                onClick={() => setExpandedId(expandedId === dep.id ? null : dep.id)}
                style={{ marginTop: tokens.spacingVerticalS }}
              >
                {expandedId === dep.id ? 'Hide Details' : 'Show Details'}
              </Button>
            )}
          </Card>
        ))}
      </div>
    </div>
  );
}
