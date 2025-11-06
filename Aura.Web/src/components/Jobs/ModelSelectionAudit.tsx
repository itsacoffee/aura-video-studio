import React, { useEffect, useState } from 'react';
import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Text,
  Badge,
  Spinner,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import {
  CheckmarkCircle20Regular,
  Warning20Regular,
  LockClosed20Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  auditEntry: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    '&:last-child': {
      borderBottom: 'none',
    },
  },
  entryHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXS,
  },
  badges: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  details: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    marginTop: tokens.spacingVerticalS,
  },
});

interface ModelAuditEntry {
  provider: string;
  stage: string;
  modelId: string;
  source: string;
  reasoning: string;
  isPinned: boolean;
  isBlocked: boolean;
  blockReason?: string;
  fallbackReason?: string;
  timestamp: string;
}

interface ModelSelectionAuditProps {
  jobId: string;
}

export const ModelSelectionAudit: React.FC<ModelSelectionAuditProps> = ({ jobId }) => {
  const styles = useStyles();
  const [auditEntries, setAuditEntries] = useState<ModelAuditEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchAudit = async () => {
      try {
        setLoading(true);
        const response = await fetch(`/api/models/audit-log/job/${jobId}`);
        
        if (!response.ok) {
          throw new Error(`Failed to fetch audit log: ${response.statusText}`);
        }

        const data = await response.json();
        setAuditEntries(data.entries || []);
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : 'Unknown error';
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchAudit();
  }, [jobId]);

  const getSourceBadgeColor = (source: string) => {
    switch (source) {
      case 'RunOverridePinned':
        return 'danger' as const;
      case 'RunOverride':
        return 'important' as const;
      case 'StagePinned':
        return 'warning' as const;
      case 'ProjectOverride':
        return 'informative' as const;
      case 'GlobalDefault':
        return 'subtle' as const;
      case 'AutomaticFallback':
        return 'success' as const;
      default:
        return 'subtle' as const;
    }
  };

  const formatSourceName = (source: string): string => {
    return source.replace(/([A-Z])/g, ' $1').trim();
  };

  if (loading) {
    return (
      <Card className={styles.card}>
        <Spinner label="Loading model selection audit..." />
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={styles.card}>
        <MessageBar intent="error">
          <MessageBarBody>Failed to load model selection audit: {error}</MessageBarBody>
        </MessageBar>
      </Card>
    );
  }

  if (auditEntries.length === 0) {
    return (
      <Card className={styles.card}>
        <Text>No model selections recorded for this job.</Text>
      </Card>
    );
  }

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>
          Model Selection Audit Trail
        </Title3>
        <Text size={300} style={{ display: 'block', marginBottom: tokens.spacingVerticalL }}>
          This shows which AI models were selected for each stage, and the reasoning behind each
          choice.
        </Text>

        {auditEntries.map((entry, index) => (
          <div key={index} className={styles.auditEntry}>
            <div className={styles.entryHeader}>
              <div>
                <Text weight="semibold">
                  {entry.provider} / {entry.stage}
                </Text>
                <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                  Model: {entry.modelId}
                </Text>
              </div>
              <div className={styles.badges}>
                <Badge color={getSourceBadgeColor(entry.source)} appearance="filled">
                  {formatSourceName(entry.source)}
                </Badge>
                {entry.isPinned && (
                  <Badge color="important" appearance="outline" icon={<LockClosed20Regular />}>
                    Pinned
                  </Badge>
                )}
                {!entry.isBlocked && (
                  <Badge color="success" appearance="outline" icon={<CheckmarkCircle20Regular />}>
                    Used
                  </Badge>
                )}
              </div>
            </div>

            <div className={styles.details}>
              <Text size={200}>
                <strong>Reasoning:</strong> {entry.reasoning}
              </Text>

              {entry.fallbackReason && (
                <MessageBar intent="info" style={{ marginTop: tokens.spacingVerticalS }}>
                  <MessageBarBody>
                    <strong>Fallback Applied:</strong> {entry.fallbackReason}
                  </MessageBarBody>
                </MessageBar>
              )}

              {entry.isBlocked && (
                <MessageBar intent="warning" style={{ marginTop: tokens.spacingVerticalS }}>
                  <MessageBarBody>
                    <strong>Blocked:</strong> {entry.blockReason}
                  </MessageBarBody>
                </MessageBar>
              )}

              <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
                Selected at: {new Date(entry.timestamp).toLocaleString()}
              </Text>
            </div>
          </div>
        ))}
      </Card>

      <Card className={styles.card}>
        <MessageBar intent="info">
          <MessageBarBody>
            <strong>Selection Precedence:</strong> Run Override (Pinned) {'>'} Run Override {'>'}{' '}
            Stage Pinned {'>'} Project Override {'>'} Global Default {'>'} Automatic Fallback
          </MessageBarBody>
        </MessageBar>
      </Card>
    </div>
  );
};
