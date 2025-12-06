/**
 * LowerThirdRenderer Component
 *
 * Specialized renderer for lower third graphics with
 * optimized name/title text display and animated entry/exit sequences.
 */

import { makeStyles } from '@fluentui/react-components';
import { useMemo } from 'react';
import type { FC } from 'react';
import type {
  MotionGraphicAsset,
  AppliedGraphic,
  AnimationState,
} from '../../../types/motionGraphics';
import { evaluateEasing } from '../../../utils/motionGraphicsAnimation';

export interface LowerThirdRendererProps {
  /** The motion graphic asset definition */
  asset: MotionGraphicAsset;
  /** The applied graphic instance with customizations */
  graphic: AppliedGraphic;
  /** Current animation state */
  animationState: AnimationState;
  /** Canvas width */
  width: number;
  /** Canvas height */
  height: number;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '12px 16px',
    borderRadius: '8px',
    maxWidth: '400px',
    minWidth: '200px',
  },
  minimal: {
    backgroundColor: 'transparent',
    borderLeft: '3px solid',
    paddingLeft: '12px',
  },
  glass: {
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
    backdropFilter: 'blur(20px)',
    borderLeft: '4px solid',
  },
  broadcast: {
    backgroundColor: '#1E3A8A',
    borderTop: '3px solid #EF4444',
    borderRadius: 0,
    padding: '8px 16px',
  },
  cinematic: {
    backgroundColor: 'transparent',
    textAlign: 'center',
    alignItems: 'center',
  },
  tech: {
    backgroundColor: 'rgba(0, 0, 0, 0.85)',
    border: '1px solid rgba(255, 255, 255, 0.1)',
    borderRadius: '12px',
  },
  name: {
    fontSize: '24px',
    fontWeight: 600,
    color: '#FFFFFF',
    margin: 0,
    lineHeight: 1.2,
  },
  title: {
    fontSize: '14px',
    fontWeight: 400,
    color: 'rgba(255, 255, 255, 0.8)',
    margin: 0,
    lineHeight: 1.3,
  },
  divider: {
    width: '60px',
    height: '1px',
    backgroundColor: 'rgba(255, 255, 255, 0.4)',
    margin: '8px 0',
  },
});

/**
 * Get the variant style class based on asset ID
 */
function getVariantStyle(
  assetId: string,
  styles: ReturnType<typeof useStyles>
): string | undefined {
  if (assetId.includes('minimal')) return styles.minimal;
  if (assetId.includes('glass')) return styles.glass;
  if (assetId.includes('broadcast') || assetId.includes('news')) return styles.broadcast;
  if (assetId.includes('cinematic') || assetId.includes('elegant')) return styles.cinematic;
  if (assetId.includes('tech')) return styles.tech;
  return styles.minimal;
}

export const LowerThirdRenderer: FC<LowerThirdRendererProps> = ({
  asset,
  graphic,
  animationState,
}) => {
  const styles = useStyles();

  // Get customized values
  const name = String(graphic.customValues['name'] ?? 'Name');
  const title = String(graphic.customValues['title'] ?? graphic.customValues['subtitle'] ?? '');
  const accentColor = String(graphic.customValues['accentColor'] ?? '#3B82F6');
  const textColor = String(graphic.customValues['textColor'] ?? '#FFFFFF');

  // Calculate animation transforms
  const eased = evaluateEasing('easeOutCubic', animationState.progress);

  const containerStyle: React.CSSProperties = useMemo(() => {
    let transform = '';
    let opacity = 1;

    if (animationState.phase === 'entry') {
      // Slide in from left
      transform = `translateX(${(1 - eased) * -30}px)`;
      opacity = eased;
    } else if (animationState.phase === 'exit') {
      // Slide out to left
      transform = `translateX(${(1 - animationState.progress) * -30}px)`;
      opacity = animationState.progress;
    }

    return {
      transform,
      opacity,
      borderColor: accentColor,
    };
  }, [animationState, eased, accentColor]);

  const variantClass = getVariantStyle(asset.id, styles);
  const isCinematic = asset.id.includes('cinematic') || asset.id.includes('elegant');

  return (
    <div className={`${styles.container} ${variantClass}`} style={containerStyle}>
      <p className={styles.name} style={{ color: textColor }}>
        {name}
      </p>

      {isCinematic && <div className={styles.divider} />}

      {title && (
        <p
          className={styles.title}
          style={{
            color: asset.id.includes('tech') ? accentColor : `${textColor}CC`, // 80% opacity
          }}
        >
          {title}
        </p>
      )}
    </div>
  );
};

export default LowerThirdRenderer;
