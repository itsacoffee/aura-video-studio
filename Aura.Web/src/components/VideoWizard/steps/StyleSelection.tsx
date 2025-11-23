import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Dropdown,
  Option,
  Field,
  Slider,
  Card,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import { CheckmarkCircle24Regular, ErrorCircle24Regular } from '@fluentui/react-icons';
import { useEffect, useState, useCallback, useRef } from 'react';
import type { FC } from 'react';
import type { StyleData, BriefData, StepValidation } from '../types';
import { getVisualsClient } from '@/api/visualsClient';
import type { VisualProvider } from '@/api/visualsClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
    animation: 'fadeInUp 0.5s ease',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  providerCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
      border: `1px solid ${tokens.colorBrandStroke1}`,
    },
  },
  selectedCard: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    boxShadow: tokens.shadow8,
    backgroundColor: tokens.colorBrandBackground2,
  },
  providerHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  providerDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  formRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
  },
  formField: {
    flex: 1,
    minWidth: '200px',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  stylePresetGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  stylePresetCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    border: `2px solid ${tokens.colorNeutralStroke2}`,
    textAlign: 'center',
    position: 'relative',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
      border: `2px solid ${tokens.colorBrandStroke1}`,
    },
  },
  stylePresetSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  stylePresetIcon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalS,
  },
  stylePresetBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalS,
    right: tokens.spacingVerticalS,
  },
  '@keyframes fadeInUp': {
    '0%': {
      opacity: 0,
      transform: 'translateY(20px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
});

interface StyleSelectionProps {
  data: StyleData;
  briefData: BriefData;
  advancedMode: boolean;
  onChange: (data: StyleData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

interface StylePreset {
  name: string;
  visualStyle: 'modern' | 'minimal' | 'cinematic' | 'playful' | 'professional';
  musicGenre: 'ambient' | 'upbeat' | 'dramatic' | 'none';
  description: string;
  icon: string;
}

const STYLE_PRESETS: StylePreset[] = [
  {
    name: 'Modern',
    visualStyle: 'modern',
    musicGenre: 'upbeat',
    description: 'Clean, contemporary look with energetic music',
    icon: '🎨',
  },
  {
    name: 'Professional',
    visualStyle: 'professional',
    musicGenre: 'ambient',
    description: 'Corporate style with subtle background music',
    icon: '💼',
  },
  {
    name: 'Cinematic',
    visualStyle: 'cinematic',
    musicGenre: 'dramatic',
    description: 'Movie-like visuals with dramatic scoring',
    icon: '🎬',
  },
  {
    name: 'Minimal',
    visualStyle: 'minimal',
    musicGenre: 'ambient',
    description: 'Simple, focused design with calm ambiance',
    icon: '✨',
  },
  {
    name: 'Playful',
    visualStyle: 'playful',
    musicGenre: 'upbeat',
    description: 'Fun, colorful style with lively music',
    icon: '🎉',
  },
];

export const StyleSelection: FC<StyleSelectionProps> = ({
  data,
  advancedMode,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const visualsClient = getVisualsClient();
  const [providers, setProviders] = useState<VisualProvider[]>([]);
  const [availableStyles, setAvailableStyles] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  const loadProviders = useCallback(async () => {
    try {
      const response = await visualsClient.getProviders();
      setProviders(response.providers);

      if (!data.imageProvider) {
        const availableProvider = response.providers.find((p) => p.isAvailable);
        if (availableProvider) {
          onChange({
            ...data,
            imageProvider: availableProvider.name,
          });
        }
      }
    } catch (error) {
      console.error('Failed to load providers:', error);
      // CRITICAL FIX: Add fallback providers if API fails
      // This ensures users can still select providers even if the API is blocked or unavailable
      // Placeholder is always available as the ultimate fallback
      const fallbackProviders: VisualProvider[] = [
        {
          name: 'Placeholder',
          isAvailable: true,
          requiresApiKey: false,
          capabilities: {
            providerName: 'Placeholder',
            supportsNegativePrompts: false,
            supportsBatchGeneration: true,
            supportsStylePresets: false,
            supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
            supportedStyles: ['solid-color', 'gradient', 'text-overlay'],
            maxWidth: 1920,
            maxHeight: 1080,
            isLocal: true,
            isFree: true,
            costPerImage: 0,
            tier: 'Free - Always Available',
          },
        },
        {
          name: 'Stock',
          isAvailable: true,
          requiresApiKey: false,
          capabilities: {
            providerName: 'Stock',
            supportsNegativePrompts: false,
            supportsBatchGeneration: false,
            supportsStylePresets: false,
            supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
            supportedStyles: ['photorealistic', 'artistic', 'cinematic'],
            maxWidth: 1920,
            maxHeight: 1080,
            isLocal: false,
            isFree: true,
            costPerImage: 0,
            tier: 'Free',
          },
        },
        {
          name: 'LocalSD',
          isAvailable: false,
          requiresApiKey: false,
          capabilities: {
            providerName: 'LocalSD',
            supportsNegativePrompts: true,
            supportsBatchGeneration: true,
            supportsStylePresets: true,
            supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
            supportedStyles: ['photorealistic', 'artistic', 'cinematic', 'minimalist'],
            maxWidth: 2048,
            maxHeight: 2048,
            isLocal: true,
            isFree: true,
            costPerImage: 0,
            tier: 'Free',
          },
        },
      ];
      setProviders(fallbackProviders);
      
      // Auto-select Placeholder if no provider is selected (guaranteed fallback)
      if (!data.imageProvider) {
        onChange({
          ...data,
          imageProvider: 'Placeholder',
        });
      }
    } finally {
      setLoading(false);
    }
  }, [visualsClient, data, onChange]);

  const loadStyles = useCallback(async () => {
    try {
      const response = await visualsClient.getStyles();
      setAvailableStyles(response.allStyles);

      if (!data.imageStyle && response.allStyles.length > 0) {
        onChange({
          ...data,
          imageStyle: response.allStyles[0],
        });
      }
    } catch (error) {
      console.error('Failed to load styles:', error);
      setAvailableStyles(['photorealistic', 'artistic', 'cinematic', 'minimalist']);
    }
  }, [visualsClient, data, onChange]);

  useEffect(() => {
    loadProviders();
    loadStyles();
  }, [loadProviders, loadStyles]);

  // CRITICAL FIX: Ensure default values are set on mount if not present
  // Use ref to track if defaults have been set to avoid stale closures
  const defaultsSetRef = useRef(false);
  useEffect(() => {
    // Only set defaults once on mount
    if (defaultsSetRef.current) {
      return;
    }

    const needsDefaults = !data.voiceProvider || !data.visualStyle || !data.imageProvider;
    if (needsDefaults) {
      defaultsSetRef.current = true;
      onChange({
        ...data,
        voiceProvider: data.voiceProvider || 'Windows',
        visualStyle: data.visualStyle || 'modern',
        imageProvider: data.imageProvider || 'Placeholder',
      });
    } else {
      defaultsSetRef.current = true;
    }
  }, [data, onChange]);

  useEffect(() => {
    // CRITICAL FIX: Allow validation to pass if imageProvider is set, even if providers list is empty
    // This prevents blocking when API fails but user has already selected a provider
    const hasVoiceProvider = !!data.voiceProvider;
    const hasVisualStyle = !!data.visualStyle;
    const hasImageProvider = !!data.imageProvider;

    // If providers haven't loaded yet but we have a provider selected, still allow validation
    const isValid = hasVoiceProvider && hasVisualStyle && (hasImageProvider || providers.length === 0);

    const errors: string[] = [];
    if (!hasVoiceProvider) errors.push('Voice provider');
    if (!hasVisualStyle) errors.push('Visual style');
    if (!hasImageProvider && providers.length > 0) errors.push('Image provider');

    onValidationChange({
      isValid,
      errors: isValid ? [] : [`Please select: ${errors.join(', ')}`],
    });
  }, [data, onValidationChange, providers.length]);

  const handleProviderSelect = useCallback(
    (providerName: string) => {
      onChange({
        ...data,
        imageProvider: providerName,
      });
    },
    [data, onChange]
  );

  const handleStyleChange = useCallback(
    (field: keyof StyleData, value: string | number | boolean) => {
      onChange({
        ...data,
        [field]: value,
      });
    },
    [data, onChange]
  );

  const handlePresetSelect = useCallback(
    (preset: StylePreset) => {
      onChange({
        ...data,
        visualStyle: preset.visualStyle,
        musicGenre: preset.musicGenre,
        musicEnabled: preset.musicGenre !== 'none',
        // CRITICAL FIX: Ensure voiceProvider is set when using presets
        // If not already set, use a default
        voiceProvider: data.voiceProvider || 'Windows',
        // CRITICAL FIX: Ensure imageProvider is set when using presets
        // If not already set and providers are available, auto-select first available
        imageProvider: data.imageProvider || (providers.length > 0
          ? providers.find(p => p.isAvailable)?.name || 'Placeholder'
          : 'Placeholder'),
      });
    },
    [data, onChange, providers]
  );

  if (loading) {
    return (
      <div className={styles.container}>
        <Title2>Style Selection</Title2>
        <div className={styles.loadingContainer}>
          <Spinner size="large" label="Loading providers..." />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.section}>
        <Title2>Style Selection</Title2>
        <Text>Configure voice, visual style, and music preferences for your video.</Text>
      </div>

      <div className={styles.section}>
        <Title3>Quick Style Presets</Title3>
        <Text
          size={300}
          style={{ marginBottom: tokens.spacingVerticalM, color: tokens.colorNeutralForeground3 }}
        >
          Choose a preset style to quickly configure your video&apos;s look and feel
        </Text>
        <div className={styles.stylePresetGrid}>
          {STYLE_PRESETS.map((preset) => (
            <Card
              key={preset.name}
              className={`${styles.stylePresetCard} ${
                data.visualStyle === preset.visualStyle && data.musicGenre === preset.musicGenre
                  ? styles.stylePresetSelected
                  : ''
              }`}
              onClick={() => handlePresetSelect(preset)}
            >
              {data.visualStyle === preset.visualStyle && data.musicGenre === preset.musicGenre && (
                <Badge appearance="filled" color="success" className={styles.stylePresetBadge}>
                  Active
                </Badge>
              )}
              <div className={styles.stylePresetIcon}>{preset.icon}</div>
              <Text weight="semibold" size={300}>
                {preset.name}
              </Text>
              <Text
                size={200}
                style={{
                  marginTop: tokens.spacingVerticalXS,
                  color: tokens.colorNeutralForeground3,
                }}
              >
                {preset.description}
              </Text>
            </Card>
          ))}
        </div>
      </div>

      <div className={styles.section}>
        <Title3>Voice Settings</Title3>
        <div className={styles.formRow}>
          <Field label="Voice Provider" className={styles.formField}>
            <Dropdown
              value={data.voiceProvider}
              selectedOptions={[data.voiceProvider]}
              onOptionSelect={(_, option) => {
                if (option.optionValue) {
                  handleStyleChange('voiceProvider', option.optionValue as string);
                }
              }}
            >
              <Option value="ElevenLabs">ElevenLabs</Option>
              <Option value="PlayHT">PlayHT</Option>
              <Option value="Windows">Windows</Option>
              <Option value="Piper">Piper</Option>
            </Dropdown>
          </Field>

          <Field label="Voice Name" className={styles.formField}>
            <Dropdown
              placeholder="Select a voice"
              value={data.voiceName}
              selectedOptions={data.voiceName ? [data.voiceName] : []}
              onOptionSelect={(_, option) => {
                if (option.optionValue) {
                  handleStyleChange('voiceName', option.optionValue as string);
                }
              }}
            >
              <Option value="default">Default</Option>
              <Option value="professional">Professional</Option>
              <Option value="friendly">Friendly</Option>
              <Option value="energetic">Energetic</Option>
            </Dropdown>
          </Field>
        </div>
      </div>

      <div className={styles.section}>
        <Title3>Image Generation Provider</Title3>
        <Text>Select the AI provider for generating scene images</Text>

        {providers.length === 0 && !loading && (
          <div
            style={{
              padding: tokens.spacingVerticalM,
              backgroundColor: tokens.colorNeutralBackground3,
              borderRadius: tokens.borderRadiusMedium,
              marginTop: tokens.spacingVerticalM,
            }}
          >
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              No providers available. Using default Placeholder provider (solid colors).
            </Text>
          </div>
        )}

        <div className={styles.grid}>
          {providers.length > 0 ? (
            providers.map((provider) => (
              <Card
                key={provider.name}
                className={`${styles.providerCard} ${
                  data.imageProvider === provider.name ? styles.selectedCard : ''
                }`}
                onClick={() => provider.isAvailable && handleProviderSelect(provider.name)}
                style={{
                  opacity: provider.isAvailable ? 1 : 0.5,
                  cursor: provider.isAvailable ? 'pointer' : 'not-allowed',
                }}
              >
                <div className={styles.providerHeader}>
                  <Title3>{provider.name}</Title3>
                  {data.imageProvider === provider.name && (
                    <Badge appearance="filled" color="success">
                      Selected
                    </Badge>
                  )}
                </div>

                <div className={styles.providerDetails}>
                  <div
                    style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                  >
                    {provider.isAvailable ? (
                      <CheckmarkCircle24Regular
                        style={{ color: tokens.colorPaletteGreenForeground1, fontSize: '16px' }}
                      />
                    ) : (
                      <ErrorCircle24Regular
                        style={{ color: tokens.colorPaletteRedForeground1, fontSize: '16px' }}
                      />
                    )}
                    <Text size={200}>{provider.isAvailable ? 'Available' : 'Not Available'}</Text>
                  </div>

                  {provider.capabilities && (
                    <>
                      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                        {provider.capabilities.tier} •{' '}
                        {provider.capabilities.isFree
                          ? 'Free'
                          : `$${provider.capabilities.costPerImage}/image`}
                      </Text>
                      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                        Max: {provider.capabilities.maxWidth}x{provider.capabilities.maxHeight}
                      </Text>
                      {provider.capabilities.supportedStyles.length > 0 && (
                        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                          {provider.capabilities.supportedStyles.length} styles supported
                        </Text>
                      )}
                    </>
                  )}
                </div>
              </Card>
            ))
          ) : (
            !loading && (
              <Card
                className={`${styles.providerCard} ${
                  data.imageProvider === 'Placeholder' ? styles.selectedCard : ''
                }`}
                onClick={() => handleProviderSelect('Placeholder')}
                style={{ cursor: 'pointer' }}
              >
                <div className={styles.providerHeader}>
                  <Title3>Placeholder</Title3>
                  {data.imageProvider === 'Placeholder' && (
                    <Badge appearance="filled" color="success">
                      Selected
                    </Badge>
                  )}
                </div>
                <div className={styles.providerDetails}>
                  <div
                    style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                  >
                    <CheckmarkCircle24Regular
                      style={{ color: tokens.colorPaletteGreenForeground1, fontSize: '16px' }}
                    />
                    <Text size={200}>Always Available</Text>
                  </div>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    Free • Solid color backgrounds with text - guaranteed fallback
                  </Text>
                </div>
              </Card>
            )
          )}
        </div>
      </div>

      <div className={styles.section}>
        <Title3>Visual Style Settings</Title3>
        <div className={styles.formRow}>
          <Field label="Visual Style" className={styles.formField}>
            <Dropdown
              value={data.visualStyle}
              selectedOptions={[data.visualStyle]}
              onOptionSelect={(_, option) => {
                if (option.optionValue) {
                  handleStyleChange('visualStyle', option.optionValue as string);
                }
              }}
            >
              <Option value="modern">Modern</Option>
              <Option value="minimal">Minimal</Option>
              <Option value="cinematic">Cinematic</Option>
              <Option value="playful">Playful</Option>
              <Option value="professional">Professional</Option>
            </Dropdown>
          </Field>

          {availableStyles.length > 0 && (
            <Field label="Image Style" className={styles.formField}>
              <Dropdown
                placeholder="Select image style"
                value={data.imageStyle}
                selectedOptions={data.imageStyle ? [data.imageStyle] : []}
                onOptionSelect={(_, option) => {
                  if (option.optionValue) {
                    handleStyleChange('imageStyle', option.optionValue as string);
                  }
                }}
              >
                {availableStyles.map((style) => (
                  <Option key={style} value={style}>
                    {style.charAt(0).toUpperCase() + style.slice(1)}
                  </Option>
                ))}
              </Dropdown>
            </Field>
          )}
        </div>

        {advancedMode && (
          <div className={styles.formRow}>
            <Field label="Aspect Ratio" className={styles.formField}>
              <Dropdown
                value={data.imageAspectRatio || '16:9'}
                selectedOptions={[data.imageAspectRatio || '16:9']}
                onOptionSelect={(_, option) => {
                  if (option.optionValue) {
                    handleStyleChange('imageAspectRatio', option.optionValue as string);
                  }
                }}
              >
                <Option value="16:9">16:9 (Widescreen)</Option>
                <Option value="9:16">9:16 (Portrait)</Option>
                <Option value="1:1">1:1 (Square)</Option>
                <Option value="4:3">4:3 (Standard)</Option>
              </Dropdown>
            </Field>

            <Field
              label={`Image Quality: ${data.imageQuality || 80}%`}
              className={styles.formField}
            >
              <Slider
                value={data.imageQuality || 80}
                min={50}
                max={100}
                step={10}
                onChange={(_, sliderData) => handleStyleChange('imageQuality', sliderData.value)}
              />
            </Field>
          </div>
        )}
      </div>

      <div className={styles.section}>
        <Title3>Music Settings</Title3>
        <div className={styles.formRow}>
          <Field label="Music Genre" className={styles.formField}>
            <Dropdown
              value={data.musicGenre}
              selectedOptions={[data.musicGenre]}
              onOptionSelect={(_, option) => {
                if (option.optionValue) {
                  handleStyleChange('musicGenre', option.optionValue as string);
                }
              }}
            >
              <Option value="ambient">Ambient</Option>
              <Option value="upbeat">Upbeat</Option>
              <Option value="dramatic">Dramatic</Option>
              <Option value="none">None</Option>
            </Dropdown>
          </Field>
        </div>
      </div>
    </div>
  );
};
