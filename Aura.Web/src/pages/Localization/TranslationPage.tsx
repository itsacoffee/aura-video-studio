/**
 * Translation Page - Complete translation and localization interface
 * Supports 55+ languages with cultural adaptation, quality scoring, and glossary management
 */

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
  Dropdown,
  Option,
  Textarea,
  Input,
} from '@fluentui/react-components';
import {
  LocalLanguage24Regular,
  ArrowSync24Regular,
  Info24Regular,
  DocumentMultiple24Regular,
  Database24Regular,
  Search24Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect, useMemo, useRef } from 'react';
import { ErrorState } from '../../components/Loading';
import {
  translateScript,
  batchTranslate,
  getSupportedLanguages,
} from '../../services/api/localizationApi';
import type {
  TranslateScriptRequest,
  TranslationResultDto,
  BatchTranslateRequest,
  LanguageInfoDto,
} from '../../types/api-v1';
import { BatchTranslationQueue } from './components/BatchTranslationQueue';
import { GlossaryManager } from './components/GlossaryManager';
import { TranslationResult } from './components/TranslationResult';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1600px',
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
  },
  languageRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
  },
  optionsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
    alignItems: 'center',
  },
  statsBar: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalL,
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  statLabel: {
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  statValue: {
    fontSize: '18px',
    fontWeight: tokens.fontWeightSemibold,
  },
  infoText: {
    fontSize: '13px',
    color: tokens.colorNeutralForeground3,
    fontStyle: 'italic',
  },
  autocompleteContainer: {
    position: 'relative',
    width: '100%',
  },
  suggestionsList: {
    position: 'absolute',
    top: '100%',
    left: 0,
    right: 0,
    zIndex: 1000,
    maxHeight: '300px',
    overflowY: 'auto',
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalXS,
    boxShadow: tokens.shadow16,
  },
  suggestionItem: {
    padding: tokens.spacingVerticalS,
    cursor: 'pointer',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground2,
    },
    ':last-child': {
      borderBottom: 'none',
    },
  },
  transcreationSection: {
    marginTop: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorBrandStroke2}`,
    boxShadow: `0 2px 8px ${tokens.colorNeutralShadowAmbient}, 0 1px 2px ${tokens.colorNeutralShadowKey}`,
    animation: 'slideDownFade 0.4s cubic-bezier(0.16, 1, 0.3, 1)',
    position: 'relative',
    overflow: 'hidden',
  },
  transcreationAccentBar: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    height: '3px',
    background: `linear-gradient(90deg, ${tokens.colorBrandForeground1}, ${tokens.colorPalettePurpleForeground1})`,
    opacity: 0.7,
  },
  transcreationHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  transcreationLabel: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  transcreationBadge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground2,
    borderRadius: tokens.borderRadiusCircular,
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
  },
  transcreationHint: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    lineHeight: tokens.lineHeightBase400,
    marginTop: tokens.spacingVerticalXS,
    marginBottom: tokens.spacingVerticalL,
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandStroke1}`,
  },
  transcreationTextarea: {
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    borderWidth: '2px',
    ':focus-within': {
      borderColor: tokens.colorBrandStroke1,
      boxShadow: `0 0 0 4px ${tokens.colorBrandBackground2}`,
      outline: 'none',
    },
  },
  '@keyframes slideDownFade': {
    '0%': {
      opacity: 0,
      transform: 'translateY(-12px) scale(0.98)',
      maxHeight: '0',
      marginTop: 0,
      marginBottom: 0,
      paddingTop: 0,
      paddingBottom: 0,
    },
    '100%': {
      opacity: 1,
      transform: 'translateY(0) scale(1)',
      maxHeight: '600px',
    },
  },
});

type TabValue = 'translate' | 'batch' | 'glossary';

export const TranslationPage: React.FC = () => {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<TabValue>('translate');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [sourceText, setSourceText] = useState('');
  const [sourceLanguage, setSourceLanguage] = useState('en');
  const [targetLanguage, setTargetLanguage] = useState('es');
  const [targetLanguages, setTargetLanguages] = useState<string[]>(['es', 'fr']);
  const [translationMode, setTranslationMode] = useState<string>('Localized');
  const [enableBackTranslation, setEnableBackTranslation] = useState(true);
  const [adjustTimings, setAdjustTimings] = useState(true);
  const [transcreationContext, setTranscreationContext] = useState<string>('');

  // Results
  const [translationResult, setTranslationResult] = useState<TranslationResultDto | null>(null);
  const [batchResults, setBatchResults] = useState<Record<string, TranslationResultDto>>({});

  // Languages
  const [languages, setLanguages] = useState<LanguageInfoDto[]>([]);
  const [loadingLanguages, setLoadingLanguages] = useState(true);
  const [sourceLanguageInput, setSourceLanguageInput] = useState('English');
  const [targetLanguageInput, setTargetLanguageInput] = useState('Spanish');
  const [showSourceSuggestions, setShowSourceSuggestions] = useState(false);
  const [showTargetSuggestions, setShowTargetSuggestions] = useState(false);
  const sourceInputRef = useRef<HTMLInputElement>(null);
  const targetInputRef = useRef<HTMLInputElement>(null);

  // Load supported languages on mount (non-blocking - suggestions only)
  useEffect(() => {
    async function loadLanguages() {
      try {
        const langs = await getSupportedLanguages();
        setLanguages(langs);
        // Auto-populate with first common language if we have suggestions
        if (langs.length > 0) {
          const enLang = langs.find(l => l.code.toLowerCase().startsWith('en'));
          const esLang = langs.find(l => l.code.toLowerCase().startsWith('es'));
          if (enLang) {
            setSourceLanguageInput(enLang.name);
            setSourceLanguage(enLang.code);
          }
          if (esLang) {
            setTargetLanguageInput(esLang.name);
            setTargetLanguage(esLang.code);
          }
        }
      } catch (err) {
        console.warn('Failed to load language suggestions (non-blocking):', err);
        // Don't show error - allow free-form input to work
      } finally {
        setLoadingLanguages(false);
      }
    }
    loadLanguages();
  }, []);

  // Filter language suggestions based on input
  const sourceLanguageSuggestions = useMemo(() => {
    if (!sourceLanguageInput.trim()) return languages.slice(0, 10);
    const query = sourceLanguageInput.toLowerCase();
    return languages.filter(lang => 
      lang.name.toLowerCase().includes(query) ||
      lang.nativeName.toLowerCase().includes(query) ||
      lang.code.toLowerCase().includes(query)
    ).slice(0, 10);
  }, [sourceLanguageInput, languages]);

  const targetLanguageSuggestions = useMemo(() => {
    if (!targetLanguageInput.trim()) return languages.slice(0, 10);
    const query = targetLanguageInput.toLowerCase();
    return languages.filter(lang => 
      lang.name.toLowerCase().includes(query) ||
      lang.nativeName.toLowerCase().includes(query) ||
      lang.code.toLowerCase().includes(query)
    ).slice(0, 10);
  }, [targetLanguageInput, languages]);

  // Preserve full language description - don't normalize to codes
  // This allows users to type descriptive languages like "English (US)" or "Klingon"
  // The LLM will intelligently interpret whatever the user types
  const preserveLanguageDescription = (input: string): string => {
    const trimmed = input.trim();
    if (!trimmed) return '';
    
    // Always return the full input as-is - let the LLM handle interpretation
    // This enables creative and descriptive language inputs like:
    // - "English (US)" vs "English (UK)"
    // - "Klingon" (fictional language)
    // - "Medieval English"
    // - "Slang Spanish" etc.
    return trimmed;
  };

  const handleTranslate = useCallback(async () => {
    if (!sourceText) {
      setError('Please enter text to translate');
      return;
    }

    // Preserve full language descriptions - don't normalize to codes
    // This allows creative inputs like "English (US)" or "Klingon"
    const sourceLangDescription = preserveLanguageDescription(sourceLanguageInput);
    const targetLangDescription = preserveLanguageDescription(targetLanguageInput);

    if (!sourceLangDescription || !targetLangDescription) {
      setError('Please specify source and target languages');
      return;
    }

    setLoading(true);
    setError(null);
    setTranslationResult(null);

    try {
      const request: TranslateScriptRequest = {
        sourceLanguage: sourceLangDescription,
        targetLanguage: targetLangDescription,
        sourceText,
        options: {
          mode: translationMode,
          enableBackTranslation,
          enableQualityScoring: true,
          adjustTimings,
          maxTimingVariance: 0.15,
          preserveNames: true,
          preserveBrands: true,
          adaptMeasurements: true,
          transcreationContext: translationMode === 'Transcreation' && transcreationContext.trim() 
            ? transcreationContext.trim() 
            : undefined,
        },
      };

      const result = await translateScript(request);
      setTranslationResult(result);
      // Store the language descriptions for reference
      setSourceLanguage(sourceLangDescription);
      setTargetLanguage(targetLangDescription);
    } catch (err) {
      // Handle 428 Precondition Required (setup not completed)
      if (
        err &&
        typeof err === 'object' &&
        'response' in err &&
        err.response &&
        typeof err.response === 'object' &&
        'status' in err.response &&
        err.response.status === 428
      ) {
        const responseData = err.response.data as { message?: string; redirectTo?: string } | undefined;
        const message = responseData?.message || 'Please complete the first-run wizard before using this feature.';
        setError(message);
        console.error('Translation blocked - setup not completed:', err);
        // Optionally redirect to onboarding if redirectTo is provided
        if (responseData?.redirectTo) {
          setTimeout(() => {
            window.location.href = responseData.redirectTo;
          }, 2000);
        }
      } else {
        const errorMsg = err instanceof Error ? err.message : 'Translation failed';
        setError(errorMsg);
        console.error('Translation error:', err);
      }
    } finally {
      setLoading(false);
    }
  }, [
    sourceText,
    sourceLanguageInput,
    targetLanguageInput,
    translationMode,
    enableBackTranslation,
    adjustTimings,
    transcreationContext,
    languages,
  ]);

  const handleBatchTranslate = useCallback(async () => {
    if (!sourceText) {
      setError('Please enter text to translate');
      return;
    }

    if (targetLanguages.length === 0) {
      setError('Please select at least one target language');
      return;
    }

    setLoading(true);
    setError(null);
    setBatchResults({});

    try {
      const request: BatchTranslateRequest = {
        sourceLanguage,
        targetLanguages,
        sourceText,
        options: {
          mode: translationMode,
          enableBackTranslation,
          enableQualityScoring: true,
          adjustTimings,
        },
      };

      const result = await batchTranslate(request);
      setBatchResults(result.translations);
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Batch translation failed';
      setError(errorMsg);
      console.error('Batch translation error:', err);
    } finally {
      setLoading(false);
    }
  }, [
    sourceText,
    sourceLanguage,
    targetLanguages,
    translationMode,
    enableBackTranslation,
    adjustTimings,
  ]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <LocalLanguage24Regular className={styles.headerIcon} />
        <div>
          <Title1>Translation & Localization</Title1>
          <Text className={styles.subtitle}>
            AI-powered translation with cultural adaptation - supports any language, dialect, regional variant, or even fictional languages
          </Text>
        </div>
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as TabValue)}
      >
        <Tab value="translate" icon={<ArrowSync24Regular />}>
          Single Translation
        </Tab>
        <Tab value="batch" icon={<DocumentMultiple24Regular />}>
          Batch Translation
        </Tab>
        <Tab value="glossary" icon={<Database24Regular />}>
          Glossary Management
        </Tab>
      </TabList>

      {error && <ErrorState message={error} />}

      {activeTab === 'translate' && (
        <>
          <Card className={styles.toolCard}>
            <Title2>Translate Content</Title2>
            <Text>Translate text with cultural adaptation and quality assurance</Text>

            <div className={styles.form}>
              <div className={styles.languageRow}>
                <Field 
                  label="Source Language"
                  hint="Enter any language description - codes, names, regional variants, or even fictional languages (e.g., 'English (US)', 'Klingon', 'Medieval English'). The LLM will intelligently interpret it."
                >
                  <div className={styles.autocompleteContainer}>
                    <Input
                      ref={sourceInputRef}
                      value={sourceLanguageInput}
                      onChange={(_, data) => {
                        setSourceLanguageInput(data.value);
                        setShowSourceSuggestions(true);
                      }}
                      onFocus={() => setShowSourceSuggestions(true)}
                      onBlur={() => setTimeout(() => setShowSourceSuggestions(false), 200)}
                      placeholder="e.g., English (US), Klingon, Spanish, en, Français..."
                      contentBefore={<Search24Regular />}
                    />
                    {showSourceSuggestions && sourceLanguageSuggestions.length > 0 && (
                      <div className={styles.suggestionsList}>
                        {sourceLanguageSuggestions.map((lang) => (
                          <div
                            key={lang.code}
                            className={styles.suggestionItem}
                            onClick={() => {
                              setSourceLanguageInput(`${lang.name} (${lang.nativeName})`);
                              setSourceLanguage(lang.code);
                              setShowSourceSuggestions(false);
                              sourceInputRef.current?.blur();
                            }}
                            onMouseDown={(e) => e.preventDefault()}
                          >
                            <Text weight="semibold">{lang.name}</Text>
                            <Text size={200} style={{ color: tokens.colorNeutralForeground3, display: 'block' }}>
                              {lang.nativeName} ({lang.code}){lang.isRightToLeft ? ' [RTL]' : ''}
                            </Text>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </Field>

                <Field 
                  label="Target Language"
                  hint="Enter any target language - standard, regional variants, dialects, or even fictional languages. The LLM can handle any language you describe."
                >
                  <div className={styles.autocompleteContainer}>
                    <Input
                      ref={targetInputRef}
                      value={targetLanguageInput}
                      onChange={(_, data) => {
                        setTargetLanguageInput(data.value);
                        setShowTargetSuggestions(true);
                      }}
                      onFocus={() => setShowTargetSuggestions(true)}
                      onBlur={() => setTimeout(() => setShowTargetSuggestions(false), 200)}
                      placeholder="e.g., Klingon, Spanish (Mexico), English (UK), Deutsch..."
                      contentBefore={<Search24Regular />}
                    />
                    {showTargetSuggestions && targetLanguageSuggestions.length > 0 && (
                      <div className={styles.suggestionsList}>
                        {targetLanguageSuggestions.map((lang) => (
                          <div
                            key={lang.code}
                            className={styles.suggestionItem}
                            onClick={() => {
                              setTargetLanguageInput(`${lang.name} (${lang.nativeName})`);
                              setTargetLanguage(lang.code);
                              setShowTargetSuggestions(false);
                              targetInputRef.current?.blur();
                            }}
                            onMouseDown={(e) => e.preventDefault()}
                          >
                            <Text weight="semibold">{lang.name}</Text>
                            <Text size={200} style={{ color: tokens.colorNeutralForeground3, display: 'block' }}>
                              {lang.nativeName} ({lang.code}){lang.isRightToLeft ? ' [RTL]' : ''}
                            </Text>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </Field>
              </div>

              <Field label="Translation Mode">
                <Dropdown
                  value={translationMode}
                  onOptionSelect={(_, data) => setTranslationMode(data.optionValue as string)}
                >
                  <Option value="Literal">Literal - Direct word-for-word translation</Option>
                  <Option value="Localized">Localized - Cultural adaptation with idioms</Option>
                  <Option value="Transcreation">
                    Transcreation - Creative adaptation preserving emotional impact
                  </Option>
                </Dropdown>
              </Field>

              {translationMode === 'Transcreation' && (
                <div className={styles.transcreationSection}>
                  <div className={styles.transcreationAccentBar} />
                  <div className={styles.transcreationHeader}>
                    <Sparkle24Regular style={{ color: tokens.colorBrandForeground1 }} />
                    <Text className={styles.transcreationLabel}>Advanced Transcreation</Text>
                    <span className={styles.transcreationBadge}>
                      <Sparkle24Regular style={{ fontSize: '12px' }} />
                      Creative
                    </span>
                  </div>
                  <Text className={styles.transcreationHint}>
                    Specify the target style, era, format, or audience for creative adaptation. 
                    Perfect for transforming content to match specific time periods, formats, or tones—even within the same language.
                  </Text>
                  <Field 
                    label="Transcreation Instructions"
                    hint="Examples: 'Written as if an American television commercial in 1953', 'Style of a Shakespearean monologue', 'Casual text message between friends', 'Formal corporate announcement from 1980s'"
                  >
                    <Textarea
                      value={transcreationContext}
                      onChange={(_, data) => setTranscreationContext(data.value)}
                      placeholder="e.g., Written as if an American television commercial in 1953 with upbeat, optimistic messaging..."
                      rows={5}
                      className={styles.transcreationTextarea}
                      resize="vertical"
                    />
                  </Field>
                </div>
              )}

              <Field label="Content to Translate" required>
                <Textarea
                  value={sourceText}
                  onChange={(_, data) => setSourceText(data.value)}
                  placeholder="Enter text or script to translate..."
                  rows={10}
                />
              </Field>

              <div className={styles.optionsGrid}>
                <Field label="Back-Translation QA">
                  <Dropdown
                    value={enableBackTranslation ? 'enabled' : 'disabled'}
                    onOptionSelect={(_, data) =>
                      setEnableBackTranslation(data.optionValue === 'enabled')
                    }
                  >
                    <Option value="enabled">Enabled (recommended)</Option>
                    <Option value="disabled">Disabled (faster)</Option>
                  </Dropdown>
                </Field>

                <Field label="Timing Adjustment">
                  <Dropdown
                    value={adjustTimings ? 'enabled' : 'disabled'}
                    onOptionSelect={(_, data) => setAdjustTimings(data.optionValue === 'enabled')}
                  >
                    <Option value="enabled">Enabled (for video)</Option>
                    <Option value="disabled">Disabled (text only)</Option>
                  </Dropdown>
                </Field>
              </div>

              <Text className={styles.infoText}>
                <Info24Regular /> Translation includes: Quality scoring, cultural adaptation
                analysis, idiom replacement, and visual localization recommendations. The AI supports
                any language, dialect, regional variant, or even fictional languages - simply type the full
                language description (e.g., "English (US)" to "Klingon").
              </Text>

              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  icon={loading ? <Spinner size="tiny" /> : <ArrowSync24Regular />}
                  onClick={handleTranslate}
                  disabled={loading || !sourceText || !sourceLanguageInput.trim() || !targetLanguageInput.trim()}
                >
                  {loading ? 'Translating...' : 'Translate'}
                </Button>
              </div>
            </div>
          </Card>

          {translationResult && <TranslationResult result={translationResult} />}
        </>
      )}

      {activeTab === 'batch' && (
        <BatchTranslationQueue
          sourceLanguage={sourceLanguage}
          onSourceLanguageChange={setSourceLanguage}
          targetLanguages={targetLanguages}
          onTargetLanguagesChange={setTargetLanguages}
          sourceText={sourceText}
          onSourceTextChange={setSourceText}
          translationMode={translationMode}
          onTranslationModeChange={setTranslationMode}
          languages={languages}
          loadingLanguages={loadingLanguages}
          onTranslate={handleBatchTranslate}
          loading={loading}
          results={batchResults}
        />
      )}

      {activeTab === 'glossary' && <GlossaryManager />}
    </div>
  );
};

export default TranslationPage;
