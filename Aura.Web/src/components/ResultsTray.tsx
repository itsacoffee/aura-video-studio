import {
  makeStyles,
  tokens,
  Button,
  Popover,
  PopoverTrigger,
  PopoverSurface,
  Text,
  Title3,
  Divider,
  Badge,
} from '@fluentui/react-components';
import { Video24Regular, FolderOpen24Regular, Eye24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../config/api';

const useStyles = makeStyles({
  trigger: {
    position: 'relative',
  },
  badge: {
    position: 'absolute',
    top: '-4px',
    right: '-4px',
  },
  surface: {
    width: '400px',
    maxHeight: '500px',
    display: 'flex',
    flexDirection: 'column',
  },
  header: {
    padding: tokens.spacingVerticalM,
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    overflowY: 'auto',
  },
  item: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
  },
  itemInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    flex: 1,
  },
  itemActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  emptyState: {
    padding: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
});

interface RecentArtifact {
  jobId: string;
  correlationId?: string;
  stage: string;
  finishedAt?: string;
  artifacts: Array<{
    name: string;
    path: string;
    type: string;
    sizeBytes: number;
  }>;
}

export function ResultsTray() {
  const styles = useStyles();
  const [artifacts, setArtifacts] = useState<RecentArtifact[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchRecentArtifacts = async () => {
    setLoading(true);
    try {
      const response = await fetch(apiUrl('/api/jobs/recent-artifacts?limit=5'));
      if (response.ok) {
        const data = await response.json();
        setArtifacts(data.artifacts || []);
      }
    } catch (error) {
      console.error('Error fetching recent artifacts:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecentArtifacts();

    // Refresh every 30 seconds
    const interval = setInterval(fetchRecentArtifacts, 30000);
    return () => clearInterval(interval);
  }, []);

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'Recently';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays}d ago`;
  };

  const openFolder = (artifactPath: string) => {
    const dirPath = artifactPath.substring(0, artifactPath.lastIndexOf('/'));
    window.open(`file:///${dirPath.replace(/\\/g, '/')}`);
  };

  const openArtifact = (artifactPath: string) => {
    window.open(`file:///${artifactPath.replace(/\\/g, '/')}`);
  };

  return (
    <Popover positioning="below-end">
      <PopoverTrigger>
        <div className={styles.trigger}>
          <Button appearance="subtle" icon={<Video24Regular />} aria-label="Recent results">
            Results
          </Button>
          {artifacts.length > 0 && (
            <Badge className={styles.badge} size="small" appearance="filled" color="informative">
              {artifacts.length}
            </Badge>
          )}
        </div>
      </PopoverTrigger>
      <PopoverSurface className={styles.surface}>
        <div className={styles.header}>
          <Title3>Recent Results</Title3>
        </div>
        <Divider />
        {loading ? (
          <div className={styles.emptyState}>
            <Text>Loading...</Text>
          </div>
        ) : artifacts.length === 0 ? (
          <div className={styles.emptyState}>
            <Video24Regular style={{ fontSize: '48px' }} />
            <Text>No recent outputs</Text>
            <Text size={200}>Complete a video generation to see results here</Text>
          </div>
        ) : (
          <div className={styles.list}>
            {artifacts.map((artifact) => (
              <div key={artifact.jobId} className={styles.item}>
                <div className={styles.itemInfo}>
                  <Text weight="semibold">
                    {artifact.correlationId?.substring(0, 12) || 'Video'}
                  </Text>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    {formatDate(artifact.finishedAt)} •{' '}
                    {artifact.artifacts[0]?.name || 'output.mp4'}
                  </Text>
                </div>
                <div className={styles.itemActions}>
                  <Button
                    size="small"
                    appearance="subtle"
                    icon={<Eye24Regular />}
                    onClick={() => {
                      if (artifact.artifacts[0]) {
                        openArtifact(artifact.artifacts[0].path);
                      }
                    }}
                  />
                  <Button
                    size="small"
                    appearance="subtle"
                    icon={<FolderOpen24Regular />}
                    onClick={() => {
                      if (artifact.artifacts[0]) {
                        openFolder(artifact.artifacts[0].path);
                      }
                    }}
                  />
                </div>
              </div>
            ))}
          </div>
        )}
      </PopoverSurface>
    </Popover>
  );
}
