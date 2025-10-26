import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';
import { NotFoundPage } from '../pages/NotFoundPage';

describe('NotFoundPage', () => {
  it('should render 404 error page', () => {
    render(
      <BrowserRouter>
        <FluentProvider theme={webLightTheme}>
          <NotFoundPage />
        </FluentProvider>
      </BrowserRouter>
    );

    expect(screen.getByText('404')).toBeInTheDocument();
    expect(screen.getByText('Page Not Found')).toBeInTheDocument();
  });

  it('should have navigation buttons', () => {
    render(
      <BrowserRouter>
        <FluentProvider theme={webLightTheme}>
          <NotFoundPage />
        </FluentProvider>
      </BrowserRouter>
    );

    expect(screen.getByText('Go to Home')).toBeInTheDocument();
    expect(screen.getByText('Go Back')).toBeInTheDocument();
  });
});
