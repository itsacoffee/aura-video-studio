import React from 'react';
import {
  Card,
  CardHeader,
  Text,
  makeStyles,
  tokens,
  Badge,
  Button,
} from '@fluentui/react-components';
import { ThumbLikeRegular, ThumbDislikeRegular, ArrowExpandRegular } from '@fluentui/react-icons';
import type { ConceptIdea } from '../../services/ideationService';
import { ideationService } from '../../services/ideationService';

const useStyles = makeStyles({
  card: {
    width: '100%',
    marginBottom: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow16,
    },
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  icon: {
    fontSize: '24px',
  },
  content: {
    padding: tokens.spacingVerticalM,
  },
  description: {
    marginBottom: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground2,
  },
  section: {
    marginBottom: tokens.spacingVerticalM,
  },
  sectionTitle: {
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  list: {
    margin: 0,
    paddingLeft: tokens.spacingHorizontalL,
    color: tokens.colorNeutralForeground2,
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  scoreContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  scoreLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  badge: {
    marginLeft: tokens.spacingHorizontalS,
  },
});

interface ConceptCardProps {
  concept: ConceptIdea;
  onSelect?: (concept: ConceptIdea) => void;
  onExpand?: (concept: ConceptIdea) => void;
}

export const ConceptCard: React.FC<ConceptCardProps> = ({ concept, onSelect, onExpand }) => {
  const styles = useStyles();
  const scoreColor = ideationService.getAppealScoreColor(concept.appealScore);
  const scoreText = ideationService.formatAppealScore(concept.appealScore);
  const angleIcon = ideationService.getAngleIcon(concept.angle);

  const handleClick = () => {
    if (onSelect) {
      onSelect(concept);
    }
  };

  const handleExpand = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onExpand) {
      onExpand(concept);
    }
  };

  return (
    <Card className={styles.card} onClick={handleClick}>
      <CardHeader
        header={
          <div className={styles.header}>
            <span className={styles.icon}>{angleIcon}</span>
            <Text weight="semibold" size={400}>
              {concept.title}
            </Text>
            <Badge className={styles.badge} appearance="filled">
              {concept.angle}
            </Badge>
          </div>
        }
        description={
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            {concept.targetAudience}
          </Text>
        }
      />

      <div className={styles.content}>
        <Text className={styles.description}>{concept.description}</Text>

        {concept.hook && (
          <div className={styles.section}>
            <Text className={styles.sectionTitle}>Hook:</Text>
            <Text style={{ color: tokens.colorBrandForeground1 }}>&quot;{concept.hook}&quot;</Text>
          </div>
        )}

        <div className={styles.section}>
          <Text className={styles.sectionTitle}>
            <ThumbLikeRegular style={{ marginRight: '4px' }} />
            Pros:
          </Text>
          <ul className={styles.list}>
            {concept.pros.slice(0, 3).map((pro, index) => (
              <li key={index}>{pro}</li>
            ))}
          </ul>
        </div>

        <div className={styles.section}>
          <Text className={styles.sectionTitle}>
            <ThumbDislikeRegular style={{ marginRight: '4px' }} />
            Cons:
          </Text>
          <ul className={styles.list}>
            {concept.cons.slice(0, 3).map((con, index) => (
              <li key={index}>{con}</li>
            ))}
          </ul>
        </div>

        <div className={styles.footer}>
          <div className={styles.scoreContainer}>
            <Text className={styles.scoreLabel}>Appeal:</Text>
            <Badge appearance="filled" style={{ backgroundColor: scoreColor, color: 'white' }}>
              {scoreText} ({concept.appealScore})
            </Badge>
          </div>

          <div className={styles.actions}>
            <Button appearance="subtle" icon={<ArrowExpandRegular />} onClick={handleExpand}>
              Expand
            </Button>
          </div>
        </div>
      </div>
    </Card>
  );
};
