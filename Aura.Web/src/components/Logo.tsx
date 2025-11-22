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
      // Base paths to try
      const sizes = [
        { max: 16, paths: ['/favicon-16x16.png', './favicon-16x16.png', 'favicon-16x16.png'] },
        { max: 32, paths: ['/favicon-32x32.png', './favicon-32x32.png', 'favicon-32x32.png'] },
        { max: 128, paths: ['/logo256.png', './logo256.png', 'logo256.png'] },
        { max: Infinity, paths: ['/logo512.png', './logo512.png', 'logo512.png'] },
      ];

      const sizeConfig = sizes.find((s) => requestedSize <= s.max);
      if (!sizeConfig) return '/logo512.png';

      // In Electron with file:// protocol, try relative paths first
      const isElectron = window.navigator.userAgent.toLowerCase().includes('electron');
      const isFileProtocol = window.location.protocol === 'file:';

      if (isElectron || isFileProtocol) {
        // Try relative path without leading slash first for file:// protocol
        return sizeConfig.paths[1] || sizeConfig.paths[0];
      }

      // For web/http, use absolute path
      return sizeConfig.paths[0];
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
        <div className={styles.fallback} style={{ fontSize: `${size * 0.5}px` }}>
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
