import React, { useEffect } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  makeStyles,
  tokens,
  Card,
  Caption1,
  Divider,
} from '@fluentui/react-components';
import { useCostTrackingStore, type RunCostReport } from '../../state/costTracking';
import { Dismiss24Regular, ArrowDownload24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  summaryCard: {
    padding: tokens.spacingVerticalM,
  },
  totalCost: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
  },
  sectionTitle: {
    marginTop: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalS,
  },
  stageCard: {
    padding: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalS,
  },
  suggestionCard: {
    padding: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  savingsAmount: {
    color: tokens.colorPaletteGreenForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
});

interface RunCostSummaryProps {
  /**
   * Job ID to show summary for
   */
  jobId: string;
  
  /**
   * Whether the dialog is open
   */
  open: boolean;
  
  /**
   * Callback when dialog is closed
   */
  onClose: () => void;
}

/**
 * Comprehensive cost summary modal shown after a run completes
 */
export const RunCostSummary: React.FC<RunCostSummaryProps> = ({
  jobId,
  open,
  onClose,
}) => {
  const styles = useStyles();
  const { getRunSummary, exportReport, runReports } = useCostTrackingStore();
  
  const report = runReports[jobId];
  
  useEffect(() => {
    if (open && !report) {
      void getRunSummary(jobId);
    }
  }, [open, jobId, report, getRunSummary]);
  
  if (!report) {
    return null;
  }
  
  const handleExport = async (format: 'json' | 'csv') => {
    await exportReport(jobId, format);
  };
  
  const topStages = Object.values(report.costByStage)
    .sort((a, b) => b.cost - a.cost)
    .slice(0, 5);
  
  const topProviders = Object.entries(report.costByProvider)
    .sort(([, a], [, b]) => b - a)
    .slice(0, 5);
  
  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogTitle
          action={
            <Button
              appearance="subtle"
              icon={<Dismiss24Regular />}
              onClick={onClose}
              aria-label="Close"
            />
          }
        >
          Cost Summary - {report.projectName || report.jobId}
        </DialogTitle>
        <DialogBody>
          <DialogContent className={styles.content}>
            <Card className={styles.summaryCard}>
              <div className={styles.row}>
                <Text weight="semibold">Total Cost</Text>
                <Text className={styles.totalCost}>
                  {report.currency} ${report.totalCost.toFixed(4)}
                </Text>
              </div>
              <div className={styles.row}>
                <Caption1>Duration</Caption1>
                <Caption1>{report.durationSeconds.toFixed(1)}s</Caption1>
              </div>
              {report.budgetLimit && (
                <div className={styles.row}>
                  <Caption1>Budget Status</Caption1>
                  <Caption1>
                    {report.withinBudget ? (
                      <span style={{ color: tokens.colorPaletteGreenForeground1 }}>
                        Within budget (${report.budgetLimit.toFixed(2)})
                      </span>
                    ) : (
                      <span style={{ color: tokens.colorPaletteRedForeground1 }}>
                        Over budget (${report.budgetLimit.toFixed(2)})
                      </span>
                    )}
                  </Caption1>
                </div>
              )}
            </Card>
            
            {report.tokenStats && (
              <Card className={styles.summaryCard}>
                <Text weight="semibold">Token Usage</Text>
                <div className={styles.row}>
                  <Caption1>Total Tokens</Caption1>
                  <Caption1>{report.tokenStats.totalTokens.toLocaleString()}</Caption1>
                </div>
                <div className={styles.row}>
                  <Caption1>Cache Hit Rate</Caption1>
                  <Caption1>{report.tokenStats.cacheHitRate.toFixed(1)}%</Caption1>
                </div>
                <div className={styles.row}>
                  <Caption1>Cost Saved by Cache</Caption1>
                  <Caption1 className={styles.savingsAmount}>
                    ${report.tokenStats.costSavedByCache.toFixed(4)}
                  </Caption1>
                </div>
              </Card>
            )}
            
            <Divider />
            
            <div>
              <Text weight="semibold" className={styles.sectionTitle}>
                Top Stages by Cost
              </Text>
              {topStages.map((stage) => (
                <Card key={stage.stageName} className={styles.stageCard}>
                  <div className={styles.row}>
                    <Text>{stage.stageName}</Text>
                    <Text weight="semibold">
                      ${stage.cost.toFixed(4)} ({stage.percentageOfTotal.toFixed(1)}%)
                    </Text>
                  </div>
                </Card>
              ))}
            </div>
            
            <div>
              <Text weight="semibold" className={styles.sectionTitle}>
                Cost by Provider
              </Text>
              {topProviders.map(([provider, cost]) => (
                <div key={provider} className={styles.row}>
                  <Caption1>{provider}</Caption1>
                  <Caption1>${cost.toFixed(4)}</Caption1>
                </div>
              ))}
            </div>
            
            {report.optimizationSuggestions.length > 0 && (
              <>
                <Divider />
                <div>
                  <Text weight="semibold" className={styles.sectionTitle}>
                    Cost Optimization Suggestions
                  </Text>
                  {report.optimizationSuggestions.map((suggestion, index) => (
                    <Card key={index} className={styles.suggestionCard}>
                      <Text weight="semibold">{suggestion.category}</Text>
                      <Caption1>{suggestion.suggestion}</Caption1>
                      <div className={styles.row}>
                        <Caption1>Potential Savings:</Caption1>
                        <Caption1 className={styles.savingsAmount}>
                          ${suggestion.estimatedSavings.toFixed(4)}
                        </Caption1>
                      </div>
                      {suggestion.qualityImpact && (
                        <Caption1>Impact: {suggestion.qualityImpact}</Caption1>
                      )}
                    </Card>
                  ))}
                </div>
              </>
            )}
          </DialogContent>
        </DialogBody>
        <DialogActions>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={() => void handleExport('csv')}
          >
            Export CSV
          </Button>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={() => void handleExport('json')}
          >
            Export JSON
          </Button>
          <Button appearance="primary" onClick={onClose}>
            Close
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};
