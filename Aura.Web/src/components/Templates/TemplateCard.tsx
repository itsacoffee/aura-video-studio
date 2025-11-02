/**
 * Card component for displaying a template in the library
 */

import {
  Card,
  CardHeader,
  CardPreview,
  Text,
  Badge,
  Button,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Play24Regular, Star24Filled } from '@fluentui/react-icons';
import { memo } from 'react';
import { TemplateListItem } from '../../types/templates';
import { LazyImage } from '../common/LazyImage';

const useStyles = makeStyles({
  card: {
    width: '280px',
    height: '340px',
    cursor: 'pointer',
    transition: 'transform 0.2s ease, box-shadow 0.2s ease',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  preview: {
    height: '180px',
    backgroundColor: tokens.colorNeutralBackground3,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
    overflow: 'hidden',
  },
  previewImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  playButton: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: '50%',
    padding: tokens.spacingVerticalM,
    opacity: 0,
    transition: 'opacity 0.2s ease',
    ':hover': {
      opacity: 1,
    },
  },
  cardHeader: {
    padding: tokens.spacingVerticalM,
  },
  titleRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalXS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    lineHeight: tokens.lineHeightBase300,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    lineHeight: tokens.lineHeightBase200,
    marginBottom: tokens.spacingVerticalS,
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
  },
  stats: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  rating: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  categoryBadge: {
    fontSize: tokens.fontSizeBase100,
  },
  tags: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalXS,
  },
});

export interface TemplateCardProps {
  template: TemplateListItem;
  onClick: (template: TemplateListItem) => void;
  onPreview?: (template: TemplateListItem) => void;
}

function TemplateCardComponent({ template, onClick, onPreview }: TemplateCardProps) {
  const styles = useStyles();

  const handlePreviewClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onPreview?.(template);
  };

  return (
    <Card className={styles.card} onClick={() => onClick(template)}>
      <CardPreview className={styles.preview}>
        {template.previewImage ? (
          <LazyImage
            src={template.previewImage}
            alt={template.name}
            width="100%"
            height="180px"
            className={styles.previewImage}
            placeholderText="Loading preview..."
          />
        ) : (
          <Text>No preview available</Text>
        )}
        {onPreview && template.previewVideo && (
          <Button
            icon={<Play24Regular />}
            appearance="transparent"
            className={styles.playButton}
            onClick={handlePreviewClick}
            aria-label="Preview template"
          />
        )}
      </CardPreview>
      <CardHeader
        header={
          <div className={styles.cardHeader}>
            <div className={styles.titleRow}>
              <Text className={styles.title}>{template.name}</Text>
              <Badge appearance="outline" className={styles.categoryBadge}>
                {template.subCategory || template.category}
              </Badge>
            </div>
            <Text className={styles.description}>{template.description}</Text>
            <div className={styles.footer}>
              <div className={styles.stats}>
                <div className={styles.rating}>
                  <Star24Filled color={tokens.colorPaletteYellowForeground1} />
                  <Text>{template.rating.toFixed(1)}</Text>
                </div>
                <Text>•</Text>
                <Text>{template.usageCount} uses</Text>
              </div>
            </div>
            {template.tags.length > 0 && (
              <div className={styles.tags}>
                {template.tags.slice(0, 3).map((tag) => (
                  <Badge key={tag} size="small" appearance="tint">
                    {tag}
                  </Badge>
                ))}
              </div>
            )}
          </div>
        }
      />
    </Card>
  );
}

export const TemplateCard = memo(TemplateCardComponent);
