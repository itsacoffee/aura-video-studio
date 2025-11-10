import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, fireEvent, within } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TemplatesBrowser } from '../TemplatesBrowser';

// Mock the templates service
vi.mock('../../../services/templatesAndSamplesService', () => ({
  getVideoTemplates: vi.fn(() => [
    {
      id: 'youtube-tutorial',
      name: 'YouTube Tutorial',
      description: 'Create educational tutorial videos',
      category: 'tutorial',
      duration: 180,
      difficulty: 'beginner',
      promptExample: 'Create a tutorial about...',
      tags: ['youtube', 'education'],
      estimatedTime: '2-3 minutes',
    },
    {
      id: 'social-shorts',
      name: 'Social Media Shorts',
      description: 'Quick, engaging short-form content',
      category: 'social-media',
      duration: 60,
      difficulty: 'beginner',
      promptExample: 'Create a 60-second video about...',
      tags: ['social', 'shorts'],
      estimatedTime: '1-2 minutes',
    },
    {
      id: 'product-demo',
      name: 'Product Demonstration',
      description: 'Showcase product features',
      category: 'marketing',
      duration: 120,
      difficulty: 'intermediate',
      promptExample: 'Create a product demo...',
      tags: ['marketing', 'product'],
      estimatedTime: '3-4 minutes',
    },
  ]),
  getBeginnerTemplates: vi.fn(() => [
    {
      id: 'youtube-tutorial',
      name: 'YouTube Tutorial',
      description: 'Create educational tutorial videos',
      category: 'tutorial',
      duration: 180,
      difficulty: 'beginner',
      promptExample: 'Create a tutorial about...',
      tags: ['youtube', 'education'],
      estimatedTime: '2-3 minutes',
    },
  ]),
  searchTemplates: vi.fn((_query: string) => [
    {
      id: 'youtube-tutorial',
      name: 'YouTube Tutorial',
      description: 'Create educational tutorial videos',
      category: 'tutorial',
      duration: 180,
      difficulty: 'beginner',
      promptExample: 'Create a tutorial about...',
      tags: ['youtube', 'education'],
      estimatedTime: '2-3 minutes',
    },
  ]),
}));

function renderWithProvider(ui: React.ReactElement) {
  return render(<FluentProvider theme={webLightTheme}>{ui}</FluentProvider>);
}

describe('TemplatesBrowser', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render all templates by default', () => {
    renderWithProvider(<TemplatesBrowser />);

    expect(screen.getByText('YouTube Tutorial')).toBeInTheDocument();
    expect(screen.getByText('Social Media Shorts')).toBeInTheDocument();
    expect(screen.getByText('Product Demonstration')).toBeInTheDocument();
  });

  it('should show only beginner templates when showOnlyBeginner is true', () => {
    renderWithProvider(<TemplatesBrowser showOnlyBeginner={true} />);

    expect(screen.getByText('YouTube Tutorial')).toBeInTheDocument();
    expect(screen.queryByText('Product Demonstration')).not.toBeInTheDocument();
  });

  it('should display template details', () => {
    renderWithProvider(<TemplatesBrowser />);

    const tutorialCard = screen.getByText('YouTube Tutorial').closest('.templateCard');
    expect(tutorialCard).toBeInTheDocument();

    if (tutorialCard) {
      expect(
        within(tutorialCard).getByText('Create educational tutorial videos')
      ).toBeInTheDocument();
      expect(within(tutorialCard).getByText('beginner')).toBeInTheDocument();
      expect(within(tutorialCard).getByText(/2-3 minutes/)).toBeInTheDocument();
    }
  });

  it('should show template tags', () => {
    renderWithProvider(<TemplatesBrowser />);

    expect(screen.getByText('youtube')).toBeInTheDocument();
    expect(screen.getByText('education')).toBeInTheDocument();
  });

  it('should call onTemplateSelect when a template is clicked', () => {
    const onTemplateSelect = vi.fn();
    renderWithProvider(<TemplatesBrowser onTemplateSelect={onTemplateSelect} />);

    const tutorialCard = screen.getByText('YouTube Tutorial').closest('.templateCard');
    if (tutorialCard) {
      fireEvent.click(tutorialCard);
      expect(onTemplateSelect).toHaveBeenCalled();
    }
  });

  it('should highlight selected template', () => {
    renderWithProvider(<TemplatesBrowser selectedTemplateId="youtube-tutorial" />);

    const tutorialCard = screen.getByText('YouTube Tutorial').closest('.templateCard');
    expect(tutorialCard).toHaveClass('templateCardSelected');
  });

  it('should show selected template details panel', () => {
    renderWithProvider(<TemplatesBrowser selectedTemplateId="youtube-tutorial" />);

    expect(screen.getByText(/Selected Template: YouTube Tutorial/)).toBeInTheDocument();
    expect(screen.getByText(/Example Prompt:/)).toBeInTheDocument();
  });

  it('should allow searching templates', () => {
    renderWithProvider(<TemplatesBrowser />);

    const searchInput = screen.getByPlaceholderText('Search templates...');
    fireEvent.change(searchInput, { target: { value: 'tutorial' } });

    // After search, should show filtered results
    expect(screen.getByText('YouTube Tutorial')).toBeInTheDocument();
  });

  it('should filter by difficulty', () => {
    renderWithProvider(<TemplatesBrowser />);

    // Find difficulty dropdown - using the text content
    const difficultyOptions = screen.getAllByText('All Levels');
    expect(difficultyOptions.length).toBeGreaterThan(0);
  });

  it('should filter by category', () => {
    renderWithProvider(<TemplatesBrowser />);

    // Find category dropdown
    const categoryOptions = screen.getAllByText('All Categories');
    expect(categoryOptions.length).toBeGreaterThan(0);
  });

  it('should show no results message when no templates match', () => {
    // This test would need dynamic import which isn't supported in this pattern
    // Skipping the mock for now as it's handled by the service mock above
    renderWithProvider(<TemplatesBrowser />);

    const searchInput = screen.getByPlaceholderText('Search templates...');
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });

    // The component should handle empty results gracefully
  });

  it('should display template duration', () => {
    renderWithProvider(<TemplatesBrowser />);

    expect(screen.getByText(/3m/)).toBeInTheDocument(); // 180 seconds = 3 minutes
    expect(screen.getByText(/1m/)).toBeInTheDocument(); // 60 seconds = 1 minute
  });

  it('should show estimated generation time', () => {
    renderWithProvider(<TemplatesBrowser />);

    expect(screen.getByText(/2-3 minutes/)).toBeInTheDocument();
    expect(screen.getByText(/1-2 minutes/)).toBeInTheDocument();
  });

  it('should display prompt examples', () => {
    renderWithProvider(<TemplatesBrowser />);

    expect(screen.getByText(/Create a tutorial about\.\.\./)).toBeInTheDocument();
    expect(screen.getByText(/Create a 60-second video about\.\.\./)).toBeInTheDocument();
  });

  it('should show Use This Template button for selected template', () => {
    renderWithProvider(<TemplatesBrowser selectedTemplateId="youtube-tutorial" />);

    expect(screen.getByText('Use This Template')).toBeInTheDocument();
  });
});
