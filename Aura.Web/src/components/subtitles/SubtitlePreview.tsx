/**
 * Subtitle Preview Component with RTL Support
 * Displays subtitle content with proper formatting and directional layout
 */

import { Card, Text, Button } from '@fluentui/react-components';
import { DocumentArrowDownRegular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { translationIntegrationService } from '@/services/translationIntegrationService';
import type { SubtitleOutputDto } from '@/types/api-v1';

interface SubtitlePreviewProps {
  subtitles: SubtitleOutputDto;
  isRTL?: boolean;
  filename?: string;
}

export const SubtitlePreview: FC<SubtitlePreviewProps> = ({
  subtitles,
  isRTL = false,
  filename,
}) => {
  const handleDownload = (): void => {
    translationIntegrationService.downloadSubtitles(subtitles, filename);
  };

  const formatSubtitleLine = (line: string): string => {
    return line.trim();
  };

  const lines = subtitles.content.split('\n').filter((line) => line.trim().length > 0);

  return (
    <Card style={{ padding: '16px' }}>
      <div style={{ marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '12px' }}>
        <Text weight="semibold" size={400}>
          Subtitle Preview
        </Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
          ({subtitles.lineCount} entries, {subtitles.format})
        </Text>
        {isRTL && (
          <Text size={300} style={{ color: 'var(--colorBrandForeground1)' }}>
            RTL Layout
          </Text>
        )}
        <div style={{ marginLeft: 'auto' }}>
          <Button appearance="primary" icon={<DocumentArrowDownRegular />} onClick={handleDownload}>
            Download {subtitles.format}
          </Button>
        </div>
      </div>

      <div
        className={isRTL ? 'subtitle-rtl' : 'subtitle-ltr'}
        style={{
          maxHeight: '400px',
          overflowY: 'auto',
          padding: '12px',
          backgroundColor: 'var(--colorNeutralBackground1)',
          borderRadius: '4px',
          border: '1px solid var(--colorNeutralStroke1)',
          fontFamily: 'monospace',
          fontSize: '14px',
          lineHeight: '1.5',
          whiteSpace: 'pre-wrap',
        }}
      >
        {lines.map((line, index) => (
          <div key={index} style={{ marginBottom: '4px' }}>
            {formatSubtitleLine(line)}
          </div>
        ))}
      </div>

      <div style={{ marginTop: '12px' }}>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Format: {subtitles.format} | Lines: {subtitles.lineCount}
        </Text>
      </div>
    </Card>
  );
};
