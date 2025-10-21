import React from 'react';
import { Card, Text, makeStyles, tokens } from '@fluentui/react-components';
import type {
  EmotionalArc,
  EmotionalPoint,
  EmotionalTone,
} from '../../services/scriptEnhancementService';
import { scriptEnhancementService } from '../../services/scriptEnhancementService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalM,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  metrics: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  metric: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  chart: {
    position: 'relative',
    width: '100%',
    height: '200px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
    overflow: 'hidden',
  },
  curve: {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    height: '100%',
  },
  point: {
    position: 'absolute',
    width: '12px',
    height: '12px',
    borderRadius: '50%',
    border: '2px solid white',
    transform: 'translate(-50%, -50%)',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      transform: 'translate(-50%, -50%) scale(1.5)',
    },
  },
  moments: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  moment: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandBackground}`,
  },
});

interface EmotionalArcVisualizerProps {
  arc: EmotionalArc;
  title?: string;
}

export const EmotionalArcVisualizer: React.FC<EmotionalArcVisualizerProps> = ({
  arc,
  title = 'Emotional Arc',
}) => {
  const styles = useStyles();

  // Convert emotional points to SVG path
  const generatePath = (points: EmotionalPoint[]): string => {
    if (points.length === 0) return '';

    const width = 100; // percentage
    const height = 100; // percentage

    let path = '';
    points.forEach((point, index) => {
      const x = point.timePosition * width;
      const y = height - (point.intensity / 100) * height; // Invert Y axis

      if (index === 0) {
        path += `M ${x} ${y}`;
      } else {
        // Use quadratic bezier curves for smooth line
        const prevPoint = points[index - 1];
        const prevX = prevPoint.timePosition * width;
        const prevY = height - (prevPoint.intensity / 100) * height;
        const cpX = (prevX + x) / 2;
        const cpY = (prevY + y) / 2;
        path += ` Q ${cpX} ${cpY} ${x} ${y}`;
      }
    });

    return path;
  };

  const pathData = generatePath(arc.targetCurve);

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Text size={500} weight="semibold">
          {title}
        </Text>
      </div>

      <div className={styles.metrics}>
        <div className={styles.metric}>
          <Text size={200} weight="semibold">
            Smoothness
          </Text>
          <Text size={400} weight="bold">
            {Math.round(arc.curveSmoothnessScore)}%
          </Text>
        </div>
        <div className={styles.metric}>
          <Text size={200} weight="semibold">
            Variety
          </Text>
          <Text size={400} weight="bold">
            {Math.round(arc.varietyScore)}%
          </Text>
        </div>
        <div className={styles.metric}>
          <Text size={200} weight="semibold">
            Strategy
          </Text>
          <Text size={200}>{arc.arcStrategy}</Text>
        </div>
      </div>

      <div className={styles.chart}>
        <svg width="100%" height="100%" viewBox="0 0 100 100" preserveAspectRatio="none">
          {/* Grid lines */}
          <line
            x1="0"
            y1="25"
            x2="100"
            y2="25"
            stroke={tokens.colorNeutralStroke2}
            strokeWidth="0.5"
            strokeDasharray="2,2"
          />
          <line
            x1="0"
            y1="50"
            x2="100"
            y2="50"
            stroke={tokens.colorNeutralStroke2}
            strokeWidth="0.5"
            strokeDasharray="2,2"
          />
          <line
            x1="0"
            y1="75"
            x2="100"
            y2="75"
            stroke={tokens.colorNeutralStroke2}
            strokeWidth="0.5"
            strokeDasharray="2,2"
          />

          {/* Emotional curve */}
          <path
            d={pathData}
            fill="none"
            stroke={tokens.colorBrandBackground}
            strokeWidth="2"
            vectorEffect="non-scaling-stroke"
          />

          {/* Data points */}
          {arc.targetCurve.map((point, index) => {
            const x = point.timePosition * 100;
            const y = 100 - (point.intensity / 100) * 100;
            const color = scriptEnhancementService.getEmotionalToneColor(
              point.tone as EmotionalTone
            );

            return (
              <g key={index}>
                <circle
                  cx={x}
                  cy={y}
                  r="1.5"
                  fill={color}
                  stroke="white"
                  strokeWidth="0.5"
                  vectorEffect="non-scaling-stroke"
                >
                  <title>
                    {point.tone} ({Math.round(point.intensity)}%) at{' '}
                    {Math.round(point.timePosition * 100)}%
                  </title>
                </circle>
              </g>
            );
          })}
        </svg>
      </div>

      {arc.peakMoments.length > 0 && (
        <div className={styles.moments}>
          <Text size={300} weight="semibold">
            Peak Moments
          </Text>
          {arc.peakMoments.map((moment, index) => (
            <div key={index} className={styles.moment}>
              <Text size={200}>🎯 {moment}</Text>
            </div>
          ))}
        </div>
      )}

      {arc.valleyMoments.length > 0 && (
        <div className={styles.moments} style={{ marginTop: tokens.spacingVerticalM }}>
          <Text size={300} weight="semibold">
            Calm Moments
          </Text>
          {arc.valleyMoments.map((moment, index) => (
            <div key={index} className={styles.moment}>
              <Text size={200}>😌 {moment}</Text>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
};
