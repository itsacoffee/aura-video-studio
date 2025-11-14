import {
  makeStyles,
  tokens,
  TabList,
  Tab,
  Text,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Link,
  Button,
} from '@fluentui/react-components';
import { Info24Regular, LockClosed24Regular } from '@fluentui/react-icons';
import { useState, useEffect, type FC } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAdvancedMode } from '../../hooks/useAdvancedMode';
import { useMLLabStore } from '../../state/mlLab';
import { AnnotationTab } from './AnnotationTab';
import { TrainingTab } from './TrainingTab';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalXL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalL,
  },
  warningBanner: {
    marginBottom: tokens.spacingVerticalM,
  },
  tabsContainer: {
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
    padding: `0 ${tokens.spacingHorizontalXL}`,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalXL,
  },
  blockedContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    padding: tokens.spacingVerticalXXXL,
    textAlign: 'center',
    gap: tokens.spacingVerticalXL,
  },
  blockedIcon: {
    fontSize: '72px',
    color: tokens.colorPaletteRedForeground1,
  },
  blockedTitle: {
    fontSize: tokens.fontSizeHero900,
    fontWeight: tokens.fontWeightSemibold,
  },
  blockedDescription: {
    maxWidth: '600px',
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorNeutralForeground2,
    lineHeight: '1.6',
  },
  blockedActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
});

export const MLLabPage: FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const [advancedMode] = useAdvancedMode();
  const { currentTab, setCurrentTab, checkSystemCapabilities, systemCapabilities } =
    useMLLabStore();
  const [selectedTab, setSelectedTab] = useState(currentTab);

  useEffect(() => {
    if (advancedMode) {
      checkSystemCapabilities();
    }
  }, [advancedMode, checkSystemCapabilities]);

  const handleTabSelect = (tabValue: string) => {
    const tab = tabValue as 'annotation' | 'training';
    setSelectedTab(tab);
    setCurrentTab(tab);
  };

  const handleGoToSettings = () => {
    navigate('/settings');
  };

  const handleGoBack = () => {
    navigate('/');
  };

  const hasWarnings = systemCapabilities && systemCapabilities.warnings.length > 0;

  // Show blocked state if advanced mode is not enabled
  if (!advancedMode) {
    return (
      <div className={styles.container}>
        <div className={styles.blockedContainer}>
          <LockClosed24Regular className={styles.blockedIcon} />
          <Text className={styles.blockedTitle}>Advanced Mode Required</Text>
          <Text className={styles.blockedDescription}>
            The ML Lab is an advanced feature that requires Advanced Mode to be enabled. Advanced
            Mode unlocks expert-level features including in-app ML model training, deep prompt
            customization, and low-level render controls.
          </Text>
          <Text className={styles.blockedDescription}>
            To access this feature, enable Advanced Mode in Settings &gt; General. This feature
            assumes familiarity with machine learning concepts and video processing workflows.
          </Text>
          <div className={styles.blockedActions}>
            <Button appearance="primary" onClick={handleGoToSettings}>
              Go to Settings
            </Button>
            <Button appearance="secondary" onClick={handleGoBack}>
              Go Back
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>ML Lab (Advanced)</Text>
        <Text className={styles.subtitle}>
          Annotate video frames and retrain the frame importance model with your own data
        </Text>

        {hasWarnings && (
          <MessageBar intent="warning" className={styles.warningBanner}>
            <MessageBarBody>
              <MessageBarTitle>System Warnings</MessageBarTitle>
              <ul style={{ margin: 0, paddingLeft: '20px' }}>
                {systemCapabilities.warnings.map((warning, idx) => (
                  <li key={`warning-${warning.substring(0, 30)}-${idx}`}>{warning}</li>
                ))}
              </ul>
              Training may take longer or require adjustments to complete successfully.
            </MessageBarBody>
          </MessageBar>
        )}

        <MessageBar intent="info" icon={<Info24Regular />}>
          <MessageBarBody>
            <MessageBarTitle>Important: Training Requirements</MessageBarTitle>
            Training a custom model requires significant computational resources and time.
            Recommended: 100+ annotated frames, 8GB+ RAM, dedicated GPU. Training can take 10-60
            minutes depending on your system. You can always{' '}
            <Link href="#" onClick={(e) => e.preventDefault()}>
              revert to the default model
            </Link>{' '}
            if needed.
          </MessageBarBody>
        </MessageBar>
      </div>

      <div className={styles.tabsContainer}>
        <TabList
          selectedValue={selectedTab}
          onTabSelect={(_, data) => handleTabSelect(data.value as string)}
        >
          <Tab value="annotation">Annotate Frames</Tab>
          <Tab value="training">Train Model</Tab>
        </TabList>
      </div>

      <div className={styles.content}>
        {selectedTab === 'annotation' && <AnnotationTab />}
        {selectedTab === 'training' && <TrainingTab />}
      </div>
    </div>
  );
};
