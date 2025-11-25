import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RagToggle } from '../RagToggle';

describe('RagToggle', () => {
  const defaultProps = {
    enabled: false,
    onChange: vi.fn(),
    documentCount: 5,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render with document count', () => {
    render(<RagToggle {...defaultProps} />);

    expect(screen.getByText(/Use Knowledge Base \(5 documents\)/)).toBeInTheDocument();
  });

  it('should call onChange when toggle is clicked', async () => {
    const onChange = vi.fn();
    render(<RagToggle {...defaultProps} onChange={onChange} />);

    const switchElement = screen.getByRole('switch');
    fireEvent.click(switchElement);

    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith(true);
    });
  });

  it('should be disabled when documentCount is 0', () => {
    render(<RagToggle {...defaultProps} documentCount={0} />);

    const switchElement = screen.getByRole('switch');
    expect(switchElement).toBeDisabled();
  });

  it('should show warning when no documents are indexed', () => {
    render(<RagToggle {...defaultProps} documentCount={0} />);

    expect(screen.getByText(/No documents indexed/)).toBeInTheDocument();
  });

  it('should show success message when enabled and documents exist', () => {
    render(<RagToggle {...defaultProps} enabled={true} documentCount={5} />);

    expect(screen.getByText(/Script will be enhanced with relevant context/)).toBeInTheDocument();
  });

  it('should not show success message when disabled', () => {
    render(<RagToggle {...defaultProps} enabled={false} documentCount={5} />);

    expect(
      screen.queryByText(/Script will be enhanced with relevant context/)
    ).not.toBeInTheDocument();
  });
});
