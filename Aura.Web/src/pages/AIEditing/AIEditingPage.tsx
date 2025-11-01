import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Tab,
  TabList,
  Field,
  Input,
  Slider,
} from '@fluentui/react-components';
import {
  VideoClip24Regular,
  Cut24Regular,
  Timer24Regular,
  Wand24Regular,
  Lightbulb24Regular,
  Crop24Regular,
  TextDescription24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { ErrorState } from '../../components/Loading';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  toolHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  toolIcon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '600px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  resultsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  resultItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
});

type ToolTab = 'scenes' | 'highlights' | 'beats' | 'framing' | 'captions';

interface SceneResult {
  timestamp: number;
  confidence: number;
}

interface HighlightResult {
  startTime: number;
  endTime: number;
  score: number;
  type: string;
}

interface BeatResult {
  timestamp: number;
  strength: number;
}

interface CaptionResult {
  text: string;
  startTime: number;
  endTime: number;
}

export const AIEditingPage: React.FC = () => {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<ToolTab>('scenes');
  const [videoPath, setVideoPath] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sceneResults, setSceneResults] = useState<SceneResult[]>([]);
  const [highlightResults, setHighlightResults] = useState<HighlightResult[]>([]);
  const [beatResults, setBeatResults] = useState<BeatResult[]>([]);
  const [captionResults, setCaptionResults] = useState<CaptionResult[]>([]);
  const [sceneThreshold, setSceneThreshold] = useState(0.3);
  const [highlightMinDuration, setHighlightMinDuration] = useState(2);
  const [aspectRatio, setAspectRatio] = useState('9:16');

  const handleDetectScenes = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/ai-editing/detect-scenes', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          videoPath,
          threshold: sceneThreshold,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to detect scenes');
      }

      const data = await response.json();
      setSceneResults(data.result.scenes || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath, sceneThreshold]);

  const handleDetectHighlights = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/ai-editing/detect-highlights', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          videoPath,
          minDuration: highlightMinDuration,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to detect highlights');
      }

      const data = await response.json();
      setHighlightResults(data.result.highlights || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath, highlightMinDuration]);

  const handleDetectBeats = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/ai-editing/detect-beats', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          videoPath,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to detect beats');
      }

      const data = await response.json();
      setBeatResults(data.result.beats || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath]);

  const handleAutoFrame = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const endpoint =
        aspectRatio === '9:16'
          ? '/api/ai-editing/convert-vertical'
          : '/api/ai-editing/convert-square';

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          videoPath,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to auto-frame video');
      }

      const data = await response.json();
      alert(`Auto-framing complete! Output: ${data.result.outputPath}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath, aspectRatio]);

  const handleGenerateCaptions = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/ai-editing/generate-captions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          videoPath,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to generate captions');
      }

      const data = await response.json();
      setCaptionResults(data.result.captions || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath]);

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Wand24Regular className={styles.headerIcon} />
        <div>
          <Title1>AI Editing Tools</Title1>
          <Text className={styles.subtitle}>
            Intelligent video editing powered by AI: scene detection, highlight extraction, beat
            sync, auto-framing, and caption generation
          </Text>
        </div>
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as ToolTab)}
      >
        <Tab value="scenes" icon={<Cut24Regular />}>
          Scene Detection
        </Tab>
        <Tab value="highlights" icon={<Lightbulb24Regular />}>
          Highlights
        </Tab>
        <Tab value="beats" icon={<Timer24Regular />}>
          Beat Sync
        </Tab>
        <Tab value="framing" icon={<Crop24Regular />}>
          Auto-Framing
        </Tab>
        <Tab value="captions" icon={<TextDescription24Regular />}>
          Captions
        </Tab>
      </TabList>

      {error && <ErrorState message={error} />}

      <div className={styles.content}>
        {activeTab === 'scenes' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Cut24Regular className={styles.toolIcon} />
              <div>
                <Title2>Scene Detection</Title2>
                <Text>Automatically detect scene changes in your video</Text>
              </div>
            </div>

            <div className={styles.form}>
              <Field label="Video Path" required>
                <Input
                  value={videoPath}
                  onChange={(_, data) => setVideoPath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>

              <Field label={`Detection Threshold: ${sceneThreshold.toFixed(2)}`}>
                <Slider
                  min={0.1}
                  max={0.9}
                  step={0.1}
                  value={sceneThreshold}
                  onChange={(_, data) => setSceneThreshold(data.value)}
                />
              </Field>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={<VideoClip24Regular />}
                  onClick={handleDetectScenes}
                  disabled={loading || !videoPath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Detect Scenes'}
                </Button>
              </div>
            </div>

            {sceneResults.length > 0 && (
              <div className={styles.resultsSection}>
                <Title3>Detected Scenes ({sceneResults.length})</Title3>
                <div className={styles.resultsList}>
                  {sceneResults.map((scene, index) => (
                    <div key={index} className={styles.resultItem}>
                      <div>
                        <Text weight="semibold">Scene {index + 1}</Text>
                        <Text> at {formatTime(scene.timestamp)}</Text>
                      </div>
                      <Text>Confidence: {(scene.confidence * 100).toFixed(1)}%</Text>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Card>
        )}

        {activeTab === 'highlights' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Lightbulb24Regular className={styles.toolIcon} />
              <div>
                <Title2>Highlight Detection</Title2>
                <Text>Find the most engaging moments in your video</Text>
              </div>
            </div>

            <div className={styles.form}>
              <Field label="Video Path" required>
                <Input
                  value={videoPath}
                  onChange={(_, data) => setVideoPath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>

              <Field label="Minimum Duration (seconds)">
                <Input
                  type="number"
                  value={highlightMinDuration.toString()}
                  onChange={(_, data) => setHighlightMinDuration(Number(data.value))}
                />
              </Field>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={<Lightbulb24Regular />}
                  onClick={handleDetectHighlights}
                  disabled={loading || !videoPath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Detect Highlights'}
                </Button>
              </div>
            </div>

            {highlightResults.length > 0 && (
              <div className={styles.resultsSection}>
                <Title3>Detected Highlights ({highlightResults.length})</Title3>
                <div className={styles.resultsList}>
                  {highlightResults.map((highlight, index) => (
                    <div key={index} className={styles.resultItem}>
                      <div>
                        <Text weight="semibold">Highlight {index + 1}</Text>
                        <Text>
                          {' '}
                          ({formatTime(highlight.startTime)} - {formatTime(highlight.endTime)})
                        </Text>
                        <Text> - {highlight.type}</Text>
                      </div>
                      <Text>Score: {(highlight.score * 100).toFixed(1)}%</Text>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Card>
        )}

        {activeTab === 'beats' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Timer24Regular className={styles.toolIcon} />
              <div>
                <Title2>Beat Detection & Sync</Title2>
                <Text>Detect audio beats and sync cuts to music</Text>
              </div>
            </div>

            <div className={styles.form}>
              <Field label="Video Path" required>
                <Input
                  value={videoPath}
                  onChange={(_, data) => setVideoPath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={<Timer24Regular />}
                  onClick={handleDetectBeats}
                  disabled={loading || !videoPath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Detect Beats'}
                </Button>
              </div>
            </div>

            {beatResults.length > 0 && (
              <div className={styles.resultsSection}>
                <Title3>Detected Beats ({beatResults.length})</Title3>
                <div className={styles.resultsList}>
                  {beatResults.slice(0, 50).map((beat, index) => (
                    <div key={index} className={styles.resultItem}>
                      <div>
                        <Text weight="semibold">Beat {index + 1}</Text>
                        <Text> at {formatTime(beat.timestamp)}</Text>
                      </div>
                      <Text>Strength: {(beat.strength * 100).toFixed(1)}%</Text>
                    </div>
                  ))}
                  {beatResults.length > 50 && <Text>... and {beatResults.length - 50} more</Text>}
                </div>
              </div>
            )}
          </Card>
        )}

        {activeTab === 'framing' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Crop24Regular className={styles.toolIcon} />
              <div>
                <Title2>Auto-Framing</Title2>
                <Text>Intelligently reframe video for different aspect ratios</Text>
              </div>
            </div>

            <div className={styles.form}>
              <Field label="Video Path" required>
                <Input
                  value={videoPath}
                  onChange={(_, data) => setVideoPath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>

              <Field label="Target Aspect Ratio">
                <Input
                  value={aspectRatio}
                  onChange={(_, data) => setAspectRatio(data.value)}
                  placeholder="9:16 or 1:1"
                />
              </Field>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={<Crop24Regular />}
                  onClick={handleAutoFrame}
                  disabled={loading || !videoPath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Auto-Frame Video'}
                </Button>
              </div>
            </div>
          </Card>
        )}

        {activeTab === 'captions' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <TextDescription24Regular className={styles.toolIcon} />
              <div>
                <Title2>Auto-Captioning</Title2>
                <Text>Generate accurate captions using speech recognition</Text>
              </div>
            </div>

            <div className={styles.form}>
              <Field label="Video Path" required>
                <Input
                  value={videoPath}
                  onChange={(_, data) => setVideoPath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={<TextDescription24Regular />}
                  onClick={handleGenerateCaptions}
                  disabled={loading || !videoPath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Generate Captions'}
                </Button>
              </div>
            </div>

            {captionResults.length > 0 && (
              <div className={styles.resultsSection}>
                <Title3>Generated Captions ({captionResults.length})</Title3>
                <div className={styles.resultsList}>
                  {captionResults.map((caption, index) => (
                    <div key={index} className={styles.resultItem}>
                      <div>
                        <Text weight="semibold">
                          {formatTime(caption.startTime)} - {formatTime(caption.endTime)}
                        </Text>
                        <Text>: {caption.text}</Text>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Card>
        )}
      </div>
    </div>
  );
};

export default AIEditingPage;
