import { useEffect, useState } from 'react';
import {
  makeStyles,
  tokens,
  Card,
  Text,
  Title1,
  Title3,
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
  TabList,
  Tab,
} from '@fluentui/react-components';
import {
  FolderOpen24Regular,
  Video24Regular,
  ArrowClockwise24Regular,
  MoreVertical24Regular,
  Eye24Regular,
  Edit24Regular,
  Delete24Regular,
  DocumentCopy24Regular,
} from '@fluentui/react-icons';
import { useJobsStore } from '../../state/jobs';
import { useNavigate } from 'react-router-dom';
import { getProjects, deleteProject, duplicateProject } from '../../services/projectService';
import { ProjectListItem } from '../../types/project';

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
  const navigate = useNavigate();
  const [selectedTab, setSelectedTab] = useState<'editor' | 'generated'>('editor');
  const [editorProjects, setEditorProjects] = useState<ProjectListItem[]>([]);
  const [loadingProjects, setLoadingProjects] = useState(false);

  useEffect(() => {
    listJobs();
    loadEditorProjects();
  }, [listJobs]);

  const loadEditorProjects = async () => {
    setLoadingProjects(true);
    try {
      const projects = await getProjects();
      setEditorProjects(projects);
    } catch (error) {
      console.error('Failed to load editor projects:', error);
    } finally {
      setLoadingProjects(false);
    }
  };

  const handleOpenProject = (projectId: string) => {
    navigate(`/editor?projectId=${projectId}`);
  };

  const handleDeleteProject = async (projectId: string) => {
    if (window.confirm('Are you sure you want to delete this project?')) {
      try {
        await deleteProject(projectId);
        await loadEditorProjects();
      } catch (error) {
        console.error('Failed to delete project:', error);
      }
    }
  };

  const handleDuplicateProject = async (projectId: string) => {
    try {
      await duplicateProject(projectId);
      await loadEditorProjects();
    } catch (error) {
      console.error('Failed to duplicate project:', error);
    }
  };

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
          onClick={() => {
            listJobs();
            loadEditorProjects();
          }}
          disabled={loading || loadingProjects}
        >
          Refresh
        </Button>
      </div>

      {/* Tabs */}
      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as 'editor' | 'generated')}
        style={{ marginBottom: tokens.spacingVerticalL }}
      >
        <Tab value="editor">Editor Projects</Tab>
        <Tab value="generated">Generated Videos</Tab>
      </TabList>

      {/* Editor Projects Tab */}
      {selectedTab === 'editor' && (
        <>
          {loadingProjects && (
            <div
              style={{ display: 'flex', justifyContent: 'center', padding: tokens.spacingVerticalXXL }}
            >
              <Spinner label="Loading projects..." />
            </div>
          )}

          {!loadingProjects && editorProjects.length === 0 && (
            <Card>
              <div className={styles.emptyState}>
                <Edit24Regular style={{ fontSize: '64px' }} />
                <Title3>No editor projects yet</Title3>
                <Text>Create a project in the video editor to see it here</Text>
                <Button appearance="primary" onClick={() => navigate('/editor')}>
                  Open Video Editor
                </Button>
              </div>
            </Card>
          )}

          {!loadingProjects && editorProjects.length > 0 && (
            <Card>
              <Table className={styles.table}>
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>Name</TableHeaderCell>
                    <TableHeaderCell>Last Modified</TableHeaderCell>
                    <TableHeaderCell>Duration</TableHeaderCell>
                    <TableHeaderCell>Clips</TableHeaderCell>
                    <TableHeaderCell>Actions</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {editorProjects.map((project) => (
                    <TableRow 
                      key={project.id}
                      style={{ cursor: 'pointer' }}
                      onClick={() => handleOpenProject(project.id)}
                    >
                      <TableCell>
                        <Text weight="semibold">{project.name}</Text>
                        {project.description && (
                          <div>
                            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                              {project.description}
                            </Text>
                          </div>
                        )}
                      </TableCell>
                      <TableCell>{new Date(project.lastModifiedAt).toLocaleString()}</TableCell>
                      <TableCell>{formatProjectDuration(project.duration)}</TableCell>
                      <TableCell>{project.clipCount}</TableCell>
                      <TableCell>
                        <div 
                          style={{ display: 'flex', gap: tokens.spacingHorizontalS }}
                          onClick={(e) => e.stopPropagation()}
                        >
                          <Button
                            size="small"
                            appearance="primary"
                            icon={<Edit24Regular />}
                            onClick={() => handleOpenProject(project.id)}
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
                                  icon={<DocumentCopy24Regular />}
                                  onClick={() => handleDuplicateProject(project.id)}
                                >
                                  Duplicate
                                </MenuItem>
                                <MenuItem
                                  icon={<Delete24Regular />}
                                  onClick={() => handleDeleteProject(project.id)}
                                >
                                  Delete
                                </MenuItem>
                              </MenuList>
                            </MenuPopover>
                          </Menu>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Card>
          )}
        </>
      )}

      {/* Generated Videos Tab */}
      {selectedTab === 'generated' && (
        <>
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
                <Title3>No generated videos yet</Title3>
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
        </>
      )}
    </div>
  );
}

// Helper function to format project duration
function formatProjectDuration(seconds: number): string {
  if (seconds === 0) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}
