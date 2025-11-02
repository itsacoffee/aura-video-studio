import {
  makeStyles,
  tokens,
  Tab,
  TabList,
  SelectTabData,
  SelectTabEvent,
} from '@fluentui/react-components';
import React, { useState } from 'react';
import { RouteErrorBoundary } from '../ErrorBoundary/RouteErrorBoundary';
import { AudienceInsightPanel } from './AudienceInsightPanel';
import { ContentCalendarView } from './ContentCalendarView';
import { TopicSuggestionList } from './TopicSuggestionList';
import { TrendAnalysisPanel } from './TrendAnalysisPanel';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingVerticalL,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase300,
  },
  tabContent: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalL,
  },
});

const ContentPlanningDashboardContent: React.FC = () => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string>('trends');

  const handleTabSelect = (_event: SelectTabEvent, data: SelectTabData) => {
    setSelectedTab(data.value as string);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>Content Planning</div>
        <div className={styles.subtitle}>
          Plan your content strategy with AI-powered insights and recommendations
        </div>
      </div>

      <TabList selectedValue={selectedTab} onTabSelect={handleTabSelect}>
        <Tab value="trends">Trend Analysis</Tab>
        <Tab value="topics">Topic Suggestions</Tab>
        <Tab value="calendar">Content Calendar</Tab>
        <Tab value="audience">Audience Insights</Tab>
      </TabList>

      <div className={styles.tabContent}>
        {selectedTab === 'trends' && <TrendAnalysisPanel />}
        {selectedTab === 'topics' && <TopicSuggestionList />}
        {selectedTab === 'calendar' && <ContentCalendarView />}
        {selectedTab === 'audience' && <AudienceInsightPanel />}
      </div>
    </div>
  );
};

// Main export with error boundary
export const ContentPlanningDashboard: React.FC = () => {
  return (
    <RouteErrorBoundary>
      <ContentPlanningDashboardContent />
    </RouteErrorBoundary>
  );
};
