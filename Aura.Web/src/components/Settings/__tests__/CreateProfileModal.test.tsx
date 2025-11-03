/**
 * Tests for CreateProfileModal component
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { CreateProfileModal } from '../CreateProfileModal';

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('CreateProfileModal', () => {
  it('should render modal when open', () => {
    renderWithProvider(<CreateProfileModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);
    expect(screen.getByText('Create Custom Audience Profile')).toBeInTheDocument();
  });

  it('should not render modal when closed', () => {
    renderWithProvider(
      <CreateProfileModal open={false} onOpenChange={() => {}} onSave={vi.fn()} />
    );
    expect(screen.queryByText('Create Custom Audience Profile')).not.toBeInTheDocument();
  });

  it('should have required fields', () => {
    renderWithProvider(<CreateProfileModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);
    expect(screen.getByText('Profile Name')).toBeInTheDocument();
    expect(screen.getByText('Description')).toBeInTheDocument();
    expect(screen.getByText('Minimum Age')).toBeInTheDocument();
    expect(screen.getByText('Maximum Age')).toBeInTheDocument();
  });

  it('should disable create button when name is empty', () => {
    renderWithProvider(<CreateProfileModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);
    const createButton = screen.getByRole('button', { name: /create profile/i });
    expect(createButton).toBeDisabled();
  });

  it('should enable create button when name is provided', async () => {
    const user = userEvent.setup();
    renderWithProvider(<CreateProfileModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);

    const nameInput = screen.getByPlaceholderText(/tech-savvy millennials/i);
    await user.type(nameInput, 'Test Profile');

    const createButton = screen.getByRole('button', { name: /create profile/i });
    expect(createButton).toBeEnabled();
  });

  it('should call onSave with profile data', async () => {
    const mockOnSave = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderWithProvider(
      <CreateProfileModal open={true} onOpenChange={() => {}} onSave={mockOnSave} />
    );

    const nameInput = screen.getByPlaceholderText(/tech-savvy millennials/i);
    await user.clear(nameInput);
    await user.type(nameInput, 'MyTest');

    const createButton = screen.getByRole('button', { name: /create profile/i });
    await user.click(createButton);

    expect(mockOnSave).toHaveBeenCalledTimes(1);
    const callArg = mockOnSave.mock.calls[0][0];
    expect(callArg.isCustom).toBe(true);
    expect(callArg.name).toContain('My');
  });

  it('should call onOpenChange when cancel is clicked', async () => {
    const mockOnOpenChange = vi.fn();
    const user = userEvent.setup();

    renderWithProvider(
      <CreateProfileModal open={true} onOpenChange={mockOnOpenChange} onSave={vi.fn()} />
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    await user.click(cancelButton);

    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
  });
});
