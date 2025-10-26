import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Dropdown,
  Option,
  Input,
  Label,
  Spinner,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import {
  VideoClip24Regular,
  Star24Regular,
  MusicNote224Regular,
  Crop24Regular,
  Subtitles24Regular,
  Play24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { aiEditingService } from '../../services/aiEditingService';
import type {
  SceneDetectionResult,
  HighlightDetectionResult,
  BeatDetectionResult,
  AutoFramingResult,
  SpeechRecognitionResult,
} from '../../services/aiEditingService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  cardHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  cardContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  results: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    maxHeight: '200px',
    overflowY: 'auto',
  },
  resultItem: {
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    '&:last-child': {
      borderBottom: 'none',
    },
  },
});

export interface AutoEditPanelProps {
  videoPath?: string;
  onApply?: (feature: string, data: any) => void;
}

export function AutoEditPanel({ videoPath, onApply }: AutoEditPanelProps) {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sceneResult, setSceneResult] = useState<SceneDetectionResult | null>(null);
  const [highlightResult, setHighlightResult] = useState<HighlightDetectionResult | null>(null);
  const [beatResult, setBeatResult] = useState<BeatDetectionResult | null>(null);
  const [framingResult, setFramingResult] = useState<AutoFramingResult | null>(null);
  const [captionResult, setCaptionResult] = useState<SpeechRecognitionResult | null>(null);

  // Form inputs
  const [sceneThreshold, setSceneThreshold] = useState('0.3');
  const [maxHighlights, setMaxHighlights] = useState('10');
  const [targetFormat, setTargetFormat] = useState<'vertical' | 'square'>('vertical');
  const [captionLanguage, setCaptionLanguage] = useState('en');

  const handleDetectScenes = async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await aiEditingService.detectScenes({
        videoPath,
        threshold: parseFloat(sceneThreshold),
      });
      setSceneResult(result);
      onApply?.('scenes', result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to detect scenes');
    } finally {
      setLoading(false);
    }
  };

  const handleDetectHighlights = async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await aiEditingService.detectHighlights({
        videoPath,
        maxHighlights: parseInt(maxHighlights),
      });
      setHighlightResult(result);
      onApply?.('highlights', result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to detect highlights');
    } finally {
      setLoading(false);
    }
  };

  const handleDetectBeats = async () => {
    if (!videoPath) {
      setError('Please provide a video/audio path');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await aiEditingService.detectBeats({ filePath: videoPath });
      setBeatResult(result);
      onApply?.('beats', result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to detect beats');
    } finally {
      setLoading(false);
    }
  };

  const handleAutoFrame = async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result =
        targetFormat === 'vertical'
          ? await aiEditingService.convertToVertical(videoPath)
          : await aiEditingService.convertToSquare(videoPath);
      setFramingResult(result);
      onApply?.('framing', result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze framing');
    } finally {
      setLoading(false);
    }
  };

  const handleGenerateCaptions = async () => {
    if (!videoPath) {
      setError('Please provide a video/audio path');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await aiEditingService.generateCaptions({
        filePath: videoPath,
        language: captionLanguage,
      });
      setCaptionResult(result);
      onApply?.('captions', result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate captions');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>AI-Powered Auto Editing</Title3>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.grid}>
        {/* Scene Detection */}
        <Card className={styles.card}>
          <div className={styles.cardHeader}>
            <VideoClip24Regular />
            <Text weight="semibold">Scene Detection</Text>
          </div>
          <div className={styles.cardContent}>
            <Text size={200}>
              Automatically detect scene changes based on visual content and camera cuts
            </Text>
            <div className={styles.field}>
              <Label>Detection Threshold</Label>
              <Input
                type="number"
                value={sceneThreshold}
                onChange={(e) => setSceneThreshold(e.target.value)}
                step="0.1"
                min="0"
                max="1"
              />
            </div>
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<Play24Regular />}
                onClick={handleDetectScenes}
                disabled={loading || !videoPath}
              >
                Detect Scenes
              </Button>
            </div>
            {sceneResult && (
              <div className={styles.results}>
                <Text size={200} weight="semibold">
                  Found {sceneResult.scenes.length} scenes
                </Text>
                {sceneResult.scenes.slice(0, 3).map((scene, i) => (
                  <div key={i} className={styles.resultItem}>
                    <Text size={100}>
                      {scene.timestamp} - {scene.description} ({(scene.confidence * 100).toFixed(0)}
                      %)
                    </Text>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>

        {/* Highlight Detection */}
        <Card className={styles.card}>
          <div className={styles.cardHeader}>
            <Star24Regular />
            <Text weight="semibold">Highlight Detection</Text>
          </div>
          <div className={styles.cardContent}>
            <Text size={200}>
              Find the most engaging moments based on action, expressions, and audio
            </Text>
            <div className={styles.field}>
              <Label>Max Highlights</Label>
              <Input
                type="number"
                value={maxHighlights}
                onChange={(e) => setMaxHighlights(e.target.value)}
                min="1"
                max="50"
              />
            </div>
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<Star24Regular />}
                onClick={handleDetectHighlights}
                disabled={loading || !videoPath}
              >
                Find Highlights
              </Button>
            </div>
            {highlightResult && (
              <div className={styles.results}>
                <Text size={200} weight="semibold">
                  Found {highlightResult.highlights.length} highlights
                </Text>
                {highlightResult.highlights.slice(0, 3).map((highlight, i) => (
                  <div key={i} className={styles.resultItem}>
                    <Text size={100}>
                      {highlight.type} - {highlight.reasoning} (Score:{' '}
                      {(highlight.score * 100).toFixed(0)})
                    </Text>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>

        {/* Beat Detection */}
        <Card className={styles.card}>
          <div className={styles.cardHeader}>
            <MusicNote224Regular />
            <Text weight="semibold">Beat Detection</Text>
          </div>
          <div className={styles.cardContent}>
            <Text size={200}>Detect beats in music and sync cuts to rhythm automatically</Text>
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<MusicNote224Regular />}
                onClick={handleDetectBeats}
                disabled={loading || !videoPath}
              >
                Detect Beats
              </Button>
            </div>
            {beatResult && (
              <div className={styles.results}>
                <Text size={200} weight="semibold">
                  Found {beatResult.totalBeats} beats at {beatResult.averageTempo.toFixed(1)} BPM
                </Text>
              </div>
            )}
          </div>
        </Card>

        {/* Auto Framing */}
        <Card className={styles.card}>
          <div className={styles.cardHeader}>
            <Crop24Regular />
            <Text weight="semibold">Auto Framing</Text>
          </div>
          <div className={styles.cardContent}>
            <Text size={200}>Automatically crop and reframe for vertical or square formats</Text>
            <div className={styles.field}>
              <Label>Target Format</Label>
              <Dropdown
                value={targetFormat === 'vertical' ? 'Vertical (9:16)' : 'Square (1:1)'}
                onOptionSelect={(_, data) =>
                  setTargetFormat(data.optionValue === 'vertical' ? 'vertical' : 'square')
                }
              >
                <Option value="vertical">Vertical (9:16)</Option>
                <Option value="square">Square (1:1)</Option>
              </Dropdown>
            </div>
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<Crop24Regular />}
                onClick={handleAutoFrame}
                disabled={loading || !videoPath}
              >
                Analyze Framing
              </Button>
            </div>
            {framingResult && (
              <div className={styles.results}>
                <Text size={200} weight="semibold">
                  Generated {framingResult.suggestions.length} framing suggestions
                </Text>
              </div>
            )}
          </div>
        </Card>

        {/* Auto Captions */}
        <Card className={styles.card}>
          <div className={styles.cardHeader}>
            <Subtitles24Regular />
            <Text weight="semibold">Auto Captions</Text>
          </div>
          <div className={styles.cardContent}>
            <Text size={200}>Generate accurate subtitles with speech recognition</Text>
            <div className={styles.field}>
              <Label>Language</Label>
              <Dropdown
                value={captionLanguage}
                onOptionSelect={(_, data) => setCaptionLanguage(data.optionValue as string)}
              >
                <Option value="en">English</Option>
                <Option value="es">Spanish</Option>
                <Option value="fr">French</Option>
                <Option value="de">German</Option>
                <Option value="it">Italian</Option>
                <Option value="pt">Portuguese</Option>
                <Option value="zh">Chinese</Option>
                <Option value="ja">Japanese</Option>
              </Dropdown>
            </div>
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<Subtitles24Regular />}
                onClick={handleGenerateCaptions}
                disabled={loading || !videoPath}
              >
                Generate Captions
              </Button>
            </div>
            {captionResult && (
              <div className={styles.results}>
                <Text size={200} weight="semibold">
                  Generated {captionResult.captions.length} captions (
                  {(captionResult.averageConfidence * 100).toFixed(0)}% confidence)
                </Text>
                {captionResult.captions.slice(0, 3).map((caption, i) => (
                  <div key={i} className={styles.resultItem}>
                    <Text size={100}>{caption.text}</Text>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>
      </div>

      {loading && (
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          <Spinner size="small" />
          <Text>Processing...</Text>
        </div>
      )}
    </div>
  );
}
