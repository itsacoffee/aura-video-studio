/**
 * System Requirements Check Component
 *
 * Displays system requirements checking during first-run setup
 */

import {
  Card,
  makeStyles,
  tokens,
  Text,
  Title3,
  Spinner,
  Button,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Warning24Regular,
  Dismiss24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState, useCallback } from 'react';
import type { SystemRequirements } from '../../services/systemRequirementsService';
import {
  checkSystemRequirements,
  getSystemRecommendations,
} from '../../services/systemRequirementsService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  requirementCard: {
    padding: tokens.spacingVerticalL,
  },
  requirementHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  requirementDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginLeft: '36px',
  },
  statusIcon: {
    width: '24px',
    height: '24px',
  },
  warningList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  warningItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  recommendationsCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorPaletteBlueBackground2,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder1}`,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXXL,
  },
});

export interface SystemRequirementsCheckProps {
  onCheckComplete?: (requirements: SystemRequirements) => void;
}

export function SystemRequirementsCheck({ onCheckComplete }: SystemRequirementsCheckProps) {
  const styles = useStyles();
  const [requirements, setRequirements] = useState<SystemRequirements | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const performCheck = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await checkSystemRequirements();
      setRequirements(result);
      onCheckComplete?.(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to check system requirements');
    } finally {
      setLoading(false);
    }
  }, [onCheckComplete]);

  useEffect(() => {
    performCheck();
  }, [performCheck]);

  const getStatusIcon = (status: 'pass' | 'warning' | 'fail') => {
    switch (status) {
      case 'pass':
        return (
          <Checkmark24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteGreenForeground1 }}
          />
        );
      case 'warning':
        return (
          <Warning24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteYellowForeground1 }}
          />
        );
      case 'fail':
        return (
          <Dismiss24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
    }
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner size="large" label="Checking system requirements..." />
      </div>
    );
  }

  if (error) {
    return (
      <Card className={styles.requirementCard}>
        <div className={styles.requirementHeader}>
          <Dismiss24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
          <Title3>Error Checking Requirements</Title3>
        </div>
        <Text>{error}</Text>
        <Button
          appearance="secondary"
          onClick={performCheck}
          style={{ marginTop: tokens.spacingVerticalM }}
        >
          Retry
        </Button>
      </Card>
    );
  }

  if (!requirements) {
    return null;
  }

  const recommendations = getSystemRecommendations(requirements);

  return (
    <div className={styles.container}>
      {/* Overall Status */}
      <Card className={styles.requirementCard}>
        <div className={styles.requirementHeader}>
          {getStatusIcon(requirements.overall)}
          <Title3>System Requirements Check</Title3>
        </div>
        <Text>
          {requirements.overall === 'pass' &&
            'Your system meets all requirements for video generation.'}
          {requirements.overall === 'warning' &&
            'Your system meets minimum requirements, but there are some recommendations below.'}
          {requirements.overall === 'fail' &&
            'Your system does not meet minimum requirements. Please review the details below.'}
        </Text>
      </Card>

      {/* Disk Space */}
      <Card className={styles.requirementCard}>
        <div className={styles.requirementHeader}>
          {getStatusIcon(requirements.diskSpace.status)}
          <Title3>Disk Space</Title3>
        </div>
        <div className={styles.requirementDetails}>
          <Text>
            <strong>Available:</strong> {requirements.diskSpace.available.toFixed(2)} GB /{' '}
            {requirements.diskSpace.total.toFixed(2)} GB
          </Text>
          <Text>
            <strong>Free:</strong> {requirements.diskSpace.percentage.toFixed(1)}%
          </Text>
          {requirements.diskSpace.warnings.length > 0 && (
            <ul className={styles.warningList}>
              {requirements.diskSpace.warnings.map((warning, index) => (
                <li key={index} className={styles.warningItem}>
                  <Warning24Regular
                    style={{
                      width: '16px',
                      height: '16px',
                      color: tokens.colorPaletteYellowForeground1,
                      flexShrink: 0,
                    }}
                  />
                  <Text size={200}>{warning}</Text>
                </li>
              ))}
            </ul>
          )}
        </div>
      </Card>

      {/* GPU */}
      <Card className={styles.requirementCard}>
        <div className={styles.requirementHeader}>
          {getStatusIcon(requirements.gpu.status)}
          <Title3>Graphics Card (GPU)</Title3>
        </div>
        <div className={styles.requirementDetails}>
          {requirements.gpu.detected ? (
            <>
              <Text>
                <strong>Detected:</strong> {requirements.gpu.vendor} - {requirements.gpu.model}
              </Text>
              {requirements.gpu.memory && requirements.gpu.memory > 0 && (
                <Text>
                  <strong>Memory:</strong> {requirements.gpu.memory} MB
                </Text>
              )}
              <Text>
                <strong>Hardware Acceleration:</strong>{' '}
                {requirements.gpu.capabilities.hardwareAcceleration ? 'Yes' : 'No'}
              </Text>
              <Text>
                <strong>Video Encoding:</strong>{' '}
                {requirements.gpu.capabilities.videoEncoding ? 'Yes' : 'No'}
              </Text>
            </>
          ) : (
            <Text>No dedicated GPU detected</Text>
          )}
          {requirements.gpu.recommendations.length > 0 && (
            <ul className={styles.warningList} style={{ marginTop: tokens.spacingVerticalM }}>
              {requirements.gpu.recommendations.map((rec, index) => (
                <li key={index} className={styles.warningItem}>
                  <Info24Regular
                    style={{
                      width: '16px',
                      height: '16px',
                      color: tokens.colorPaletteBlueForeground2,
                      flexShrink: 0,
                    }}
                  />
                  <Text size={200}>{rec}</Text>
                </li>
              ))}
            </ul>
          )}
        </div>
      </Card>

      {/* Memory */}
      <Card className={styles.requirementCard}>
        <div className={styles.requirementHeader}>
          {getStatusIcon(requirements.memory.status)}
          <Title3>System Memory (RAM)</Title3>
        </div>
        <div className={styles.requirementDetails}>
          <Text>
            <strong>Total:</strong> {requirements.memory.total.toFixed(2)} GB
          </Text>
          <Text>
            <strong>Available:</strong> {requirements.memory.available.toFixed(2)} GB (
            {requirements.memory.percentage.toFixed(1)}%)
          </Text>
          {requirements.memory.warnings.length > 0 && (
            <ul className={styles.warningList}>
              {requirements.memory.warnings.map((warning, index) => (
                <li key={index} className={styles.warningItem}>
                  <Warning24Regular
                    style={{
                      width: '16px',
                      height: '16px',
                      color: tokens.colorPaletteYellowForeground1,
                      flexShrink: 0,
                    }}
                  />
                  <Text size={200}>{warning}</Text>
                </li>
              ))}
            </ul>
          )}
        </div>
      </Card>

      {/* Operating System */}
      <Card className={styles.requirementCard}>
        <div className={styles.requirementHeader}>
          {getStatusIcon(requirements.os.compatible ? 'pass' : 'fail')}
          <Title3>Operating System</Title3>
        </div>
        <div className={styles.requirementDetails}>
          <Text>
            <strong>Platform:</strong> {requirements.os.platform}
          </Text>
          <Text>
            <strong>Version:</strong> {requirements.os.version}
          </Text>
          <Text>
            <strong>Architecture:</strong> {requirements.os.architecture}
          </Text>
          {!requirements.os.compatible && (
            <div className={styles.warningItem}>
              <Warning24Regular
                style={{
                  width: '16px',
                  height: '16px',
                  color: tokens.colorPaletteRedForeground1,
                  flexShrink: 0,
                }}
              />
              <Text size={200}>Your operating system is not officially supported</Text>
            </div>
          )}
        </div>
      </Card>

      {/* Recommendations */}
      {recommendations.length > 0 && (
        <Card className={styles.recommendationsCard}>
          <div className={styles.requirementHeader}>
            <Info24Regular
              className={styles.statusIcon}
              style={{ color: tokens.colorPaletteBlueForeground2 }}
            />
            <Title3>Recommendations</Title3>
          </div>
          <ul className={styles.warningList}>
            {recommendations.map((rec, index) => (
              <li key={index} className={styles.warningItem}>
                <Info24Regular
                  style={{
                    width: '16px',
                    height: '16px',
                    color: tokens.colorPaletteBlueForeground2,
                    flexShrink: 0,
                  }}
                />
                <Text size={200}>{rec}</Text>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}
