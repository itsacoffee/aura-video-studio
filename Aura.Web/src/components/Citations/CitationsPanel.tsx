import {
  Badge,
  Body1,
  Caption1,
  Card,
  makeStyles,
  Text,
  Title3,
  tokens,
} from '@fluentui/react-components';
import { DocumentText24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import type { Citation } from '../../types';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  citationList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  citation: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'background-color 0.2s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  citationNumber: {
    marginRight: tokens.spacingHorizontalS,
    fontWeight: tokens.fontWeightSemibold,
  },
  citationMeta: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface CitationsPanelProps {
  citations: Citation[];
  onCitationClick?: (citation: Citation) => void;
}

export const CitationsPanel: FC<CitationsPanelProps> = ({ citations, onCitationClick }) => {
  const styles = useStyles();

  if (citations.length === 0) {
    return (
      <Card className={styles.container}>
        <div className={styles.header}>
          <DocumentText24Regular />
          <Title3>Citations</Title3>
        </div>
        <div className={styles.emptyState}>
          <Body1>No citations available</Body1>
          <Caption1>Enable RAG and attach source documents to see citations</Caption1>
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <DocumentText24Regular />
        <Title3>Citations</Title3>
        <Badge appearance="filled" color="informative">
          {citations.length}
        </Badge>
      </div>
      <div className={styles.citationList}>
        {citations.map((citation) => (
          <Card
            key={citation.number}
            className={styles.citation}
            onClick={() => onCitationClick?.(citation)}
          >
            <Text className={styles.citationNumber}>[{citation.number}]</Text>
            <Body1>{citation.title || citation.source}</Body1>
            <div className={styles.citationMeta}>
              {citation.source && citation.source !== citation.title && (
                <Caption1>
                  <strong>Source:</strong> {citation.source}
                </Caption1>
              )}
              {citation.section && (
                <Caption1>
                  <strong>Section:</strong> {citation.section}
                </Caption1>
              )}
              {citation.pageNumber !== undefined && citation.pageNumber !== null && (
                <Caption1>
                  <strong>Page:</strong> {citation.pageNumber}
                </Caption1>
              )}
            </div>
          </Card>
        ))}
      </div>
    </Card>
  );
};
