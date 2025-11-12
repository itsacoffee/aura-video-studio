import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ExampleValidatedForm } from '../ExampleValidatedForm';

describe('ExampleValidatedForm', () => {
  it('should render all form fields', () => {
    render(<ExampleValidatedForm />);

    expect(screen.getByLabelText(/Video Title/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Description/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Duration/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/API Key/i)).toBeInTheDocument();
  });

  it('should show required field indicators', () => {
    render(<ExampleValidatedForm />);

    const titleLabel = screen.getByText(/Video Title/i).closest('label');
    const apiKeyLabel = screen.getByText(/API Key/i).closest('label');

    expect(titleLabel).toBeInTheDocument();
    expect(apiKeyLabel).toBeInTheDocument();
  });

  it('should show validation errors for empty required fields', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Title is required/i)).toBeInTheDocument();
      expect(screen.getByText(/API key is required/i)).toBeInTheDocument();
    });
  });

  it('should show validation error for title too short', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const titleInput = screen.getByLabelText(/Video Title/i);
    await user.type(titleInput, 'ab');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Title must be at least 3 characters/i)).toBeInTheDocument();
    });
  });

  it('should show validation error for title too long', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const titleInput = screen.getByLabelText(/Video Title/i);
    await user.type(titleInput, 'a'.repeat(101));

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Title must be less than 100 characters/i)).toBeInTheDocument();
    });
  });

  it('should show validation error for description too short', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const descriptionInput = screen.getByLabelText(/Description/i);
    await user.type(descriptionInput, 'short');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Description must be at least 10 characters/i)).toBeInTheDocument();
    });
  });

  it('should show validation error for duration out of range', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const durationInput = screen.getByLabelText(/Duration/i);
    await user.clear(durationInput);
    await user.type(durationInput, '5');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Duration must be at least 10 seconds/i)).toBeInTheDocument();
    });
  });

  it('should show validation error for invalid API key format', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const apiKeyInput = screen.getByLabelText(/API Key/i);
    await user.type(apiKeyInput, 'invalid@key!');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/API key contains invalid characters/i)).toBeInTheDocument();
    });
  });

  it('should not submit form with validation errors', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<ExampleValidatedForm onSubmit={onSubmit} />);

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Title is required/i)).toBeInTheDocument();
    });

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('should submit form with valid data', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<ExampleValidatedForm onSubmit={onSubmit} />);

    const titleInput = screen.getByLabelText(/Video Title/i);
    const descriptionInput = screen.getByLabelText(/Description/i);
    const durationInput = screen.getByLabelText(/Duration/i);
    const apiKeyInput = screen.getByLabelText(/API Key/i);

    await user.type(titleInput, 'Test Video');
    await user.type(descriptionInput, 'This is a test video description');
    await user.clear(durationInput);
    await user.type(durationInput, '120');
    await user.type(apiKeyInput, 'test-api-key-123');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledTimes(1);
      expect(onSubmit).toHaveBeenCalledWith({
        title: 'Test Video',
        description: 'This is a test video description',
        duration: 120,
        apiKey: 'test-api-key-123',
      });
    });
  });

  it('should show loading state during submission', async () => {
    const onSubmit = vi
      .fn()
      .mockImplementation(() => new Promise((resolve) => setTimeout(resolve, 100)));
    const user = userEvent.setup();
    render(<ExampleValidatedForm onSubmit={onSubmit} />);

    const titleInput = screen.getByLabelText(/Video Title/i);
    const apiKeyInput = screen.getByLabelText(/API Key/i);

    await user.type(titleInput, 'Test Video');
    await user.type(apiKeyInput, 'test-api-key-123');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    expect(screen.getByText(/Submitting.../i)).toBeInTheDocument();
    expect(submitButton).toBeDisabled();
  });

  it('should show success message after submission', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<ExampleValidatedForm onSubmit={onSubmit} />);

    const titleInput = screen.getByLabelText(/Video Title/i);
    const apiKeyInput = screen.getByLabelText(/API Key/i);

    await user.type(titleInput, 'Test Video');
    await user.type(apiKeyInput, 'test-api-key-123');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Form submitted successfully/i)).toBeInTheDocument();
    });
  });

  it('should reset form when reset button is clicked', async () => {
    const user = userEvent.setup();
    render(<ExampleValidatedForm />);

    const titleInput = screen.getByLabelText(/Video Title/i) as HTMLInputElement;
    const apiKeyInput = screen.getByLabelText(/API Key/i) as HTMLInputElement;

    await user.type(titleInput, 'Test Video');
    await user.type(apiKeyInput, 'test-key');

    expect(titleInput.value).toBe('Test Video');
    expect(apiKeyInput.value).toBe('test-key');

    const resetButton = screen.getByRole('button', { name: /Reset/i });
    await user.click(resetButton);

    await waitFor(() => {
      expect(titleInput.value).toBe('');
      expect(apiKeyInput.value).toBe('');
    });
  });

  it('should disable submit and reset buttons during submission', async () => {
    const onSubmit = vi
      .fn()
      .mockImplementation(() => new Promise((resolve) => setTimeout(resolve, 100)));
    const user = userEvent.setup();
    render(<ExampleValidatedForm onSubmit={onSubmit} />);

    const titleInput = screen.getByLabelText(/Video Title/i);
    const apiKeyInput = screen.getByLabelText(/API Key/i);

    await user.type(titleInput, 'Test Video');
    await user.type(apiKeyInput, 'test-key');

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    const resetButton = screen.getByRole('button', { name: /Reset/i });

    await user.click(submitButton);

    expect(submitButton).toBeDisabled();
    expect(resetButton).toBeDisabled();
  });
});
