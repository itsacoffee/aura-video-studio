/**
 * Pacing Optimization Panel Component
 * Main UI component for visual selection and pacing optimization
 */

import {
  Card,
  Button,
  makeStyles,
  tokens,
  Spinner,
  Tab,
  TabList,
  SelectTabData,
  SelectTabEvent,
  Title3,
  Body1,
  Badge,
} from '@fluentui/react-components';
import {
  VideoClip24Regular,
  FlashFlow24Regular,
  ChartMultiple24Regular,
  Beaker24Regular as TestBeaker24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { Scene } from '../../types';
import { FrameSelectionView } from './FrameSelectionView';
import { OptimizationResultsView } from './OptimizationResultsView';
import { PaceAdjustmentSlider } from './PaceAdjustmentSlider';
import { TransitionSuggestionCard } from './TransitionSuggestionCard';

interface PacingOptimizationPanelProps {
  scenes: Scene[];
  videoPath?: string;
  onScenesUpdated?: (scenes: Scene[]) => void;
}

const useStyles = makeStyles({
  container: {
    width: '100%',
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalL,
  },
  tabContent: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

export const PacingOptimizationPanel = ({
  scenes,
  videoPath,
  onScenesUpdated,
}: PacingOptimizationPanelProps) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string>('frames');
  const [loading, setLoading] = useState(false);
  const [optimizationActive, setOptimizationActive] = useState(false);

  const handleOptimize = useCallback(async () => {
    setLoading(true);
    setOptimizationActive(true);

    try {
      // Trigger optimization process
      await new Promise((resolve) => setTimeout(resolve, 1500)); // Simulated delay

      // In production, this would call the optimization services
    } catch (error) {
      console.error('Optimization failed:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleTabChange = (_: SelectTabEvent, data: SelectTabData) => {
    setSelectedTab(data.value as string);
  };

  if (scenes.length === 0) {
    return (
      <Card className={styles.container}>
        <div className={styles.emptyState}>
          <VideoClip24Regular />
          <Body1>No scenes available. Add scenes to begin pacing optimization.</Body1>
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          <FlashFlow24Regular style={{ fontSize: '24px' }} />
          <Title3>Pacing Optimization</Title3>
          {optimizationActive && (
            <Badge appearance="tint" color="success">
              Active
            </Badge>
          )}
        </div>
        <Button
          appearance="primary"
          icon={loading ? <Spinner size="tiny" /> : <ChartMultiple24Regular />}
          onClick={handleOptimize}
          disabled={loading}
        >
          {loading ? 'Analyzing...' : 'Optimize Pacing'}
        </Button>
      </div>

      <TabList selectedValue={selectedTab} onTabSelect={handleTabChange}>
        <Tab value="frames" icon={<VideoClip24Regular />}>
          Frame Selection
        </Tab>
        <Tab value="pacing" icon={<FlashFlow24Regular />}>
          Pace Adjustment
        </Tab>
        <Tab value="transitions" icon={<ChartMultiple24Regular />}>
          Transitions
        </Tab>
        <Tab value="results" icon={<TestBeaker24Regular />}>
          Results
        </Tab>
      </TabList>

      <div className={styles.tabContent}>
        {selectedTab === 'frames' && (
          <FrameSelectionView
            scenes={scenes}
            videoPath={videoPath}
            optimizationActive={optimizationActive}
          />
        )}

        {selectedTab === 'pacing' && (
          <PaceAdjustmentSlider
            scenes={scenes}
            onScenesUpdated={onScenesUpdated}
            optimizationActive={optimizationActive}
          />
        )}

        {selectedTab === 'transitions' && (
          <TransitionSuggestionCard scenes={scenes} optimizationActive={optimizationActive} />
        )}

        {selectedTab === 'results' && (
          <OptimizationResultsView scenes={scenes} optimizationActive={optimizationActive} />
        )}
      </div>
    </Card>
  );
};
