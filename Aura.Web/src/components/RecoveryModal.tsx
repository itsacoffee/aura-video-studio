import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import { ArrowClockwise24Regular, Delete24Regular, Info24Regular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import type { RecoverableProject, ProjectDetails } from '../hooks/useProjectRecovery';

interface RecoveryModalProps {
  projects: RecoverableProject[];
  onResume: (projectId: string) => void;
  onDiscard: (projectId: string) => void;
  onViewDetails: (projectId: string) => Promise<ProjectDetails | null>;
}

export const RecoveryModal: React.FC<RecoveryModalProps> = ({
  projects,
  onResume,
  onDiscard,
  onViewDetails,
}) => {
  const [open, setOpen] = useState(projects.length > 0);
  const [selectedProject, setSelectedProject] = useState<string | null>(null);
  const [projectDetails, setProjectDetails] = useState<ProjectDetails | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [viewingDetails, setViewingDetails] = useState(false);

  const handleResume = (projectId: string) => {
    onResume(projectId);
    setOpen(false);
  };

  const handleDiscard = async (projectId: string) => {
    try {
      await onDiscard(projectId);
    } catch (error) {
      console.error('Failed to discard project:', error);
    }
  };

  const handleViewDetails = async (projectId: string) => {
    setLoadingDetails(true);
    setSelectedProject(projectId);

    try {
      const details = await onViewDetails(projectId);
      setProjectDetails(details);
      setViewingDetails(true);
    } catch (error) {
      console.error('Failed to load project details:', error);
    } finally {
      setLoadingDetails(false);
    }
  };

  const formatTimeAgo = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
  };

  if (projects.length === 0) {
    return null;
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Resume Unfinished Projects</DialogTitle>
          <DialogContent>
            {!viewingDetails ? (
              <>
                <Text>
                  We found {projects.length} unfinished project{projects.length > 1 ? 's' : ''}.
                  Would you like to resume where you left off?
                </Text>

                <div
                  style={{
                    marginTop: '16px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '12px',
                  }}
                >
                  {projects.map((project) => (
                    <div
                      key={project.projectId}
                      style={{
                        padding: '12px',
                        border: '1px solid var(--colorNeutralStroke2)',
                        borderRadius: '4px',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '8px',
                      }}
                    >
                      <div
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                        }}
                      >
                        <Text weight="semibold">{project.title}</Text>
                        {project.canRecover ? (
                          <Badge appearance="tint" color="success">
                            Recoverable
                          </Badge>
                        ) : (
                          <Badge appearance="tint" color="danger">
                            Files Missing
                          </Badge>
                        )}
                      </div>

                      <div
                        style={{
                          display: 'flex',
                          gap: '16px',
                          fontSize: '12px',
                          color: 'var(--colorNeutralForeground3)',
                        }}
                      >
                        <Text size={200}>Stage: {project.currentStage || 'Unknown'}</Text>
                        <Text size={200}>Progress: {project.progressPercent}%</Text>
                        <Text size={200}>Last saved: {formatTimeAgo(project.updatedAt)}</Text>
                      </div>

                      <div style={{ display: 'flex', gap: '8px', marginTop: '4px' }}>
                        <Button
                          size="small"
                          appearance="primary"
                          icon={<ArrowClockwise24Regular />}
                          onClick={() => handleResume(project.projectId)}
                          disabled={!project.canRecover}
                        >
                          Resume
                        </Button>
                        <Button
                          size="small"
                          appearance="subtle"
                          icon={<Info24Regular />}
                          onClick={() => handleViewDetails(project.projectId)}
                          disabled={loadingDetails && selectedProject === project.projectId}
                        >
                          {loadingDetails && selectedProject === project.projectId ? (
                            <Spinner size="tiny" />
                          ) : (
                            'View Details'
                          )}
                        </Button>
                        <Button
                          size="small"
                          appearance="subtle"
                          icon={<Delete24Regular />}
                          onClick={() => handleDiscard(project.projectId)}
                        >
                          Discard
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              </>
            ) : projectDetails ? (
              <>
                <Button
                  size="small"
                  appearance="subtle"
                  onClick={() => setViewingDetails(false)}
                  style={{ marginBottom: '12px' }}
                >
                  ← Back to projects
                </Button>

                <Text weight="semibold" block style={{ marginBottom: '8px' }}>
                  {projectDetails.title}
                </Text>

                <div
                  style={{ display: 'flex', flexDirection: 'column', gap: '8px', fontSize: '14px' }}
                >
                  <div>
                    <Text weight="semibold">Current Stage: </Text>
                    <Text>{projectDetails.currentStage || 'Unknown'}</Text>
                  </div>

                  <div>
                    <Text weight="semibold">Progress: </Text>
                    <Text>{projectDetails.progressPercent}%</Text>
                  </div>

                  {projectDetails.latestCheckpoint && (
                    <>
                      <div>
                        <Text weight="semibold">Last Checkpoint: </Text>
                        <Text>{projectDetails.latestCheckpoint.stageName}</Text>
                      </div>

                      <div>
                        <Text weight="semibold">Completed Scenes: </Text>
                        <Text>
                          {projectDetails.latestCheckpoint.completedScenes} of{' '}
                          {projectDetails.latestCheckpoint.totalScenes}
                        </Text>
                      </div>
                    </>
                  )}

                  {!projectDetails.filesExist && (
                    <div>
                      <Text
                        weight="semibold"
                        style={{ color: 'var(--colorPaletteRedForeground1)' }}
                      >
                        Warning: {projectDetails.missingFilesCount} file(s) missing
                      </Text>
                      <div style={{ marginTop: '4px', paddingLeft: '12px' }}>
                        {projectDetails.missingFiles.slice(0, 3).map((file, idx) => (
                          <Text
                            key={idx}
                            size={200}
                            block
                            style={{ color: 'var(--colorNeutralForeground3)' }}
                          >
                            {file}
                          </Text>
                        ))}
                        {projectDetails.missingFiles.length > 3 && (
                          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                            ... and {projectDetails.missingFiles.length - 3} more
                          </Text>
                        )}
                      </div>
                    </div>
                  )}
                </div>

                <div style={{ marginTop: '16px', display: 'flex', gap: '8px' }}>
                  <Button
                    appearance="primary"
                    icon={<ArrowClockwise24Regular />}
                    onClick={() => handleResume(projectDetails.projectId)}
                    disabled={!projectDetails.canRecover}
                  >
                    Resume Project
                  </Button>
                  <Button
                    appearance="subtle"
                    icon={<Delete24Regular />}
                    onClick={() => handleDiscard(projectDetails.projectId)}
                  >
                    Discard Project
                  </Button>
                </div>
              </>
            ) : null}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setOpen(false)}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
