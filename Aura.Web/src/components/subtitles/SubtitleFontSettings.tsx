/**
 * Subtitle Font Settings Component
 * Configure font settings for subtitles with RTL support
 */

import { Card, Text, Label, Input, Dropdown, Option } from '@fluentui/react-components';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import type { SubtitleFontConfigDto } from '@/types/api-v1';

interface SubtitleFontSettingsProps {
  isRTL: boolean;
  onChange: (config: SubtitleFontConfigDto) => void;
  initialConfig?: SubtitleFontConfigDto;
}

export const SubtitleFontSettings: FC<SubtitleFontSettingsProps> = ({
  isRTL,
  onChange,
  initialConfig,
}) => {
  const [config, setConfig] = useState<SubtitleFontConfigDto>(
    initialConfig || {
      fontFamily: isRTL ? 'Arial, Noto Sans Arabic, sans-serif' : 'Arial, sans-serif',
      fontSize: 24,
      primaryColor: 'FFFFFF',
      outlineColor: '000000',
      outlineWidth: 2,
      alignment: 'center',
      isRTL,
    }
  );

  useEffect(() => {
    onChange(config);
  }, [config, onChange]);

  const fontFamilies = isRTL
    ? [
        'Arial, Noto Sans Arabic, sans-serif',
        'Tahoma, sans-serif',
        'Microsoft Sans Serif',
        'Segoe UI, sans-serif',
        'Noto Sans Arabic',
      ]
    : [
        'Arial, sans-serif',
        'Helvetica, sans-serif',
        'Verdana, sans-serif',
        'Segoe UI, sans-serif',
        'Roboto, sans-serif',
      ];

  const alignments = [
    { key: 'left', text: 'Left' },
    { key: 'center', text: 'Center' },
    { key: 'right', text: 'Right' },
  ];

  return (
    <Card style={{ padding: '16px' }}>
      <Text weight="semibold" size={400} style={{ marginBottom: '16px', display: 'block' }}>
        Subtitle Font Settings
      </Text>

      <div style={{ display: 'grid', gap: '16px' }}>
        <div>
          <Label htmlFor="fontFamily">Font Family</Label>
          <Dropdown
            id="fontFamily"
            value={config.fontFamily}
            onOptionSelect={(_, data) => {
              if (data.optionValue) {
                setConfig({ ...config, fontFamily: data.optionValue });
              }
            }}
          >
            {fontFamilies.map((font) => (
              <Option key={font} value={font}>
                {font}
              </Option>
            ))}
          </Dropdown>
        </div>

        <div>
          <Label htmlFor="fontSize">Font Size (12-72pt)</Label>
          <Input
            id="fontSize"
            type="number"
            value={config.fontSize.toString()}
            min={12}
            max={72}
            onChange={(e) => {
              const value = parseInt(e.target.value, 10);
              if (!isNaN(value) && value >= 12 && value <= 72) {
                setConfig({ ...config, fontSize: value });
              }
            }}
          />
        </div>

        <div>
          <Label htmlFor="primaryColor">Text Color (Hex)</Label>
          <Input
            id="primaryColor"
            value={config.primaryColor}
            onChange={(e) => setConfig({ ...config, primaryColor: e.target.value })}
            placeholder="FFFFFF"
          />
        </div>

        <div>
          <Label htmlFor="outlineColor">Outline Color (Hex)</Label>
          <Input
            id="outlineColor"
            value={config.outlineColor}
            onChange={(e) => setConfig({ ...config, outlineColor: e.target.value })}
            placeholder="000000"
          />
        </div>

        <div>
          <Label htmlFor="outlineWidth">Outline Width</Label>
          <Input
            id="outlineWidth"
            type="number"
            value={config.outlineWidth.toString()}
            onChange={(e) => {
              const value = parseInt(e.target.value, 10);
              if (!isNaN(value) && value >= 0) {
                setConfig({ ...config, outlineWidth: value });
              }
            }}
          />
        </div>

        <div>
          <Label htmlFor="alignment">Text Alignment</Label>
          <Dropdown
            id="alignment"
            value={config.alignment}
            onOptionSelect={(_, data) => {
              if (data.optionValue) {
                setConfig({ ...config, alignment: data.optionValue });
              }
            }}
          >
            {alignments.map((align) => (
              <Option key={align.key} value={align.key}>
                {align.text}
              </Option>
            ))}
          </Dropdown>
        </div>

        {isRTL && (
          <div style={{ marginTop: '8px' }}>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              ✓ RTL font fallback configured
            </Text>
          </div>
        )}
      </div>
    </Card>
  );
};
