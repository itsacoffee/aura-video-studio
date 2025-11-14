import type { ConfirmationDialogProps } from './ConfirmationDialog';

/**
 * Hook to use confirmation dialog programmatically
 */
export function useConfirmation() {
  const confirm = (options: Omit<ConfirmationDialogProps, 'trigger' | 'open' | 'onOpenChange'>) => {
    return new Promise<boolean>((resolve) => {
      // This is a simplified version - in production, you'd want to use a
      // dialog context provider for truly imperative dialogs
      const result = window.confirm(`${options.title}\n\n${options.message}`);
      resolve(result);
    });
  };

  return { confirm };
}
