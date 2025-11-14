/**
 * Batch Translation Queue Component
 * Manages batch translation to multiple languages with queue status
 */

import {
  Card,
  Text,
  Title2,
  Title3,
  Button,
  makeStyles,
  tokens,
  Field,
  Dropdown,
  Option,
  Textarea,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import {
  DocumentMultiple24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import type { LanguageInfoDto, TranslationResultDto } from '../../../types/api-v1';
import { TranslationResult } from './TranslationResult';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  languageRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 2fr',
    gap: tokens.spacingHorizontalL,
  },
  languageTagsContainer: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
    alignItems: 'center',
  },
  queueList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
  queueItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  queueItemContent: {
    flex: 1,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  resultCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface BatchTranslationQueueProps {
  sourceLanguage: string;
  onSourceLanguageChange: (lang: string) => void;
  targetLanguages: string[];
  onTargetLanguagesChange: (langs: string[]) => void;
  sourceText: string;
  onSourceTextChange: (text: string) => void;
  translationMode: string;
  onTranslationModeChange: (mode: string) => void;
  languages: LanguageInfoDto[];
  loadingLanguages: boolean;
  onTranslate: () => void;
  loading: boolean;
  results: Record<string, TranslationResultDto>;
}

export const BatchTranslationQueue: React.FC<BatchTranslationQueueProps> = ({
  sourceLanguage,
  onSourceLanguageChange,
  targetLanguages,
  onTargetLanguagesChange,
  sourceText,
  onSourceTextChange,
  translationMode,
  onTranslationModeChange,
  languages,
  loadingLanguages,
  onTranslate,
  loading,
  results,
}) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string | null>(null);

  const addTargetLanguage = (langCode: string) => {
    if (!targetLanguages.includes(langCode)) {
      onTargetLanguagesChange([...targetLanguages, langCode]);
    }
  };

  const removeTargetLanguage = (langCode: string) => {
    onTargetLanguagesChange(targetLanguages.filter((l) => l !== langCode));
  };

  const getLanguageName = (code: string): string => {
    const lang = languages.find((l) => l.code === code);
    return lang ? `${lang.name} (${lang.nativeName})` : code;
  };

  return (
    <>
      <Card className={styles.card}>
        <Title2>Batch Translation</Title2>
        <Text>Translate content to multiple languages simultaneously</Text>

        <div className={styles.form}>
          <div className={styles.languageRow}>
            <Field label="Source Language">
              <Dropdown
                value={languages.find((l) => l.code === sourceLanguage)?.name || sourceLanguage}
                onOptionSelect={(_, data) => onSourceLanguageChange(data.optionValue as string)}
                disabled={loadingLanguages}
              >
                {languages.map((lang) => (
                  <Option
                    key={lang.code}
                    value={lang.code}
                    text={`${lang.name} (${lang.nativeName})`}
                  >
                    {lang.name} ({lang.nativeName})
                  </Option>
                ))}
              </Dropdown>
            </Field>

            <Field label="Target Languages">
              <Dropdown
                placeholder="Select languages to add..."
                onOptionSelect={(_, data) => {
                  addTargetLanguage(data.optionValue as string);
                }}
                disabled={loadingLanguages}
              >
                {languages
                  .filter(
                    (lang) => lang.code !== sourceLanguage && !targetLanguages.includes(lang.code)
                  )
                  .map((lang) => (
                    <Option
                      key={lang.code}
                      value={lang.code}
                      text={`${lang.name} (${lang.nativeName})`}
                    >
                      {lang.name} ({lang.nativeName})
                    </Option>
                  ))}
              </Dropdown>
            </Field>
          </div>

          {targetLanguages.length > 0 && (
            <div>
              <Text style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}>
                Selected Languages ({targetLanguages.length}):
              </Text>
              <div className={styles.languageTagsContainer}>
                {targetLanguages.map((langCode) => (
                  <Badge
                    key={langCode}
                    appearance="filled"
                    color="brand"
                    style={{ cursor: 'pointer' }}
                    onClick={() => removeTargetLanguage(langCode)}
                  >
                    {getLanguageName(langCode)} ✕
                  </Badge>
                ))}
              </div>
            </div>
          )}

          <Field label="Translation Mode">
            <Dropdown
              value={translationMode}
              onOptionSelect={(_, data) => onTranslationModeChange(data.optionValue as string)}
            >
              <Option value="Literal">Literal - Direct word-for-word translation</Option>
              <Option value="Localized">Localized - Cultural adaptation with idioms</Option>
              <Option value="Transcreation">Transcreation - Creative adaptation</Option>
            </Dropdown>
          </Field>

          <Field label="Content to Translate" required>
            <Textarea
              value={sourceText}
              onChange={(_, data) => onSourceTextChange(data.value)}
              placeholder="Enter text or script to translate to multiple languages..."
              rows={10}
            />
          </Field>

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={loading ? <Spinner size="tiny" /> : <DocumentMultiple24Regular />}
              onClick={onTranslate}
              disabled={loading || !sourceText || targetLanguages.length === 0 || loadingLanguages}
            >
              {loading ? 'Translating...' : `Translate to ${targetLanguages.length} Languages`}
            </Button>
            <Text style={{ fontSize: '13px', color: tokens.colorNeutralForeground3 }}>
              Estimated time: {(targetLanguages.length * 15).toFixed(0)}s -{' '}
              {(targetLanguages.length * 45).toFixed(0)}s
            </Text>
          </div>
        </div>

        {loading && targetLanguages.length > 0 && (
          <>
            <Title3 style={{ marginTop: tokens.spacingVerticalXL }}>Translation Queue</Title3>
            <div className={styles.queueList}>
              {targetLanguages.map((langCode) => (
                <div key={langCode} className={styles.queueItem}>
                  <Spinner size="small" />
                  <div className={styles.queueItemContent}>
                    <Text weight="semibold">{getLanguageName(langCode)}</Text>
                    <Text style={{ fontSize: '13px', color: tokens.colorNeutralForeground3 }}>
                      In progress...
                    </Text>
                  </div>
                  <Badge appearance="outline">Pending</Badge>
                </div>
              ))}
            </div>
          </>
        )}
      </Card>

      {Object.keys(results).length > 0 && (
        <div className={styles.resultsSection}>
          <Card>
            <Title2>Translation Results</Title2>
            <Text style={{ color: tokens.colorNeutralForeground3 }}>
              Completed {Object.keys(results).length} of {targetLanguages.length} translations
            </Text>

            <div className={styles.queueList}>
              {targetLanguages.map((langCode) => {
                const result = results[langCode];
                const isComplete = !!result;

                return (
                  <div
                    key={langCode}
                    className={styles.queueItem}
                    style={{ cursor: isComplete ? 'pointer' : 'default' }}
                    role={isComplete ? 'button' : undefined}
                    tabIndex={isComplete ? 0 : undefined}
                    onClick={() => isComplete && setSelectedTab(langCode)}
                    onKeyDown={(e) => {
                      if (isComplete && (e.key === 'Enter' || e.key === ' ')) {
                        e.preventDefault();
                        setSelectedTab(langCode);
                      }
                    }}
                  >
                    {isComplete ? (
                      <CheckmarkCircle24Regular color={tokens.colorPaletteGreenForeground1} />
                    ) : (
                      <ErrorCircle24Regular color={tokens.colorNeutralForeground4} />
                    )}
                    <div className={styles.queueItemContent}>
                      <Text weight="semibold">{getLanguageName(langCode)}</Text>
                      {isComplete && (
                        <Text style={{ fontSize: '13px', color: tokens.colorNeutralForeground3 }}>
                          Quality: {result.quality.overallScore.toFixed(1)}% •
                          {result.culturalAdaptations.length} adaptations •
                          {result.translationTimeSeconds.toFixed(1)}s
                        </Text>
                      )}
                    </div>
                    <Badge appearance="filled" color={isComplete ? 'success' : 'subtle'}>
                      {isComplete ? 'Complete' : 'Failed'}
                    </Badge>
                  </div>
                );
              })}
            </div>
          </Card>

          {selectedTab && results[selectedTab] && (
            <>
              <Title2>{getLanguageName(selectedTab)} - Detailed Results</Title2>
              <TranslationResult result={results[selectedTab]} />
            </>
          )}
        </div>
      )}
    </>
  );
};
