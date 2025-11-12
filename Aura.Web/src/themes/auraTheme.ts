import {
  webDarkTheme,
  webLightTheme,
  type Theme,
  type BrandVariants,
  createDarkTheme,
  createLightTheme,
} from '@fluentui/react-components';

/**
 * Aura brand colors derived from the app icon
 * Blue (#00D4FF) to Orange (#FF6B35) gradient with cyan accent (#0EA5E9)
 */
const auraBrandColors: BrandVariants = {
  10: '#020305',
  20: '#0C1823',
  30: '#152B40',
  40: '#1D3E5E',
  50: '#26527C',
  60: '#2E669A',
  70: '#3C7DB5',
  80: '#4E94CC',
  90: '#66ACE1',
  100: '#82C3F0',
  110: '#9FD9FF',
  120: '#BCE8FF',
  130: '#D6F3FF',
  140: '#EDFAFF',
  150: '#F7FDFF',
  160: '#FFFFFF',
};

/**
 * Aura Dark Theme - Inspired by the app icon's dark background
 * with blue-to-orange gradient accent colors
 */
export const auraDarkTheme: Theme = {
  ...createDarkTheme(auraBrandColors),
  colorNeutralBackground1: '#0F1419', // Dark background similar to icon
  colorNeutralBackground2: '#1A1F26',
  colorNeutralBackground3: '#252A31',
  colorBrandBackground: '#00D4FF', // Cyan blue from icon
  colorBrandBackgroundHover: '#0EA5E9', // Sky blue
  colorBrandBackgroundPressed: '#0284C7',
  colorBrandForeground1: '#00D4FF',
  colorBrandForeground2: '#0EA5E9',
  colorCompoundBrandBackground: 'linear-gradient(90deg, #00D4FF 0%, #FF6B35 100%)',
  colorCompoundBrandBackgroundHover: 'linear-gradient(90deg, #0EA5E9 0%, #FF8960 100%)',
  colorCompoundBrandBackgroundPressed: 'linear-gradient(90deg, #0284C7 0%, #E85D2F 100%)',
};

/**
 * Aura Light Theme - Light variant with icon-inspired colors
 */
export const auraLightTheme: Theme = {
  ...createLightTheme(auraBrandColors),
  colorBrandBackground: '#0EA5E9', // Sky blue (more suitable for light mode)
  colorBrandBackgroundHover: '#0284C7',
  colorBrandBackgroundPressed: '#0369A1',
  colorBrandForeground1: '#0EA5E9',
  colorBrandForeground2: '#0284C7',
  colorCompoundBrandBackground: 'linear-gradient(90deg, #00D4FF 0%, #FF6B35 100%)',
  colorCompoundBrandBackgroundHover: 'linear-gradient(90deg, #0EA5E9 0%, #FF8960 100%)',
  colorCompoundBrandBackgroundPressed: 'linear-gradient(90deg, #0284C7 0%, #E85D2F 100%)',
};

/**
 * Get the appropriate Aura theme based on dark mode preference
 */
export function getAuraTheme(isDark: boolean): Theme {
  return isDark ? auraDarkTheme : auraLightTheme;
}

/**
 * Theme selection helper
 */
export function getTheme(themeName: string, isDark: boolean): Theme {
  switch (themeName) {
    case 'aura':
      return getAuraTheme(isDark);
    case 'default':
    default:
      return isDark ? webDarkTheme : webLightTheme;
  }
}
