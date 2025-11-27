import {
  Card,
  Text,
  makeStyles,
  tokens,
  Badge,
  Button,
  Tooltip,
  Divider,
} from '@fluentui/react-components';
import {
  ChevronDownRegular,
  ChevronUpRegular,
  PlayRegular,
  BookmarkRegular,
  LightbulbRegular,
  TargetRegular,
  SparkleRegular,
  MoneyRegular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import type { ConceptIdea } from '../../services/ideationService';
import { ideationService } from '../../services/ideationService';

// Constants for consistent styling
const INSIGHT_MAX_WIDTH = '150px';
const INSIGHT_TRUNCATE_LENGTH = 40;

const useStyles = makeStyles({
  card: {
    width: '100%',
    cursor: 'pointer',
    transition: 'all 0.15s ease',
    padding: tokens.spacingVerticalS,
    '&:hover': {
      transform: 'translateY(-1px)',
      boxShadow: tokens.shadow8,
    },
  },
  // Header row with title, angle badge, and scores
  headerRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalXS,
  },
  titleSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
    minWidth: 0,
  },
  icon: {
    fontSize: '18px',
    flexShrink: 0,
  },
  title: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    flex: 1,
  },
  angleBadge: {
    flexShrink: 0,
  },
  scoresSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    flexShrink: 0,
  },
  scoreBadge: {
    fontSize: tokens.fontSizeBase100,
    padding: '2px 6px',
  },
  // Quick info row with audience, hook, and unique value
  quickInfoRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    flexWrap: 'wrap',
  },
  quickInfoItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    maxWidth: '200px',
  },
  quickInfoIcon: {
    fontSize: '12px',
    flexShrink: 0,
  },
  quickInfoText: {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  // Description and key insights
  descriptionSection: {
    marginBottom: tokens.spacingVerticalXS,
  },
  description: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    lineHeight: tokens.lineHeightBase200,
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
  },
  keyInsightsRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  insightBullet: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  bulletDot: {
    width: '4px',
    height: '4px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandForeground1,
  },
  // Footer with actions
  footerRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalS,
    paddingTop: tokens.spacingVerticalXS,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  tagsSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  tagBadge: {
    fontSize: tokens.fontSizeBase100,
  },
  actionsSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actionButton: {
    minWidth: 'auto',
    padding: '4px 8px',
    fontSize: tokens.fontSizeBase200,
  },
  // Expanded content
  expandedContent: {
    marginTop: tokens.spacingVerticalS,
    paddingTop: tokens.spacingVerticalS,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  expandedSection: {
    marginBottom: tokens.spacingVerticalS,
  },
  expandedSectionTitle: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXXS,
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  expandedList: {
    margin: 0,
    paddingLeft: tokens.spacingHorizontalL,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    lineHeight: tokens.lineHeightBase300,
  },
  hookText: {
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
    color: tokens.colorBrandForeground1,
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalXS + ' ' + tokens.spacingHorizontalS,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandForeground1}`,
  },
  prosConsRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
  },
});

interface ConceptCardProps {
  concept: ConceptIdea;
  onSelect?: (concept: ConceptIdea) => void;
  onExpand?: (concept: ConceptIdea) => void;
  onUseForVideo?: (concept: ConceptIdea) => void;
  onSave?: (concept: ConceptIdea) => void;
}

export const ConceptCard: React.FC<ConceptCardProps> = ({
  concept,
  onSelect,
  onExpand,
  onUseForVideo,
  onSave,
}) => {
  const styles = useStyles();
  const [isExpanded, setIsExpanded] = useState(false);

  const appealScoreColor = ideationService.getAppealScoreColor(concept.appealScore);
  const viralityScoreColor = ideationService.getViralityScoreColor(concept.viralityScore);
  const angleIcon = ideationService.getAngleIcon(concept.angle);
  const monetizationIcon = ideationService.getMonetizationIcon(concept.monetizationPotential);

  const handleClick = () => {
    if (onSelect) {
      onSelect(concept);
    }
  };

  const handleExpand = (e: React.MouseEvent) => {
    e.stopPropagation();
    setIsExpanded(!isExpanded);
    if (onExpand && !isExpanded) {
      onExpand(concept);
    }
  };

  const handleUseForVideo = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onUseForVideo) {
      onUseForVideo(concept);
    }
  };

  const handleSave = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onSave) {
      onSave(concept);
    }
  };

  // Get first 3 key insights for display
  const displayInsights =
    concept.keyInsights?.slice(0, 3) || concept.talkingPoints?.slice(0, 3) || [];

  return (
    <Card className={styles.card} onClick={handleClick}>
      {/* Header Row: Icon, Title, Angle Badge, Scores */}
      <div className={styles.headerRow}>
        <div className={styles.titleSection}>
          <span className={styles.icon}>{angleIcon}</span>
          <Tooltip content={concept.title} relationship="label">
            <Text className={styles.title}>{concept.title}</Text>
          </Tooltip>
          <Badge className={styles.angleBadge} appearance="outline" size="small">
            {concept.angle}
          </Badge>
        </div>
        <div className={styles.scoresSection}>
          <Tooltip
            content={`Appeal Score: ${Math.round(concept.appealScore)}/100`}
            relationship="label"
          >
            <Badge
              className={styles.scoreBadge}
              appearance="filled"
              style={{ backgroundColor: appealScoreColor, color: 'white' }}
            >
              {Math.round(concept.appealScore)}
            </Badge>
          </Tooltip>
          {concept.viralityScore !== undefined && concept.viralityScore > 0 && (
            <Tooltip
              content={`Virality Score: ${Math.round(concept.viralityScore)}/100`}
              relationship="label"
            >
              <Badge
                className={styles.scoreBadge}
                appearance="filled"
                style={{ backgroundColor: viralityScoreColor, color: 'white' }}
              >
                🔥 {Math.round(concept.viralityScore)}
              </Badge>
            </Tooltip>
          )}
        </div>
      </div>

      {/* Quick Info Row: Audience, Hook hint, Unique Value */}
      <div className={styles.quickInfoRow}>
        <Tooltip content={concept.targetAudience} relationship="label">
          <div className={styles.quickInfoItem}>
            <TargetRegular className={styles.quickInfoIcon} />
            <span className={styles.quickInfoText}>{concept.targetAudience}</span>
          </div>
        </Tooltip>
        {concept.uniqueValue && (
          <Tooltip content={concept.uniqueValue} relationship="label">
            <div className={styles.quickInfoItem}>
              <SparkleRegular className={styles.quickInfoIcon} />
              <span className={styles.quickInfoText}>{concept.uniqueValue}</span>
            </div>
          </Tooltip>
        )}
        {concept.monetizationPotential && (
          <Tooltip content={`Monetization: ${concept.monetizationPotential}`} relationship="label">
            <div className={styles.quickInfoItem}>
              <MoneyRegular className={styles.quickInfoIcon} />
              <span className={styles.quickInfoText}>{monetizationIcon}</span>
            </div>
          </Tooltip>
        )}
      </div>

      {/* Description */}
      <div className={styles.descriptionSection}>
        <Text className={styles.description}>{concept.description}</Text>
      </div>

      {/* Key Insights Row */}
      {displayInsights.length > 0 && (
        <div className={styles.keyInsightsRow}>
          <LightbulbRegular className={styles.quickInfoIcon} />
          {displayInsights.map((insight, index) => (
            <React.Fragment key={index}>
              <span className={styles.insightBullet}>
                <span className={styles.bulletDot} />
                <span className={styles.quickInfoText} style={{ maxWidth: INSIGHT_MAX_WIDTH }}>
                  {insight.length > INSIGHT_TRUNCATE_LENGTH
                    ? insight.substring(0, INSIGHT_TRUNCATE_LENGTH) + '...'
                    : insight}
                </span>
              </span>
            </React.Fragment>
          ))}
        </div>
      )}

      {/* Footer Row: Tags and Actions */}
      <div className={styles.footerRow}>
        <div className={styles.tagsSection}>
          {concept.contentGap && (
            <Tooltip content={`Content Gap: ${concept.contentGap}`} relationship="label">
              <Badge
                className={styles.tagBadge}
                appearance="outline"
                size="small"
                color="informative"
              >
                Gap Filler
              </Badge>
            </Tooltip>
          )}
        </div>
        <div className={styles.actionsSection}>
          <Button
            className={styles.actionButton}
            appearance="subtle"
            size="small"
            icon={isExpanded ? <ChevronUpRegular /> : <ChevronDownRegular />}
            onClick={handleExpand}
          >
            {isExpanded ? 'Less' : 'More'}
          </Button>
          {onUseForVideo && (
            <Button
              className={styles.actionButton}
              appearance="primary"
              size="small"
              icon={<PlayRegular />}
              onClick={handleUseForVideo}
            >
              Use
            </Button>
          )}
          {onSave && (
            <Button
              className={styles.actionButton}
              appearance="subtle"
              size="small"
              icon={<BookmarkRegular />}
              onClick={handleSave}
            />
          )}
        </div>
      </div>

      {/* Expanded Content */}
      {isExpanded && (
        <div className={styles.expandedContent}>
          {/* Hook */}
          {concept.hook && (
            <div className={styles.expandedSection}>
              <Text className={styles.expandedSectionTitle}>⚡ Hook</Text>
              <div className={styles.hookText}>&quot;{concept.hook}&quot;</div>
            </div>
          )}

          {/* Unique Value & Content Gap */}
          {(concept.uniqueValue || concept.contentGap) && (
            <div className={styles.expandedSection}>
              {concept.uniqueValue && (
                <>
                  <Text className={styles.expandedSectionTitle}>
                    <SparkleRegular /> Unique Value
                  </Text>
                  <Text
                    style={{
                      fontSize: tokens.fontSizeBase200,
                      color: tokens.colorNeutralForeground2,
                    }}
                  >
                    {concept.uniqueValue}
                  </Text>
                </>
              )}
              {concept.contentGap && (
                <>
                  <Text
                    className={styles.expandedSectionTitle}
                    style={{ marginTop: tokens.spacingVerticalXS }}
                  >
                    🎯 Content Gap
                  </Text>
                  <Text
                    style={{
                      fontSize: tokens.fontSizeBase200,
                      color: tokens.colorNeutralForeground2,
                    }}
                  >
                    {concept.contentGap}
                  </Text>
                </>
              )}
            </div>
          )}

          {/* Key Insights / Talking Points */}
          {(concept.keyInsights || concept.talkingPoints) && (
            <div className={styles.expandedSection}>
              <Text className={styles.expandedSectionTitle}>
                <LightbulbRegular /> {concept.keyInsights ? 'Key Insights' : 'Talking Points'}
              </Text>
              <ul className={styles.expandedList}>
                {(concept.keyInsights || concept.talkingPoints || []).map((point, index) => (
                  <li key={index}>{point}</li>
                ))}
              </ul>
            </div>
          )}

          {/* Visual Suggestions */}
          {concept.visualSuggestions && concept.visualSuggestions.length > 0 && (
            <div className={styles.expandedSection}>
              <Text className={styles.expandedSectionTitle}>🎨 Visual Suggestions</Text>
              <ul className={styles.expandedList}>
                {concept.visualSuggestions.map((visual, index) => (
                  <li key={index}>{visual}</li>
                ))}
              </ul>
            </div>
          )}

          <Divider style={{ margin: `${tokens.spacingVerticalS} 0` }} />

          {/* Pros and Cons */}
          <div className={styles.prosConsRow}>
            <div className={styles.expandedSection}>
              <Text
                className={styles.expandedSectionTitle}
                style={{ color: tokens.colorPaletteGreenForeground1 }}
              >
                ✓ Pros
              </Text>
              <ul className={styles.expandedList}>
                {concept.pros.slice(0, 3).map((pro, index) => (
                  <li key={index}>{pro}</li>
                ))}
              </ul>
            </div>
            <div className={styles.expandedSection}>
              <Text
                className={styles.expandedSectionTitle}
                style={{ color: tokens.colorPaletteRedForeground1 }}
              >
                ✗ Cons
              </Text>
              <ul className={styles.expandedList}>
                {concept.cons.slice(0, 3).map((con, index) => (
                  <li key={index}>{con}</li>
                ))}
              </ul>
            </div>
          </div>

          {/* Monetization */}
          {concept.monetizationPotential && (
            <div className={styles.expandedSection}>
              <Text className={styles.expandedSectionTitle}>💰 Monetization Potential</Text>
              <Text
                style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground2 }}
              >
                {concept.monetizationPotential}
              </Text>
            </div>
          )}
        </div>
      )}
    </Card>
  );
};
