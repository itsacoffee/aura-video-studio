/**
 * Effects Engine - Renders video effects using Canvas 2D API
 */

import {
  applyChromaKey as chromaKeyProcessor,
  applyEdgeRefinement,
  applyEdgeFeather,
  applyMatteCleanup,
} from '../services/chromaKeyService';
import { AppliedEffect, Keyframe } from '../types/effects';

/**
 * Interpolates between keyframes at a given time
 */
function interpolateKeyframes(
  keyframes: Keyframe[],
  currentTime: number
): number | boolean | string {
  if (keyframes.length === 0) return 0;
  if (keyframes.length === 1) return keyframes[0].value;

  // Sort keyframes by time
  const sorted = [...keyframes].sort((a, b) => a.time - b.time);

  // If before first keyframe, use first value
  if (currentTime <= sorted[0].time) return sorted[0].value;

  // If after last keyframe, use last value
  if (currentTime >= sorted[sorted.length - 1].time) return sorted[sorted.length - 1].value;

  // Find the two keyframes to interpolate between
  for (let i = 0; i < sorted.length - 1; i++) {
    const kf1 = sorted[i];
    const kf2 = sorted[i + 1];

    if (currentTime >= kf1.time && currentTime <= kf2.time) {
      // Linear interpolation for now (can be extended with easing functions)
      const t = (currentTime - kf1.time) / (kf2.time - kf1.time);

      if (typeof kf1.value === 'number' && typeof kf2.value === 'number') {
        // Apply easing
        const easedT = applyEasing(t, kf1.easing || 'linear', kf1.bezier);
        return kf1.value + (kf2.value - kf1.value) * easedT;
      }

      // For non-numeric values, just use the first keyframe's value
      return kf1.value;
    }
  }

  return sorted[0].value;
}

/**
 * Apply easing function to interpolation
 */
function applyEasing(
  t: number,
  easing: 'linear' | 'ease-in' | 'ease-out' | 'ease-in-out' | 'bezier',
  bezier?: [number, number, number, number]
): number {
  switch (easing) {
    case 'linear':
      return t;
    case 'ease-in':
      return t * t;
    case 'ease-out':
      return t * (2 - t);
    case 'ease-in-out':
      return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    case 'bezier':
      if (bezier) {
        // Simplified cubic bezier - in production would use proper bezier calculation
        return cubicBezier(t, bezier[0], bezier[1], bezier[2], bezier[3]);
      }
      return t;
    default:
      return t;
  }
}

/**
 * Simplified cubic bezier calculation
 */
function cubicBezier(t: number, p1: number, p2: number, p3: number, p4: number): number {
  const u = 1 - t;
  return u * u * u * p1 + 3 * u * u * t * p2 + 3 * u * t * t * p3 + t * t * t * p4;
}

/**
 * Get effective parameter value considering keyframes
 */
export function getEffectiveParameterValue(
  effect: AppliedEffect,
  paramName: string,
  currentTime: number
): number | boolean | string {
  // Check if there are keyframes for this parameter
  if (effect.keyframes && effect.keyframes[paramName]) {
    return interpolateKeyframes(effect.keyframes[paramName], currentTime);
  }

  // Otherwise use the static parameter value
  return effect.parameters[paramName];
}

/**
 * Apply effects to a video frame
 */
export function applyEffectsToFrame(
  sourceCanvas: HTMLCanvasElement,
  effects: AppliedEffect[],
  currentTime: number
): HTMLCanvasElement {
  const outputCanvas = document.createElement('canvas');
  outputCanvas.width = sourceCanvas.width;
  outputCanvas.height = sourceCanvas.height;
  const ctx = outputCanvas.getContext('2d');

  if (!ctx) return sourceCanvas;

  // Start with the source image
  ctx.drawImage(sourceCanvas, 0, 0);

  // Apply each enabled effect in order
  for (const effect of effects) {
    if (!effect.enabled) continue;

    switch (effect.effectType) {
      case 'brightness':
        applyBrightness(ctx, effect, currentTime);
        break;
      case 'contrast':
        applyContrast(ctx, effect, currentTime);
        break;
      case 'saturation':
        applySaturation(ctx, effect, currentTime);
        break;
      case 'hue':
        applyHue(ctx, effect, currentTime);
        break;
      case 'gaussian-blur':
        applyGaussianBlur(ctx, effect, currentTime);
        break;
      case 'motion-blur':
        applyMotionBlur(ctx, effect, currentTime);
        break;
      case 'scale':
        applyScale(ctx, outputCanvas, effect, currentTime);
        break;
      case 'rotate':
        applyRotate(ctx, outputCanvas, effect, currentTime);
        break;
      case 'position':
        applyPosition(ctx, outputCanvas, effect, currentTime);
        break;
      case 'fade':
        applyFade(ctx, effect, currentTime);
        break;
      case 'dissolve':
        applyDissolve(ctx, effect, currentTime);
        break;
      case 'wipe':
        applyWipe(ctx, outputCanvas, effect, currentTime);
        break;
      case 'vignette':
        applyVignette(ctx, outputCanvas, effect, currentTime);
        break;
      case 'grain':
        applyGrain(ctx, outputCanvas, effect, currentTime);
        break;
      case 'chroma-key':
        applyChromaKey(ctx, effect, currentTime);
        break;
      case 'blend-mode':
        applyBlendMode(ctx, effect, currentTime);
        break;
    }
  }

  return outputCanvas;
}

function applyBrightness(
  ctx: CanvasRenderingContext2D,
  effect: AppliedEffect,
  currentTime: number
) {
  const value = getEffectiveParameterValue(effect, 'value', currentTime) as number;
  const imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    data[i] = Math.max(0, Math.min(255, data[i] + value)); // R
    data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + value)); // G
    data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + value)); // B
  }

  ctx.putImageData(imageData, 0, 0);
}

function applyContrast(ctx: CanvasRenderingContext2D, effect: AppliedEffect, currentTime: number) {
  const value = getEffectiveParameterValue(effect, 'value', currentTime) as number;
  const factor = (259 * (value + 255)) / (255 * (259 - value));
  const imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    data[i] = Math.max(0, Math.min(255, factor * (data[i] - 128) + 128));
    data[i + 1] = Math.max(0, Math.min(255, factor * (data[i + 1] - 128) + 128));
    data[i + 2] = Math.max(0, Math.min(255, factor * (data[i + 2] - 128) + 128));
  }

  ctx.putImageData(imageData, 0, 0);
}

function applySaturation(
  ctx: CanvasRenderingContext2D,
  effect: AppliedEffect,
  currentTime: number
) {
  const value = getEffectiveParameterValue(effect, 'value', currentTime) as number;
  const saturation = 1 + value / 100;
  const imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];

    // Calculate gray value
    const gray = 0.2989 * r + 0.587 * g + 0.114 * b;

    data[i] = Math.max(0, Math.min(255, gray + saturation * (r - gray)));
    data[i + 1] = Math.max(0, Math.min(255, gray + saturation * (g - gray)));
    data[i + 2] = Math.max(0, Math.min(255, gray + saturation * (b - gray)));
  }

  ctx.putImageData(imageData, 0, 0);
}

function applyHue(ctx: CanvasRenderingContext2D, effect: AppliedEffect, currentTime: number) {
  const value = getEffectiveParameterValue(effect, 'value', currentTime) as number;
  const imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i] / 255;
    const g = data[i + 1] / 255;
    const b = data[i + 2] / 255;

    // Convert RGB to HSL
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h = 0;
    const l = (max + min) / 2;
    const d = max - min;

    if (d !== 0) {
      const s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

      if (max === r) h = ((g - b) / d + (g < b ? 6 : 0)) / 6;
      else if (max === g) h = ((b - r) / d + 2) / 6;
      else h = ((r - g) / d + 4) / 6;

      // Rotate hue
      h = (h + value / 360) % 1;

      // Convert back to RGB
      const hue2rgb = (p: number, q: number, t: number) => {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6) return p + (q - p) * 6 * t;
        if (t < 1 / 2) return q;
        if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
        return p;
      };

      const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
      const p = 2 * l - q;

      data[i] = Math.round(hue2rgb(p, q, h + 1 / 3) * 255);
      data[i + 1] = Math.round(hue2rgb(p, q, h) * 255);
      data[i + 2] = Math.round(hue2rgb(p, q, h - 1 / 3) * 255);
    }
  }

  ctx.putImageData(imageData, 0, 0);
}

function applyGaussianBlur(
  ctx: CanvasRenderingContext2D,
  effect: AppliedEffect,
  currentTime: number
) {
  const radius = getEffectiveParameterValue(effect, 'radius', currentTime) as number;
  if (radius > 0) {
    // Use canvas filter for blur (modern browsers)
    ctx.filter = `blur(${radius}px)`;
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = ctx.canvas.width;
    tempCanvas.height = ctx.canvas.height;
    const tempCtx = tempCanvas.getContext('2d');
    if (tempCtx) {
      tempCtx.drawImage(ctx.canvas, 0, 0);
      ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
      ctx.drawImage(tempCanvas, 0, 0);
      ctx.filter = 'none';
    }
  }
}

function applyMotionBlur(
  _ctx: CanvasRenderingContext2D,
  _effect: AppliedEffect,
  _currentTime: number
) {
  // Motion blur is complex - simplified implementation
  // In production, would use directional blur shader or multiple frame blending
}

function applyScale(
  ctx: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  effect: AppliedEffect,
  currentTime: number
) {
  const scaleX = getEffectiveParameterValue(effect, 'scaleX', currentTime) as number;
  const scaleY = getEffectiveParameterValue(effect, 'scaleY', currentTime) as number;

  const tempCanvas = document.createElement('canvas');
  tempCanvas.width = canvas.width;
  tempCanvas.height = canvas.height;
  const tempCtx = tempCanvas.getContext('2d');
  if (tempCtx) {
    tempCtx.drawImage(canvas, 0, 0);

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.save();
    ctx.translate(canvas.width / 2, canvas.height / 2);
    ctx.scale(scaleX, scaleY);
    ctx.translate(-canvas.width / 2, -canvas.height / 2);
    ctx.drawImage(tempCanvas, 0, 0);
    ctx.restore();
  }
}

function applyRotate(
  ctx: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  effect: AppliedEffect,
  currentTime: number
) {
  const angle = getEffectiveParameterValue(effect, 'angle', currentTime) as number;

  const tempCanvas = document.createElement('canvas');
  tempCanvas.width = canvas.width;
  tempCanvas.height = canvas.height;
  const tempCtx = tempCanvas.getContext('2d');
  if (tempCtx) {
    tempCtx.drawImage(canvas, 0, 0);

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.save();
    ctx.translate(canvas.width / 2, canvas.height / 2);
    ctx.rotate((angle * Math.PI) / 180);
    ctx.translate(-canvas.width / 2, -canvas.height / 2);
    ctx.drawImage(tempCanvas, 0, 0);
    ctx.restore();
  }
}

function applyPosition(
  ctx: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  effect: AppliedEffect,
  currentTime: number
) {
  const x = getEffectiveParameterValue(effect, 'x', currentTime) as number;
  const y = getEffectiveParameterValue(effect, 'y', currentTime) as number;

  const tempCanvas = document.createElement('canvas');
  tempCanvas.width = canvas.width;
  tempCanvas.height = canvas.height;
  const tempCtx = tempCanvas.getContext('2d');
  if (tempCtx) {
    tempCtx.drawImage(canvas, 0, 0);

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(tempCanvas, x, y);
  }
}

function applyFade(ctx: CanvasRenderingContext2D, effect: AppliedEffect, currentTime: number) {
  const opacity = getEffectiveParameterValue(effect, 'opacity', currentTime) as number;
  ctx.globalAlpha = opacity;

  const tempCanvas = document.createElement('canvas');
  tempCanvas.width = ctx.canvas.width;
  tempCanvas.height = ctx.canvas.height;
  const tempCtx = tempCanvas.getContext('2d');
  if (tempCtx) {
    tempCtx.drawImage(ctx.canvas, 0, 0);
    ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
    ctx.drawImage(tempCanvas, 0, 0);
    ctx.globalAlpha = 1;
  }
}

function applyDissolve(ctx: CanvasRenderingContext2D, effect: AppliedEffect, currentTime: number) {
  const amount = getEffectiveParameterValue(effect, 'amount', currentTime) as number;

  // Dissolve using opacity
  ctx.globalAlpha = 1 - amount;
  const tempCanvas = document.createElement('canvas');
  tempCanvas.width = ctx.canvas.width;
  tempCanvas.height = ctx.canvas.height;
  const tempCtx = tempCanvas.getContext('2d');
  if (tempCtx) {
    tempCtx.drawImage(ctx.canvas, 0, 0);
    ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
    ctx.drawImage(tempCanvas, 0, 0);
    ctx.globalAlpha = 1;
  }
}

function applyWipe(
  ctx: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  effect: AppliedEffect,
  currentTime: number
) {
  const progress = getEffectiveParameterValue(effect, 'progress', currentTime) as number;
  const direction = getEffectiveParameterValue(effect, 'direction', currentTime) as string;

  const tempCanvas = document.createElement('canvas');
  tempCanvas.width = canvas.width;
  tempCanvas.height = canvas.height;
  const tempCtx = tempCanvas.getContext('2d');
  if (tempCtx) {
    tempCtx.drawImage(canvas, 0, 0);

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.save();

    // Create clipping region based on direction
    ctx.beginPath();
    switch (direction) {
      case 'left-to-right':
        ctx.rect(0, 0, canvas.width * progress, canvas.height);
        break;
      case 'right-to-left':
        ctx.rect(canvas.width * (1 - progress), 0, canvas.width * progress, canvas.height);
        break;
      case 'top-to-bottom':
        ctx.rect(0, 0, canvas.width, canvas.height * progress);
        break;
      case 'bottom-to-top':
        ctx.rect(0, canvas.height * (1 - progress), canvas.width, canvas.height * progress);
        break;
    }
    ctx.clip();
    ctx.drawImage(tempCanvas, 0, 0);
    ctx.restore();
  }
}

function applyVignette(
  ctx: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  effect: AppliedEffect,
  currentTime: number
) {
  const intensity = getEffectiveParameterValue(effect, 'intensity', currentTime) as number;
  const radius = getEffectiveParameterValue(effect, 'radius', currentTime) as number;

  const centerX = canvas.width / 2;
  const centerY = canvas.height / 2;
  const maxRadius = Math.sqrt(centerX * centerX + centerY * centerY);

  const gradient = ctx.createRadialGradient(
    centerX,
    centerY,
    maxRadius * radius,
    centerX,
    centerY,
    maxRadius
  );
  gradient.addColorStop(0, `rgba(0, 0, 0, 0)`);
  gradient.addColorStop(1, `rgba(0, 0, 0, ${intensity})`);

  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
}

function applyGrain(
  ctx: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  effect: AppliedEffect,
  currentTime: number
) {
  const intensity = getEffectiveParameterValue(effect, 'intensity', currentTime) as number;
  const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    const noise = (Math.random() - 0.5) * intensity * 255;
    data[i] = Math.max(0, Math.min(255, data[i] + noise));
    data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + noise));
    data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + noise));
  }

  ctx.putImageData(imageData, 0, 0);
}

function applyChromaKey(ctx: CanvasRenderingContext2D, effect: AppliedEffect, currentTime: number) {
  const keyColor = getEffectiveParameterValue(effect, 'keyColor', currentTime) as string;
  const similarity = getEffectiveParameterValue(effect, 'similarity', currentTime) as number;
  const smoothness = getEffectiveParameterValue(effect, 'smoothness', currentTime) as number;
  const spillSuppression = getEffectiveParameterValue(
    effect,
    'spillSuppression',
    currentTime
  ) as number;
  const edgeThickness = getEffectiveParameterValue(effect, 'edgeThickness', currentTime) as number;
  const edgeFeather = getEffectiveParameterValue(effect, 'edgeFeather', currentTime) as number;
  const choke = getEffectiveParameterValue(effect, 'choke', currentTime) as number;
  const matteCleanup = getEffectiveParameterValue(effect, 'matteCleanup', currentTime) as number;

  // Get image data
  let imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);

  // Apply chroma key
  imageData = chromaKeyProcessor(imageData, keyColor, similarity, smoothness, spillSuppression);

  // Apply edge refinement
  if (edgeThickness !== 0) {
    imageData = applyEdgeRefinement(imageData, edgeThickness);
  }

  // Apply choke
  if (choke !== 0) {
    imageData = applyEdgeRefinement(imageData, choke);
  }

  // Apply edge feather
  if (edgeFeather > 0) {
    imageData = applyEdgeFeather(imageData, edgeFeather);
  }

  // Apply matte cleanup
  if (matteCleanup > 0) {
    imageData = applyMatteCleanup(imageData, matteCleanup);
  }

  // Put processed image back
  ctx.putImageData(imageData, 0, 0);
}

function applyBlendMode(ctx: CanvasRenderingContext2D, effect: AppliedEffect, currentTime: number) {
  const mode = getEffectiveParameterValue(effect, 'mode', currentTime) as string;
  const opacity = getEffectiveParameterValue(effect, 'opacity', currentTime) as number;

  // Map mode names to canvas composite operations
  const compositeOperations: Record<string, string> = {
    normal: 'source-over',
    multiply: 'multiply',
    screen: 'screen',
    overlay: 'overlay',
    add: 'lighter',
  };

  if (compositeOperations[mode]) {
    ctx.globalCompositeOperation = compositeOperations[mode] as GlobalCompositeOperation;
  }

  ctx.globalAlpha = opacity / 100;
}
