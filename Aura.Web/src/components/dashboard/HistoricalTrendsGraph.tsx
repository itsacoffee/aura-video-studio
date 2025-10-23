import React, { useEffect } from 'react';
import {
  Card,
  Title3,
  Body1,
  Body2,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { useQualityDashboardStore } from '../../state/qualityDashboard';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  chartCard: {
    padding: tokens.spacingVerticalL,
  },
  chartContainer: {
    width: '100%',
    height: '400px',
    marginTop: tokens.spacingVerticalM,
    position: 'relative',
  },
  trendInfo: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    marginTop: tokens.spacingVerticalM,
  },
  trendItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  trendValue: {
    fontSize: '24px',
    fontWeight: 'bold',
  },
  improving: {
    color: tokens.colorPaletteGreenForeground1,
  },
  declining: {
    color: tokens.colorPaletteRedForeground1,
  },
  stable: {
    color: tokens.colorNeutralForeground1,
  },
  svg: {
    width: '100%',
    height: '100%',
  },
  grid: {
    stroke: tokens.colorNeutralStroke2,
    strokeWidth: '1',
  },
  line: {
    fill: 'none',
    stroke: tokens.colorBrandBackground,
    strokeWidth: '3',
  },
  point: {
    fill: tokens.colorBrandBackground,
  },
  axis: {
    stroke: tokens.colorNeutralStroke1,
    strokeWidth: '1',
  },
  axisLabel: {
    fill: tokens.colorNeutralForeground2,
    fontSize: '12px',
  },
});

export const HistoricalTrendsGraph: React.FC = () => {
  const styles = useStyles();
  const { historicalTrends, fetchHistoricalData } = useQualityDashboardStore();

  useEffect(() => {
    if (!historicalTrends) {
      fetchHistoricalData();
    }
  }, [historicalTrends, fetchHistoricalData]);

  if (!historicalTrends || !historicalTrends.dataPoints.length) {
    return <Body1>No historical data available</Body1>;
  }

  const { dataPoints, trendDirection, averageChange } = historicalTrends;

  // Calculate chart dimensions
  const width = 1200;
  const height = 400;
  const padding = { top: 20, right: 40, bottom: 40, left: 60 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;

  // Find min/max values
  const qualityScores = dataPoints.map((d) => d.qualityScore);
  const minScore = Math.min(...qualityScores);
  const maxScore = Math.max(...qualityScores);
  const scoreRange = maxScore - minScore || 1;

  // Create scale functions
  const xScale = (index: number) => (index / (dataPoints.length - 1)) * chartWidth;
  const yScale = (score: number) =>
    chartHeight - ((score - minScore) / scoreRange) * chartHeight;

  // Generate path data
  const pathData = dataPoints
    .map((point, index) => {
      const x = xScale(index) + padding.left;
      const y = yScale(point.qualityScore) + padding.top;
      return index === 0 ? `M ${x} ${y}` : `L ${x} ${y}`;
    })
    .join(' ');

  // Generate grid lines
  const gridLines = [];
  for (let i = 0; i <= 5; i++) {
    const y = (i / 5) * chartHeight + padding.top;
    gridLines.push(
      <line
        key={`grid-${i}`}
        x1={padding.left}
        y1={y}
        x2={width - padding.right}
        y2={y}
        className={styles.grid}
      />
    );
  }

  const getTrendClassName = () => {
    switch (trendDirection) {
      case 'improving':
        return styles.improving;
      case 'declining':
        return styles.declining;
      default:
        return styles.stable;
    }
  };

  return (
    <div className={styles.container}>
      <Card className={styles.chartCard}>
        <Title3>Quality Score Trends</Title3>
        <Body2>
          Last {dataPoints.length} days - {trendDirection} trend
        </Body2>

        <div className={styles.chartContainer}>
          <svg viewBox={`0 0 ${width} ${height}`} className={styles.svg}>
            {/* Grid lines */}
            {gridLines}

            {/* Y-axis labels */}
            {[0, 1, 2, 3, 4, 5].map((i) => {
              const score = minScore + (scoreRange * i) / 5;
              const y = chartHeight - (i / 5) * chartHeight + padding.top;
              return (
                <text
                  key={`y-label-${i}`}
                  x={padding.left - 10}
                  y={y}
                  textAnchor="end"
                  alignmentBaseline="middle"
                  className={styles.axisLabel}
                >
                  {score.toFixed(1)}
                </text>
              );
            })}

            {/* X-axis labels */}
            {dataPoints
              .filter((_, i) => i % Math.ceil(dataPoints.length / 7) === 0)
              .map((point, index) => {
                const originalIndex = dataPoints.indexOf(point);
                const x = xScale(originalIndex) + padding.left;
                const date = new Date(point.timestamp);
                return (
                  <text
                    key={`x-label-${index}`}
                    x={x}
                    y={height - padding.bottom + 20}
                    textAnchor="middle"
                    className={styles.axisLabel}
                  >
                    {date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                  </text>
                );
              })}

            {/* Line chart */}
            <path d={pathData} className={styles.line} />

            {/* Data points */}
            {dataPoints.map((point, index) => {
              const x = xScale(index) + padding.left;
              const y = yScale(point.qualityScore) + padding.top;
              return <circle key={`point-${index}`} cx={x} cy={y} r="4" className={styles.point} />;
            })}
          </svg>
        </div>

        <div className={styles.trendInfo}>
          <div className={styles.trendItem}>
            <Body2>Trend Direction</Body2>
            <div className={`${styles.trendValue} ${getTrendClassName()}`}>
              {trendDirection.charAt(0).toUpperCase() + trendDirection.slice(1)}
            </div>
          </div>
          <div className={styles.trendItem}>
            <Body2>Average Change</Body2>
            <div className={`${styles.trendValue} ${getTrendClassName()}`}>
              {averageChange > 0 ? '+' : ''}
              {averageChange.toFixed(2)}
            </div>
          </div>
          <div className={styles.trendItem}>
            <Body2>Data Points</Body2>
            <div className={styles.trendValue}>{dataPoints.length}</div>
          </div>
        </div>
      </Card>
    </div>
  );
};
