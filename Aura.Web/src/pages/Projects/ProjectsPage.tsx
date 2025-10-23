import { useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Card,
  Text,
  Title1,
  Button,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Spinner,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
} from '@fluentui/react-components';
import {
  FolderOpen24Regular,
  Video24Regular,
  ArrowClockwise24Regular,
  MoreVertical24Regular,
  Eye24Regular,
} from '@fluentui/react-icons';
import { useJobsStore } from '../../state/jobs';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
  },
  table: {
    width: '100%',
  },
  statusBadge: {
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: '12px',
    fontWeight: 600,
  },
  statusDone: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground1,
  },
  statusRunning: {
    backgroundColor: tokens.colorPaletteBlueBackground2,
    color: tokens.colorPaletteBlueForeground2,
  },
  statusFailed: {
    backgroundColor: tokens.colorPaletteRedBackground2,
    color: tokens.colorPaletteRedForeground1,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
});

export function ProjectsPage() {
  const styles = useStyles();
  const { jobs, loading, listJobs } = useJobsStore();

  useEffect(() => {
    listJobs();
  }, [listJobs]);

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleString();
  };

  const formatDuration = (startedAt: string, finishedAt?: string) => {
    if (!finishedAt) return '--';
    const start = new Date(startedAt);
    const end = new Date(finishedAt);
    const diffMs = end.getTime() - start.getTime();
    const minutes = Math.floor(diffMs / 60000);
    const seconds = Math.floor((diffMs % 60000) / 1000);
    return `${minutes}m ${seconds}s`;
  };

  const openFolder = (artifactPath: string) => {
    // Extract directory from artifact path
    const dirPath = artifactPath.substring(0, artifactPath.lastIndexOf('/'));
    window.open(`file:///${dirPath.replace(/\\/g, '/')}`);
  };

  const revealInExplorer = (artifactPath: string) => {
    // For web apps, we can only open the folder
    // In a desktop app, this would call the OS's "reveal in explorer" API
    const dirPath = artifactPath.substring(0, artifactPath.lastIndexOf('/'));
    window.open(`file:///${dirPath.replace(/\\/g, '/')}`);
  };

  const openArtifact = (artifactPath: string) => {
    window.open(`file:///${artifactPath.replace(/\\/g, '/')}`);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Projects</Title1>
        <Button
          appearance="secondary"
          icon={<ArrowClockwise24Regular />}
          onClick={() => listJobs()}
          disabled={loading}
        >
          Refresh
        </Button>
      </div>

      {loading && (
        <div
          style={{ display: 'flex', justifyContent: 'center', padding: tokens.spacingVerticalXXL }}
        >
          <Spinner label="Loading projects..." />
        </div>
      )}

      {!loading && jobs.length === 0 && (
        <Card>
          <div className={styles.emptyState}>
            <Video24Regular style={{ fontSize: '64px' }} />
            <Title1>No projects yet</Title1>
            <Text>Create your first video to see it here</Text>
          </div>
        </Card>
      )}

      {!loading && jobs.length > 0 && (
        <Card>
          <Table className={styles.table}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Date</TableHeaderCell>
                <TableHeaderCell>Topic</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell>Stage</TableHeaderCell>
                <TableHeaderCell>Duration</TableHeaderCell>
                <TableHeaderCell>Actions</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {jobs.map((job) => (
                <TableRow key={job.id}>
                  <TableCell>{formatDate(job.startedAt)}</TableCell>
                  <TableCell>
                    <Text weight="semibold">{job.correlationId?.substring(0, 8) || 'Unknown'}</Text>
                  </TableCell>
                  <TableCell>
                    <span
                      className={`${styles.statusBadge} ${
                        job.status === 'Done'
                          ? styles.statusDone
                          : job.status === 'Running'
                            ? styles.statusRunning
                            : styles.statusFailed
                      }`}
                    >
                      {job.status}
                    </span>
                  </TableCell>
                  <TableCell>{job.stage}</TableCell>
                  <TableCell>{formatDuration(job.startedAt, job.finishedAt)}</TableCell>
                  <TableCell>
                    <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                      {job.artifacts.length > 0 && (
                        <>
                          <Button
                            size="small"
                            appearance="primary"
                            icon={<Eye24Regular />}
                            onClick={() => {
                              const firstArtifact = job.artifacts[0];
                              if (firstArtifact) {
                                openArtifact(firstArtifact.path);
                              }
                            }}
                          >
                            Open
                          </Button>
                          <Menu>
                            <MenuTrigger>
                              <Button
                                size="small"
                                appearance="subtle"
                                icon={<MoreVertical24Regular />}
                              />
                            </MenuTrigger>
                            <MenuPopover>
                              <MenuList>
                                <MenuItem
                                  icon={<FolderOpen24Regular />}
                                  onClick={() => {
                                    const firstArtifact = job.artifacts[0];
                                    if (firstArtifact) {
                                      openFolder(firstArtifact.path);
                                    }
                                  }}
                                >
                                  Open outputs folder
                                </MenuItem>
                                <MenuItem
                                  icon={<FolderOpen24Regular />}
                                  onClick={() => {
                                    const firstArtifact = job.artifacts[0];
                                    if (firstArtifact) {
                                      revealInExplorer(firstArtifact.path);
                                    }
                                  }}
                                >
                                  Reveal in Explorer
                                </MenuItem>
                              </MenuList>
                            </MenuPopover>
                          </Menu>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  );
}
