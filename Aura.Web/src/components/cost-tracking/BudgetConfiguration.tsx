import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Text,
  Field,
  Input,
  Dropdown,
  Option,
  Switch,
  Button,
  Checkbox,
} from '@fluentui/react-components';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import type {
  CostTrackingConfiguration,
  ProviderPricing,
} from '../../services/providers/providerRecommendationService';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  field: {
    marginBottom: tokens.spacingVerticalM,
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  budgetRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalS,
  },
  providerName: {
    width: '150px',
    fontWeight: tokens.fontWeightSemibold,
  },
  budgetInput: {
    width: '150px',
  },
  currentSpend: {
    color: tokens.colorNeutralForeground3,
  },
  thresholdGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  saveButton: {
    marginTop: tokens.spacingVerticalL,
  },
});

interface BudgetConfigurationProps {
  configuration: CostTrackingConfiguration;
  providerPricing: ProviderPricing[];
  onSave: (config: CostTrackingConfiguration) => void;
  currentSpending: Record<string, number>;
}

export const BudgetConfiguration: FC<BudgetConfigurationProps> = ({
  configuration,
  providerPricing,
  onSave,
  currentSpending,
}) => {
  const styles = useStyles();
  const [config, setConfig] = useState(configuration);

  useEffect(() => {
    setConfig(configuration);
  }, [configuration]);

  const handleOverallBudgetChange = (value: string) => {
    const budget = value ? parseFloat(value) : undefined;
    setConfig((prev) => ({ ...prev, overallMonthlyBudget: budget }));
  };

  const handleProviderBudgetChange = (provider: string, value: string) => {
    const budget = value ? parseFloat(value) : 0;
    setConfig((prev) => ({
      ...prev,
      providerBudgets: {
        ...prev.providerBudgets,
        [provider]: budget,
      },
    }));
  };

  const handleThresholdToggle = (threshold: number, checked: boolean) => {
    setConfig((prev) => {
      const thresholds = checked
        ? [...prev.alertThresholds, threshold].sort((a, b) => a - b)
        : prev.alertThresholds.filter((t) => t !== threshold);
      return { ...prev, alertThresholds: thresholds };
    });
  };

  const handleSave = () => {
    onSave(config);
  };

  const paidProviders = providerPricing.filter((p) => !p.isFree);
  const availableThresholds = [50, 75, 90, 100];

  return (
    <Card className={styles.card}>
      <Title3>Budget Configuration</Title3>

      <div className={styles.section}>
        <Field
          className={styles.field}
          label="Overall Monthly Budget"
          hint={`Total budget across all providers in ${config.currency}`}
        >
          <Input
            type="number"
            value={config.overallMonthlyBudget?.toString() || ''}
            onChange={(_, data) => handleOverallBudgetChange(data.value)}
            placeholder="No limit"
          />
        </Field>

        <Field className={styles.field} label="Budget Period">
          <Dropdown
            value={config.periodType}
            selectedOptions={[config.periodType]}
            onOptionSelect={(_, data) =>
              setConfig((prev) => ({
                ...prev,
                periodType: data.optionValue as 'Monthly' | 'Weekly' | 'Custom',
              }))
            }
          >
            <Option value="Monthly">Monthly (Calendar month)</Option>
            <Option value="Weekly">Weekly (Sunday-Saturday)</Option>
            <Option value="Custom">Custom Date Range</Option>
          </Dropdown>
        </Field>

        <Field className={styles.field} label="Currency">
          <Dropdown
            value={config.currency}
            selectedOptions={[config.currency]}
            onOptionSelect={(_, data) =>
              setConfig((prev) => ({ ...prev, currency: data.optionValue as string }))
            }
          >
            <Option value="USD">USD ($)</Option>
            <Option value="EUR">EUR (€)</Option>
            <Option value="GBP">GBP (£)</Option>
            <Option value="JPY">JPY (¥)</Option>
          </Dropdown>
        </Field>
      </div>

      <div className={styles.section}>
        <Title3>Per-Provider Budgets</Title3>
        <Text>Set individual budget limits for each provider</Text>
        {paidProviders.map((provider) => (
          <div key={provider.providerName} className={styles.budgetRow}>
            <span className={styles.providerName}>{provider.providerName}</span>
            <Input
              className={styles.budgetInput}
              type="number"
              value={config.providerBudgets[provider.providerName]?.toString() || ''}
              onChange={(_, data) => handleProviderBudgetChange(provider.providerName, data.value)}
              placeholder="No limit"
            />
            <span className={styles.currentSpend}>
              Current: {config.currency} {(currentSpending[provider.providerName] || 0).toFixed(2)}
            </span>
          </div>
        ))}
      </div>

      <div className={styles.section}>
        <Title3>Budget Alerts</Title3>
        <Text>Select which thresholds trigger alerts</Text>
        <div className={styles.thresholdGroup}>
          {availableThresholds.map((threshold) => (
            <Checkbox
              key={threshold}
              label={`${threshold}%`}
              checked={config.alertThresholds.includes(threshold)}
              onChange={(_, data) => handleThresholdToggle(threshold, data.checked === true)}
            />
          ))}
        </div>

        <Field
          className={styles.field}
          label="Alert Frequency"
          hint="How often to send alerts when threshold is exceeded"
        >
          <Dropdown
            value={config.alertFrequency}
            selectedOptions={[config.alertFrequency]}
            onOptionSelect={(_, data) =>
              setConfig((prev) => ({
                ...prev,
                alertFrequency: data.optionValue as 'Once' | 'Daily' | 'EveryTime',
              }))
            }
          >
            <Option value="Once">Once per period</Option>
            <Option value="Daily">Daily</Option>
            <Option value="EveryTime">Every time</Option>
          </Dropdown>
        </Field>

        <Field className={styles.field} label="Email Notifications">
          <Switch
            checked={config.emailNotificationsEnabled}
            onChange={(_, data) =>
              setConfig((prev) => ({ ...prev, emailNotificationsEnabled: data.checked }))
            }
          />
        </Field>

        {config.emailNotificationsEnabled && (
          <Field className={styles.field} label="Notification Email">
            <Input
              type="email"
              value={config.notificationEmail || ''}
              onChange={(_, data) =>
                setConfig((prev) => ({ ...prev, notificationEmail: data.value }))
              }
              placeholder="your.email@example.com"
            />
          </Field>
        )}
      </div>

      <div className={styles.section}>
        <Field className={styles.field} label="Hard Budget Limit">
          <Switch
            checked={config.hardBudgetLimit}
            onChange={(_, data) =>
              setConfig((prev) => ({ ...prev, hardBudgetLimit: data.checked }))
            }
            label={
              config.hardBudgetLimit
                ? 'Block operations when budget exceeded'
                : 'Show warnings only (operations allowed)'
            }
          />
        </Field>

        <Field className={styles.field} label="Project Tracking">
          <Switch
            checked={config.enableProjectTracking}
            onChange={(_, data) =>
              setConfig((prev) => ({ ...prev, enableProjectTracking: data.checked }))
            }
            label="Track costs by project"
          />
        </Field>
      </div>

      <Button appearance="primary" className={styles.saveButton} onClick={handleSave}>
        Save Configuration
      </Button>
    </Card>
  );
};
