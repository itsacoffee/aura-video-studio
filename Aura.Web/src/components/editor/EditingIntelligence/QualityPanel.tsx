/**
 * Quality Panel Component
 * Displays quality control issues and recommendations
 */

import {
  Card,
  makeStyles,
  tokens,
  Badge,
  Body1,
  Body1Strong,
  Caption1,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import { Warning24Regular, Info24Regular, ErrorCircle24Regular } from '@fluentui/react-icons';
import React from 'react';
import { QualityIssue } from '../../../services/editingIntelligenceService';

interface QualityPanelProps {
  issues: QualityIssue[];
  jobId: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  issueCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  fixSuggestion: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalS,
  },
});

const getSeverityColor = (severity: string): 'danger' | 'warning' | 'informative' | 'success' => {
  switch (severity) {
    case 'Critical':
      return 'danger';
    case 'Error':
      return 'danger';
    case 'Warning':
      return 'warning';
    case 'Info':
      return 'informative';
    default:
      return 'informative';
  }
};

const getSeverityIcon = (severity: string) => {
  switch (severity) {
    case 'Critical':
    case 'Error':
      return <ErrorCircle24Regular />;
    case 'Warning':
      return <Warning24Regular />;
    default:
      return <Info24Regular />;
  }
};

const getIssueTypeLabel = (type: string): string => {
  return type.replace(/([A-Z])/g, ' $1').trim();
};

export const QualityPanel: React.FC<QualityPanelProps> = ({ issues }) => {
  const styles = useStyles();

  const criticalIssues = issues.filter((i) => i.severity === 'Critical');
  const errorIssues = issues.filter((i) => i.severity === 'Error');
  const warningIssues = issues.filter((i) => i.severity === 'Warning');

  if (issues.length === 0) {
    return (
      <MessageBar intent="success">
        <MessageBarBody>
          <MessageBarTitle>All Clear!</MessageBarTitle>
          No quality issues detected. Timeline is ready for rendering.
        </MessageBarBody>
      </MessageBar>
    );
  }

  return (
    <div className={styles.container}>
      {criticalIssues.length > 0 && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Critical Issues</MessageBarTitle>
            {criticalIssues.length} critical issues must be resolved before rendering.
          </MessageBarBody>
        </MessageBar>
      )}

      {issues.map((issue, index) => (
        <Card key={index} className={styles.issueCard}>
          <div className={styles.header}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
              {getSeverityIcon(issue.severity)}
              <Body1Strong>{getIssueTypeLabel(issue.type)}</Body1Strong>
            </div>
            <Badge appearance="filled" color={getSeverityColor(issue.severity)}>
              {issue.severity}
            </Badge>
          </div>

          <Body1>{issue.description}</Body1>

          {issue.location && (
            <Caption1 style={{ marginTop: tokens.spacingVerticalS }}>
              Location: {issue.location}
            </Caption1>
          )}

          {issue.fixSuggestion && (
            <div className={styles.fixSuggestion}>
              <Caption1>
                <strong>Fix:</strong> {issue.fixSuggestion}
              </Caption1>
            </div>
          )}
        </Card>
      ))}

      <Card className={styles.issueCard}>
        <Body1Strong>Summary</Body1Strong>
        <Body1 style={{ marginTop: tokens.spacingVerticalS }}>
          • {criticalIssues.length} Critical Issues
          <br />• {errorIssues.length} Errors
          <br />• {warningIssues.length} Warnings
        </Body1>
      </Card>
    </div>
  );
};
