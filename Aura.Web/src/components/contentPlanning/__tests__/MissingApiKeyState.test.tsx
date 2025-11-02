import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { MissingApiKeyState } from '../MissingApiKeyState';

const renderWithRouter = (component: React.ReactElement) => {
  return render(<BrowserRouter>{component}</BrowserRouter>);
};

describe('MissingApiKeyState', () => {
  it('should render feature name and default providers', () => {
    renderWithRouter(<MissingApiKeyState featureName="Test Feature" />);

    expect(screen.getByText('API Key Required')).toBeInTheDocument();
    expect(screen.getByText(/Test Feature requires an API key/)).toBeInTheDocument();
    expect(screen.getByText(/OpenAI/)).toBeInTheDocument();
    expect(screen.getByText(/Anthropic \(Claude\)/)).toBeInTheDocument();
    expect(screen.getByText(/Google \(Gemini\)/)).toBeInTheDocument();
  });

  it('should render custom description when provided', () => {
    const customDescription = 'This is a custom description for the feature.';
    renderWithRouter(
      <MissingApiKeyState featureName="Test Feature" description={customDescription} />
    );

    expect(screen.getByText(customDescription)).toBeInTheDocument();
  });

  it('should render custom providers when provided', () => {
    renderWithRouter(
      <MissingApiKeyState
        featureName="Test Feature"
        requiredProviders={['Custom Provider 1', 'Custom Provider 2']}
      />
    );

    expect(screen.getByText(/Custom Provider 1/)).toBeInTheDocument();
    expect(screen.getByText(/Custom Provider 2/)).toBeInTheDocument();
  });

  it('should render Configure API Keys button', () => {
    renderWithRouter(<MissingApiKeyState featureName="Test Feature" />);

    const button = screen.getByRole('button', { name: /Configure API Keys/i });
    expect(button).toBeInTheDocument();
  });
});
