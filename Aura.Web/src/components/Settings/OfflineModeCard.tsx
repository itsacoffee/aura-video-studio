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

export const OfflineModeCard: React.FC<OfflineModeCardProps> = ({ offlineMode, onModeChange }) => {
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

      {/* Provider availability in current mode */}
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
            {offlineMode ? 'Ollama, RuleBased' : 'All configured'}
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
            {offlineMode ? 'Windows SAPI, Piper, Mimic3' : 'All configured'}
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
            {offlineMode ? 'Placeholder colors only' : 'All configured'}
          </Text>
        </div>
      </div>
    </Card>
  );
};

export default OfflineModeCard;
