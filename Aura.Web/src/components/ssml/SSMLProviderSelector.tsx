/**
 * SSML Provider Selector Component
 * Dropdown for selecting TTS provider with support badges
 */

import { Dropdown, Option, Badge, Text } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import type { FC } from 'react';
import { getSSMLConstraints } from '@/services/ssmlService';
import { useSSMLEditorStore } from '@/state/ssmlEditor';

interface Provider {
  id: string;
  name: string;
  tier: 'premium' | 'free' | 'offline';
  supportsSSML: boolean;
}

const PROVIDERS: Provider[] = [
  { id: 'ElevenLabs', name: 'ElevenLabs', tier: 'premium', supportsSSML: true },
  { id: 'PlayHT', name: 'PlayHT', tier: 'premium', supportsSSML: true },
  { id: 'WindowsSAPI', name: 'Windows SAPI', tier: 'free', supportsSSML: true },
  { id: 'Piper', name: 'Piper', tier: 'offline', supportsSSML: true },
  { id: 'Mimic3', name: 'Mimic3', tier: 'offline', supportsSSML: true },
];

export const SSMLProviderSelector: FC = () => {
  const { selectedProvider, setProvider, setProviderConstraints } = useSSMLEditorStore();
  const [loading, setLoading] = useState(false);

  const handleProviderChange = async (providerId: string): Promise<void> => {
    setProvider(providerId);
    setLoading(true);

    try {
      const constraints = await getSSMLConstraints(providerId);
      setProviderConstraints(constraints);
    } catch (error: unknown) {
      console.error('Failed to load provider constraints:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (selectedProvider) {
      void handleProviderChange(selectedProvider);
    }
  }, []);

  const getTierColor = (tier: string): 'success' | 'warning' | 'informative' => {
    switch (tier) {
      case 'premium':
        return 'success';
      case 'free':
        return 'informative';
      case 'offline':
        return 'warning';
      default:
        return 'informative';
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
      <Text weight="semibold">TTS Provider</Text>
      <Dropdown
        placeholder="Select a provider"
        value={selectedProvider || ''}
        onOptionSelect={(_, data) => {
          if (data.optionValue) {
            void handleProviderChange(data.optionValue);
          }
        }}
        disabled={loading}
      >
        {PROVIDERS.map((provider) => (
          <Option key={provider.id} value={provider.id} text={provider.name}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Text>{provider.name}</Text>
              <Badge appearance="outline" color={getTierColor(provider.tier)}>
                {provider.tier}
              </Badge>
              {provider.supportsSSML && (
                <Badge appearance="filled" color="success" size="small">
                  SSML
                </Badge>
              )}
            </div>
          </Option>
        ))}
      </Dropdown>
      {loading && (
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Loading provider constraints...
        </Text>
      )}
    </div>
  );
};
