import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Input,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Tooltip,
} from '@fluentui/react-components';
import {
  SearchRegular,
  WandRegular,
  ColorFillRegular,
  BlurRegular,
  ArrowRotateClockwiseRegular,
  PanelLeftRegular,
  StarRegular,
} from '@fluentui/react-icons';
import { EFFECT_DEFINITIONS, EFFECT_PRESETS, EffectCategory, EffectDefinition, EffectPreset } from '../../types/effects';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  title: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  searchContainer: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalS,
  },
  effectItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    cursor: 'grab',
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    transition: 'all 0.2s ease',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    '&:active': {
      cursor: 'grabbing',
    },
  },
  effectIcon: {
    fontSize: '20px',
    color: tokens.colorBrandForeground1,
  },
  effectInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  effectName: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  effectDescription: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  presetItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    transition: 'all 0.2s ease',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  presetHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  presetName: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    flex: 1,
  },
  presetCategory: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorBrandForeground1,
    padding: `2px 6px`,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusSmall,
  },
  presetDescription: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
});

interface EffectsLibraryPanelProps {
  onEffectDragStart?: (effectType: string) => void;
  onPresetApply?: (preset: EffectPreset) => void;
}

export function EffectsLibraryPanel({ onEffectDragStart, onPresetApply }: EffectsLibraryPanelProps) {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');

  // Filter effects based on search
  const filteredEffects = EFFECT_DEFINITIONS.filter(
    (effect) =>
      effect.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      effect.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      effect.category.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Group effects by category
  const effectsByCategory: Record<EffectCategory, EffectDefinition[]> = {
    [EffectCategory.ColorCorrection]: [],
    [EffectCategory.BlurSharpen]: [],
    [EffectCategory.Transform]: [],
    [EffectCategory.Transitions]: [],
    [EffectCategory.Stylize]: [],
    [EffectCategory.Compositing]: [],
  };

  filteredEffects.forEach((effect) => {
    effectsByCategory[effect.category].push(effect);
  });

  const getCategoryIcon = (category: EffectCategory) => {
    switch (category) {
      case EffectCategory.ColorCorrection:
        return <ColorFillRegular className={styles.effectIcon} />;
      case EffectCategory.BlurSharpen:
        return <BlurRegular className={styles.effectIcon} />;
      case EffectCategory.Transform:
        return <ArrowRotateClockwiseRegular className={styles.effectIcon} />;
      case EffectCategory.Transitions:
        return <PanelLeftRegular className={styles.effectIcon} />;
      case EffectCategory.Stylize:
        return <WandRegular className={styles.effectIcon} />;
      default:
        return <WandRegular className={styles.effectIcon} />;
    }
  };

  const handleEffectDragStart = (e: React.DragEvent, effectType: string) => {
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('application/json', JSON.stringify({ type: 'effect', effectType }));
    onEffectDragStart?.(effectType);
  };

  const handlePresetClick = (preset: EffectPreset) => {
    onPresetApply?.(preset);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>Effects Library</Text>
      </div>

      <div className={styles.searchContainer}>
        <Input
          placeholder="Search effects..."
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
          contentBefore={<SearchRegular />}
        />
      </div>

      <div className={styles.content}>
        <Accordion collapsible multiple defaultOpenItems={['presets', 'color-correction']}>
          {/* Effect Presets */}
          <AccordionItem value="presets">
            <AccordionHeader icon={<StarRegular />}>Effect Presets</AccordionHeader>
            <AccordionPanel>
              {EFFECT_PRESETS.map((preset) => (
                <Tooltip
                  key={preset.id}
                  content={`${preset.effects.length} effects: ${preset.description}`}
                  relationship="description"
                >
                  <div 
                    className={styles.presetItem} 
                    onClick={() => handlePresetClick(preset)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault();
                        handlePresetClick(preset);
                      }
                    }}
                    role="button"
                    tabIndex={0}
                    aria-label={`Apply ${preset.name} preset`}
                  >
                    <div className={styles.presetHeader}>
                      <Text className={styles.presetName}>{preset.name}</Text>
                      {preset.category && (
                        <Text className={styles.presetCategory}>{preset.category}</Text>
                      )}
                    </div>
                    <Text className={styles.presetDescription}>{preset.description}</Text>
                  </div>
                </Tooltip>
              ))}
            </AccordionPanel>
          </AccordionItem>

          {/* Effect Categories */}
          {Object.entries(effectsByCategory).map(([category, effects]) => {
            if (effects.length === 0) return null;

            return (
              <AccordionItem key={category} value={category.toLowerCase().replace(/\s+/g, '-')}>
                <AccordionHeader icon={getCategoryIcon(category as EffectCategory)}>
                  {category}
                </AccordionHeader>
                <AccordionPanel>
                  {effects.map((effect) => (
                    <Tooltip
                      key={effect.type}
                      content={`Drag to clip or properties panel: ${effect.description}`}
                      relationship="description"
                    >
                      <div
                        className={styles.effectItem}
                        draggable
                        onDragStart={(e) => handleEffectDragStart(e, effect.type)}
                      >
                        {getCategoryIcon(effect.category)}
                        <div className={styles.effectInfo}>
                          <Text className={styles.effectName}>{effect.name}</Text>
                          <Text className={styles.effectDescription}>{effect.description}</Text>
                        </div>
                      </div>
                    </Tooltip>
                  ))}
                </AccordionPanel>
              </AccordionItem>
            );
          })}
        </Accordion>

        {filteredEffects.length === 0 && (
          <div className={styles.emptyState}>
            <Text>No effects found matching "{searchQuery}"</Text>
          </div>
        )}
      </div>
    </div>
  );
}
