import { Button, ButtonProps, Spinner } from '@fluentui/react-components';
import React, { useState } from 'react';

export interface AsyncButtonProps {
  /**
   * Async click handler
   */
  onClick: () => Promise<void> | void;
  /**
   * Loading text to show when operation is in progress
   */
  loadingText?: string;
  /**
   * Controlled loading state (external)
   */
  loading?: boolean;
  /**
   * Error handler
   */
  onAsyncError?: (error: Error) => void;
  /**
   * Button text/content
   */
  children?: React.ReactNode;
  /**
   * Button disabled state
   */
  disabled?: boolean;
  /**
   * Button appearance
   */
  appearance?: ButtonProps['appearance'];
  /**
   * Button icon
   */
  icon?: ButtonProps['icon'];
  /**
   * Button size
   */
  size?: ButtonProps['size'];
  /**
   * Additional className
   */
  className?: string;
  /**
   * ARIA label
   */
  'aria-label'?: string;
}

/**
 * Button component that handles async operations with loading state
 * Automatically shows spinner and disables button during async operations
 */
export function AsyncButton({
  onClick,
  loadingText,
  loading: externalLoading,
  onAsyncError,
  children,
  disabled,
  icon,
  appearance,
  size,
  className,
  'aria-label': ariaLabel,
}: AsyncButtonProps) {
  const [internalLoading, setInternalLoading] = useState(false);

  const isLoading = externalLoading ?? internalLoading;

  const handleClick = async () => {
    if (isLoading) return;

    try {
      // Only manage internal loading if not controlled externally
      if (externalLoading === undefined) {
        setInternalLoading(true);
      }

      await onClick();
    } catch (error) {
      if (onAsyncError && error instanceof Error) {
        onAsyncError(error);
      } else {
        console.error('AsyncButton error:', error);
      }
    } finally {
      if (externalLoading === undefined) {
        setInternalLoading(false);
      }
    }
  };

  return (
    <Button
      onClick={handleClick}
      disabled={disabled || isLoading}
      icon={isLoading ? <Spinner size="tiny" /> : icon}
      appearance={appearance}
      size={size}
      className={className}
      aria-busy={isLoading}
      aria-disabled={disabled || isLoading}
      aria-label={ariaLabel}
    >
      {isLoading && loadingText ? loadingText : children}
    </Button>
  );
}
