import { renderHook, RenderHookOptions, RenderHookResult } from '@testing-library/react';
import { ReactNode } from 'react';
import { FluentProvider, webLightTheme, Theme } from '@fluentui/react-components';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createTestQueryClient } from './testUtils';

/**
 * Custom render hook options
 */
interface CustomRenderHookOptions<Props> extends RenderHookOptions<Props> {
  theme?: Theme;
  withQueryClient?: boolean;
  queryClient?: QueryClient;
}

/**
 * Render a hook with common providers
 * 
 * @example
 * ```ts
 * const { result } = renderHookWithProviders(() => useMyHook());
 * expect(result.current.value).toBe(42);
 * ```
 * 
 * @example with initial props
 * ```ts
 * const { result, rerender } = renderHookWithProviders(
 *   ({ id }) => useProject(id),
 *   { initialProps: { id: '123' } }
 * );
 * ```
 */
export function renderHookWithProviders<Result, Props>(
  hook: (props: Props) => Result,
  {
    theme = webLightTheme,
    withQueryClient = true,
    queryClient,
    ...options
  }: CustomRenderHookOptions<Props> = {} as CustomRenderHookOptions<Props>
): RenderHookResult<Result, Props> {
  const testQueryClient = queryClient ?? createTestQueryClient();

  function Wrapper({ children }: { children: ReactNode }) {
    const fluentContent = <FluentProvider theme={theme}>{children}</FluentProvider>;
    
    if (withQueryClient) {
      return <QueryClientProvider client={testQueryClient}>{fluentContent}</QueryClientProvider>;
    }

    return fluentContent;
  }

  return renderHook(hook, {
    wrapper: Wrapper,
    ...options,
  });
}

/**
 * Wait for hook to update
 */
export { waitFor } from '@testing-library/react';

/**
 * Act utility for hook updates
 */
export { act } from '@testing-library/react';
