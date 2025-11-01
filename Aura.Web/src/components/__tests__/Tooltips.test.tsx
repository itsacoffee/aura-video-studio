import { FluentProvider, webLightTheme, Tooltip } from '@fluentui/react-components';
import { Info24Regular } from '@fluentui/react-icons';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { TooltipContent, TooltipWithLink } from '../Tooltips';

describe('TooltipContent', () => {
  it('should contain all required tooltip content sections', () => {
    // Brief Section
    expect(TooltipContent.topic).toBeDefined();
    expect(TooltipContent.audience).toBeDefined();
    expect(TooltipContent.goal).toBeDefined();
    expect(TooltipContent.tone).toBeDefined();
    expect(TooltipContent.language).toBeDefined();
    expect(TooltipContent.aspect).toBeDefined();

    // Plan Section
    expect(TooltipContent.duration).toBeDefined();
    expect(TooltipContent.pacing).toBeDefined();
    expect(TooltipContent.density).toBeDefined();
    expect(TooltipContent.style).toBeDefined();

    // Voice/TTS Providers
    expect(TooltipContent.ttsWindowsSapi).toBeDefined();
    expect(TooltipContent.ttsPiper).toBeDefined();
    expect(TooltipContent.ttsElevenLabs).toBeDefined();
    expect(TooltipContent.ttsPlayHT).toBeDefined();
    expect(TooltipContent.voiceProvider).toBeDefined();

    // Settings - Performance
    expect(TooltipContent.hardwareAcceleration).toBeDefined();
    expect(TooltipContent.qualityModeStandard).toBeDefined();
    expect(TooltipContent.encoderNvenc).toBeDefined();
    expect(TooltipContent.parallelJobs).toBeDefined();
    expect(TooltipContent.renderThreads).toBeDefined();

    // Settings - API Keys
    expect(TooltipContent.apiKeyOpenAI).toBeDefined();
    expect(TooltipContent.apiKeyElevenLabs).toBeDefined();
    expect(TooltipContent.apiKeyPexels).toBeDefined();

    // Welcome Page
    expect(TooltipContent.welcomeCreateNew).toBeDefined();
    expect(TooltipContent.welcomeSettings).toBeDefined();

    // Video Editor
    expect(TooltipContent.editorTimeline).toBeDefined();
    expect(TooltipContent.editorTransform).toBeDefined();

    // Export Dialog
    expect(TooltipContent.exportCodecH264).toBeDefined();
    expect(TooltipContent.exportPreset).toBeDefined();
  });

  it('should have text property for all tooltip content', () => {
    Object.entries(TooltipContent).forEach(([_key, value]) => {
      expect(value).toHaveProperty('text');
      expect(typeof value.text).toBe('string');
      expect(value.text.length).toBeGreaterThan(0);
    });
  });

  it('should have docLink property for all tooltip content', () => {
    Object.entries(TooltipContent).forEach(([_key, value]) => {
      expect(value).toHaveProperty('docLink');
      expect(value.docLink === null || typeof value.docLink === 'string').toBe(true);
    });
  });

  it('should have informative text that is not too long', () => {
    Object.entries(TooltipContent).forEach(([_key, value]) => {
      // Tooltips should be concise - aim for one to two sentences maximum
      // Allowing up to 250 characters as a reasonable limit
      expect(value.text.length).toBeLessThan(250);
    });
  });

  it('should include cost information for premium providers', () => {
    // ElevenLabs should mention cost
    expect(TooltipContent.ttsElevenLabs.text.toLowerCase()).toContain('cost');
    expect(TooltipContent.apiKeyElevenLabs.text.toLowerCase()).toMatch(/cost|character/i);

    // PlayHT should mention subscription
    expect(TooltipContent.ttsPlayHT.text.toLowerCase()).toMatch(/subscription|require/i);
  });

  it('should highlight free options clearly', () => {
    // Windows SAPI should mention free
    expect(TooltipContent.ttsWindowsSapi.text.toLowerCase()).toContain('free');

    // Piper should mention free
    expect(TooltipContent.ttsPiper.text.toLowerCase()).toContain('free');
  });

  it('should explain hardware acceleration requirements', () => {
    // NVENC should mention NVIDIA
    expect(TooltipContent.encoderNvenc.text.toLowerCase()).toContain('nvidia');

    // QuickSync should mention Intel
    expect(TooltipContent.encoderQuickSync.text.toLowerCase()).toContain('intel');
  });
});

describe('TooltipWithLink', () => {
  it('should render tooltip text', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <TooltipWithLink content={TooltipContent.topic} />
      </FluentProvider>
    );

    expect(screen.getByText(TooltipContent.topic.text)).toBeInTheDocument();
  });

  it('should render learn more link when docLink is provided', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <TooltipWithLink content={TooltipContent.audience} />
      </FluentProvider>
    );

    const link = screen.getByText('Learn more');
    expect(link).toBeInTheDocument();
    expect(link).toHaveAttribute('href', TooltipContent.audience.docLink);
  });

  it('should not render link when docLink is null', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <TooltipWithLink content={TooltipContent.topic} />
      </FluentProvider>
    );

    expect(screen.queryByText('Learn more')).not.toBeInTheDocument();
  });
});

describe('Tooltip Accessibility', () => {
  it('should have proper ARIA relationship when used with Info icon', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <Tooltip content={<TooltipWithLink content={TooltipContent.topic} />} relationship="label">
          <Info24Regular aria-label="Information about topic" />
        </Tooltip>
      </FluentProvider>
    );

    const icon = screen.getByLabelText('Information about topic');
    expect(icon).toBeInTheDocument();
  });

  it('should have concise text suitable for screen readers', () => {
    // All tooltips should be concise enough for screen readers
    Object.entries(TooltipContent).forEach(([_key, value]) => {
      // Screen readers work best with concise descriptions
      // Split by sentences and check each is reasonable
      const sentences = value.text.split(/[.!?]+/).filter((s) => s.trim().length > 0);
      sentences.forEach((sentence) => {
        // Each sentence should be under 150 characters for good screen reader experience
        expect(sentence.trim().length).toBeLessThan(150);
      });
    });
  });
});
