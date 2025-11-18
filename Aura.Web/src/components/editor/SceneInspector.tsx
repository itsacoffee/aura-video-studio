import {
  Card,
  Button,
  Text,
  Badge,
  Slider,
  Spinner,
  makeStyles,
  tokens,
  Tooltip,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ArrowCounterclockwise24Regular,
  Sparkle24Regular,
  Info24Regular,
  Eye24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';
import type { ImageCandidate } from '@/services/visualSelectionService';
import { useVisualSelectionStore } from '@/state/visualSelection';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    height: '100%',
    overflow: 'auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingBottom: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  thresholdSection: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  thresholdLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  candidatesGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  candidateCard: {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    border: `2px solid ${tokens.colorNeutralStroke1}`,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    ':hover': {
      border: `2px solid ${tokens.colorBrandStroke1}`,
      boxShadow: tokens.shadow8,
    },
  },
  selectedCard: {
    border: `2px solid ${tokens.colorPaletteGreenBorder1}`,
    backgroundColor: tokens.colorPaletteGreenBackground1,
  },
  belowThresholdCard: {
    opacity: 0.5,
    border: `2px solid ${tokens.colorPaletteRedBorder1}`,
  },
  imageContainer: {
    position: 'relative',
    width: '100%',
    paddingTop: '56.25%',
    backgroundColor: tokens.colorNeutralBackground3,
    overflow: 'hidden',
  },
  image: {
    position: 'absolute',
    top: '0',
    left: '0',
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  scoreOverlay: {
    position: 'absolute',
    top: tokens.spacingVerticalS,
    right: tokens.spacingVerticalS,
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  cardContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    flex: 1,
  },
  scores: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: tokens.spacingVerticalXS,
  },
  scoreItem: {
    display: 'flex',
    justifyContent: 'space-between',
    fontSize: tokens.fontSizeBase200,
  },
  coverageSection: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
  },
  keywordTags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXXS,
    marginTop: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: 'auto',
  },
  noSelection: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
  loadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(255, 255, 255, 0.8)',
    zIndex: 10,
  },
});

interface SceneInspectorProps {
  jobId: string;
  sceneIndex: number;
  candidates: ImageCandidate[];
  selectedCandidate?: ImageCandidate;
  onAccept: (candidate: ImageCandidate) => void;
  onRegenerate: () => void;
  onSuggestRefinement: () => void;
  isLoading?: boolean;
}

const SceneInspector: FC<SceneInspectorProps> = ({
  jobId,
  sceneIndex,
  candidates,
  selectedCandidate,
  onAccept,
  onRegenerate,
  onSuggestRefinement,
  isLoading = false,
}) => {
  const styles = useStyles();
  const { getThresholds, setThresholds } = useVisualSelectionStore();

  const projectId = jobId;
  const thresholds = getThresholds(projectId);
  const [threshold, setThreshold] = useState(thresholds.minimumAestheticThreshold);

  useEffect(() => {
    const updated = getThresholds(projectId);
    setThreshold(updated.minimumAestheticThreshold);
  }, [projectId, getThresholds]);

  const handleThresholdChange = useCallback(
    (_event: unknown, data: { value: number }) => {
      setThreshold(data.value);
      setThresholds(projectId, {
        ...thresholds,
        minimumAestheticThreshold: data.value,
      });
    },
    [projectId, thresholds, setThresholds]
  );

  const getScoreBadgeColor = (score: number): 'success' | 'warning' | 'danger' => {
    if (score >= 75) return 'success';
    if (score >= 60) return 'warning';
    return 'danger';
  };

  const meetsThreshold = (score: number): boolean => score >= threshold;

  const filteredCandidates = candidates.filter((c) => meetsThreshold(c.overallScore));
  const belowThresholdCount = candidates.length - filteredCandidates.length;

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.loadingOverlay}>
          <Spinner size="large" label="Loading candidates..." />
        </div>
      </div>
    );
  }

  if (candidates.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.noSelection}>
          <Text size={400}>No candidates available for this scene.</Text>
          <br />
          <Button
            appearance="primary"
            onClick={onRegenerate}
            icon={<ArrowCounterclockwise24Regular />}
          >
            Generate Candidates
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Text size={500} weight="semibold">
            Scene {sceneIndex + 1} - Visual Candidates ({candidates.length})
          </Text>
          {belowThresholdCount > 0 && (
            <Text
              size={200}
              style={{
                color: tokens.colorPaletteRedForeground1,
                marginLeft: tokens.spacingHorizontalM,
              }}
            >
              ({belowThresholdCount} below threshold)
            </Text>
          )}
        </div>
        <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
          <Button
            appearance="secondary"
            icon={<ArrowCounterclockwise24Regular />}
            onClick={onRegenerate}
            disabled={isLoading}
          >
            Regenerate
          </Button>
          <Tooltip
            content="Get LLM-suggested improvements to the visual prompt"
            relationship="description"
          >
            <Button
              appearance="secondary"
              icon={<Sparkle24Regular />}
              onClick={onSuggestRefinement}
              disabled={isLoading}
            >
              Suggest Better
            </Button>
          </Tooltip>
        </div>
      </div>

      <div className={styles.thresholdSection}>
        <div className={styles.thresholdLabel}>
          <Text size={300} weight="semibold">
            Quality Threshold
          </Text>
          <Badge appearance="filled" color={getScoreBadgeColor(threshold)}>
            {threshold.toFixed(0)}
          </Badge>
        </div>
        <Slider min={0} max={100} value={threshold} onChange={handleThresholdChange} step={5} />
        <Text
          size={200}
          style={{ marginTop: tokens.spacingVerticalXS, color: tokens.colorNeutralForeground3 }}
        >
          Candidates below this threshold will be dimmed
        </Text>
      </div>

      <div className={styles.candidatesGrid}>
        {candidates.map((candidate, index) => {
          const isSelected = selectedCandidate?.imageUrl === candidate.imageUrl;
          const passesThreshold = meetsThreshold(candidate.overallScore);

          let cardClass = styles.candidateCard;
          if (isSelected) {
            cardClass += ` ${styles.selectedCard}`;
          } else if (!passesThreshold) {
            cardClass += ` ${styles.belowThresholdCard}`;
          }

          return (
            <Card key={`${candidate.imageUrl}-${index}`} className={cardClass}>
              <div className={styles.imageContainer}>
                <img
                  src={candidate.imageUrl}
                  alt={`Candidate ${index + 1}`}
                  className={styles.image}
                />
                <div className={styles.scoreOverlay}>
                  <Badge appearance="filled" color={getScoreBadgeColor(candidate.overallScore)}>
                    {candidate.overallScore.toFixed(0)}
                  </Badge>
                  {isSelected && (
                    <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
                      Selected
                    </Badge>
                  )}
                </div>
              </div>

              <div className={styles.cardContent}>
                <Text size={300} weight="semibold">
                  Candidate {index + 1} • {candidate.source}
                </Text>

                <div className={styles.scores}>
                  <div className={styles.scoreItem}>
                    <Text size={200}>Aesthetic:</Text>
                    <Text size={200} weight="semibold">
                      {candidate.aestheticScore.toFixed(1)}
                    </Text>
                  </div>
                  <div className={styles.scoreItem}>
                    <Text size={200}>Keywords:</Text>
                    <Text size={200} weight="semibold">
                      {candidate.keywordCoverageScore.toFixed(1)}
                    </Text>
                  </div>
                  <div className={styles.scoreItem}>
                    <Text size={200}>Quality:</Text>
                    <Text size={200} weight="semibold">
                      {candidate.qualityScore.toFixed(1)}
                    </Text>
                  </div>
                  <div className={styles.scoreItem}>
                    <Text size={200}>Overall:</Text>
                    <Text size={200} weight="semibold">
                      {candidate.overallScore.toFixed(1)}
                    </Text>
                  </div>
                </div>

                <Tooltip content={candidate.reasoning} relationship="description">
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: tokens.spacingHorizontalXS,
                    }}
                  >
                    <Info24Regular />
                    <Text size={200} truncate>
                      {candidate.reasoning}
                    </Text>
                  </div>
                </Tooltip>

                {candidate.rejectionReasons.length > 0 && (
                  <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                    Issues: {candidate.rejectionReasons.join(', ')}
                  </Text>
                )}

                <div className={styles.actions}>
                  {!isSelected && passesThreshold ? (
                    <Button
                      appearance="primary"
                      icon={<CheckmarkCircle24Regular />}
                      onClick={() => onAccept(candidate)}
                      style={{ flex: 1 }}
                    >
                      Accept
                    </Button>
                  ) : isSelected ? (
                    <Badge
                      appearance="filled"
                      color="success"
                      icon={<Eye24Regular />}
                      style={{ flex: 1 }}
                    >
                      Currently Selected
                    </Badge>
                  ) : (
                    <Button appearance="outline" disabled style={{ flex: 1 }}>
                      Below Threshold
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
};

export default SceneInspector;
