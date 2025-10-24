/**
 * Attention Curve Chart Component
 * Interactive line chart showing predicted viewer attention over time
 */

import { useState, useMemo } from 'react';
import {
  Card,
  makeStyles,
  tokens,
  Title3,
  Caption1,
  Body1,
} from '@fluentui/react-components';
import { AttentionCurveData } from '../../types/pacing';
import { durationToSeconds, formatDuration } from '../../services/pacingService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  chartWrapper: {
    position: 'relative',
    width: '100%',
    height: '400px',
    '@media (max-width: 768px)': {
      height: '300px',
    },
  },
  svg: {
    width: '100%',
    height: '100%',
  },
  tooltip: {
    position: 'absolute',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow16,
    pointerEvents: 'none',
    zIndex: 1000,
  },
  legend: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalM,
    flexWrap: 'wrap',
  },
  legendItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  legendColor: {
    width: '16px',
    height: '16px',
    borderRadius: tokens.borderRadiusSmall,
  },
});

interface AttentionCurveChartProps {
  data: AttentionCurveData;
  height?: number;
}

interface TooltipData {
  x: number;
  y: number;
  timestamp: string;
  attentionLevel: number;
  retentionRate: number;
  engagementScore: number;
}

export const AttentionCurveChart: React.FC<AttentionCurveChartProps> = ({
  data,
  height = 400,
}) => {
  const styles = useStyles();
  const [tooltip, setTooltip] = useState<TooltipData | null>(null);

  // Chart dimensions
  const margin = { top: 20, right: 30, bottom: 50, left: 50 };
  const width = 900; // Base width, actual width is responsive
  const chartHeight = height - margin.top - margin.bottom;
  const chartWidth = width - margin.left - margin.right;

  // Process data points
  const processedData = useMemo(() => {
    if (!data.dataPoints || data.dataPoints.length === 0) return [];

    return data.dataPoints.map(point => ({
      seconds: durationToSeconds(point.timestamp),
      attentionLevel: point.attentionLevel,
      retentionRate: point.retentionRate,
      engagementScore: point.engagementScore,
      timestamp: point.timestamp,
    }));
  }, [data.dataPoints]);

  // Calculate scales
  const { xScale, yScale } = useMemo(() => {
    if (processedData.length === 0) {
      return { xScale: (_x: number) => 0, yScale: (_y: number) => 0 };
    }

    const maxTime = Math.max(...processedData.map(d => d.seconds));
    const xScale = (seconds: number) => (seconds / maxTime) * chartWidth;
    const yScale = (value: number) => chartHeight - (value / 100) * chartHeight;

    return { xScale, yScale };
  }, [processedData, chartWidth, chartHeight]);

  // Generate path for attention curve
  const attentionPath = useMemo(() => {
    if (processedData.length === 0) return '';

    return processedData
      .map((point, i) => {
        const x = xScale(point.seconds);
        const y = yScale(point.attentionLevel);
        return i === 0 ? `M ${x} ${y}` : `L ${x} ${y}`;
      })
      .join(' ');
  }, [processedData, xScale, yScale]);

  // Generate path for retention curve
  const retentionPath = useMemo(() => {
    if (processedData.length === 0) return '';

    return processedData
      .map((point, i) => {
        const xPos = xScale(point.seconds);
        const yPos = yScale(point.retentionRate);
        return i === 0 ? `M ${xPos} ${yPos}` : `L ${xPos} ${yPos}`;
      })
      .join(' ');
  }, [processedData, xScale, yScale]);

  // Get color for attention zone
  const getZoneColor = (level: number): string => {
    if (level >= 70) return tokens.colorPaletteGreenBackground3;
    if (level >= 40) return tokens.colorPaletteYellowBackground3;
    return tokens.colorPaletteRedBackground3;
  };

  // Handle mouse move over chart
  const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
    const svg = e.currentTarget;
    const rect = svg.getBoundingClientRect();
    const mouseX = e.clientX - rect.left - margin.left;

    // Find closest data point
    const relativeX = (mouseX / chartWidth) * Math.max(...processedData.map(d => d.seconds));
    const closest = processedData.reduce((prev, curr) =>
      Math.abs(curr.seconds - relativeX) < Math.abs(prev.seconds - relativeX) ? curr : prev
    );

    if (closest) {
      setTooltip({
        x: e.clientX - rect.left,
        y: e.clientY - rect.top,
        timestamp: closest.timestamp,
        attentionLevel: closest.attentionLevel,
        retentionRate: closest.retentionRate,
        engagementScore: closest.engagementScore,
      });
    }
  };

  const handleMouseLeave = () => {
    setTooltip(null);
  };

  if (processedData.length === 0) {
    return (
      <Card className={styles.container}>
        <div className={styles.header}>
          <Title3>Attention Curve</Title3>
        </div>
        <Body1>No attention data available</Body1>
      </Card>
    );
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Title3>Attention Curve</Title3>
        <Caption1>Predicted viewer attention over video timeline</Caption1>
      </div>

      <div className={styles.chartWrapper}>
        <svg
          className={styles.svg}
          viewBox={`0 0 ${width} ${height}`}
          preserveAspectRatio="xMidYMid meet"
          onMouseMove={handleMouseMove}
          onMouseLeave={handleMouseLeave}
        >
          {/* Background zones */}
          <defs>
            <linearGradient id="attentionGradient" x1="0%" y1="0%" x2="0%" y2="100%">
              <stop offset="0%" stopColor={tokens.colorPaletteGreenBackground3} stopOpacity="0.3" />
              <stop offset="50%" stopColor={tokens.colorPaletteYellowBackground3} stopOpacity="0.3" />
              <stop offset="100%" stopColor={tokens.colorPaletteRedBackground3} stopOpacity="0.3" />
            </linearGradient>
          </defs>

          <g transform={`translate(${margin.left},${margin.top})`}>
            {/* Background gradient */}
            <rect
              x={0}
              y={0}
              width={chartWidth}
              height={chartHeight}
              fill="url(#attentionGradient)"
            />

            {/* Grid lines */}
            {[0, 25, 50, 75, 100].map(value => (
              <g key={value}>
                <line
                  x1={0}
                  y1={yScale(value)}
                  x2={chartWidth}
                  y2={yScale(value)}
                  stroke={tokens.colorNeutralStroke2}
                  strokeWidth="1"
                  strokeDasharray="4 4"
                />
                <text
                  x={-10}
                  y={yScale(value)}
                  textAnchor="end"
                  dominantBaseline="middle"
                  fill={tokens.colorNeutralForeground3}
                  fontSize="12px"
                >
                  {value}%
                </text>
              </g>
            ))}

            {/* X-axis labels */}
            {processedData.filter((_, i) => i % Math.ceil(processedData.length / 8) === 0).map((point, i) => (
              <text
                key={i}
                x={xScale(point.seconds)}
                y={chartHeight + 20}
                textAnchor="middle"
                fill={tokens.colorNeutralForeground3}
                fontSize="12px"
              >
                {formatDuration(point.timestamp)}
              </text>
            ))}

            {/* Retention curve */}
            <path
              d={retentionPath}
              fill="none"
              stroke={tokens.colorBrandForeground2}
              strokeWidth="2"
              strokeDasharray="5 5"
            />

            {/* Attention curve */}
            <path
              d={attentionPath}
              fill="none"
              stroke={tokens.colorBrandForeground1}
              strokeWidth="3"
            />

            {/* Data points */}
            {processedData.map((point, i) => (
              <circle
                key={i}
                cx={xScale(point.seconds)}
                cy={yScale(point.attentionLevel)}
                r="4"
                fill={getZoneColor(point.attentionLevel)}
                stroke={tokens.colorBrandForeground1}
                strokeWidth="2"
              />
            ))}

            {/* Engagement peaks markers */}
            {data.engagementPeaks?.map((peak, i) => {
              const seconds = durationToSeconds(peak);
              return (
                <line
                  key={`peak-${i}`}
                  x1={xScale(seconds)}
                  y1={0}
                  x2={xScale(seconds)}
                  y2={chartHeight}
                  stroke={tokens.colorPaletteGreenForeground1}
                  strokeWidth="2"
                  strokeDasharray="2 2"
                  opacity="0.5"
                />
              );
            })}

            {/* Engagement valleys markers */}
            {data.engagementValleys?.map((valley, i) => {
              const seconds = durationToSeconds(valley);
              return (
                <line
                  key={`valley-${i}`}
                  x1={xScale(seconds)}
                  y1={0}
                  x2={xScale(seconds)}
                  y2={chartHeight}
                  stroke={tokens.colorPaletteRedForeground1}
                  strokeWidth="2"
                  strokeDasharray="2 2"
                  opacity="0.5"
                />
              );
            })}
          </g>
        </svg>

        {/* Tooltip */}
        {tooltip && (
          <div
            className={styles.tooltip}
            style={{
              left: tooltip.x + 10,
              top: tooltip.y - 80,
            }}
          >
            <Caption1 style={{ fontWeight: 600 }}>
              {formatDuration(tooltip.timestamp)}
            </Caption1>
            <Caption1>Attention: {tooltip.attentionLevel.toFixed(1)}%</Caption1>
            <Caption1>Retention: {tooltip.retentionRate.toFixed(1)}%</Caption1>
            <Caption1>Engagement: {tooltip.engagementScore.toFixed(1)}%</Caption1>
          </div>
        )}
      </div>

      {/* Legend */}
      <div className={styles.legend}>
        <div className={styles.legendItem}>
          <div
            className={styles.legendColor}
            style={{ backgroundColor: tokens.colorBrandForeground1 }}
          />
          <Caption1>Attention Level</Caption1>
        </div>
        <div className={styles.legendItem}>
          <div
            className={styles.legendColor}
            style={{ backgroundColor: tokens.colorBrandForeground2 }}
          />
          <Caption1>Retention Rate</Caption1>
        </div>
        <div className={styles.legendItem}>
          <div
            className={styles.legendColor}
            style={{ backgroundColor: tokens.colorPaletteGreenBackground3 }}
          />
          <Caption1>High Attention (&gt;70%)</Caption1>
        </div>
        <div className={styles.legendItem}>
          <div
            className={styles.legendColor}
            style={{ backgroundColor: tokens.colorPaletteYellowBackground3 }}
          />
          <Caption1>Medium Attention (40-70%)</Caption1>
        </div>
        <div className={styles.legendItem}>
          <div
            className={styles.legendColor}
            style={{ backgroundColor: tokens.colorPaletteRedBackground3 }}
          />
          <Caption1>Low Attention (&lt;40%)</Caption1>
        </div>
      </div>
    </Card>
  );
};
