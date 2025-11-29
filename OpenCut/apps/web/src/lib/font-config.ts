// System fonts fallback configuration for offline builds
// This configuration does not require network access to Google Fonts

// Create fallback font objects that mimic the Google Fonts API structure
const createFallbackFont = (fallbacks: string[]) => ({
  className: "",
  style: {
    fontFamily: fallbacks.join(", "),
  },
});

// Fallback fonts for offline builds - use system font stacks
const inter = createFallbackFont([
  "Inter",
  "system-ui",
  "-apple-system",
  "BlinkMacSystemFont",
  "Segoe UI",
  "sans-serif",
]);
const roboto = createFallbackFont([
  "Roboto",
  "system-ui",
  "Arial",
  "sans-serif",
]);
const openSans = createFallbackFont([
  "Open Sans",
  "system-ui",
  "Arial",
  "sans-serif",
]);
const playfairDisplay = createFallbackFont([
  "Playfair Display",
  "Georgia",
  "Times New Roman",
  "serif",
]);
const comicNeue = createFallbackFont([
  "Comic Neue",
  "Comic Sans MS",
  "cursive",
  "sans-serif",
]);

// Export font class mapping for use in components
// These are empty strings since we use inline system fonts
export const FONT_CLASS_MAP = {
  Inter: inter.className,
  Roboto: roboto.className,
  "Open Sans": openSans.className,
  "Playfair Display": playfairDisplay.className,
  "Comic Neue": comicNeue.className,
  Arial: "",
  Helvetica: "",
  "Times New Roman": "",
  Georgia: "",
} as const;

// Export individual fonts for use in layout
export const fonts = {
  inter,
  roboto,
  openSans,
  playfairDisplay,
  comicNeue,
};

// Default font for the body - use system fonts for reliability
export const defaultFont = {
  className: "",
  style: {
    fontFamily:
      'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
  },
};
