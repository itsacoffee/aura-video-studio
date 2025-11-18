import {
  Button,
  Card,
  Field,
  Textarea,
  makeStyles,
  tokens,
  Text,
  Badge,
  Spinner,
  Divider,
  type GriffelStyle,
} from '@fluentui/react-components';
import {
  EditRegular,
  ArrowExpandRegular,
  ArrowMinimizeRegular,
  PeopleRegular,
  ArrowSyncRegular,
  SaveRegular,
} from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
import type { FC } from 'react';
import type { ConceptIdea } from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    width: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  headerContent: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  icon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  mainCard: {
    padding: tokens.spacingVerticalXL,
  },
  conceptSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  conceptHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  conceptInfo: {
    flex: 1,
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
  },
  appealScore: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalXXS,
    minWidth: '80px',
  },
  scoreCircle: {
    width: '60px',
    height: '60px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightBold,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  description: {
    fontSize: tokens.fontSizeBase300,
    lineHeight: tokens.lineHeightBase400,
    color: tokens.colorNeutralForeground2,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  prosConsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
  },
  prosConsColumn: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  columnTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  prosItem: {
    padding: tokens.spacingVerticalXS,
    paddingLeft: tokens.spacingHorizontalM,
    borderLeft: `3px solid ${tokens.colorPaletteGreenBorder2}`,
    fontSize: tokens.fontSizeBase200,
  },
  consItem: {
    padding: tokens.spacingVerticalXS,
    paddingLeft: tokens.spacingHorizontalM,
    borderLeft: `3px solid ${tokens.colorPaletteRedBorder2}`,
    fontSize: tokens.fontSizeBase200,
  },
  refinementSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
  refinementOptions: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  refinementCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    border: `2px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      borderColor: tokens.colorBrandStroke1,
      boxShadow: tokens.shadow4,
    },
  } as GriffelStyle,
  refinementCardSelected: {
    borderColor: tokens.colorBrandForeground1,
    backgroundColor: tokens.colorBrandBackground2Hover,
  } as GriffelStyle,
  refinementIcon: {
    fontSize: '32px',
    marginBottom: tokens.spacingVerticalXS,
  },
  refinementTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXXS,
  },
  refinementDescription: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  feedbackSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  loadingOverlay: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXXL,
  },
  metadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
});

export interface ConceptRefinerProps {
  concept: ConceptIdea;
  onRefine: (direction: RefinementDirection, feedback?: string) => void;
  isRefining?: boolean;
  onSave?: (concept: ConceptIdea) => void;
}

export type RefinementDirection = 'expand' | 'simplify' | 'adjust-audience' | 'merge';

interface RefinementOption {
  id: RefinementDirection;
  title: string;
  description: string;
  icon: React.ReactElement;
}

const refinementOptions: RefinementOption[] = [
  {
    id: 'expand',
    title: 'Expand',
    description: 'Add more depth and detail to the concept',
    icon: <ArrowExpandRegular />,
  },
  {
    id: 'simplify',
    title: 'Simplify',
    description: 'Streamline and focus the concept',
    icon: <ArrowMinimizeRegular />,
  },
  {
    id: 'adjust-audience',
    title: 'Adjust Audience',
    description: 'Retarget for a different audience',
    icon: <PeopleRegular />,
  },
  {
    id: 'merge',
    title: 'Merge Ideas',
    description: 'Combine with other concepts',
    icon: <ArrowSyncRegular />,
  },
];

export const ConceptRefiner: FC<ConceptRefinerProps> = ({
  concept,
  onRefine,
  isRefining = false,
  onSave,
}) => {
  const styles = useStyles();
  const [selectedDirection, setSelectedDirection] = useState<RefinementDirection | null>(null);
  const [feedback, setFeedback] = useState('');

  const handleRefine = useCallback(() => {
    if (!selectedDirection) return;
    onRefine(selectedDirection, feedback || undefined);
    setFeedback('');
  }, [selectedDirection, feedback, onRefine]);

  if (isRefining) {
    return (
      <div className={styles.loadingOverlay}>
        <Spinner size="extra-large" />
        <Text size={400}>Refining concept...</Text>
        <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
          The AI is analyzing and improving your concept
        </Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <EditRegular className={styles.icon} />
          <div>
            <Text size={500} weight="semibold">
              Concept Refiner
            </Text>
            <div>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Interactively improve and adjust your concept
              </Text>
            </div>
          </div>
        </div>
        {onSave && (
          <Button appearance="primary" icon={<SaveRegular />} onClick={() => onSave(concept)}>
            Save
          </Button>
        )}
      </div>

      <Card className={styles.mainCard}>
        <div className={styles.conceptSection}>
          <div className={styles.conceptHeader}>
            <div className={styles.conceptInfo}>
              <Text className={styles.title}>{concept.title}</Text>
              <Text className={styles.subtitle}>{concept.angle}</Text>
              <div className={styles.metadata}>
                <Badge appearance="tint" color="brand">
                  {concept.targetAudience}
                </Badge>
              </div>
            </div>
            <div className={styles.appealScore}>
              <div className={styles.scoreCircle}>{Math.round(concept.appealScore)}</div>
              <Text size={200}>Appeal</Text>
            </div>
          </div>

          <Text className={styles.description}>{concept.description}</Text>

          {concept.hook && (
            <>
              <Divider />
              <div>
                <Text weight="semibold" size={300}>
                  Hook:
                </Text>
                <div style={{ marginTop: tokens.spacingVerticalXS }}>
                  <Text size={300}>{concept.hook}</Text>
                </div>
              </div>
            </>
          )}

          {((concept.pros && concept.pros.length > 0) ||
            (concept.cons && concept.cons.length > 0)) && (
            <>
              <Divider />
              <div className={styles.prosConsGrid}>
                {concept.pros && concept.pros.length > 0 && (
                  <div className={styles.prosConsColumn}>
                    <Text
                      className={styles.columnTitle}
                      style={{ color: tokens.colorPaletteGreenForeground1 }}
                    >
                      Strengths
                    </Text>
                    {concept.pros.map((pro, idx) => (
                      <div key={idx} className={styles.prosItem}>
                        {pro}
                      </div>
                    ))}
                  </div>
                )}
                {concept.cons && concept.cons.length > 0 && (
                  <div className={styles.prosConsColumn}>
                    <Text
                      className={styles.columnTitle}
                      style={{ color: tokens.colorPaletteRedForeground1 }}
                    >
                      Challenges
                    </Text>
                    {concept.cons.map((con, idx) => (
                      <div key={idx} className={styles.consItem}>
                        {con}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </Card>

      <div className={styles.refinementSection}>
        <Text size={400} weight="semibold">
          How would you like to refine this concept?
        </Text>

        <div className={styles.refinementOptions}>
          {refinementOptions.map((option) => (
            <Card
              key={option.id}
              className={`${styles.refinementCard} ${
                selectedDirection === option.id ? styles.refinementCardSelected : ''
              }`}
              onClick={() => setSelectedDirection(option.id)}
            >
              <div
                className={styles.refinementIcon}
                style={{ color: tokens.colorBrandForeground1 }}
              >
                {option.icon}
              </div>
              <Text className={styles.refinementTitle}>{option.title}</Text>
              <Text className={styles.refinementDescription}>{option.description}</Text>
            </Card>
          ))}
        </div>

        {selectedDirection && (
          <div className={styles.feedbackSection}>
            <Field
              label="Additional feedback (optional)"
              hint="Tell the AI what specific changes you'd like"
            >
              <Textarea
                value={feedback}
                onChange={(_, data) => setFeedback(data.value)}
                placeholder="E.g., 'Make it more humorous' or 'Focus on technical details'"
                rows={3}
              />
            </Field>
          </div>
        )}

        <div className={styles.actions}>
          <Button
            appearance="primary"
            icon={<ArrowSyncRegular />}
            onClick={handleRefine}
            disabled={!selectedDirection}
          >
            Refine Concept
          </Button>
        </div>
      </div>
    </div>
  );
};
