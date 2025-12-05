/**
 * CaptionStyleEditor Component
 *
 * Caption styling controls:
 * - Font family selector
 * - Font size slider
 * - Color pickers (text, background, outline)
 * - Position selector (top/center/bottom)
 * - Alignment controls
 */

import {
  makeStyles,
  tokens,
  Label,
  Slider,
  Select,
  ToggleButton,
  Tooltip,
} from '@fluentui/react-components';
import {
  TextAlignLeft24Regular,
  TextAlignCenter24Regular,
  TextAlignRight24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { openCutTokens } from '../../../styles/designTokens';
import type { TextAlign } from '../../../types/opencut';

export interface CaptionStyleEditorProps {
  className?: string;
}

const DEFAULT_BACKGROUND_COLOR = '#000000';
const DEFAULT_STROKE_COLOR = '#000000';
const DEFAULT_STROKE_WIDTH = 2;

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  label: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground2,
  },
  row: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
  },
  colorRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  colorInput: {
    width: '40px',
    height: '32px',
    padding: '2px',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: openCutTokens.radius.sm,
    cursor: 'pointer',
    '::-webkit-color-swatch-wrapper': {
      padding: 0,
    },
    '::-webkit-color-swatch': {
      border: 'none',
      borderRadius: '2px',
    },
  },
  colorLabel: {
    flex: 1,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  buttonGroup: {
    display: 'flex',
    gap: '2px',
  },
  sliderValue: {
    minWidth: '40px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    color: tokens.colorNeutralForeground2,
  },
  sliderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

const FONT_FAMILIES = [
  { value: 'Inter, system-ui, sans-serif', label: 'Inter (Default)' },
  { value: 'Arial, sans-serif', label: 'Arial' },
  { value: 'Helvetica, sans-serif', label: 'Helvetica' },
  { value: 'Georgia, serif', label: 'Georgia' },
  { value: 'Times New Roman, serif', label: 'Times New Roman' },
  { value: 'Courier New, monospace', label: 'Courier New' },
  { value: 'Verdana, sans-serif', label: 'Verdana' },
];

export const CaptionStyleEditor: FC<CaptionStyleEditorProps> = ({ className }) => {
  const styles = useStyles();
  const { defaultStyle, setDefaultStyle } = useOpenCutCaptionsStore();

  const handleFontFamilyChange = (value: string) => {
    setDefaultStyle({ fontFamily: value });
  };

  const handleFontSizeChange = (value: number) => {
    setDefaultStyle({ fontSize: value });
  };

  const handleColorChange = (value: string) => {
    setDefaultStyle({ color: value });
  };

  const handleBackgroundColorChange = (value: string) => {
    setDefaultStyle({ backgroundColor: value });
  };

  const handleStrokeColorChange = (value: string) => {
    setDefaultStyle({ strokeColor: value });
  };

  const handleStrokeWidthChange = (value: number) => {
    setDefaultStyle({ strokeWidth: value });
  };

  const handleTextAlignChange = (value: TextAlign) => {
    setDefaultStyle({ textAlign: value });
  };

  return (
    <div className={`${styles.root} ${className ?? ''}`}>
      {/* Font Family */}
      <div className={styles.field}>
        <Label className={styles.label}>Font Family</Label>
        <Select
          value={defaultStyle.fontFamily}
          onChange={(_, data) => handleFontFamilyChange(data.value)}
        >
          {FONT_FAMILIES.map((font) => (
            <option key={font.value} value={font.value}>
              {font.label}
            </option>
          ))}
        </Select>
      </div>

      {/* Font Size */}
      <div className={styles.field}>
        <Label className={styles.label}>Font Size</Label>
        <div className={styles.sliderRow}>
          <Slider
            min={12}
            max={72}
            value={defaultStyle.fontSize}
            onChange={(_, data) => handleFontSizeChange(data.value)}
            style={{ flex: 1 }}
          />
          <span className={styles.sliderValue}>{defaultStyle.fontSize}px</span>
        </div>
      </div>

      {/* Text Alignment */}
      <div className={styles.field}>
        <Label className={styles.label}>Alignment</Label>
        <div className={styles.buttonGroup}>
          <Tooltip content="Align left" relationship="label">
            <ToggleButton
              appearance="subtle"
              size="small"
              icon={<TextAlignLeft24Regular />}
              checked={defaultStyle.textAlign === 'left'}
              onClick={() => handleTextAlignChange('left')}
            />
          </Tooltip>
          <Tooltip content="Align center" relationship="label">
            <ToggleButton
              appearance="subtle"
              size="small"
              icon={<TextAlignCenter24Regular />}
              checked={defaultStyle.textAlign === 'center'}
              onClick={() => handleTextAlignChange('center')}
            />
          </Tooltip>
          <Tooltip content="Align right" relationship="label">
            <ToggleButton
              appearance="subtle"
              size="small"
              icon={<TextAlignRight24Regular />}
              checked={defaultStyle.textAlign === 'right'}
              onClick={() => handleTextAlignChange('right')}
            />
          </Tooltip>
        </div>
      </div>

      {/* Colors */}
      <div className={styles.field}>
        <Label className={styles.label}>Colors</Label>
        <div className={styles.colorRow}>
          <input
            type="color"
            value={defaultStyle.color}
            onChange={(e) => handleColorChange(e.target.value)}
            className={styles.colorInput}
            title="Text color"
          />
          <span className={styles.colorLabel}>Text</span>
        </div>
        <div className={styles.colorRow}>
          <input
            type="color"
            value={defaultStyle.backgroundColor ?? DEFAULT_BACKGROUND_COLOR}
            onChange={(e) => handleBackgroundColorChange(e.target.value)}
            className={styles.colorInput}
            title="Background color"
          />
          <span className={styles.colorLabel}>Background</span>
        </div>
        <div className={styles.colorRow}>
          <input
            type="color"
            value={defaultStyle.strokeColor ?? DEFAULT_STROKE_COLOR}
            onChange={(e) => handleStrokeColorChange(e.target.value)}
            className={styles.colorInput}
            title="Outline color"
          />
          <span className={styles.colorLabel}>Outline</span>
        </div>
      </div>

      {/* Outline Width */}
      <div className={styles.field}>
        <Label className={styles.label}>Outline Width</Label>
        <div className={styles.sliderRow}>
          <Slider
            min={0}
            max={8}
            value={defaultStyle.strokeWidth ?? DEFAULT_STROKE_WIDTH}
            onChange={(_, data) => handleStrokeWidthChange(data.value)}
            style={{ flex: 1 }}
          />
          <span className={styles.sliderValue}>
            {defaultStyle.strokeWidth ?? DEFAULT_STROKE_WIDTH}px
          </span>
        </div>
      </div>
    </div>
  );
};

export default CaptionStyleEditor;
