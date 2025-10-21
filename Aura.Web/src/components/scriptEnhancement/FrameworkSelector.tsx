import React from 'react';
import {
  Card,
  Text,
  Button,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { ArrowRightRegular } from '@fluentui/react-icons';
import {
  StoryFrameworkType,
  scriptEnhancementService,
} from '../../services/scriptEnhancementService';

const useStyles = makeStyles({
  container: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  card: {
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow16,
    },
  },
  cardContent: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  title: {
    marginBottom: tokens.spacingVerticalXS,
  },
  description: {
    color: tokens.colorNeutralForeground2,
    flexGrow: 1,
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
  },
});

interface FrameworkSelectorProps {
  onSelect: (framework: StoryFrameworkType) => void;
  disabled?: boolean;
}

export const FrameworkSelector: React.FC<FrameworkSelectorProps> = ({
  onSelect,
  disabled = false,
}) => {
  const styles = useStyles();

  const frameworks = [
    {
      type: StoryFrameworkType.HeroJourney,
      icon: '⚔️',
      name: "Hero's Journey",
    },
    {
      type: StoryFrameworkType.ThreeAct,
      icon: '🎬',
      name: 'Three-Act Structure',
    },
    {
      type: StoryFrameworkType.ProblemSolution,
      icon: '🔧',
      name: 'Problem-Solution',
    },
    {
      type: StoryFrameworkType.AIDA,
      icon: '📣',
      name: 'AIDA',
    },
    {
      type: StoryFrameworkType.BeforeAfter,
      icon: '🔄',
      name: 'Before-After-Bridge',
    },
    {
      type: StoryFrameworkType.Comparison,
      icon: '⚖️',
      name: 'Comparison',
    },
    {
      type: StoryFrameworkType.Chronological,
      icon: '📅',
      name: 'Chronological',
    },
    {
      type: StoryFrameworkType.CauseEffect,
      icon: '🎯',
      name: 'Cause-Effect',
    },
  ];

  return (
    <div className={styles.container}>
      {frameworks.map((framework) => {
        const description = scriptEnhancementService.getFrameworkDescription(
          framework.type
        );

        return (
          <Card
            key={framework.type}
            className={styles.card}
            onClick={() => !disabled && onSelect(framework.type)}
          >
            <div className={styles.cardContent}>
              <div style={{ fontSize: '32px', textAlign: 'center' }}>
                {framework.icon}
              </div>
              <div className={styles.title}>
                <Text size={400} weight="semibold">
                  {framework.name}
                </Text>
              </div>
              <div className={styles.description}>
                <Text size={200}>{description}</Text>
              </div>
              <div className={styles.actions}>
                <Button
                  appearance="subtle"
                  icon={<ArrowRightRegular />}
                  iconPosition="after"
                  disabled={disabled}
                >
                  Apply
                </Button>
              </div>
            </div>
          </Card>
        );
      })}
    </div>
  );
};
