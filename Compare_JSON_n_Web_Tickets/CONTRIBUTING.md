# Contributing

## Guidelines

- Follow the rules in `.editorconfig`. Use 4 spaces for indentation and keep lines under 120 characters.
- Use descriptive branch names: `feature/<short-description>`, `fix/<issue-number>-short-desc`, `chore/<task>`.
- Open a pull request (PR) against `main`. PRs should include a clear description, screenshots (if UI), and reference issue numbers when applicable.

## Coding Standards

- Public types and methods: PascalCase.
- Private and protected fields: _camelCase (underscore prefix).
- Prefer explicit types for clarity (avoid `var` for built-in types unless obvious).
- Use async/await for asynchronous code; tests setup/teardown may return `Task`.
- Keep Page Object classes small and focused; one class per page.

## Tests

- Tests live under `Tests/` and should be named `<Feature>Tests`.
- Each test should be independent and clean up any state.
- Use Playwright fixtures in `Base/` to set up browser contexts.
- Do not commit real credentials. Use environment variables or secret stores.

## Playwright

- Prefer launching browsers in headless mode in CI. Use environment variable `PLAYWRIGHT_HEADLESS` to control locally.
- Use BrowserContext per test when tests modify storage or cookies.
- Centralize selectors in Page Objects and keep selectors resilient (prefer data- attributes or semantic selectors).

## CI

- CI should run `dotnet test` with a Playwright environment that supports browsers (use Playwright CLI to install browsers in CI).
- Add a job to run tests in both headless and headed (optional) modes for debugging.

## Pull Request Reviews

- Include unit and integration tests for new features when applicable.
- PR requires at least one reviewer and passing CI checks before merge.