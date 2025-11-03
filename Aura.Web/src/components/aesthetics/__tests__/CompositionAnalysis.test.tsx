import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import CompositionAnalysis from '../CompositionAnalysis';

describe('CompositionAnalysis', () => {
  it('renders without crashing', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <CompositionAnalysis />
      </FluentProvider>
    );

    expect(screen.getByText('Composition Analysis')).toBeInTheDocument();
  });

  it('displays form fields for image dimensions', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <CompositionAnalysis />
      </FluentProvider>
    );

    expect(screen.getByText('Image Width (pixels)')).toBeInTheDocument();
    expect(screen.getByText('Image Height (pixels)')).toBeInTheDocument();
  });

  it('displays analyze button', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <CompositionAnalysis />
      </FluentProvider>
    );

    expect(screen.getByText('Analyze Composition')).toBeInTheDocument();
  });
});
