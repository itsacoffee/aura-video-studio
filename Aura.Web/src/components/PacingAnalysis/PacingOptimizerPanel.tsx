/**
 * Pacing Optimizer Panel Component
 * Main container for pacing analysis visualization and suggestion management
 */

import {
  Card,
  Button,
  Spinner,
  Badge,
  makeStyles,
  tokens,
  Title1,
  Title3,
  Body1,
  Caption1,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Drawer,
  DrawerHeader,
  DrawerHeaderTitle,
  DrawerBody,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
} from '@fluentui/react-components';
import {
  ChartMultiple24Regular,
  Checkmark24Regular,
  ArrowClockwise24Regular,
  Settings24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { usePacingAnalysis } from '../../hooks/usePacingAnalysis';
import { pacingAnalytics } from '../../services/analytics';
import { getPlatformPresets } from '../../services/pacingService';
import { Brief } from '../../types';
import { Scene, PacingSettings as PacingSettingsType, PlatformPreset } from '../../types/pacing';
import { AttentionCurveChart } from './AttentionCurveChart';
import { PacingSettings } from './PacingSettings';
import { SceneSuggestionCard } from './SceneSuggestionCard';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: tokens.spacingVerticalM,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerRight: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  scoreCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
  },
  scoreValue: {
    fontSize: '48px',
    fontWeight: 600,
    color: tokens.colorBrandForeground1,
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  metricCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  suggestionsContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalL,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface PacingOptimizerPanelProps {
  script: string;
  scenes: Scene[];
  brief: Brief;
  onScenesUpdated?: (scenes: Scene[]) => void;
  onClose?: () => void;
}

const DEFAULT_SETTINGS: PacingSettingsType = {
  enabled: true,
  optimizationLevel: 'Moderate',
  targetPlatform: 'YouTube',
  minConfidence: 60,
  autoApply: false,
};

export const PacingOptimizerPanel: React.FC<PacingOptimizerPanelProps> = ({
  script,
  scenes,
  brief,
  onScenesUpdated,
  onClose,
}) => {
  const styles = useStyles();
  const { loading, error, data, analyzePacing, reanalyzePacing } = usePacingAnalysis();

  const [settings, setSettings] = useState<PacingSettingsType>(DEFAULT_SETTINGS);
  const [platforms, setPlatforms] = useState<PlatformPreset[]>([]);
  const [showSettings, setShowSettings] = useState(false);
  const [appliedSuggestions, setAppliedSuggestions] = useState<Set<number>>(new Set());
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [pendingSuggestions, setPendingSuggestions] = useState<number[]>([]);

  // Fetch platform presets on mount
  useEffect(() => {
    const fetchPresets = async () => {
      try {
        const response = await getPlatformPresets();
        setPlatforms(response.platforms);
      } catch (err) {
        console.error('Failed to fetch platform presets:', err);
      }
    };
    fetchPresets();
  }, []);

  // Auto-analyze on mount if enabled
  useEffect(() => {
    if (settings.enabled && script && scenes.length > 0 && !data && !loading) {
      handleAnalyze();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleAnalyze = useCallback(async () => {
    const targetDuration = scenes.reduce((sum, scene) => sum + (scene.duration || 0), 0);

    const tempCorrelationId = `temp-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;

    // Track analysis start
    pacingAnalytics.analysisStarted(scenes.length, settings.targetPlatform, tempCorrelationId);

    try {
      await analyzePacing({
        script,
        scenes,
        targetPlatform: settings.targetPlatform,
        targetDuration,
        brief,
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      pacingAnalytics.analysisFailed(errorMessage, tempCorrelationId);
      throw error;
    }
  }, [script, scenes, settings.targetPlatform, brief, analyzePacing]);

  // Track analysis completion when data changes
  useEffect(() => {
    if (data && !loading && !error) {
      pacingAnalytics.analysisCompleted(
        data.analysisId,
        data.overallScore,
        data.suggestions.length,
        data.estimatedRetention,
        data.correlationId
      );
    }
  }, [data, loading, error]);

  const handleReanalyze = useCallback(async () => {
    const optimizationLevelMap: Record<string, 'Low' | 'Medium' | 'High'> = {
      Conservative: 'Low',
      Moderate: 'Medium',
      Aggressive: 'High',
    };

    if (data?.analysisId) {
      pacingAnalytics.reanalysisTriggered(
        data.analysisId,
        settings.optimizationLevel,
        settings.targetPlatform,
        data.correlationId
      );
    }

    await reanalyzePacing({
      optimizationLevel: optimizationLevelMap[settings.optimizationLevel],
      targetPlatform: settings.targetPlatform,
    });
  }, [data, settings, reanalyzePacing]);

  const handleAcceptSuggestion = (sceneIndex: number) => {
    const suggestion = data?.suggestions.find((s) => s.sceneIndex === sceneIndex);
    if (!suggestion) return;

    setAppliedSuggestions((prev) => new Set(prev).add(sceneIndex));
    setSuccessMessage(`Applied pacing suggestion for Scene ${sceneIndex + 1}`);

    // Track suggestion application
    if (data?.analysisId) {
      pacingAnalytics.suggestionApplied(sceneIndex, data.analysisId);
    }

    // Clear success message after 3 seconds
    setTimeout(() => setSuccessMessage(null), 3000);

    // Notify parent component if callback provided
    if (onScenesUpdated) {
      // This would update the actual scene durations in the parent
      onScenesUpdated(scenes);
    }
  };

  const handleRejectSuggestion = (sceneIndex: number) => {
    // Track suggestion rejection
    if (data?.analysisId) {
      pacingAnalytics.suggestionRejected(sceneIndex, data.analysisId);
    }

    // Just remove from applied if it was applied
    setAppliedSuggestions((prev) => {
      const next = new Set(prev);
      next.delete(sceneIndex);
      return next;
    });
  };

  const handleApplyAll = () => {
    if (!data?.suggestions) return;

    const highConfidenceSuggestions = data.suggestions.filter(
      (s) => s.confidence >= settings.minConfidence
    );

    // Store pending suggestions and show confirmation dialog
    setPendingSuggestions(highConfidenceSuggestions.map((s) => s.sceneIndex));
    setShowConfirmDialog(true);
  };

  const handleConfirmApply = () => {
    const newApplied = new Set(pendingSuggestions);
    setAppliedSuggestions(newApplied);
    setSuccessMessage(`Applied ${newApplied.size} pacing suggestions`);

    // Track bulk application
    if (data?.analysisId) {
      pacingAnalytics.allSuggestionsApplied(newApplied.size, data.analysisId);
    }

    setTimeout(() => setSuccessMessage(null), 3000);

    if (onScenesUpdated) {
      onScenesUpdated(scenes);
    }

    setShowConfirmDialog(false);
    setPendingSuggestions([]);
  };

  const handleCancelApply = () => {
    setShowConfirmDialog(false);
    setPendingSuggestions([]);
  };

  const handleSettingsSave = () => {
    setShowSettings(false);
    setSuccessMessage('Settings saved successfully');

    // Track settings change
    pacingAnalytics.settingsChanged(
      settings.optimizationLevel,
      settings.targetPlatform,
      settings.minConfidence
    );

    setTimeout(() => setSuccessMessage(null), 3000);
  };

  const getScoreColor = (score: number) => {
    if (score >= 80) return tokens.colorPaletteGreenForeground1;
    if (score >= 60) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const getScoreBadge = (score: number) => {
    if (score >= 80)
      return (
        <Badge appearance="filled" color="success">
          Excellent
        </Badge>
      );
    if (score >= 60)
      return (
        <Badge appearance="filled" color="warning">
          Good
        </Badge>
      );
    return (
      <Badge appearance="filled" color="danger">
        Needs Improvement
      </Badge>
    );
  };

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <ChartMultiple24Regular style={{ fontSize: '32px' }} />
          <div>
            <Title1>Pacing Optimizer</Title1>
            <Caption1>AI-powered video pacing analysis and optimization</Caption1>
          </div>
        </div>
        <div className={styles.headerRight}>
          <Button
            appearance="secondary"
            icon={<Settings24Regular />}
            onClick={() => setShowSettings(true)}
          >
            Settings
          </Button>
          {data && (
            <Button
              appearance="secondary"
              icon={<ArrowClockwise24Regular />}
              onClick={handleReanalyze}
              disabled={loading}
            >
              Reanalyze
            </Button>
          )}
          {!data && (
            <Button
              appearance="primary"
              icon={loading ? <Spinner size="tiny" /> : <ChartMultiple24Regular />}
              onClick={handleAnalyze}
              disabled={loading || !script || scenes.length === 0}
            >
              {loading ? 'Analyzing...' : 'Analyze Pacing'}
            </Button>
          )}
          {onClose && <Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />}
        </div>
      </div>

      {/* Success Message */}
      {successMessage && (
        <MessageBar intent="success">
          <MessageBarBody>
            <MessageBarTitle>Success</MessageBarTitle>
            {successMessage}
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Error State */}
      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Loading State */}
      {loading && (
        <Card className={styles.loadingContainer}>
          <Spinner size="extra-large" />
          <Title3>Analyzing pacing...</Title3>
          <Body1>This may take a moment while we analyze your video content</Body1>
        </Card>
      )}

      {/* Empty State */}
      {!loading && !data && !error && (
        <Card className={styles.emptyState}>
          <ChartMultiple24Regular
            style={{ fontSize: '64px', marginBottom: tokens.spacingVerticalL }}
          />
          <Title3>Ready to optimize your video pacing</Title3>
          <Body1>
            Click &quot;Analyze Pacing&quot; to get AI-powered suggestions for scene timing
          </Body1>
        </Card>
      )}

      {/* Results */}
      {!loading && data && (
        <>
          {/* Overall Score */}
          <Card className={styles.scoreCard}>
            <Caption1>Overall Pacing Score</Caption1>
            <div className={styles.scoreValue} style={{ color: getScoreColor(data.overallScore) }}>
              {data.overallScore.toFixed(0)}
            </div>
            {getScoreBadge(data.overallScore)}
          </Card>

          {/* Metrics */}
          <div className={styles.metricsGrid}>
            <Card className={styles.metricCard}>
              <Caption1>Estimated Retention</Caption1>
              <Title3>{data.estimatedRetention.toFixed(1)}%</Title3>
            </Card>
            <Card className={styles.metricCard}>
              <Caption1>Average Engagement</Caption1>
              <Title3>{data.averageEngagement.toFixed(1)}%</Title3>
            </Card>
            <Card className={styles.metricCard}>
              <Caption1>Confidence Score</Caption1>
              <Title3>{data.confidenceScore.toFixed(1)}%</Title3>
            </Card>
            <Card className={styles.metricCard}>
              <Caption1>Suggestions</Caption1>
              <Title3>{data.suggestions.length}</Title3>
            </Card>
          </div>

          {/* Diagnostic Info */}
          {data.correlationId && (
            <Card style={{ padding: tokens.spacingVerticalS }}>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                Analysis ID: {data.analysisId} | Correlation ID: {data.correlationId}
              </Caption1>
            </Card>
          )}

          {/* Attention Curve */}
          {data.attentionCurve && <AttentionCurveChart data={data.attentionCurve} />}

          {/* Warnings */}
          {data.warnings.length > 0 && (
            <MessageBar intent="warning">
              <MessageBarBody>
                <MessageBarTitle>Recommendations</MessageBarTitle>
                {data.warnings.map((warning, i) => (
                  <div key={i}>{warning}</div>
                ))}
              </MessageBarBody>
            </MessageBar>
          )}

          {/* Scene Suggestions */}
          {data.suggestions.length > 0 && (
            <div className={styles.suggestionsContainer}>
              <div
                style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              >
                <Title3>Scene-by-Scene Suggestions</Title3>
                <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleApplyAll}>
                  Apply All High-Confidence
                </Button>
              </div>

              {data.suggestions
                .filter((s) => s.confidence >= settings.minConfidence)
                .map((suggestion) => (
                  <SceneSuggestionCard
                    key={suggestion.sceneIndex}
                    suggestion={suggestion}
                    onAccept={handleAcceptSuggestion}
                    onReject={handleRejectSuggestion}
                    isApplied={appliedSuggestions.has(suggestion.sceneIndex)}
                  />
                ))}
            </div>
          )}
        </>
      )}

      {/* Settings Drawer */}
      <Drawer
        type="overlay"
        open={showSettings}
        onOpenChange={(_, { open }) => setShowSettings(open)}
        position="end"
        size="medium"
      >
        <DrawerHeader>
          <DrawerHeaderTitle>Pacing Settings</DrawerHeaderTitle>
        </DrawerHeader>
        <DrawerBody>
          <PacingSettings
            settings={settings}
            platforms={platforms}
            onSettingsChange={setSettings}
            onSave={handleSettingsSave}
          />
        </DrawerBody>
      </Drawer>

      {/* Confirmation Dialog for Applying Suggestions */}
      <Dialog open={showConfirmDialog} onOpenChange={(_, { open }) => !open && handleCancelApply()}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Apply Pacing Suggestions?</DialogTitle>
            <DialogContent>
              <Body1>
                You are about to apply {pendingSuggestions.length} high-confidence pacing
                suggestions to your scenes. This will adjust the duration of the following scenes:
              </Body1>
              <div
                style={{
                  marginTop: tokens.spacingVerticalM,
                  marginBottom: tokens.spacingVerticalM,
                }}
              >
                {pendingSuggestions.map((sceneIndex) => {
                  const suggestion = data?.suggestions.find((s) => s.sceneIndex === sceneIndex);
                  if (!suggestion) return null;
                  return (
                    <Caption1
                      key={sceneIndex}
                      block
                      style={{ marginTop: tokens.spacingVerticalXS }}
                    >
                      • Scene {sceneIndex + 1}: {suggestion.currentDuration} →{' '}
                      {suggestion.optimalDuration}
                    </Caption1>
                  );
                })}
              </div>
              <Body1>
                You can revert individual suggestions later if needed. Would you like to proceed?
              </Body1>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={handleCancelApply}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={handleConfirmApply}>
                Apply Suggestions
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};
