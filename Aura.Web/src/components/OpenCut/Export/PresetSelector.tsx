/**
 * PresetSelector Component
 *
 * Grid-based preset selection panel with platform tabs,
 * preset cards showing specs, and quick info tooltips.
 */

import {
  makeStyles,
  tokens,
  Text,
  TabList,
  Tab,
  Tooltip,
  mergeClasses,
} from '@fluentui/react-components';
import { Video24Regular, Document24Regular, Phone24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC, ReactElement } from 'react';
import { useExportStore, type ExportPreset } from '../../../stores/opencutExport';
import { openCutTokens } from '../../../styles/designTokens';

export interface PresetSelectorProps {
  className?: string;
  onPresetSelect?: (presetId: string) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  tabList: {
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
  },
  tab: {
    minWidth: 'auto',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    fontSize: tokens.fontSizeBase200,
  },
  presetsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))',
    gap: tokens.spacingHorizontalS,
  },
  presetCard: {
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    transition: 'all 0.15s ease',
    border: `1px solid transparent`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
      transform: 'translateY(-2px)',
      boxShadow: openCutTokens.shadows.sm,
    },
    ':active': {
      transform: 'translateY(0)',
    },
  },
  presetCardSelected: {
    border: `1px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3Hover,
  },
  presetCardCustom: {
    border: `1px dashed ${tokens.colorNeutralStroke2}`,
  },
  presetIcon: {
    width: '40px',
    height: '40px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorBrandForeground1,
    fontSize: '20px',
  },
  presetName: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: 600,
    marginBottom: tokens.spacingVerticalXS,
  },
  presetSpecs: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  presetPlatform: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
    marginTop: tokens.spacingVerticalXS,
  },
});

const getPlatformIcon = (platform: string): ReactElement => {
  switch (platform) {
    case 'Video Platform':
      return <Video24Regular />;
    case 'Mobile Video':
      return <Phone24Regular />;
    default:
      return <Document24Regular />;
  }
};

const formatResolution = (width: number, height: number): string => {
  if (width === height) {
    return `${width}×${height}`;
  }
  if (width > height) {
    return `${height}p`;
  }
  return `${width}×${height}`;
};

const PresetCard: FC<{
  preset: ExportPreset;
  isSelected: boolean;
  isCustom: boolean;
  onClick: () => void;
}> = ({ preset, isSelected, isCustom, onClick }) => {
  const styles = useStyles();
  const { settings } = preset;

  const specs = useMemo(() => {
    const parts: string[] = [];
    parts.push(formatResolution(settings.resolution.width, settings.resolution.height));
    if (settings.frameRate) {
      parts.push(`${settings.frameRate}fps`);
    }
    if (settings.videoBitrate > 0) {
      parts.push(`${settings.videoBitrate / 1000}Mbps`);
    }
    return parts.join(' • ');
  }, [settings]);

  return (
    <Tooltip content={preset.description} relationship="description" positioning="below">
      <div
        className={mergeClasses(
          styles.presetCard,
          isSelected && styles.presetCardSelected,
          isCustom && styles.presetCardCustom
        )}
        onClick={onClick}
        role="button"
        tabIndex={0}
        aria-label={`${preset.name} preset`}
        aria-pressed={isSelected}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onClick();
          }
        }}
      >
        <div className={styles.presetIcon}>{getPlatformIcon(preset.platform)}</div>
        <Text className={styles.presetName}>{preset.name}</Text>
        <Text className={styles.presetSpecs}>{specs}</Text>
        <Text className={styles.presetPlatform}>{preset.platform}</Text>
      </div>
    </Tooltip>
  );
};

export const PresetSelector: FC<PresetSelectorProps> = ({ className, onPresetSelect }) => {
  const styles = useStyles();
  const { builtinPresets, customPresets, selectedPresetId, selectPreset, getAllPlatforms } =
    useExportStore();

  const platforms = useMemo(() => {
    return ['All', ...getAllPlatforms()];
  }, [getAllPlatforms]);

  const [selectedPlatform, setSelectedPlatform] = useState('All');

  const allPresets = useMemo(() => {
    return [...builtinPresets, ...customPresets];
  }, [builtinPresets, customPresets]);

  const filteredPresets = useMemo(() => {
    if (selectedPlatform === 'All') {
      return allPresets;
    }
    return allPresets.filter((p) => p.platform === selectedPlatform);
  }, [allPresets, selectedPlatform]);

  const handlePlatformChange = useCallback((_: unknown, data: { value: string }) => {
    setSelectedPlatform(data.value);
  }, []);

  const handlePresetClick = useCallback(
    (presetId: string) => {
      selectPreset(presetId);
      onPresetSelect?.(presetId);
    },
    [selectPreset, onPresetSelect]
  );

  const customPresetIds = useMemo(() => new Set(customPresets.map((p) => p.id)), [customPresets]);

  return (
    <div className={mergeClasses(styles.container, className)}>
      <TabList
        className={styles.tabList}
        selectedValue={selectedPlatform}
        onTabSelect={handlePlatformChange}
        size="small"
      >
        {platforms.map((platform) => (
          <Tab key={platform} value={platform} className={styles.tab}>
            {platform}
          </Tab>
        ))}
      </TabList>

      <div className={styles.presetsGrid}>
        {filteredPresets.map((preset) => (
          <PresetCard
            key={preset.id}
            preset={preset}
            isSelected={selectedPresetId === preset.id}
            isCustom={customPresetIds.has(preset.id)}
            onClick={() => handlePresetClick(preset.id)}
          />
        ))}
      </div>
    </div>
  );
};

export default PresetSelector;
