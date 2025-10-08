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

  useEffect(() => {
    fetchSettings();
    fetchProfiles();
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
          <div className={styles.form}>
            <Field label="OpenAI API Key">
              <Input type="password" placeholder="sk-..." />
            </Field>
            <Field label="ElevenLabs API Key">
              <Input type="password" placeholder="..." />
            </Field>
            <Field label="Pexels API Key">
              <Input type="password" placeholder="..." />
            </Field>
            <Text size={200}>
              API keys are stored securely using DPAPI on Windows
            </Text>
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
