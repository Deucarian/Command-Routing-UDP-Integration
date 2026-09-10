# Changelog

## [0.1.5] - 2026-09-09

### Changed

- Adopt the shared Editor 1.7 workspace presentation: neutral surfaces, readable typography, consistent actions and aligned controls.
- Preserve package workflows and native serialized editing; this is an editor-only presentation update.

## [0.1.4] - 2026-09-09

- Register package tooling and navigation actions as shared Control Center pages. Preserve the domain workflow while using Editor-owned submenus, in-window navigation, and UI scaling.

## [0.1.3] - 2026-08-31

- Registered the package workflow and a bounded, sanitized local-state card with Deucarian Control Center.
- Removed normal `Tools/Deucarian` menu exposure while preserving the standalone open API.
- Updated the shared Editor dependency to 1.2.0.
- Aligned Command Routing 0.2.5, Diagnostics 0.1.6, and Logging 1.0.4.

## [0.1.2] - 2026-08-26

- Derived the editor workflow footer from installed package metadata instead
  of a hardcoded package version.
- Updated exact Command Routing, Diagnostics, Editor, and Logging dependencies
  for the coordinated platform migration.

## [0.1.1] - 2026-07-24

- Corrected the package workflow to use the canonical shared Deucarian package
  validation pipeline.

## [0.1.0] - 2026-07-24

- Added a reusable UDP command transport with Unity-context delivery.
- Added a composed UDP command-routing host.
- Added diagnostics, Deucarian-styled editor settings, and tests.
- Added JSON Python interoperability and a legacy plain-text codec.
