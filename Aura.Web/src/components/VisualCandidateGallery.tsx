import {
  Card,
  Button,
  Text,
  Badge,
  Tooltip,
  makeStyles,
  tokens,
  Spinner,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Dismiss24Regular,
  ArrowCounterclockwise24Regular,
  Sparkle24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
  },
  sceneHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
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
  },
  selectedCard: {
    border: `2px solid ${tokens.colorPaletteGreenBorder1}`,
    backgroundColor: tokens.colorPaletteGreenBackground1,
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
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: 'auto',
  },
  licensingInfo: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
  noSelection: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
});

export interface ImageCandidate {
  imageUrl: string;
  source: string;
  aestheticScore: number;
  keywordCoverageScore: number;
  qualityScore: number;
  overallScore: number;
  reasoning: string;
  width: number;
  height: number;
  rejectionReasons: string[];
  generationLatencyMs: number;
  licensing?: {
    licenseType: string;
    commercialUseAllowed: boolean;
    attributionRequired: boolean;
    creatorName?: string;
    creatorUrl?: string;
    sourcePlatform: string;
    licenseUrl?: string;
    attribution?: string;
  };
}

export interface SceneVisualSelection {
  sceneIndex: number;
  selectedCandidate?: ImageCandidate;
  candidates: ImageCandidate[];
  state: 'Pending' | 'Accepted' | 'Rejected' | 'Replaced';
  rejectionReason?: string;
  metadata?: {
    totalGenerationTimeMs: number;
    regenerationCount: number;
    autoSelected: boolean;
    autoSelectionConfidence?: number;
  };
}

interface VisualCandidateGalleryProps {
  selection: SceneVisualSelection;
  onAccept: (candidate: ImageCandidate) => void;
  onReject: (reason: string) => void;
  onRegenerate: () => void;
  onSuggestRefinement: () => void;
  isLoading?: boolean;
}

const VisualCandidateGallery: FC<VisualCandidateGalleryProps> = ({
  selection,
  onAccept,
  onReject,
  onRegenerate,
  onSuggestRefinement,
  isLoading = false,
}) => {
  const styles = useStyles();
  const [selectedCandidateIndex, setSelectedCandidateIndex] = useState<number | null>(
    selection.selectedCandidate
      ? selection.candidates.findIndex((c) => c.imageUrl === selection.selectedCandidate?.imageUrl)
      : null
  );

  const getScoreBadgeColor = (score: number): 'success' | 'warning' | 'danger' => {
    if (score >= 75) return 'success';
    if (score >= 60) return 'warning';
    return 'danger';
  };

  const handleAcceptCandidate = (candidate: ImageCandidate, index: number) => {
    setSelectedCandidateIndex(index);
    onAccept(candidate);
  };

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXXL }}>
          <Spinner size="large" label="Generating candidates..." />
        </div>
      </div>
    );
  }

  if (selection.candidates.length === 0) {
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
      <div className={styles.sceneHeader}>
        <div>
          <Text size={500} weight="semibold">
            Scene {selection.sceneIndex + 1} - Visual Candidates
          </Text>
          {selection.metadata && selection.metadata.regenerationCount > 0 && (
            <Text size={200} style={{ marginLeft: tokens.spacingHorizontalM }}>
              ({selection.metadata.regenerationCount} regeneration
              {selection.metadata.regenerationCount > 1 ? 's' : ''})
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

      <div className={styles.candidatesGrid}>
        {selection.candidates.map((candidate, index) => {
          const isSelected = index === selectedCandidateIndex;
          const cardClass = isSelected
            ? `${styles.candidateCard} ${styles.selectedCard}`
            : styles.candidateCard;

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
                  Candidate {index + 1}
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

                {candidate.licensing && (
                  <div className={styles.licensingInfo}>
                    <Text size={200}>
                      {candidate.licensing.licenseType} • {candidate.licensing.sourcePlatform}
                    </Text>
                    {candidate.licensing.attributionRequired && (
                      <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
                        ⚠ Attribution required
                      </Text>
                    )}
                  </div>
                )}

                {candidate.rejectionReasons.length > 0 && (
                  <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                    Issues: {candidate.rejectionReasons.join(', ')}
                  </Text>
                )}

                <div className={styles.actions}>
                  {!isSelected ? (
                    <Button
                      appearance="primary"
                      icon={<CheckmarkCircle24Regular />}
                      onClick={() => handleAcceptCandidate(candidate, index)}
                      style={{ flex: 1 }}
                    >
                      Accept
                    </Button>
                  ) : (
                    <Button
                      appearance="secondary"
                      icon={<Dismiss24Regular />}
                      onClick={() => onReject('Changed selection')}
                      style={{ flex: 1 }}
                    >
                      Remove
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

export default VisualCandidateGallery;
