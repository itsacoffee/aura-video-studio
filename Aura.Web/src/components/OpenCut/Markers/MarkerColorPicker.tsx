/**
 * MarkerColorPicker Component
 *
 * Color selection palette for timeline markers.
 * Provides a visual color picker with all available marker colors.
 */

import {
  makeStyles,
  tokens,
  Button,
  Popover,
  PopoverTrigger,
  PopoverSurface,
} from '@fluentui/react-components';
import { Color24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { useState } from 'react';
import type { MarkerColor } from '../../../types/opencut';

export interface MarkerColorPickerProps {
  selectedColor: MarkerColor;
  onColorChange: (color: MarkerColor) => void;
  disabled?: boolean;
}

const MARKER_COLORS: { color: MarkerColor; hex: string; label: string }[] = [
  { color: 'blue', hex: tokens.colorPaletteBlueBorderActive, label: 'Blue' },
  { color: 'green', hex: tokens.colorPaletteGreenBorderActive, label: 'Green' },
  { color: 'orange', hex: tokens.colorPaletteDarkOrangeBorderActive, label: 'Orange' },
  { color: 'purple', hex: tokens.colorPalettePurpleBorderActive, label: 'Purple' },
  { color: 'red', hex: tokens.colorPaletteRedBorderActive, label: 'Red' },
  { color: 'yellow', hex: tokens.colorPaletteYellowBorderActive, label: 'Yellow' },
  { color: 'pink', hex: tokens.colorPalettePinkBorderActive, label: 'Pink' },
  { color: 'cyan', hex: tokens.colorPaletteTealBorderActive, label: 'Cyan' },
];

const useStyles = makeStyles({
  triggerButton: {
    minWidth: '36px',
    minHeight: '36px',
    padding: tokens.spacingHorizontalXS,
  },
  colorIndicator: {
    width: '16px',
    height: '16px',
    borderRadius: tokens.borderRadiusSmall,
    marginRight: tokens.spacingHorizontalXS,
  },
  surface: {
    display: 'grid',
    gridTemplateColumns: 'repeat(4, 1fr)',
    gap: tokens.spacingHorizontalXS,
    padding: tokens.spacingHorizontalS,
  },
});

export const MarkerColorPicker: FC<MarkerColorPickerProps> = ({
  selectedColor,
  onColorChange,
  disabled = false,
}) => {
  const styles = useStyles();
  const [open, setOpen] = useState(false);

  const selectedColorInfo =
    MARKER_COLORS.find((c) => c.color === selectedColor) || MARKER_COLORS[0];

  const handleColorSelect = (color: MarkerColor) => {
    onColorChange(color);
    setOpen(false);
  };

  return (
    <Popover open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      <PopoverTrigger>
        <Button
          appearance="subtle"
          className={styles.triggerButton}
          disabled={disabled}
          icon={<Color24Regular />}
          aria-label={`Color: ${selectedColorInfo.label}`}
        >
          <span
            className={styles.colorIndicator}
            style={{
              backgroundColor: selectedColorInfo.hex,
              border: `1px solid ${tokens.colorNeutralStroke1}`,
            }}
          />
        </Button>
      </PopoverTrigger>
      <PopoverSurface className={styles.surface}>
        {MARKER_COLORS.map(({ color, hex, label }) => {
          const isSelected = selectedColor === color;
          return (
            <button
              key={color}
              type="button"
              style={{
                width: '28px',
                height: '28px',
                minWidth: '28px',
                minHeight: '28px',
                padding: 0,
                borderRadius: tokens.borderRadiusSmall,
                cursor: 'pointer',
                border: isSelected
                  ? `2px solid ${tokens.colorBrandStroke1}`
                  : '2px solid transparent',
                backgroundColor: hex,
                transition: 'border-color 100ms ease-out, transform 100ms ease-out',
                boxShadow: isSelected ? tokens.shadow4 : undefined,
              }}
              onClick={() => handleColorSelect(color)}
              aria-label={label}
              aria-pressed={isSelected}
              title={label}
            />
          );
        })}
      </PopoverSurface>
    </Popover>
  );
};

export default MarkerColorPicker;

/** Get hex color for a marker color */
export function getMarkerColorHex(color: MarkerColor): string {
  const colorInfo = MARKER_COLORS.find((c) => c.color === color);
  return colorInfo?.hex || tokens.colorPaletteBlueBorderActive;
}
