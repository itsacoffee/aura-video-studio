/**
 * OfflineModeCard component
 * Provides a prominent toggle for switching between online and offline operating modes
 */

import { Card, Switch, Text, tokens } from '@fluentui/react-components';
import { Cloud24Regular, CloudOff24Regular } from '@fluentui/react-icons';
import React from 'react';
import { useSettingsStore } from '../../stores/settingsStore';

interface OfflineModeCardProps {
  offlineMode: boolean;
  onModeChange: (isOffline: boolean) => void;
}

/**
 * Format provider list for user-friendly display
 * Consolidates Windows/WindowsSAPI into single entry
 */
function formatProviderList(providers: string[]): string {
  // Consolidate Windows/WindowsSAPI into single "Windows SAPI"
  const displayProviders = providers
    .filter((p) => p !== 'WindowsSAPI') // Remove duplicate
    .map((p) => (p === 'Windows' ? 'Windows SAPI' : p));
  return displayProviders.join(', ');
}

export const OfflineModeCard: React.FC<OfflineModeCardProps> = ({ offlineMode, onModeChange }) => {
  // Use provider lists from the settings store (synced from backend)
  const { allowedLlmProviders, allowedTtsProviders, allowedImageProviders } = useSettingsStore();

  const handleToggle = (checked: boolean) => {
    onModeChange(checked);
    // Also update the settings store for global access
    useSettingsStore.getState().setOperatingMode(checked ? 'offline' : 'online');
  };

  return (
    <Card
      style={{
        padding: tokens.spacingVerticalL,
        backgroundColor: offlineMode
          ? tokens.colorPaletteYellowBackground2
          : tokens.colorNeutralBackground3,
        border: `2px solid ${offlineMode ? tokens.colorPaletteYellowBorder2 : tokens.colorNeutralStroke1}`,
        borderRadius: tokens.borderRadiusMedium,
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: tokens.spacingHorizontalL,
          marginBottom: tokens.spacingVerticalM,
        }}
      >
        <div
          style={{
            fontSize: '32px',
            color: offlineMode
              ? tokens.colorPaletteYellowForeground2
              : tokens.colorBrandForeground1,
          }}
        >
          {offlineMode ? <CloudOff24Regular /> : <Cloud24Regular />}
        </div>
        <div style={{ flex: 1 }}>
          <Text weight="semibold" size={500}>
            {offlineMode ? 'Offline Mode' : 'Online Mode'}
          </Text>
          <Text
            size={200}
            style={{
              display: 'block',
              color: tokens.colorNeutralForeground3,
              marginTop: tokens.spacingVerticalXS,
            }}
          >
            {offlineMode
              ? 'Only local providers are available. No internet connection required.'
              : 'All configured providers are available. Prefers cloud for quality.'}
          </Text>
        </div>
        <Switch
          checked={offlineMode}
          onChange={(_, data) => handleToggle(data.checked)}
          label={offlineMode ? 'Offline' : 'Online'}
        />
      </div>

      {/* Provider availability in current mode - uses store values synced from backend */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: tokens.spacingVerticalS,
          marginTop: tokens.spacingVerticalM,
          paddingTop: tokens.spacingVerticalM,
          borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
        }}
      >
        <div>
          <Text weight="semibold" size={200}>
            LLM Providers
          </Text>
          <Text
            size={200}
            style={{
              display: 'block',
              color: tokens.colorNeutralForeground3,
            }}
          >
            {offlineMode ? formatProviderList(allowedLlmProviders) : 'All configured'}
          </Text>
        </div>
        <div>
          <Text weight="semibold" size={200}>
            TTS Providers
          </Text>
          <Text
            size={200}
            style={{
              display: 'block',
              color: tokens.colorNeutralForeground3,
            }}
          >
            {offlineMode ? formatProviderList(allowedTtsProviders) : 'All configured'}
          </Text>
        </div>
        <div>
          <Text weight="semibold" size={200}>
            Image Providers
          </Text>
          <Text
            size={200}
            style={{
              display: 'block',
              color: tokens.colorNeutralForeground3,
            }}
          >
            {offlineMode ? formatProviderList(allowedImageProviders) : 'All configured'}
          </Text>
        </div>
      </div>
    </Card>
  );
};

export default OfflineModeCard;
