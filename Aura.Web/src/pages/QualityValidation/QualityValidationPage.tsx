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
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  VideoClip24Regular,
  Speaker224Regular,
  Timer24Regular,
  Apps24Regular,
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
  validationStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  validIcon: {
    color: tokens.colorPaletteGreenForeground1,
    fontSize: '24px',
  },
  invalidIcon: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: '24px',
  },
});

type TabValue = 'resolution' | 'audio' | 'framerate' | 'consistency' | 'platform';

interface ValidationResult {
  isValid: boolean;
  score: number;
  issues?: string[];
  message?: string;
}

const QualityValidationPage: React.FC = () => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<TabValue>('resolution');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [width, setWidth] = useState('1920');
  const [height, setHeight] = useState('1080');
  const [minResolution, setMinResolution] = useState('1280x720');
  const [resolutionResult, setResolutionResult] = useState<ValidationResult | null>(null);

  const [audioFilePath, setAudioFilePath] = useState('');
  const [audioResult, setAudioResult] = useState<ValidationResult | null>(null);

  const [expectedFps, setExpectedFps] = useState('30');
  const [actualFps, setActualFps] = useState('29.97');
  const [tolerance, setTolerance] = useState('0.5');
  const [framerateResult, setFramerateResult] = useState<ValidationResult | null>(null);

  const [videoFilePath, setVideoFilePath] = useState('');
  const [consistencyResult, setConsistencyResult] = useState<ValidationResult | null>(null);

  const [platform, setPlatform] = useState('youtube');
  const [platformWidth, setPlatformWidth] = useState('1920');
  const [platformHeight, setPlatformHeight] = useState('1080');
  const [fileSize, setFileSize] = useState('52428800');
  const [duration, setDuration] = useState('60');
  const [codec, setCodec] = useState('H.264');
  const [platformResult, setPlatformResult] = useState<ValidationResult | null>(null);

  const handleValidateResolution = useCallback(async () => {
    setLoading(true);
    setError(null);
    setResolutionResult(null);

    try {
      const response = await fetch(
        `/api/quality/validate/resolution?width=${width}&height=${height}&min_resolution=${minResolution}`
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Validation failed');
      }

      const data = await response.json();
      setResolutionResult(data.result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [width, height, minResolution]);

  const handleValidateAudio = useCallback(async () => {
    setLoading(true);
    setError(null);
    setAudioResult(null);

    try {
      const response = await fetch('/api/quality/validate/audio', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ audioFilePath }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Validation failed');
      }

      const data = await response.json();
      setAudioResult(data.result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [audioFilePath]);

  const handleValidateFramerate = useCallback(async () => {
    setLoading(true);
    setError(null);
    setFramerateResult(null);

    try {
      const response = await fetch(
        `/api/quality/validate/framerate?expected_fps=${expectedFps}&actual_fps=${actualFps}&tolerance=${tolerance}`
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Validation failed');
      }

      const data = await response.json();
      setFramerateResult(data.result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [expectedFps, actualFps, tolerance]);

  const handleValidateConsistency = useCallback(async () => {
    setLoading(true);
    setError(null);
    setConsistencyResult(null);

    try {
      const response = await fetch('/api/quality/validate/consistency', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ videoFilePath }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Validation failed');
      }

      const data = await response.json();
      setConsistencyResult(data.result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [videoFilePath]);

  const handleValidatePlatform = useCallback(async () => {
    setLoading(true);
    setError(null);
    setPlatformResult(null);

    try {
      const response = await fetch(
        `/api/quality/validate/platform-requirements?platform=${platform}&width=${platformWidth}&height=${platformHeight}&file_size_bytes=${fileSize}&duration_seconds=${duration}&codec=${codec}`
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Validation failed');
      }

      const data = await response.json();
      setPlatformResult(data.result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [platform, platformWidth, platformHeight, fileSize, duration, codec]);

  const renderValidationResult = (result: ValidationResult | null) => {
    if (!result) return null;

    return (
      <div className={styles.resultsSection}>
        <div className={styles.validationStatus}>
          <CheckmarkCircle24Regular
            className={result.isValid ? styles.validIcon : styles.invalidIcon}
          />
          <Title3>{result.isValid ? 'Valid' : 'Invalid'}</Title3>
        </div>
        <Text>Quality Score: {(result.score * 100).toFixed(1)}%</Text>
        {result.message && <Text>{result.message}</Text>}
        {result.issues && result.issues.length > 0 && (
          <div>
            <Text weight="semibold">Issues:</Text>
            {result.issues.map((issue, i) => (
              <Text key={i}>• {issue}</Text>
            ))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <CheckmarkCircle24Regular className={styles.headerIcon} />
        <div>
          <Title1>Quality Validation</Title1>
          <Text className={styles.subtitle}>
            Comprehensive quality checks for video resolution, audio, frame rate, and platform
            requirements
          </Text>
        </div>
      </div>

      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as TabValue)}
        className={styles.tabs}
      >
        <Tab value="resolution" icon={<VideoClip24Regular />}>
          Resolution
        </Tab>
        <Tab value="audio" icon={<Speaker224Regular />}>
          Audio Quality
        </Tab>
        <Tab value="framerate" icon={<Timer24Regular />}>
          Frame Rate
        </Tab>
        <Tab value="consistency" icon={<CheckmarkCircle24Regular />}>
          Consistency
        </Tab>
        <Tab value="platform" icon={<Apps24Regular />}>
          Platform Requirements
        </Tab>
      </TabList>

      <div className={styles.content}>
        {error && <ErrorState message={error} />}

        {selectedTab === 'resolution' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <VideoClip24Regular className={styles.toolIcon} />
              <Title2>Resolution Validation</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Width (pixels)" required>
                <Input
                  type="number"
                  value={width}
                  onChange={(_, data) => setWidth(data.value)}
                  placeholder="1920"
                />
              </Field>
              <Field label="Height (pixels)" required>
                <Input
                  type="number"
                  value={height}
                  onChange={(_, data) => setHeight(data.value)}
                  placeholder="1080"
                />
              </Field>
              <Field label="Minimum Resolution">
                <Input
                  value={minResolution}
                  onChange={(_, data) => setMinResolution(data.value)}
                  placeholder="1280x720"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleValidateResolution}
                  disabled={loading || !width || !height}
                >
                  {loading ? <Spinner size="tiny" /> : 'Validate Resolution'}
                </Button>
              </div>
            </div>
            {renderValidationResult(resolutionResult)}
          </Card>
        )}

        {selectedTab === 'audio' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Speaker224Regular className={styles.toolIcon} />
              <Title2>Audio Quality Validation</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Audio File Path" required>
                <Input
                  value={audioFilePath}
                  onChange={(_, data) => setAudioFilePath(data.value)}
                  placeholder="/path/to/audio.wav"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleValidateAudio}
                  disabled={loading || !audioFilePath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Validate Audio'}
                </Button>
              </div>
            </div>
            {renderValidationResult(audioResult)}
          </Card>
        )}

        {selectedTab === 'framerate' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Timer24Regular className={styles.toolIcon} />
              <Title2>Frame Rate Validation</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Expected FPS" required>
                <Input
                  type="number"
                  value={expectedFps}
                  onChange={(_, data) => setExpectedFps(data.value)}
                  placeholder="30"
                />
              </Field>
              <Field label="Actual FPS" required>
                <Input
                  type="number"
                  value={actualFps}
                  onChange={(_, data) => setActualFps(data.value)}
                  placeholder="29.97"
                />
              </Field>
              <Field label="Tolerance">
                <Input
                  type="number"
                  value={tolerance}
                  onChange={(_, data) => setTolerance(data.value)}
                  placeholder="0.5"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleValidateFramerate}
                  disabled={loading || !expectedFps || !actualFps}
                >
                  {loading ? <Spinner size="tiny" /> : 'Validate Frame Rate'}
                </Button>
              </div>
            </div>
            {renderValidationResult(framerateResult)}
          </Card>
        )}

        {selectedTab === 'consistency' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <CheckmarkCircle24Regular className={styles.toolIcon} />
              <Title2>Content Consistency Validation</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Video File Path" required>
                <Input
                  value={videoFilePath}
                  onChange={(_, data) => setVideoFilePath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleValidateConsistency}
                  disabled={loading || !videoFilePath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Validate Consistency'}
                </Button>
              </div>
            </div>
            {renderValidationResult(consistencyResult)}
          </Card>
        )}

        {selectedTab === 'platform' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Apps24Regular className={styles.toolIcon} />
              <Title2>Platform Requirements Validation</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Platform">
                <Dropdown
                  value={platform}
                  onOptionSelect={(_, data) => setPlatform(data.optionText || 'youtube')}
                >
                  <Option>youtube</Option>
                  <Option>tiktok</Option>
                  <Option>instagram</Option>
                  <Option>twitter</Option>
                </Dropdown>
              </Field>
              <Field label="Width (pixels)" required>
                <Input
                  type="number"
                  value={platformWidth}
                  onChange={(_, data) => setPlatformWidth(data.value)}
                  placeholder="1920"
                />
              </Field>
              <Field label="Height (pixels)" required>
                <Input
                  type="number"
                  value={platformHeight}
                  onChange={(_, data) => setPlatformHeight(data.value)}
                  placeholder="1080"
                />
              </Field>
              <Field label="File Size (bytes)" required>
                <Input
                  type="number"
                  value={fileSize}
                  onChange={(_, data) => setFileSize(data.value)}
                  placeholder="52428800"
                />
              </Field>
              <Field label="Duration (seconds)" required>
                <Input
                  type="number"
                  value={duration}
                  onChange={(_, data) => setDuration(data.value)}
                  placeholder="60"
                />
              </Field>
              <Field label="Codec">
                <Input
                  value={codec}
                  onChange={(_, data) => setCodec(data.value)}
                  placeholder="H.264"
                />
              </Field>
              <div className={styles.actions}>
                <Button appearance="primary" onClick={handleValidatePlatform} disabled={loading}>
                  {loading ? <Spinner size="tiny" /> : 'Validate Platform Requirements'}
                </Button>
              </div>
            </div>
            {renderValidationResult(platformResult)}
          </Card>
        )}
      </div>
    </div>
  );
};

export default QualityValidationPage;
