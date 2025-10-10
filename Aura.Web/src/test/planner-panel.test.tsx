import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { PlannerPanel } from '../components/PlannerPanel';
import type { PlannerRecommendations } from '../types';

describe('PlannerPanel', () => {
  const mockRecommendations: PlannerRecommendations = {
    outline: '# Test Video\n\n## Introduction\nTest intro\n\n## Main Content\nTest content\n\n## Conclusion\nTest conclusion',
    sceneCount: 5,
    shotsPerScene: 3,
    bRollPercentage: 30,
    overlayDensity: 5,
    readingLevel: 12,
    voice: {
      rate: 1.0,
      pitch: 1.0,
      style: 'Conversational',
    },
    music: {
      tempo: 'Moderate',
      intensityCurve: 'Rising',
      genre: 'Corporate',
    },
    captions: {
      position: 'Bottom',
      fontSize: 'Large',
      highlightKeywords: true,
    },
    thumbnailPrompt: 'A professional image showing test content',
    seo: {
      title: 'Test Video Title',
      description: 'This is a test video description',
      tags: ['test', 'video', 'demo'],
    },
    qualityScore: 0.85,
    providerUsed: 'OpenAI',
    explainabilityNotes: 'Generated using OpenAI with test parameters',
  };

  it('should render loading state', () => {
    render(<PlannerPanel recommendations={null} loading={true} />);
    expect(screen.getByText('Generating recommendations...')).toBeTruthy();
  });

  it('should render recommendations when provided', () => {
    render(<PlannerPanel recommendations={mockRecommendations} />);
    
    // Check outline is displayed - look for the full markdown text in the display div
    const outlineElements = screen.getAllByText(/Test Video/);
    expect(outlineElements.length).toBeGreaterThan(0);
    
    // Check metrics are displayed
    expect(screen.getByText('Scene Count')).toBeTruthy();
    expect(screen.getByText('5')).toBeTruthy();
    expect(screen.getByText('Shots per Scene')).toBeTruthy();
    
    // Check SEO section - use exact match for title
    expect(screen.getByText('Test Video Title')).toBeTruthy();
    expect(screen.getByText('This is a test video description')).toBeTruthy();
    
    // Check tags
    expect(screen.getByText('test')).toBeTruthy();
    expect(screen.getByText('video')).toBeTruthy();
  });

  it('should display quality score badge', () => {
    render(<PlannerPanel recommendations={mockRecommendations} />);
    expect(screen.getByText('Excellent')).toBeTruthy();
  });

  it('should display provider name', () => {
    render(<PlannerPanel recommendations={mockRecommendations} />);
    expect(screen.getByText('OpenAI')).toBeTruthy();
  });

  it('should display explainability notes', () => {
    render(<PlannerPanel recommendations={mockRecommendations} />);
    expect(screen.getByText(/Generated using OpenAI with test parameters/)).toBeTruthy();
  });

  it('should enter edit mode when Edit button is clicked', async () => {
    render(<PlannerPanel recommendations={mockRecommendations} />);
    
    const editButton = screen.getByRole('button', { name: /Edit/i });
    fireEvent.click(editButton);
    
    // Check textarea appears
    await waitFor(() => {
      const textarea = screen.getByRole('textbox');
      expect(textarea).toBeTruthy();
      expect((textarea as HTMLTextAreaElement).value).toContain('Test Video');
    });
    
    // Check action buttons appear
    expect(screen.getByRole('button', { name: /Save Changes/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Cancel/i })).toBeTruthy();
  });

  it('should call onOutlineChange when outline is edited and saved', async () => {
    const onOutlineChange = vi.fn();
    render(
      <PlannerPanel
        recommendations={mockRecommendations}
        onOutlineChange={onOutlineChange}
      />
    );
    
    // Enter edit mode
    const editButton = screen.getByRole('button', { name: /Edit/i });
    fireEvent.click(editButton);
    
    // Edit the outline
    await waitFor(() => {
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, {
        target: { value: '# Modified Outline\n\nNew content here' },
      });
    });
    
    // Save changes
    const saveButton = screen.getByRole('button', { name: /Save Changes/i });
    fireEvent.click(saveButton);
    
    // Check callback was called with new value
    await waitFor(() => {
      expect(onOutlineChange).toHaveBeenCalledWith('# Modified Outline\n\nNew content here');
    });
  });

  it('should cancel editing without calling onOutlineChange', async () => {
    const onOutlineChange = vi.fn();
    render(
      <PlannerPanel
        recommendations={mockRecommendations}
        onOutlineChange={onOutlineChange}
      />
    );
    
    // Enter edit mode
    const editButton = screen.getByRole('button', { name: /Edit/i });
    fireEvent.click(editButton);
    
    // Edit the outline
    await waitFor(() => {
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, {
        target: { value: '# Modified Outline' },
      });
    });
    
    // Cancel
    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    fireEvent.click(cancelButton);
    
    // Check callback was NOT called
    expect(onOutlineChange).not.toHaveBeenCalled();
    
    // Check original outline is still displayed - use getAllByText and check the outline container
    const outlineElements = screen.getAllByText(/Test Video/);
    // The outline display should be present (not in a textarea)
    expect(outlineElements.length).toBeGreaterThan(0);
  });

  it('should persist edited outline across edit sessions', async () => {
    const onOutlineChange = vi.fn();
    const { rerender } = render(
      <PlannerPanel
        recommendations={mockRecommendations}
        onOutlineChange={onOutlineChange}
      />
    );
    
    // First edit
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));
    await waitFor(() => {
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, {
        target: { value: '# First Edit' },
      });
    });
    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }));
    
    // Update recommendations with the new outline
    const updatedRecommendations = {
      ...mockRecommendations,
      outline: '# First Edit',
    };
    
    rerender(
      <PlannerPanel
        recommendations={updatedRecommendations}
        onOutlineChange={onOutlineChange}
      />
    );
    
    // Verify new outline is displayed - should only have the new text now
    const firstEditElements = screen.getAllByText('# First Edit');
    expect(firstEditElements.length).toBeGreaterThan(0);
  });

  it('should call onAccept when accept button is clicked', () => {
    const onAccept = vi.fn();
    render(
      <PlannerPanel
        recommendations={mockRecommendations}
        onAccept={onAccept}
      />
    );
    
    const acceptButton = screen.getByRole('button', { name: /Accept & Continue/i });
    fireEvent.click(acceptButton);
    
    expect(onAccept).toHaveBeenCalledOnce();
  });

  it('should not render accept button when onAccept is not provided', () => {
    render(<PlannerPanel recommendations={mockRecommendations} />);
    
    const acceptButton = screen.queryByRole('button', { name: /Accept & Continue/i });
    expect(acceptButton).toBeNull();
  });

  it('should show different quality labels based on score', () => {
    const scenarios = [
      { score: 0.90, label: 'Excellent' },
      { score: 0.80, label: 'Good' },
      { score: 0.72, label: 'Fair' },
      { score: 0.60, label: 'Basic' },
    ];
    
    scenarios.forEach(({ score, label }) => {
      const recs = { ...mockRecommendations, qualityScore: score };
      const { unmount } = render(<PlannerPanel recommendations={recs} />);
      expect(screen.getByText(label)).toBeTruthy();
      unmount();
    });
  });
});
