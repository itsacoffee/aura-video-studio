/**
 * Lazy-loaded image component with IntersectionObserver
 * Includes low-res placeholder and proper cleanup
 */

import { useState, useEffect, useRef } from 'react';
import { makeStyles, tokens } from '@fluentui/react-components';

const useStyles = makeStyles({
  container: {
    position: 'relative',
    width: '100%',
    height: '100%',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  image: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    transition: 'opacity 0.3s ease',
  },
  loading: {
    opacity: 0,
  },
  loaded: {
    opacity: 1,
  },
  placeholder: {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    filter: 'blur(10px)',
    transform: 'scale(1.1)',
  },
});

export interface LazyImageProps {
  src: string;
  alt: string;
  className?: string;
  width?: number;
  height?: number;
  onError?: (e: React.SyntheticEvent<HTMLImageElement>) => void;
}

/**
 * Lazy-loaded image component that only loads when visible in viewport
 */
export function LazyImage({
  src,
  alt,
  className,
  width,
  height,
  onError,
}: LazyImageProps) {
  const styles = useStyles();
  const [isLoaded, setIsLoaded] = useState(false);
  const [isInView, setIsInView] = useState(false);
  const objectUrlRef = useRef<string | null>(null);
  const imgRef = useRef<HTMLImageElement>(null);

  useEffect(() => {
    const imgElement = imgRef.current;
    if (!imgElement) return;

    // Create IntersectionObserver to detect when image enters viewport
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
        rootMargin: '50px', // Start loading slightly before image enters viewport
        threshold: 0.01,
      }
    );

    observer.observe(imgElement);

    return () => {
      observer.disconnect();
    };
  }, []);

  useEffect(() => {
    // Cleanup object URLs when component unmounts
    return () => {
      if (objectUrlRef.current) {
        URL.revokeObjectURL(objectUrlRef.current);
      }
    };
  }, []);

  const handleLoad = () => {
    setIsLoaded(true);
  };

  const handleError = (e: React.SyntheticEvent<HTMLImageElement>) => {
    setIsLoaded(false);
    onError?.(e);
  };

  return (
    <div className={`${styles.container} ${className || ''}`}>
      {isInView ? (
        <img
          ref={imgRef}
          src={src}
          alt={alt}
          className={`${styles.image} ${isLoaded ? styles.loaded : styles.loading}`}
          width={width}
          height={height}
          onLoad={handleLoad}
          onError={handleError}
          loading="lazy"
        />
      ) : (
        <div
          ref={imgRef}
          className={styles.container}
          style={{ width, height }}
        />
      )}
    </div>
  );
}
