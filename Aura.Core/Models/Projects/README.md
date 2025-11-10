# Project File Models

This directory contains models for the .aura project file format and project management operations.

## Core Models

### AuraProjectFile
The main project file structure (.aura format):
- Project metadata (ID, name, description, dates)
- Asset references
- Timeline data
- Embedded project data (brief, plan, voice, render specs)
- Project settings

### ProjectAsset
Asset reference with path management:
- Asset metadata (ID, name, type)
- Absolute and relative paths
- Missing status tracking
- Content hash for deduplication
- File size and import date

### ProjectTimeline
Timeline structure with tracks and clips:
- Multiple tracks (video, audio, subtitle)
- Clips with timing and trim information
- Duration and framerate

## Operation Models

### AssetRelinkRequest/Result
Asset relinking for moved files

### MissingAssetsReport
Report of missing assets in a project

### ProjectConsolidationRequest/Result
Copy external assets into project folder

### ProjectPackageRequest/Result
Package project for export/sharing as .aurapack file

## File Format

The .aura file format is JSON-based for human readability and easy debugging. Version 1.0 schema.

Example:
```json
{
  "version": "1.0",
  "id": "guid",
  "name": "My Project",
  "assets": [...],
  "timeline": {...}
}
```

## Usage

These models are used by the `ProjectFileService` to manage project files and operations.
