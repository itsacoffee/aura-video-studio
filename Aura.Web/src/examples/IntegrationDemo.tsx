/**
 * Integration Demo Component
 * Demonstrates full frontend-backend integration with video generation and project management
 */

import {
  makeStyles,
  tokens,
  Button,
  Card,
  Text,
  Title2,
  Title3,
  ProgressBar,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import { Play24Regular, Save24Regular, Delete24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { useProjects } from '../hooks/useProjects';
import { useVideoGeneration } from '../hooks/useVideoGeneration';
import { useAppStore } from '../stores/appStore';
import { useProjectsStore } from '../stores/projectsStore';
import { useVideoGenerationStore } from '../stores/videoGenerationStore';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  card: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  status: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  jobItem: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
});

export function IntegrationDemo() {
  const styles = useStyles();
  
  // Hooks for API operations
  const videoGeneration = useVideoGeneration({
    onComplete: (status) => {
      appStore.addNotification({
        type: 'success',
        title: 'Video Generation Complete',
        message: `Video generated successfully: ${status.id}`,
        duration: 5000,
      });
    },
    onError: (error) => {
      appStore.addNotification({
        type: 'error',
        title: 'Video Generation Failed',
        message: error.message,
        duration: 8000,
      });
    },
    onProgress: (progress, message) => {
      console.log(`Progress: ${progress}% - ${message}`);
    },
  });

  const projects = useProjects({
    filters: { status: 'draft' },
    autoRefetch: true,
    refetchInterval: 30000,
  });

  // Zustand stores
  const videoStore = useVideoGenerationStore();
  const projectsStore = useProjectsStore();
  const appStore = useAppStore();

  const [demoTopic, setDemoTopic] = useState('Introduction to AI and Machine Learning');

  // Load projects on mount
  useEffect(() => {
    projects.refetch();
  }, []);

  // Handle video generation
  const handleGenerateVideo = async () => {
    try {
      await videoGeneration.generate({
        brief: {
          topic: demoTopic,
          audience: 'General',
          goal: 'Inform',
          tone: 'Informative',
          language: 'en-US',
          aspect: 'Widescreen16x9',
        },
        planSpec: {
          targetDuration: '00:03:00',
          pacing: 'Conversational',
          density: 'Balanced',
          style: 'Standard',
        },
        voiceSpec: {
          voiceName: 'en-US-Standard-A',
          rate: 1.0,
          pitch: 0.0,
          pause: 'Medium',
        },
        renderSpec: {
          res: '1920x1080',
          container: 'mp4',
          videoBitrateK: 5000,
          audioBitrateK: 192,
          fps: 30,
          codec: 'H264',
          qualityLevel: 'High',
          enableSceneCut: true,
        },
      });
    } catch (error) {
      console.error('Failed to generate video:', error);
    }
  };

  // Handle project creation
  const handleCreateProject = async () => {
    try {
      const newProject = await projects.createProject({
        name: `Project: ${demoTopic}`,
        description: 'Demo project created from integration example',
        brief: {
          topic: demoTopic,
          audience: 'General',
          goal: 'Inform',
          tone: 'Informative',
          language: 'en-US',
          aspect: 'Widescreen16x9',
        },
        planSpec: {
          targetDuration: '00:03:00',
          pacing: 'Conversational',
          density: 'Balanced',
          style: 'Standard',
        },
        tags: ['demo', 'ai'],
      });

      appStore.addNotification({
        type: 'success',
        title: 'Project Created',
        message: `Project "${newProject.name}" created successfully`,
        duration: 3000,
      });
    } catch (error) {
      console.error('Failed to create project:', error);
    }
  };

  return (
    <div className={styles.container}>
      <Title2>Frontend-Backend Integration Demo</Title2>

      {/* Video Generation Section */}
      <div className={styles.section}>
        <Card className={styles.card}>
          <Title3>Video Generation</Title3>
          <Text>Generate a video using the full backend pipeline</Text>

          <div className={styles.status}>
            <Badge
              appearance="filled"
              color={videoGeneration.isGenerating ? 'warning' : 'success'}
            >
              {videoGeneration.isGenerating ? 'Generating' : 'Ready'}
            </Badge>
            {videoGeneration.isGenerating && (
              <>
                <Text>{videoGeneration.progress}%</Text>
                <Spinner size="tiny" />
              </>
            )}
          </div>

          {videoGeneration.isGenerating && (
            <ProgressBar
              value={videoGeneration.progress / 100}
              thickness="large"
              style={{ marginTop: tokens.spacingVerticalM }}
            />
          )}

          {videoGeneration.error && (
            <Text style={{ color: tokens.colorPaletteRedForeground1, marginTop: tokens.spacingVerticalM }}>
              Error: {videoGeneration.error.message}
            </Text>
          )}

          {videoGeneration.status && (
            <div style={{ marginTop: tokens.spacingVerticalM }}>
              <Text weight="semibold">Job ID: </Text>
              <Text>{videoGeneration.status.id}</Text>
              <br />
              <Text weight="semibold">Status: </Text>
              <Text>{videoGeneration.status.status}</Text>
              <br />
              {videoGeneration.status.outputPath && (
                <>
                  <Text weight="semibold">Output: </Text>
                  <Text>{videoGeneration.status.outputPath}</Text>
                </>
              )}
            </div>
          )}

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={<Play24Regular />}
              onClick={handleGenerateVideo}
              disabled={videoGeneration.isGenerating}
            >
              Generate Video
            </Button>
            {videoGeneration.isGenerating && (
              <Button
                onClick={videoGeneration.cancel}
                disabled={!videoGeneration.isGenerating}
              >
                Cancel
              </Button>
            )}
            {videoGeneration.error && (
              <Button onClick={videoGeneration.retry}>
                Retry
              </Button>
            )}
          </div>
        </Card>
      </div>

      {/* Projects Management Section */}
      <div className={styles.section}>
        <Card className={styles.card}>
          <Title3>Project Management</Title3>
          <Text>Create and manage video projects</Text>

          <div className={styles.status}>
            <Text>Total Projects: {projects.total}</Text>
            {projects.isLoading && <Spinner size="tiny" />}
          </div>

          {projects.error && (
            <Text style={{ color: tokens.colorPaletteRedForeground1, marginTop: tokens.spacingVerticalM }}>
              Error: {projects.error.message}
            </Text>
          )}

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={<Save24Regular />}
              onClick={handleCreateProject}
              disabled={projects.isLoading}
            >
              Create Project
            </Button>
            <Button onClick={projects.refetch} disabled={projects.isLoading}>
              Refresh
            </Button>
          </div>

          {projects.projects.length > 0 && (
            <div style={{ marginTop: tokens.spacingVerticalL }}>
              <Text weight="semibold">Recent Projects:</Text>
              {projects.projects.slice(0, 5).map((project) => (
                <div key={project.id} className={styles.jobItem}>
                  <Text weight="semibold">{project.name}</Text>
                  <br />
                  <Text size={200}>{project.brief.topic}</Text>
                  <br />
                  <Badge appearance="tint" color="informative">
                    {project.status}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      {/* Store State Section */}
      <div className={styles.section}>
        <div className={styles.grid}>
          <Card className={styles.card}>
            <Title3>Video Generation Store</Title3>
            <Text>Active Jobs: {videoStore.activeJobs.size}</Text>
            <br />
            <Text>History: {videoStore.jobHistory.length}</Text>
            <br />
            <Text>Auto-save: {videoStore.autoSaveProjects ? 'On' : 'Off'}</Text>
          </Card>

          <Card className={styles.card}>
            <Title3>Projects Store</Title3>
            <Text>Cached: {projectsStore.projects.size}</Text>
            <br />
            <Text>Recent: {projectsStore.recentProjects.length}</Text>
            <br />
            <Text>Selected: {projectsStore.selectedProjectId || 'None'}</Text>
          </Card>

          <Card className={styles.card}>
            <Title3>App Store</Title3>
            <Text>Online: {appStore.isOnline ? 'Yes' : 'No'}</Text>
            <br />
            <Text>Notifications: {appStore.notifications.length}</Text>
            <br />
            <Text>Theme: {appStore.settings.theme}</Text>
          </Card>
        </div>
      </div>

      {/* Notifications */}
      {appStore.notifications.length > 0 && (
        <div className={styles.section}>
          <Card className={styles.card}>
            <Title3>Recent Notifications</Title3>
            {appStore.notifications.map((notification) => (
              <div key={notification.id} style={{ marginTop: tokens.spacingVerticalS }}>
                <Badge
                  appearance="filled"
                  color={
                    notification.type === 'success' ? 'success' :
                    notification.type === 'error' ? 'danger' :
                    notification.type === 'warning' ? 'warning' : 'informative'
                  }
                >
                  {notification.type}
                </Badge>
                <Text weight="semibold"> {notification.title}</Text>
                <Text> - {notification.message}</Text>
              </div>
            ))}
            <Button
              onClick={appStore.clearNotifications}
              style={{ marginTop: tokens.spacingVerticalM }}
            >
              Clear All
            </Button>
          </Card>
        </div>
      )}
    </div>
  );
}
