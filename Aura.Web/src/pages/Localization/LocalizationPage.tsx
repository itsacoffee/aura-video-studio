import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Spinner,
  makeStyles,
  tokens,
  Tab,
  TabList,
  Field,
  Input,
  Dropdown,
  Option,
  Textarea,
  ProgressBar,
} from '@fluentui/react-components';
import {
  LocalLanguage24Regular,
  ArrowSync24Regular,
  TextDescription24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useRef, useEffect } from 'react';
import { ErrorState } from '../../components/Loading';
import { getOperationTimeout } from '../../config/timeouts';

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
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '800px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  resultText: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    fontFamily: 'monospace',
    fontSize: '14px',
    whiteSpace: 'pre-wrap',
  },
  progressContainer: {
    marginTop: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
  },
  progressText: {
    marginTop: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
    fontSize: '12px',
  },
});

type TabValue = 'translate' | 'subtitles' | 'adapt';

// Timeout error class to distinguish timeout from other errors
class TimeoutError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'TimeoutError';
  }
}

export const LocalizationPage: React.FC = () => {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<TabValue>('translate');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sourceText, setSourceText] = useState('');
  const [sourceLanguage, setSourceLanguage] = useState('en');
  const [targetLanguage, setTargetLanguage] = useState('es');
  const [translatedText, setTranslatedText] = useState('');
  const [videoPath, setVideoPath] = useState('');
  const [subtitles, setSubtitles] = useState('');
  const [loadingMessage, setLoadingMessage] = useState('');

  // AbortController ref for cancelling in-flight requests
  const abortControllerRef = useRef<AbortController | null>(null);

  // Cleanup abort controller on unmount
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  // Cancel any in-flight request
  const cancelRequest = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
    setLoading(false);
    setLoadingMessage('');
  }, []);

  // Helper function to handle request errors
  const handleRequestError = useCallback((err: unknown, operationType: string): string => {
    if (err instanceof Error && err.name === 'AbortError') {
      return `${operationType} request timed out. Please try again with shorter content or check your connection.`;
    } else if (err instanceof TimeoutError) {
      return err.message;
    }
    return err instanceof Error ? err.message : 'An error occurred';
  }, []);

  const handleTranslate = useCallback(async () => {
    if (!sourceText) {
      setError('Please enter text to translate');
      return;
    }

    // Cancel any previous request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }

    setLoading(true);
    setError(null);
    setLoadingMessage('Translating...');

    // Create new AbortController for this request
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    // Set up timeout
    const timeoutMs = getOperationTimeout('localization');
    const timeoutId = setTimeout(() => {
      abortController.abort();
    }, timeoutMs);

    try {
      const response = await fetch('/api/localization/translate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sourceText: sourceText,
          sourceLanguage,
          targetLanguage,
        }),
        signal: abortController.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        if (response.status === 408) {
          throw new TimeoutError(
            'Translation request timed out. Please try again with shorter text.'
          );
        }
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.detail || errorData.error || 'Translation failed');
      }

      const data = await response.json();
      setTranslatedText(data.translatedText || '');
    } catch (err: unknown) {
      setError(handleRequestError(err, 'Translation'));
    } finally {
      clearTimeout(timeoutId);
      setLoading(false);
      setLoadingMessage('');
      abortControllerRef.current = null;
    }
  }, [sourceText, sourceLanguage, targetLanguage, handleRequestError]);

  const handleGenerateSubtitles = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // Note: The generate-subtitles endpoint is not implemented.
      // For subtitle generation, use the translate-and-plan-ssml endpoint
      // which provides a full translation and SSML planning workflow.
      setError(
        'Subtitle generation requires video transcription. Please use the Create workflow to generate subtitles from your video script.'
      );
      setSubtitles('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath]);

  const handleAdaptContent = useCallback(async () => {
    if (!sourceText) {
      setError('Please enter content to adapt');
      return;
    }

    // Cancel any previous request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }

    setLoading(true);
    setError(null);
    setLoadingMessage('Analyzing cultural context...');

    // Create new AbortController for this request
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    // Set up timeout
    const timeoutMs = getOperationTimeout('localization');
    const timeoutId = setTimeout(() => {
      abortController.abort();
    }, timeoutMs);

    try {
      const response = await fetch('/api/localization/analyze-culture', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: sourceText,
          targetLanguage: targetLanguage,
          targetRegion: targetLanguage.includes('-')
            ? targetLanguage.split('-')[1]
            : targetLanguage,
        }),
        signal: abortController.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        if (response.status === 408) {
          throw new TimeoutError(
            'Cultural analysis request timed out. Please try again with shorter content.'
          );
        }
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.detail || errorData.error || 'Content adaptation failed');
      }

      const data = await response.json();
      // Format the cultural analysis result for display
      const analysisResult = [
        `Cultural Sensitivity Score: ${data.culturalSensitivityScore || 'N/A'}`,
        '',
        'Recommendations:',
        ...(data.recommendations || []).map(
          (rec: { category: string; recommendation: string }) =>
            `• ${rec.category}: ${rec.recommendation}`
        ),
        '',
        'Issues Found:',
        ...(data.issues || []).map(
          (issue: { category: string; issue: string; suggestion: string }) =>
            `• ${issue.category}: ${issue.issue} - ${issue.suggestion}`
        ),
      ].join('\n');
      setTranslatedText(analysisResult || 'No analysis results available');
    } catch (err: unknown) {
      setError(handleRequestError(err, 'Cultural analysis'));
    } finally {
      clearTimeout(timeoutId);
      setLoading(false);
      setLoadingMessage('');
      abortControllerRef.current = null;
    }
  }, [sourceText, targetLanguage, handleRequestError]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <LocalLanguage24Regular className={styles.headerIcon} />
        <div>
          <Title1>Localization & Translation</Title1>
          <Text className={styles.subtitle}>
            Translate content, generate subtitles, and adapt for different cultures and languages
          </Text>
        </div>
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as TabValue)}
      >
        <Tab value="translate" icon={<ArrowSync24Regular />}>
          Translation
        </Tab>
        <Tab value="subtitles" icon={<TextDescription24Regular />}>
          Subtitle Generation
        </Tab>
        <Tab value="adapt" icon={<LocalLanguage24Regular />}>
          Cultural Adaptation
        </Tab>
      </TabList>

      {error && <ErrorState message={error} />}

      {activeTab === 'translate' && (
        <Card className={styles.toolCard}>
          <Title2>Text Translation</Title2>
          <Text>Translate text content between languages</Text>

          <div className={styles.form}>
            <Field label="Source Language">
              <Dropdown
                value={sourceLanguage}
                onOptionSelect={(_, data) => setSourceLanguage(data.optionValue as string)}
              >
                <Option value="en">English</Option>
                <Option value="es">Spanish</Option>
                <Option value="fr">French</Option>
                <Option value="de">German</Option>
                <Option value="it">Italian</Option>
                <Option value="pt">Portuguese</Option>
                <Option value="zh">Chinese</Option>
                <Option value="ja">Japanese</Option>
                <Option value="ko">Korean</Option>
              </Dropdown>
            </Field>

            <Field label="Target Language">
              <Dropdown
                value={targetLanguage}
                onOptionSelect={(_, data) => setTargetLanguage(data.optionValue as string)}
              >
                <Option value="es">Spanish</Option>
                <Option value="fr">French</Option>
                <Option value="de">German</Option>
                <Option value="it">Italian</Option>
                <Option value="pt">Portuguese</Option>
                <Option value="zh">Chinese</Option>
                <Option value="ja">Japanese</Option>
                <Option value="ko">Korean</Option>
                <Option value="en">English</Option>
              </Dropdown>
            </Field>

            <Field label="Text to Translate" required>
              <Textarea
                value={sourceText}
                onChange={(_, data) => setSourceText(data.value)}
                placeholder="Enter text to translate..."
                rows={8}
              />
            </Field>

            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<ArrowSync24Regular />}
                onClick={handleTranslate}
                disabled={loading || !sourceText}
              >
                {loading ? <Spinner size="tiny" /> : 'Translate'}
              </Button>
              {loading && (
                <Button appearance="secondary" onClick={cancelRequest}>
                  Cancel
                </Button>
              )}
            </div>

            {loading && loadingMessage && (
              <div className={styles.progressContainer}>
                <ProgressBar />
                <Text className={styles.progressText}>{loadingMessage}</Text>
              </div>
            )}
          </div>

          {translatedText && (
            <div className={styles.resultsSection}>
              <Title2>Translation Result</Title2>
              <div className={styles.resultText}>{translatedText}</div>
            </div>
          )}
        </Card>
      )}

      {activeTab === 'subtitles' && (
        <Card className={styles.toolCard}>
          <Title2>Subtitle Generation</Title2>
          <Text>Generate translated subtitles for video content</Text>

          <div className={styles.form}>
            <Field label="Video Path" required>
              <Input
                value={videoPath}
                onChange={(_, data) => setVideoPath(data.value)}
                placeholder="/path/to/video.mp4"
              />
            </Field>

            <Field label="Target Language">
              <Dropdown
                value={targetLanguage}
                onOptionSelect={(_, data) => setTargetLanguage(data.optionValue as string)}
              >
                <Option value="es">Spanish</Option>
                <Option value="fr">French</Option>
                <Option value="de">German</Option>
                <Option value="it">Italian</Option>
                <Option value="pt">Portuguese</Option>
                <Option value="zh">Chinese</Option>
                <Option value="ja">Japanese</Option>
                <Option value="ko">Korean</Option>
              </Dropdown>
            </Field>

            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<TextDescription24Regular />}
                onClick={handleGenerateSubtitles}
                disabled={loading || !videoPath}
              >
                {loading ? <Spinner size="tiny" /> : 'Generate Subtitles'}
              </Button>
            </div>
          </div>

          {subtitles && (
            <div className={styles.resultsSection}>
              <Title2>Generated Subtitles</Title2>
              <div className={styles.resultText}>{subtitles}</div>
            </div>
          )}
        </Card>
      )}

      {activeTab === 'adapt' && (
        <Card className={styles.toolCard}>
          <Title2>Cultural Adaptation</Title2>
          <Text>Adapt content for cultural context and local references</Text>

          <div className={styles.form}>
            <Field label="Target Culture">
              <Dropdown
                value={targetLanguage}
                onOptionSelect={(_, data) => setTargetLanguage(data.optionValue as string)}
              >
                <Option value="es-MX">Spanish (Mexico)</Option>
                <Option value="es-ES">Spanish (Spain)</Option>
                <Option value="fr-FR">French (France)</Option>
                <Option value="fr-CA">French (Canada)</Option>
                <Option value="de-DE">German (Germany)</Option>
                <Option value="it-IT">Italian (Italy)</Option>
                <Option value="pt-BR">Portuguese (Brazil)</Option>
                <Option value="pt-PT">Portuguese (Portugal)</Option>
                <Option value="zh-CN">Chinese (Simplified)</Option>
                <Option value="zh-TW">Chinese (Traditional)</Option>
                <Option value="ja-JP">Japanese (Japan)</Option>
                <Option value="ko-KR">Korean (Korea)</Option>
              </Dropdown>
            </Field>

            <Field label="Content to Adapt" required>
              <Textarea
                value={sourceText}
                onChange={(_, data) => setSourceText(data.value)}
                placeholder="Enter content to culturally adapt..."
                rows={8}
              />
            </Field>

            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<LocalLanguage24Regular />}
                onClick={handleAdaptContent}
                disabled={loading || !sourceText}
              >
                {loading ? <Spinner size="tiny" /> : 'Adapt Content'}
              </Button>
              {loading && (
                <Button appearance="secondary" onClick={cancelRequest}>
                  Cancel
                </Button>
              )}
            </div>

            {loading && loadingMessage && (
              <div className={styles.progressContainer}>
                <ProgressBar />
                <Text className={styles.progressText}>{loadingMessage}</Text>
              </div>
            )}
          </div>

          {translatedText && (
            <div className={styles.resultsSection}>
              <Title2>Adapted Content</Title2>
              <div className={styles.resultText}>{translatedText}</div>
            </div>
          )}
        </Card>
      )}
    </div>
  );
};

export default LocalizationPage;
