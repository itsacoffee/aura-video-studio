/**
 * Tests for CreatePolicyModal component
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { CreatePolicyModal } from '../CreatePolicyModal';

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('CreatePolicyModal', () => {
  it('should render modal when open', () => {
    renderWithProvider(<CreatePolicyModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);
    expect(screen.getByText('Create Content Filtering Policy')).toBeInTheDocument();
  });

  it('should not render modal when closed', () => {
    renderWithProvider(<CreatePolicyModal open={false} onOpenChange={() => {}} onSave={vi.fn()} />);
    expect(screen.queryByText('Create Content Filtering Policy')).not.toBeInTheDocument();
  });

  it('should have required fields', () => {
    renderWithProvider(<CreatePolicyModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);
    expect(screen.getByText('Policy Name')).toBeInTheDocument();
    expect(screen.getByText('Description')).toBeInTheDocument();
    expect(screen.getByText('Enable Content Filtering')).toBeInTheDocument();
  });

  it('should disable create button when name is empty', () => {
    renderWithProvider(<CreatePolicyModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);
    const createButton = screen.getByRole('button', { name: /create policy/i });
    expect(createButton).toBeDisabled();
  });

  it('should enable create button when name is provided', async () => {
    const user = userEvent.setup();
    renderWithProvider(<CreatePolicyModal open={true} onOpenChange={() => {}} onSave={vi.fn()} />);

    const nameInput = screen.getByPlaceholderText(/family-friendly content/i);
    await user.type(nameInput, 'Test Policy');

    const createButton = screen.getByRole('button', { name: /create policy/i });
    expect(createButton).toBeEnabled();
  });

  it('should call onSave with policy data', async () => {
    const mockOnSave = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderWithProvider(
      <CreatePolicyModal open={true} onOpenChange={() => {}} onSave={mockOnSave} />
    );

    const nameInput = screen.getByPlaceholderText(/family-friendly content/i);
    await user.clear(nameInput);
    await user.type(nameInput, 'MyTest');

    const createButton = screen.getByRole('button', { name: /create policy/i });
    await user.click(createButton);

    expect(mockOnSave).toHaveBeenCalledTimes(1);
    const callArg = mockOnSave.mock.calls[0][0];
    expect(callArg.filteringEnabled).toBe(true);
    expect(callArg.name).toContain('My');
  });

  it('should call onOpenChange when cancel is clicked', async () => {
    const mockOnOpenChange = vi.fn();
    const user = userEvent.setup();

    renderWithProvider(
      <CreatePolicyModal open={true} onOpenChange={mockOnOpenChange} onSave={vi.fn()} />
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    await user.click(cancelButton);

    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
  });
});
