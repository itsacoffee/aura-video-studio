/**
 * Tests for Loading components
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SkeletonCard } from '../SkeletonCard';
import { SkeletonList } from '../SkeletonList';
import { SkeletonTable } from '../SkeletonTable';
import { ProgressIndicator } from '../ProgressIndicator';
import { AsyncButton } from '../AsyncButton';
import { ErrorState } from '../ErrorState';

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('SkeletonCard', () => {
  it('should render single skeleton card by default', () => {
    renderWithProvider(<SkeletonCard />);
    const cards = screen.getAllByRole('status');
    expect(cards).toHaveLength(1);
  });

  it('should render multiple skeleton cards', () => {
    renderWithProvider(<SkeletonCard count={3} />);
    const cards = screen.getAllByRole('status');
    expect(cards).toHaveLength(3);
  });

  it('should have accessible aria labels', () => {
    renderWithProvider(<SkeletonCard ariaLabel="Loading projects" />);
    expect(screen.getByLabelText('Loading projects')).toBeInTheDocument();
  });

  it('should be marked as busy', () => {
    renderWithProvider(<SkeletonCard />);
    const card = screen.getByRole('status');
    expect(card).toHaveAttribute('aria-busy', 'true');
  });
});

describe('SkeletonList', () => {
  it('should render skeleton list items', () => {
    renderWithProvider(<SkeletonList count={3} />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('should have accessible aria labels', () => {
    renderWithProvider(<SkeletonList ariaLabel="Loading list items" />);
    expect(screen.getByLabelText('Loading list items')).toBeInTheDocument();
  });

  it('should be marked as busy', () => {
    renderWithProvider(<SkeletonList />);
    const list = screen.getByRole('status');
    expect(list).toHaveAttribute('aria-busy', 'true');
  });
});

describe('SkeletonTable', () => {
  it('should render table with headers', () => {
    renderWithProvider(<SkeletonTable columns={['Name', 'Date', 'Status']} />);
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Date')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
  });

  it('should have accessible aria labels', () => {
    renderWithProvider(<SkeletonTable columns={['Col1']} ariaLabel="Loading table" />);
    expect(screen.getByLabelText('Loading table')).toBeInTheDocument();
  });

  it('should be marked as busy', () => {
    renderWithProvider(<SkeletonTable columns={['Col1']} />);
    const table = screen.getByRole('status');
    expect(table).toHaveAttribute('aria-busy', 'true');
  });
});

describe('ProgressIndicator', () => {
  it('should display progress percentage', () => {
    renderWithProvider(<ProgressIndicator progress={45} />);
    expect(screen.getByText('45%')).toBeInTheDocument();
  });

  it('should display title', () => {
    renderWithProvider(<ProgressIndicator progress={50} title="Uploading" />);
    expect(screen.getByText('Uploading')).toBeInTheDocument();
  });

  it('should display status message', () => {
    renderWithProvider(<ProgressIndicator progress={50} status="Processing video..." />);
    expect(screen.getByText('Processing video...')).toBeInTheDocument();
  });

  it('should format time remaining', () => {
    renderWithProvider(<ProgressIndicator progress={50} estimatedTimeRemaining={125} />);
    expect(screen.getByText('2m 5s remaining')).toBeInTheDocument();
  });

  it('should format seconds only', () => {
    renderWithProvider(<ProgressIndicator progress={90} estimatedTimeRemaining={45} />);
    expect(screen.getByText('45s remaining')).toBeInTheDocument();
  });

  it('should have accessible aria labels', () => {
    renderWithProvider(<ProgressIndicator progress={50} ariaLabel="Upload progress" />);
    const statusElement = screen.getByRole('status');
    expect(statusElement).toHaveAttribute('aria-label', 'Upload progress');
  });
});

describe('AsyncButton', () => {
  it('should render button with text', () => {
    renderWithProvider(<AsyncButton onClick={async () => {}}>Click me</AsyncButton>);
    expect(screen.getByText('Click me')).toBeInTheDocument();
  });

  it('should call onClick handler', async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();

    renderWithProvider(<AsyncButton onClick={handleClick}>Click me</AsyncButton>);

    const button = screen.getByRole('button');
    await user.click(button);

    expect(handleClick).toHaveBeenCalled();
  });

  it('should show loading text when loading', () => {
    renderWithProvider(
      <AsyncButton onClick={async () => {}} loading={true} loadingText="Processing...">
        Click me
      </AsyncButton>
    );
    expect(screen.getByText('Processing...')).toBeInTheDocument();
  });

  it('should disable button when loading', () => {
    renderWithProvider(
      <AsyncButton onClick={async () => {}} loading={true}>
        Click me
      </AsyncButton>
    );
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  it('should set aria-busy when loading', () => {
    renderWithProvider(
      <AsyncButton onClick={async () => {}} loading={true}>
        Click me
      </AsyncButton>
    );
    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('aria-busy', 'true');
  });
});

describe('ErrorState', () => {
  it('should display error message', () => {
    renderWithProvider(<ErrorState message="Something went wrong" title="Error occurred" />);
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('should display custom title', () => {
    renderWithProvider(<ErrorState message="Error" title="Custom Error" />);
    expect(screen.getByText('Custom Error')).toBeInTheDocument();
  });

  it('should show retry button', () => {
    const handleRetry = vi.fn();
    renderWithProvider(<ErrorState message="Error" onRetry={handleRetry} />);
    expect(screen.getByText('Try Again')).toBeInTheDocument();
  });

  it('should call retry handler', async () => {
    const handleRetry = vi.fn();
    const user = userEvent.setup();

    renderWithProvider(<ErrorState message="Error" onRetry={handleRetry} />);

    const retryButton = screen.getByText('Try Again');
    await user.click(retryButton);

    expect(handleRetry).toHaveBeenCalled();
  });

  it('should have role alert for accessibility', () => {
    renderWithProvider(<ErrorState message="Error" />);
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('should have accessible aria labels', () => {
    renderWithProvider(<ErrorState message="Error" ariaLabel="Error occurred" />);
    expect(screen.getByLabelText('Error occurred')).toBeInTheDocument();
  });
});
