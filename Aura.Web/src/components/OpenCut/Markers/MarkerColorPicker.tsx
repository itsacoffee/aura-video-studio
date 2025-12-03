/**
 * MarkerColorPicker Component
 *
 * A color picker for selecting marker colors.
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import type { FC } from 'react';
import type { MarkerColor } from '../../../types/opencut';

export interface MarkerColorPickerProps {
  selectedColor: MarkerColor;
  onChange: (color: MarkerColor) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
  },
  colorButton: {
    width: '24px',
    height: '24px',
    borderRadius: '50%',
    border: '2px solid transparent',
    cursor: 'pointer',
    transition: 'transform 0.1s ease, border-color 0.1s ease',
    ':hover': {
      transform: 'scale(1.1)',
    },
  },
  selected: {
    border: `2px solid ${tokens.colorNeutralForeground1}`,
    transform: 'scale(1.1)',
  },
});

const COLOR_MAP: Record<MarkerColor, string> = {
  red: '#EF4444',
  orange: '#F97316',
  yellow: '#EAB308',
  green: '#22C55E',
  blue: '#3B82F6',
  purple: '#A855F7',
  pink: '#EC4899',
};

const COLORS: MarkerColor[] = ['red', 'orange', 'yellow', 'green', 'blue', 'purple', 'pink'];

export const MarkerColorPicker: FC<MarkerColorPickerProps> = ({ selectedColor, onChange }) => {
  const styles = useStyles();

  return (
    <div className={styles.container} role="radiogroup" aria-label="Marker color">
      {COLORS.map((color) => (
        <button
          key={color}
          type="button"
          className={mergeClasses(styles.colorButton, selectedColor === color && styles.selected)}
          style={{ backgroundColor: COLOR_MAP[color] }}
          onClick={() => onChange(color)}
          aria-label={`${color} color`}
          aria-checked={selectedColor === color}
          role="radio"
        />
      ))}
    </div>
  );
};

export default MarkerColorPicker;
