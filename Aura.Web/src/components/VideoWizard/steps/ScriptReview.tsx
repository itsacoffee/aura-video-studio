import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Spinner,
  Card,
  Field,
  Textarea,
  Badge,
  Tooltip,
  Divider,
} from '@fluentui/react-components';
import {
  Sparkle24Regular,
  ArrowClockwise24Regular,
  DocumentBulletList24Regular,
  Clock24Regular,
  TextGrammarCheckmark24Regular,
  DocumentText24Regular,
  ArrowDownload24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback, useRef } from 'react';
import type { FC } from 'react';
import {
  generateScript,
  updateScene,
  listProviders,
  exportScript,
  type GenerateScriptResponse,
  type ProviderInfoDto,
  type ScriptSceneDto,
} from '../../../services/api/scriptApi';
import type { ScriptData, BriefData, StyleData, StepValidation } from '../types';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  statsBar: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  stat: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  statLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  statValue: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  scenesContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  sceneCard: {
    padding: tokens.spacingVerticalL,
    position: 'relative',
  },
  sceneHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  sceneNumber: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  sceneActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  narrationField: {
    marginBottom: tokens.spacingVerticalM,
  },
  sceneMetadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    textAlign: 'center',
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
  },
  providerSelect: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

interface ScriptReviewProps {
  data: ScriptData;
  briefData: BriefData;
  styleData: StyleData;
  advancedMode: boolean;
  onChange: (data: ScriptData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

export const ScriptReview: FC<ScriptReviewProps> = ({
  briefData,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const [isGenerating, setIsGenerating] = useState(false);
  const [providers, setProviders] = useState<ProviderInfoDto[]>([]);
  const [selectedProvider, setSelectedProvider] = useState<string | undefined>();
  const [generatedScript, setGeneratedScript] = useState<GenerateScriptResponse | null>(null);
  const [editingScenes, setEditingScenes] = useState<Record<number, string>>({});
  const autoSaveTimeouts = useRef<Record<number, ReturnType<typeof setTimeout>>>({});

  useEffect(() => {
    loadProviders();
  }, []);

  useEffect(() => {
    if (generatedScript && generatedScript.scenes.length > 0) {
      onValidationChange({ isValid: true, errors: [] });
    } else {
      onValidationChange({ isValid: false, errors: ['Generate a script to continue'] });
    }
  }, [generatedScript, onValidationChange]);

  const loadProviders = async () => {
    try {
      const response = await listProviders();
      setProviders(response.providers);
      const availableProvider = response.providers.find((p) => p.isAvailable);
      if (availableProvider) {
        setSelectedProvider(availableProvider.name);
      }
    } catch (error) {
      console.error('Failed to load providers:', error);
    }
  };

  const handleGenerateScript = async () => {
    setIsGenerating(true);
    try {
      const response = await generateScript({
        topic: briefData.topic,
        audience: briefData.targetAudience,
        goal: briefData.keyMessage,
        tone: 'Conversational',
        language: 'en',
        aspect: '16:9',
        targetDurationSeconds: briefData.duration,
        pacing: 'Conversational',
        density: 'Balanced',
        style: 'Modern',
        preferredProvider: selectedProvider,
      });

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Script generation failed:', error);
    } finally {
      setIsGenerating(false);
    }
  };

  const handleSceneEdit = useCallback(
    (sceneNumber: number, newNarration: string) => {
      setEditingScenes((prev) => ({
        ...prev,
        [sceneNumber]: newNarration,
      }));

      if (autoSaveTimeouts.current[sceneNumber]) {
        clearTimeout(autoSaveTimeouts.current[sceneNumber]);
      }

      autoSaveTimeouts.current[sceneNumber] = setTimeout(async () => {
        if (!generatedScript) return;

        try {
          await updateScene(generatedScript.scriptId, sceneNumber, {
            narration: newNarration,
          });

          const updatedScenes = generatedScript.scenes.map((scene) =>
            scene.number === sceneNumber ? { ...scene, narration: newNarration } : scene
          );

          setGeneratedScript({
            ...generatedScript,
            scenes: updatedScenes,
          });

          const scriptScenes = updatedScenes.map((scene) => ({
            id: `scene-${scene.number}`,
            text: scene.narration,
            duration: scene.durationSeconds,
            visualDescription: scene.visualPrompt,
            timestamp: updatedScenes
              .slice(0, scene.number - 1)
              .reduce((sum, s) => sum + s.durationSeconds, 0),
          }));

          onChange({
            content: updatedScenes.map((s) => s.narration).join('\n\n'),
            scenes: scriptScenes,
            generatedAt: new Date(),
          });
        } catch (error) {
          console.error('Failed to save scene:', error);
        }
      }, 2000);
    },
    [generatedScript, onChange]
  );

  const handleExportScript = async (format: 'text' | 'markdown') => {
    if (!generatedScript) return;

    try {
      const blob = await exportScript(generatedScript.scriptId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${generatedScript.title.replace(/\s+/g, '_')}_${new Date().toISOString().slice(0, 10)}.${format === 'markdown' ? 'md' : 'txt'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to export script:', error);
    }
  };

  const calculateWordCount = (scenes: ScriptSceneDto[]): number => {
    return scenes.reduce((total, scene) => {
      return total + scene.narration.split(/\s+/).filter((word) => word.length > 0).length;
    }, 0);
  };

  const calculateReadingSpeed = (wordCount: number, durationSeconds: number): number => {
    if (durationSeconds === 0) return 0;
    return Math.round((wordCount / durationSeconds) * 60);
  };

  const isSceneDurationAppropriate = (scene: ScriptSceneDto): 'short' | 'good' | 'long' => {
    const wordCount = scene.narration.split(/\s+/).filter((word) => word.length > 0).length;
    const wpm = calculateReadingSpeed(wordCount, scene.durationSeconds);

    if (wpm < 120) return 'short';
    if (wpm > 180) return 'long';
    return 'good';
  };

  if (isGenerating) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Script Review</Title2>
        </div>
        <div className={styles.loadingContainer}>
          <Spinner size="extra-large" />
          <Title3>Generating your script...</Title3>
          <Text>This may take a few moments. We&apos;re crafting the perfect narrative.</Text>
        </div>
      </div>
    );
  }

  if (!generatedScript) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Script Review</Title2>
          <div className={styles.headerActions}>
            <div className={styles.providerSelect}>
              <Text size={200}>Provider:</Text>
              {providers.length > 0 && (
                <Badge
                  appearance="outline"
                  color={
                    providers.find((p) => p.name === selectedProvider)?.isAvailable
                      ? 'success'
                      : 'warning'
                  }
                >
                  {selectedProvider || 'Auto'}
                </Badge>
              )}
            </div>
            <Button
              appearance="primary"
              icon={<Sparkle24Regular />}
              onClick={handleGenerateScript}
              disabled={!briefData.topic}
            >
              Generate Script
            </Button>
          </div>
        </div>
        <div className={styles.emptyState}>
          <DocumentBulletList24Regular />
          <Title3>No script generated yet</Title3>
          <Text>
            Click &quot;Generate Script&quot; to create an AI-powered script based on your brief.
          </Text>
        </div>
      </div>
    );
  }

  const wordCount = calculateWordCount(generatedScript.scenes);
  const wpm = calculateReadingSpeed(wordCount, generatedScript.totalDurationSeconds);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>{generatedScript.title}</Title2>
        <div className={styles.headerActions}>
          <Tooltip content="Export as text file" relationship="label">
            <Button icon={<DocumentText24Regular />} onClick={() => handleExportScript('text')}>
              Export Text
            </Button>
          </Tooltip>
          <Tooltip content="Export as markdown file" relationship="label">
            <Button
              icon={<ArrowDownload24Regular />}
              onClick={() => handleExportScript('markdown')}
            >
              Export Markdown
            </Button>
          </Tooltip>
          <Tooltip content="Regenerate entire script" relationship="label">
            <Button icon={<ArrowClockwise24Regular />} onClick={handleGenerateScript}>
              Regenerate
            </Button>
          </Tooltip>
        </div>
      </div>

      <div className={styles.statsBar}>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Total Duration</Text>
          <Text className={styles.statValue}>
            {Math.floor(generatedScript.totalDurationSeconds / 60)}:
            {String(Math.floor(generatedScript.totalDurationSeconds % 60)).padStart(2, '0')}
          </Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Word Count</Text>
          <Text className={styles.statValue}>{wordCount}</Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Reading Speed</Text>
          <Text className={styles.statValue}>
            {wpm} WPM
            {wpm < 120 && ' (Slow)'}
            {wpm >= 120 && wpm <= 180 && ' (Good)'}
            {wpm > 180 && ' (Fast)'}
          </Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Scenes</Text>
          <Text className={styles.statValue}>{generatedScript.scenes.length}</Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Provider</Text>
          <Badge appearance="tint" color="brand">
            {generatedScript.metadata.providerName}
          </Badge>
        </div>
      </div>

      <div className={styles.scenesContainer}>
        {generatedScript.scenes.map((scene) => {
          const durationStatus = isSceneDurationAppropriate(scene);
          const currentNarration = editingScenes[scene.number] ?? scene.narration;

          return (
            <Card key={scene.number} className={styles.sceneCard}>
              <div className={styles.sceneHeader}>
                <div className={styles.sceneNumber}>
                  <Badge appearance="filled" color="brand">
                    Scene {scene.number}
                  </Badge>
                  {durationStatus === 'short' && (
                    <Badge appearance="outline" color="warning">
                      Too Short
                    </Badge>
                  )}
                  {durationStatus === 'long' && (
                    <Badge appearance="outline" color="danger">
                      Too Long
                    </Badge>
                  )}
                </div>
              </div>

              <Field className={styles.narrationField} label="Narration">
                <Textarea
                  value={currentNarration}
                  onChange={(e) => handleSceneEdit(scene.number, e.target.value)}
                  rows={4}
                  resize="vertical"
                />
              </Field>

              <div className={styles.sceneMetadata}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <Clock24Regular />
                  <Text>{scene.durationSeconds.toFixed(1)}s</Text>
                </div>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <TextGrammarCheckmark24Regular />
                  <Text>
                    {scene.narration.split(/\s+/).filter((word) => word.length > 0).length} words
                  </Text>
                </div>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <Text>Transition: {scene.transition}</Text>
                </div>
              </div>

              <Divider
                style={{
                  marginTop: tokens.spacingVerticalM,
                  marginBottom: tokens.spacingVerticalM,
                }}
              />

              <Field label="Visual Prompt">
                <Text size={200}>{scene.visualPrompt}</Text>
              </Field>
            </Card>
          );
        })}
      </div>
    </div>
  );
};
