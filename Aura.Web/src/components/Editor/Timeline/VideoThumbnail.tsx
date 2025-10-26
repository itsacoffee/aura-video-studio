/**
 * Video thumbnail component for timeline clips
 * Uses FFmpeg to extract thumbnails from video files
 */

import { FFmpeg } from '@ffmpeg/ffmpeg';
import { fetchFile, toBlobURL } from '@ffmpeg/util';
import { makeStyles, tokens, Spinner } from '@fluentui/react-components';
import { useEffect, useRef, useState } from 'react';

const useStyles = makeStyles({
  container: {
    position: 'relative',
    width: '100%',
    height: '100%',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
  thumbnail: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    display: 'block',
  },
  loadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
  },
  errorMessage: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    textAlign: 'center',
    padding: tokens.spacingHorizontalS,
  },
  placeholder: {
    width: '100%',
    height: '100%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground4,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
  },
});

export interface VideoThumbnailProps {
  videoPath?: string;
  timestamp?: number; // Time in seconds to extract thumbnail from
  width?: number;
  height?: number;
}

export function VideoThumbnail({
  videoPath,
  timestamp = 1,
  width = 160,
  height = 90,
}: VideoThumbnailProps) {
  const styles = useStyles();
  const [thumbnailUrl, setThumbnailUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const ffmpegRef = useRef<FFmpeg | null>(null);
  const [ffmpegLoaded, setFfmpegLoaded] = useState(false);

  // Initialize FFmpeg
  useEffect(() => {
    const loadFFmpeg = async () => {
      try {
        const ffmpeg = new FFmpeg();
        ffmpegRef.current = ffmpeg;

        // Load FFmpeg core - using 0.12.4 for compatibility with @ffmpeg/ffmpeg 0.12.10
        const baseURL = 'https://unpkg.com/@ffmpeg/core@0.12.4/dist/umd';
        await ffmpeg.load({
          coreURL: await toBlobURL(`${baseURL}/ffmpeg-core.js`, 'text/javascript'),
          wasmURL: await toBlobURL(`${baseURL}/ffmpeg-core.wasm`, 'application/wasm'),
        });

        setFfmpegLoaded(true);
      } catch (err) {
        console.error('Failed to load FFmpeg:', err);
        setError('Failed to initialize video processor');
      }
    };

    loadFFmpeg();

    return () => {
      // Cleanup
      if (thumbnailUrl) {
        URL.revokeObjectURL(thumbnailUrl);
      }
    };
  }, []);

  // Extract thumbnail from video
  useEffect(() => {
    if (!videoPath || !ffmpegLoaded || !ffmpegRef.current) return;

    const extractThumbnail = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const ffmpeg = ffmpegRef.current!;

        // Load video file
        await ffmpeg.writeFile('input.mp4', await fetchFile(videoPath));

        // Extract thumbnail at specified timestamp
        await ffmpeg.exec([
          '-i',
          'input.mp4',
          '-ss',
          timestamp.toString(),
          '-vframes',
          '1',
          '-vf',
          `scale=${width}:${height}`,
          'thumbnail.jpg',
        ]);

        // Read the thumbnail file
        const data = await ffmpeg.readFile('thumbnail.jpg');
        const blob = new Blob([data], { type: 'image/jpeg' });
        const url = URL.createObjectURL(blob);

        // Cleanup FFmpeg files
        await ffmpeg.deleteFile('input.mp4');
        await ffmpeg.deleteFile('thumbnail.jpg');

        // Update thumbnail URL
        if (thumbnailUrl) {
          URL.revokeObjectURL(thumbnailUrl);
        }
        setThumbnailUrl(url);
      } catch (err) {
        console.error('Failed to extract thumbnail:', err);
        setError('Failed to load thumbnail');
      } finally {
        setIsLoading(false);
      }
    };

    extractThumbnail();
  }, [videoPath, timestamp, width, height, ffmpegLoaded]);

  if (!videoPath) {
    return (
      <div className={styles.container}>
        <div className={styles.placeholder}>No Video</div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {thumbnailUrl && !isLoading && !error && (
        <img src={thumbnailUrl} alt="Video thumbnail" className={styles.thumbnail} />
      )}
      {isLoading && (
        <div className={styles.loadingOverlay}>
          <Spinner size="small" label="Loading thumbnail..." />
        </div>
      )}
      {error && !isLoading && <div className={styles.errorMessage}>{error}</div>}
      {!thumbnailUrl && !isLoading && !error && <div className={styles.placeholder}>📹</div>}
    </div>
  );
}
