import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Switch,
  Field,
  Card,
  Dropdown,
  Option,
  InfoLabel,
} from '@fluentui/react-components';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import { providerRecommendationService } from '../../services/providers/providerRecommendationService';
import type {
  ProviderPreferences,
  CostTrackingConfiguration,
  CurrentPeriodSpending,
  SpendingReport,
  ProviderPricing,
} from '../../services/providers/providerRecommendationService';
import { BudgetConfiguration } from '../cost-tracking/BudgetConfiguration';
import { CostDashboard } from '../cost-tracking/CostDashboard';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  field: {
    marginBottom: tokens.spacingVerticalM,
  },
  subsection: {
    marginTop: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalL,
  },
  divider: {
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    marginTop: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalXL,
  },
  helpText: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalL,
  },
});

export const ProviderRecommendationsTab: FC = () => {
  const styles = useStyles();
  const [preferences, setPreferences] = useState<ProviderPreferences>({
    enableRecommendations: false,
    assistanceLevel: 'Off',
    enableHealthMonitoring: false,
    enableCostTracking: false,
    enableLearning: false,
    enableProfiles: false,
    enableAutoFallback: false,
    alwaysUseDefault: false,
    perOperationOverrides: {},
    activeProfile: 'Balanced',
    excludedProviders: [],
    fallbackChains: {},
    perProviderBudgetLimits: {},
    hardBudgetLimit: false,
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [costConfig, setCostConfig] = useState<CostTrackingConfiguration | null>(null);
  const [currentSpending, setCurrentSpending] = useState<CurrentPeriodSpending | null>(null);
  const [spendingReport, setSpendingReport] = useState<SpendingReport | null>(null);
  const [providerPricing, setProviderPricing] = useState<ProviderPricing[]>([]);

  useEffect(() => {
    loadPreferences();
    loadCostTrackingData();
  }, []);

  useEffect(() => {
    if (preferences.enableCostTracking) {
      loadCostTrackingData();
    }
  }, [preferences.enableCostTracking]);

  const loadPreferences = async () => {
    try {
      setLoading(true);
      const prefs = await providerRecommendationService.getPreferences();
      setPreferences(prefs);
    } catch (error: unknown) {
      console.error('Failed to load preferences:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadCostTrackingData = async () => {
    try {
      const now = new Date();
      const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

      const [config, spending, report, pricing] = await Promise.all([
        providerRecommendationService.getCostTrackingConfiguration(),
        providerRecommendationService.getCurrentPeriodSpending(),
        providerRecommendationService.getSpendingReport(startOfMonth, now),
        providerRecommendationService.getProviderPricing(),
      ]);

      setCostConfig(config);
      setCurrentSpending(spending);
      setSpendingReport(report);
      setProviderPricing(pricing);
    } catch (error: unknown) {
      console.error('Failed to load cost tracking data:', error);
    }
  };

  const updatePreference = async (updates: Partial<ProviderPreferences>) => {
    try {
      setSaving(true);
      const newPrefs = { ...preferences, ...updates };
      setPreferences(newPrefs);
      await providerRecommendationService.updatePreferences(newPrefs);
    } catch (error: unknown) {
      console.error('Failed to update preferences:', error);
    } finally {
      setSaving(false);
    }
  };

  const handleSaveCostConfiguration = async (config: CostTrackingConfiguration) => {
    try {
      setSaving(true);
      const success = await providerRecommendationService.updateCostTrackingConfiguration(config);
      if (success) {
        setCostConfig(config);
        await loadCostTrackingData();
      }
    } catch (error: unknown) {
      console.error('Failed to save cost configuration:', error);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Card className={styles.section}>
        <Text>Loading preferences...</Text>
      </Card>
    );
  }

  return (
    <>
      {/* Master Controls Section */}
      <Card className={styles.section}>
        <Title2>Master Controls</Title2>
        <Text className={styles.helpText}>
          Control the provider recommendation system. When disabled, you have complete manual
          control over provider selection with no automation or suggestions.
        </Text>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  When disabled, all recommendation features are turned off. You&apos;ll have pure
                  manual provider selection with no badges, suggestions, or automation.
                </Text>
              }
            >
              Enable Provider Recommendations (OFF by default - opt-in)
            </InfoLabel>
          }
        >
          <Switch
            checked={preferences.enableRecommendations}
            onChange={(_, data) => updatePreference({ enableRecommendations: data.checked })}
            disabled={saving}
          />
        </Field>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  Controls how much help and automation the system provides. OFF completely disables
                  all recommendation features.
                </Text>
              }
            >
              Assistance Level
            </InfoLabel>
          }
        >
          <Dropdown
            value={preferences.assistanceLevel}
            selectedOptions={[preferences.assistanceLevel]}
            onOptionSelect={(_, data) =>
              updatePreference({
                assistanceLevel: data.optionValue as 'Off' | 'Minimal' | 'Moderate' | 'Full',
              })
            }
            disabled={saving || !preferences.enableRecommendations}
          >
            <Option value="Off">OFF - No recommendations at all</Option>
            <Option value="Minimal">MINIMAL - Only show recommendation badge</Option>
            <Option value="Moderate">MODERATE - Brief reasoning, no automatic actions</Option>
            <Option value="Full">FULL - Detailed explanations and all features</Option>
          </Dropdown>
        </Field>

        <div className={styles.divider} />

        <Title3>Feature Toggles</Title3>
        <Text className={styles.helpText}>
          Individual toggles for specific features. All are OFF by default (opt-in).
        </Text>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  When disabled, no health tracking occurs and providers are used exactly as you
                  specify regardless of health.
                </Text>
              }
            >
              Monitor Provider Health
            </InfoLabel>
          }
        >
          <Switch
            checked={preferences.enableHealthMonitoring}
            onChange={(_, data) => updatePreference({ enableHealthMonitoring: data.checked })}
            disabled={saving || !preferences.enableRecommendations}
          />
        </Field>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  When disabled, no cost tracking, no budget warnings, and no spend monitoring. You
                  can use providers without seeing costs.
                </Text>
              }
            >
              Track Provider Costs
            </InfoLabel>
          }
        >
          <Switch
            checked={preferences.enableCostTracking}
            onChange={(_, data) => updatePreference({ enableCostTracking: data.checked })}
            disabled={saving || !preferences.enableRecommendations}
          />
        </Field>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  When disabled, system never tracks your overrides, never learns patterns, and
                  never adjusts recommendations based on your history.
                </Text>
              }
            >
              Learn From My Choices
            </InfoLabel>
          }
        >
          <Switch
            checked={preferences.enableLearning}
            onChange={(_, data) => updatePreference({ enableLearning: data.checked })}
            disabled={saving || !preferences.enableRecommendations}
          />
        </Field>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  When disabled, no profile system is shown. Profiles allow preset configurations
                  like &quot;Maximum Quality&quot; or &quot;Budget Conscious&quot;.
                </Text>
              }
            >
              Use Provider Profiles
            </InfoLabel>
          }
        >
          <Switch
            checked={preferences.enableProfiles}
            onChange={(_, data) => updatePreference({ enableProfiles: data.checked })}
            disabled={saving || !preferences.enableRecommendations}
          />
        </Field>

        <Field
          className={styles.field}
          label={
            <InfoLabel
              info={
                <Text>
                  When disabled, if a provider fails, the system shows an error and stops. You must
                  manually select a different provider or retry.
                </Text>
              }
            >
              Enable Automatic Fallback
            </InfoLabel>
          }
        >
          <Switch
            checked={preferences.enableAutoFallback}
            onChange={(_, data) => updatePreference({ enableAutoFallback: data.checked })}
            disabled={saving || !preferences.enableRecommendations}
          />
        </Field>
      </Card>

      {/* Manual Configuration Section */}
      <Card className={styles.section}>
        <Title2>Manual Configuration</Title2>
        <Text className={styles.helpText}>
          These settings are always available, regardless of whether recommendations are enabled.
        </Text>

        <Field
          className={styles.field}
          label="Default Provider"
          hint="Your fallback provider when no other is specified"
        >
          <Dropdown
            placeholder="Select default provider"
            value={preferences.globalDefault || 'None'}
            selectedOptions={[preferences.globalDefault || 'None']}
            onOptionSelect={(_, data) =>
              updatePreference({ globalDefault: data.optionValue as string })
            }
            disabled={saving}
          >
            <Option value="None">None (use recommendations)</Option>
            <Option value="OpenAI">OpenAI (GPT-4)</Option>
            <Option value="Claude">Anthropic Claude</Option>
            <Option value="Gemini">Google Gemini</Option>
            <Option value="Ollama">Ollama (Local)</Option>
            <Option value="RuleBased">RuleBased (Offline)</Option>
          </Dropdown>
        </Field>

        <Field
          className={styles.field}
          label="Always Use Default"
          hint="When enabled, bypasses all recommendations and always uses your default provider"
        >
          <Switch
            checked={preferences.alwaysUseDefault}
            onChange={(_, data) => updatePreference({ alwaysUseDefault: data.checked })}
            disabled={saving}
          />
        </Field>
      </Card>

      {/* Advanced Section - Only shown when recommendations enabled */}
      {preferences.enableRecommendations && preferences.assistanceLevel !== 'Off' && (
        <Card className={styles.section}>
          <Title2>Advanced</Title2>
          <Text className={styles.helpText}>
            Advanced settings for fine-tuning recommendation behavior. These only apply when
            recommendations are enabled.
          </Text>

          {preferences.enableProfiles && (
            <Field
              className={styles.field}
              label="Active Profile"
              hint="Choose a preset configuration for provider selection"
            >
              <Dropdown
                value={preferences.activeProfile}
                selectedOptions={[preferences.activeProfile]}
                onOptionSelect={(_, data) =>
                  updatePreference({ activeProfile: data.optionValue as string })
                }
                disabled={saving}
              >
                <Option value="MaximumQuality">Maximum Quality</Option>
                <Option value="Balanced">Balanced (recommended)</Option>
                <Option value="BudgetConscious">Budget Conscious</Option>
                <Option value="SpeedOptimized">Speed Optimized</Option>
                <Option value="LocalOnly">Local Only</Option>
                <Option value="Custom">Custom</Option>
              </Dropdown>
            </Field>
          )}

          {preferences.enableCostTracking && costConfig && (
            <>
              <CostDashboard currentPeriod={currentSpending} spendingReport={spendingReport} />

              <BudgetConfiguration
                configuration={costConfig}
                providerPricing={providerPricing}
                onSave={handleSaveCostConfiguration}
                currentSpending={spendingReport?.costByProvider || ({} as Record<string, number>)}
              />
            </>
          )}
        </Card>
      )}

      {/* About Section */}
      <Card className={styles.section}>
        <Title2>About Provider Recommendations</Title2>
        <div className={styles.subsection}>
          <Title3>What happens when disabled?</Title3>
          <Text>
            When the recommendation system is disabled, you have complete manual control.
            You&apos;ll see plain provider dropdowns with no badges, no suggestions, no cost
            estimates (unless you explicitly request them), and no health indicators (unless you
            enable them separately). The system behaves identically to if the recommendation system
            was never built.
          </Text>
        </div>

        <div className={styles.subsection}>
          <Title3>What happens when enabled?</Title3>
          <Text>
            When enabled, the system provides intelligent provider suggestions based on the
            operation type, quality requirements, cost, latency, and availability. You can control
            how much help you get with the Assistance Level setting.
          </Text>
        </div>

        <div className={styles.subsection}>
          <Title3>Privacy &amp; Performance</Title3>
          <Text>
            When disabled, there is zero performance impact: no background tracking, no monitoring,
            no data collection. When learning is disabled, the system never tracks your provider
            selections (privacy respected).
          </Text>
        </div>
      </Card>
    </>
  );
};
