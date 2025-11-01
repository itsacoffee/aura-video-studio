import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Field,
  Slider,
  Switch,
  Dropdown,
  Option,
  Badge,
} from '@fluentui/react-components';
import { Shield24Regular, Info24Regular } from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { useContentSafetyStore } from '../../state/contentSafety';
import type { SafetyPolicy, SafetyCategoryType } from '../../state/contentSafety';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  categorySlider: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  badge: {
    marginLeft: tokens.spacingHorizontalS,
  },
  keywordList: {
    maxHeight: '200px',
    overflowY: 'auto',
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  keywordItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
});

export const ContentSafetyTab = () => {
  const styles = useStyles();
  const {
    policies,
    currentPolicyId,
    isLoading,
    error,
    loadPolicies,
    loadPresets,
    createPolicy,
    updatePolicy,
    setCurrentPolicy,
  } = useContentSafetyStore();

  const [selectedPolicy, setSelectedPolicy] = useState<SafetyPolicy | null>(null);
  const [editedPolicy, setEditedPolicy] = useState<SafetyPolicy | null>(null);

  useEffect(() => {
    loadPolicies();
    loadPresets();
  }, [loadPolicies, loadPresets]);

  useEffect(() => {
    if (policies.length > 0 && !currentPolicyId) {
      const defaultPolicy = policies.find((p) => p.isDefault);
      if (defaultPolicy) {
        setCurrentPolicy(defaultPolicy.id);
        setSelectedPolicy(defaultPolicy);
        setEditedPolicy(defaultPolicy);
      }
    }
  }, [policies, currentPolicyId, setCurrentPolicy]);

  const handlePolicyChange = (policyId: string) => {
    const policy = policies.find((p) => p.id === policyId);
    if (policy) {
      setCurrentPolicy(policyId);
      setSelectedPolicy(policy);
      setEditedPolicy(policy);
    }
  };

  const handleCategoryThresholdChange = (category: SafetyCategoryType, value: number) => {
    if (!editedPolicy) return;

    setEditedPolicy({
      ...editedPolicy,
      categories: {
        ...editedPolicy.categories,
        [category]: {
          ...(editedPolicy.categories[category] || {
            type: category,
            isEnabled: true,
            defaultAction: 'Warn',
          }),
          threshold: value,
        },
      },
    });
  };

  const handleSavePolicy = async () => {
    if (!editedPolicy) return;

    if (editedPolicy.id) {
      await updatePolicy(editedPolicy.id, editedPolicy);
    } else {
      await createPolicy(editedPolicy);
    }
  };

  const getSafetyLevelColor = (threshold: number): string => {
    if (threshold <= 3) return 'success';
    if (threshold <= 6) return 'warning';
    return 'danger';
  };

  const categories: SafetyCategoryType[] = [
    'Profanity',
    'Violence',
    'SexualContent',
    'HateSpeech',
    'DrugAlcohol',
    'ControversialTopics',
    'Copyright',
    'SelfHarm',
    'GraphicImagery',
    'Misinformation',
  ];

  return (
    <div className={styles.container}>
      <div className={styles.section}>
        <div className={styles.row}>
          <Shield24Regular />
          <Title2>Content Safety & Filtering</Title2>
        </div>
        <Text>
          Configure content safety policies to control what content is appropriate for your videos.
          Choose from presets or create custom policies with granular controls.
        </Text>
      </div>

      {error && (
        <Card
          className={styles.card}
          style={{ backgroundColor: tokens.colorPaletteRedBackground2 }}
        >
          <Text style={{ color: tokens.colorPaletteRedForeground1 }}>{error}</Text>
        </Card>
      )}

      <Card className={styles.card}>
        <div className={styles.section}>
          <Field label="Active Safety Policy">
            <Dropdown
              value={selectedPolicy?.name || 'Select a policy'}
              onOptionSelect={(_, data) => handlePolicyChange(data.optionValue as string)}
              disabled={isLoading}
            >
              {policies.map((policy) => (
                <Option key={policy.id} value={policy.id} text={policy.name}>
                  {policy.name}
                  {policy.isDefault && (
                    <Badge className={styles.badge} appearance="filled" color="brand">
                      Default
                    </Badge>
                  )}
                </Option>
              ))}
            </Dropdown>
          </Field>

          {selectedPolicy && (
            <>
              <Field label="Policy Description">
                <Text>{selectedPolicy.description || 'No description provided'}</Text>
              </Field>

              <Field label="Preset Level">
                <Badge
                  appearance="filled"
                  color={
                    selectedPolicy.preset === 'Unrestricted'
                      ? 'danger'
                      : selectedPolicy.preset === 'Strict'
                        ? 'success'
                        : 'warning'
                  }
                >
                  {selectedPolicy.preset}
                </Badge>
              </Field>

              <Field>
                <Switch
                  label="Enable Content Filtering"
                  checked={editedPolicy?.isEnabled ?? false}
                  onChange={(_, data) =>
                    setEditedPolicy(
                      editedPolicy ? { ...editedPolicy, isEnabled: data.checked } : null
                    )
                  }
                />
              </Field>

              <Field>
                <Switch
                  label="Allow User Override"
                  checked={editedPolicy?.allowUserOverride ?? false}
                  onChange={(_, data) =>
                    setEditedPolicy(
                      editedPolicy ? { ...editedPolicy, allowUserOverride: data.checked } : null
                    )
                  }
                />
              </Field>
            </>
          )}
        </div>
      </Card>

      {editedPolicy && editedPolicy.isEnabled && (
        <Card className={styles.card}>
          <div className={styles.section}>
            <Title2>Safety Categories</Title2>
            <Text>
              Adjust thresholds for each category. Higher values = more restrictive filtering.
            </Text>

            {categories.map((category) => {
              const categoryData = editedPolicy.categories[category];
              const threshold = categoryData?.threshold ?? 5;

              return (
                <div key={category} className={styles.categorySlider}>
                  <div className={styles.row}>
                    <Text weight="semibold">{category}</Text>
                    <Badge appearance="filled" color={getSafetyLevelColor(threshold) as any}>
                      Level {threshold}
                    </Badge>
                  </div>
                  <Slider
                    min={0}
                    max={10}
                    value={threshold}
                    onChange={(_, data) => handleCategoryThresholdChange(category, data.value)}
                  />
                  <div className={styles.row}>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      0 = No restrictions
                    </Text>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      10 = Maximum filtering
                    </Text>
                  </div>
                </div>
              );
            })}
          </div>
        </Card>
      )}

      <div className={styles.actions}>
        <Button
          appearance="secondary"
          onClick={() => {
            if (selectedPolicy) {
              setEditedPolicy(selectedPolicy);
            }
          }}
          disabled={isLoading}
        >
          Reset Changes
        </Button>
        <Button
          appearance="primary"
          onClick={handleSavePolicy}
          disabled={isLoading || !editedPolicy}
        >
          Save Policy
        </Button>
      </div>

      <Card className={styles.card} style={{ backgroundColor: tokens.colorNeutralBackground2 }}>
        <div className={styles.row}>
          <Info24Regular />
          <Text weight="semibold">About Content Safety</Text>
        </div>
        <Text size={200}>
          Content safety policies help ensure your videos are appropriate for your target audience
          and comply with platform guidelines. Unrestricted mode disables all filtering and puts
          full responsibility on the user. Strict mode enforces family-friendly content suitable for
          all ages.
        </Text>
      </Card>
    </div>
  );
};
