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

### Home & Core Navigation

Main dashboard with the primary workout and calculations entry points.

![Home dashboard](docs/screenshots/home-dashboard.png)

Workout landing screen for session creation and history access.

![Workout overview](docs/screenshots/workout-overview.png)

Calendar view with saved sessions and edit/delete actions.

![Calendar history](docs/screenshots/calendar-history.png)

### Workout Flow

Session setup and workout detail entry.

![New workout builder](docs/screenshots/new-workout-builder.png)

Exercise browser with Push category recommendations.

![Exercise browser push](docs/screenshots/exercise-browser-push.png)

Squat variation recommendations in the browser.

![Exercise browser squat variation](docs/screenshots/exercise-browser-squat-variation.png)

Jump-focused movement recommendations.

![Exercise browser jumps](docs/screenshots/exercise-browser-jumps.png)

Olympic lift recommendations including Power Clean variations.

![Exercise browser olympic lifts](docs/screenshots/exercise-browser-olympic-lifts.png)

### Calculations

Strength estimate flow with movement selection and input capture.

![Calculations 1RM](docs/screenshots/calculations-1rm.png)

Calculated rep range output from the 1RM tool.

![Calculations 1RM range](docs/screenshots/calculations-1rm-range.png)

RSI calculation flow using jump height and GCT.

![Calculations RSI](docs/screenshots/calculations-rsi.png)

### Profile

Athletic performance logging with movement-based result tracking.

![Profile athletic performance](docs/screenshots/profile-athletic-performance.png)

Movement goal creation tied to catalog movements.

![Profile movement goals](docs/screenshots/profile-movement-goals.png)

Profile details including body metrics and sport background.

![Profile details](docs/screenshots/profile-details.png)

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
