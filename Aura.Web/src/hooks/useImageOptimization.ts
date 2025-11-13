import { useEffect, useRef, useState, useCallback } from 'react';

/**
 * Image cache to prevent re-fetching images
 */
const imageCache = new Map<string, HTMLImageElement>();
const MAX_CACHE_SIZE = 100;

/**
 * LRU (Least Recently Used) cache for images
 */
class LRUImageCache {
  private cache = new Map<string, { image: HTMLImageElement; timestamp: number }>();
  private maxSize: number;

  constructor(maxSize = MAX_CACHE_SIZE) {
    this.maxSize = maxSize;
  }

  set(key: string, image: HTMLImageElement): void {
    if (this.cache.size >= this.maxSize) {
      const oldestKey = Array.from(this.cache.entries()).sort(
        (a, b) => a[1].timestamp - b[1].timestamp
      )[0][0];
      this.cache.delete(oldestKey);
    }

    this.cache.set(key, { image, timestamp: Date.now() });
  }

  get(key: string): HTMLImageElement | undefined {
    const entry = this.cache.get(key);
    if (entry) {
      entry.timestamp = Date.now();
      return entry.image;
    }
    return undefined;
  }

  has(key: string): boolean {
    return this.cache.has(key);
  }

  clear(): void {
    this.cache.clear();
  }

  size(): number {
    return this.cache.size;
  }
}

const lruCache = new LRUImageCache();

/**
 * Hook for lazy loading images with caching
 */
export function useLazyLoadImage(src: string, rootMargin = '50px') {
  const [isLoaded, setIsLoaded] = useState(false);
  const [isInView, setIsInView] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const imgRef = useRef<HTMLImageElement>(null);

  useEffect(() => {
    if (!imgRef.current) return;

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
        rootMargin,
      }
    );

    observer.observe(imgRef.current);

    return () => {
      observer.disconnect();
    };
  }, [rootMargin]);

  useEffect(() => {
    if (!isInView || !src) return;

    if (lruCache.has(src)) {
      setIsLoaded(true);
      return;
    }

    const img = new Image();

    img.onload = () => {
      lruCache.set(src, img);
      setIsLoaded(true);
      setError(null);
    };

    img.onerror = () => {
      setError(new Error(`Failed to load image: ${src}`));
      setIsLoaded(false);
    };

    img.src = src;

    return () => {
      img.onload = null;
      img.onerror = null;
    };
  }, [src, isInView]);

  return {
    imgRef,
    isLoaded,
    isInView,
    error,
  };
}

/**
 * Hook for preloading images
 */
export function useImagePreloader(srcs: string[]) {
  const [loadedCount, setLoadedCount] = useState(0);
  const [isComplete, setIsComplete] = useState(false);

  useEffect(() => {
    let mounted = true;
    let completed = 0;

    const loadImage = (src: string) => {
      return new Promise<void>((resolve) => {
        if (lruCache.has(src)) {
          if (mounted) {
            completed++;
            setLoadedCount(completed);
          }
          resolve();
          return;
        }

        const img = new Image();

        img.onload = () => {
          lruCache.set(src, img);
          if (mounted) {
            completed++;
            setLoadedCount(completed);
          }
          resolve();
        };

        img.onerror = () => {
          if (mounted) {
            completed++;
            setLoadedCount(completed);
          }
          resolve();
        };

        img.src = src;
      });
    };

    Promise.all(srcs.map(loadImage)).then(() => {
      if (mounted) {
        setIsComplete(true);
      }
    });

    return () => {
      mounted = false;
    };
  }, [srcs]);

  return {
    loadedCount,
    totalCount: srcs.length,
    isComplete,
    progress: srcs.length > 0 ? (loadedCount / srcs.length) * 100 : 0,
  };
}

/**
 * Hook for progressive image loading (blur-up technique)
 */
export function useProgressiveImage(lowQualitySrc: string, highQualitySrc: string) {
  const [currentSrc, setCurrentSrc] = useState(lowQualitySrc);
  const [isHighQualityLoaded, setIsHighQualityLoaded] = useState(false);

  useEffect(() => {
    setCurrentSrc(lowQualitySrc);
    setIsHighQualityLoaded(false);

    if (lruCache.has(highQualitySrc)) {
      setCurrentSrc(highQualitySrc);
      setIsHighQualityLoaded(true);
      return;
    }

    const img = new Image();

    img.onload = () => {
      lruCache.set(highQualitySrc, img);
      setCurrentSrc(highQualitySrc);
      setIsHighQualityLoaded(true);
    };

    img.src = highQualitySrc;

    return () => {
      img.onload = null;
    };
  }, [lowQualitySrc, highQualitySrc]);

  return {
    currentSrc,
    isHighQualityLoaded,
  };
}

/**
 * Clear the image cache
 */
export function clearImageCache() {
  lruCache.clear();
}

/**
 * Get cache statistics
 */
export function getImageCacheStats() {
  return {
    size: lruCache.size(),
    maxSize: MAX_CACHE_SIZE,
  };
}

/**
 * Preload a single image
 */
export function preloadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    if (lruCache.has(src)) {
      const cached = lruCache.get(src);
      if (cached) {
        resolve(cached);
        return;
      }
    }

    const img = new Image();

    img.onload = () => {
      lruCache.set(src, img);
      resolve(img);
    };

    img.onerror = () => {
      reject(new Error(`Failed to load image: ${src}`));
    };

    img.src = src;
  });
}
