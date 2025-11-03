/**
 * Tests for TranslationPage component
 * Verifies error handling and fallback behavior
 */

import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fallbackLanguages } from '../../../data/fallbackLanguages';
import * as localizationApi from '../../../services/api/localizationApi';
import { TranslationPage } from '../TranslationPage';

// Mock the localization API
vi.mock('../../../services/api/localizationApi');

// Mock the components that aren't the focus of these tests
vi.mock('../components/BatchTranslationQueue', () => ({
  BatchTranslationQueue: () => (
    <div data-testid="batch-translation-queue">Batch Translation Queue</div>
  ),
}));

vi.mock('../components/GlossaryManager', () => ({
  GlossaryManager: () => <div data-testid="glossary-manager">Glossary Manager</div>,
}));

vi.mock('../components/TranslationResult', () => ({
  TranslationResult: () => <div data-testid="translation-result">Translation Result</div>,
}));

const renderWithRouter = (component: React.ReactElement) => {
  return render(<BrowserRouter>{component}</BrowserRouter>);
};

describe('TranslationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render with fallback languages when API call fails', async () => {
    // Mock API to fail
    vi.mocked(localizationApi.getSupportedLanguages).mockRejectedValue(
      new Error('API not available')
    );

    renderWithRouter(<TranslationPage />);

    // Wait for the component to finish loading
    await waitFor(() => {
      expect(screen.queryByText(/Loading/i)).not.toBeInTheDocument();
    });

    // Should show warning banner
    await waitFor(() => {
      expect(
        screen.getByText(/Unable to load full language list from server/i)
      ).toBeInTheDocument();
    });

    // Should show fallback language count
    expect(
      screen.getByText(new RegExp(`Using ${fallbackLanguages.length} offline languages`, 'i'))
    ).toBeInTheDocument();

    // Should still render the page with language dropdowns
    expect(screen.getByText(/Source Language/i)).toBeInTheDocument();
    expect(screen.getByText(/Target Language/i)).toBeInTheDocument();
  });

  it('should not show warning banner when languages load successfully', async () => {
    // Mock successful API response
    const mockLanguages = [
      {
        code: 'en',
        name: 'English',
        nativeName: 'English',
        region: 'Global',
        isRightToLeft: false,
        defaultFormality: 'Neutral',
        typicalExpansionFactor: 1.0,
      },
      {
        code: 'es',
        name: 'Spanish',
        nativeName: 'Español',
        region: 'Global',
        isRightToLeft: false,
        defaultFormality: 'Neutral',
        typicalExpansionFactor: 1.15,
      },
    ];

    vi.mocked(localizationApi.getSupportedLanguages).mockResolvedValue(mockLanguages);

    renderWithRouter(<TranslationPage />);

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.queryByText(/Loading/i)).not.toBeInTheDocument();
    });

    // Should NOT show warning banner
    expect(
      screen.queryByText(/Unable to load full language list from server/i)
    ).not.toBeInTheDocument();

    // Should render the page normally
    expect(screen.getByText(/Translation & Localization/i)).toBeInTheDocument();
  });

  it('should allow retry when language loading fails', async () => {
    const user = userEvent.setup();

    // Mock API to fail initially
    vi.mocked(localizationApi.getSupportedLanguages)
      .mockRejectedValueOnce(new Error('API not available'))
      .mockResolvedValueOnce([
        {
          code: 'en',
          name: 'English',
          nativeName: 'English',
          region: 'Global',
          isRightToLeft: false,
          defaultFormality: 'Neutral',
          typicalExpansionFactor: 1.0,
        },
      ]);

    renderWithRouter(<TranslationPage />);

    // Wait for error state
    await waitFor(() => {
      expect(
        screen.getByText(/Unable to load full language list from server/i)
      ).toBeInTheDocument();
    });

    // Find and click retry button
    const retryButton = screen.getByText(/Retry/i);
    await user.click(retryButton);

    // Wait for successful load
    await waitFor(() => {
      expect(
        screen.queryByText(/Unable to load full language list from server/i)
      ).not.toBeInTheDocument();
    });

    // Verify API was called twice
    expect(localizationApi.getSupportedLanguages).toHaveBeenCalledTimes(2);
  });

  it('should render tab navigation', async () => {
    // Mock successful API response
    vi.mocked(localizationApi.getSupportedLanguages).mockResolvedValue([]);

    renderWithRouter(<TranslationPage />);

    // Wait for component to render
    await waitFor(() => {
      expect(screen.getAllByText(/Single Translation/i).length).toBeGreaterThan(0);
    });

    // Check all tabs are present using role
    const tabs = screen.getAllByRole('tab');
    expect(tabs.length).toBe(3);

    // Verify tab labels
    const tabLabels = tabs.map((tab) => tab.textContent);
    expect(tabLabels.some((label) => label?.includes('Single Translation'))).toBe(true);
    expect(tabLabels.some((label) => label?.includes('Batch Translation'))).toBe(true);
    expect(tabLabels.some((label) => label?.includes('Glossary Management'))).toBe(true);
  });

  it('should switch tabs', async () => {
    const user = userEvent.setup();

    // Mock successful API response
    vi.mocked(localizationApi.getSupportedLanguages).mockResolvedValue([]);

    renderWithRouter(<TranslationPage />);

    await waitFor(() => {
      expect(screen.getByText(/Single Translation/i)).toBeInTheDocument();
    });

    // Initially on translate tab
    expect(screen.getByText(/Translate Content/i)).toBeInTheDocument();

    // Click batch tab - use getAllByRole to find tabs specifically
    const tabs = screen.getAllByRole('tab');
    const batchTab = tabs.find((tab) => tab.textContent?.includes('Batch Translation'));
    expect(batchTab).toBeDefined();
    await user.click(batchTab!);

    // Should show batch translation content
    await waitFor(() => {
      expect(screen.getByTestId('batch-translation-queue')).toBeInTheDocument();
    });

    // Click glossary tab
    const glossaryTab = tabs.find((tab) => tab.textContent?.includes('Glossary Management'));
    expect(glossaryTab).toBeDefined();
    await user.click(glossaryTab!);

    // Should show glossary manager
    await waitFor(() => {
      expect(screen.getByTestId('glossary-manager')).toBeInTheDocument();
    });
  });

  it('should use fallback languages immediately on mount', async () => {
    // Mock API to fail (simulating network error)
    vi.mocked(localizationApi.getSupportedLanguages).mockRejectedValue(new Error('Network error'));

    renderWithRouter(<TranslationPage />);

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.queryByText(/Loading/i)).not.toBeInTheDocument();
    });

    // Should show dropdowns with fallback languages
    expect(screen.getByText(/Source Language/i)).toBeInTheDocument();
    expect(screen.getByText(/Target Language/i)).toBeInTheDocument();

    // Dropdowns should not be disabled even though API failed
    const dropdowns = screen.getAllByRole('combobox');
    dropdowns.forEach((dropdown) => {
      expect(dropdown).not.toBeDisabled();
    });

    // Should show warning banner
    expect(screen.getByText(/Unable to load full language list from server/i)).toBeInTheDocument();
  });
});
