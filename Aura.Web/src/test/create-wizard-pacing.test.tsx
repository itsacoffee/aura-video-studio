/**
 * Tests for Create Wizard pacing button removal
 * Verifies that the "Analyze Pacing" button is not present in the Create Wizard
 * as per PR3 requirements - pacing analysis should only be available where
 * actual content (scripts/scenes) exists.
 */

import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';
import { CreateWizard } from '../pages/Wizard/CreateWizard';

describe('CreateWizard - Pacing Button Removal', () => {
  it('should not render "Analyze Pacing" button in the wizard', () => {
    render(
      <BrowserRouter>
        <CreateWizard />
      </BrowserRouter>
    );

    // The "Analyze Pacing" button should not be present anywhere in the Create Wizard
    const pacingButton = screen.queryByText(/Analyze Pacing/i);
    expect(pacingButton).toBeNull();
  });

  it('should not have FlashFlow icon that was associated with pacing button', () => {
    const { container } = render(
      <BrowserRouter>
        <CreateWizard />
      </BrowserRouter>
    );

    // Check that there's no pacing-related content in step 2
    // The FlashFlow icon was removed along with the pacing button
    const flashFlowIcons = container.querySelectorAll('[data-icon-name="FlashFlow24Regular"]');
    expect(flashFlowIcons.length).toBe(0);
  });
});
