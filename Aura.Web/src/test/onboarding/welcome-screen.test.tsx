import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WelcomeScreen } from '../../components/Onboarding/WelcomeScreen';

describe('WelcomeScreen', () => {
  const renderWithProvider = (component: React.ReactElement) => {
    return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
  };

  it('should render welcome message and branding', () => {
    const mockOnGetStarted = vi.fn();
    
    renderWithProvider(<WelcomeScreen onGetStarted={mockOnGetStarted} />);

    expect(screen.getByText(/Welcome to Aura Video Studio!/i)).toBeInTheDocument();
    expect(screen.getByText(/all-in-one platform/i)).toBeInTheDocument();
  });

  it('should display value propositions', () => {
    const mockOnGetStarted = vi.fn();
    
    renderWithProvider(<WelcomeScreen onGetStarted={mockOnGetStarted} />);

    expect(screen.getByText(/Create Professional Videos/i)).toBeInTheDocument();
    expect(screen.getByText(/AI-Powered Automation/i)).toBeInTheDocument();
    expect(screen.getByText(/Save Valuable Time/i)).toBeInTheDocument();
  });

  it('should display time estimate', () => {
    const mockOnGetStarted = vi.fn();
    
    renderWithProvider(<WelcomeScreen onGetStarted={mockOnGetStarted} />);

    expect(screen.getByText(/3-5 minutes/i)).toBeInTheDocument();
    expect(screen.getByText(/pause and resume/i)).toBeInTheDocument();
  });

  it('should call onGetStarted when Get Started button is clicked', async () => {
    const user = userEvent.setup();
    const mockOnGetStarted = vi.fn();
    
    renderWithProvider(<WelcomeScreen onGetStarted={mockOnGetStarted} />);

    const getStartedButton = screen.getByRole('button', { name: /Get Started/i });
    await user.click(getStartedButton);

    expect(mockOnGetStarted).toHaveBeenCalledTimes(1);
  });

  it('should show Import Project button when handler is provided', () => {
    const mockOnGetStarted = vi.fn();
    const mockOnImportProject = vi.fn();
    
    renderWithProvider(
      <WelcomeScreen 
        onGetStarted={mockOnGetStarted} 
        onImportProject={mockOnImportProject}
      />
    );

    const importButton = screen.getByRole('button', { name: /Import Existing Project/i });
    expect(importButton).toBeInTheDocument();
  });

  it('should not show Import Project button when handler is not provided', () => {
    const mockOnGetStarted = vi.fn();
    
    renderWithProvider(<WelcomeScreen onGetStarted={mockOnGetStarted} />);

    const importButton = screen.queryByRole('button', { name: /Import Existing Project/i });
    expect(importButton).not.toBeInTheDocument();
  });

  it('should call onImportProject when Import button is clicked', async () => {
    const user = userEvent.setup();
    const mockOnGetStarted = vi.fn();
    const mockOnImportProject = vi.fn();
    
    renderWithProvider(
      <WelcomeScreen 
        onGetStarted={mockOnGetStarted} 
        onImportProject={mockOnImportProject}
      />
    );

    const importButton = screen.getByRole('button', { name: /Import Existing Project/i });
    await user.click(importButton);

    expect(mockOnImportProject).toHaveBeenCalledTimes(1);
  });
});
