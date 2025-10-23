import React, { useState } from 'react';
import { Card, Text, Button, makeStyles, tokens, Badge, Spinner } from '@fluentui/react-components';
import { CheckmarkCircleRegular, DismissCircleRegular, InfoRegular } from '@fluentui/react-icons';
import type {
  EnhancementSuggestion,
  SuggestionType,
} from '../../services/scriptEnhancementService';
import { scriptEnhancementService } from '../../services/scriptEnhancementService';

const useStyles = makeStyles({
  card: {
    marginBottom: tokens.spacingVerticalM,
    transition: 'all 0.2s ease',
    '&:hover': {
      boxShadow: tokens.shadow8,
    },
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  typeIcon: {
    fontSize: '20px',
    marginRight: tokens.spacingHorizontalXS,
  },
  originalText: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalS,
    fontFamily: 'monospace',
    fontSize: '12px',
  },
  suggestedText: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalS,
    fontFamily: 'monospace',
    fontSize: '12px',
  },
  explanation: {
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground2,
  },
  benefits: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginBottom: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
  confidence: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
});

interface SuggestionCardProps {
  suggestion: EnhancementSuggestion;
  onAccept?: (suggestionId: string) => void;
  onReject?: (suggestionId: string) => void;
}

export const SuggestionCard: React.FC<SuggestionCardProps> = ({
  suggestion,
  onAccept,
  onReject,
}) => {
  const styles = useStyles();
  const [isProcessing, setIsProcessing] = useState(false);

  const handleAccept = async () => {
    if (!onAccept) return;
    setIsProcessing(true);
    try {
      await onAccept(suggestion.suggestionId);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleReject = async () => {
    if (!onReject) return;
    setIsProcessing(true);
    try {
      await onReject(suggestion.suggestionId);
    } finally {
      setIsProcessing(false);
    }
  };

  const typeIcon = scriptEnhancementService.getSuggestionTypeIcon(
    suggestion.type as SuggestionType
  );

  const confidenceColor =
    suggestion.confidenceScore >= 70
      ? 'success'
      : suggestion.confidenceScore >= 50
        ? 'warning'
        : 'danger';

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <span className={styles.typeIcon}>{typeIcon}</span>
          <Text weight="semibold">{suggestion.type}</Text>
        </div>
        <div className={styles.confidence}>
          <Badge color={confidenceColor} appearance="filled">
            {Math.round(suggestion.confidenceScore)}% confident
          </Badge>
        </div>
      </div>

      {suggestion.originalText && suggestion.originalText !== 'Overall structure' && (
        <div>
          <Text size={200} weight="semibold">
            Original:
          </Text>
          <div className={styles.originalText}>{suggestion.originalText}</div>
        </div>
      )}

      <div>
        <Text size={200} weight="semibold">
          Suggestion:
        </Text>
        <div className={styles.suggestedText}>{suggestion.suggestedText}</div>
      </div>

      <div className={styles.explanation}>
        <InfoRegular style={{ marginRight: tokens.spacingHorizontalXXS }} />
        <Text size={200}>{suggestion.explanation}</Text>
      </div>

      <div className={styles.benefits}>
        {suggestion.benefits.map((benefit, index) => (
          <Badge key={index} appearance="outline" color="brand">
            {benefit}
          </Badge>
        ))}
      </div>

      <div className={styles.actions}>
        {isProcessing ? (
          <Spinner size="small" />
        ) : (
          <>
            {onReject && (
              <Button appearance="subtle" icon={<DismissCircleRegular />} onClick={handleReject}>
                Reject
              </Button>
            )}
            {onAccept && (
              <Button appearance="primary" icon={<CheckmarkCircleRegular />} onClick={handleAccept}>
                Accept
              </Button>
            )}
          </>
        )}
      </div>
    </Card>
  );
};
