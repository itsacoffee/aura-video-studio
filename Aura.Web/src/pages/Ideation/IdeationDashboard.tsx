import { makeStyles, tokens, Text, Button, Input } from '@fluentui/react-components';
import { LightbulbRegular, LightbulbFilamentRegular } from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { BrainstormInput, BrainstormOptions } from '../../components/ideation/BrainstormInput';
import { ConceptCard } from '../../components/ideation/ConceptCard';
import { SkeletonCard, ErrorState } from '../../components/Loading';
import {
  ideationService,
  type ConceptIdea,
  type BrainstormRequest,
} from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
    maxWidth: '1600px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalS,
  },
  icon: {
    fontSize: '28px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    maxWidth: '800px',
    fontSize: tokens.fontSizeBase200,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  conceptsSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  conceptsHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalM,
  },
  conceptsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: tokens.spacingHorizontalM,
    '@media (max-width: 1000px)': {
      gridTemplateColumns: '1fr',
    },
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: '48px',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
    justifyContent: 'flex-end',
  },
  hotkeyEditor: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    minWidth: '200px',
  },
  hotkeyInputRow: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  hotkeyHint: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  hotkeyHelper: {
    color: tokens.colorNeutralForeground4,
    fontSize: tokens.fontSizeBase100,
  },
  hotkeyReset: {
    padding: 0,
    height: 'auto',
    fontSize: tokens.fontSizeBase200,
  },
  conceptCount: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  topicLabel: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
  },
});

interface HotkeyConfig {
  key?: string;
  code: string;
  ctrlKey: boolean;
  altKey: boolean;
  shiftKey: boolean;
  metaKey: boolean;
}

const REFRESH_HOTKEY_STORAGE_KEY = 'ideation-refresh-hotkey';
const IDEA_COUNT_STORAGE_KEY = 'ideation-idea-count';
const DEFAULT_HOTKEY: HotkeyConfig = {
  key: ' ',
  code: 'Space',
  ctrlKey: true,
  altKey: false,
  shiftKey: false,
  metaKey: false,
};

const clampIdeaCount = (value: number) => Math.min(9, Math.max(3, value));

export const IdeationDashboard: React.FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const [concepts, setConcepts] = useState<ConceptIdea[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [originalTopic, setOriginalTopic] = useState<string>('');
  const [originalOptions, setOriginalOptions] = useState<BrainstormOptions>({});
  const [ideaCount, setIdeaCount] = useState<number>(6);
  const [refreshHotkey, setRefreshHotkey] = useState<HotkeyConfig>(DEFAULT_HOTKEY);
  const [isCapturingHotkey, setIsCapturingHotkey] = useState(false);

  const formatHotkeyLabel = useCallback((config: HotkeyConfig) => {
    const segments: string[] = [];
    if (config.ctrlKey) segments.push('Ctrl');
    if (config.altKey) segments.push('Alt');
    if (config.shiftKey) segments.push('Shift');
    if (config.metaKey) segments.push('Meta');

    const primary =
      config.code === 'Space'
        ? 'Space'
        : config.key && config.key.length === 1
          ? config.key.toUpperCase()
          : config.code.replace(/^(Key|Digit)/, '');

    segments.push(primary);
    return segments.join(' + ');
  }, []);

  const matchesHotkey = useCallback((event: KeyboardEvent, config: HotkeyConfig) => {
    return (
      event.code === config.code &&
      event.ctrlKey === config.ctrlKey &&
      event.altKey === config.altKey &&
      event.shiftKey === config.shiftKey &&
      event.metaKey === config.metaKey
    );
  }, []);

  const isModifierOnly = (event: React.KeyboardEvent<HTMLInputElement>) => {
    return (
      event.key === 'Shift' ||
      event.key === 'Control' ||
      event.key === 'Alt' ||
      event.key === 'Meta'
    );
  };

  const handleBrainstorm = useCallback(
    async (topic: string, options: BrainstormOptions) => {
      setLoading(true);
      setError(null);
      setOriginalTopic(topic);

      const normalizedOptions: BrainstormOptions = {
        ...options,
        conceptCount: clampIdeaCount(options.conceptCount ?? ideaCount),
      };
      setOriginalOptions(normalizedOptions);

      try {
        const request: BrainstormRequest = {
          topic,
          ...normalizedOptions,
        };

        const response = await ideationService.brainstorm(request);
        setConcepts(response.concepts);
        setError(null);
      } catch (err) {
        console.error('Brainstorm error:', err);

        let errorMessage = 'Failed to generate concepts';
        let suggestions: string[] = [];

        if (err instanceof Error) {
          errorMessage = err.message;
        }

        // Try to extract suggestions from API response
        if (typeof err === 'object' && err !== null) {
          const apiError = err as {
            response?: { data?: { suggestions?: string[]; error?: string } };
          };
          if (apiError.response?.data?.suggestions) {
            suggestions = apiError.response.data.suggestions;
          }
          if (apiError.response?.data?.error) {
            errorMessage = apiError.response.data.error;
          }
        }

        // Build comprehensive error message
        const fullError =
          suggestions.length > 0
            ? `${errorMessage}\n\nSuggestions:\n${suggestions.map((s, i) => `${i + 1}. ${s}`).join('\n')}`
            : errorMessage;

        setError(fullError);
      } finally {
        setLoading(false);
      }
    },
    [ideaCount]
  );

  const handleSelectConcept = (concept: ConceptIdea) => {
    navigate(`/ideation/concept/${concept.conceptId}`, { state: { concept } });
  };

  const handleExpandConcept = (concept: ConceptIdea) => {
    navigate(`/ideation/concept/${concept.conceptId}`, { state: { concept } });
  };

  const handleRefresh = useCallback(() => {
    if (originalTopic) {
      const latestOptions: BrainstormOptions = {
        ...originalOptions,
        conceptCount: clampIdeaCount(originalOptions.conceptCount ?? ideaCount),
      };
      void handleBrainstorm(originalTopic, latestOptions);
    }
  }, [handleBrainstorm, ideaCount, originalOptions, originalTopic]);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    const storedCount = Number(window.localStorage.getItem(IDEA_COUNT_STORAGE_KEY));
    if (!Number.isNaN(storedCount)) {
      setIdeaCount(clampIdeaCount(storedCount));
    }
  }, []);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    window.localStorage.setItem(IDEA_COUNT_STORAGE_KEY, ideaCount.toString());
  }, [ideaCount]);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    try {
      const storedHotkey = window.localStorage.getItem(REFRESH_HOTKEY_STORAGE_KEY);
      if (storedHotkey) {
        const parsed = JSON.parse(storedHotkey) as HotkeyConfig;
        if (parsed && typeof parsed.code === 'string') {
          setRefreshHotkey({
            key: parsed.key ?? parsed.code,
            code: parsed.code,
            ctrlKey: Boolean(parsed.ctrlKey),
            altKey: Boolean(parsed.altKey),
            shiftKey: Boolean(parsed.shiftKey),
            metaKey: Boolean(parsed.metaKey),
          });
        }
      }
    } catch (err) {
      console.warn('Failed to restore ideation refresh hotkey', err);
    }
  }, []);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    window.localStorage.setItem(REFRESH_HOTKEY_STORAGE_KEY, JSON.stringify(refreshHotkey));
  }, [refreshHotkey]);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    const listener = (event: KeyboardEvent) => {
      if (isCapturingHotkey) {
        return;
      }

      if (matchesHotkey(event, refreshHotkey)) {
        event.preventDefault();
        handleRefresh();
      }
    };

    window.addEventListener('keydown', listener);
    return () => window.removeEventListener('keydown', listener);
  }, [handleRefresh, matchesHotkey, refreshHotkey, isCapturingHotkey]);

  const hotkeyLabel = formatHotkeyLabel(refreshHotkey);

  const handleHotkeyKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    event.preventDefault();
    event.stopPropagation();

    if (!isCapturingHotkey) {
      return;
    }

    if (event.key === 'Escape') {
      setIsCapturingHotkey(false);
      return;
    }

    if (isModifierOnly(event)) {
      return;
    }

    setRefreshHotkey({
      key: event.key,
      code: event.code,
      ctrlKey: event.ctrlKey,
      altKey: event.altKey,
      shiftKey: event.shiftKey,
      metaKey: event.metaKey,
    });
    setIsCapturingHotkey(false);
  };

  const resetHotkey = () => {
    setRefreshHotkey(DEFAULT_HOTKEY);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <LightbulbRegular className={styles.icon} />
          <Text size={800} weight="bold">
            AI Ideation & Brainstorming
          </Text>
        </div>
        <Text className={styles.subtitle} size={400}>
          Transform your ideas into fully-fleshed video concepts. Enter a topic and let AI generate
          creative variations with different storytelling approaches, target audiences, and
          production angles.
        </Text>
        <div style={{ marginTop: tokens.spacingVerticalM }}>
          <Button appearance="outline" onClick={() => navigate('/ideation/brief-builder')}>
            Start with Brief Builder
          </Button>
        </div>
      </div>

      <div className={styles.content}>
        <BrainstormInput
          onBrainstorm={handleBrainstorm}
          loading={loading}
          ideaCount={ideaCount}
          onIdeaCountChange={(value) => setIdeaCount(clampIdeaCount(value))}
        />

        {error && (
          <ErrorState
            title="Failed to generate concepts"
            message={error}
            onRetry={handleRefresh}
            withCard={true}
          />
        )}

        {concepts.length > 0 && (
          <div className={styles.conceptsSection}>
            <div className={styles.conceptsHeader}>
              <div>
                <Text className={styles.conceptCount}>{concepts.length} Concepts Generated</Text>
                <Text className={styles.topicLabel}>for &quot;{originalTopic}&quot;</Text>
              </div>
              <div className={styles.headerActions}>
                <Button appearance="subtle" onClick={handleRefresh}>
                  Generate More
                </Button>
                <div className={styles.hotkeyEditor}>
                  <Text size={200} weight="semibold">
                    Refresh shortcut
                  </Text>
                  <div className={styles.hotkeyInputRow}>
                    <Input
                      readOnly
                      appearance={isCapturingHotkey ? 'filled-darker' : 'outline'}
                      value={isCapturingHotkey ? 'Press new keys…' : hotkeyLabel}
                      onFocus={() => setIsCapturingHotkey(true)}
                      onBlur={() => setIsCapturingHotkey(false)}
                      onKeyDown={handleHotkeyKeyDown}
                      aria-label="Refresh ideas hotkey"
                    />
                    <Button
                      appearance="subtle"
                      className={styles.hotkeyReset}
                      onClick={resetHotkey}
                    >
                      Reset
                    </Button>
                  </div>
                  <Text className={styles.hotkeyHint}>{hotkeyLabel} to refresh</Text>
                </div>
              </div>
            </div>

            <div className={styles.conceptsGrid}>
              {concepts.map((concept) => (
                <ConceptCard
                  key={concept.conceptId}
                  concept={concept}
                  onSelect={handleSelectConcept}
                  onExpand={handleExpandConcept}
                  onUseForVideo={handleSelectConcept}
                />
              ))}
            </div>
          </div>
        )}

        {!loading && concepts.length === 0 && !error && (
          <div className={styles.emptyState}>
            <LightbulbFilamentRegular className={styles.emptyIcon} />
            <Text size={500} weight="semibold">
              Ready to brainstorm?
            </Text>
            <Text>Enter a video topic above to get started with AI-powered concept generation</Text>
          </div>
        )}

        {loading && (
          <div className={styles.conceptsGrid}>
            {Array.from({ length: 6 }).map((_, i) => (
              <SkeletonCard key={`skeleton-${i}`} hasImage={false} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
