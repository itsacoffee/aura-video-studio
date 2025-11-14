# Architecture Documentation

This directory contains system architecture, design documents, and technical diagrams for Aura Video Studio.

## Core Architecture

- [System Architecture](ARCHITECTURE.md) - Complete system architecture overview

## Architecture Patterns

### Provider System
- [Provider Selection Architecture](PROVIDER_SELECTION_ARCHITECTURE.md) - Provider selection and fallback

### Event & Data Flow
- [SSE Event Flow](SSE_EVENT_FLOW.md) - Server-Sent Events architecture
- [Error Flow Diagram](ERROR_FLOW_DIAGRAM.md) - Error handling flow
- [Service Initialization Order](SERVICE_INITIALIZATION_ORDER.md) - Service startup sequence

### Component Flows
- [FFmpeg Locator Flow](FFMPEG_SINGLE_LOCATOR_FLOW.md) - FFmpeg detection and location
- [Wizard State Machine Diagram](WIZARD_STATE_MACHINE_DIAGRAM.md) - First-run wizard states

## UI/UX Design

- [Error Modal UI Design](ERROR_MODAL_UI_DESIGN.md) - Error dialog design
- [Professional Wizard UX](PROFESSIONAL_WIZARD_UX.md) - Onboarding wizard UX
- [Brand Kit UI](BRAND_KIT_UI.md) - Brand kit interface design

## Feature Architecture

- [Advanced Timeline Features](ADVANCED_TIMELINE_FEATURES.md) - Timeline editor architecture

## Key Design Principles

### Modularity
The system is organized into distinct layers:
- **Aura.Core** - Business logic and core services
- **Aura.Providers** - External service integrations
- **Aura.Api** - RESTful API backend
- **Aura.Web** - React + TypeScript frontend
- **Aura.App** - WinUI 3 desktop wrapper

### Provider Pattern
AI and media processing providers follow a common interface allowing:
- Automatic fallback on failure
- Free/Pro tier mixing
- Health monitoring
- Dynamic selection

### Error Handling
Comprehensive error handling with:
- Graceful degradation
- User-friendly error messages
- Detailed logging
- Recovery mechanisms

### State Management
- Centralized state in frontend
- Server-sent events for real-time updates
- Optimistic UI updates
- State persistence

## Related Documentation

- API Documentation - REST API reference
- Developer Guides - Development setup and guides
- User Guides - End-user documentation
- [Documentation Index](../INDEX.md) - Complete documentation map

## Diagrams & Visualizations

Many architecture documents include:
- System diagrams
- Sequence diagrams
- State machines
- Data flow diagrams
- Component relationships

These visualizations help understand:
- How components interact
- Data flow through the system
- State transitions
- Error handling paths
- Service dependencies
