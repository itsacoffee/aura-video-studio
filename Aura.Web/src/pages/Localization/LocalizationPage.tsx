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
} from '@fluentui/react-components';
import {
  LocalLanguage24Regular,
  ArrowSync24Regular,
  TextDescription24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
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
});

type TabValue = 'translate' | 'subtitles' | 'adapt';

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

  const handleTranslate = useCallback(async () => {
    if (!sourceText) {
      setError('Please enter text to translate');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/localization/translate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          text: sourceText,
          sourceLanguage,
          targetLanguage,
        }),
      });

      if (!response.ok) {
        throw new Error('Translation failed');
      }

      const data = await response.json();
      setTranslatedText(data.translatedText || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [sourceText, sourceLanguage, targetLanguage]);

  const handleGenerateSubtitles = useCallback(async () => {
    if (!videoPath) {
      setError('Please provide a video path');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/localization/generate-subtitles', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          videoPath,
          targetLanguage,
        }),
      });

      if (!response.ok) {
        throw new Error('Subtitle generation failed');
      }

      const data = await response.json();
      setSubtitles(data.subtitles || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [videoPath, targetLanguage]);

  const handleAdaptContent = useCallback(async () => {
    if (!sourceText) {
      setError('Please enter content to adapt');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/localization/adapt-content', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: sourceText,
          targetCulture: targetLanguage,
        }),
      });

      if (!response.ok) {
        throw new Error('Content adaptation failed');
      }

      const data = await response.json();
      setTranslatedText(data.adaptedContent || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [sourceText, targetLanguage]);

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
            </div>
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
            </div>
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
