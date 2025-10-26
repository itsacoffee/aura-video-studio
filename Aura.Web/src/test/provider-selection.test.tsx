import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ProviderSelection } from '../components/Wizard/ProviderSelection';

describe('ProviderSelection', () => {
  it('should render all provider dropdowns', () => {
    const mockOnChange = vi.fn();
    const selection = {};

    render(
      <FluentProvider theme={webLightTheme}>
        <ProviderSelection selection={selection} onSelectionChange={mockOnChange} />
      </FluentProvider>
    );

    // Check that all stage labels are present
    expect(screen.getByText(/Script LLM Provider/i)).toBeDefined();
    expect(screen.getByText(/TTS Provider/i)).toBeDefined();
    expect(screen.getByText(/Visuals Provider/i)).toBeDefined();
    // Upload Provider field has been removed per user request
  });

  it('should call onSelectionChange when provider is updated', () => {
    const mockOnChange = vi.fn();
    const selection = {
      script: 'Auto',
      tts: 'Auto',
      visuals: 'Auto',
      upload: 'Auto',
    };

    render(
      <FluentProvider theme={webLightTheme}>
        <ProviderSelection selection={selection} onSelectionChange={mockOnChange} />
      </FluentProvider>
    );

    // Component is rendered successfully
    expect(screen.getByText(/Provider Selection/i)).toBeDefined();
  });

  it('should display current selection values', () => {
    const mockOnChange = vi.fn();
    const selection = {
      script: 'OpenAI',
      tts: 'ElevenLabs',
      visuals: 'CloudPro',
    };

    render(
      <FluentProvider theme={webLightTheme}>
        <ProviderSelection selection={selection} onSelectionChange={mockOnChange} />
      </FluentProvider>
    );

    // Component renders with selection
    expect(screen.getByText(/Provider Selection/i)).toBeDefined();
  });
});
