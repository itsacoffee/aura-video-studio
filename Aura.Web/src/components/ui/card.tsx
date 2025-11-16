import { Card as FluentCard } from '@fluentui/react-components';
import type { CardProps as FluentCardProps } from '@fluentui/react-components';
import React, { type ReactNode } from 'react';

export interface CardProps extends FluentCardProps {
  children: ReactNode;
}

export function Card({ children, ...props }: CardProps) {
  return <FluentCard {...props}>{children}</FluentCard>;
}

export interface CardHeaderProps {
  children: ReactNode;
  className?: string;
}

export function CardHeader({ children, className }: CardHeaderProps) {
  return <div className={className}>{children}</div>;
}

export interface CardTitleProps {
  children: ReactNode;
  className?: string;
}

export function CardTitle({ children, className }: CardTitleProps) {
  return <h3 className={className}>{children}</h3>;
}

export interface CardContentProps {
  children: ReactNode;
  className?: string;
}

export function CardContent({ children, className }: CardContentProps) {
  return <div className={className}>{children}</div>;
}
