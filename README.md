# FreakLete

FreakLete is a mobile app for field athletes who lift and want to log gym sessions, track athletic performance, and keep key calculations in one focused workflow.

## Overview

FreakLete combines classic strength tracking with athletic metrics such as jumps, sprint work, RSI, movement goals, and exercise-specific data entry. The app now runs with a production backend and PostgreSQL persistence, so accounts and training data are no longer limited to a single device.

The current product also includes:
- structured athlete profile selection for sport, position, and coach preferences
- initial training program persistence
- an early `FreakAI` coach workflow for chat, program guidance, and active program review

Long term, `FreakAI` will move beyond its current MVP and become a deeper intelligence layer sitting on top of structured athlete data, exercise metadata, and recommendation logic.

Near-term product direction also includes deeper tracking visibility and session flow improvements:
- analytics dashboards for PR, bodyweight, and workout consistency trends
- a live workout mode with set/rest timing
- internal fatigue scoring to support smarter coaching and recovery decisions

The app is designed around a fast daily workflow:
- build workouts with categorized exercise recommendations
- review saved sessions from a calendar view
- calculate 1RM and RSI values
- track athletic performance and movement goals in one profile

## Features

- Register and login flow with JWT-based authentication
- Workout logging with categorized exercise selection
- Exercise browser with recommended movements by category
- Calendar-based workout history
- Edit and delete support for saved workouts
- Calculations page with 1RM and RSI tools
- Athletic performance tracking
- Movement goal tracking
- Profile and body metrics management
- Structured sport, position, and coach profile selection
- Training program persistence and active program retrieval
- FreakAI coach MVP with multilingual response behavior
- Cloud-backed persistence for profile, workouts, PRs, movement goals, and athletic performance

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
- ASP.NET Core Web API
- PostgreSQL
- Railway
- Google Gemini
- xUnit
- VS Code
- GenAI
  - Codex GPT-5.4 for coding
  - Opus 4.6 for prompting
  - GPT-5.4 Deep Research for ExerciseCatalog work
- Git for version control
- GitHub for tracking

## Quality

Automated testing is included. The production backend has also passed end-to-end smoke tests for auth, profile, workouts, PRs, athletic performance, movement goals, and account deletion.

## Roadmap

- Android Play Store release flow
- iOS release preparation
- Tracking analytics dashboards
  - PR trend line charts
  - bodyweight trend line charts
  - workout count / consistency trend charts
- Live workout mode
  - start workout flow
  - set timer and rest timer flow
  - per-set reps, RPE, and optional concentric time capture
- Internal fatigue modeling
  - calculate total session fatigue in the background
  - classify fatigue internally as low / intermediate / high
  - use this as an internal signal rather than a user-facing score
- Structured athlete profile improvements
- Training template library and better program browsing
- Richer exercise metadata and recommendation groundwork
- Deeper FreakAI intelligence layer

## Author

- GitHub: https://github.com/MerttBodur
- LinkedIn: https://www.linkedin.com/in/mert-bodur-08a053285/
