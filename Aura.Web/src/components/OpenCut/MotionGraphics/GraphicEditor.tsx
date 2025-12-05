/**
 * GraphicEditor Component
 *
 * Customization panel for applied graphics featuring organized sections
 * for Text, Colors, Layout, Animation, and Advanced options.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Input,
  Slider,
  Switch,
  Dropdown,
  Option,
  Label,
  Divider,
  mergeClasses,
} from '@fluentui/react-components';
import {
  TextT24Regular,
  Color24Regular,
  LayoutCellFour24Regular,
  Play24Regular,
  Settings24Regular,
  ArrowReset24Regular,
} from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import { openCutTokens } from '../../../styles/designTokens';
import type { CustomizationField } from '../../../types/motionGraphics';

export interface GraphicEditorProps {
  graphicId: string;
  className?: string;
}

type EditorSection = 'text' | 'colors' | 'layout' | 'animation' | 'advanced';

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
    gap: '2px',
    flex: 1,
    minWidth: 0,
  },
  graphicName: {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  graphicType: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  sections: {
    flex: 1,
    overflow: 'auto',
    padding: openCutTokens.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.lg,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    cursor: 'pointer',
    padding: `${openCutTokens.spacing.xs} 0`,
    ':hover': {
      opacity: 0.8,
    },
  },
  sectionIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '18px',
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  sectionContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
    paddingLeft: openCutTokens.spacing.lg,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xxs,
  },
  fieldLabel: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground2,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  fieldCharCount: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  colorInput: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
  },
  colorSwatch: {
    width: '32px',
    height: '32px',
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    cursor: 'pointer',
    transition: 'transform 100ms ease-out, box-shadow 100ms ease-out',
    ':hover': {
      transform: 'scale(1.05)',
      boxShadow: tokens.shadow4,
    },
  },
  colorHexInput: {
    flex: 1,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  sliderContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  sliderValue: {
    minWidth: '40px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground2,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  resetButton: {
    minWidth: '36px',
    minHeight: '32px',
  },
  footer: {
    padding: openCutTokens.spacing.md,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    justifyContent: 'flex-end',
  },
});

const sectionIcons: Record<EditorSection, React.ReactNode> = {
  text: <TextT24Regular />,
  colors: <Color24Regular />,
  layout: <LayoutCellFour24Regular />,
  animation: <Play24Regular />,
  advanced: <Settings24Regular />,
};

const sectionLabels: Record<EditorSection, string> = {
  text: 'Text',
  colors: 'Colors',
  layout: 'Layout',
  animation: 'Animation',
  advanced: 'Advanced',
};

export const GraphicEditor: FC<GraphicEditorProps> = ({ graphicId, className }) => {
  const styles = useStyles();
  const [expandedSections, setExpandedSections] = useState<Set<EditorSection>>(
    new Set(['text', 'colors'])
  );

  const graphicsStore = useMotionGraphicsStore();
  const graphic = graphicsStore.applied.find((g) => g.id === graphicId);
  const asset = graphic ? graphicsStore.getAsset(graphic.assetId) : null;

  const toggleSection = useCallback((section: EditorSection) => {
    setExpandedSections((prev) => {
      const next = new Set(prev);
      if (next.has(section)) {
        next.delete(section);
      } else {
        next.add(section);
      }
      return next;
    });
  }, []);

  const handleValueChange = useCallback(
    (fieldId: string, value: string | number | boolean) => {
      graphicsStore.updateGraphicValue(graphicId, fieldId, value);
    },
    [graphicsStore, graphicId]
  );

  const handleResetToDefaults = useCallback(() => {
    if (!asset) return;
    graphicsStore.updateGraphic(graphicId, {
      customValues: { ...asset.defaultValues },
    });
  }, [graphicsStore, graphicId, asset]);

  // Group fields by category
  const fieldsByCategory = useMemo(() => {
    if (!asset) return {};

    const groups: Record<string, CustomizationField[]> = {};
    asset.customization.fields.forEach((field) => {
      if (!groups[field.category]) {
        groups[field.category] = [];
      }
      groups[field.category].push(field);
    });
    return groups;
  }, [asset]);

  if (!graphic || !asset) {
    return (
      <div className={mergeClasses(styles.container, className)}>
        <div className={styles.header}>
          <Text size={300}>No graphic selected</Text>
        </div>
      </div>
    );
  }

  const renderField = (field: CustomizationField) => {
    const currentValue = graphic.customValues[field.id] ?? field.defaultValue;

    switch (field.type) {
      case 'text':
        return (
          <div key={field.id} className={styles.fieldGroup}>
            <div className={styles.fieldLabel}>
              <Label htmlFor={field.id}>{field.label}</Label>
              {field.maxLength && (
                <span className={styles.fieldCharCount}>
                  {String(currentValue).length}/{field.maxLength}
                </span>
              )}
            </div>
            <Input
              id={field.id}
              size="small"
              value={String(currentValue)}
              onChange={(_, data) => handleValueChange(field.id, data.value)}
              placeholder={field.placeholder}
              maxLength={field.maxLength}
            />
          </div>
        );

      case 'color':
        return (
          <div key={field.id} className={styles.fieldGroup}>
            <Label htmlFor={field.id}>{field.label}</Label>
            <div className={styles.colorInput}>
              <input
                type="color"
                id={field.id}
                value={String(currentValue)}
                onChange={(e) => handleValueChange(field.id, e.target.value)}
                className={styles.colorSwatch}
                style={{ backgroundColor: String(currentValue) }}
              />
              <Input
                className={styles.colorHexInput}
                size="small"
                value={String(currentValue).toUpperCase()}
                onChange={(_, data) => {
                  const value = data.value.startsWith('#') ? data.value : `#${data.value}`;
                  handleValueChange(field.id, value);
                }}
              />
            </div>
          </div>
        );

      case 'number':
      case 'slider':
        return (
          <div key={field.id} className={styles.fieldGroup}>
            <Label htmlFor={field.id}>{field.label}</Label>
            <div className={styles.sliderContainer}>
              <Slider
                id={field.id}
                min={field.min ?? 0}
                max={field.max ?? 100}
                step={field.step ?? 1}
                value={Number(currentValue)}
                onChange={(_, data) => handleValueChange(field.id, data.value)}
                style={{ flex: 1 }}
              />
              <span className={styles.sliderValue}>{currentValue}</span>
            </div>
          </div>
        );

      case 'toggle':
        return (
          <div key={field.id} className={styles.fieldGroup}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Label htmlFor={field.id}>{field.label}</Label>
              <Switch
                id={field.id}
                checked={Boolean(currentValue)}
                onChange={(_, data) => handleValueChange(field.id, data.checked)}
              />
            </div>
          </div>
        );

      case 'select':
        return (
          <div key={field.id} className={styles.fieldGroup}>
            <Label htmlFor={field.id}>{field.label}</Label>
            <Dropdown
              id={field.id}
              size="small"
              value={
                field.options?.find((o) => o.value === String(currentValue))?.label ||
                String(currentValue)
              }
              onOptionSelect={(_, data) => handleValueChange(field.id, data.optionValue as string)}
            >
              {field.options?.map((option) => (
                <Option key={option.value} value={option.value}>
                  {option.label}
                </Option>
              ))}
            </Dropdown>
          </div>
        );

      default:
        return null;
    }
  };

  const renderSection = (section: EditorSection) => {
    const fields = fieldsByCategory[section];
    if (!fields || fields.length === 0) return null;

    const isExpanded = expandedSections.has(section);

    return (
      <div key={section} className={styles.section}>
        <button
          type="button"
          className={styles.sectionHeader}
          onClick={() => toggleSection(section)}
          style={{ background: 'none', border: 'none', cursor: 'pointer', width: '100%' }}
        >
          <span className={styles.sectionIcon}>{sectionIcons[section]}</span>
          <Text className={styles.sectionTitle}>{sectionLabels[section]}</Text>
        </button>
        <AnimatePresence>
          {isExpanded && (
            <motion.div
              initial={{ height: 0, opacity: 0 }}
              animate={{ height: 'auto', opacity: 1 }}
              exit={{ height: 0, opacity: 0 }}
              transition={{ duration: 0.2 }}
              className={styles.sectionContent}
            >
              {fields.map(renderField)}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    );
  };

  return (
    <div className={mergeClasses(styles.container, className)}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Text weight="semibold" size={400} className={styles.graphicName}>
            {graphic.name || asset.name}
          </Text>
          <Text className={styles.graphicType}>{asset.category}</Text>
        </div>
        <Button
          appearance="subtle"
          size="small"
          icon={<ArrowReset24Regular />}
          onClick={handleResetToDefaults}
          className={styles.resetButton}
          title="Reset to defaults"
        />
      </div>

      {/* Sections */}
      <div className={styles.sections}>
        {renderSection('text')}
        {renderSection('colors')}
        <Divider />
        {renderSection('layout')}
        {renderSection('animation')}
        {renderSection('advanced')}
      </div>
    </div>
  );
};

export default GraphicEditor;
