import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Switch,
  Spinner,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Settings24Regular,
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  Add24Regular,
  Edit24Regular,
  Delete24Regular,
  Shield24Regular,
  Brain24Regular,
  Eye24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import { useUserPreferencesStore } from '../../state/userPreferences';
import type { CustomAudienceProfile, ContentFilteringPolicy } from '../../state/userPreferences';
import { ConfirmationDialog } from '../Dialogs/ConfirmationDialog';
import { CreatePolicyModal } from './CreatePolicyModal';
import { CreateProfileModal } from './CreateProfileModal';
import { EditPolicyModal } from './EditPolicyModal';
import { EditProfileModal } from './EditProfileModal';

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
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  card: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  profileItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    ':last-child': {
      borderBottom: 'none',
    },
  },
  profileInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  profileActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  advancedModeToggle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export const UserPreferencesTab: FC = () => {
  const styles = useStyles();
  const {
    customAudienceProfiles,
    contentFilteringPolicies,
    selectedAudienceProfileId,
    selectedFilteringPolicyId,
    advancedMode,
    isLoading,
    error,
    loadCustomAudienceProfiles,
    loadContentFilteringPolicies,
    selectAudienceProfile,
    selectFilteringPolicy,
    createCustomAudienceProfile,
    updateCustomAudienceProfile,
    deleteCustomAudienceProfile,
    createContentFilteringPolicy,
    updateContentFilteringPolicy,
    deleteContentFilteringPolicy,
    setAdvancedMode,
    exportPreferences,
    importPreferences,
  } = useUserPreferencesStore();

  const [importFile, setImportFile] = useState<File | null>(null);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const [createProfileOpen, setCreateProfileOpen] = useState(false);
  const [editProfileOpen, setEditProfileOpen] = useState(false);
  const [selectedProfile, setSelectedProfile] = useState<CustomAudienceProfile | null>(null);

  const [createPolicyOpen, setCreatePolicyOpen] = useState(false);
  const [editPolicyOpen, setEditPolicyOpen] = useState(false);
  const [selectedPolicy, setSelectedPolicy] = useState<ContentFilteringPolicy | null>(null);

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [itemToDelete, setItemToDelete] = useState<{
    type: 'profile' | 'policy';
    id: string;
    name: string;
  } | null>(null);

  useEffect(() => {
    loadCustomAudienceProfiles();
    loadContentFilteringPolicies();
  }, [loadCustomAudienceProfiles, loadContentFilteringPolicies]);

  const handleExport = async () => {
    try {
      const jsonData = await exportPreferences();
      const blob = new Blob([jsonData], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `aura-preferences-${new Date().toISOString().split('T')[0]}.json`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      setMessage({ type: 'success', text: 'Preferences exported successfully' });
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Export failed: ${errorObj.message}` });
    }
  };

  const handleImport = async () => {
    if (!importFile) return;

    try {
      const text = await importFile.text();
      await importPreferences(text);
      setMessage({ type: 'success', text: 'Preferences imported successfully' });
      setImportFile(null);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Import failed: ${errorObj.message}` });
    }
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setImportFile(file);
    }
  };

  const handleCreateProfile = async (
    profile: Omit<CustomAudienceProfile, 'id' | 'createdAt' | 'updatedAt'>
  ) => {
    try {
      await createCustomAudienceProfile(profile);
      setMessage({ type: 'success', text: 'Profile created successfully' });
      await loadCustomAudienceProfiles();
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Create failed: ${errorObj.message}` });
      throw errorObj;
    }
  };

  const handleEditProfile = (profile: CustomAudienceProfile) => {
    setSelectedProfile(profile);
    setEditProfileOpen(true);
  };

  const handleSaveProfile = async (id: string, profile: Partial<CustomAudienceProfile>) => {
    try {
      await updateCustomAudienceProfile(id, profile);
      setMessage({ type: 'success', text: 'Profile updated successfully' });
      await loadCustomAudienceProfiles();
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Update failed: ${errorObj.message}` });
      throw errorObj;
    }
  };

  const handleDeleteProfile = (id: string, name: string) => {
    setItemToDelete({ type: 'profile', id, name });
    setDeleteConfirmOpen(true);
  };

  const handleCreatePolicy = async (
    policy: Omit<ContentFilteringPolicy, 'id' | 'createdAt' | 'updatedAt'>
  ) => {
    try {
      await createContentFilteringPolicy(policy);
      setMessage({ type: 'success', text: 'Policy created successfully' });
      await loadContentFilteringPolicies();
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Create failed: ${errorObj.message}` });
      throw errorObj;
    }
  };

  const handleEditPolicy = (policy: ContentFilteringPolicy) => {
    setSelectedPolicy(policy);
    setEditPolicyOpen(true);
  };

  const handleSavePolicy = async (id: string, policy: Partial<ContentFilteringPolicy>) => {
    try {
      await updateContentFilteringPolicy(id, policy);
      setMessage({ type: 'success', text: 'Policy updated successfully' });
      await loadContentFilteringPolicies();
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Update failed: ${errorObj.message}` });
      throw errorObj;
    }
  };

  const handleDeletePolicy = (id: string, name: string) => {
    setItemToDelete({ type: 'policy', id, name });
    setDeleteConfirmOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!itemToDelete) return;

    try {
      if (itemToDelete.type === 'profile') {
        await deleteCustomAudienceProfile(itemToDelete.id);
        setMessage({ type: 'success', text: 'Profile deleted successfully' });
      } else {
        await deleteContentFilteringPolicy(itemToDelete.id);
        setMessage({ type: 'success', text: 'Policy deleted successfully' });
      }
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Delete failed: ${errorObj.message}` });
    }

    setItemToDelete(null);
    setDeleteConfirmOpen(false);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title3>User Preferences &amp; Customization</Title3>
          <Text>Customize AI behavior, content filtering, and generation settings</Text>
        </div>
        <div className={styles.actions}>
          <Button icon={<ArrowDownload24Regular />} onClick={handleExport} disabled={isLoading}>
            Export
          </Button>
          <label htmlFor="import-file">
            <Button
              icon={<ArrowUpload24Regular />}
              onClick={() => document.getElementById('import-file')?.click()}
              disabled={isLoading}
            >
              Import
            </Button>
          </label>
          <input
            id="import-file"
            type="file"
            accept=".json"
            style={{ display: 'none' }}
            onChange={handleFileSelect}
          />
          {importFile && (
            <Button appearance="primary" onClick={handleImport}>
              Confirm Import
            </Button>
          )}
        </div>
      </div>

      {message && (
        <MessageBar intent={message.type === 'error' ? 'error' : 'success'}>
          <MessageBarBody>
            <MessageBarTitle>{message.type === 'error' ? 'Error' : 'Success'}</MessageBarTitle>
            {message.text}
          </MessageBarBody>
        </MessageBar>
      )}

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.advancedModeToggle}>
        <Settings24Regular />
        <Text weight="semibold">Advanced Mode</Text>
        <Switch checked={advancedMode} onChange={(_, data) => setAdvancedMode(data.checked)} />
        <Text size={200}>
          {advancedMode
            ? 'All customization options visible'
            : 'Basic options only (toggle to see all)'}
        </Text>
      </div>

      {isLoading ? (
        <Spinner label="Loading preferences..." />
      ) : (
        <Accordion multiple collapsible>
          <AccordionItem value="audience">
            <AccordionHeader icon={<Eye24Regular />}>
              Custom Audience Profiles ({customAudienceProfiles.length})
            </AccordionHeader>
            <AccordionPanel>
              <Card className={styles.card}>
                <div className={styles.sectionHeader}>
                  <Text weight="semibold">Manage custom audience profiles</Text>
                  <Button
                    icon={<Add24Regular />}
                    appearance="primary"
                    size="small"
                    onClick={() => setCreateProfileOpen(true)}
                  >
                    Create Profile
                  </Button>
                </div>

                {customAudienceProfiles.length === 0 ? (
                  <div className={styles.emptyState}>
                    <Text>No custom audience profiles yet. Create one to get started.</Text>
                  </div>
                ) : (
                  customAudienceProfiles.map((profile) => (
                    <div key={profile.id} className={styles.profileItem}>
                      <div className={styles.profileInfo}>
                        <Text weight="semibold">{profile.name}</Text>
                        <Text size={200}>
                          Age: {profile.minAge}-{profile.maxAge} | Formality:{' '}
                          {profile.formalityLevel}/10
                        </Text>
                        {profile.description && <Text size={200}>{profile.description}</Text>}
                      </div>
                      <div className={styles.profileActions}>
                        <Button
                          size="small"
                          appearance={
                            selectedAudienceProfileId === profile.id ? 'primary' : 'secondary'
                          }
                          onClick={() => selectAudienceProfile(profile.id)}
                        >
                          {selectedAudienceProfileId === profile.id ? 'Selected' : 'Select'}
                        </Button>
                        <Button
                          size="small"
                          icon={<Edit24Regular />}
                          onClick={() => handleEditProfile(profile)}
                        />
                        <Button
                          size="small"
                          icon={<Delete24Regular />}
                          onClick={() => handleDeleteProfile(profile.id, profile.name)}
                        />
                      </div>
                    </div>
                  ))
                )}
              </Card>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="filtering">
            <AccordionHeader icon={<Shield24Regular />}>
              Content Filtering Policies ({contentFilteringPolicies.length})
            </AccordionHeader>
            <AccordionPanel>
              <Card className={styles.card}>
                <div className={styles.sectionHeader}>
                  <Text weight="semibold">Manage content filtering policies</Text>
                  <Button
                    icon={<Add24Regular />}
                    appearance="primary"
                    size="small"
                    onClick={() => setCreatePolicyOpen(true)}
                  >
                    Create Policy
                  </Button>
                </div>

                {contentFilteringPolicies.length === 0 ? (
                  <div className={styles.emptyState}>
                    <Text>No content filtering policies yet. Create one to get started.</Text>
                  </div>
                ) : (
                  contentFilteringPolicies.map((policy) => (
                    <div key={policy.id} className={styles.profileItem}>
                      <div className={styles.profileInfo}>
                        <Text weight="semibold">{policy.name}</Text>
                        <Text size={200}>
                          Filtering: {policy.filteringEnabled ? 'Enabled' : 'Disabled'} | Profanity:{' '}
                          {policy.profanityFilter}
                        </Text>
                        {policy.description && <Text size={200}>{policy.description}</Text>}
                      </div>
                      <div className={styles.profileActions}>
                        <Button
                          size="small"
                          appearance={
                            selectedFilteringPolicyId === policy.id ? 'primary' : 'secondary'
                          }
                          onClick={() => selectFilteringPolicy(policy.id)}
                        >
                          {selectedFilteringPolicyId === policy.id ? 'Selected' : 'Select'}
                        </Button>
                        <Button
                          size="small"
                          icon={<Edit24Regular />}
                          onClick={() => handleEditPolicy(policy)}
                        />
                        <Button
                          size="small"
                          icon={<Delete24Regular />}
                          onClick={() => handleDeletePolicy(policy.id, policy.name)}
                        />
                      </div>
                    </div>
                  ))
                )}
              </Card>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="ai-behavior">
            <AccordionHeader icon={<Brain24Regular />}>AI Behavior Settings</AccordionHeader>
            <AccordionPanel>
              <Card className={styles.card}>
                <Text>
                  AI Behavior customization coming soon. This will allow you to control LLM
                  parameters, prompts, and behavior for each pipeline stage.
                </Text>
              </Card>
            </AccordionPanel>
          </AccordionItem>
        </Accordion>
      )}

      <CreateProfileModal
        open={createProfileOpen}
        onOpenChange={setCreateProfileOpen}
        onSave={handleCreateProfile}
      />

      <EditProfileModal
        open={editProfileOpen}
        onOpenChange={setEditProfileOpen}
        profile={selectedProfile}
        onSave={handleSaveProfile}
      />

      <CreatePolicyModal
        open={createPolicyOpen}
        onOpenChange={setCreatePolicyOpen}
        onSave={handleCreatePolicy}
      />

      <EditPolicyModal
        open={editPolicyOpen}
        onOpenChange={setEditPolicyOpen}
        policy={selectedPolicy}
        onSave={handleSavePolicy}
      />

      <ConfirmationDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={`Delete ${itemToDelete?.type === 'profile' ? 'Profile' : 'Policy'}`}
        message={`Are you sure you want to delete "${itemToDelete?.name}"? This action cannot be undone.`}
        confirmLabel="Delete"
        cancelLabel="Cancel"
        variant="destructive"
        onConfirm={handleConfirmDelete}
      />
    </div>
  );
};
