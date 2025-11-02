import { Card, Text, Button, makeStyles, tokens } from '@fluentui/react-components';
import { KeyRegular, SettingsRegular } from '@fluentui/react-icons';
import React from 'react';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
  card: {
    maxWidth: '600px',
    width: '100%',
    padding: tokens.spacingVerticalXXL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    textAlign: 'center',
    gap: tokens.spacingVerticalL,
  },
  icon: {
    fontSize: '64px',
    color: tokens.colorNeutralForeground2,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalS,
  },
  message: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    lineHeight: tokens.lineHeightBase300,
    marginBottom: tokens.spacingVerticalM,
  },
  providerList: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontStyle: 'italic',
    marginBottom: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

interface MissingApiKeyStateProps {
  featureName: string;
  requiredProviders?: string[];
  description?: string;
}

export const MissingApiKeyState: React.FC<MissingApiKeyStateProps> = ({
  featureName,
  requiredProviders = ['OpenAI', 'Anthropic (Claude)', 'Google (Gemini)'],
  description,
}) => {
  const styles = useStyles();
  const navigate = useNavigate();

  const handleNavigateToSettings = () => {
    navigate('/settings');
  };

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <KeyRegular className={styles.icon} />

          <div>
            <div className={styles.title}>API Key Required</div>
            <Text className={styles.message}>
              {description ||
                `${featureName} requires an API key to fetch trending topics and generate content suggestions.`}
            </Text>
            <Text className={styles.providerList}>
              Supported providers: {requiredProviders.join(', ')}
            </Text>
          </div>

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={<SettingsRegular />}
              onClick={handleNavigateToSettings}
            >
              Configure API Keys
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
};
