import {
  Card,
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  ProgressBar,
  Tooltip,
} from '@fluentui/react-components';
import {
  BookInformationRegular,
  LinkRegular,
  CheckmarkCircleRegular,
  DismissCircleRegular,
  InfoRegular,
} from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
import type { FC } from 'react';
import type { ResearchFinding } from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    width: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  headerContent: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  icon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  findingsGrid: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  findingCard: {
    padding: tokens.spacingVerticalL,
    transition: 'all 0.2s ease',
    ':hover': {
      boxShadow: tokens.shadow8,
    },
  },
  findingHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalS,
  },
  findingContent: {
    flex: 1,
  },
  fact: {
    fontSize: tokens.fontSizeBase300,
    marginBottom: tokens.spacingVerticalS,
    lineHeight: tokens.lineHeightBase300,
  },
  example: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalS,
    fontStyle: 'italic',
  },
  exampleLabel: {
    fontWeight: tokens.fontWeightSemibold,
    fontStyle: 'normal',
    marginRight: tokens.spacingHorizontalXS,
  },
  source: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorBrandForeground1,
    marginTop: tokens.spacingVerticalXS,
  },
  scores: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    minWidth: '200px',
  },
  scoreRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  scoreLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: '48px',
  },
  loadingState: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalL,
  },
  filterBar: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  stats: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  statValue: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  statLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

export interface ResearchPanelProps {
  findings: ResearchFinding[];
  isLoading?: boolean;
  onRefresh?: () => void;
  onAccept?: (findingId: string) => void;
  onReject?: (findingId: string) => void;
  showActions?: boolean;
}

export const ResearchPanel: FC<ResearchPanelProps> = ({
  findings,
  isLoading = false,
  onRefresh,
  onAccept,
  onReject,
  showActions = true,
}) => {
  const styles = useStyles();
  const [filter, setFilter] = useState<'all' | 'high-credibility' | 'high-relevance'>('all');

  const getScoreColor = useCallback((score: number): 'brand' | 'success' | 'warning' | 'error' => {
    if (score >= 80) return 'success';
    if (score >= 60) return 'brand';
    if (score >= 40) return 'warning';
    return 'error';
  }, []);

  const filteredFindings = findings.filter((finding) => {
    if (filter === 'high-credibility') return finding.credibilityScore >= 70;
    if (filter === 'high-relevance') return finding.relevanceScore >= 70;
    return true;
  });

  const stats = {
    total: findings.length,
    highCredibility: findings.filter((f) => f.credibilityScore >= 70).length,
    highRelevance: findings.filter((f) => f.relevanceScore >= 70).length,
    avgCredibility: findings.length
      ? Math.round(findings.reduce((sum, f) => sum + f.credibilityScore, 0) / findings.length)
      : 0,
  };

  if (findings.length === 0 && !isLoading) {
    return (
      <div className={styles.emptyState}>
        <BookInformationRegular className={styles.emptyIcon} />
        <Text size={400}>No research findings available</Text>
        <Text size={300}>Generate research to see AI-gathered facts and insights</Text>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className={styles.loadingState}>
        <Spinner size="medium" />
        <Text>Gathering research...</Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <BookInformationRegular className={styles.icon} />
          <div>
            <Text size={500} weight="semibold">
              Research Findings
            </Text>
            <div>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                {filteredFindings.length} finding{filteredFindings.length !== 1 ? 's' : ''}
              </Text>
            </div>
          </div>
        </div>
        {onRefresh && (
          <Button appearance="subtle" onClick={onRefresh}>
            Refresh
          </Button>
        )}
      </div>

      <div className={styles.stats}>
        <div className={styles.statItem}>
          <Text className={styles.statValue}>{stats.total}</Text>
          <Text className={styles.statLabel}>Total Findings</Text>
        </div>
        <div className={styles.statItem}>
          <Text className={styles.statValue}>{stats.highCredibility}</Text>
          <Text className={styles.statLabel}>High Credibility</Text>
        </div>
        <div className={styles.statItem}>
          <Text className={styles.statValue}>{stats.highRelevance}</Text>
          <Text className={styles.statLabel}>High Relevance</Text>
        </div>
        <div className={styles.statItem}>
          <Text className={styles.statValue}>{stats.avgCredibility}%</Text>
          <Text className={styles.statLabel}>Avg Credibility</Text>
        </div>
      </div>

      <div className={styles.filterBar}>
        <Button
          appearance={filter === 'all' ? 'primary' : 'subtle'}
          size="small"
          onClick={() => setFilter('all')}
        >
          All ({findings.length})
        </Button>
        <Button
          appearance={filter === 'high-credibility' ? 'primary' : 'subtle'}
          size="small"
          onClick={() => setFilter('high-credibility')}
        >
          High Credibility ({stats.highCredibility})
        </Button>
        <Button
          appearance={filter === 'high-relevance' ? 'primary' : 'subtle'}
          size="small"
          onClick={() => setFilter('high-relevance')}
        >
          High Relevance ({stats.highRelevance})
        </Button>
      </div>

      <div className={styles.findingsGrid}>
        {filteredFindings.map((finding) => (
          <Card key={finding.findingId} className={styles.findingCard}>
            <div className={styles.findingHeader}>
              <div className={styles.findingContent}>
                <Text className={styles.fact}>{finding.fact}</Text>
                {finding.source && (
                  <a
                    href={finding.source}
                    target="_blank"
                    rel="noopener noreferrer"
                    className={styles.source}
                    style={{ textDecoration: 'none' }}
                  >
                    <LinkRegular />
                    <Text size={200}>View source</Text>
                  </a>
                )}
                {finding.example && (
                  <div className={styles.example}>
                    <Text className={styles.exampleLabel}>Example:</Text>
                    {finding.example}
                  </div>
                )}
              </div>
              <div className={styles.scores}>
                <div className={styles.scoreRow}>
                  <div className={styles.scoreLabel}>
                    <Text>Credibility</Text>
                    <Tooltip content="How trustworthy this information is" relationship="label">
                      <InfoRegular style={{ fontSize: '14px' }} />
                    </Tooltip>
                  </div>
                  <ProgressBar
                    value={finding.credibilityScore / 100}
                    color={getScoreColor(finding.credibilityScore)}
                    thickness="large"
                  />
                  <Text size={200} style={{ textAlign: 'right' }}>
                    {Math.round(finding.credibilityScore)}%
                  </Text>
                </div>
                <div className={styles.scoreRow}>
                  <div className={styles.scoreLabel}>
                    <Text>Relevance</Text>
                    <Tooltip content="How relevant this is to your topic" relationship="label">
                      <InfoRegular style={{ fontSize: '14px' }} />
                    </Tooltip>
                  </div>
                  <ProgressBar
                    value={finding.relevanceScore / 100}
                    color={getScoreColor(finding.relevanceScore)}
                    thickness="large"
                  />
                  <Text size={200} style={{ textAlign: 'right' }}>
                    {Math.round(finding.relevanceScore)}%
                  </Text>
                </div>
              </div>
            </div>
            {showActions && (onAccept || onReject) && (
              <div className={styles.actions}>
                {onAccept && (
                  <Button
                    appearance="subtle"
                    icon={<CheckmarkCircleRegular />}
                    onClick={() => onAccept(finding.findingId)}
                  >
                    Accept
                  </Button>
                )}
                {onReject && (
                  <Button
                    appearance="subtle"
                    icon={<DismissCircleRegular />}
                    onClick={() => onReject(finding.findingId)}
                  >
                    Reject
                  </Button>
                )}
              </div>
            )}
          </Card>
        ))}
      </div>
    </div>
  );
};
