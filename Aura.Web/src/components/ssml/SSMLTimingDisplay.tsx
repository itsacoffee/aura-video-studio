/**
 * SSML Timing Display Component
 * Displays duration fitting statistics and tolerance metrics
 */

import { Card, Text, Badge } from '@fluentui/react-components';
import type { FC } from 'react';
import type { DurationFittingStats } from '@/services/ssmlService';

interface SSMLTimingDisplayProps {
  stats: DurationFittingStats;
}

export const SSMLTimingDisplay: FC<SSMLTimingDisplayProps> = ({ stats }) => {
  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = (seconds % 60).toFixed(1);
    return mins > 0 ? `${mins}m ${secs}s` : `${secs}s`;
  };

  const getDeviationColor = (deviation: number): 'success' | 'warning' | 'danger' => {
    if (deviation <= 2) return 'success';
    if (deviation <= 5) return 'warning';
    return 'danger';
  };

  return (
    <Card>
      <div style={{ padding: '16px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
        <Text weight="semibold" size={400}>
          Duration Fitting Statistics
        </Text>

        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
            gap: '12px',
          }}
        >
          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Within Tolerance
            </Text>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Text weight="semibold" size={500}>
                {stats.withinTolerancePercent.toFixed(1)}%
              </Text>
              <Badge
                appearance="filled"
                color={
                  stats.withinTolerancePercent >= 95
                    ? 'success'
                    : stats.withinTolerancePercent >= 80
                      ? 'warning'
                      : 'danger'
                }
              >
                {stats.withinTolerancePercent >= 95
                  ? 'Excellent'
                  : stats.withinTolerancePercent >= 80
                    ? 'Good'
                    : 'Needs Work'}
              </Badge>
            </div>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Segments Adjusted
            </Text>
            <Text weight="semibold" size={500}>
              {stats.segmentsAdjusted}
            </Text>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Avg Fit Iterations
            </Text>
            <Text weight="semibold" size={500}>
              {stats.averageFitIterations.toFixed(1)}
            </Text>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Max Fit Iterations
            </Text>
            <Text weight="semibold" size={500}>
              {stats.maxFitIterations}
            </Text>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Avg Deviation
            </Text>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Text weight="semibold" size={500}>
                {stats.averageDeviation.toFixed(2)}%
              </Text>
              <Badge appearance="outline" color={getDeviationColor(stats.averageDeviation)} />
            </div>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Max Deviation
            </Text>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Text weight="semibold" size={500}>
                {stats.maxDeviation.toFixed(2)}%
              </Text>
              <Badge appearance="outline" color={getDeviationColor(stats.maxDeviation)} />
            </div>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Target Duration
            </Text>
            <Text weight="semibold" size={500}>
              {formatDuration(stats.targetDurationSeconds)}
            </Text>
          </div>

          <div>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Actual Duration
            </Text>
            <Text weight="semibold" size={500}>
              {formatDuration(stats.actualDurationSeconds)}
            </Text>
          </div>
        </div>
      </div>
    </Card>
  );
};
