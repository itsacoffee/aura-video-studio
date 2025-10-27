/**
 * Chroma Key Service
 * Advanced green screen / blue screen keying with WebGL acceleration
 */

/**
 * Convert hex color to RGB
 */
export function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  return result
    ? {
        r: parseInt(result[1], 16) / 255,
        g: parseInt(result[2], 16) / 255,
        b: parseInt(result[3], 16) / 255,
      }
    : { r: 0, g: 1, b: 0 }; // Default to green
}

/**
 * Calculate color distance in RGB space
 */
export function colorDistance(
  r1: number,
  g1: number,
  b1: number,
  r2: number,
  g2: number,
  b2: number
): number {
  return Math.sqrt((r1 - r2) ** 2 + (g1 - g2) ** 2 + (b1 - b2) ** 2);
}

/**
 * Apply chroma key to image data (CPU-based implementation)
 */
export function applyChromaKey(
  imageData: ImageData,
  keyColor: string,
  similarity: number,
  smoothness: number,
  spillSuppression: number
): ImageData {
  const data = imageData.data;
  const keyRgb = hexToRgb(keyColor);
  const similarityThreshold = similarity / 100;
  const smoothnessRange = smoothness / 100;

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i] / 255;
    const g = data[i + 1] / 255;
    const b = data[i + 2] / 255;

    // Calculate distance to key color
    const distance = colorDistance(r, g, b, keyRgb.r, keyRgb.g, keyRgb.b);

    // Calculate alpha based on distance
    let alpha = 1;
    if (distance < similarityThreshold) {
      if (smoothnessRange > 0) {
        // Smooth transition
        alpha = Math.min(1, distance / similarityThreshold);
        alpha = Math.pow(alpha, 1 / (1 + smoothnessRange));
      } else {
        alpha = 0;
      }
    }

    // Apply spill suppression
    if (spillSuppression > 0 && alpha > 0) {
      const spillAmount = spillSuppression / 100;
      const spillDistance = colorDistance(r, g, b, keyRgb.r, keyRgb.g, keyRgb.b);

      if (spillDistance < similarityThreshold * 1.5) {
        // Reduce the key color component
        if (keyRgb.g > keyRgb.r && keyRgb.g > keyRgb.b) {
          // Green spill
          const avgRB = (r + b) / 2;
          data[i + 1] = Math.round(Math.min(g, avgRB + (g - avgRB) * (1 - spillAmount)) * 255);
        } else if (keyRgb.b > keyRgb.r && keyRgb.b > keyRgb.g) {
          // Blue spill
          const avgRG = (r + g) / 2;
          data[i + 2] = Math.round(Math.min(b, avgRG + (b - avgRG) * (1 - spillAmount)) * 255);
        }
      }
    }

    // Set alpha channel
    data[i + 3] = Math.round(alpha * 255);
  }

  return imageData;
}

/**
 * Process neighbors for edge refinement
 */
function processNeighborsForRefinement(
  data: Uint8ClampedArray,
  x: number,
  y: number,
  width: number,
  height: number,
  kernelSize: number,
  shouldErode: boolean,
  currentAlpha: number
): number {
  let newAlpha = currentAlpha;

  for (let ky = -kernelSize; ky <= kernelSize; ky++) {
    for (let kx = -kernelSize; kx <= kernelSize; kx++) {
      const ny = y + ky;
      const nx = x + kx;

      if (ny >= 0 && ny < height && nx >= 0 && nx < width) {
        const nIdx = (ny * width + nx) * 4;
        const neighborAlpha = data[nIdx + 3];

        if (shouldErode) {
          // Erode (choke) - make edges thinner
          newAlpha = Math.min(newAlpha, neighborAlpha);
        } else {
          // Dilate (spread) - make edges thicker
          newAlpha = Math.max(newAlpha, neighborAlpha);
        }
      }
    }
  }

  return newAlpha;
}

/**
 * Apply edge refinement (choke/spread)
 */
export function applyEdgeRefinement(imageData: ImageData, thickness: number): ImageData {
  if (thickness === 0) return imageData;

  const width = imageData.width;
  const height = imageData.height;
  const data = imageData.data;
  const outputData = new Uint8ClampedArray(data);

  const kernelSize = Math.abs(Math.round(thickness)) + 1;
  const shouldErode = thickness > 0;

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const idx = (y * width + x) * 4;
      const currentAlpha = data[idx + 3];

      const newAlpha = processNeighborsForRefinement(
        data,
        x,
        y,
        width,
        height,
        kernelSize,
        shouldErode,
        currentAlpha
      );

      outputData[idx + 3] = newAlpha;
    }
  }

  return new ImageData(outputData, width, height);
}

/**
 * Calculate average alpha within radius for feathering
 */
function calculateAverageAlpha(
  data: Uint8ClampedArray,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number
): number {
  let alphaSum = 0;
  let count = 0;

  for (let ky = -radius; ky <= radius; ky++) {
    for (let kx = -radius; kx <= radius; kx++) {
      const ny = y + ky;
      const nx = x + kx;

      if (ny >= 0 && ny < height && nx >= 0 && nx < width) {
        const nIdx = (ny * width + nx) * 4;
        const distance = Math.sqrt(kx * kx + ky * ky);

        if (distance <= radius) {
          alphaSum += data[nIdx + 3];
          count++;
        }
      }
    }
  }

  return count > 0 ? alphaSum / count : data[(y * width + x) * 4 + 3];
}

/**
 * Apply edge feathering
 */
export function applyEdgeFeather(imageData: ImageData, featherRadius: number): ImageData {
  if (featherRadius === 0) return imageData;

  const width = imageData.width;
  const height = imageData.height;
  const data = imageData.data;
  const outputData = new Uint8ClampedArray(data);

  const radius = Math.round(featherRadius);

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const idx = (y * width + x) * 4;
      outputData[idx + 3] = calculateAverageAlpha(data, x, y, width, height, radius);
    }
  }

  return new ImageData(outputData, width, height);
}

/**
 * Apply matte cleanup (remove noise)
 */
export function applyMatteCleanup(imageData: ImageData, threshold: number): ImageData {
  if (threshold === 0) return imageData;

  const data = imageData.data;
  const cleanupThreshold = (threshold / 100) * 255;

  for (let i = 0; i < data.length; i += 4) {
    const alpha = data[i + 3];

    // Clean up semi-transparent pixels
    if (alpha < cleanupThreshold) {
      data[i + 3] = 0;
    } else if (alpha > 255 - cleanupThreshold) {
      data[i + 3] = 255;
    }
  }

  return imageData;
}

/**
 * WebGL Chroma Key Shader (vertex shader)
 */
export const chromaKeyVertexShader = `
  attribute vec2 a_position;
  attribute vec2 a_texCoord;
  varying vec2 v_texCoord;

  void main() {
    gl_Position = vec4(a_position, 0.0, 1.0);
    v_texCoord = a_texCoord;
  }
`;

/**
 * WebGL Chroma Key Shader (fragment shader)
 */
export const chromaKeyFragmentShader = `
  precision mediump float;
  
  uniform sampler2D u_image;
  uniform vec3 u_keyColor;
  uniform float u_similarity;
  uniform float u_smoothness;
  uniform float u_spillSuppression;
  
  varying vec2 v_texCoord;

  void main() {
    vec4 color = texture2D(u_image, v_texCoord);
    
    // Calculate distance to key color
    float dist = distance(color.rgb, u_keyColor);
    
    // Calculate alpha
    float alpha = 1.0;
    if (dist < u_similarity) {
      if (u_smoothness > 0.0) {
        alpha = smoothstep(0.0, u_similarity, dist);
        alpha = pow(alpha, 1.0 / (1.0 + u_smoothness));
      } else {
        alpha = 0.0;
      }
    }
    
    // Spill suppression
    vec3 finalColor = color.rgb;
    if (u_spillSuppression > 0.0 && alpha > 0.0) {
      float spillDist = distance(color.rgb, u_keyColor);
      if (spillDist < u_similarity * 1.5) {
        // Reduce key color component
        if (u_keyColor.g > u_keyColor.r && u_keyColor.g > u_keyColor.b) {
          // Green spill
          float avgRB = (color.r + color.b) / 2.0;
          finalColor.g = min(color.g, avgRB + (color.g - avgRB) * (1.0 - u_spillSuppression));
        } else if (u_keyColor.b > u_keyColor.r && u_keyColor.b > u_keyColor.g) {
          // Blue spill
          float avgRG = (color.r + color.g) / 2.0;
          finalColor.b = min(color.b, avgRG + (color.b - avgRG) * (1.0 - u_spillSuppression));
        }
      }
    }
    
    gl_FragColor = vec4(finalColor, alpha);
  }
`;

/**
 * Create WebGL chroma key processor
 */
export class WebGLChromaKeyProcessor {
  private gl: WebGLRenderingContext | null = null;
  private program: WebGLProgram | null = null;
  private canvas: HTMLCanvasElement;

  constructor() {
    this.canvas = document.createElement('canvas');
    this.gl =
      this.canvas.getContext('webgl') ||
      (this.canvas.getContext('experimental-webgl') as WebGLRenderingContext);

    if (this.gl) {
      this.initShaders();
    }
  }

  private initShaders(): void {
    if (!this.gl) return;

    const vertexShader = this.compileShader(this.gl.VERTEX_SHADER, chromaKeyVertexShader);
    const fragmentShader = this.compileShader(this.gl.FRAGMENT_SHADER, chromaKeyFragmentShader);

    if (!vertexShader || !fragmentShader) return;

    this.program = this.gl.createProgram();
    if (!this.program) return;

    this.gl.attachShader(this.program, vertexShader);
    this.gl.attachShader(this.program, fragmentShader);
    this.gl.linkProgram(this.program);

    if (!this.gl.getProgramParameter(this.program, this.gl.LINK_STATUS)) {
      console.error('Shader program failed to link');
      this.program = null;
    }
  }

  private compileShader(type: number, source: string): WebGLShader | null {
    if (!this.gl) return null;

    const shader = this.gl.createShader(type);
    if (!shader) return null;

    this.gl.shaderSource(shader, source);
    this.gl.compileShader(shader);

    if (!this.gl.getShaderParameter(shader, this.gl.COMPILE_STATUS)) {
      console.error('Shader compilation error:', this.gl.getShaderInfoLog(shader));
      this.gl.deleteShader(shader);
      return null;
    }

    return shader;
  }

  public processFrame(
    sourceImage: HTMLImageElement | HTMLCanvasElement,
    keyColor: string,
    similarity: number,
    smoothness: number,
    spillSuppression: number
  ): HTMLCanvasElement {
    if (!this.gl || !this.program) {
      // Fallback to canvas if WebGL not available
      return this.processCPU(sourceImage, keyColor, similarity, smoothness, spillSuppression);
    }

    this.canvas.width = sourceImage.width;
    this.canvas.height = sourceImage.height;

    const gl = this.gl;
    gl.useProgram(this.program);

    // Set up texture
    const texture = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, sourceImage);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);

    // Set uniforms
    const keyRgb = hexToRgb(keyColor);
    gl.uniform3f(gl.getUniformLocation(this.program, 'u_keyColor'), keyRgb.r, keyRgb.g, keyRgb.b);
    gl.uniform1f(gl.getUniformLocation(this.program, 'u_similarity'), similarity / 100);
    gl.uniform1f(gl.getUniformLocation(this.program, 'u_smoothness'), smoothness / 100);
    gl.uniform1f(gl.getUniformLocation(this.program, 'u_spillSuppression'), spillSuppression / 100);

    // Draw
    gl.viewport(0, 0, this.canvas.width, this.canvas.height);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

    return this.canvas;
  }

  private processCPU(
    sourceImage: HTMLImageElement | HTMLCanvasElement,
    keyColor: string,
    similarity: number,
    smoothness: number,
    spillSuppression: number
  ): HTMLCanvasElement {
    const canvas = document.createElement('canvas');
    canvas.width = sourceImage.width;
    canvas.height = sourceImage.height;
    const ctx = canvas.getContext('2d');

    if (!ctx) return canvas;

    ctx.drawImage(sourceImage, 0, 0);
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const processed = applyChromaKey(imageData, keyColor, similarity, smoothness, spillSuppression);
    ctx.putImageData(processed, 0, 0);

    return canvas;
  }

  public destroy(): void {
    if (this.gl && this.program) {
      this.gl.deleteProgram(this.program);
    }
  }
}
