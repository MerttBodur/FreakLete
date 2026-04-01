using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

public class StarterTemplateSeedService
{
    private readonly AppDbContext _db;

    public StarterTemplateSeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        if (await _db.TrainingPrograms.AnyAsync(p => p.IsStarterTemplate))
            return;

        var systemUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "system@freaklete.internal");
        if (systemUser is null)
        {
            systemUser = new User
            {
                FirstName = "System",
                LastName = "Templates",
                Email = "system@freaklete.internal",
                PasswordHash = "!",
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(systemUser);
            await _db.SaveChangesAsync();
        }

        var templates = BuildTemplates(systemUser.Id);
        _db.TrainingPrograms.AddRange(templates);
        await _db.SaveChangesAsync();
    }

    private static List<TrainingProgram> BuildTemplates(int systemUserId)
    {
        var now = DateTime.UtcNow;
        return
        [
            BuildFullBodyFoundation(systemUserId, now),
            BuildStrengthBase5x5(systemUserId, now),
            BuildUpperLowerPerformance(systemUserId, now),
            Build531Strength(systemUserId, now),
            BuildInSeasonMaintenance(systemUserId, now)
        ];
    }

    // ── 1. Full Body Foundation 3-Day ──────────────────────────────────

    private static TrainingProgram BuildFullBodyFoundation(int userId, DateTime now) => new()
    {
        UserId = userId,
        Name = "Full Body Foundation 3-Day",
        Description = "Beginner-friendly full body program focusing on general strength, movement quality, and building an athletic base.",
        Goal = "Beginner Strength + Athletic Base",
        DaysPerWeek = 3,
        SessionDurationMinutes = 60,
        Status = "active",
        IsStarterTemplate = true,
        CreatedAt = now,
        UpdatedAt = now,
        Weeks =
        [
            new ProgramWeek
            {
                WeekNumber = 1,
                Focus = "Foundation",
                Sessions =
                [
                    new ProgramSession
                    {
                        DayNumber = 1,
                        SessionName = "Day 1",
                        Focus = "Full Body A",
                        Exercises =
                        [
                            Ex(1, "Box Jump", "Jumps", 3, "3"),
                            Ex(2, "Back Squat", "Squat Variation", 3, "5"),
                            Ex(3, "Bench Press", "Push", 3, "5"),
                            Ex(4, "Romanian Deadlift", "Deadlift Variation", 3, "6"),
                            Ex(5, "Chest Supported Row", "Pull", 3, "8"),
                            Ex(6, "Split Squat", "Squat Variation", 2, "8/side"),
                            Ex(7, "Copenhagen Side Plank", "Squat Variation", 3, "20-30s/side")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 2,
                        SessionName = "Day 2",
                        Focus = "Full Body B",
                        Exercises =
                        [
                            Ex(1, "Med Ball Chest Throw", "Plyometrics", 3, "4"),
                            Ex(2, "Trap Bar Deadlift", "Deadlift Variation", 3, "5"),
                            Ex(3, "Overhead Press", "Push", 3, "5"),
                            Ex(4, "Pull-Up", "Pull", 3, "6-8", notes: "Lat Pulldown alternative"),
                            Ex(5, "Bulgarian Split Squat", "Squat Variation", 2, "8/side"),
                            Ex(6, "Farmers Carry", "Pull", 3, "20-30m")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 3,
                        SessionName = "Day 3",
                        Focus = "Full Body C",
                        Exercises =
                        [
                            Ex(1, "Broad Jump", "Jumps", 3, "3"),
                            Ex(2, "Front Squat", "Squat Variation", 3, "5-6"),
                            Ex(3, "Incline Dumbbell Press", "Push", 3, "6-8"),
                            Ex(4, "Barbell Hip Thrust", "Deadlift Variation", 3, "8"),
                            Ex(5, "Barbell Row", "Pull", 3, "8"),
                            Ex(6, "Copenhagen Side Plank", "Squat Variation", 3, "20-30s/side")
                        ]
                    }
                ]
            }
        ]
    };

    // ── 2. Strength Base 5x5 ───────────────────────────────────────────

    private static TrainingProgram BuildStrengthBase5x5(int userId, DateTime now) => new()
    {
        UserId = userId,
        Name = "Strength Base 5x5",
        Description = "Classic A/B alternating barbell strength program with athletic carryover. Rotate Day A and Day B across 3 training days per week.",
        Goal = "Foundational Barbell Strength",
        DaysPerWeek = 3,
        SessionDurationMinutes = 55,
        Status = "active",
        IsStarterTemplate = true,
        CreatedAt = now,
        UpdatedAt = now,
        Weeks =
        [
            new ProgramWeek
            {
                WeekNumber = 1,
                Focus = "A/B Rotation",
                Sessions =
                [
                    new ProgramSession
                    {
                        DayNumber = 1,
                        SessionName = "Day A",
                        Focus = "Squat + Bench + Row",
                        Exercises =
                        [
                            Ex(1, "Box Jump", "Jumps", 3, "3"),
                            Ex(2, "Back Squat", "Squat Variation", 5, "5"),
                            Ex(3, "Bench Press", "Push", 5, "5"),
                            Ex(4, "Barbell Row", "Pull", 5, "5"),
                            Ex(5, "Split Squat", "Squat Variation", 2, "8/side"),
                            Ex(6, "Copenhagen Side Plank", "Squat Variation", 3, "20-30s/side")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 2,
                        SessionName = "Day B",
                        Focus = "Squat + Press + Deadlift",
                        Exercises =
                        [
                            Ex(1, "Broad Jump", "Jumps", 3, "3"),
                            Ex(2, "Back Squat", "Squat Variation", 5, "5"),
                            Ex(3, "Overhead Press", "Push", 5, "5"),
                            Ex(4, "Trap Bar Deadlift", "Deadlift Variation", 1, "5"),
                            Ex(5, "Pull-Up", "Pull", 3, "6-8", notes: "Lat Pulldown alternative"),
                            Ex(6, "Farmers Carry", "Pull", 3, "20-30m")
                        ]
                    }
                ]
            }
        ]
    };

    // ── 3. Upper/Lower Performance 4-Day ───────────────────────────────

    private static TrainingProgram BuildUpperLowerPerformance(int userId, DateTime now) => new()
    {
        UserId = userId,
        Name = "Upper/Lower Performance 4-Day",
        Description = "4-day upper/lower split combining strength, hypertrophy support, and athletic transfer with power primers.",
        Goal = "Strength + Hypertrophy + Athletic Transfer",
        DaysPerWeek = 4,
        SessionDurationMinutes = 65,
        Status = "active",
        IsStarterTemplate = true,
        CreatedAt = now,
        UpdatedAt = now,
        Weeks =
        [
            new ProgramWeek
            {
                WeekNumber = 1,
                Focus = "Performance",
                Sessions =
                [
                    new ProgramSession
                    {
                        DayNumber = 1,
                        SessionName = "Lower 1",
                        Focus = "Squat emphasis",
                        Exercises =
                        [
                            Ex(1, "Box Jump", "Jumps", 3, "3"),
                            Ex(2, "Back Squat", "Squat Variation", 4, "4"),
                            Ex(3, "Romanian Deadlift", "Deadlift Variation", 3, "6"),
                            Ex(4, "Bulgarian Split Squat", "Squat Variation", 3, "8/side"),
                            Ex(5, "Barbell Hip Thrust", "Deadlift Variation", 3, "8"),
                            Ex(6, "Copenhagen Side Plank", "Squat Variation", 3, "20-30s/side")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 2,
                        SessionName = "Upper 1",
                        Focus = "Bench emphasis",
                        Exercises =
                        [
                            Ex(1, "Med Ball Chest Throw", "Plyometrics", 3, "4"),
                            Ex(2, "Bench Press", "Push", 4, "4"),
                            Ex(3, "Pull-Up", "Pull", 4, "6", notes: "Lat Pulldown alternative"),
                            Ex(4, "Incline Dumbbell Press", "Push", 3, "8"),
                            Ex(5, "Chest Supported Row", "Pull", 3, "8"),
                            Ex(6, "Face Pull", "Pull", 2, "12")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 3,
                        SessionName = "Lower 2",
                        Focus = "Power + Front Squat",
                        Exercises =
                        [
                            Ex(1, "Hang Power Clean", "Olympic Lifts", 4, "3"),
                            Ex(2, "Front Squat", "Squat Variation", 3, "5"),
                            Ex(3, "Trap Bar Deadlift", "Deadlift Variation", 3, "3"),
                            Ex(4, "Split Squat", "Squat Variation", 3, "8/side"),
                            Ex(5, "Barbell Hip Thrust", "Deadlift Variation", 3, "8")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 4,
                        SessionName = "Upper 2",
                        Focus = "Press emphasis",
                        Exercises =
                        [
                            Ex(1, "Med Ball Overhead Throw", "Plyometrics", 3, "4"),
                            Ex(2, "Overhead Press", "Push", 4, "4"),
                            Ex(3, "Barbell Row", "Pull", 4, "6"),
                            Ex(4, "Dumbbell Bench Press", "Push", 3, "8"),
                            Ex(5, "Rear Delt Fly", "Pull", 2, "12"),
                            Ex(6, "Farmers Carry", "Pull", 3, "20-30m")
                        ]
                    }
                ]
            }
        ]
    };

    // ── 4. 5/3/1 Strength 4-Day ────────────────────────────────────────

    private static TrainingProgram Build531Strength(int userId, DateTime now) => new()
    {
        UserId = userId,
        Name = "5/3/1 Strength 4-Day",
        Description = "Intermediate sustainable strength progression based on 5/3/1 principles. Each session has one main lift with prescribed rep scheme and athletic accessories.",
        Goal = "Intermediate Sustainable Strength",
        DaysPerWeek = 4,
        SessionDurationMinutes = 60,
        Status = "active",
        IsStarterTemplate = true,
        Notes = "5/3/1 progression: Week 1 = 3x5, Week 2 = 3x3, Week 3 = 5/3/1, Week 4 = deload. Use training max (90% of true 1RM) and progress +2.5kg upper / +5kg lower per cycle.",
        CreatedAt = now,
        UpdatedAt = now,
        Weeks =
        [
            new ProgramWeek
            {
                WeekNumber = 1,
                Focus = "5/3/1 Base",
                Sessions =
                [
                    new ProgramSession
                    {
                        DayNumber = 1,
                        SessionName = "Squat Day",
                        Focus = "Back Squat 5/3/1",
                        Exercises =
                        [
                            Ex(1, "Box Jump", "Jumps", 3, "3"),
                            Ex(2, "Back Squat", "Squat Variation", 3, "5/3/1", intensity: "5/3/1 progression"),
                            Ex(3, "Romanian Deadlift", "Deadlift Variation", 3, "6"),
                            Ex(4, "Split Squat", "Squat Variation", 3, "8/side"),
                            Ex(5, "Copenhagen Side Plank", "Squat Variation", 3, "20-30s/side")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 2,
                        SessionName = "Bench Day",
                        Focus = "Bench Press 5/3/1",
                        Exercises =
                        [
                            Ex(1, "Med Ball Chest Throw", "Plyometrics", 3, "4"),
                            Ex(2, "Bench Press", "Push", 3, "5/3/1", intensity: "5/3/1 progression"),
                            Ex(3, "Incline Dumbbell Press", "Push", 3, "8"),
                            Ex(4, "Barbell Row", "Pull", 4, "8"),
                            Ex(5, "Face Pull", "Pull", 3, "12")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 3,
                        SessionName = "Deadlift Day",
                        Focus = "Trap Bar Deadlift 5/3/1",
                        Exercises =
                        [
                            Ex(1, "Clean Pull", "Olympic Lifts", 3, "3"),
                            Ex(2, "Trap Bar Deadlift", "Deadlift Variation", 3, "5/3/1", intensity: "5/3/1 progression"),
                            Ex(3, "Front Squat", "Squat Variation", 3, "5"),
                            Ex(4, "Barbell Hip Thrust", "Deadlift Variation", 3, "8"),
                            Ex(5, "Farmers Carry", "Pull", 3, "20-30m")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 4,
                        SessionName = "Press Day",
                        Focus = "Overhead Press 5/3/1",
                        Exercises =
                        [
                            Ex(1, "Med Ball Overhead Throw", "Plyometrics", 3, "4"),
                            Ex(2, "Overhead Press", "Push", 3, "5/3/1", intensity: "5/3/1 progression"),
                            Ex(3, "Pull-Up", "Pull", 4, "6-8", notes: "Lat Pulldown alternative"),
                            Ex(4, "Dumbbell Bench Press", "Push", 3, "8"),
                            Ex(5, "Rear Delt Fly", "Pull", 3, "12")
                        ]
                    }
                ]
            }
        ]
    };

    // ── 5. In-Season Maintenance 2-Day ─────────────────────────────────

    private static TrainingProgram BuildInSeasonMaintenance(int userId, DateTime now) => new()
    {
        UserId = userId,
        Name = "In-Season Maintenance 2-Day",
        Description = "Minimal volume program designed to preserve strength and power during competitive season while keeping fatigue low.",
        Goal = "Preserve Strength/Power, Low Fatigue",
        DaysPerWeek = 2,
        SessionDurationMinutes = 45,
        Status = "active",
        IsStarterTemplate = true,
        CreatedAt = now,
        UpdatedAt = now,
        Weeks =
        [
            new ProgramWeek
            {
                WeekNumber = 1,
                Focus = "Maintenance",
                Sessions =
                [
                    new ProgramSession
                    {
                        DayNumber = 1,
                        SessionName = "Day 1",
                        Focus = "Hinge + Press",
                        Exercises =
                        [
                            Ex(1, "Box Jump", "Jumps", 3, "2"),
                            Ex(2, "Trap Bar Deadlift", "Deadlift Variation", 3, "3"),
                            Ex(3, "Bench Press", "Push", 3, "4"),
                            Ex(4, "Barbell Row", "Pull", 3, "6"),
                            Ex(5, "Split Squat", "Squat Variation", 2, "6/side"),
                            Ex(6, "Copenhagen Side Plank", "Squat Variation", 3, "20-30s/side")
                        ]
                    },
                    new ProgramSession
                    {
                        DayNumber = 2,
                        SessionName = "Day 2",
                        Focus = "Power + Squat",
                        Exercises =
                        [
                            Ex(1, "Hang Power Clean", "Olympic Lifts", 3, "2", notes: "Clean Pull alternative"),
                            Ex(2, "Front Squat", "Squat Variation", 3, "3"),
                            Ex(3, "Overhead Press", "Push", 3, "4"),
                            Ex(4, "Pull-Up", "Pull", 3, "6", notes: "Lat Pulldown alternative"),
                            Ex(5, "Romanian Deadlift", "Deadlift Variation", 2, "5"),
                            Ex(6, "Farmers Carry", "Pull", 3, "20-30m")
                        ]
                    }
                ]
            }
        ]
    };

    // ── Helper ──────────────────────────────────────────────────────────

    private static ProgramExercise Ex(
        int order,
        string name,
        string category,
        int sets,
        string repsOrDuration,
        string intensity = "",
        string notes = "") => new()
    {
        Order = order,
        ExerciseName = name,
        ExerciseCategory = category,
        Sets = sets,
        RepsOrDuration = repsOrDuration,
        IntensityGuidance = intensity,
        Notes = notes
    };
}
