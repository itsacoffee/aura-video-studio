/**
 * Matte Preview Component
 * Split-view showing original, matte (alpha channel), and composited result
 */

import { makeStyles, tokens, Label, Button, ToggleButton, Card } from '@fluentui/react-components';
import { Grid24Regular, SplitVertical24Regular } from '@fluentui/react-icons';
import { useEffect, useRef, useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  viewControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  previewContainer: {
    display: 'grid',
    gap: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
  },
  previewSingle: {
    gridTemplateColumns: '1fr',
  },
  previewSplit: {
    gridTemplateColumns: '1fr 1fr 1fr',
  },
  previewItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  canvas: {
    width: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: '#000',
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  label: {
    textAlign: 'center',
    fontWeight: tokens.fontWeightSemibold,
  },
});

type ViewMode = 'single' | 'split';
type DisplayMode = 'composite' | 'matte' | 'original';

interface MattePreviewProps {
  sourceCanvas?: HTMLCanvasElement;
  matteCanvas?: HTMLCanvasElement;
  compositeCanvas?: HTMLCanvasElement;
  backgroundCanvas?: HTMLCanvasElement;
}

export function MattePreview({
  sourceCanvas,
  matteCanvas,
  compositeCanvas,
  backgroundCanvas,
}: MattePreviewProps) {
  const styles = useStyles();

  const [viewMode, setViewMode] = useState<ViewMode>('single');
  const [displayMode, setDisplayMode] = useState<DisplayMode>('composite');

  const originalRef = useRef<HTMLCanvasElement>(null);
  const matteRef = useRef<HTMLCanvasElement>(null);
  const compositeRef = useRef<HTMLCanvasElement>(null);

  // Update canvases when inputs change
  useEffect(() => {
    if (viewMode === 'split') {
      // Update original view
      if (originalRef.current && sourceCanvas) {
        const ctx = originalRef.current.getContext('2d');
        if (ctx) {
          originalRef.current.width = sourceCanvas.width;
          originalRef.current.height = sourceCanvas.height;
          ctx.drawImage(sourceCanvas, 0, 0);
        }
      }

      // Update matte view (alpha channel visualization)
      if (matteRef.current && matteCanvas) {
        const ctx = matteRef.current.getContext('2d');
        if (ctx) {
          matteRef.current.width = matteCanvas.width;
          matteRef.current.height = matteCanvas.height;

          // Draw matte as grayscale based on alpha channel
          ctx.drawImage(matteCanvas, 0, 0);
          const imageData = ctx.getImageData(0, 0, matteRef.current.width, matteRef.current.height);
          const data = imageData.data;

          for (let i = 0; i < data.length; i += 4) {
            const alpha = data[i + 3];
            data[i] = alpha; // R
            data[i + 1] = alpha; // G
            data[i + 2] = alpha; // B
            data[i + 3] = 255; // Full opacity
          }

          ctx.putImageData(imageData, 0, 0);
        }
      }

      // Update composite view
      if (compositeRef.current && compositeCanvas && backgroundCanvas) {
        const ctx = compositeRef.current.getContext('2d');
        if (ctx) {
          compositeRef.current.width = compositeCanvas.width;
          compositeRef.current.height = compositeCanvas.height;

          // Draw background first
          ctx.drawImage(backgroundCanvas, 0, 0);

          // Then draw keyed foreground on top
          ctx.drawImage(compositeCanvas, 0, 0);
        }
      } else if (compositeRef.current && compositeCanvas) {
        const ctx = compositeRef.current.getContext('2d');
        if (ctx) {
          compositeRef.current.width = compositeCanvas.width;
          compositeRef.current.height = compositeCanvas.height;
          ctx.drawImage(compositeCanvas, 0, 0);
        }
      }
    } else {
      // Single view mode - show selected view
      const targetRef =
        displayMode === 'original'
          ? originalRef
          : displayMode === 'matte'
            ? matteRef
            : compositeRef;
      const targetCanvas =
        displayMode === 'original'
          ? sourceCanvas
          : displayMode === 'matte'
            ? matteCanvas
            : compositeCanvas;

      if (targetRef.current && targetCanvas) {
        const ctx = targetRef.current.getContext('2d');
        if (ctx) {
          targetRef.current.width = targetCanvas.width;
          targetRef.current.height = targetCanvas.height;

          if (displayMode === 'matte') {
            // Visualize alpha channel
            ctx.drawImage(targetCanvas, 0, 0);
            const imageData = ctx.getImageData(
              0,
              0,
              targetRef.current.width,
              targetRef.current.height
            );
            const data = imageData.data;

            for (let i = 0; i < data.length; i += 4) {
              const alpha = data[i + 3];
              data[i] = alpha;
              data[i + 1] = alpha;
              data[i + 2] = alpha;
              data[i + 3] = 255;
            }

            ctx.putImageData(imageData, 0, 0);
          } else if (displayMode === 'composite' && backgroundCanvas) {
            // Composite with background
            ctx.drawImage(backgroundCanvas, 0, 0);
            ctx.drawImage(targetCanvas, 0, 0);
          } else {
            ctx.drawImage(targetCanvas, 0, 0);
          }
        }
      }
    }
  }, [viewMode, displayMode, sourceCanvas, matteCanvas, compositeCanvas, backgroundCanvas]);

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Label size="large" weight="semibold">
          Preview
        </Label>
        <div className={styles.viewControls}>
          <ToggleButton
            icon={<Grid24Regular />}
            checked={viewMode === 'single'}
            onClick={() => setViewMode('single')}
          >
            Single
          </ToggleButton>
          <ToggleButton
            icon={<SplitVertical24Regular />}
            checked={viewMode === 'split'}
            onClick={() => setViewMode('split')}
          >
            Split
          </ToggleButton>
        </div>
      </div>

      {viewMode === 'single' && (
        <div className={styles.viewControls}>
          <Button
            size="small"
            appearance={displayMode === 'original' ? 'primary' : 'secondary'}
            onClick={() => setDisplayMode('original')}
          >
            Original
          </Button>
          <Button
            size="small"
            appearance={displayMode === 'matte' ? 'primary' : 'secondary'}
            onClick={() => setDisplayMode('matte')}
          >
            Matte
          </Button>
          <Button
            size="small"
            appearance={displayMode === 'composite' ? 'primary' : 'secondary'}
            onClick={() => setDisplayMode('composite')}
          >
            Composite
          </Button>
        </div>
      )}

      <div
        className={`${styles.previewContainer} ${viewMode === 'split' ? styles.previewSplit : styles.previewSingle}`}
      >
        {viewMode === 'split' ? (
          <>
            <div className={styles.previewItem}>
              <canvas ref={originalRef} className={styles.canvas} />
              <Label className={styles.label}>Original</Label>
            </div>
            <div className={styles.previewItem}>
              <canvas ref={matteRef} className={styles.canvas} />
              <Label className={styles.label}>Matte</Label>
            </div>
            <div className={styles.previewItem}>
              <canvas ref={compositeRef} className={styles.canvas} />
              <Label className={styles.label}>Composite</Label>
            </div>
          </>
        ) : (
          <div className={styles.previewItem}>
            <canvas
              ref={
                displayMode === 'original'
                  ? originalRef
                  : displayMode === 'matte'
                    ? matteRef
                    : compositeRef
              }
              className={styles.canvas}
            />
            <Label className={styles.label}>
              {displayMode === 'original'
                ? 'Original'
                : displayMode === 'matte'
                  ? 'Matte'
                  : 'Composite'}
            </Label>
          </div>
        )}
      </div>
    </Card>
  );
}
