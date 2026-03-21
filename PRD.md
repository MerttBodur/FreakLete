# GymTracker MVP PRD

## 1. Product Summary
GymTracker is a mobile fitness tracking application that lets users log workouts, view them on a calendar, and calculate 1RM values.

The MVP goal is to provide a usable end-to-end experience for:
- account creation and login
- workout creation
- calendar-based workout history
- 1RM calculation, PR saving, Athletic Performance Tracking
- persistent local data storage

## 2. Problem
Users who train regularly often track workouts in scattered places such as notes apps, spreadsheets, or memory. This makes it hard to:
- remember what was trained on a given day
- track progression consistently
- save PR attempts in a structured way

GymTracker solves this by keeping workout logging and strength tracking in one mobile app.

## 3. Target User
- Individual gym users especially Athletes
- Beginner to Advanced Athletes
- Athletes who want simple workout logging
- Users who want to track PRs estimated strength, athletic performances.

## 4. MVP Goal
The first release should allow a user to:
1. Register
2. Log in
3. Create a workout
4. Add exercises to a workout
5. Enter sets, reps, optional RIR, and optional rest time
6. View workouts on a calendar
7. Calculate 1RM
8. Save PR-like entries from the 1RM page
9. View basic profile information

## 5. Success Criteria
The MVP is successful if:
- a user can register and log in
- saved workouts persist after app restart
- workouts appear on the correct calendar date
- 1RM calculations return expected results
- saved PR entries persist and can be listed again
- the main app flow works without crashes
- Users password successfully encrypted at backend

## 6. Scope
### In Scope
- Login
- Register
- Home screen
- Bottom navigation
- Workout creation
- Exercise entry
- Calendar workout view
- 1RM calculator
- PR save feature
- Profile screen
- SQLite storage

### Out of Scope
- Cloud sync
- Multi-device sync
- Social features
- Advanced charts
- Push notifications
- Online backend
- Production-grade password security

## 7. Current System State
Current project structure includes:
- `HomePage`, `WorkoutPage`, `NewWorkoutPage`, `CalendarPage`, `OneRmPage`, `ProfilePage`, `LoginPage`
- custom `BottomNavBar`
- existing active storage using `Preferences`
- newly added SQLite `AppDatabase`
- models: `User`, `Workout`, `ExerciseEntry`, `PrEntry`

Important note:
- SQLite infrastructure exists
- page logic is not yet fully migrated from `Preferences` to SQLite

## 8. User Flows

### 8.1 Register / Login
1. User opens app
2. User sees login screen
3. If no account exists, user goes to register screen
4. User enters account information
5. Successful registration leads to login
6. Successful login opens `HomePage`

### 8.2 Workout Creation
1. User opens `Workout` tab
2. User taps `Add New Workout`
3. User enters date
4. User enters workout name
5. User selects exercise from predefined list
6. User enters sets and reps
7. User optionally enters RIR
8. User optionally enters rest seconds
9. User confirms workout
10. Workout is saved and later shown in calendar

### 8.3 Calendar Flow
1. User opens calendar from workout page
2. Calendar screen opens
3. Saved workout days are marked
4. Selecting a day shows workouts for that date

### 8.4 1RM Flow
1. User opens `1RM` tab
2. User enters weight, reps, and RIR
3. System calculates estimated 1RM using Epley logic
4. System shows values from 1RM to 8RM
5. User can save a PR-style entry

### 8.5 Profile Flow
1. User opens `Profile` tab
2. User sees username, email, lifting and athletic performance stats
3. User can set a goal at any spesific movement 
4. User can log out

## 9. Functional Requirements

### 9.1 Authentication
- User must be able to register
- Email must be unique
- User must be able to log in
- Session state must be stored
- User must be able to log out
- Password must be encrypted before hashing

### 9.2 Workout
- Workout name is required
- Date is required
- At least one exercise is required
- Exercise should be selected from predefined options
- Sets are required
- Reps are required
- RIR is optional
- Rest seconds are optional

### 9.3 Calendar
- Monthly calendar view should exist
- Days with workouts should be marked
- Selecting a date should list workouts for that date

### 9.4 1RM
- Weight input should be validated
- Rep input should be validated
- RIR should affect the calculation
- System should show 1RM through 8RM values
- PR entries should be savable

### 9.5 Profile
- Username should be displayed
- Email should be displayed
- Total workout count should be shown
- Total PR count should be shown
- Logout button should exist

## 10. Data Model

### User
- `Id`
- `Username`
- `Email`
- `Password`
- `CreatedAt`

### Workout
- `Id`
- `UserId`
- `WorkoutName`
- `WorkoutDate`

### ExerciseEntry
- `Id`
- `WorkoutId`
- `ExerciseName`
- `Sets`
- `Reps`
- `RIR`
- `RestSeconds`

### PrEntry
- `Id`
- `UserId`
- `ExerciseName`
- `Weight`
- `Reps`
- `RIR`
- `CreatedAt`

## 11. Technical Approach

### Layers
- UI: XAML pages
- Code-behind: page logic
- Data: `AppDatabase`
- Models: data classes
- Storage: SQLite

### Technologies
- .NET MAUI
- C#
- XAML
- `sqlite-net-pcl`
- Firebase Authentication

### Migration Plan
Current `Preferences` storage will be replaced with SQLite for:
- workout records
- exercise records
- PR records
- user records
