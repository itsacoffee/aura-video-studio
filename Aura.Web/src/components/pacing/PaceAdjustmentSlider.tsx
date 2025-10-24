/**
 * Pace Adjustment Slider Component
 * Interactive timeline adjustment for pacing optimization
 */

import { useState, useEffect } from 'react';
import {
  Card,
  makeStyles,
  tokens,
  Slider,
  Body1,
  Body1Strong,
  Caption1,
  Button,
  Badge,
  Divider,
} from '@fluentui/react-components';
import {
  ChevronRight24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { Scene } from '../../types';

interface PaceAdjustmentSliderProps {
  scenes: Scene[];
  onScenesUpdated?: (scenes: Scene[]) => void;
  optimizationActive: boolean;
}

interface ScenePacing {
  sceneIndex: number;
  currentDuration: number;
  recommendedDuration: number;
  paceMultiplier: number;
  status: 'optimal' | 'too-fast' | 'too-slow';
}

const useStyles = makeStyles({
  container: {
    width: '100%',
  },
  sceneList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  sceneCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  sceneHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  sceneInfo: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  sliderContainer: {
    marginTop: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

export const PaceAdjustmentSlider = ({
  scenes,
  onScenesUpdated,
  optimizationActive,
}: PaceAdjustmentSliderProps) => {
  const styles = useStyles();
  const [scenePacing, setScenePacing] = useState<ScenePacing[]>([]);

  useEffect(() => {
    if (optimizationActive && scenes.length > 0) {
      initializeScenePacing();
    }
  }, [optimizationActive, scenes]);

  const initializeScenePacing = () => {
    const pacing: ScenePacing[] = scenes.map((scene, index) => {
      const currentDuration = scene.duration;
      const wordCount = scene.script.split(/\s+/).length;
      const wordsPerSecond = wordCount / currentDuration;
      
      // Calculate recommended duration (target: 2.5 words/second)
      const recommendedDuration = wordCount / 2.5;
      const paceMultiplier = currentDuration / recommendedDuration;
      
      let status: 'optimal' | 'too-fast' | 'too-slow' = 'optimal';
      if (wordsPerSecond > 3.5) status = 'too-fast';
      else if (wordsPerSecond < 1.5) status = 'too-slow';

      return {
        sceneIndex: index,
        currentDuration,
        recommendedDuration,
        paceMultiplier,
        status,
      };
    });

    setScenePacing(pacing);
  };

  const handlePaceChange = (sceneIndex: number, newMultiplier: number) => {
    setScenePacing(prev =>
      prev.map(sp =>
        sp.sceneIndex === sceneIndex
          ? {
              ...sp,
              paceMultiplier: newMultiplier,
              currentDuration: sp.recommendedDuration * newMultiplier,
            }
          : sp
      )
    );
  };

  const applyChanges = () => {
    if (!onScenesUpdated) return;

    const updatedScenes = scenes.map((scene, index) => {
      const pacing = scenePacing.find(sp => sp.sceneIndex === index);
      if (!pacing) return scene;

      const newDuration = pacing.currentDuration;
      return {
        ...scene,
        duration: newDuration,
      };
    });

    onScenesUpdated(updatedScenes);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'optimal':
        return <Badge color="success" icon={<CheckmarkCircle24Regular />}>Optimal</Badge>;
      case 'too-fast':
        return <Badge color="danger" icon={<Warning24Regular />}>Too Fast</Badge>;
      case 'too-slow':
        return <Badge color="warning" icon={<Warning24Regular />}>Too Slow</Badge>;
      default:
        return null;
    }
  };

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (!optimizationActive) {
    return (
      <div className={styles.emptyState}>
        <Body1>Click &quot;Optimize Pacing&quot; to adjust scene pacing</Body1>
      </div>
    );
  }

  if (scenePacing.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Body1>No pacing data available</Body1>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.sceneList}>
        {scenePacing.map((pacing) => {
          const scene = scenes[pacing.sceneIndex];
          
          return (
            <Card key={pacing.sceneIndex} className={styles.sceneCard}>
              <div className={styles.sceneHeader}>
                <div>
                  <Body1Strong>Scene {pacing.sceneIndex + 1}</Body1Strong>
                  <Caption1>{scene.heading}</Caption1>
                </div>
                {getStatusBadge(pacing.status)}
              </div>

              <div className={styles.sceneInfo}>
                <div>
                  <Caption1>Current: {formatDuration(scene.duration)}</Caption1>
                </div>
                <ChevronRight24Regular />
                <div>
                  <Caption1>Adjusted: {formatDuration(pacing.currentDuration)}</Caption1>
                </div>
                {pacing.recommendedDuration !== pacing.currentDuration && (
                  <>
                    <Divider vertical />
                    <Caption1 style={{ color: tokens.colorPaletteBlueForeground2 }}>
                      Recommended: {formatDuration(pacing.recommendedDuration)}
                    </Caption1>
                  </>
                )}
              </div>

              <div className={styles.sliderContainer}>
                <Slider
                  min={0.5}
                  max={1.5}
                  step={0.05}
                  value={pacing.paceMultiplier}
                  onChange={(_, data) => handlePaceChange(pacing.sceneIndex, data.value)}
                />
                <div className={styles.footer}>
                  <Caption1>50% slower</Caption1>
                  <Caption1>{(pacing.paceMultiplier * 100).toFixed(0)}%</Caption1>
                  <Caption1>50% faster</Caption1>
                </div>
              </div>
            </Card>
          );
        })}
      </div>

      <div style={{ marginTop: tokens.spacingVerticalL, textAlign: 'center' }}>
        <Button
          appearance="primary"
          onClick={applyChanges}
          disabled={!onScenesUpdated}
        >
          Apply Pace Adjustments
        </Button>
      </div>
    </div>
  );
};
