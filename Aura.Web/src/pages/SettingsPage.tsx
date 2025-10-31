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
  Slider,
} from '@fluentui/react-components';
import { Save24Regular, ArrowDownload24Regular, ArrowUpload24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { ValidatedInput } from '../components/forms/ValidatedInput';
import { AIOptimizationPanel } from '../components/Settings/AIOptimizationPanel';
import { ApiKeysSettingsTab } from '../components/Settings/ApiKeysSettingsTab';
import { EditorPreferencesSettingsTab } from '../components/Settings/EditorPreferencesSettingsTab';
import { FileLocationsSettingsTab } from '../components/Settings/FileLocationsSettingsTab';
import { GeneralSettingsTab } from '../components/Settings/GeneralSettingsTab';
import { KeyboardShortcutsTab } from '../components/Settings/KeyboardShortcutsTab';
import { LocalEngines } from '../components/Settings/LocalEngines';
import { LoggingSettingsTab } from '../components/Settings/LoggingSettingsTab';
import { OutputSettingsTab } from '../components/Settings/OutputSettingsTab';
import { PerformanceSettingsTab } from '../components/Settings/PerformanceSettingsTab';
import { ProvidersTable } from '../components/Settings/ProvidersTable';
import { ThemeCustomizationTab } from '../components/Settings/ThemeCustomizationTab';
import { VideoDefaultsSettingsTab } from '../components/Settings/VideoDefaultsSettingsTab';
import { apiUrl } from '../config/api';
import { useFormValidation } from '../hooks/useFormValidation';
import { resetFirstRunStatus } from '../services/firstRunService';
import { settingsService } from '../services/settingsService';
import type { Profile } from '../types';
import type { UserSettings } from '../types/settings';
import { createDefaultSettings } from '../types/settings';
import { apiKeysSchema, providerPathsSchema } from '../utils/formValidation';
import { RescanPanel } from './DownloadCenter/RescanPanel';

const useStyles = makeStyles({
  container: {
    maxWidth: '900px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  section: {
    padding: tokens.spacingVerticalXL,
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
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
    },
  },
});

interface ValidationResult {
  name: string;
  ok: boolean;
  details: string;
  durationMs?: number;
  elapsedMs?: number;
}

interface ValidationResults {
  ok: boolean;
  results: ValidationResult[];
}

interface CustomProfile {
  name: string;
  settings: {
    offlineMode: boolean;
    uiScale: number;
    compactMode: boolean;
  };
  providerPaths: Record<string, string>;
  timestamp: string;
}

export function SettingsPage() {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState('general');
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [offlineMode, setOfflineMode] = useState(false);
  const [settings, setSettings] = useState<Record<string, unknown>>({});

  // Comprehensive user settings (new)
  const [userSettings, setUserSettings] = useState<UserSettings>(createDefaultSettings());
  const [originalSettings, setOriginalSettings] = useState<UserSettings>(createDefaultSettings());
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  // UI Settings state
  const [uiScale, setUiScale] = useState(100);
  const [compactMode, setCompactMode] = useState(false);

  // API Keys state
  const [apiKeys, setApiKeys] = useState({
    openai: '',
    elevenlabs: '',
    pexels: '',
    pixabay: '',
    unsplash: '',
    stabilityai: '',
  });
  const [keysModified, setKeysModified] = useState(false);
  const [savingKeys, setSavingKeys] = useState(false);

  // Local Provider Paths state
  const [providerPaths, setProviderPaths] = useState({
    stableDiffusionUrl: 'http://127.0.0.1:7860',
    ollamaUrl: 'http://127.0.0.1:11434',
    ffmpegPath: '',
    ffprobePath: '',
    outputDirectory: '',
  });
  const [pathsModified, setPathsModified] = useState(false);
  const [savingPaths, setSavingPaths] = useState(false);
  const [testResults, setTestResults] = useState<
    Record<string, { success: boolean; message: string } | null>
  >({});

  // Ollama models state
  const [ollamaModels, setOllamaModels] = useState<
    Array<{ name: string; size: number; sizeGB: number }>
  >([]);
  const [selectedOllamaModel, setSelectedOllamaModel] = useState<string>('llama3.1:8b-q4_k_m');
  const [loadingOllamaModels, setLoadingOllamaModels] = useState(false);

  // Provider validation state
  const [validating, setValidating] = useState(false);
  const [validationResults, setValidationResults] = useState<ValidationResults | null>(null);

  // Profile templates state
  const [customProfileName, setCustomProfileName] = useState('');
  const [savedProfiles, setSavedProfiles] = useState<CustomProfile[]>([]);

  // Portable mode state (read-only - portable is always enabled)
  const [portableRootPath, setPortableRootPath] = useState('');
  const [toolsDirectory, setToolsDirectory] = useState('');
  const [auraDataDirectory, setAuraDataDirectory] = useState('');
  const [logsDirectory, setLogsDirectory] = useState('');
  const [projectsDirectory, setProjectsDirectory] = useState('');
  const [downloadsDirectory, setDownloadsDirectory] = useState('');

  // Form validation for API keys
  const {
    values: apiKeyValues,
    errors: apiKeyErrors,
    setValue: setApiKeyValue,
  } = useFormValidation({
    schema: apiKeysSchema,
    initialValues: apiKeys,
    debounceMs: 500,
  });

  // Form validation for provider paths
  const {
    values: providerPathValues,
    errors: providerPathErrors,
    setValue: setProviderPathValue,
  } = useFormValidation({
    schema: providerPathsSchema,
    initialValues: providerPaths,
    debounceMs: 500,
  });

  useEffect(() => {
    loadUserSettings();
    fetchSettings();
    fetchProfiles();
    fetchApiKeys();
    fetchProviderPaths();
    fetchPortableModeSettings();
    fetchSelectedOllamaModel();
  }, []);

  // Apply UI scale on load
  useEffect(() => {
    if (uiScale !== 100) {
      document.documentElement.style.fontSize = `${uiScale}%`;
    }
  }, [uiScale]);

  // Track unsaved changes
  useEffect(() => {
    const hasChanges = JSON.stringify(userSettings) !== JSON.stringify(originalSettings);
    setHasUnsavedChanges(hasChanges);
  }, [userSettings, originalSettings]);

  // Load comprehensive user settings
  const loadUserSettings = async () => {
    try {
      const loaded = await settingsService.loadSettings();
      setUserSettings(loaded);
      setOriginalSettings(loaded);
    } catch (error) {
      console.error('Error loading user settings:', error);
    }
  };

  const fetchSettings = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/load'));
      if (response.ok) {
        const data = await response.json();
        setSettings(data);
        setOfflineMode(data.offlineMode || false);
        setUiScale(data.uiScale || 100);
        setCompactMode(data.compactMode || false);
      }
    } catch (error) {
      console.error('Error fetching settings:', error);
    }
  };

  const fetchProfiles = async () => {
    try {
      const response = await fetch(apiUrl('/api/profiles/list'));
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
      const response = await fetch(apiUrl('/api/apikeys/load'));
      if (response.ok) {
        const data = await response.json();
        setApiKeys({
          openai: data.openai || '',
          elevenlabs: data.elevenlabs || '',
          pexels: data.pexels || '',
          pixabay: data.pixabay || '',
          unsplash: data.unsplash || '',
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
        body: JSON.stringify({
          ...settings,
          offlineOnly: offlineMode,
          uiScale,
          compactMode,
        }),
      });
      if (response.ok) {
        alert('Settings saved successfully');
        // Apply UI scale by updating document root font size
        document.documentElement.style.fontSize = `${uiScale}%`;
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
          pixabayKey: apiKeys.pixabay,
          unsplashKey: apiKeys.unsplash,
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
    setApiKeys((prev) => ({ ...prev, [key]: value }));
    setKeysModified(true);
  };

  const fetchProviderPaths = async () => {
    try {
      const response = await fetch(apiUrl('/api/providers/paths/load'));
      if (response.ok) {
        const data = await response.json();
        setProviderPaths({
          stableDiffusionUrl: data.stableDiffusionUrl || 'http://127.0.0.1:7860',
          ollamaUrl: data.ollamaUrl || 'http://127.0.0.1:11434',
          ffmpegPath: data.ffmpegPath || '',
          ffprobePath: data.ffprobePath || '',
          outputDirectory: data.outputDirectory || '',
        });
      }
    } catch (error) {
      console.error('Error fetching provider paths:', error);
    }
  };

  const saveProviderPaths = async () => {
    setSavingPaths(true);
    try {
      const response = await fetch('/api/providers/paths/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(providerPaths),
      });
      if (response.ok) {
        alert('Provider paths saved successfully');
        setPathsModified(false);
      } else {
        alert('Error saving provider paths');
      }
    } catch (error) {
      console.error('Error saving provider paths:', error);
      alert('Error saving provider paths');
    } finally {
      setSavingPaths(false);
    }
  };

  const updateProviderPath = (key: keyof typeof providerPaths, value: string) => {
    setProviderPaths((prev) => ({ ...prev, [key]: value }));
    setPathsModified(true);
    // Clear test result when path changes
    setTestResults((prev) => ({ ...prev, [key]: null }));
  };

  const testProvider = async (provider: string, url?: string, path?: string) => {
    try {
      const response = await fetch(`/api/providers/test/${provider}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url, path }),
      });
      if (response.ok) {
        const data = await response.json();
        setTestResults((prev) => ({ ...prev, [provider]: data }));
      } else {
        setTestResults((prev) => ({
          ...prev,
          [provider]: { success: false, message: 'Failed to test connection' },
        }));
      }
    } catch (error) {
      console.error(`Error testing ${provider}:`, error);
      setTestResults((prev) => ({
        ...prev,
        [provider]: { success: false, message: `Network error: ${error}` },
      }));
    }
  };

  const fetchOllamaModels = async () => {
    setLoadingOllamaModels(true);
    try {
      const url = providerPaths.ollamaUrl || 'http://127.0.0.1:11434';
      const response = await fetch(
        apiUrl(`/api/engines/ollama/models?url=${encodeURIComponent(url)}`)
      );
      if (response.ok) {
        const data = await response.json();
        setOllamaModels(data.models || []);
      } else {
        console.error('Failed to fetch Ollama models');
        setOllamaModels([]);
      }
    } catch (error) {
      console.error('Error fetching Ollama models:', error);
      setOllamaModels([]);
    } finally {
      setLoadingOllamaModels(false);
    }
  };

  const fetchSelectedOllamaModel = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/ollama/model'));
      if (response.ok) {
        const data = await response.json();
        if (data.success && data.model) {
          setSelectedOllamaModel(data.model);
        }
      }
    } catch (error) {
      console.error('Error fetching selected Ollama model:', error);
    }
  };

  const saveOllamaModel = async (model: string) => {
    try {
      const response = await fetch(apiUrl('/api/settings/ollama/model'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ model }),
      });
      if (response.ok) {
        setSelectedOllamaModel(model);
      } else {
        console.error('Failed to save Ollama model');
      }
    } catch (error) {
      console.error('Error saving Ollama model:', error);
    }
  };

  const validateProviders = async () => {
    setValidating(true);
    setValidationResults(null);
    try {
      const response = await fetch('/api/providers/validate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ providers: [] }), // Empty array means validate all
      });
      if (response.ok) {
        const data = await response.json();
        setValidationResults(data);
      } else {
        alert('Error validating providers');
      }
    } catch (error) {
      console.error('Error validating providers:', error);
      alert('Error validating providers');
    } finally {
      setValidating(false);
    }
  };

  const copyValidationResults = () => {
    if (!validationResults) return;

    let text = 'Provider Validation Results\n\n';
    validationResults.results.forEach((result: ValidationResult) => {
      text += `${result.name}: ${result.ok ? 'OK' : 'Failed'} - ${result.details} (${result.elapsedMs}ms)\n`;
    });
    text += `\nOverall: ${validationResults.ok ? 'All providers validated successfully' : 'Some providers failed'}`;

    navigator.clipboard
      .writeText(text)
      .then(() => {
        alert('Results copied to clipboard!');
      })
      .catch((err) => {
        console.error('Failed to copy:', err);
      });
  };

  const exportSettings = () => {
    const settingsData = {
      version: '1.0.0',
      exported: new Date().toISOString(),
      settings: {
        offlineMode,
        uiScale,
        compactMode,
      },
      apiKeys,
      providerPaths,
      profiles,
    };

    const blob = new Blob([JSON.stringify(settingsData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `aura-settings-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const importSettings = async (file: File) => {
    try {
      const text = await file.text();
      const data = JSON.parse(text);

      // Validate schema
      if (!data.version || !data.settings) {
        alert('Invalid settings file format');
        return;
      }

      // Apply settings
      if (data.settings) {
        setOfflineMode(data.settings.offlineMode ?? false);
        setUiScale(data.settings.uiScale ?? 100);
        setCompactMode(data.settings.compactMode ?? false);
      }

      if (data.apiKeys) {
        setApiKeys(data.apiKeys);
        setKeysModified(true);
      }

      if (data.providerPaths) {
        setProviderPaths(data.providerPaths);
        setPathsModified(true);
      }

      alert('Settings imported successfully! Remember to save.');
    } catch (error) {
      console.error('Error importing settings:', error);
      alert('Error importing settings: Invalid JSON file');
    }
  };

  const applyProfileTemplate = (template: string) => {
    const confirm = window.confirm(
      `Apply "${template}" template? This will update your provider preferences but not overwrite API keys.`
    );
    if (!confirm) return;

    switch (template) {
      case 'free-only':
        setSettings((prev: Record<string, unknown>) => ({
          ...prev,
          preferredScriptProvider: 'Template',
          preferredTtsProvider: 'WindowsTTS',
          preferredVisualsProvider: 'LocalStock',
        }));
        setOfflineMode(true);
        break;
      case 'balanced-mix':
        setSettings((prev: Record<string, unknown>) => ({
          ...prev,
          preferredScriptProvider: 'GPT4',
          preferredTtsProvider: 'ElevenLabs',
          preferredVisualsProvider: 'LocalStock',
        }));
        setOfflineMode(false);
        break;
      case 'pro-max':
        setSettings((prev: Record<string, unknown>) => ({
          ...prev,
          preferredScriptProvider: 'GPT4',
          preferredTtsProvider: 'ElevenLabs',
          preferredVisualsProvider: 'StabilityAI',
        }));
        setOfflineMode(false);
        break;
    }

    alert(`"${template}" template applied! Remember to save settings.`);
  };

  const saveCustomProfile = () => {
    if (!customProfileName.trim()) return;

    const profile = {
      name: customProfileName,
      settings: {
        offlineMode,
        uiScale,
        compactMode,
      },
      providerPaths,
      timestamp: new Date().toISOString(),
    };

    const existing = savedProfiles.filter((p) => p.name !== customProfileName);
    const updated = [...existing, profile];
    setSavedProfiles(updated);
    localStorage.setItem('aura-custom-profiles', JSON.stringify(updated));

    alert(`Profile "${customProfileName}" saved!`);
    setCustomProfileName('');
  };

  const loadCustomProfiles = () => {
    try {
      const stored = localStorage.getItem('aura-custom-profiles');
      if (stored) {
        setSavedProfiles(JSON.parse(stored));
      } else {
        alert('No saved profiles found');
      }
    } catch (error) {
      console.error('Error loading profiles:', error);
      alert('Error loading profiles');
    }
  };

  const loadCustomProfile = (profile: CustomProfile) => {
    if (profile.settings) {
      setOfflineMode(profile.settings.offlineMode ?? false);
      setUiScale(profile.settings.uiScale ?? 100);
      setCompactMode(profile.settings.compactMode ?? false);
    }

    if (profile.providerPaths) {
      setProviderPaths({
        stableDiffusionUrl: profile.providerPaths.stableDiffusionUrl || '',
        ollamaUrl: profile.providerPaths.ollamaUrl || '',
        ffmpegPath: profile.providerPaths.ffmpegPath || '',
        ffprobePath: profile.providerPaths.ffprobePath || '',
        outputDirectory: profile.providerPaths.outputDirectory || '',
      });
      setPathsModified(true);
    }

    alert(`Profile "${profile.name}" loaded! Remember to save settings.`);
  };

  const deleteCustomProfile = (name: string) => {
    const confirm = window.confirm(`Delete profile "${name}"?`);
    if (!confirm) return;

    const updated = savedProfiles.filter((p) => p.name !== name);
    setSavedProfiles(updated);
    localStorage.setItem('aura-custom-profiles', JSON.stringify(updated));
    alert(`Profile "${name}" deleted`);
  };

  // Save comprehensive user settings
  const saveUserSettings = async () => {
    try {
      const success = await settingsService.saveSettings(userSettings);
      if (success) {
        setOriginalSettings({ ...userSettings });
        alert('Settings saved successfully!');
      } else {
        alert('Failed to save settings to backend, but saved locally.');
      }
    } catch (error) {
      console.error('Error saving settings:', error);
      alert('Error saving settings');
    }
  };

  // Export settings
  const exportUserSettings = () => {
    settingsService.exportSettings(userSettings);
  };

  // Import settings
  const importUserSettings = async () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (file) {
        const imported = await settingsService.importSettings(file);
        if (imported) {
          setUserSettings(imported);
          alert('Settings imported successfully! Remember to save.');
        } else {
          alert('Failed to import settings. Please check the file format.');
        }
      }
    };
    input.click();
  };

  // Reset settings to defaults
  const resetUserSettings = async () => {
    if (!confirm('Reset all settings to defaults? This cannot be undone.')) {
      return;
    }
    try {
      const defaults = await settingsService.resetToDefaults();
      setUserSettings(defaults);
      setOriginalSettings(defaults);
      alert('Settings reset to defaults');
    } catch (error) {
      console.error('Error resetting settings:', error);
      alert('Error resetting settings');
    }
  };

  const fetchPortableModeSettings = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/portable'));
      if (response.ok) {
        const data = await response.json();
        setPortableRootPath(data.portableRootPath || '');
        setToolsDirectory(data.toolsDirectory || '');
        setAuraDataDirectory(data.auraDataDirectory || '');
        setLogsDirectory(data.logsDirectory || '');
        setProjectsDirectory(data.projectsDirectory || '');
        setDownloadsDirectory(data.downloadsDirectory || '');
      }
    } catch (error) {
      console.error('Error fetching portable settings:', error);
    }
  };

  const openToolsFolder = async () => {
    try {
      const response = await fetch('/api/settings/open-tools-folder', {
        method: 'POST',
      });
      if (response.ok) {
        await response.json();
        // Folder opened successfully
      } else {
        alert('Error opening tools folder');
      }
    } catch (error) {
      console.error('Error opening tools folder:', error);
      alert('Error opening tools folder');
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Settings</Title1>
        <Text className={styles.subtitle}>
          Configure system preferences, providers, and API keys
        </Text>
        {hasUnsavedChanges && (
          <Text style={{ color: tokens.colorPaletteYellowForeground1, fontWeight: 600 }}>
            ⚠️ You have unsaved changes
          </Text>
        )}
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as string)}
      >
        <Tab value="general">General</Tab>
        <Tab value="apikeys">API Keys</Tab>
        <Tab value="filelocations">File Locations</Tab>
        <Tab value="videodefaults">Video Defaults</Tab>
        <Tab value="editorpreferences">Editor</Tab>
        <Tab value="system">System</Tab>
        <Tab value="output">Output</Tab>
        <Tab value="performance">Performance</Tab>
        <Tab value="ui">UI</Tab>
        <Tab value="theme">Theme</Tab>
        <Tab value="shortcuts">Shortcuts</Tab>
        <Tab value="logging">Logging</Tab>
        <Tab value="portable">Portable Info</Tab>
        <Tab value="providers">Providers</Tab>
        <Tab value="localproviders">Local Providers</Tab>
        <Tab value="localengines">Local Engines</Tab>
        <Tab value="aioptimization">AI Optimization</Tab>
        <Tab value="importexport">Import/Export</Tab>
        <Tab value="privacy">Privacy</Tab>
      </TabList>

      {activeTab === 'general' && (
        <GeneralSettingsTab
          settings={userSettings.general}
          onChange={(general) => setUserSettings({ ...userSettings, general })}
          onSave={saveUserSettings}
          hasChanges={hasUnsavedChanges}
        />
      )}

      {activeTab === 'apikeys' && (
        <ApiKeysSettingsTab
          settings={userSettings.apiKeys}
          onChange={(apiKeys) => setUserSettings({ ...userSettings, apiKeys })}
          onSave={saveUserSettings}
          onTestApiKey={async (provider, apiKey) => settingsService.testApiKey(provider, apiKey)}
          hasChanges={hasUnsavedChanges}
        />
      )}

      {activeTab === 'filelocations' && (
        <FileLocationsSettingsTab
          settings={userSettings.fileLocations}
          onChange={(fileLocations) => setUserSettings({ ...userSettings, fileLocations })}
          onSave={saveUserSettings}
          onValidatePath={async (path) => settingsService.validatePath(path)}
          hasChanges={hasUnsavedChanges}
        />
      )}

      {activeTab === 'videodefaults' && (
        <VideoDefaultsSettingsTab
          settings={userSettings.videoDefaults}
          onChange={(videoDefaults) => setUserSettings({ ...userSettings, videoDefaults })}
          onSave={saveUserSettings}
          hasChanges={hasUnsavedChanges}
        />
      )}

      {activeTab === 'editorpreferences' && (
        <EditorPreferencesSettingsTab
          settings={userSettings.editorPreferences}
          onChange={(editorPreferences) => setUserSettings({ ...userSettings, editorPreferences })}
          onSave={saveUserSettings}
          hasChanges={hasUnsavedChanges}
        />
      )}

      {activeTab === 'system' && (
        <Card className={styles.section}>
          <Title2>System Profile</Title2>
          <div className={styles.form}>
            <Field label="Offline Mode">
              <Switch checked={offlineMode} onChange={(_, data) => setOfflineMode(data.checked)} />
              <Text size={200}>
                {offlineMode ? 'Enabled' : 'Disabled'} - Blocks all cloud providers. Only local and
                stock assets are used.
              </Text>
            </Field>

            <div style={{ display: 'flex', gap: tokens.spacingHorizontalM }}>
              <Button appearance="primary" onClick={saveSettings}>
                Save Settings
              </Button>
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
              <Button
                onClick={() => {
                  if (
                    confirm(
                      'This will guide you through setup again. Current settings will be preserved. Continue?'
                    )
                  ) {
                    window.location.href = '/setup?rerun=true';
                  }
                }}
              >
                Re-run Setup Wizard
              </Button>
              <Button
                onClick={async () => {
                  if (
                    confirm(
                      'This will reset the first-run wizard and restart onboarding. You will be redirected to the onboarding wizard. Continue?'
                    )
                  ) {
                    try {
                      await resetFirstRunStatus();
                      window.location.href = '/onboarding';
                    } catch (error) {
                      console.error('Error resetting first-run status:', error);
                      alert('Failed to reset first-run status. Check console for details.');
                    }
                  }
                }}
              >
                Reset First-Run Wizard
              </Button>
            </div>
          </div>
        </Card>
      )}

      {activeTab === 'output' && <OutputSettingsTab />}

      {activeTab === 'performance' && <PerformanceSettingsTab />}

      {activeTab === 'shortcuts' && <KeyboardShortcutsTab />}

      {activeTab === 'logging' && <LoggingSettingsTab />}

      {activeTab === 'theme' && <ThemeCustomizationTab />}

      {activeTab === 'ui' && (
        <Card className={styles.section}>
          <Title2>UI Customization</Title2>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
            Customize the appearance and scale of the user interface
          </Text>
          <div className={styles.form}>
            <Field label={`UI Scale: ${uiScale}%`} hint="Adjust the overall size of the interface">
              <Slider
                min={75}
                max={150}
                step={5}
                value={uiScale}
                onChange={(_, data) => setUiScale(data.value)}
              />
            </Field>
            <Field label="Compact Mode">
              <Switch checked={compactMode} onChange={(_, data) => setCompactMode(data.checked)} />
              <Text size={200}>
                {compactMode ? 'Enabled' : 'Disabled'} - Reduces spacing and padding for a more
                compact layout
              </Text>
            </Field>
            <Text size={200} style={{ fontStyle: 'italic', color: tokens.colorNeutralForeground3 }}>
              Note: Changes take effect after saving and refreshing the page
            </Text>
          </div>
        </Card>
      )}

      {activeTab === 'portable' && (
        <Card className={styles.section}>
          <Title2>Portable Installation</Title2>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
            Aura Video Studio is portable-only. All application data, tools, and dependencies are
            stored relative to the application root.
          </Text>

          <Card
            style={{
              marginBottom: tokens.spacingVerticalL,
              padding: tokens.spacingVerticalM,
              backgroundColor: tokens.colorNeutralBackground3,
            }}
          >
            <Text weight="semibold" size={300}>
              ℹ️ About Portable Mode
            </Text>
            <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
              All data is stored in the application folder, making it easy to:
            </Text>
            <ul
              style={{ marginTop: tokens.spacingVerticalS, marginLeft: tokens.spacingHorizontalL }}
            >
              <li>
                <Text size={200}>
                  Move the entire application to another machine by copying the folder
                </Text>
              </li>
              <li>
                <Text size={200}>Keep multiple installations without conflicts</Text>
              </li>
              <li>
                <Text size={200}>No registry entries or AppData dependencies</Text>
              </li>
              <li>
                <Text size={200}>Clean uninstall by simply deleting the folder</Text>
              </li>
            </ul>
            <Text
              size={200}
              style={{
                marginTop: tokens.spacingVerticalS,
                fontStyle: 'italic',
                color: tokens.colorNeutralForeground3,
              }}
            >
              Note: System dependencies (like GPU drivers, .NET runtime) must still be installed on
              each machine.
            </Text>
          </Card>

          <div className={styles.form}>
            <Card
              style={{
                padding: tokens.spacingVerticalM,
                backgroundColor: tokens.colorNeutralBackground2,
              }}
            >
              <Text weight="semibold" size={300}>
                Portable Directory Structure
              </Text>
              <div
                style={{
                  marginTop: tokens.spacingVerticalS,
                  display: 'flex',
                  flexDirection: 'column',
                  gap: tokens.spacingVerticalXS,
                  fontFamily: 'monospace',
                  fontSize: '0.9em',
                }}
              >
                <Text size={200}>
                  <strong>Root:</strong> {portableRootPath || 'Loading...'}
                </Text>
                <div style={{ marginLeft: tokens.spacingHorizontalM }}>
                  <Text size={200}>
                    ├─ <strong>Tools/</strong> - {toolsDirectory || 'Loading...'}
                  </Text>
                  <Text
                    size={200}
                    style={{
                      marginLeft: tokens.spacingHorizontalM,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Downloaded dependencies (FFmpeg, Ollama, etc.)
                  </Text>
                  <Text size={200}>
                    ├─ <strong>AuraData/</strong> - {auraDataDirectory || 'Loading...'}
                  </Text>
                  <Text
                    size={200}
                    style={{
                      marginLeft: tokens.spacingHorizontalM,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Settings, manifests, and configuration files
                  </Text>
                  <Text size={200}>
                    ├─ <strong>Logs/</strong> - {logsDirectory || 'Loading...'}
                  </Text>
                  <Text
                    size={200}
                    style={{
                      marginLeft: tokens.spacingHorizontalM,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Application and tool logs
                  </Text>
                  <Text size={200}>
                    ├─ <strong>Projects/</strong> - {projectsDirectory || 'Loading...'}
                  </Text>
                  <Text
                    size={200}
                    style={{
                      marginLeft: tokens.spacingHorizontalM,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Generated videos and project files
                  </Text>
                  <Text size={200}>
                    ├─ <strong>Downloads/</strong> - {downloadsDirectory || 'Loading...'}
                  </Text>
                  <Text
                    size={200}
                    style={{
                      marginLeft: tokens.spacingHorizontalM,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Temporary download storage
                  </Text>
                </div>
              </div>
            </Card>

            <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, flexWrap: 'wrap' }}>
              <Button appearance="primary" onClick={openToolsFolder}>
                Open Tools Folder
              </Button>
              <Button
                appearance="secondary"
                onClick={() => navigator.clipboard.writeText(portableRootPath)}
              >
                Copy Root Path
              </Button>
            </div>
          </div>
        </Card>
      )}

      {activeTab === 'providers' && (
        <>
          <Card className={styles.section}>
            <Title2>Provider Capabilities</Title2>
            <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
              View which providers are available on your system based on hardware, API keys, and OS.
            </Text>
            <ProvidersTable />
          </Card>

          <Card className={styles.section} style={{ marginTop: tokens.spacingVerticalL }}>
            <Title2>Provider Profiles</Title2>
            <Text>Select a provider profile to configure which services are used</Text>
            <div style={{ marginTop: tokens.spacingVerticalL }}>
              {profiles.map((profile) => (
                <div
                  key={profile.name}
                  className={styles.profileCard}
                  role="button"
                  tabIndex={0}
                  onClick={() => applyProfile(profile.name)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      applyProfile(profile.name);
                    }
                  }}
                >
                  <Text weight="semibold">{profile.name}</Text>
                  <br />
                  <Text size={200}>{profile.description}</Text>
                </div>
              ))}
            </div>
          </Card>

          <Card className={styles.section} style={{ marginTop: tokens.spacingVerticalL }}>
            <Title2>Provider Validation</Title2>
            <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
              Test connectivity and API keys for all configured providers
            </Text>
            <div className={styles.form}>
              <div
                style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}
              >
                <Button appearance="primary" onClick={validateProviders} disabled={validating}>
                  {validating ? 'Validating...' : 'Validate Providers'}
                </Button>
                {validationResults && (
                  <Button appearance="subtle" onClick={copyValidationResults}>
                    Copy to Clipboard
                  </Button>
                )}
              </div>

              {validationResults && (
                <div style={{ marginTop: tokens.spacingVerticalL }}>
                  <Text
                    weight="semibold"
                    style={{ marginBottom: tokens.spacingVerticalS, display: 'block' }}
                  >
                    Validation Results:
                  </Text>
                  <div
                    style={{
                      border: `1px solid ${tokens.colorNeutralStroke1}`,
                      borderRadius: tokens.borderRadiusMedium,
                      overflow: 'hidden',
                    }}
                  >
                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                      <thead style={{ backgroundColor: tokens.colorNeutralBackground2 }}>
                        <tr>
                          <th
                            style={{
                              padding: tokens.spacingVerticalS,
                              textAlign: 'left',
                              borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                            }}
                          >
                            Provider
                          </th>
                          <th
                            style={{
                              padding: tokens.spacingVerticalS,
                              textAlign: 'left',
                              borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                            }}
                          >
                            Status
                          </th>
                          <th
                            style={{
                              padding: tokens.spacingVerticalS,
                              textAlign: 'left',
                              borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                            }}
                          >
                            Details
                          </th>
                          <th
                            style={{
                              padding: tokens.spacingVerticalS,
                              textAlign: 'right',
                              borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                            }}
                          >
                            Time (ms)
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {validationResults.results.map((result: ValidationResult) => (
                          <tr key={result.name}>
                            <td
                              style={{
                                padding: tokens.spacingVerticalS,
                                borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                              }}
                            >
                              <Text weight="semibold">{result.name}</Text>
                            </td>
                            <td
                              style={{
                                padding: tokens.spacingVerticalS,
                                borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                              }}
                            >
                              <Text
                                style={{
                                  color: result.ok
                                    ? tokens.colorPaletteGreenForeground1
                                    : tokens.colorPaletteRedForeground1,
                                }}
                              >
                                {result.ok ? '✓ OK' : '✗ Failed'}
                              </Text>
                            </td>
                            <td
                              style={{
                                padding: tokens.spacingVerticalS,
                                borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                              }}
                            >
                              <Text size={200}>{result.details}</Text>
                            </td>
                            <td
                              style={{
                                padding: tokens.spacingVerticalS,
                                textAlign: 'right',
                                borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
                              }}
                            >
                              <Text size={200}>{result.elapsedMs}</Text>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <Text
                    size={200}
                    style={{
                      marginTop: tokens.spacingVerticalS,
                      display: 'block',
                      fontStyle: 'italic',
                    }}
                  >
                    Overall:{' '}
                    {validationResults.ok
                      ? '✓ All providers validated successfully'
                      : '✗ Some providers failed validation'}
                  </Text>
                </div>
              )}
            </div>
          </Card>
        </>
      )}

      {activeTab === 'localproviders' && (
        <Card className={styles.section}>
          <Title2>Local AI Providers Configuration</Title2>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
            Configure paths and URLs for locally-installed AI tools. These tools run on your machine
            and don&apos;t require API keys.
          </Text>

          <Card
            style={{
              marginBottom: tokens.spacingVerticalL,
              padding: tokens.spacingVerticalM,
              backgroundColor: tokens.colorNeutralBackground3,
            }}
          >
            <Text weight="semibold" size={300}>
              📖 Need Help?
            </Text>
            <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
              See the <strong>LOCAL_PROVIDERS_SETUP.md</strong> guide in the repository for detailed
              setup instructions for Stable Diffusion, Ollama, and FFmpeg. Visit the{' '}
              <strong>Downloads</strong> page to install components automatically.
            </Text>
          </Card>

          <div className={styles.form}>
            <div>
              <ValidatedInput
                label="Stable Diffusion WebUI URL"
                placeholder="http://127.0.0.1:7860"
                value={providerPathValues.stableDiffusionUrl || ''}
                onChange={(value) => {
                  setProviderPathValue('stableDiffusionUrl', value);
                  updateProviderPath('stableDiffusionUrl', value);
                }}
                error={providerPathErrors.stableDiffusionUrl?.error}
                isValid={providerPathErrors.stableDiffusionUrl?.isValid}
                isValidating={providerPathErrors.stableDiffusionUrl?.isValidating}
                hint="URL where Stable Diffusion WebUI is running (requires NVIDIA GPU with 6GB+ VRAM). Format: http://host:port"
              />
              <Button
                size="small"
                onClick={() =>
                  testProvider('stablediffusion', providerPathValues.stableDiffusionUrl || '')
                }
                style={{ marginTop: tokens.spacingVerticalXS }}
              >
                Test Connection
              </Button>
              {testResults.stablediffusion && (
                <Text
                  size={200}
                  style={{
                    marginTop: tokens.spacingVerticalXS,
                    color: testResults.stablediffusion.success
                      ? tokens.colorPaletteGreenForeground1
                      : tokens.colorPaletteRedForeground1,
                  }}
                >
                  {testResults.stablediffusion.success ? '✓' : '✗'}{' '}
                  {testResults.stablediffusion.message}
                </Text>
              )}
            </div>

            <div>
              <ValidatedInput
                label="Ollama URL"
                placeholder="http://127.0.0.1:11434"
                value={providerPathValues.ollamaUrl || ''}
                onChange={(value) => {
                  setProviderPathValue('ollamaUrl', value);
                  updateProviderPath('ollamaUrl', value);
                }}
                error={providerPathErrors.ollamaUrl?.error}
                isValid={providerPathErrors.ollamaUrl?.isValid}
                isValidating={providerPathErrors.ollamaUrl?.isValidating}
                hint="URL where Ollama is running for local LLM generation. Format: http://host:port"
              />
              <Button
                size="small"
                onClick={() => testProvider('ollama', providerPathValues.ollamaUrl || '')}
                style={{ marginTop: tokens.spacingVerticalXS }}
              >
                Test Connection
              </Button>
              {testResults.ollama && (
                <Text
                  size={200}
                  style={{
                    marginTop: tokens.spacingVerticalXS,
                    color: testResults.ollama.success
                      ? tokens.colorPaletteGreenForeground1
                      : tokens.colorPaletteRedForeground1,
                  }}
                >
                  {testResults.ollama.success ? '✓' : '✗'} {testResults.ollama.message}
                </Text>
              )}
            </div>

            <div>
              <Field
                label="Ollama Model"
                hint="Select which Ollama model to use for script generation. Make sure the model is pulled in Ollama first."
              >
                <div
                  style={{
                    display: 'flex',
                    gap: tokens.spacingHorizontalS,
                    alignItems: 'flex-start',
                  }}
                >
                  <select
                    value={selectedOllamaModel}
                    onChange={(e) => saveOllamaModel(e.target.value)}
                    disabled={loadingOllamaModels || ollamaModels.length === 0}
                    style={{
                      flex: 1,
                      padding: '8px',
                      borderRadius: tokens.borderRadiusMedium,
                      border: `1px solid ${tokens.colorNeutralStroke1}`,
                      backgroundColor: tokens.colorNeutralBackground1,
                      color: tokens.colorNeutralForeground1,
                      fontSize: tokens.fontSizeBase300,
                    }}
                  >
                    {ollamaModels.length === 0 && !loadingOllamaModels && (
                      <option value={selectedOllamaModel}>{selectedOllamaModel}</option>
                    )}
                    {ollamaModels.map((model) => (
                      <option key={model.name} value={model.name}>
                        {model.name} ({model.sizeGB.toFixed(2)} GB)
                      </option>
                    ))}
                  </select>
                  <Button size="small" onClick={fetchOllamaModels} disabled={loadingOllamaModels}>
                    {loadingOllamaModels ? 'Loading...' : 'Refresh Models'}
                  </Button>
                </div>
                {ollamaModels.length === 0 && !loadingOllamaModels && (
                  <Text
                    size={200}
                    style={{
                      marginTop: tokens.spacingVerticalXS,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Click &quot;Refresh Models&quot; to load available models from Ollama
                  </Text>
                )}
                {ollamaModels.length > 0 && (
                  <Text
                    size={200}
                    style={{
                      marginTop: tokens.spacingVerticalXS,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Found {ollamaModels.length} model{ollamaModels.length !== 1 ? 's' : ''} in
                    Ollama
                  </Text>
                )}
              </Field>
            </div>

            <Field
              label="FFmpeg Executable Path"
              hint="Path to ffmpeg.exe (leave empty to use system PATH or download from Downloads page)"
            >
              <div
                style={{
                  display: 'flex',
                  gap: tokens.spacingHorizontalS,
                  alignItems: 'flex-start',
                }}
              >
                <Input
                  style={{ flex: 1 }}
                  placeholder="C:\path\to\ffmpeg.exe or leave empty for system PATH"
                  value={providerPaths.ffmpegPath}
                  onChange={(e) => updateProviderPath('ffmpegPath', e.target.value)}
                />
                <Button
                  size="small"
                  onClick={() =>
                    testProvider('ffmpeg', undefined, providerPaths.ffmpegPath || 'ffmpeg')
                  }
                >
                  Test
                </Button>
              </div>
              {testResults.ffmpeg && (
                <Text
                  size={200}
                  style={{
                    marginTop: tokens.spacingVerticalXS,
                    color: testResults.ffmpeg.success
                      ? tokens.colorPaletteGreenForeground1
                      : tokens.colorPaletteRedForeground1,
                  }}
                >
                  {testResults.ffmpeg.success ? '✓' : '✗'} {testResults.ffmpeg.message}
                </Text>
              )}
            </Field>

            <Field
              label="FFprobe Executable Path"
              hint="Path to ffprobe.exe (usually in the same folder as FFmpeg)"
            >
              <Input
                placeholder="C:\path\to\ffprobe.exe or leave empty for system PATH"
                value={providerPaths.ffprobePath}
                onChange={(e) => updateProviderPath('ffprobePath', e.target.value)}
              />
            </Field>

            <Field
              label="Output Directory"
              hint="Default directory for rendered videos (leave empty for Documents\AuraVideoStudio)"
            >
              <Input
                placeholder="C:\Users\YourName\Videos\AuraOutput"
                value={providerPaths.outputDirectory}
                onChange={(e) => updateProviderPath('outputDirectory', e.target.value)}
              />
            </Field>

            {pathsModified && (
              <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
                ⚠️ You have unsaved changes
              </Text>
            )}
            <Button
              appearance="primary"
              onClick={saveProviderPaths}
              disabled={!pathsModified || savingPaths}
            >
              {savingPaths ? 'Saving...' : 'Save Provider Paths'}
            </Button>
          </div>
        </Card>
      )}

      {activeTab === 'localengines' && (
        <>
          <Card className={styles.section} style={{ marginBottom: tokens.spacingVerticalL }}>
            <Title2>Local Engines</Title2>
            <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
              Manage local AI engines (Stable Diffusion, ComfyUI, Piper, Mimic3) with automatic
              installation and configuration.
            </Text>
            <LocalEngines />
          </Card>

          <RescanPanel />
        </>
      )}

      {activeTab === 'apikeys' && (
        <Card className={styles.section}>
          <Title2>API Keys</Title2>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
            Configure API keys for external services. Keys are stored securely.
          </Text>
          <div className={styles.form}>
            <ValidatedInput
              label="OpenAI API Key"
              type="password"
              placeholder="sk-..."
              value={apiKeyValues.openai || ''}
              onChange={(value) => {
                setApiKeyValue('openai', value);
                updateApiKey('openai', value);
              }}
              error={apiKeyErrors.openai?.error}
              isValid={apiKeyErrors.openai?.isValid}
              isValidating={apiKeyErrors.openai?.isValidating}
              hint="Required for GPT-based script generation. Must start with 'sk-' and be at least 20 characters."
            />
            <ValidatedInput
              label="ElevenLabs API Key"
              type="password"
              placeholder="Enter your ElevenLabs API key"
              value={apiKeyValues.elevenlabs || ''}
              onChange={(value) => {
                setApiKeyValue('elevenlabs', value);
                updateApiKey('elevenlabs', value);
              }}
              error={apiKeyErrors.elevenlabs?.error}
              isValid={apiKeyErrors.elevenlabs?.isValid}
              isValidating={apiKeyErrors.elevenlabs?.isValidating}
              hint="Required for high-quality voice synthesis. Must be at least 32 characters."
            />
            <ValidatedInput
              label="Pexels API Key"
              type="password"
              placeholder="Enter your Pexels API key"
              value={apiKeyValues.pexels || ''}
              onChange={(value) => {
                setApiKeyValue('pexels', value);
                updateApiKey('pexels', value);
              }}
              error={apiKeyErrors.pexels?.error}
              isValid={apiKeyErrors.pexels?.isValid}
              isValidating={apiKeyErrors.pexels?.isValidating}
              hint="Required for stock video and images from Pexels."
            />
            <ValidatedInput
              label="Pixabay API Key"
              type="password"
              placeholder="Enter your Pixabay API key"
              value={apiKeyValues.pixabay || ''}
              onChange={(value) => {
                setApiKeyValue('pixabay', value);
                updateApiKey('pixabay', value);
              }}
              error={apiKeyErrors.pixabay?.error}
              isValid={apiKeyErrors.pixabay?.isValid}
              isValidating={apiKeyErrors.pixabay?.isValidating}
              hint="Required for stock video and images from Pixabay."
            />
            <ValidatedInput
              label="Unsplash API Key"
              type="password"
              placeholder="Enter your Unsplash API key"
              value={apiKeyValues.unsplash || ''}
              onChange={(value) => {
                setApiKeyValue('unsplash', value);
                updateApiKey('unsplash', value);
              }}
              error={apiKeyErrors.unsplash?.error}
              isValid={apiKeyErrors.unsplash?.isValid}
              isValidating={apiKeyErrors.unsplash?.isValidating}
              hint="Required for stock images from Unsplash."
            />
            <ValidatedInput
              label="Stability AI API Key"
              type="password"
              placeholder="sk-..."
              value={apiKeyValues.stabilityai || ''}
              onChange={(value) => {
                setApiKeyValue('stabilityai', value);
                updateApiKey('stabilityai', value);
              }}
              error={apiKeyErrors.stabilityai?.error}
              isValid={apiKeyErrors.stabilityai?.isValid}
              isValidating={apiKeyErrors.stabilityai?.isValidating}
              hint="Optional - for AI image generation. Must start with 'sk-'."
            />
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

      {activeTab === 'aioptimization' && <AIOptimizationPanel />}

      {activeTab === 'templates' && (
        <>
          <Card className={styles.section}>
            <Title2>Settings Export/Import</Title2>
            <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
              Export all your settings to a JSON file, or import settings from a previously saved
              file
            </Text>
            <div className={styles.form}>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, flexWrap: 'wrap' }}>
                <Button appearance="primary" onClick={exportSettings}>
                  Export Settings to JSON
                </Button>
                <Button
                  appearance="secondary"
                  onClick={() => {
                    const input = document.createElement('input');
                    input.type = 'file';
                    input.accept = '.json';
                    input.onchange = (e) => {
                      const file = (e.target as HTMLInputElement).files?.[0];
                      if (file) {
                        importSettings(file);
                      }
                    };
                    input.click();
                  }}
                >
                  Import Settings from JSON
                </Button>
              </div>
              <Text
                size={200}
                style={{ fontStyle: 'italic', color: tokens.colorNeutralForeground3 }}
              >
                Exported settings include API keys, provider paths, UI preferences, and more. Keep
                your exported files secure as they may contain sensitive information.
              </Text>
            </div>
          </Card>

          <Card className={styles.section} style={{ marginTop: tokens.spacingVerticalL }}>
            <Title2>Profile Templates</Title2>
            <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
              Quick-start templates with pre-configured provider settings for common use cases
            </Text>
            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
              <div
                className={styles.profileCard}
                role="button"
                tabIndex={0}
                onClick={() => applyProfileTemplate('free-only')}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    applyProfileTemplate('free-only');
                  }
                }}
              >
                <Text weight="semibold">Free-Only</Text>
                <br />
                <Text size={200}>
                  No API keys required. Uses template-based script generation, Windows TTS, and free
                  stock sources (Pexels, Pixabay, Unsplash).
                </Text>
              </div>
              <div
                className={styles.profileCard}
                role="button"
                tabIndex={0}
                onClick={() => applyProfileTemplate('balanced-mix')}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    applyProfileTemplate('balanced-mix');
                  }
                }}
              >
                <Text weight="semibold">Balanced Mix</Text>
                <br />
                <Text size={200}>
                  Combines free and paid services. Uses GPT-4 for scripts (requires OpenAI key),
                  ElevenLabs for voice (requires key), and free stock sources.
                </Text>
              </div>
              <div
                className={styles.profileCard}
                role="button"
                tabIndex={0}
                onClick={() => applyProfileTemplate('pro-max')}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    applyProfileTemplate('pro-max');
                  }
                }}
              >
                <Text weight="semibold">Pro-Max</Text>
                <br />
                <Text size={200}>
                  Maximum quality with all premium providers. Uses GPT-4, ElevenLabs, Stability AI
                  for images, and premium stock sources. Requires all API keys.
                </Text>
              </div>
            </div>
            <Text
              size={200}
              style={{
                marginTop: tokens.spacingVerticalM,
                fontStyle: 'italic',
                color: tokens.colorNeutralForeground3,
              }}
            >
              Applying a template will update your provider selections but won&apos;t overwrite your
              API keys.
            </Text>
          </Card>

          <Card className={styles.section} style={{ marginTop: tokens.spacingVerticalL }}>
            <Title2>Custom Profile Management</Title2>
            <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
              Save your current configuration as a custom profile or load a previously saved profile
            </Text>
            <div className={styles.form}>
              <Field label="Profile Name">
                <Input
                  placeholder="Enter profile name (e.g., My YouTube Setup)"
                  value={customProfileName}
                  onChange={(e) => setCustomProfileName(e.target.value)}
                />
              </Field>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM }}>
                <Button
                  appearance="primary"
                  onClick={saveCustomProfile}
                  disabled={!customProfileName.trim()}
                >
                  Save Current Settings as Profile
                </Button>
                <Button appearance="secondary" onClick={loadCustomProfiles}>
                  Load Saved Profiles
                </Button>
              </div>
              {savedProfiles.length > 0 && (
                <div style={{ marginTop: tokens.spacingVerticalM }}>
                  <Text
                    weight="semibold"
                    style={{ marginBottom: tokens.spacingVerticalS, display: 'block' }}
                  >
                    Saved Profiles:
                  </Text>
                  <div
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      gap: tokens.spacingVerticalS,
                    }}
                  >
                    {savedProfiles.map((profile: CustomProfile, index: number) => (
                      <div
                        key={index}
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          padding: tokens.spacingVerticalS,
                          backgroundColor: tokens.colorNeutralBackground2,
                          borderRadius: tokens.borderRadiusMedium,
                        }}
                      >
                        <Text>{profile.name}</Text>
                        <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                          <Button
                            size="small"
                            appearance="subtle"
                            onClick={() => loadCustomProfile(profile)}
                          >
                            Load
                          </Button>
                          <Button
                            size="small"
                            appearance="subtle"
                            onClick={() => deleteCustomProfile(profile.name)}
                          >
                            Delete
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </Card>
        </>
      )}

      {activeTab === 'privacy' && (
        <Card className={styles.section}>
          <Title2>Privacy Settings</Title2>
          <div className={styles.form}>
            <Field label="Telemetry">
              <Switch
                checked={userSettings.advanced.enableTelemetry}
                onChange={(_, data) =>
                  setUserSettings({
                    ...userSettings,
                    advanced: { ...userSettings.advanced, enableTelemetry: data.checked },
                  })
                }
              />
              <Text size={200}>
                Send anonymous usage data to improve the app (disabled by default)
              </Text>
            </Field>
            <Field label="Crash Reports">
              <Switch
                checked={userSettings.advanced.enableCrashReports}
                onChange={(_, data) =>
                  setUserSettings({
                    ...userSettings,
                    advanced: { ...userSettings.advanced, enableCrashReports: data.checked },
                  })
                }
              />
              <Text size={200}>
                Send crash reports to help diagnose issues (disabled by default)
              </Text>
            </Field>
          </div>
        </Card>
      )}

      {activeTab === 'importexport' && (
        <Card className={styles.section}>
          <Title2>Import/Export Settings</Title2>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
            Backup and restore all your settings as a JSON file
          </Text>
          <div className={styles.form}>
            <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, flexWrap: 'wrap' }}>
              <Button
                appearance="primary"
                icon={<ArrowDownload24Regular />}
                onClick={exportUserSettings}
              >
                Export All Settings
              </Button>
              <Button
                appearance="secondary"
                icon={<ArrowUpload24Regular />}
                onClick={importUserSettings}
              >
                Import Settings
              </Button>
              <Button appearance="subtle" onClick={resetUserSettings}>
                Reset to Defaults
              </Button>
            </div>
            <Card
              style={{
                marginTop: tokens.spacingVerticalL,
                padding: tokens.spacingVerticalM,
                backgroundColor: tokens.colorNeutralBackground3,
              }}
            >
              <Text weight="semibold" size={300}>
                ⚠️ Important
              </Text>
              <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                Exported settings include API keys and sensitive information. Keep your exported
                files secure and never share them publicly.
              </Text>
            </Card>
            <Card
              style={{
                marginTop: tokens.spacingVerticalM,
                padding: tokens.spacingVerticalM,
                backgroundColor: tokens.colorNeutralBackground2,
              }}
            >
              <Text weight="semibold" size={300}>
                📋 What&apos;s Included
              </Text>
              <ul style={{ marginTop: tokens.spacingVerticalS, paddingLeft: '20px' }}>
                <li>
                  <Text size={200}>General settings (theme, language, autosave, etc.)</Text>
                </li>
                <li>
                  <Text size={200}>API keys for all services</Text>
                </li>
                <li>
                  <Text size={200}>File locations and paths</Text>
                </li>
                <li>
                  <Text size={200}>Video defaults (resolution, codec, etc.)</Text>
                </li>
                <li>
                  <Text size={200}>Editor preferences</Text>
                </li>
                <li>
                  <Text size={200}>UI customization</Text>
                </li>
                <li>
                  <Text size={200}>Advanced settings</Text>
                </li>
              </ul>
            </Card>
          </div>
        </Card>
      )}

      <div className={styles.actions}>
        <Button
          appearance="primary"
          icon={<Save24Regular />}
          onClick={saveUserSettings}
          disabled={!hasUnsavedChanges}
        >
          Save All Settings
        </Button>
        {hasUnsavedChanges && (
          <Button
            appearance="subtle"
            onClick={() => {
              if (confirm('Discard all unsaved changes?')) {
                setUserSettings({ ...originalSettings });
              }
            }}
          >
            Discard Changes
          </Button>
        )}
      </div>
    </div>
  );
}
