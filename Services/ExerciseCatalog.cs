using GymTracker.Models;

namespace GymTracker.Services;

public static class ExerciseCatalog
{
	public const string Push = "Push";
	public const string Pull = "Pull";
	public const string SquatVariation = "Squat Variation";
	public const string DeadliftVariation = "Deadlift Variation";
	public const string Sprint = "Sprint";
	public const string Jumps = "Jumps";
	public const string Plyometrics = "Plyometrics";
	public const string OlympicLifts = "Olympic Lifts";

	public static IReadOnlyList<string> Categories { get; } =
	[
		Push,
		Pull,
		SquatVariation,
		DeadliftVariation,
		Sprint,
		Jumps,
		Plyometrics,
		OlympicLifts
	];

	private static readonly IReadOnlyList<ExerciseCatalogItem> _items =
	[
		Strength(Push, "Bench Press"),
		Strength(Push, "Incline Bench Press"),
		Strength(Push, "Decline Bench Press"),
		Strength(Push, "Dumbbell Bench Press"),
		Strength(Push, "Incline Dumbbell Press"),
		Strength(Push, "Overhead Press"),
		Strength(Push, "Seated Dumbbell Press"),
		Strength(Push, "Push Press"),
		Strength(Push, "Dips"),
		Strength(Push, "Close Grip Bench Press"),
		Strength(Push, "Cable Chest Press"),
		Strength(Push, "Machine Chest Press"),
		Strength(Push, "Dumbbell Fly"),
		Strength(Push, "Incline Fly"),
		Strength(Push, "Lateral Raise"),
		Strength(Push, "Front Raise"),
		Strength(Push, "Skull Crusher"),
		Strength(Push, "Triceps Pushdown"),
		Strength(Push, "JM Press"),
		Strength(Push, "Landmine Press"),
		Strength(Push, "Pin Press"),
		Strength(Push, "Floor Press"),
		Strength(Push, "Arnold Press"),
		Strength(Push, "Behind the Neck Press"),
		Strength(Push, "Cable Lateral Raise"),

		Strength(Pull, "Pull-Up"),
		Strength(Pull, "Chin-Up"),
		Strength(Pull, "Lat Pulldown"),
		Strength(Pull, "Neutral Grip Pulldown"),
		Strength(Pull, "Barbell Row"),
		Strength(Pull, "Pendlay Row"),
		Strength(Pull, "Chest Supported Row"),
		Strength(Pull, "Seated Cable Row"),
		Strength(Pull, "Single Arm Dumbbell Row"),
		Strength(Pull, "T-Bar Row"),
		Strength(Pull, "Face Pull"),
		Strength(Pull, "Rear Delt Fly"),
		Strength(Pull, "Shrug"),
		Strength(Pull, "EZ Bar Curl"),
		Strength(Pull, "Hammer Curl"),
		Strength(Pull, "Preacher Curl"),
		Strength(Pull, "Cable Curl"),
		Strength(Pull, "Rope Hammer Curl"),
		Strength(Pull, "Straight Arm Pulldown"),
		Strength(Pull, "Meadows Row"),
		Strength(Pull, "Seal Row"),
		Strength(Pull, "Inverted Row"),
		Strength(Pull, "Machine High Row"),
		Strength(Pull, "Wide Grip Seated Row"),
		Strength(Pull, "Bayesian Curl"),

		Strength(SquatVariation, "Back Squat"),
		Strength(SquatVariation, "Front Squat"),
		Strength(SquatVariation, "Box Squat"),
		Strength(SquatVariation, "Pause Squat"),
		Strength(SquatVariation, "Tempo Squat"),
		Strength(SquatVariation, "Safety Bar Squat"),
		Strength(SquatVariation, "High Bar Squat"),
		Strength(SquatVariation, "Low Bar Squat"),
		Strength(SquatVariation, "Goblet Squat"),
		Strength(SquatVariation, "Zercher Squat"),
		Strength(SquatVariation, "Split Squat"),
		Strength(SquatVariation, "Bulgarian Split Squat"),
		Strength(SquatVariation, "Belt Squat"),
		Strength(SquatVariation, "Hack Squat"),
		Strength(SquatVariation, "Overhead Squat"),
		Strength(SquatVariation, "Anderson Squat"),
		Strength(SquatVariation, "Pin Squat"),
		Strength(SquatVariation, "Cyclist Squat"),
		Strength(SquatVariation, "Cossack Squat"),
		Strength(SquatVariation, "Landmine Squat"),
		Strength(SquatVariation, "Hatfield Squat"),
		Strength(SquatVariation, "Tempo Front Squat"),
		Strength(SquatVariation, "Paused Front Squat"),
		Strength(SquatVariation, "Jump Squat"),
		Strength(SquatVariation, "Spanish Squat"),

		Strength(DeadliftVariation, "Conventional Deadlift"),
		Strength(DeadliftVariation, "Sumo Deadlift"),
		Strength(DeadliftVariation, "Romanian Deadlift"),
		Strength(DeadliftVariation, "Stiff Leg Deadlift"),
		Strength(DeadliftVariation, "Deficit Deadlift"),
		Strength(DeadliftVariation, "Block Pull"),
		Strength(DeadliftVariation, "Rack Pull"),
		Strength(DeadliftVariation, "Paused Deadlift"),
		Strength(DeadliftVariation, "Snatch Grip Deadlift"),
		Strength(DeadliftVariation, "Trap Bar Deadlift"),
		Strength(DeadliftVariation, "Single Leg RDL"),
		Strength(DeadliftVariation, "Good Morning"),
		Strength(DeadliftVariation, "Cable Pull Through"),
		Strength(DeadliftVariation, "Clean Pull"),
		Strength(DeadliftVariation, "Halting Deadlift"),
		Strength(DeadliftVariation, "Jefferson Deadlift"),
		Strength(DeadliftVariation, "Banded Deadlift"),
		Strength(DeadliftVariation, "Tempo Deadlift"),
		Strength(DeadliftVariation, "Deadlift from Blocks"),
		Strength(DeadliftVariation, "Dimel Deadlift"),
		Strength(DeadliftVariation, "Floating Snatch Grip Deadlift"),
		Strength(DeadliftVariation, "Paused Romanian Deadlift"),
		Strength(DeadliftVariation, "Suitcase Deadlift"),
		Strength(DeadliftVariation, "Clean Deadlift"),
		Strength(DeadliftVariation, "Snatch Deadlift"),

		SprintItem("0-10m Sprint"),
		SprintItem("0-20m Sprint"),
		SprintItem("0-30m Sprint"),
		SprintItem("20-40m Sprint"),
		SprintItem("30-60m Sprint"),
		SprintItem("40y Dash"),
		SprintItem("60m Sprint"),
		SprintItem("Flying 10m"),
		SprintItem("Flying 20m"),
		SprintItem("Flying 30m"),
		SprintItem("Hill Sprint 10m"),
		SprintItem("Hill Sprint 20m"),
		SprintItem("Hill Sprint 30m"),
		SprintItem("Resisted Sled Sprint"),
		SprintItem("Assisted Sprint"),
		SprintItem("Curve Sprint"),
		SprintItem("Build-Up Sprint"),
		SprintItem("Acceleration Sprint"),
		SprintItem("Deceleration Sprint"),
		SprintItem("Repeat Sprint"),
		SprintItem("Wicket Sprint"),
		SprintItem("Falling Start Sprint"),
		SprintItem("Three-Point Start Sprint"),
		SprintItem("Rolling 30m"),
		SprintItem("Sprint-Float-Sprint"),

		JumpHeight("Vertical Jump"),
		JumpHeight("Countermovement Jump"),
		JumpHeight("Squat Jump"),
		JumpDistance("Single Broad Jump"),
		JumpDistance("Standing Broad Jump"),
		JumpDistanceMeters("Triple Broad Jump"),
		Custom(Jumps, "Repetitive Broad Jumps", "Distance", "cm", "Contacts", "reps"),
		JumpHeight("Box Jump"),
		JumpHeight("Depth Jump"),
		Custom(Jumps, "Weighted Jump", "Load", "kg", "Reps", "reps"),
		JumpHeight("Seated Box Jump"),
		JumpDistanceMeters("Lateral Bound"),
		JumpDistanceMeters("Single Leg Hop"),
		JumpDistanceMeters("Triple Hop"),
		Custom(Jumps, "Hurdle Hop", "Hurdles", "count", "Reps", "reps"),
		Custom(Jumps, "Continuous Hops", "Contacts", "reps", "Distance", "cm"),
		JumpHeight("Drop Jump"),
		JumpHeight("Tuck Jump"),
		JumpHeight("Split Jump"),
		JumpHeight("Single Leg Box Jump"),
		JumpHeight("Repeated Vertical Jump"),
		JumpDistanceMeters("Five Bound Test"),
		JumpDistanceMeters("Standing Triple Jump"),
		Custom(Jumps, "Reactive Hurdle Hop", "Hurdles", "count", "Height", "cm"),
		Custom(Jumps, "Loaded Broad Jump", "Load", "kg", "Distance", "cm"),

		Custom(Plyometrics, "Pogo Jumps", "Contacts", "reps", "Height", "cm"),
		Custom(Plyometrics, "Ankle Hops", "Contacts", "reps", "Height", "cm"),
		Custom(Plyometrics, "Line Hops", "Contacts", "reps", "Time", "s"),
		Custom(Plyometrics, "Skater Bounds", "Distance", "cm", "Contacts", "reps"),
		Custom(Plyometrics, "Lateral Line Hops", "Contacts", "reps", "Time", "s"),
		Custom(Plyometrics, "Med Ball Chest Throw", "Load", "kg", "Distance", "m"),
		Custom(Plyometrics, "Med Ball Scoop Toss", "Load", "kg", "Distance", "m"),
		Custom(Plyometrics, "Med Ball Overhead Throw", "Load", "kg", "Distance", "m"),
		Custom(Plyometrics, "Plyo Push-Up", "Reps", "reps", "Box Height", "cm"),
		Custom(Plyometrics, "Clap Push-Up", "Reps", "reps", "Time", "s"),
		Custom(Plyometrics, "Depth Push-Up", "Box Height", "cm", "Reps", "reps"),
		Custom(Plyometrics, "Reactive Step-Up", "Box Height", "cm", "Reps", "reps"),
		Custom(Plyometrics, "Sprint A-March", "Distance", "m", "Time", "s"),
		Custom(Plyometrics, "Sprint A-Skip", "Distance", "m", "Time", "s"),
		Custom(Plyometrics, "Sprint B-Skip", "Distance", "m", "Time", "s"),
		Custom(Plyometrics, "Bounding", "Distance", "m", "Contacts", "reps"),
		Custom(Plyometrics, "Alternate Bounds", "Distance", "m", "Contacts", "reps"),
		Custom(Plyometrics, "Stair Jumps", "Steps", "count", "Reps", "reps"),
		Custom(Plyometrics, "Lateral Box Shuffle", "Box Height", "cm", "Time", "s"),
		Custom(Plyometrics, "Reactive Broad Jump", "Distance", "cm", "Reps", "reps"),
		Custom(Plyometrics, "Depth Drop to Stick", "Box Height", "cm", "Reps", "reps"),
		Custom(Plyometrics, "Repeated Tuck Jump", "Contacts", "reps", "Time", "s"),
		Custom(Plyometrics, "Band Assisted Pogos", "Contacts", "reps", "Height", "cm"),
		Custom(Plyometrics, "Hurdle Mobility Hop", "Hurdles", "count", "Reps", "reps"),
		Custom(Plyometrics, "Single Leg Pogos", "Contacts", "reps", "Time", "s"),

		Strength(OlympicLifts, "Power Clean"),
		Strength(OlympicLifts, "Hang Power Clean"),
		Strength(OlympicLifts, "Clean"),
		Strength(OlympicLifts, "Hang Clean"),
		Strength(OlympicLifts, "Power Snatch"),
		Strength(OlympicLifts, "Hang Snatch"),
		Strength(OlympicLifts, "Snatch"),
		Strength(OlympicLifts, "Clean Pull"),
		Strength(OlympicLifts, "Snatch Pull"),
		Strength(OlympicLifts, "High Pull"),
		Strength(OlympicLifts, "Push Press"),
		Strength(OlympicLifts, "Push Jerk"),
		Strength(OlympicLifts, "Split Jerk"),
		Strength(OlympicLifts, "Power Jerk"),
		Strength(OlympicLifts, "Muscle Snatch"),
		Strength(OlympicLifts, "Muscle Clean"),
		Strength(OlympicLifts, "Tall Clean"),
		Strength(OlympicLifts, "Tall Snatch"),
		Strength(OlympicLifts, "Clean from Blocks"),
		Strength(OlympicLifts, "Snatch from Blocks"),
		Strength(OlympicLifts, "Hang Power Jerk"),
		Strength(OlympicLifts, "Clean Grip High Pull"),
		Strength(OlympicLifts, "Snatch Grip High Pull"),
		Strength(OlympicLifts, "Power Clean from Blocks"),
		Strength(OlympicLifts, "Power Snatch from Blocks")
	];

	public static IReadOnlyList<ExerciseCatalogItem> GetItemsByCategory(string category)
	{
		return _items.Where(item => item.Category == category).ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetRecommendedItemsByCategory(string category, int take = 20)
	{
		return _items
			.Where(item => item.Category == category)
			.Take(take)
			.ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetItemsByCategories(IEnumerable<string> categories)
	{
		HashSet<string> allowed = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return _items.Where(item => allowed.Contains(item.Category)).ToList();
	}

	public static ExerciseCatalogItem? GetByName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		return _items.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	public static ExerciseCatalogItem? GetByNameAndCategory(string? name, string? category)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		return _items.FirstOrDefault(item =>
			string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase) &&
			(string.IsNullOrWhiteSpace(category) || string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)));
	}

	private static ExerciseCatalogItem Strength(string category, string name)
	{
		return new ExerciseCatalogItem
		{
			Category = category,
			Name = name,
			TrackingMode = ExerciseTrackingMode.Strength,
			PrimaryLabel = "Load",
			PrimaryUnit = "kg"
		};
	}

	private static ExerciseCatalogItem SprintItem(string name)
	{
		return Custom(Sprint, name, "Distance", "m", "Time", "s");
	}

	private static ExerciseCatalogItem JumpHeight(string name)
	{
		return Custom(Jumps, name, "Height", "cm", "Attempts", "reps");
	}

	private static ExerciseCatalogItem JumpDistance(string name)
	{
		return Custom(Jumps, name, "Distance", "cm", "Attempts", "reps");
	}

	private static ExerciseCatalogItem JumpDistanceMeters(string name)
	{
		return Custom(Jumps, name, "Distance", "m", "Attempts", "reps");
	}

	private static ExerciseCatalogItem Custom(
		string category,
		string name,
		string primaryLabel,
		string primaryUnit,
		string secondaryLabel,
		string secondaryUnit)
	{
		return new ExerciseCatalogItem
		{
			Category = category,
			Name = name,
			TrackingMode = ExerciseTrackingMode.Custom,
			PrimaryLabel = primaryLabel,
			PrimaryUnit = primaryUnit,
			SecondaryLabel = secondaryLabel,
			SecondaryUnit = secondaryUnit
		};
	}
}
