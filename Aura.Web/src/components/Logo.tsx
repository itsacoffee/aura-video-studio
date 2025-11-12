import { makeStyles } from '@fluentui/react-components';
import { memo } from 'react';

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
 * Uses the actual icon image from the public folder instead of emoji.
 */
export const Logo = memo<LogoProps>(({ size = 64, className, alt = 'Aura Video Studio' }) => {
  const styles = useStyles();

  // Use the appropriate size based on requested size
  // We have: 16x16, 32x32, 64x64, 128x128, 256x256, 512x512
  const getIconSize = (requestedSize: number): string => {
    if (requestedSize <= 16) return '/favicon-16x16.png';
    if (requestedSize <= 32) return '/favicon-32x32.png';
    if (requestedSize <= 128) return '/logo256.png';
    return '/logo512.png';
  };

  const iconPath = getIconSize(size);

  return (
    <span className={className} style={{ width: size, height: size }}>
      <img
        src={iconPath}
        alt={alt}
        className={styles.image}
        width={size}
        height={size}
        draggable={false}
      />
    </span>
  );
});

Logo.displayName = 'Logo';
