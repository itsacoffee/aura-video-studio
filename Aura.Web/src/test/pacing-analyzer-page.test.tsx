/**
 * Tests for PacingAnalyzerPage inline analysis
 * Verifies that the PacingAnalyzerPage shows analysis inline instead of
 * navigating to the Create page as per PR3 requirements.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { PacingAnalyzerPage } from '../pages/PacingAnalyzerPage';

// Mock the PacingOptimizerPanel component
vi.mock('../components/PacingAnalysis', () => ({
  PacingOptimizerPanel: ({ onClose }: { onClose: () => void }) => (
    <div data-testid="pacing-optimizer-panel">
      <button onClick={onClose}>Close Panel</button>
    </div>
  ),
}));

describe('PacingAnalyzerPage - Inline Analysis', () => {
  it('should render the analyze pacing button', () => {
    render(
      <BrowserRouter>
        <PacingAnalyzerPage />
      </BrowserRouter>
    );

    const analyzeButton = screen.getByRole('button', { name: /Analyze Pacing/i });
    expect(analyzeButton).toBeDefined();
  });

  it('should show inline panel when analyze button is clicked', () => {
    render(
      <BrowserRouter>
        <PacingAnalyzerPage />
      </BrowserRouter>
    );

    // Initially, the panel should not be visible
    expect(screen.queryByTestId('pacing-optimizer-panel')).toBeNull();

    // Enter some script text
    const textarea = screen.getByPlaceholderText(/Enter your video script/i);
    fireEvent.change(textarea, { target: { value: 'Test script content' } });

    // Click the analyze button
    const analyzeButton = screen.getByRole('button', { name: /Analyze Pacing/i });
    fireEvent.click(analyzeButton);

    // The panel should now be visible
    expect(screen.getByTestId('pacing-optimizer-panel')).toBeDefined();
  });

  it('should close the panel when close button is clicked', () => {
    render(
      <BrowserRouter>
        <PacingAnalyzerPage />
      </BrowserRouter>
    );

    // Enter script text and show panel
    const textarea = screen.getByPlaceholderText(/Enter your video script/i);
    fireEvent.change(textarea, { target: { value: 'Test script content' } });

    const analyzeButton = screen.getByRole('button', { name: /Analyze Pacing/i });
    fireEvent.click(analyzeButton);

    // Panel should be visible
    expect(screen.getByTestId('pacing-optimizer-panel')).toBeDefined();

    // Click close button
    const closeButton = screen.getByRole('button', { name: /Close Panel/i });
    fireEvent.click(closeButton);

    // Panel should be hidden
    expect(screen.queryByTestId('pacing-optimizer-panel')).toBeNull();
  });

  it('should keep analyze button disabled when script is empty', () => {
    render(
      <BrowserRouter>
        <PacingAnalyzerPage />
      </BrowserRouter>
    );

    const analyzeButton = screen.getByRole('button', { name: /Analyze Pacing/i });
    expect(analyzeButton).toBeDisabled();
  });
});
