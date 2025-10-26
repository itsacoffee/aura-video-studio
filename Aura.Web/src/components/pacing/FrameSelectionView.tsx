/**
 * Frame Selection View Component
 * Visual interface for frame selection and analysis
 */

import {
  Card,
  makeStyles,
  tokens,
  Spinner,
  Body1,
  Body1Strong,
  Caption1,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import { Image24Regular, Star24Regular, Eye24Regular, Info24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { Scene } from '../../types';

interface FrameSelectionViewProps {
  scenes: Scene[];
  videoPath?: string;
  optimizationActive: boolean;
}

interface FrameInfo {
  index: number;
  timestamp: number;
  importanceScore: number;
  isKeyFrame: boolean;
  thumbnailUrl?: string;
}

const useStyles = makeStyles({
  container: {
    width: '100%',
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  framesGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  frameCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow8,
    },
  },
  frameCardSelected: {
    border: `2px solid ${tokens.colorBrandBackground}`,
  },
  thumbnail: {
    width: '100%',
    height: '120px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  frameInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  scoreBar: {
    height: '4px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusLarge,
    overflow: 'hidden',
    marginTop: tokens.spacingVerticalXS,
  },
  scoreFill: {
    height: '100%',
    transition: 'width 0.3s ease',
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

export const FrameSelectionView = ({
  scenes,
  videoPath: _videoPath,
  optimizationActive,
}: FrameSelectionViewProps) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [frames, setFrames] = useState<FrameInfo[]>([]);
  const [selectedFrameIndex, setSelectedFrameIndex] = useState<number | null>(null);

  useEffect(() => {
    if (optimizationActive && scenes.length > 0) {
      analyzeFrames();
    }
  }, [optimizationActive, scenes]);

  const analyzeFrames = async () => {
    setLoading(true);

    try {
      // Simulate frame analysis
      // In production, this would call the FrameAnalysisService API
      await new Promise((resolve) => setTimeout(resolve, 1000));

      const mockFrames: FrameInfo[] = scenes.flatMap((scene, sceneIndex) => {
        const numFrames = Math.floor(scene.duration / 2); // Sample every 2 seconds
        return Array.from({ length: numFrames }, (_, i) => ({
          index: sceneIndex * 100 + i,
          timestamp: scene.start + i * 2,
          importanceScore: Math.random() * 0.5 + 0.3, // Random score between 0.3 and 0.8
          isKeyFrame: i === 0 || i === numFrames - 1,
        }));
      });

      setFrames(mockFrames.sort((a, b) => b.importanceScore - a.importanceScore).slice(0, 20));
    } catch (error) {
      console.error('Frame analysis failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const getScoreColor = (score: number): string => {
    if (score >= 0.7) return tokens.colorPaletteGreenBackground3;
    if (score >= 0.5) return tokens.colorPaletteYellowBackground3;
    return tokens.colorPaletteRedBackground3;
  };

  const formatTimestamp = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Analyzing video frames..." />
      </div>
    );
  }

  if (!optimizationActive) {
    return (
      <div className={styles.emptyState}>
        <Info24Regular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
        <Body1>Click &quot;Optimize Pacing&quot; to analyze video frames</Body1>
      </div>
    );
  }

  if (frames.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Image24Regular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
        <Body1>No frames analyzed yet</Body1>
        <Button
          appearance="primary"
          onClick={analyzeFrames}
          style={{ marginTop: tokens.spacingVerticalM }}
        >
          Analyze Frames
        </Button>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Body1Strong>Key Frames (Top {frames.length} by importance)</Body1Strong>
        <Caption1>Select frames to highlight important moments in your video</Caption1>
      </div>

      <div className={styles.framesGrid}>
        {frames.map((frame) => (
          <Card
            key={frame.index}
            className={`${styles.frameCard} ${
              selectedFrameIndex === frame.index ? styles.frameCardSelected : ''
            }`}
            onClick={() => setSelectedFrameIndex(frame.index)}
          >
            <div className={styles.thumbnail}>
              <Image24Regular style={{ fontSize: '48px', opacity: 0.3 }} />
            </div>

            <div className={styles.frameInfo}>
              <div
                style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              >
                <Body1Strong>{formatTimestamp(frame.timestamp)}</Body1Strong>
                {frame.isKeyFrame && (
                  <Tooltip content="Key Frame" relationship="label">
                    <Star24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                  </Tooltip>
                )}
              </div>

              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
              >
                <Eye24Regular style={{ fontSize: '16px' }} />
                <Caption1>Importance: {(frame.importanceScore * 100).toFixed(0)}%</Caption1>
              </div>

              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreFill}
                  style={{
                    width: `${frame.importanceScore * 100}%`,
                    backgroundColor: getScoreColor(frame.importanceScore),
                  }}
                />
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
};
