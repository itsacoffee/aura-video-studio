import { makeStyles } from '@fluentui/react-components';
import { memo, useState, useEffect } from 'react';

const useStyles = makeStyles({
  logo: {
    display: 'inline-block',
  },
  image: {
    display: 'block',
    width: '100%',
    height: '100%',
    objectFit: 'contain',
  },
  fallback: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
    height: '100%',
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '12px',
    color: 'white',
    fontWeight: 'bold',
    fontSize: '24px',
  },
});

// Constants
const ELECTRON_USER_AGENT_MARKER = 'electron';
const FALLBACK_FONT_SIZE_RATIO = 0.5;

export interface LogoProps {
  /**
   * Size of the logo in pixels
   */
  size?: number;
  /**
   * Additional CSS class name
   */
  className?: string;
  /**
   * Alt text for the logo image
   */
  alt?: string;
}

/**
 * Logo component that displays the Aura Video Studio icon.
 * Handles both web and Electron (file://) protocols with fallback.
 */
export const Logo = memo<LogoProps>(({ size = 64, className, alt = 'Aura Video Studio' }) => {
  const styles = useStyles();
  const [imageError, setImageError] = useState(false);
  const [imagePath, setImagePath] = useState<string>('');

  useEffect(() => {
    // Determine the correct image path based on environment
    const getIconPath = (requestedSize: number): string => {
      // Get base URL from Vite (handles both dev and production)
      const baseUrl = import.meta.env.BASE_URL || './';
      
      // Map sizes to appropriate icon files
      const sizeMap: Array<{ max: number; file: string }> = [
        { max: 16, file: 'favicon-16x16.png' },
        { max: 32, file: 'favicon-32x32.png' },
        { max: 128, file: 'logo256.png' },
        { max: Infinity, file: 'logo512.png' },
      ];

      const sizeConfig = sizeMap.find((s) => requestedSize <= s.max);
      if (!sizeConfig) {
        return `${baseUrl}logo512.png`;
      }

      // In Electron with file:// protocol, use relative paths
      const isElectron = window.navigator.userAgent
        .toLowerCase()
        .includes(ELECTRON_USER_AGENT_MARKER);
      const isFileProtocol = window.location.protocol === 'file:';

      if (isElectron || isFileProtocol) {
        // For file:// protocol, use relative path without base URL prefix
        return `./${sizeConfig.file}`;
      }

      // For web/http, use base URL (which handles both / and ./)
      return `${baseUrl}${sizeConfig.file}`;
    };

    setImagePath(getIconPath(size));
    setImageError(false);
  }, [size]);

  const handleImageError = () => {
    console.warn(`[Logo] Failed to load image from ${imagePath}, showing fallback`);
    setImageError(true);
  };

  if (imageError) {
    // Fallback to gradient with "A" text
    return (
      <span className={className} style={{ width: size, height: size }}>
        <div
          className={styles.fallback}
          style={{ fontSize: `${size * FALLBACK_FONT_SIZE_RATIO}px` }}
        >
          A
        </div>
      </span>
    );
  }

  return (
    <span className={className} style={{ width: size, height: size }}>
      <img
        src={imagePath}
        alt={alt}
        className={styles.image}
        width={size}
        height={size}
        draggable={false}
        onError={handleImageError}
        loading="eager"
      />
    </span>
  );
});

Logo.displayName = 'Logo';
