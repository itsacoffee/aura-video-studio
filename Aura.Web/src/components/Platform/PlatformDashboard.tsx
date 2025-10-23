import React, { useState } from 'react';
import {
  Card,
  Title2,
  Title3,
  Body1,
  Tab,
  TabList,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Video24Regular, TagMultiple24Regular, DataTrending24Regular } from '@fluentui/react-icons';
import { PlatformSelector } from './PlatformSelector';
import { MetadataGenerator } from './MetadataGenerator';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  tabContent: {
    marginTop: tokens.spacingVerticalL,
  },
  featuresGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalL,
  },
  featureCard: {
    padding: tokens.spacingVerticalL,
  },
  featureIcon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalM,
  },
});

export const PlatformDashboard: React.FC = () => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string>('overview');

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Platform Optimization</Title2>
        <Body1>
          Optimize your content for multiple platforms with AI-powered metadata, thumbnails, and
          scheduling
        </Body1>
      </div>

      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as string)}
      >
        <Tab value="overview" icon={<Video24Regular />}>
          Overview
        </Tab>
        <Tab value="metadata" icon={<TagMultiple24Regular />}>
          Metadata Generator
        </Tab>
        <Tab value="platforms" icon={<DataTrending24Regular />}>
          Platform Selector
        </Tab>
      </TabList>

      <div className={styles.tabContent}>
        {selectedTab === 'overview' && (
          <div>
            <Title3>Platform Optimization Features</Title3>
            <div className={styles.featuresGrid}>
              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>📱</div>
                <Title3>Platform Profiles</Title3>
                <Body1>
                  Detailed specifications for YouTube, TikTok, Instagram, LinkedIn, Twitter,
                  Facebook, and more.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>🎬</div>
                <Title3>Video Optimization</Title3>
                <Body1>
                  Automatically adapt aspect ratios, duration, and format for each
                  platform&apos;s requirements.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>✍️</div>
                <Title3>AI Metadata</Title3>
                <Body1>
                  Generate platform-specific titles, descriptions, tags, and hashtags optimized for
                  discovery.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>🖼️</div>
                <Title3>Thumbnail Intelligence</Title3>
                <Body1>
                  AI-powered thumbnail concepts with predicted click-through rates for each
                  platform.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>🔍</div>
                <Title3>SEO Research</Title3>
                <Body1>
                  Keyword research, search volume analysis, and competition insights for better
                  discoverability.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>⏰</div>
                <Title3>Optimal Scheduling</Title3>
                <Body1>
                  Get recommendations for the best times to post based on platform algorithms and
                  audience activity.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>📊</div>
                <Title3>Platform Trends</Title3>
                <Body1>
                  Stay updated with current trending topics, hashtags, and content strategies for
                  each platform.
                </Body1>
              </Card>

              <Card className={styles.featureCard}>
                <div className={styles.featureIcon}>🚀</div>
                <Title3>Multi-Platform Export</Title3>
                <Body1>
                  Export optimized versions for multiple platforms simultaneously with a single
                  click.
                </Body1>
              </Card>
            </div>
          </div>
        )}

        {selectedTab === 'metadata' && <MetadataGenerator />}

        {selectedTab === 'platforms' && (
          <PlatformSelector
            onPlatformsSelected={(_platforms) => {
              // TODO: Handle platform selection
            }}
          />
        )}
      </div>
    </div>
  );
};
