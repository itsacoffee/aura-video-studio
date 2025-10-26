/**
 * Effects Worker
 *
 * Web Worker for CPU-intensive effects processing.
 * Offloads image manipulation and effect rendering from the main thread
 * to maintain 60fps UI performance.
 */

interface EffectMessage {
  type: 'apply-effects';
  imageData: ImageData;
  effects: Array<{ effectType: string; enabled: boolean; parameters: Record<string, number> }>;
  timestamp: number;
}

interface ResultMessage {
  type: 'result';
  imageData: ImageData;
  timestamp: number;
  processingTime: number;
}

interface ErrorMessage {
  type: 'error';
  error: string;
  timestamp: number;
}

type WorkerMessage = EffectMessage;

// Worker message handler
self.onmessage = (event: MessageEvent<WorkerMessage>) => {
  const message = event.data;

  if (message.type === 'apply-effects') {
    try {
      const startTime = performance.now();
      const result = applyEffects(message.imageData, message.effects);
      const processingTime = performance.now() - startTime;

      const response: ResultMessage = {
        type: 'result',
        imageData: result,
        timestamp: message.timestamp,
        processingTime,
      };

      self.postMessage(response);
    } catch (error) {
      const errorMessage: ErrorMessage = {
        type: 'error',
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: message.timestamp,
      };
      self.postMessage(errorMessage);
    }
  }
};

/**
 * Apply multiple effects to an ImageData object
 */
function applyEffects(imageData: ImageData, effects: Array<{ effectType: string; enabled: boolean; parameters: Record<string, number> }>): ImageData {
  let result = cloneImageData(imageData);

  for (const effect of effects) {
    if (!effect.enabled) continue;

    switch (effect.effectType) {
      case 'brightness':
        result = applyBrightness(result, effect.parameters.value);
        break;
      case 'contrast':
        result = applyContrast(result, effect.parameters.value);
        break;
      case 'saturation':
        result = applySaturation(result, effect.parameters.value);
        break;
      case 'blur':
        result = applyBlur(result, effect.parameters.amount);
        break;
      case 'grayscale':
        result = applyGrayscale(result);
        break;
      case 'sepia':
        result = applySepia(result);
        break;
      case 'invert':
        result = applyInvert(result);
        break;
      case 'hue':
        result = applyHueRotation(result, effect.parameters.rotation);
        break;
      default:
        console.warn(`Unknown effect type: ${effect.effectType}`);
    }
  }

  return result;
}

/**
 * Clone ImageData object
 */
function cloneImageData(imageData: ImageData): ImageData {
  const cloned = new ImageData(imageData.width, imageData.height);
  cloned.data.set(imageData.data);
  return cloned;
}

/**
 * Apply brightness effect
 */
function applyBrightness(imageData: ImageData, value: number): ImageData {
  const data = imageData.data;
  const factor = (value + 100) / 100; // value is -100 to 100

  for (let i = 0; i < data.length; i += 4) {
    data[i] = Math.min(255, Math.max(0, data[i] * factor)); // R
    data[i + 1] = Math.min(255, Math.max(0, data[i + 1] * factor)); // G
    data[i + 2] = Math.min(255, Math.max(0, data[i + 2] * factor)); // B
  }

  return imageData;
}

/**
 * Apply contrast effect
 */
function applyContrast(imageData: ImageData, value: number): ImageData {
  const data = imageData.data;
  const factor = (259 * (value + 255)) / (255 * (259 - value)); // value is -100 to 100

  for (let i = 0; i < data.length; i += 4) {
    data[i] = Math.min(255, Math.max(0, factor * (data[i] - 128) + 128)); // R
    data[i + 1] = Math.min(255, Math.max(0, factor * (data[i + 1] - 128) + 128)); // G
    data[i + 2] = Math.min(255, Math.max(0, factor * (data[i + 2] - 128) + 128)); // B
  }

  return imageData;
}

/**
 * Apply saturation effect
 */
function applySaturation(imageData: ImageData, value: number): ImageData {
  const data = imageData.data;
  const factor = (value + 100) / 100; // value is -100 to 100

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];

    const gray = 0.2989 * r + 0.587 * g + 0.114 * b;

    data[i] = Math.min(255, Math.max(0, gray + factor * (r - gray)));
    data[i + 1] = Math.min(255, Math.max(0, gray + factor * (g - gray)));
    data[i + 2] = Math.min(255, Math.max(0, gray + factor * (b - gray)));
  }

  return imageData;
}

/**
 * Apply blur effect (simple box blur)
 */
function applyBlur(imageData: ImageData, amount: number): ImageData {
  if (amount <= 0) return imageData;

  const width = imageData.width;
  const height = imageData.height;
  const data = imageData.data;
  const result = cloneImageData(imageData);
  const resultData = result.data;

  const radius = Math.min(Math.floor(amount), 20); // Limit blur radius for performance

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      let r = 0,
        g = 0,
        b = 0,
        a = 0,
        count = 0;

      for (let ky = -radius; ky <= radius; ky++) {
        for (let kx = -radius; kx <= radius; kx++) {
          const nx = Math.min(width - 1, Math.max(0, x + kx));
          const ny = Math.min(height - 1, Math.max(0, y + ky));
          const idx = (ny * width + nx) * 4;

          r += data[idx];
          g += data[idx + 1];
          b += data[idx + 2];
          a += data[idx + 3];
          count++;
        }
      }

      const idx = (y * width + x) * 4;
      resultData[idx] = r / count;
      resultData[idx + 1] = g / count;
      resultData[idx + 2] = b / count;
      resultData[idx + 3] = a / count;
    }
  }

  return result;
}

/**
 * Apply grayscale effect
 */
function applyGrayscale(imageData: ImageData): ImageData {
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    const gray = 0.2989 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
    data[i] = gray;
    data[i + 1] = gray;
    data[i + 2] = gray;
  }

  return imageData;
}

/**
 * Apply sepia effect
 */
function applySepia(imageData: ImageData): ImageData {
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];

    data[i] = Math.min(255, 0.393 * r + 0.769 * g + 0.189 * b);
    data[i + 1] = Math.min(255, 0.349 * r + 0.686 * g + 0.168 * b);
    data[i + 2] = Math.min(255, 0.272 * r + 0.534 * g + 0.131 * b);
  }

  return imageData;
}

/**
 * Apply invert effect
 */
function applyInvert(imageData: ImageData): ImageData {
  const data = imageData.data;

  for (let i = 0; i < data.length; i += 4) {
    data[i] = 255 - data[i];
    data[i + 1] = 255 - data[i + 1];
    data[i + 2] = 255 - data[i + 2];
  }

  return imageData;
}

/**
 * Apply hue rotation
 */
function applyHueRotation(imageData: ImageData, rotation: number): ImageData {
  const data = imageData.data;
  const angle = (rotation * Math.PI) / 180;

  const cosA = Math.cos(angle);
  const sinA = Math.sin(angle);

  const matrix = [
    cosA + (1 - cosA) / 3,
    (1 / 3) * (1 - cosA) - Math.sqrt(1 / 3) * sinA,
    (1 / 3) * (1 - cosA) + Math.sqrt(1 / 3) * sinA,
    (1 / 3) * (1 - cosA) + Math.sqrt(1 / 3) * sinA,
    cosA + (1 / 3) * (1 - cosA),
    (1 / 3) * (1 - cosA) - Math.sqrt(1 / 3) * sinA,
    (1 / 3) * (1 - cosA) - Math.sqrt(1 / 3) * sinA,
    (1 / 3) * (1 - cosA) + Math.sqrt(1 / 3) * sinA,
    cosA + (1 / 3) * (1 - cosA),
  ];

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];

    data[i] = Math.min(255, Math.max(0, matrix[0] * r + matrix[1] * g + matrix[2] * b));
    data[i + 1] = Math.min(255, Math.max(0, matrix[3] * r + matrix[4] * g + matrix[5] * b));
    data[i + 2] = Math.min(255, Math.max(0, matrix[6] * r + matrix[7] * g + matrix[8] * b));
  }

  return imageData;
}

export {};
