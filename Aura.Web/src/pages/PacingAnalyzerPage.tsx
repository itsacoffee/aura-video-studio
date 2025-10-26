import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Body1,
  Button,
  Card,
  Textarea,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import { FlashFlow24Regular, Play24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { PacingOptimizerPanel } from '../components/PacingAnalysis';
import type { Brief } from '../types';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
    flexWrap: 'wrap',
    gap: tokens.spacingVerticalM,
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  instructionCard: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  quickStartCard: {
    padding: tokens.spacingVerticalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  textareaContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  featureList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalL,
  },
  infoMessage: {
    marginBottom: tokens.spacingVerticalL,
  },
});

export function PacingAnalyzerPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [scriptText, setScriptText] = useState('');
  const [showAnalysisPanel, setShowAnalysisPanel] = useState(false);

  const handleAnalyze = () => {
    // Show inline pacing analysis panel instead of navigating
    setShowAnalysisPanel(true);
  };

  const handleGoToEditor = () => {
    navigate('/editor');
  };

  // Create a minimal brief for pacing analysis
  const brief: Brief = {
    topic: 'Script Analysis',
    audience: 'General',
    goal: 'Inform',
    tone: 'Informative',
    language: 'en-US',
    aspect: 'Widescreen16x9',
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>
            <FlashFlow24Regular style={{ marginRight: '8px', verticalAlign: 'middle' }} />
            Pacing Analyzer
          </Title1>
          <Body1 className={styles.subtitle}>
            Optimize video pacing for maximum engagement and retention
          </Body1>
        </div>
      </div>

      <MessageBar intent="info" className={styles.infoMessage}>
        <MessageBarBody>
          <MessageBarTitle>AI-Powered Pacing Analysis</MessageBarTitle>
          The Pacing Analyzer helps you optimize scene timing, transitions, and flow for your target
          platform. It analyzes your script and provides data-driven recommendations to maximize
          viewer engagement.
        </MessageBarBody>
      </MessageBar>

      <div className={styles.section}>
        <Card className={styles.instructionCard}>
          <Title2>How It Works</Title2>
          <div className={styles.featureList}>
            <Body1>
              <strong>• Platform Optimization:</strong> Tailored recommendations for YouTube,
              TikTok, Instagram Reels, and more
            </Body1>
            <Body1>
              <strong>• Attention Curve Analysis:</strong> Visualize predicted viewer engagement
              throughout your video
            </Body1>
            <Body1>
              <strong>• Scene Timing:</strong> Get optimal duration suggestions for each scene based
              on content and platform
            </Body1>
            <Body1>
              <strong>• Retention Predictions:</strong> Estimate viewer retention rates with
              AI-driven analysis
            </Body1>
            <Body1>
              <strong>• Smart Transitions:</strong> Recommendations for cuts, pacing changes, and
              hooks to maintain engagement
            </Body1>
          </div>
        </Card>

        <Card className={styles.quickStartCard}>
          <Title2>Quick Start</Title2>
          <Body1>Paste your script below to get started with pacing analysis:</Body1>
          <div className={styles.textareaContainer}>
            <Textarea
              placeholder="Enter your video script here... The analyzer will break it down and provide timing recommendations for each scene."
              value={scriptText}
              onChange={(_, data) => setScriptText(data.value)}
              rows={8}
              resize="vertical"
            />
          </div>
          <div className={styles.actionButtons}>
            <Button
              appearance="primary"
              icon={<Play24Regular />}
              onClick={handleAnalyze}
              disabled={!scriptText.trim()}
              size="large"
            >
              Analyze Pacing
            </Button>
            <Button appearance="secondary" onClick={() => navigate('/create')} size="large">
              Go to Create Wizard
            </Button>
          </div>
        </Card>

        <Card className={styles.instructionCard}>
          <Title2>Available Through</Title2>
          <div className={styles.featureList}>
            <Body1>
              <strong>• Create Wizard:</strong> Access pacing analysis during video creation
            </Body1>
            <Body1>
              <strong>• Timeline Editor:</strong> Analyze and optimize existing video timelines
            </Body1>
            <Body1>
              <strong>• Standalone Analysis:</strong> Use this page for quick script analysis
            </Body1>
          </div>
          <div className={styles.actionButtons}>
            <Button appearance="secondary" onClick={handleGoToEditor}>
              Open Timeline Editor
            </Button>
          </div>
        </Card>
      </div>

      {/* Inline Pacing Analysis Panel */}
      {showAnalysisPanel && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: tokens.colorNeutralBackground1,
            zIndex: 1000,
            overflow: 'auto',
          }}
        >
          <PacingOptimizerPanel
            script={scriptText}
            scenes={[]} // Empty scenes - analyzer will parse script
            brief={brief}
            onClose={() => setShowAnalysisPanel(false)}
          />
        </div>
      )}
    </div>
  );
}
