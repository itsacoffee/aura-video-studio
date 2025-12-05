/**
 * AnimatedTextRenderer Component
 *
 * CSS-based renderer for text animations including typewriter with cursor,
 * letter-by-letter effects, word highlighting, karaoke fill effect,
 * and glitch distortion. Uses CSS animations and keyframes for smooth
 * performance.
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { useMemo, useState, useEffect } from 'react';
import type { FC, CSSProperties } from 'react';
import {
  useTextAnimationsStore,
  type AppliedTextAnimation,
  type TextAnimationType,
} from '../../../stores/opencutTextAnimations';

export interface AnimatedTextRendererProps {
  /** The text content to render */
  text: string;
  /** Target clip or caption ID for animation lookup */
  targetId: string;
  /** Current playback time in seconds (relative to clip start) */
  currentTime: number;
  /** Duration of the clip or caption in seconds */
  duration: number;
  /** Base text styles */
  style?: CSSProperties;
  /** CSS class name */
  className?: string;
  /** Whether animation is currently playing */
  isPlaying?: boolean;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    display: 'inline-block',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  word: {
    display: 'inline-block',
    whiteSpace: 'pre',
  },
  letter: {
    display: 'inline-block',
  },
  cursor: {
    display: 'inline-block',
    marginLeft: '2px',
    animation: 'blink 0.7s step-end infinite',
  },
  '@keyframes blink': {
    '50%': { opacity: 0 },
  },
  highlight: {
    backgroundColor: 'currentColor',
    transition: 'background-color 0.1s ease',
  },
});

// CSS keyframes for animations (injected once)
const injectKeyframes = () => {
  const styleId = 'animated-text-keyframes';
  if (document.getElementById(styleId)) return;

  const style = document.createElement('style');
  style.id = styleId;
  style.textContent = `
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes fadeOut {
      from { opacity: 1; }
      to { opacity: 0; }
    }
    @keyframes slideUp {
      from { transform: translateY(var(--slide-distance, 50px)); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
    @keyframes slideDown {
      from { transform: translateY(calc(-1 * var(--slide-distance, 50px))); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
    @keyframes slideLeft {
      from { transform: translateX(var(--slide-distance, 50px)); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
    @keyframes slideRight {
      from { transform: translateX(calc(-1 * var(--slide-distance, 50px))); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
    @keyframes scaleIn {
      from { transform: scale(var(--start-scale, 0)); opacity: 0; }
      to { transform: scale(1); opacity: 1; }
    }
    @keyframes scaleOut {
      from { transform: scale(1); opacity: 1; }
      to { transform: scale(var(--end-scale, 0)); opacity: 0; }
    }
    @keyframes bounceIn {
      0% { transform: scale(0); opacity: 0; }
      50% { transform: scale(1.1); }
      70% { transform: scale(0.95); }
      100% { transform: scale(1); opacity: 1; }
    }
    @keyframes bounceLetter {
      0% { transform: translateY(0); }
      40% { transform: translateY(-10px); }
      60% { transform: translateY(-5px); }
      80% { transform: translateY(-2px); }
      100% { transform: translateY(0); }
    }
    @keyframes blurIn {
      from { filter: blur(var(--start-blur, 10px)); opacity: 0; }
      to { filter: blur(0); opacity: 1; }
    }
    @keyframes rotateIn {
      from { transform: rotate(var(--start-rotation, -90deg)); opacity: 0; }
      to { transform: rotate(0); opacity: 1; }
    }
    @keyframes wave {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(calc(-1 * var(--wave-amplitude, 10px))); }
    }
    @keyframes rainbow {
      0% { color: #ff0000; }
      17% { color: #ff8000; }
      33% { color: #ffff00; }
      50% { color: #00ff00; }
      67% { color: #0080ff; }
      83% { color: #8000ff; }
      100% { color: #ff0000; }
    }
    @keyframes glitch {
      0%, 100% { transform: translate(0); }
      20% { transform: translate(-2px, 2px); }
      40% { transform: translate(-2px, -2px); }
      60% { transform: translate(2px, 2px); }
      80% { transform: translate(2px, -2px); }
    }
    @keyframes blink {
      50% { opacity: 0; }
    }
  `;
  document.head.appendChild(style);
};

export const AnimatedTextRenderer: FC<AnimatedTextRendererProps> = ({
  text,
  targetId,
  currentTime,
  duration,
  style,
  className,
  isPlaying = false,
}) => {
  const styles = useStyles();
  const textAnimationsStore = useTextAnimationsStore();
  const [typewriterIndex, setTypewriterIndex] = useState(0);

  // Inject keyframes on mount
  useEffect(() => {
    injectKeyframes();
  }, []);

  // Get all animations for this target
  const animations = textAnimationsStore.getAnimationsForTarget(targetId);

  // Find active animations based on current time
  const activeAnimations = useMemo(() => {
    return animations.filter((anim) => {
      const animStart = anim.delay;
      const animEnd = anim.delay + anim.duration;

      if (anim.position === 'in') {
        return currentTime >= animStart && currentTime <= animEnd;
      }
      if (anim.position === 'out') {
        const outStart = duration - anim.duration - anim.delay;
        return currentTime >= outStart && currentTime <= duration;
      }
      if (anim.position === 'continuous') {
        return currentTime >= animStart;
      }
      return false;
    });
  }, [animations, currentTime, duration]);

  // Handle typewriter animation
  const inAnimation = animations.find((a) => a.position === 'in');
  const isTypewriter =
    inAnimation &&
    (textAnimationsStore.getPreset(inAnimation.presetId)?.type === 'typewriter' ||
      textAnimationsStore.getPreset(inAnimation.presetId)?.type === 'typewriter-cursor');

  useEffect(() => {
    if (!isTypewriter || !isPlaying || !inAnimation) {
      if (typewriterIndex !== text.length) {
        setTypewriterIndex(text.length);
      }
      return;
    }

    const animProgress = Math.min(
      1,
      Math.max(0, (currentTime - inAnimation.delay) / inAnimation.duration)
    );
    const targetIndex = Math.floor(animProgress * text.length);
    setTypewriterIndex(targetIndex);
  }, [isTypewriter, isPlaying, currentTime, inAnimation, text.length, typewriterIndex]);

  // Calculate animation styles
  const getAnimationStyle = (anim: AppliedTextAnimation): CSSProperties => {
    const preset = textAnimationsStore.getPreset(anim.presetId);
    if (!preset) return {};

    const animationName = getAnimationName(preset.type);
    if (!animationName) return {};

    const progress = getAnimationProgress(anim, currentTime, duration);
    if (progress < 0 || progress > 1) return {};

    const cssVars: Record<string, string | number> = {};

    // Set CSS custom properties for animation parameters
    if (anim.parameters.distance !== undefined) {
      cssVars['--slide-distance'] = `${anim.parameters.distance}px`;
    }
    if (anim.parameters.startScale !== undefined) {
      cssVars['--start-scale'] = String(anim.parameters.startScale);
    }
    if (anim.parameters.endScale !== undefined) {
      cssVars['--end-scale'] = String(anim.parameters.endScale);
    }
    if (anim.parameters.startBlur !== undefined) {
      cssVars['--start-blur'] = `${anim.parameters.startBlur}px`;
    }
    if (anim.parameters.startRotation !== undefined) {
      cssVars['--start-rotation'] = `${anim.parameters.startRotation}deg`;
    }
    if (anim.parameters.amplitude !== undefined) {
      cssVars['--wave-amplitude'] = `${anim.parameters.amplitude}px`;
    }

    return {
      ...cssVars,
      animation: `${animationName} ${anim.duration}s ${preset.easing} ${anim.delay}s forwards`,
      animationPlayState: isPlaying ? 'running' : 'paused',
    } as CSSProperties;
  };

  const getAnimationName = (type: TextAnimationType): string | null => {
    const animationMap: Record<TextAnimationType, string | null> = {
      none: null,
      'fade-in': 'fadeIn',
      'fade-out': 'fadeOut',
      typewriter: null, // Handled specially
      'typewriter-cursor': null, // Handled specially
      'bounce-in': 'bounceIn',
      'bounce-letters': 'bounceLetter',
      'slide-up': 'slideUp',
      'slide-down': 'slideDown',
      'slide-left': 'slideLeft',
      'slide-right': 'slideRight',
      'scale-in': 'scaleIn',
      'scale-out': 'scaleOut',
      'word-by-word': null, // Handled specially
      'word-highlight': null, // Handled specially
      karaoke: null, // Handled specially
      glitch: 'glitch',
      'blur-in': 'blurIn',
      'rotate-in': 'rotateIn',
      wave: 'wave',
      rainbow: 'rainbow',
    };
    return animationMap[type];
  };

  const getAnimationProgress = (
    anim: AppliedTextAnimation,
    time: number,
    clipDuration: number
  ): number => {
    if (anim.position === 'in') {
      return (time - anim.delay) / anim.duration;
    }
    if (anim.position === 'out') {
      const outStart = clipDuration - anim.duration - anim.delay;
      return (time - outStart) / anim.duration;
    }
    // Continuous
    return ((time - anim.delay) % anim.duration) / anim.duration;
  };

  // Combine animation styles
  const combinedAnimationStyle = useMemo(() => {
    let combined: CSSProperties = {};
    activeAnimations.forEach((anim) => {
      const animStyle = getAnimationStyle(anim);
      combined = { ...combined, ...animStyle };
    });
    return combined;
  }, [activeAnimations, currentTime, duration, isPlaying]);

  // Render typewriter text
  if (isTypewriter && inAnimation) {
    const preset = textAnimationsStore.getPreset(inAnimation.presetId);
    const showCursor = preset?.parameters.cursor === true;
    const cursorChar = (preset?.parameters.cursorChar as string) || '|';

    const visibleText = text.slice(0, typewriterIndex);
    const isComplete = typewriterIndex >= text.length;

    return (
      <span className={mergeClasses(styles.container, className)} style={{ ...style }}>
        {visibleText}
        {showCursor && !isComplete && <span className={styles.cursor}>{cursorChar}</span>}
      </span>
    );
  }

  // Render word-by-word text
  const wordByWordAnim = activeAnimations.find(
    (a) => textAnimationsStore.getPreset(a.presetId)?.type === 'word-by-word'
  );
  if (wordByWordAnim) {
    const words = text.split(/(\s+)/);
    const progress = getAnimationProgress(wordByWordAnim, currentTime, duration);
    const stagger = (wordByWordAnim.parameters.stagger as number) || 0.1;

    return (
      <span className={mergeClasses(styles.container, className)} style={style}>
        {words.map((word, index) => {
          const wordProgress = Math.min(1, Math.max(0, progress - index * stagger));
          const opacity = wordProgress;
          return (
            <span
              key={index}
              className={styles.word}
              style={{ opacity, transition: 'opacity 0.2s ease' }}
            >
              {word}
            </span>
          );
        })}
      </span>
    );
  }

  // Render word-highlight text
  const highlightAnim = activeAnimations.find(
    (a) => textAnimationsStore.getPreset(a.presetId)?.type === 'word-highlight'
  );
  if (highlightAnim) {
    const words = text.split(/(\s+)/);
    const progress = getAnimationProgress(highlightAnim, currentTime, duration);
    const highlightColor = (highlightAnim.parameters.highlightColor as string) || '#FFFF00';
    const activeWordIndex = Math.floor(progress * words.filter((w) => w.trim()).length);

    let wordCount = 0;
    return (
      <span className={mergeClasses(styles.container, className)} style={style}>
        {words.map((word, index) => {
          const isWord = word.trim().length > 0;
          const isHighlighted = isWord && wordCount === activeWordIndex;
          if (isWord) wordCount++;
          return (
            <span
              key={index}
              className={styles.word}
              style={{
                backgroundColor: isHighlighted ? highlightColor : 'transparent',
                color: isHighlighted ? tokens.colorNeutralForegroundOnBrand : undefined,
                transition: 'all 0.15s ease',
              }}
            >
              {word}
            </span>
          );
        })}
      </span>
    );
  }

  // Render karaoke text
  const karaokeAnim = activeAnimations.find(
    (a) => textAnimationsStore.getPreset(a.presetId)?.type === 'karaoke'
  );
  if (karaokeAnim) {
    const progress = getAnimationProgress(karaokeAnim, currentTime, duration);
    const fillColor = (karaokeAnim.parameters.fillColor as string) || '#00FF00';
    const fillPercent = Math.min(100, Math.max(0, progress * 100));

    return (
      <span
        className={mergeClasses(styles.container, className)}
        style={{
          ...style,
          background: `linear-gradient(to right, ${fillColor} ${fillPercent}%, currentColor ${fillPercent}%)`,
          WebkitBackgroundClip: 'text',
          WebkitTextFillColor: 'transparent',
          backgroundClip: 'text',
        }}
      >
        {text}
      </span>
    );
  }

  // Default render with combined animation styles
  return (
    <span
      className={mergeClasses(styles.container, className)}
      style={{ ...style, ...combinedAnimationStyle }}
    >
      {text}
    </span>
  );
};

export default AnimatedTextRenderer;
