import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Text,
  Button,
  Input,
  Switch,
  Card,
  Field,
  Tab,
  TabList,
} from '@fluentui/react-components';
import { Save24Regular } from '@fluentui/react-icons';
import type { Profile } from '../types';

const useStyles = makeStyles({
  container: {
    maxWidth: '800px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  section: {
    padding: tokens.spacingVerticalL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalXL,
  },
  profileCard: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
});

export function SettingsPage() {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState('system');
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [offlineMode, setOfflineMode] = useState(false);
  const [settings, setSettings] = useState<any>({});
  
  // API Keys state
  const [apiKeys, setApiKeys] = useState({
    openai: '',
    elevenlabs: '',
    pexels: '',
    stabilityai: '',
  });
  const [keysModified, setKeysModified] = useState(false);
  const [savingKeys, setSavingKeys] = useState(false);

  useEffect(() => {
    fetchSettings();
    fetchProfiles();
    fetchApiKeys();
  }, []);

  const fetchSettings = async () => {
    try {
      const response = await fetch('/api/settings/load');
      if (response.ok) {
        const data = await response.json();
        setSettings(data);
        setOfflineMode(data.offlineMode || false);
      }
    } catch (error) {
      console.error('Error fetching settings:', error);
    }
  };

  const fetchProfiles = async () => {
    try {
      const response = await fetch('/api/profiles/list');
      if (response.ok) {
        const data = await response.json();
        setProfiles(data.profiles || []);
      }
    } catch (error) {
      console.error('Error fetching profiles:', error);
    }
  };

  const fetchApiKeys = async () => {
    try {
      const response = await fetch('/api/apikeys/load');
      if (response.ok) {
        const data = await response.json();
        setApiKeys({
          openai: data.openai || '',
          elevenlabs: data.elevenlabs || '',
          pexels: data.pexels || '',
          stabilityai: data.stabilityai || '',
        });
      }
    } catch (error) {
      console.error('Error fetching API keys:', error);
    }
  };

  const saveSettings = async () => {
    try {
      const response = await fetch('/api/settings/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...settings, offlineMode }),
      });
      if (response.ok) {
        alert('Settings saved successfully');
      }
    } catch (error) {
      console.error('Error saving settings:', error);
      alert('Error saving settings');
    }
  };

  const saveApiKeys = async () => {
    setSavingKeys(true);
    try {
      const response = await fetch('/api/apikeys/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          openAiKey: apiKeys.openai,
          elevenLabsKey: apiKeys.elevenlabs,
          pexelsKey: apiKeys.pexels,
          stabilityAiKey: apiKeys.stabilityai,
        }),
      });
      if (response.ok) {
        alert('API keys saved successfully');
        setKeysModified(false);
      } else {
        alert('Error saving API keys');
      }
    } catch (error) {
      console.error('Error saving API keys:', error);
      alert('Error saving API keys');
    } finally {
      setSavingKeys(false);
    }
  };

  const applyProfile = async (profileName: string) => {
    try {
      const response = await fetch('/api/profiles/apply', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ profileName }),
      });
      if (response.ok) {
        alert(`Profile "${profileName}" applied successfully`);
      }
    } catch (error) {
      console.error('Error applying profile:', error);
    }
  };

  const updateApiKey = (key: keyof typeof apiKeys, value: string) => {
    setApiKeys(prev => ({ ...prev, [key]: value }));
    setKeysModified(true);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Settings</Title1>
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as string)}
      >
        <Tab value="system">System</Tab>
        <Tab value="providers">Providers</Tab>
        <Tab value="apikeys">API Keys</Tab>
        <Tab value="privacy">Privacy</Tab>
      </TabList>

      {activeTab === 'system' && (
        <Card className={styles.section}>
          <Title2>System Profile</Title2>
          <div className={styles.form}>
            <Field label="Offline Mode">
              <Switch
                checked={offlineMode}
                onChange={(_, data) => setOfflineMode(data.checked)}
                label={offlineMode ? 'On' : 'Off'}
              />
              <Text size={200}>Blocks all network providers. Only local and stock assets are used.</Text>
            </Field>

            <Button
              onClick={async () => {
                try {
                  const response = await fetch('/api/probes/run', { method: 'POST' });
                  if (response.ok) {
                    alert('Hardware probes completed successfully');
                  }
                } catch (error) {
                  console.error('Error running probes:', error);
                }
              }}
            >
              Run Hardware Probes
            </Button>
          </div>
        </Card>
      )}

      {activeTab === 'providers' && (
        <Card className={styles.section}>
          <Title2>Provider Profiles</Title2>
          <Text>Select a provider profile to configure which services are used</Text>
          <div style={{ marginTop: tokens.spacingVerticalL }}>
            {profiles.map((profile) => (
              <div
                key={profile.name}
                className={styles.profileCard}
                onClick={() => applyProfile(profile.name)}
              >
                <Text weight="semibold">{profile.name}</Text>
                <br />
                <Text size={200}>{profile.description}</Text>
              </div>
            ))}
          </div>
        </Card>
      )}

      {activeTab === 'apikeys' && (
        <Card className={styles.section}>
          <Title2>API Keys</Title2>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
            Configure API keys for external services. Keys are stored securely.
          </Text>
          <div className={styles.form}>
            <Field label="OpenAI API Key" hint="Required for GPT-based script generation">
              <Input 
                type="password" 
                placeholder="sk-..." 
                value={apiKeys.openai}
                onChange={(e) => updateApiKey('openai', e.target.value)}
              />
            </Field>
            <Field label="ElevenLabs API Key" hint="Required for high-quality voice synthesis">
              <Input 
                type="password" 
                placeholder="..." 
                value={apiKeys.elevenlabs}
                onChange={(e) => updateApiKey('elevenlabs', e.target.value)}
              />
            </Field>
            <Field label="Pexels API Key" hint="Required for stock video and images">
              <Input 
                type="password" 
                placeholder="..." 
                value={apiKeys.pexels}
                onChange={(e) => updateApiKey('pexels', e.target.value)}
              />
            </Field>
            <Field label="Stability AI API Key" hint="Optional - for AI image generation">
              <Input 
                type="password" 
                placeholder="..." 
                value={apiKeys.stabilityai}
                onChange={(e) => updateApiKey('stabilityai', e.target.value)}
              />
            </Field>
            {keysModified && (
              <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
                ⚠️ You have unsaved changes
              </Text>
            )}
            <Button 
              appearance="primary" 
              onClick={saveApiKeys}
              disabled={!keysModified || savingKeys}
            >
              {savingKeys ? 'Saving...' : 'Save API Keys'}
            </Button>
          </div>
        </Card>
      )}

      {activeTab === 'privacy' && (
        <Card className={styles.section}>
          <Title2>Privacy Settings</Title2>
          <div className={styles.form}>
            <Field label="Telemetry">
              <Switch label="Off (default)" />
              <Text size={200}>Send anonymous usage data to improve the app</Text>
            </Field>
            <Field label="Crash Reports">
              <Switch label="Off (default)" />
              <Text size={200}>Send crash reports to help diagnose issues</Text>
            </Field>
          </div>
        </Card>
      )}

      <div className={styles.actions}>
        <Button appearance="primary" icon={<Save24Regular />} onClick={saveSettings}>
          Save Settings
        </Button>
      </div>
    </div>
  );
}
