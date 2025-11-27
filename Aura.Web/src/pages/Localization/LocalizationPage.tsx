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
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  MessageBarActions,
} from '@fluentui/react-components';
import {
  LocalLanguage24Regular,
  ArrowSync24Regular,
  TextDescription24Regular,
  ArrowClockwise24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useRef, useEffect } from 'react';
import { getOperationTimeout } from '../../config/timeouts';
import {
  parseLocalizationError,
  getUserFriendlyMessage,
  getErrorGuidance,
  getErrorSeverity,
  type ParsedLocalizationError,
} from '../../utils/localizationErrors';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    paddingLeft: tokens.spacingHorizontalXXL,
    paddingRight: tokens.spacingHorizontalXXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalL,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
    flexShrink: 0,
    marginTop: tokens.spacingVerticalXS,
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground2,
    lineHeight: '1.5',
    maxWidth: '600px',
  },
  tabs: {
    marginBottom: tokens.spacingVerticalXL,
  },
  toolCard: {
    padding: tokens.spacingVerticalXXL,
    paddingLeft: tokens.spacingHorizontalXXL,
    paddingRight: tokens.spacingHorizontalXXL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalS,
  },
  sectionDescription: {
    color: tokens.colorNeutralForeground2,
    lineHeight: '1.6',
    marginBottom: tokens.spacingVerticalXL,
    maxWidth: '650px',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
    maxWidth: '700px',
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  fieldHint: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    lineHeight: '1.5',
    marginTop: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalXL,
    paddingTop: tokens.spacingVerticalM,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalXXL,
    padding: tokens.spacingVerticalL,
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  resultText: {
    padding: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    fontFamily: 'monospace',
    fontSize: '14px',
    lineHeight: '1.6',
    whiteSpace: 'pre-wrap',
  },
  progressContainer: {
    marginTop: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  progressText: {
    marginTop: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
    fontSize: '12px',
    lineHeight: '1.5',
  },
  errorContainer: {
    marginBottom: tokens.spacingVerticalXL,
  },
  errorGuidance: {
    marginTop: tokens.spacingVerticalM,
    fontSize: '13px',
    lineHeight: '1.6',
    color: tokens.colorNeutralForeground2,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalXS,
  },
  errorActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalM,
  },
  suggestedAction: {
    fontSize: '12px',
    lineHeight: '1.5',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
});

type TabValue = 'translate' | 'subtitles' | 'adapt';

export const LocalizationPage: React.FC = () => {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<TabValue>('translate');
  const [loading, setLoading] = useState(false);
  const [parsedError, setParsedError] = useState<ParsedLocalizationError | null>(null);
  const [sourceText, setSourceText] = useState('');
  const [sourceLanguage, setSourceLanguage] = useState('en');
  const [targetLanguage, setTargetLanguage] = useState('es');
  const [translatedText, setTranslatedText] = useState('');
  const [videoPath, setVideoPath] = useState('');
  const [subtitles, setSubtitles] = useState('');
  const [loadingMessage, setLoadingMessage] = useState('');
  const [lastOperation, setLastOperation] = useState<'translate' | 'analyze' | null>(null);

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

  // Clear error state
  const clearError = useCallback(() => {
    setParsedError(null);
  }, []);

  // Handle request errors with detailed parsing
  const handleRequestError = useCallback(
    async (response: Response): Promise<ParsedLocalizationError> => {
      try {
        const errorData = await response.json();
        return parseLocalizationError(errorData);
      } catch {
        return parseLocalizationError({
          title: 'Request Failed',
          status: response.status,
          detail: `Request failed with status ${response.status}`,
        });
      }
    },
    []
  );

  const handleTranslate = useCallback(async () => {
    if (!sourceText) {
      setParsedError(
        parseLocalizationError({
          title: 'Empty Content',
          status: 400,
          detail: 'Please enter text to translate',
          errorCode: 'EMPTY_CONTENT',
        })
      );
      return;
    }

    // Cancel any previous request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }

    setLoading(true);
    setParsedError(null);
    setLoadingMessage('Translating...');
    setLastOperation('translate');

    // Create new AbortController for this request
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    // Set up timeout
    const timeoutMs = getOperationTimeout('localization');
    const timeoutId = setTimeout(() => {
      abortController.abort();
    }, timeoutMs);

    try {
      const response = await fetch('/api/localization/translate/simple', {
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
        const error = await handleRequestError(response);
        setParsedError(error);
        return;
      }

      const data = await response.json();
      setTranslatedText(data.translatedText || '');
    } catch (err: unknown) {
      const error = parseLocalizationError(err);
      setParsedError(error);
    } finally {
      clearTimeout(timeoutId);
      setLoading(false);
      setLoadingMessage('');
      abortControllerRef.current = null;
    }
  }, [sourceText, sourceLanguage, targetLanguage, handleRequestError]);

  const handleGenerateSubtitles = useCallback(async () => {
    if (!videoPath) {
      setParsedError(
        parseLocalizationError({
          title: 'Missing Video Path',
          status: 400,
          detail: 'Please provide a video path',
          errorCode: 'EMPTY_CONTENT',
        })
      );
      return;
    }

    setLoading(true);
    setParsedError(null);

    try {
      // Note: The generate-subtitles endpoint is not implemented.
      // For subtitle generation, use the translate-and-plan-ssml endpoint
      // which provides a full translation and SSML planning workflow.
      setParsedError(
        parseLocalizationError({
          title: 'Feature Not Available',
          status: 501,
          detail:
            'Subtitle generation requires video transcription. Please use the Create workflow to generate subtitles from your video script.',
          errorCode: 'NOT_IMPLEMENTED',
        })
      );
      setSubtitles('');
    } catch (err: unknown) {
      setParsedError(parseLocalizationError(err));
    } finally {
      setLoading(false);
    }
  }, [videoPath]);

  const handleAdaptContent = useCallback(async () => {
    if (!sourceText) {
      setParsedError(
        parseLocalizationError({
          title: 'Empty Content',
          status: 400,
          detail: 'Please enter content to adapt',
          errorCode: 'EMPTY_CONTENT',
        })
      );
      return;
    }

    // Cancel any previous request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }

    setLoading(true);
    setParsedError(null);
    setLoadingMessage('Analyzing cultural context...');
    setLastOperation('analyze');

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
        const error = await handleRequestError(response);
        setParsedError(error);
        return;
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
      const error = parseLocalizationError(err);
      setParsedError(error);
    } finally {
      clearTimeout(timeoutId);
      setLoading(false);
      setLoadingMessage('');
      abortControllerRef.current = null;
    }
  }, [sourceText, targetLanguage, handleRequestError]);

  // Retry the last failed operation
  const handleRetry = useCallback(() => {
    clearError();
    if (lastOperation === 'translate') {
      handleTranslate();
    } else if (lastOperation === 'analyze') {
      handleAdaptContent();
    }
  }, [lastOperation, handleTranslate, handleAdaptContent, clearError]);

  // Get the message bar intent based on error severity
  const getMessageBarIntent = useCallback((errorCode: string): 'error' | 'warning' | 'info' => {
    const severity = getErrorSeverity(errorCode);
    switch (severity) {
      case 'warning':
      case 'info':
        return 'warning';
      case 'critical':
      case 'error':
      default:
        return 'error';
    }
  }, []);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <LocalLanguage24Regular className={styles.headerIcon} />
        <div className={styles.headerContent}>
          <Title1>Localization & Translation</Title1>
          <Text className={styles.subtitle}>
            Translate content, generate subtitles, and adapt your videos for different cultures and
            languages. Use these tools to reach a global audience with properly localized content.
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

      {parsedError && (
        <div className={styles.errorContainer}>
          <MessageBar intent={getMessageBarIntent(parsedError.errorCode)}>
            <MessageBarBody>
              <MessageBarTitle>{parsedError.title}</MessageBarTitle>
              {getUserFriendlyMessage(parsedError)}
              {getErrorGuidance(parsedError.errorCode) && (
                <div className={styles.errorGuidance}>
                  <Info24Regular style={{ marginRight: '4px', verticalAlign: 'middle' }} />
                  {getErrorGuidance(parsedError.errorCode)}
                </div>
              )}
              {parsedError.suggestedActions.length > 0 && (
                <div className={styles.errorActions}>
                  {parsedError.suggestedActions.slice(0, 3).map((action, idx) => (
                    <span key={idx} className={styles.suggestedAction}>
                      {action}
                    </span>
                  ))}
                </div>
              )}
            </MessageBarBody>
            <MessageBarActions>
              {parsedError.isRetryable && lastOperation && (
                <Button icon={<ArrowClockwise24Regular />} onClick={handleRetry} size="small">
                  Retry
                </Button>
              )}
              <Button onClick={clearError} size="small" appearance="subtle">
                Dismiss
              </Button>
            </MessageBarActions>
          </MessageBar>
        </div>
      )}

      {activeTab === 'translate' && (
        <Card className={styles.toolCard}>
          <Title2 className={styles.sectionTitle}>Text Translation</Title2>
          <Text className={styles.sectionDescription}>
            Translate text content between languages with AI-powered translation. Select your source
            and target languages below, then enter the text you want to translate.
          </Text>

          <div className={styles.form}>
            <div className={styles.fieldGroup}>
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
              <Text className={styles.fieldHint}>
                The language of your original content
              </Text>
            </div>

            <div className={styles.fieldGroup}>
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
              <Text className={styles.fieldHint}>
                The language to translate your content into
              </Text>
            </div>

            <div className={styles.fieldGroup}>
              <Field label="Text to Translate" required>
                <Textarea
                  value={sourceText}
                  onChange={(_, data) => setSourceText(data.value)}
                  placeholder="Enter text to translate..."
                  rows={8}
                />
              </Field>
              <Text className={styles.fieldHint}>
                Enter the text you want to translate. Longer texts may take more time to process.
              </Text>
            </div>

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
              <Title2 className={styles.sectionTitle}>Translation Result</Title2>
              <div className={styles.resultText}>{translatedText}</div>
            </div>
          )}
        </Card>
      )}

      {activeTab === 'subtitles' && (
        <Card className={styles.toolCard}>
          <Title2 className={styles.sectionTitle}>Subtitle Generation</Title2>
          <Text className={styles.sectionDescription}>
            Generate translated subtitles for your video content. Provide the path to your video
            file and select the target language for the subtitles.
          </Text>

          <div className={styles.form}>
            <div className={styles.fieldGroup}>
              <Field label="Video Path" required>
                <Input
                  value={videoPath}
                  onChange={(_, data) => setVideoPath(data.value)}
                  placeholder="/path/to/video.mp4"
                />
              </Field>
              <Text className={styles.fieldHint}>
                Enter the full path to your video file (e.g., C:\Videos\my-video.mp4)
              </Text>
            </div>

            <div className={styles.fieldGroup}>
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
              <Text className={styles.fieldHint}>
                The language for your generated subtitles
              </Text>
            </div>

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
              <Title2 className={styles.sectionTitle}>Generated Subtitles</Title2>
              <div className={styles.resultText}>{subtitles}</div>
            </div>
          )}
        </Card>
      )}

      {activeTab === 'adapt' && (
        <Card className={styles.toolCard}>
          <Title2 className={styles.sectionTitle}>Cultural Adaptation</Title2>
          <Text className={styles.sectionDescription}>
            Adapt your content for cultural context and local references. This tool analyzes your
            text and provides recommendations for making it culturally appropriate for your target
            audience.
          </Text>

          <div className={styles.form}>
            <div className={styles.fieldGroup}>
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
              <Text className={styles.fieldHint}>
                Select the specific culture and region for content adaptation
              </Text>
            </div>

            <div className={styles.fieldGroup}>
              <Field label="Content to Adapt" required>
                <Textarea
                  value={sourceText}
                  onChange={(_, data) => setSourceText(data.value)}
                  placeholder="Enter content to culturally adapt..."
                  rows={8}
                />
              </Field>
              <Text className={styles.fieldHint}>
                Enter your content for cultural analysis. The tool will identify areas that may
                need adaptation for your target audience.
              </Text>
            </div>

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
              <Title2 className={styles.sectionTitle}>Adapted Content</Title2>
              <div className={styles.resultText}>{translatedText}</div>
            </div>
          )}
        </Card>
      )}
    </div>
  );
};

export default LocalizationPage;
