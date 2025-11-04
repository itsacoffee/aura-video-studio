import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
} from '@fluentui/react-components';
import { Dismiss24Regular, ThumbLike24Regular, ThumbDislike24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { guidedModeService } from '../../services/guidedModeService';
import { useGuidedMode } from '../../state/guidedMode';

const useStyles = makeStyles({
  panel: {
    position: 'fixed',
    right: tokens.spacingHorizontalL,
    top: '80px',
    width: '400px',
    maxHeight: '80vh',
    overflowY: 'auto',
    zIndex: 1000,
    boxShadow: tokens.shadow64,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  content: {
    marginBottom: tokens.spacingVerticalL,
  },
  keyPoints: {
    listStyleType: 'disc',
    paddingLeft: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalM,
  },
  keyPoint: {
    marginBottom: tokens.spacingVerticalS,
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  feedbackButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

export const ExplanationPanel: FC = () => {
  const styles = useStyles();
  const { explanationPanel, hideExplanation } = useGuidedMode();

  if (!explanationPanel?.visible) {
    return null;
  }

  const handleFeedback = async (rating: 'positive' | 'negative') => {
    await guidedModeService.trackFeedback('explain', explanationPanel.artifactType, rating);
  };

  return (
    <Card className={styles.panel}>
      <div className={styles.header}>
        <Title3>Understanding Your {explanationPanel.artifactType}</Title3>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          onClick={hideExplanation}
          aria-label="Close explanation"
        />
      </div>

      {explanationPanel.loading ? (
        <div className={styles.loadingContainer}>
          <Spinner label="Generating explanation..." />
        </div>
      ) : (
        <>
          <div className={styles.content}>
            {explanationPanel.response?.explanation && (
              <Text>{explanationPanel.response.explanation}</Text>
            )}

            {explanationPanel.response?.keyPoints &&
              explanationPanel.response.keyPoints.length > 0 && (
                <ul className={styles.keyPoints}>
                  {explanationPanel.response.keyPoints.map((point, index) => (
                    <li key={index} className={styles.keyPoint}>
                      <Text>{point}</Text>
                    </li>
                  ))}
                </ul>
              )}
          </div>

          <div className={styles.footer}>
            <Text size={300}>Was this helpful?</Text>
            <div className={styles.feedbackButtons}>
              <Button
                appearance="subtle"
                icon={<ThumbLike24Regular />}
                onClick={() => handleFeedback('positive')}
                aria-label="This was helpful"
              />
              <Button
                appearance="subtle"
                icon={<ThumbDislike24Regular />}
                onClick={() => handleFeedback('negative')}
                aria-label="This was not helpful"
              />
            </div>
          </div>
        </>
      )}
    </Card>
  );
};
