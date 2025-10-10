import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Textarea,
  Card,
  Badge,
  Spinner,
  Tooltip,
  Field,
} from '@fluentui/react-components';
import {
  Lightbulb24Regular,
  Edit24Regular,
  Checkmark24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import type { PlannerRecommendations } from '../types';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  qualityBadge: {
    marginLeft: tokens.spacingHorizontalS,
  },
  outlineCard: {
    padding: tokens.spacingVerticalL,
  },
  outlineDisplay: {
    whiteSpace: 'pre-wrap',
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase300,
    lineHeight: tokens.lineHeightBase300,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  outlineEditor: {
    minHeight: '300px',
    fontFamily: tokens.fontFamilyMonospace,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalM,
  },
  metricsCard: {
    padding: tokens.spacingVerticalL,
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  metricValue: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  seoCard: {
    padding: tokens.spacingVerticalL,
  },
  seoSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  tags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
  },
  explainability: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

interface PlannerPanelProps {
  recommendations: PlannerRecommendations | null;
  loading?: boolean;
  onOutlineChange?: (outline: string) => void;
  onAccept?: () => void;
}

export const PlannerPanel: React.FC<PlannerPanelProps> = ({
  recommendations,
  loading = false,
  onOutlineChange,
  onAccept,
}) => {
  const styles = useStyles();
  const [isEditing, setIsEditing] = useState(false);
  const [editedOutline, setEditedOutline] = useState('');

  const handleEdit = () => {
    if (recommendations) {
      setEditedOutline(recommendations.outline);
      setIsEditing(true);
    }
  };

  const handleSave = () => {
    if (onOutlineChange) {
      onOutlineChange(editedOutline);
    }
    setIsEditing(false);
  };

  const handleCancel = () => {
    setIsEditing(false);
    setEditedOutline('');
  };

  const getQualityColor = (score: number): 'success' | 'warning' | 'important' => {
    if (score >= 0.8) return 'success';
    if (score >= 0.7) return 'warning';
    return 'important';
  };

  const getQualityLabel = (score: number): string => {
    if (score >= 0.85) return 'Excellent';
    if (score >= 0.75) return 'Good';
    if (score >= 0.65) return 'Fair';
    return 'Basic';
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Card className={styles.outlineCard}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            <Spinner size="medium" />
            <Text>Generating recommendations...</Text>
          </div>
        </Card>
      </div>
    );
  }

  if (!recommendations) {
    return null;
  }

  const qualityScore = recommendations.qualityScore ?? 0.75;

  return (
    <div className={styles.container}>
      {/* Outline Section */}
      <Card className={styles.outlineCard}>
        <div className={styles.header}>
          <div className={styles.headerLeft}>
            <Lightbulb24Regular />
            <Title3>Video Outline</Title3>
            {recommendations.providerUsed && (
              <Badge appearance="outline" size="small">
                {recommendations.providerUsed}
              </Badge>
            )}
            {recommendations.qualityScore !== undefined && (
              <Tooltip
                content={`Quality Score: ${(qualityScore * 100).toFixed(0)}% - ${getQualityLabel(qualityScore)}`}
                relationship="label"
              >
                <Badge
                  appearance="filled"
                  color={getQualityColor(qualityScore)}
                  size="small"
                  className={styles.qualityBadge}
                >
                  {getQualityLabel(qualityScore)}
                </Badge>
              </Tooltip>
            )}
          </div>
          {!isEditing && (
            <Button
              appearance="subtle"
              icon={<Edit24Regular />}
              onClick={handleEdit}
            >
              Edit
            </Button>
          )}
        </div>

        {isEditing ? (
          <>
            <Field label="Edit outline (Markdown supported)">
              <Textarea
                className={styles.outlineEditor}
                value={editedOutline}
                onChange={(e) => setEditedOutline(e.target.value)}
                resize="vertical"
              />
            </Field>
            <div className={styles.actions}>
              <Button appearance="secondary" onClick={handleCancel}>
                Cancel
              </Button>
              <Button
                appearance="primary"
                icon={<Checkmark24Regular />}
                onClick={handleSave}
              >
                Save Changes
              </Button>
            </div>
          </>
        ) : (
          <div className={styles.outlineDisplay}>{recommendations.outline}</div>
        )}

        {recommendations.explainabilityNotes && (
          <div className={styles.explainability}>
            <Text size={200}>
              <Info24Regular style={{ verticalAlign: 'middle', marginRight: tokens.spacingHorizontalXS }} />
              {recommendations.explainabilityNotes}
            </Text>
          </div>
        )}
      </Card>

      {/* Metrics Section */}
      <Card className={styles.metricsCard}>
        <Title3>Production Metrics</Title3>
        <div className={styles.metricsGrid}>
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>Scene Count</Text>
            <Text className={styles.metricValue}>{recommendations.sceneCount}</Text>
          </div>
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>Shots per Scene</Text>
            <Text className={styles.metricValue}>{recommendations.shotsPerScene}</Text>
          </div>
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>B-Roll %</Text>
            <Text className={styles.metricValue}>{recommendations.bRollPercentage.toFixed(0)}%</Text>
          </div>
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>Reading Level</Text>
            <Text className={styles.metricValue}>Grade {recommendations.readingLevel}</Text>
          </div>
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>Voice Style</Text>
            <Text className={styles.metricValue}>{recommendations.voice.style}</Text>
          </div>
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>Music Tempo</Text>
            <Text className={styles.metricValue}>{recommendations.music.tempo}</Text>
          </div>
        </div>
      </Card>

      {/* SEO Section */}
      <Card className={styles.seoCard}>
        <Title3>SEO & Publishing</Title3>
        <div className={styles.seoSection}>
          <div>
            <Text weight="semibold">Title</Text>
            <Text block>{recommendations.seo.title}</Text>
          </div>
          <div>
            <Text weight="semibold">Description</Text>
            <Text block>{recommendations.seo.description}</Text>
          </div>
          <div>
            <Text weight="semibold">Tags</Text>
            <div className={styles.tags}>
              {recommendations.seo.tags.map((tag, index) => (
                <Badge key={index} appearance="outline" size="small">
                  {tag}
                </Badge>
              ))}
            </div>
          </div>
          <div>
            <Text weight="semibold">Thumbnail Prompt</Text>
            <Text block style={{ fontStyle: 'italic', color: tokens.colorNeutralForeground3 }}>
              {recommendations.thumbnailPrompt}
            </Text>
          </div>
        </div>
      </Card>

      {/* Accept Button */}
      {onAccept && (
        <div className={styles.actions}>
          <Button
            appearance="primary"
            size="large"
            icon={<Checkmark24Regular />}
            onClick={onAccept}
          >
            Accept & Continue to Generation
          </Button>
        </div>
      )}
    </div>
  );
};
