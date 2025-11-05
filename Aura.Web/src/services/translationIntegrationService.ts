import apiClient from './api/apiClient';
import type {
  TranslateAndPlanSSMLRequest,
  TranslatedSSMLResultDto,
  VoiceRecommendationRequest,
  VoiceRecommendationDto,
  SubtitleOutputDto,
  LineDto,
} from '@/types/api-v1';

/**
 * Service for translation integration with SSML and subtitles
 */
export const translationIntegrationService = {
  /**
   * Translate script and generate SSML with subtitles
   */
  async translateAndPlanSSML(
    request: TranslateAndPlanSSMLRequest
  ): Promise<TranslatedSSMLResultDto> {
    const response = await apiClient.post<TranslatedSSMLResultDto>(
      '/api/localization/translate-and-plan-ssml',
      request
    );
    return response.data;
  },

  /**
   * Get recommended voices for target language
   */
  async getVoiceRecommendation(
    request: VoiceRecommendationRequest
  ): Promise<VoiceRecommendationDto> {
    const response = await apiClient.post<VoiceRecommendationDto>(
      '/api/localization/voice-recommendation',
      request
    );
    return response.data;
  },

  /**
   * Download subtitles as file
   */
  downloadSubtitles(subtitles: SubtitleOutputDto, filename?: string): void {
    const extension = subtitles.format.toLowerCase();
    const name = filename || `subtitles.${extension}`;

    const blob = new Blob([subtitles.content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  },

  /**
   * Format duration for subtitle display
   */
  formatSubtitleDuration(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    const ms = Math.floor((seconds % 1) * 1000);

    return `${hours.toString().padStart(2, '0')}:${minutes
      .toString()
      .padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
  },

  /**
   * Validate script lines for translation
   */
  validateScriptLines(lines: LineDto[]): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (lines.length === 0) {
      errors.push('At least one script line is required');
    }

    lines.forEach((line, index) => {
      if (!line.text || line.text.trim().length === 0) {
        errors.push(`Line ${index + 1}: Text is required`);
      }
      if (line.durationSeconds <= 0) {
        errors.push(`Line ${index + 1}: Duration must be positive`);
      }
      if (line.startSeconds < 0) {
        errors.push(`Line ${index + 1}: Start time must be non-negative`);
      }
    });

    // Check for overlapping timecodes
    for (let i = 0; i < lines.length - 1; i++) {
      const currentEnd = lines[i].startSeconds + lines[i].durationSeconds;
      const nextStart = lines[i + 1].startSeconds;
      if (currentEnd > nextStart) {
        errors.push(`Lines ${i + 1} and ${i + 2}: Overlapping timecodes detected`);
      }
    }

    return {
      valid: errors.length === 0,
      errors,
    };
  },
};
