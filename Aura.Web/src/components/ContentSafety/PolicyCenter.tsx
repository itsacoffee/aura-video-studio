import {
  makeStyles,
  tokens,
  Text,
  Title2,
  Card,
  Button,
  Input,
  Field,
  Textarea,
  Badge,
  Dropdown,
  Option,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Switch,
  Divider,
} from '@fluentui/react-components';
import {
  Shield24Regular,
  Add24Regular,
  Delete24Regular,
  Edit24Regular,
  Save24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { useContentSafetyStore } from '../../state/contentSafety';
import type { SafetyPolicy } from '../../state/contentSafety';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  policyList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  policyItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  policyInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  policyActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  keywordSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
  keywordList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    maxHeight: '300px',
    overflowY: 'auto',
  },
  keywordItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  keywordInfo: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  addKeywordRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  formRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
});

export const PolicyCenter: FC = () => {
  const styles = useStyles();
  const {
    policies,
    createPolicy,
    updatePolicy,
    deletePolicy,
    isLoading,
  } = useContentSafetyStore();

  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [editingPolicy, setEditingPolicy] = useState<SafetyPolicy | null>(null);
  
  const [policyName, setPolicyName] = useState('');
  const [policyDescription, setPolicyDescription] = useState('');
  const [policyPreset, setPolicyPreset] = useState('Moderate');
  const [policyEnabled, setPolicyEnabled] = useState(true);
  const [allowOverride, setAllowOverride] = useState(true);
  
  const [newKeyword, setNewKeyword] = useState('');
  const [newKeywordAction, setNewKeywordAction] = useState('Warn');
  const [keywords, setKeywords] = useState<Array<{ keyword: string; action: string }>>([]);

  const handleCreatePolicy = async () => {
    const newPolicy: Partial<SafetyPolicy> = {
      name: policyName,
      description: policyDescription,
      preset: policyPreset as any,
      isEnabled: policyEnabled,
      allowUserOverride: allowOverride,
      categories: {},
      keywordRules: keywords.map(k => ({
        id: crypto.randomUUID(),
        keyword: k.keyword,
        matchType: 'WholeWord',
        isCaseSensitive: false,
        action: k.action,
        replacement: '',
        contextExceptions: [],
        isRegex: false,
      })),
      topicFilters: [],
    };

    await createPolicy(newPolicy as SafetyPolicy);
    
    setPolicyName('');
    setPolicyDescription('');
    setPolicyPreset('Moderate');
    setPolicyEnabled(true);
    setAllowOverride(true);
    setKeywords([]);
    setShowCreateDialog(false);
  };

  const handleEditPolicy = async () => {
    if (!editingPolicy) return;

    const updatedPolicy: SafetyPolicy = {
      ...editingPolicy,
      name: policyName,
      description: policyDescription,
      isEnabled: policyEnabled,
      allowUserOverride: allowOverride,
      keywordRules: keywords.map(k => ({
        id: crypto.randomUUID(),
        keyword: k.keyword,
        matchType: 'WholeWord',
        isCaseSensitive: false,
        action: k.action,
        replacement: '',
        contextExceptions: [],
        isRegex: false,
      })),
    };

    await updatePolicy(editingPolicy.id, updatedPolicy);
    
    setEditingPolicy(null);
    setShowEditDialog(false);
  };

  const handleDeletePolicy = async (policyId: string) => {
    if (window.confirm('Are you sure you want to delete this policy?')) {
      await deletePolicy(policyId);
    }
  };

  const handleAddKeyword = () => {
    if (newKeyword.trim()) {
      setKeywords([...keywords, { keyword: newKeyword.trim(), action: newKeywordAction }]);
      setNewKeyword('');
    }
  };

  const handleRemoveKeyword = (index: number) => {
    setKeywords(keywords.filter((_, i) => i !== index));
  };

  const openEditDialog = (policy: SafetyPolicy) => {
    setEditingPolicy(policy);
    setPolicyName(policy.name);
    setPolicyDescription(policy.description || '');
    setPolicyEnabled(policy.isEnabled);
    setAllowOverride(policy.allowUserOverride);
    setKeywords(policy.keywordRules.map(k => ({ keyword: k.keyword, action: k.action })));
    setShowEditDialog(true);
  };

  const openCreateDialog = () => {
    setPolicyName('');
    setPolicyDescription('');
    setPolicyPreset('Moderate');
    setPolicyEnabled(true);
    setAllowOverride(true);
    setKeywords([]);
    setShowCreateDialog(true);
  };

  const getActionBadge = (action: string) => {
    switch (action) {
      case 'Block':
        return <Badge appearance="filled" color="danger">Block</Badge>;
      case 'Warn':
        return <Badge appearance="filled" color="warning">Warn</Badge>;
      case 'AutoFix':
        return <Badge appearance="filled" color="brand">Auto-Fix</Badge>;
      default:
        return <Badge appearance="outline">{action}</Badge>;
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Shield24Regular />
          <Title2>Policy Center</Title2>
        </div>
        <Button
          appearance="primary"
          icon={<Add24Regular />}
          onClick={openCreateDialog}
          disabled={isLoading}
        >
          Create Policy
        </Button>
      </div>

      <Card className={styles.card}>
        <Text style={{ marginBottom: tokens.spacingVerticalM }}>
          Manage content safety policies for your organization. Create custom policies with specific
          blocked keywords, categories, and enforcement rules.
        </Text>

        <div className={styles.policyList}>
          {policies.length === 0 ? (
            <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
              <Shield24Regular style={{ fontSize: '48px', color: tokens.colorNeutralForeground3 }} />
              <Text style={{ display: 'block', marginTop: tokens.spacingVerticalM }}>
                No policies configured
              </Text>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Create your first safety policy to get started
              </Text>
            </div>
          ) : (
            policies.map((policy) => (
              <div key={policy.id} className={styles.policyItem}>
                <div className={styles.policyInfo}>
                  <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, alignItems: 'center' }}>
                    <Text weight="semibold">{policy.name}</Text>
                    {policy.isDefault && (
                      <Badge appearance="filled" color="brand">Default</Badge>
                    )}
                    {!policy.isEnabled && (
                      <Badge appearance="outline">Disabled</Badge>
                    )}
                  </div>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    {policy.description || 'No description'}
                  </Text>
                  <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, marginTop: tokens.spacingVerticalXS }}>
                    <Badge appearance="outline">{policy.preset}</Badge>
                    <Badge appearance="outline">
                      {policy.keywordRules.length} keywords
                    </Badge>
                    <Badge appearance="outline">
                      {policy.allowUserOverride ? 'Override allowed' : 'No override'}
                    </Badge>
                  </div>
                </div>
                <div className={styles.policyActions}>
                  <Button
                    appearance="subtle"
                    icon={<Edit24Regular />}
                    onClick={() => openEditDialog(policy)}
                  >
                    Edit
                  </Button>
                  <Button
                    appearance="subtle"
                    icon={<Delete24Regular />}
                    onClick={() => handleDeletePolicy(policy.id)}
                    disabled={policy.isDefault}
                  >
                    Delete
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      </Card>

      <Dialog open={showCreateDialog} onOpenChange={(_, data) => setShowCreateDialog(data.open)}>
        <DialogSurface>
          <DialogTitle>Create Safety Policy</DialogTitle>
          <DialogBody>
            <DialogContent>
              <div className={styles.dialogContent}>
                <Field label="Policy Name" required>
                  <Input
                    value={policyName}
                    onChange={(_, data) => setPolicyName(data.value)}
                    placeholder="Enter policy name..."
                  />
                </Field>

                <Field label="Description">
                  <Textarea
                    value={policyDescription}
                    onChange={(_, data) => setPolicyDescription(data.value)}
                    placeholder="Describe the purpose of this policy..."
                    rows={3}
                  />
                </Field>

                <Field label="Base Preset">
                  <Dropdown
                    value={policyPreset}
                    onOptionSelect={(_, data) => setPolicyPreset(data.optionValue as string)}
                  >
                    <Option value="Unrestricted">Unrestricted</Option>
                    <Option value="Minimal">Minimal</Option>
                    <Option value="Moderate">Moderate</Option>
                    <Option value="Strict">Strict</Option>
                  </Dropdown>
                </Field>

                <div className={styles.formRow}>
                  <Field>
                    <Switch
                      label="Enable Policy"
                      checked={policyEnabled}
                      onChange={(_, data) => setPolicyEnabled(data.checked)}
                    />
                  </Field>
                  <Field>
                    <Switch
                      label="Allow User Override"
                      checked={allowOverride}
                      onChange={(_, data) => setAllowOverride(data.checked)}
                    />
                  </Field>
                </div>

                <Divider />

                <div className={styles.keywordSection}>
                  <Text weight="semibold">Blocked Keywords</Text>
                  
                  <div className={styles.addKeywordRow}>
                    <Field label="Keyword" style={{ flex: 1 }}>
                      <Input
                        value={newKeyword}
                        onChange={(_, data) => setNewKeyword(data.value)}
                        placeholder="Enter keyword..."
                      />
                    </Field>
                    <Field label="Action">
                      <Dropdown
                        value={newKeywordAction}
                        onOptionSelect={(_, data) => setNewKeywordAction(data.optionValue as string)}
                      >
                        <Option value="Block">Block</Option>
                        <Option value="Warn">Warn</Option>
                        <Option value="AutoFix">Auto-Fix</Option>
                      </Dropdown>
                    </Field>
                    <div style={{ display: 'flex', alignItems: 'flex-end' }}>
                      <Button
                        appearance="primary"
                        icon={<Add24Regular />}
                        onClick={handleAddKeyword}
                        disabled={!newKeyword.trim()}
                      >
                        Add
                      </Button>
                    </div>
                  </div>

                  {keywords.length > 0 && (
                    <div className={styles.keywordList}>
                      {keywords.map((keyword, index) => (
                        <div key={index} className={styles.keywordItem}>
                          <div className={styles.keywordInfo}>
                            <Text style={{ fontFamily: 'monospace' }}>{keyword.keyword}</Text>
                            {getActionBadge(keyword.action)}
                          </div>
                          <Button
                            appearance="subtle"
                            size="small"
                            icon={<Dismiss24Regular />}
                            onClick={() => handleRemoveKeyword(index)}
                          />
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => setShowCreateDialog(false)}
              >
                Cancel
              </Button>
              <Button
                appearance="primary"
                onClick={handleCreatePolicy}
                disabled={!policyName.trim() || isLoading}
              >
                Create Policy
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={showEditDialog} onOpenChange={(_, data) => setShowEditDialog(data.open)}>
        <DialogSurface>
          <DialogTitle>Edit Safety Policy</DialogTitle>
          <DialogBody>
            <DialogContent>
              <div className={styles.dialogContent}>
                <Field label="Policy Name" required>
                  <Input
                    value={policyName}
                    onChange={(_, data) => setPolicyName(data.value)}
                    placeholder="Enter policy name..."
                  />
                </Field>

                <Field label="Description">
                  <Textarea
                    value={policyDescription}
                    onChange={(_, data) => setPolicyDescription(data.value)}
                    placeholder="Describe the purpose of this policy..."
                    rows={3}
                  />
                </Field>

                <div className={styles.formRow}>
                  <Field>
                    <Switch
                      label="Enable Policy"
                      checked={policyEnabled}
                      onChange={(_, data) => setPolicyEnabled(data.checked)}
                    />
                  </Field>
                  <Field>
                    <Switch
                      label="Allow User Override"
                      checked={allowOverride}
                      onChange={(_, data) => setAllowOverride(data.checked)}
                    />
                  </Field>
                </div>

                <Divider />

                <div className={styles.keywordSection}>
                  <Text weight="semibold">Blocked Keywords</Text>
                  
                  <div className={styles.addKeywordRow}>
                    <Field label="Keyword" style={{ flex: 1 }}>
                      <Input
                        value={newKeyword}
                        onChange={(_, data) => setNewKeyword(data.value)}
                        placeholder="Enter keyword..."
                      />
                    </Field>
                    <Field label="Action">
                      <Dropdown
                        value={newKeywordAction}
                        onOptionSelect={(_, data) => setNewKeywordAction(data.optionValue as string)}
                      >
                        <Option value="Block">Block</Option>
                        <Option value="Warn">Warn</Option>
                        <Option value="AutoFix">Auto-Fix</Option>
                      </Dropdown>
                    </Field>
                    <div style={{ display: 'flex', alignItems: 'flex-end' }}>
                      <Button
                        appearance="primary"
                        icon={<Add24Regular />}
                        onClick={handleAddKeyword}
                        disabled={!newKeyword.trim()}
                      >
                        Add
                      </Button>
                    </div>
                  </div>

                  {keywords.length > 0 && (
                    <div className={styles.keywordList}>
                      {keywords.map((keyword, index) => (
                        <div key={index} className={styles.keywordItem}>
                          <div className={styles.keywordInfo}>
                            <Text style={{ fontFamily: 'monospace' }}>{keyword.keyword}</Text>
                            {getActionBadge(keyword.action)}
                          </div>
                          <Button
                            appearance="subtle"
                            size="small"
                            icon={<Dismiss24Regular />}
                            onClick={() => handleRemoveKeyword(index)}
                          />
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => setShowEditDialog(false)}
              >
                Cancel
              </Button>
              <Button
                appearance="primary"
                onClick={handleEditPolicy}
                disabled={!policyName.trim() || isLoading}
              >
                Save Changes
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};
