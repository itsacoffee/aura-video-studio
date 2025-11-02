/**
 * Lazy loading image component with IntersectionObserver
 * Prevents loading images until they're near viewport
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { useState, useEffect, useRef, CSSProperties } from 'react';

const useStyles = makeStyles({
  container: {
    position: 'relative',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  image: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    transition: 'opacity 0.3s ease',
  },
  placeholder: {
    width: '100%',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground3,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: tokens.colorNeutralForeground3,
  },
  loading: {
    opacity: 0,
  },
  loaded: {
    opacity: 1,
  },
});

export interface LazyImageProps {
  src: string;
  alt: string;
  width?: number | string;
  height?: number | string;
  placeholderText?: string;
  className?: string;
  style?: CSSProperties;
  onLoad?: () => void;
  onError?: (e: React.SyntheticEvent<HTMLImageElement>) => void;
}

export function LazyImage({
  src,
  alt,
  width,
  height,
  placeholderText,
  className,
  style,
  onLoad,
  onError,
}: LazyImageProps) {
  const styles = useStyles();
  const [isLoaded, setIsLoaded] = useState(false);
  const [isInView, setIsInView] = useState(false);
  const [hasError, setHasError] = useState(false);
  const imgRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsInView(true);
            observer.disconnect();
          }
        });
      },
      {
        rootMargin: '100px', // Start loading 100px before entering viewport
        threshold: 0.01,
      }
    );

    observer.observe(container);

    return () => {
      observer.disconnect();
    };
  }, []);

  const handleLoad = () => {
    setIsLoaded(true);
    onLoad?.();
  };

  const handleError = (e: React.SyntheticEvent<HTMLImageElement>) => {
    setHasError(true);
    onError?.(e);
  };

  const containerStyle: CSSProperties = {
    ...style,
    width: width || '100%',
    height: height || '100%',
  };

  return (
    <div
      ref={containerRef}
      className={`${styles.container} ${className || ''}`}
      style={containerStyle}
    >
      {!isInView || hasError ? (
        <div className={styles.placeholder}>
          {hasError ? 'Failed to load' : placeholderText || 'Loading...'}
        </div>
      ) : (
        <img
          ref={imgRef}
          src={src}
          alt={alt}
          className={`${styles.image} ${isLoaded ? styles.loaded : styles.loading}`}
          onLoad={handleLoad}
          onError={handleError}
          loading="lazy"
          decoding="async"
          style={{
            width: width || '100%',
            height: height || '100%',
          }}
        />
      )}
    </div>
  );
}
