import {
  EffectPreset,
  EffectCategory,
  VideoEffect,
  ApplyEffectsRequest,
  ApplyEffectsResponse,
  ApplyPresetRequest,
  EffectPreviewRequest,
  EffectPreviewResponse,
  ValidationResponse,
  CacheStatistics,
} from '../../types/videoEffects';

const BASE_URL = '/api/video-effects';

class VideoEffectsApi {
  /**
   * Get all available effect presets
   */
  async getPresets(category?: EffectCategory): Promise<EffectPreset[]> {
    const params = category ? `?category=${category}` : '';
    const response = await fetch(`${BASE_URL}/presets${params}`);
    if (!response.ok) {
      throw new Error('Failed to fetch presets');
    }
    return response.json();
  }

  /**
   * Get a specific preset by ID
   */
  async getPreset(id: string): Promise<EffectPreset> {
    const response = await fetch(`${BASE_URL}/presets/${id}`);
    if (!response.ok) {
      throw new Error('Failed to fetch preset');
    }
    return response.json();
  }

  /**
   * Save a custom effect preset
   */
  async savePreset(preset: EffectPreset): Promise<EffectPreset> {
    const response = await fetch(`${BASE_URL}/presets`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(preset),
    });
    if (!response.ok) {
      throw new Error('Failed to save preset');
    }
    return response.json();
  }

  /**
   * Delete a custom preset
   */
  async deletePreset(id: string): Promise<void> {
    const response = await fetch(`${BASE_URL}/presets/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) {
      throw new Error('Failed to delete preset');
    }
  }

  /**
   * Apply effects to a video
   */
  async applyEffects(request: ApplyEffectsRequest): Promise<ApplyEffectsResponse> {
    const response = await fetch(`${BASE_URL}/apply`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      throw new Error('Failed to apply effects');
    }
    return response.json();
  }

  /**
   * Apply a preset to a video
   */
  async applyPreset(request: ApplyPresetRequest): Promise<ApplyEffectsResponse> {
    const response = await fetch(`${BASE_URL}/apply-preset`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      throw new Error('Failed to apply preset');
    }
    return response.json();
  }

  /**
   * Generate a preview for an effect
   */
  async generatePreview(request: EffectPreviewRequest): Promise<EffectPreviewResponse> {
    const response = await fetch(`${BASE_URL}/preview`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      throw new Error('Failed to generate preview');
    }
    return response.json();
  }

  /**
   * Get recommended effects for a video
   */
  async getRecommendations(videoPath: string): Promise<EffectPreset[]> {
    const response = await fetch(
      `${BASE_URL}/recommendations?videoPath=${encodeURIComponent(videoPath)}`
    );
    if (!response.ok) {
      throw new Error('Failed to get recommendations');
    }
    return response.json();
  }

  /**
   * Validate an effect
   */
  async validateEffect(effect: VideoEffect): Promise<ValidationResponse> {
    const response = await fetch(`${BASE_URL}/validate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(effect),
    });
    if (!response.ok) {
      throw new Error('Failed to validate effect');
    }
    return response.json();
  }

  /**
   * Get cache statistics
   */
  async getCacheStats(): Promise<CacheStatistics> {
    const response = await fetch(`${BASE_URL}/cache/stats`);
    if (!response.ok) {
      throw new Error('Failed to get cache stats');
    }
    return response.json();
  }

  /**
   * Clear effect cache
   */
  async clearCache(): Promise<void> {
    const response = await fetch(`${BASE_URL}/cache`, {
      method: 'DELETE',
    });
    if (!response.ok) {
      throw new Error('Failed to clear cache');
    }
  }
}

export const videoEffectsApi = new VideoEffectsApi();
