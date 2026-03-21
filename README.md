# FreakLete

FreakLete is a .NET MAUI mobile app for lifters and field athletes who want to log gym sessions, track athletic performance, and keep key calculations in one focused workflow.

## Overview

FreakLete was built as a product-style MVP for athletes who need more than a basic gym log. It combines classic strength tracking with athletic metrics such as jumps, sprint work, RSI, movement goals, and exercise-specific data entry.

The app is designed around a fast daily workflow:
- build workouts with categorized exercise recommendations
- review saved sessions from a calendar view
- calculate 1RM and RSI values
- track athletic performance and movement goals in one profile

## Features

- Login and register flow with hashed local authentication
- Workout logging with categorized exercise selection
- Exercise browser with recommended movements by category
- Calendar-based workout history
- Edit and delete support for saved workouts
- Calculations page with 1RM and RSI tools
- Athletic performance tracking
- Movement goal tracking
- Profile and body metrics management

## Screens

Screenshots for the current MVP live under `docs/screenshots/`.

### Home & Core Navigation

- `docs/screenshots/home-dashboard.png`
  - Main dashboard with the primary workout and calculations entry points.
- `docs/screenshots/workout-overview.png`
  - Workout landing screen for session creation and history access.
- `docs/screenshots/calendar-history.png`
  - Calendar view with saved sessions and edit/delete actions.

### Workout Flow

- `docs/screenshots/new-workout-builder.png`
  - Session setup and workout detail entry.
- `docs/screenshots/exercise-browser-push.png`
  - Exercise browser with Push category recommendations.
- `docs/screenshots/exercise-browser-squat-variation.png`
  - Squat variation recommendations in the browser.
- `docs/screenshots/exercise-browser-jumps.png`
  - Jump-focused movement recommendations.
- `docs/screenshots/exercise-browser-olympic-lifts.png`
  - Olympic lift recommendations including Power Clean variations.

### Calculations

- `docs/screenshots/calculations-1rm.png`
  - Strength estimate flow with movement selection and input capture.
- `docs/screenshots/calculations-1rm-range.png`
  - Calculated rep range output from the 1RM tool.
- `docs/screenshots/calculations-rsi.png`
  - RSI calculation flow using jump height and GCT.

### Profile

- `docs/screenshots/profile-athletic-performance.png`
  - Athletic performance logging with movement-based result tracking.
- `docs/screenshots/profile-movement-goals.png`
  - Movement goal creation tied to catalog movements.
- `docs/screenshots/profile-details.png`
  - Profile details including body metrics and sport background.

## Tech Stack & Tooling

- .NET MAUI
- C#
- SQLite
- xUnit
- VS Code
- GenAI
  - Codex GPT-5.4 for coding
  - Opus 4.6 for prompting
  - GPT-5.4 Deep Research for ExerciseCatalog work
- Git for version control
- GitHub for tracking

## Quality

Automated testing is included.

## Roadmap

- richer filtering and discovery inside the exercise browser
- broader athletic analytics and comparisons
- more custom UI replacements for remaining platform-native selection flows
- release polish for publishing assets and store-ready packaging

## Author

- Mert
- GitHub: https://github.com/MerttBodur
- LinkedIn: https://www.linkedin.com/in/mert-bodur-08a053285/
