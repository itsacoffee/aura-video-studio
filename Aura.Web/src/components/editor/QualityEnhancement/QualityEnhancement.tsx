/**
 * Quality Enhancement Component
 * Main component for AI-powered visual quality enhancement
 */

import React, { useState } from 'react';
import {
  Card,
  makeStyles,
  tokens,
  Button,
  Tab,
  TabList,
  Body1,
  Title3,
  Divider,
} from '@fluentui/react-components';
import {
  Sparkle24Regular,
  Color24Regular,
  Grid24Regular,
  Eye24Regular,
  Video24Regular,
} from '@fluentui/react-icons';
import { ColorGradingPanel } from './ColorGradingPanel';
import { CompositionPanel } from './CompositionPanel';
import { VisualCoherencePanel } from './VisualCoherencePanel';
import { QualityAssessmentPanel } from './QualityAssessmentPanel';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  titleSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  content: {
    marginTop: tokens.spacingVerticalM,
  },
});

interface QualityEnhancementProps {
  sceneIndex?: number;
  onApplyEnhancement?: (enhancement: any) => void;
}

type TabValue = 'colorGrading' | 'composition' | 'coherence' | 'quality';

export const QualityEnhancement: React.FC<QualityEnhancementProps> = ({
  sceneIndex,
  onApplyEnhancement,
}) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<TabValue>('quality');
  const [isEnhancing, setIsEnhancing] = useState(false);

  const handleApplyAll = async () => {
    setIsEnhancing(true);
    try {
      // Apply all enhancements
      // Implementation would apply all selected enhancements
      onApplyEnhancement?.({
        type: 'all',
        sceneIndex,
      });
    } catch (error) {
      console.error('Failed to apply enhancements:', error);
    } finally {
      setIsEnhancing(false);
    }
  };

  return (
    <div className={styles.container}>
      <Card>
        <div style={{ padding: tokens.spacingVerticalL }}>
          <div className={styles.header}>
            <div className={styles.titleSection}>
              <Sparkle24Regular />
              <Title3>AI Quality Enhancement</Title3>
            </div>
            <Button appearance="primary" onClick={handleApplyAll} disabled={isEnhancing}>
              {isEnhancing ? 'Enhancing...' : 'Apply All Enhancements'}
            </Button>
          </div>

          <Body1>
            Automatically improve visual aesthetics, composition, and quality using AI-powered
            analysis.
          </Body1>

          <Divider
            style={{ marginTop: tokens.spacingVerticalL, marginBottom: tokens.spacingVerticalL }}
          />

          <TabList
            selectedValue={selectedTab}
            onTabSelect={(_e, data) => setSelectedTab(data.value as TabValue)}
          >
            <Tab value="quality" icon={<Eye24Regular />}>
              Quality Assessment
            </Tab>
            <Tab value="colorGrading" icon={<Color24Regular />}>
              Color Grading
            </Tab>
            <Tab value="composition" icon={<Grid24Regular />}>
              Composition
            </Tab>
            <Tab value="coherence" icon={<Video24Regular />}>
              Visual Coherence
            </Tab>
          </TabList>

          <div className={styles.content}>
            {selectedTab === 'quality' && (
              <QualityAssessmentPanel
                sceneIndex={sceneIndex}
                onApplyEnhancement={onApplyEnhancement}
              />
            )}
            {selectedTab === 'colorGrading' && (
              <ColorGradingPanel sceneIndex={sceneIndex} onApplyEnhancement={onApplyEnhancement} />
            )}
            {selectedTab === 'composition' && (
              <CompositionPanel sceneIndex={sceneIndex} onApplyEnhancement={onApplyEnhancement} />
            )}
            {selectedTab === 'coherence' && (
              <VisualCoherencePanel onApplyEnhancement={onApplyEnhancement} />
            )}
          </div>
        </div>
      </Card>
    </div>
  );
};

export default QualityEnhancement;
