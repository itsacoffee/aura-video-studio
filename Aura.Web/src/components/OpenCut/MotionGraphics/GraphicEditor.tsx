/**
 * GraphicEditor Component
 *
 * Customization panel for applied motion graphics featuring
 * organized sections for text, colors, layout, animation, and advanced options.
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  Slider,
  Button,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Tooltip,
  mergeClasses,
} from '@fluentui/react-components';
import {
  TextT24Regular,
  Color24Regular,
  LayoutRowFour24Regular,
  Timer24Regular,
  Settings24Regular,
  ArrowReset24Regular,
} from '@fluentui/react-icons';
import { useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import { openCutTokens } from '../../../styles/designTokens';
import type { AppliedGraphic } from '../../../types/motionGraphics';

export interface GraphicEditorProps {
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.md}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '56px',
    gap: openCutTokens.spacing.sm,
  },
  headerTitle: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  graphicName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  graphicCategory: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    textTransform: 'capitalize',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
  },
  accordion: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  accordionItem: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke3}`,
    overflow: 'hidden',
  },
  accordionHeader: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  accordionIcon: {
    marginRight: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForeground3,
  },
  formSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalS,
  },
  formField: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  fieldLabel: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground2,
  },
  fieldDescription: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  sliderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  sliderValue: {
    minWidth: '48px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    color: tokens.colorNeutralForeground3,
  },
  colorSwatches: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
  colorSwatch: {
    width: '24px',
    height: '24px',
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    cursor: 'pointer',
    transition: 'transform 100ms ease-out, box-shadow 100ms ease-out',
    ':hover': {
      transform: 'scale(1.1)',
      boxShadow: tokens.shadow4,
    },
  },
  colorSwatchSelected: {
    outline: `2px solid ${tokens.colorBrandStroke1}`,
    outlineOffset: '2px',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    padding: tokens.spacingVerticalXL,
  },
  transformSection: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalS,
  },
  footer: {
    padding: tokens.spacingHorizontalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
});

/**
 * Get category display name
 */
function getCategoryDisplayName(category: string): string {
  return category.replace(/-/g, ' ');
}

export const GraphicEditor: FC<GraphicEditorProps> = ({ className }) => {
  const styles = useStyles();
  const graphicsStore = useMotionGraphicsStore();

  const selectedGraphic = graphicsStore.getSelectedGraphic();
  const asset = selectedGraphic ? graphicsStore.getAsset(selectedGraphic.assetId) : null;

  const handleValueChange = useCallback(
    (key: string, value: string | number | boolean) => {
      if (selectedGraphic) {
        graphicsStore.updateGraphicValue(selectedGraphic.id, key, value);
      }
    },
    [graphicsStore, selectedGraphic]
  );

  const handleTransformChange = useCallback(
    (property: keyof AppliedGraphic, value: number) => {
      if (selectedGraphic) {
        graphicsStore.updateGraphic(selectedGraphic.id, { [property]: value });
      }
    },
    [graphicsStore, selectedGraphic]
  );

  const handleReset = useCallback(() => {
    if (!selectedGraphic || !asset) return;

    const defaultValues: Record<string, string | number | boolean> = {};
    asset.customization.text.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.colors.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.layout.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.animation.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.advanced.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });

    graphicsStore.updateGraphic(selectedGraphic.id, {
      customValues: defaultValues,
      positionX: 50,
      positionY: 50,
      scale: 1,
      opacity: 1,
    });
  }, [graphicsStore, selectedGraphic, asset]);

  // Check which sections have content
  const hasTextFields = asset && asset.customization.text.length > 0;
  const hasColorFields = asset && asset.customization.colors.length > 0;
  const hasLayoutFields = asset && asset.customization.layout.length > 0;
  const hasAnimationFields = asset && asset.customization.animation.length > 0;
  const hasAdvancedFields = asset && asset.customization.advanced.length > 0;

  // Get default open sections
  const defaultOpenItems = useMemo(() => {
    const items: string[] = [];
    if (hasTextFields) items.push('text');
    if (hasColorFields) items.push('colors');
    return items;
  }, [hasTextFields, hasColorFields]);

  if (!selectedGraphic || !asset) {
    return (
      <div className={mergeClasses(styles.container, className)}>
        <div className={styles.emptyState}>
          <TextT24Regular style={{ fontSize: 48, opacity: 0.5 }} />
          <div>
            <Text weight="semibold" size={400}>
              No graphic selected
            </Text>
            <Text size={200} style={{ display: 'block', marginTop: 4 }}>
              Select a graphic from the timeline to customize it
            </Text>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Text className={styles.graphicName}>{asset.name}</Text>
          <Text className={styles.graphicCategory}>{getCategoryDisplayName(asset.category)}</Text>
        </div>
        <Tooltip content="Reset to defaults" relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={<ArrowReset24Regular />}
            onClick={handleReset}
          />
        </Tooltip>
      </div>

      <div className={styles.content}>
        <Accordion className={styles.accordion} multiple defaultOpenItems={defaultOpenItems}>
          {/* Text Section */}
          {hasTextFields && (
            <AccordionItem value="text" className={styles.accordionItem}>
              <AccordionHeader className={styles.accordionHeader}>
                <TextT24Regular className={styles.accordionIcon} />
                Text
              </AccordionHeader>
              <AccordionPanel>
                <div className={styles.formSection}>
                  {asset.customization.text.map((field) => (
                    <div key={field.id} className={styles.formField}>
                      <Text className={styles.fieldLabel}>{field.label}</Text>
                      <Input
                        size="small"
                        value={String(selectedGraphic.customValues[field.id] ?? field.defaultValue)}
                        onChange={(_, data) => handleValueChange(field.id, data.value)}
                        placeholder={field.placeholder}
                        maxLength={field.maxLength}
                      />
                      {field.maxLength && (
                        <Text className={styles.fieldDescription}>
                          {
                            String(selectedGraphic.customValues[field.id] ?? field.defaultValue)
                              .length
                          }{' '}
                          / {field.maxLength}
                        </Text>
                      )}
                    </div>
                  ))}
                </div>
              </AccordionPanel>
            </AccordionItem>
          )}

          {/* Colors Section */}
          {hasColorFields && (
            <AccordionItem value="colors" className={styles.accordionItem}>
              <AccordionHeader className={styles.accordionHeader}>
                <Color24Regular className={styles.accordionIcon} />
                Colors
              </AccordionHeader>
              <AccordionPanel>
                <div className={styles.formSection}>
                  {asset.customization.colors.map((field) => (
                    <div key={field.id} className={styles.formField}>
                      <Text className={styles.fieldLabel}>{field.label}</Text>
                      <input
                        type="color"
                        value={String(selectedGraphic.customValues[field.id] ?? field.defaultValue)}
                        onChange={(e) => handleValueChange(field.id, e.target.value)}
                        style={{
                          padding: 0,
                          height: 32,
                          width: '100%',
                          border: 'none',
                          borderRadius: 4,
                          cursor: 'pointer',
                        }}
                      />
                      {field.presets && field.presets.length > 0 && (
                        <div className={styles.colorSwatches}>
                          {field.presets.map((color) => (
                            <div
                              key={color}
                              className={mergeClasses(
                                styles.colorSwatch,
                                selectedGraphic.customValues[field.id] === color &&
                                  styles.colorSwatchSelected
                              )}
                              style={{ backgroundColor: color }}
                              onClick={() => handleValueChange(field.id, color)}
                              role="button"
                              tabIndex={0}
                              aria-label={`Select color ${color}`}
                              onKeyDown={(e) => {
                                if (e.key === 'Enter' || e.key === ' ') {
                                  handleValueChange(field.id, color);
                                }
                              }}
                            />
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </AccordionPanel>
            </AccordionItem>
          )}

          {/* Layout Section */}
          <AccordionItem value="layout" className={styles.accordionItem}>
            <AccordionHeader className={styles.accordionHeader}>
              <LayoutRowFour24Regular className={styles.accordionIcon} />
              Layout
            </AccordionHeader>
            <AccordionPanel>
              <div className={styles.formSection}>
                <div className={styles.transformSection}>
                  <div className={styles.formField}>
                    <Text className={styles.fieldLabel}>Position X</Text>
                    <div className={styles.sliderRow}>
                      <Slider
                        size="small"
                        min={0}
                        max={100}
                        step={1}
                        value={selectedGraphic.positionX}
                        onChange={(_, data) => handleTransformChange('positionX', data.value)}
                        style={{ flex: 1 }}
                      />
                      <Text className={styles.sliderValue}>
                        {Math.round(selectedGraphic.positionX)}%
                      </Text>
                    </div>
                  </div>
                  <div className={styles.formField}>
                    <Text className={styles.fieldLabel}>Position Y</Text>
                    <div className={styles.sliderRow}>
                      <Slider
                        size="small"
                        min={0}
                        max={100}
                        step={1}
                        value={selectedGraphic.positionY}
                        onChange={(_, data) => handleTransformChange('positionY', data.value)}
                        style={{ flex: 1 }}
                      />
                      <Text className={styles.sliderValue}>
                        {Math.round(selectedGraphic.positionY)}%
                      </Text>
                    </div>
                  </div>
                </div>

                <div className={styles.formField}>
                  <Text className={styles.fieldLabel}>Scale</Text>
                  <div className={styles.sliderRow}>
                    <Slider
                      size="small"
                      min={0.1}
                      max={3}
                      step={0.1}
                      value={selectedGraphic.scale}
                      onChange={(_, data) => handleTransformChange('scale', data.value)}
                      style={{ flex: 1 }}
                    />
                    <Text className={styles.sliderValue}>
                      {Math.round(selectedGraphic.scale * 100)}%
                    </Text>
                  </div>
                </div>

                <div className={styles.formField}>
                  <Text className={styles.fieldLabel}>Opacity</Text>
                  <div className={styles.sliderRow}>
                    <Slider
                      size="small"
                      min={0}
                      max={1}
                      step={0.05}
                      value={selectedGraphic.opacity}
                      onChange={(_, data) => handleTransformChange('opacity', data.value)}
                      style={{ flex: 1 }}
                    />
                    <Text className={styles.sliderValue}>
                      {Math.round(selectedGraphic.opacity * 100)}%
                    </Text>
                  </div>
                </div>

                {hasLayoutFields &&
                  asset.customization.layout.map((field) => (
                    <div key={field.id} className={styles.formField}>
                      <Text className={styles.fieldLabel}>
                        {field.label}
                        {field.unit && ` (${field.unit})`}
                      </Text>
                      <div className={styles.sliderRow}>
                        <Slider
                          size="small"
                          min={field.min ?? 0}
                          max={field.max ?? 100}
                          step={field.step ?? 1}
                          value={Number(
                            selectedGraphic.customValues[field.id] ?? field.defaultValue
                          )}
                          onChange={(_, data) => handleValueChange(field.id, data.value)}
                          style={{ flex: 1 }}
                        />
                        <Text className={styles.sliderValue}>
                          {Number(selectedGraphic.customValues[field.id] ?? field.defaultValue)}
                        </Text>
                      </div>
                    </div>
                  ))}
              </div>
            </AccordionPanel>
          </AccordionItem>

          {/* Animation Section */}
          <AccordionItem value="animation" className={styles.accordionItem}>
            <AccordionHeader className={styles.accordionHeader}>
              <Timer24Regular className={styles.accordionIcon} />
              Animation
            </AccordionHeader>
            <AccordionPanel>
              <div className={styles.formSection}>
                <div className={styles.formField}>
                  <Text className={styles.fieldLabel}>Duration (seconds)</Text>
                  <div className={styles.sliderRow}>
                    <Slider
                      size="small"
                      min={asset.minDuration}
                      max={asset.maxDuration}
                      step={0.5}
                      value={selectedGraphic.duration}
                      onChange={(_, data) =>
                        graphicsStore.updateGraphic(selectedGraphic.id, {
                          duration: data.value,
                        })
                      }
                      style={{ flex: 1 }}
                    />
                    <Text className={styles.sliderValue}>
                      {selectedGraphic.duration.toFixed(1)}s
                    </Text>
                  </div>
                </div>

                {hasAnimationFields &&
                  asset.customization.animation.map((field) => (
                    <div key={field.id} className={styles.formField}>
                      <Text className={styles.fieldLabel}>{field.label}</Text>
                      <div className={styles.sliderRow}>
                        <Slider
                          size="small"
                          min={field.min ?? 0}
                          max={field.max ?? 2}
                          step={0.1}
                          value={Number(
                            selectedGraphic.customValues[field.id] ?? field.defaultValue
                          )}
                          onChange={(_, data) => handleValueChange(field.id, data.value)}
                          style={{ flex: 1 }}
                        />
                        <Text className={styles.sliderValue}>
                          {Number(
                            selectedGraphic.customValues[field.id] ?? field.defaultValue
                          ).toFixed(1)}
                          s
                        </Text>
                      </div>
                    </div>
                  ))}
              </div>
            </AccordionPanel>
          </AccordionItem>

          {/* Advanced Section */}
          {hasAdvancedFields && (
            <AccordionItem value="advanced" className={styles.accordionItem}>
              <AccordionHeader className={styles.accordionHeader}>
                <Settings24Regular className={styles.accordionIcon} />
                Advanced
              </AccordionHeader>
              <AccordionPanel>
                <div className={styles.formSection}>
                  {asset.customization.advanced.map((field) => {
                    if (field.type === 'slider') {
                      return (
                        <div key={field.id} className={styles.formField}>
                          <Text className={styles.fieldLabel}>{field.label}</Text>
                          <div className={styles.sliderRow}>
                            <Slider
                              size="small"
                              min={field.min ?? 0}
                              max={field.max ?? 100}
                              step={field.step ?? 1}
                              value={Number(
                                selectedGraphic.customValues[field.id] ?? field.defaultValue
                              )}
                              onChange={(_, data) => handleValueChange(field.id, data.value)}
                              style={{ flex: 1 }}
                            />
                            <Text className={styles.sliderValue}>
                              {Number(selectedGraphic.customValues[field.id] ?? field.defaultValue)}
                            </Text>
                          </div>
                        </div>
                      );
                    }

                    if (field.type === 'toggle') {
                      const isChecked = Boolean(
                        selectedGraphic.customValues[field.id] ?? field.defaultValue
                      );
                      return (
                        <div key={field.id} className={styles.formField}>
                          <Button
                            appearance={isChecked ? 'primary' : 'secondary'}
                            size="small"
                            onClick={() => handleValueChange(field.id, !isChecked)}
                          >
                            {field.label}: {isChecked ? 'On' : 'Off'}
                          </Button>
                        </div>
                      );
                    }

                    return null;
                  })}
                </div>
              </AccordionPanel>
            </AccordionItem>
          )}
        </Accordion>
      </div>
    </div>
  );
};

export default GraphicEditor;
