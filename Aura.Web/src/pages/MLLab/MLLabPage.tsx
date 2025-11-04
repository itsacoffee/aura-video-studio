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
} from '@fluentui/react-components';
import { Info24Regular } from '@fluentui/react-icons';
import { useState, useEffect, type FC } from 'react';
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
});

export const MLLabPage: FC = () => {
  const styles = useStyles();
  const { currentTab, setCurrentTab, checkSystemCapabilities, systemCapabilities } =
    useMLLabStore();
  const [selectedTab, setSelectedTab] = useState(currentTab);

  useEffect(() => {
    checkSystemCapabilities();
  }, [checkSystemCapabilities]);

  const handleTabSelect = (tabValue: string) => {
    const tab = tabValue as 'annotation' | 'training';
    setSelectedTab(tab);
    setCurrentTab(tab);
  };

  const hasWarnings = systemCapabilities && systemCapabilities.warnings.length > 0;

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
                  <li key={idx}>{warning}</li>
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
