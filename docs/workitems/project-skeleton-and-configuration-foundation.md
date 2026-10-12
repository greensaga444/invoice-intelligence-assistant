# Work Item: Project skeleton and configuration foundation

## Spec

Business Goal:
Establish a buildable technical baseline so both product modules can share a reliable local configuration foundation before feature delivery begins.

User Story:
As a developer, I need a runnable solution scaffold with shared INI configuration loading so I can implement downloader and AI workflows without re-building project infrastructure.

MVP:
A .NET 8 solution containing two WPF apps and one shared core library, with validated INI loading and basic tests.

Scope:
- In scope: solution scaffold, module projects, shared core configuration loader, naming placeholder validation, sample config files, initial tests.
- Out of scope: invoice download business flow, AI pipeline, Excel I/O, MOF integration runtime behavior.

Acceptance Criteria:
- Solution builds successfully.
- Both app projects reference the shared core library.
- INI loader reads required sections/keys and validates placeholders.
- Unit tests cover success and key validation failures.
- Formatting check is clean.

## Tasks

- [x] Create feature branch and scaffold solution/projects.
- [x] Add shared core configuration models and INI parser/loader.
- [x] Add naming rule placeholder validator.
- [x] Wire both WPF app startups to load module config.
- [x] Add sample module INI files under config/.
- [x] Add and run unit tests for configuration loading/validation.
- [x] Run build, test, and format verification gates.
