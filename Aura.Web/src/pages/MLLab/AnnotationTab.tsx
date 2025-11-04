import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Spinner,
  MessageBar,
  MessageBarBody,
  Input,
  Label,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Video24Regular,
  Save24Regular,
  ThumbLike24Regular,
  ThumbDislike24Regular,
  ChevronLeft24Regular,
  ChevronRight24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, type FC } from 'react';
import { useMLLabStore } from '../../state/mlLab';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  videoSelector: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  videoInput: {
    flex: 1,
  },
  videoList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  videoCard: {
    padding: tokens.spacingVerticalM,
  },
  videoCardContent: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  videoInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  statCard: {
    padding: tokens.spacingVerticalL,
  },
  statValue: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  statLabel: {
    color: tokens.colorNeutralForeground3,
  },
  frameViewer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  framePreview: {
    width: '100%',
    maxWidth: '800px',
    height: '450px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    margin: '0 auto',
  },
  frameControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    justifyContent: 'center',
  },
  ratingButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  emptyState: {
    padding: `${tokens.spacingVerticalXXXL} ${tokens.spacingHorizontalXXL}`,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
});

export const AnnotationTab: FC = () => {
  const styles = useStyles();
  const {
    videos,
    selectedVideoPath,
    currentFrameIndex,
    annotationStats,
    isLoadingStats,
    isSavingAnnotations,
    error,
    addVideo,
    selectVideo,
    rateFrame,
    setCurrentFrameIndex,
    saveAnnotations,
    loadAnnotationStats,
  } = useMLLabStore();

  const [videoPath, setVideoPath] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);

  const selectedVideo = videos.find((v) => v.path === selectedVideoPath);
  const currentFrame = selectedVideo?.frames[currentFrameIndex];
  const hasAnnotations = videos.some((v) => v.frames.some((f) => f.rating !== undefined));

  const handleAddVideo = useCallback(async () => {
    if (!videoPath.trim()) return;

    setIsProcessing(true);
    try {
      // In a real implementation, this would call an API to extract frames
      // For now, we'll create a mock video with placeholder frames
      const mockFrames = Array.from({ length: 20 }, (_, i) => ({
        id: `${videoPath}-frame-${i}`,
        framePath: `${videoPath}/frame_${i.toString().padStart(4, '0')}.jpg`,
        videoPath: videoPath,
        timestamp: i * 5,
      }));

      addVideo({
        path: videoPath,
        name: videoPath.split('/').pop() || videoPath,
        framesExtracted: mockFrames.length,
        framesAnnotated: 0,
        frames: mockFrames,
      });

      setVideoPath('');
    } catch (error) {
      console.error('Failed to add video:', error);
    } finally {
      setIsProcessing(false);
    }
  }, [videoPath, addVideo]);

  const handleRateFrame = useCallback(
    (rating: number) => {
      if (!selectedVideoPath || !currentFrame) return;
      rateFrame(selectedVideoPath, currentFrame.id, rating);
    },
    [selectedVideoPath, currentFrame, rateFrame]
  );

  const handlePreviousFrame = useCallback(() => {
    if (currentFrameIndex > 0) {
      setCurrentFrameIndex(currentFrameIndex - 1);
    }
  }, [currentFrameIndex, setCurrentFrameIndex]);

  const handleNextFrame = useCallback(() => {
    if (selectedVideo && currentFrameIndex < selectedVideo.frames.length - 1) {
      setCurrentFrameIndex(currentFrameIndex + 1);
    }
  }, [currentFrameIndex, selectedVideo, setCurrentFrameIndex]);

  const handleSaveAnnotations = useCallback(async () => {
    try {
      await saveAnnotations();
      await loadAnnotationStats();
    } catch (error) {
      console.error('Failed to save annotations:', error);
    }
  }, [saveAnnotations, loadAnnotationStats]);

  const totalFrames = videos.reduce((sum, v) => sum + v.framesExtracted, 0);
  const annotatedFrames = videos.reduce((sum, v) => sum + v.framesAnnotated, 0);
  const annotationProgress = totalFrames > 0 ? (annotatedFrames / totalFrames) * 100 : 0;

  return (
    <div className={styles.container}>
      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {/* Video Selection Section */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>1. Select Videos</Text>
        <div className={styles.videoSelector}>
          <div className={styles.videoInput}>
            <Label htmlFor="video-path">Video Path</Label>
            <Input
              id="video-path"
              placeholder="Enter path to video file or folder"
              value={videoPath}
              onChange={(_, data) => setVideoPath(data.value)}
              disabled={isProcessing}
            />
          </div>
          <Button
            icon={<Add24Regular />}
            appearance="primary"
            onClick={handleAddVideo}
            disabled={!videoPath.trim() || isProcessing}
          >
            {isProcessing ? 'Processing...' : 'Add Video'}
          </Button>
        </div>

        {videos.length > 0 && (
          <div className={styles.videoList}>
            {videos.map((video) => (
              <Card
                key={video.path}
                className={styles.videoCard}
                appearance={video.path === selectedVideoPath ? 'filled' : 'outline'}
              >
                <div className={styles.videoCardContent}>
                  <div className={styles.videoInfo}>
                    <Video24Regular />
                    <div>
                      <Text weight="semibold">{video.name}</Text>
                      <Text size={200} style={{ display: 'block' }}>
                        {video.framesExtracted} frames extracted • {video.framesAnnotated} annotated
                      </Text>
                    </div>
                  </div>
                  <Button
                    appearance={video.path === selectedVideoPath ? 'primary' : 'secondary'}
                    onClick={() => selectVideo(video.path)}
                  >
                    {video.path === selectedVideoPath ? 'Selected' : 'Select'}
                  </Button>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Annotation Stats Section */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>2. Annotation Progress</Text>
        {isLoadingStats ? (
          <Spinner label="Loading statistics..." />
        ) : (
          <div className={styles.statsGrid}>
            <Card className={styles.statCard}>
              <Text className={styles.statValue}>{annotatedFrames}</Text>
              <Text className={styles.statLabel}>Frames Annotated</Text>
              <ProgressBar value={annotationProgress / 100} style={{ marginTop: '8px' }} />
            </Card>
            <Card className={styles.statCard}>
              <Text className={styles.statValue}>{totalFrames}</Text>
              <Text className={styles.statLabel}>Total Frames</Text>
            </Card>
            <Card className={styles.statCard}>
              <Text className={styles.statValue}>{annotationStats?.totalAnnotations || 0}</Text>
              <Text className={styles.statLabel}>Saved to Backend</Text>
            </Card>
            <Card className={styles.statCard}>
              <Text className={styles.statValue}>
                {annotationStats?.averageRating
                  ? (annotationStats.averageRating * 100).toFixed(0) + '%'
                  : 'N/A'}
              </Text>
              <Text className={styles.statLabel}>Average Rating</Text>
            </Card>
          </div>
        )}
      </div>

      {/* Frame Annotation Section */}
      {selectedVideo ? (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>3. Rate Frames</Text>
          <div className={styles.frameViewer}>
            <div className={styles.framePreview}>
              <Text style={{ color: tokens.colorNeutralForeground3 }}>
                Frame Preview ({currentFrameIndex + 1} / {selectedVideo.frames.length})
              </Text>
            </div>

            <div className={styles.frameControls}>
              <Button
                icon={<ChevronLeft24Regular />}
                onClick={handlePreviousFrame}
                disabled={currentFrameIndex === 0}
              >
                Previous
              </Button>

              <div className={styles.ratingButtons}>
                <Button
                  icon={<ThumbDislike24Regular />}
                  appearance={currentFrame?.rating === 0 ? 'primary' : 'secondary'}
                  onClick={() => handleRateFrame(0)}
                >
                  Bad Frame
                </Button>
                <Button
                  icon={<ThumbLike24Regular />}
                  appearance={currentFrame?.rating === 1 ? 'primary' : 'secondary'}
                  onClick={() => handleRateFrame(1)}
                >
                  Good Frame
                </Button>
              </div>

              <Button
                icon={<ChevronRight24Regular />}
                onClick={handleNextFrame}
                disabled={currentFrameIndex >= selectedVideo.frames.length - 1}
              >
                Next
              </Button>
            </div>

            {currentFrame?.rating !== undefined && (
              <Text style={{ textAlign: 'center' }}>
                Current rating: {currentFrame.rating === 1 ? '👍 Good' : '👎 Bad'}
              </Text>
            )}
          </div>
        </div>
      ) : (
        <div className={styles.emptyState}>
          <Text size={500}>Select a video above to start annotating frames</Text>
        </div>
      )}

      {/* Save Section */}
      {hasAnnotations && (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>4. Save Annotations</Text>
          <Button
            icon={<Save24Regular />}
            appearance="primary"
            size="large"
            onClick={handleSaveAnnotations}
            disabled={isSavingAnnotations}
          >
            {isSavingAnnotations ? 'Saving...' : 'Save All Annotations'}
          </Button>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Save your annotations to the backend before training. You have {annotatedFrames} unsaved
            annotations.
          </Text>
        </div>
      )}
    </div>
  );
};
