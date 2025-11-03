import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PathSelector } from '../../components/common/PathSelector';

describe('PathSelector Component', () => {
  const mockOnChange = vi.fn();
  const mockOnValidate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render with label and input', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          placeholder="Select path"
        />
      </FluentProvider>
    );

    expect(screen.getByText('Test Path')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Select path')).toBeInTheDocument();
  });

  it('should render browse button', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector label="Test Path" value="" onChange={mockOnChange} />
      </FluentProvider>
    );

    expect(screen.getByText('Browse...')).toBeInTheDocument();
  });

  it('should render with directory type', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector label="Test Directory" value="" onChange={mockOnChange} type="directory" />
      </FluentProvider>
    );

    expect(screen.getByText('Test Directory')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Click Browse to select folder')).toBeInTheDocument();
  });

  it('should render with file type', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector label="Test File" value="" onChange={mockOnChange} type="file" />
      </FluentProvider>
    );

    expect(screen.getByText('Test File')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Click Browse to select file')).toBeInTheDocument();
  });

  it('should render auto-detect button when autoDetect prop is provided', () => {
    const mockAutoDetect = vi.fn();

    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          autoDetect={mockAutoDetect}
        />
      </FluentProvider>
    );

    expect(screen.getByText('Auto-Detect')).toBeInTheDocument();
  });

  it('should call onChange when input value changes', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector label="Test Path" value="" onChange={mockOnChange} type="file" />
      </FluentProvider>
    );

    const input = screen.getByPlaceholderText('Click Browse to select file');
    fireEvent.change(input, { target: { value: 'C:\\test\\path.exe' } });

    expect(mockOnChange).toHaveBeenCalledWith('C:\\test\\path.exe');
  });

  it('should show validation result when path is valid', async () => {
    mockOnValidate.mockResolvedValue({
      isValid: true,
      message: 'Valid path',
      version: '1.0.0',
    });

    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value="C:\\test\\path.exe"
          onChange={mockOnChange}
          onValidate={mockOnValidate}
        />
      </FluentProvider>
    );

    await waitFor(
      () => {
        expect(screen.getByText('Valid path')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );
  });

  it('should show error message when path is invalid', async () => {
    mockOnValidate.mockResolvedValue({
      isValid: false,
      message: 'File does not exist',
    });

    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value="C:\\invalid\\path.exe"
          onChange={mockOnChange}
          onValidate={mockOnValidate}
        />
      </FluentProvider>
    );

    await waitFor(
      () => {
        expect(screen.getByText('File does not exist')).toBeInTheDocument();
      },
      { timeout: 1000 }
    );
  });

  it('should display help text when provided', () => {
    const { container } = render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          helpText="This is helpful information"
        />
      </FluentProvider>
    );

    const svg = container.querySelector('svg[aria-describedby]');
    expect(svg).toBeInTheDocument();
  });

  it('should display example path when provided', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          examplePath="C:\example\path.exe"
        />
      </FluentProvider>
    );

    expect(screen.getByText('e.g.,', { exact: false })).toBeInTheDocument();
  });

  it('should display default path when provided', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          defaultPath="C:\\default\\path.exe"
        />
      </FluentProvider>
    );

    expect(screen.getByText('Default:', { exact: false })).toBeInTheDocument();
  });

  it('should disable input and buttons when disabled prop is true', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          disabled={true}
          type="file"
        />
      </FluentProvider>
    );

    const input = screen.getByPlaceholderText('Click Browse to select file');
    const browseButton = screen.getByText('Browse...');

    expect(input).toBeDisabled();
    expect(browseButton).toBeDisabled();
  });

  it('should show clear button when value is present and showClearButton is true', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value="C:\\test\\path.exe"
          onChange={mockOnChange}
          showClearButton={true}
        />
      </FluentProvider>
    );

    expect(screen.getByText('Clear')).toBeInTheDocument();
  });

  it('should show open folder button when value is present and showOpenFolder is true', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value="C:\\test\\path.exe"
          onChange={mockOnChange}
          showOpenFolder={true}
        />
      </FluentProvider>
    );

    expect(screen.getByText('Open')).toBeInTheDocument();
  });

  it('should call onChange with empty string when clear button is clicked', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value="C:\\test\\path.exe"
          onChange={mockOnChange}
          showClearButton={true}
        />
      </FluentProvider>
    );

    const clearButton = screen.getByText('Clear');
    fireEvent.click(clearButton);

    expect(mockOnChange).toHaveBeenCalledWith('');
  });

  it('should call autoDetect when Auto-Detect button is clicked', async () => {
    const mockAutoDetect = vi.fn().mockResolvedValue('C:\\detected\\path.exe');

    render(
      <FluentProvider theme={webLightTheme}>
        <PathSelector
          label="Test Path"
          value=""
          onChange={mockOnChange}
          autoDetect={mockAutoDetect}
        />
      </FluentProvider>
    );

    const autoDetectButton = screen.getByText('Auto-Detect');
    fireEvent.click(autoDetectButton);

    await waitFor(() => {
      expect(mockAutoDetect).toHaveBeenCalled();
      expect(mockOnChange).toHaveBeenCalledWith('C:\\detected\\path.exe');
    });
  });
});
