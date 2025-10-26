import {
  makeStyles,
  tokens,
  Text,
  Body1Strong,
  Caption1,
  Button,
  Card,
  CardHeader,
} from '@fluentui/react-components';
import { Video24Regular, ArrowExport24Regular, Settings24Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { ExportPreviewCard } from './ExportPreviewCard';
import { ExportQueueManager } from './ExportQueueManager';
import { ExportSettingsEditor } from './ExportSettingsEditor';
import { PlatformSelectionGrid } from './PlatformSelectionGrid';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
    padding: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  mainContent: {
    display: 'grid',
    gridTemplateColumns: '2fr 1fr',
    gap: tokens.spacingHorizontalXL,
    '@media (max-width: 1024px)': {
      gridTemplateColumns: '1fr',
    },
  },
  leftPanel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  rightPanel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
});

export interface Platform {
  id: string;
  name: string;
  icon: string;
  color: string;
}

export interface ExportSettings {
  platforms: string[];
  resolution: { width: number; height: number };
  quality: 'draft' | 'good' | 'high' | 'maximum';
  format: string;
  optimizeForPlatform: boolean;
}

export interface MultiPlatformExportPanelProps {
  videoPath?: string;
  onExport: (settings: ExportSettings) => void;
  onExportToQueue: (settings: ExportSettings) => void;
}

export function MultiPlatformExportPanel({
  videoPath,
  onExport,
  onExportToQueue,
}: MultiPlatformExportPanelProps) {
  const styles = useStyles();
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>([]);
  const [showSettings, setShowSettings] = useState(false);
  const [exportSettings, setExportSettings] = useState<ExportSettings>({
    platforms: [],
    resolution: { width: 1920, height: 1080 },
    quality: 'high',
    format: 'mp4',
    optimizeForPlatform: true,
  });

  const handlePlatformToggle = useCallback((platformId: string) => {
    setSelectedPlatforms((prev) => {
      if (prev.includes(platformId)) {
        return prev.filter((p) => p !== platformId);
      } else {
        return [...prev, platformId];
      }
    });
  }, []);

  const handleExport = useCallback(() => {
    const settings = {
      ...exportSettings,
      platforms: selectedPlatforms,
    };
    onExport(settings);
  }, [exportSettings, selectedPlatforms, onExport]);

  const handleExportToQueue = useCallback(() => {
    const settings = {
      ...exportSettings,
      platforms: selectedPlatforms,
    };
    onExportToQueue(settings);
  }, [exportSettings, selectedPlatforms, onExportToQueue]);

  const handleSettingsChange = useCallback((settings: Partial<ExportSettings>) => {
    setExportSettings((prev) => ({ ...prev, ...settings }));
  }, []);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Body1Strong>Multi-Platform Export</Body1Strong>
          <Caption1>
            Optimize your video for multiple social media platforms simultaneously
          </Caption1>
        </div>
        <Button
          icon={<Settings24Regular />}
          appearance="subtle"
          onClick={() => setShowSettings(!showSettings)}
        >
          {showSettings ? 'Hide Settings' : 'Advanced Settings'}
        </Button>
      </div>

      <div className={styles.mainContent}>
        <div className={styles.leftPanel}>
          <div className={styles.section}>
            <Text className={styles.sectionTitle}>
              <Body1Strong>Select Target Platforms</Body1Strong>
            </Text>
            <PlatformSelectionGrid
              selectedPlatforms={selectedPlatforms}
              onPlatformToggle={handlePlatformToggle}
            />
          </div>

          {selectedPlatforms.length > 0 && (
            <div className={styles.section}>
              <Text className={styles.sectionTitle}>
                <Body1Strong>Export Previews</Body1Strong>
              </Text>
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))',
                  gap: tokens.spacingHorizontalM,
                }}
              >
                {selectedPlatforms.map((platformId) => (
                  <ExportPreviewCard
                    key={platformId}
                    platformId={platformId}
                    settings={exportSettings}
                    videoPath={videoPath}
                  />
                ))}
              </div>
            </div>
          )}

          {showSettings && (
            <div className={styles.section}>
              <ExportSettingsEditor settings={exportSettings} onChange={handleSettingsChange} />
            </div>
          )}
        </div>

        <div className={styles.rightPanel}>
          <Card>
            <CardHeader
              header={<Body1Strong>Export Queue</Body1Strong>}
              description={<Caption1>Manage your export jobs</Caption1>}
            />
            <ExportQueueManager />
          </Card>

          {selectedPlatforms.length > 0 && (
            <Card>
              <div style={{ padding: tokens.spacingVerticalM }}>
                <Caption1 style={{ marginBottom: tokens.spacingVerticalM }}>
                  Ready to export to {selectedPlatforms.length} platform
                  {selectedPlatforms.length > 1 ? 's' : ''}
                </Caption1>
                <div className={styles.actions}>
                  <Button
                    appearance="secondary"
                    icon={<ArrowExport24Regular />}
                    onClick={handleExportToQueue}
                    disabled={!videoPath}
                  >
                    Add to Queue
                  </Button>
                  <Button
                    appearance="primary"
                    icon={<Video24Regular />}
                    onClick={handleExport}
                    disabled={!videoPath}
                  >
                    Export Now
                  </Button>
                </div>
              </div>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
