namespace FreakLete.Api.Data;

public sealed class SportDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public bool HasPositions { get; init; }
    public IReadOnlyList<string> Positions { get; init; } = [];
}

public static class SportCatalog
{
    public static IReadOnlyList<SportDefinition> All { get; } = BuildCatalog();

    private static List<SportDefinition> BuildCatalog() =>
    [
        // ── Team Sports: Ball ───────────────────────────────────────

        new()
        {
            Id = "american-football", Name = "American Football", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Quarterback", "Running Back", "Wide Receiver", "Tight End", "Offensive Lineman", "Defensive End", "Defensive Tackle", "Linebacker", "Cornerback", "Safety", "Kicker", "Punter"]
        },
        new()
        {
            Id = "soccer", Name = "Soccer", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goalkeeper", "Center Back", "Full Back", "Wing Back", "Defensive Midfielder", "Central Midfielder", "Attacking Midfielder", "Winger", "Striker"]
        },
        new()
        {
            Id = "basketball", Name = "Basketball", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Point Guard", "Shooting Guard", "Small Forward", "Power Forward", "Center"]
        },
        new()
        {
            Id = "volleyball", Name = "Volleyball", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Setter", "Outside Hitter", "Middle Blocker", "Opposite Hitter", "Libero"]
        },
        new()
        {
            Id = "baseball", Name = "Baseball", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Pitcher", "Catcher", "First Baseman", "Second Baseman", "Shortstop", "Third Baseman", "Left Fielder", "Center Fielder", "Right Fielder", "Designated Hitter"]
        },
        new()
        {
            Id = "softball", Name = "Softball", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Pitcher", "Catcher", "First Baseman", "Second Baseman", "Shortstop", "Third Baseman", "Left Fielder", "Center Fielder", "Right Fielder"]
        },
        new()
        {
            Id = "rugby-union", Name = "Rugby Union", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Loosehead Prop", "Hooker", "Tighthead Prop", "Lock", "Blindside Flanker", "Openside Flanker", "Number 8", "Scrum Half", "Fly Half", "Inside Centre", "Outside Centre", "Wing", "Full Back"]
        },
        new()
        {
            Id = "rugby-league", Name = "Rugby League", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Prop", "Hooker", "Lock", "Second Row", "Loose Forward", "Half Back", "Stand Off", "Centre", "Wing", "Full Back"]
        },
        new()
        {
            Id = "handball", Name = "Handball", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goalkeeper", "Left Wing", "Left Back", "Centre Back", "Right Back", "Right Wing", "Pivot"]
        },
        new()
        {
            Id = "field-hockey", Name = "Field Hockey", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goalkeeper", "Full Back", "Half Back", "Midfielder", "Inner", "Wing", "Centre Forward"]
        },
        new()
        {
            Id = "ice-hockey", Name = "Ice Hockey", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goaltender", "Left Defenseman", "Right Defenseman", "Left Wing", "Center", "Right Wing"]
        },
        new()
        {
            Id = "lacrosse", Name = "Lacrosse", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goalkeeper", "Defenseman", "Midfielder", "Attackman"]
        },
        new()
        {
            Id = "cricket", Name = "Cricket", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Batsman", "Bowler (Fast)", "Bowler (Spin)", "All-Rounder", "Wicket Keeper"]
        },
        new()
        {
            Id = "netball", Name = "Netball", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goal Shooter", "Goal Attack", "Wing Attack", "Centre", "Wing Defence", "Goal Defence", "Goal Keeper"]
        },
        new()
        {
            Id = "water-polo", Name = "Water Polo", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Goalkeeper", "Center Back", "Wing", "Driver", "Point", "Center Forward"]
        },
        new()
        {
            Id = "australian-football", Name = "Australian Football", Category = "Team Sports",
            HasPositions = true,
            Positions = ["Full Back", "Back Pocket", "Centre Half Back", "Half Back Flank", "Wing", "Centre", "Ruck", "Ruck Rover", "Rover", "Centre Half Forward", "Half Forward Flank", "Full Forward", "Forward Pocket"]
        },

        // ── Racket Sports ───────────────────────────────────────────

        new() { Id = "tennis", Name = "Tennis", Category = "Racket Sports", HasPositions = false },
        new() { Id = "badminton", Name = "Badminton", Category = "Racket Sports", HasPositions = false },
        new() { Id = "table-tennis", Name = "Table Tennis", Category = "Racket Sports", HasPositions = false },
        new() { Id = "squash", Name = "Squash", Category = "Racket Sports", HasPositions = false },
        new() { Id = "padel", Name = "Padel", Category = "Racket Sports", HasPositions = false },
        new() { Id = "pickleball", Name = "Pickleball", Category = "Racket Sports", HasPositions = false },

        // ── Combat Sports ───────────────────────────────────────────

        new()
        {
            Id = "boxing", Name = "Boxing", Category = "Combat Sports",
            HasPositions = true,
            Positions = ["Heavyweight", "Light Heavyweight", "Middleweight", "Welterweight", "Lightweight", "Featherweight", "Bantamweight", "Flyweight"]
        },
        new()
        {
            Id = "mma", Name = "MMA", Category = "Combat Sports",
            HasPositions = true,
            Positions = ["Heavyweight", "Light Heavyweight", "Middleweight", "Welterweight", "Lightweight", "Featherweight", "Bantamweight", "Flyweight"]
        },
        new()
        {
            Id = "wrestling", Name = "Wrestling", Category = "Combat Sports",
            HasPositions = true,
            Positions = ["Freestyle", "Greco-Roman"]
        },
        new()
        {
            Id = "judo", Name = "Judo", Category = "Combat Sports", HasPositions = false
        },
        new()
        {
            Id = "bjj", Name = "Brazilian Jiu-Jitsu", Category = "Combat Sports", HasPositions = false
        },
        new()
        {
            Id = "taekwondo", Name = "Taekwondo", Category = "Combat Sports", HasPositions = false
        },
        new()
        {
            Id = "karate", Name = "Karate", Category = "Combat Sports",
            HasPositions = true,
            Positions = ["Kata", "Kumite"]
        },
        new()
        {
            Id = "muay-thai", Name = "Muay Thai", Category = "Combat Sports", HasPositions = false
        },
        new()
        {
            Id = "kickboxing", Name = "Kickboxing", Category = "Combat Sports", HasPositions = false
        },
        new()
        {
            Id = "fencing", Name = "Fencing", Category = "Combat Sports",
            HasPositions = true,
            Positions = ["Foil", "Epee", "Sabre"]
        },

        // ── Strength Sports ─────────────────────────────────────────

        new()
        {
            Id = "powerlifting", Name = "Powerlifting", Category = "Strength Sports", HasPositions = false
        },
        new()
        {
            Id = "weightlifting", Name = "Olympic Weightlifting", Category = "Strength Sports", HasPositions = false
        },
        new()
        {
            Id = "strongman", Name = "Strongman", Category = "Strength Sports", HasPositions = false
        },
        new()
        {
            Id = "bodybuilding", Name = "Bodybuilding", Category = "Strength Sports",
            HasPositions = true,
            Positions = ["Men's Physique", "Classic Physique", "Open Bodybuilding", "Women's Physique", "Bikini", "Figure", "Wellness"]
        },
        new()
        {
            Id = "crossfit", Name = "CrossFit", Category = "Strength Sports", HasPositions = false
        },
        new()
        {
            Id = "functional-fitness", Name = "Functional Fitness", Category = "Strength Sports", HasPositions = false
        },
        new()
        {
            Id = "calisthenics", Name = "Calisthenics", Category = "Strength Sports", HasPositions = false
        },

        // ── Track and Field ─────────────────────────────────────────

        new()
        {
            Id = "sprinting", Name = "Sprinting", Category = "Track and Field",
            HasPositions = true,
            Positions = ["100m", "200m", "400m"]
        },
        new()
        {
            Id = "middle-distance", Name = "Middle Distance Running", Category = "Track and Field",
            HasPositions = true,
            Positions = ["800m", "1500m"]
        },
        new()
        {
            Id = "long-distance-running", Name = "Long Distance Running", Category = "Track and Field",
            HasPositions = true,
            Positions = ["5000m", "10000m", "Marathon", "Half Marathon", "Ultra Marathon"]
        },
        new()
        {
            Id = "hurdles", Name = "Hurdles", Category = "Track and Field",
            HasPositions = true,
            Positions = ["100m Hurdles", "110m Hurdles", "400m Hurdles"]
        },
        new()
        {
            Id = "high-jump", Name = "High Jump", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "long-jump", Name = "Long Jump", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "triple-jump", Name = "Triple Jump", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "pole-vault", Name = "Pole Vault", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "shot-put", Name = "Shot Put", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "discus", Name = "Discus Throw", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "javelin", Name = "Javelin Throw", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "hammer-throw", Name = "Hammer Throw", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "decathlon", Name = "Decathlon", Category = "Track and Field", HasPositions = false
        },
        new()
        {
            Id = "heptathlon", Name = "Heptathlon", Category = "Track and Field", HasPositions = false
        },

        // ── Water Sports ────────────────────────────────────────────

        new()
        {
            Id = "swimming", Name = "Swimming", Category = "Water Sports",
            HasPositions = true,
            Positions = ["Freestyle", "Backstroke", "Breaststroke", "Butterfly", "Individual Medley"]
        },
        new()
        {
            Id = "rowing", Name = "Rowing", Category = "Water Sports",
            HasPositions = true,
            Positions = ["Sweep", "Sculling", "Coxswain"]
        },
        new()
        {
            Id = "surfing", Name = "Surfing", Category = "Water Sports", HasPositions = false
        },
        new()
        {
            Id = "diving", Name = "Diving", Category = "Water Sports", HasPositions = false
        },
        new()
        {
            Id = "kayaking", Name = "Kayaking", Category = "Water Sports", HasPositions = false
        },

        // ── Winter Sports ───────────────────────────────────────────

        new()
        {
            Id = "alpine-skiing", Name = "Alpine Skiing", Category = "Winter Sports",
            HasPositions = true,
            Positions = ["Slalom", "Giant Slalom", "Super-G", "Downhill", "Combined"]
        },
        new()
        {
            Id = "cross-country-skiing", Name = "Cross-Country Skiing", Category = "Winter Sports",
            HasPositions = true,
            Positions = ["Classic", "Skating"]
        },
        new()
        {
            Id = "snowboarding", Name = "Snowboarding", Category = "Winter Sports",
            HasPositions = true,
            Positions = ["Freestyle", "Alpine", "Snowboard Cross", "Halfpipe", "Slopestyle"]
        },
        new()
        {
            Id = "figure-skating", Name = "Figure Skating", Category = "Winter Sports",
            HasPositions = true,
            Positions = ["Singles", "Pairs", "Ice Dance"]
        },
        new()
        {
            Id = "speed-skating", Name = "Speed Skating", Category = "Winter Sports",
            HasPositions = true,
            Positions = ["Short Track", "Long Track"]
        },
        new()
        {
            Id = "biathlon", Name = "Biathlon", Category = "Winter Sports", HasPositions = false
        },

        // ── Cycling ─────────────────────────────────────────────────

        new()
        {
            Id = "road-cycling", Name = "Road Cycling", Category = "Cycling",
            HasPositions = true,
            Positions = ["Sprinter", "Climber", "Time Trialist", "All-Rounder", "Domestique"]
        },
        new()
        {
            Id = "mountain-biking", Name = "Mountain Biking", Category = "Cycling",
            HasPositions = true,
            Positions = ["Cross-Country", "Downhill", "Enduro", "Trail"]
        },
        new()
        {
            Id = "track-cycling", Name = "Track Cycling", Category = "Cycling",
            HasPositions = true,
            Positions = ["Sprint", "Endurance", "Keirin"]
        },
        new()
        {
            Id = "bmx", Name = "BMX", Category = "Cycling",
            HasPositions = true,
            Positions = ["Racing", "Freestyle"]
        },

        // ── Multi-Sport ─────────────────────────────────────────────

        new()
        {
            Id = "triathlon", Name = "Triathlon", Category = "Multi-Sport",
            HasPositions = true,
            Positions = ["Sprint Distance", "Olympic Distance", "Half Ironman", "Ironman"]
        },

        // ── Gymnastics / Acrobatic ──────────────────────────────────

        new()
        {
            Id = "artistic-gymnastics", Name = "Artistic Gymnastics", Category = "Gymnastics",
            HasPositions = true,
            Positions = ["Floor", "Vault", "Uneven Bars", "Balance Beam", "Pommel Horse", "Rings", "Parallel Bars", "Horizontal Bar", "All-Around"]
        },
        new()
        {
            Id = "rhythmic-gymnastics", Name = "Rhythmic Gymnastics", Category = "Gymnastics", HasPositions = false
        },
        new()
        {
            Id = "cheerleading", Name = "Cheerleading", Category = "Gymnastics",
            HasPositions = true,
            Positions = ["Flyer", "Base", "Back Spot", "Tumbler"]
        },

        // ── Precision / Skill ───────────────────────────────────────

        new() { Id = "golf", Name = "Golf", Category = "Precision Sports", HasPositions = false },
        new() { Id = "archery", Name = "Archery", Category = "Precision Sports", HasPositions = false },
        new() { Id = "shooting", Name = "Shooting", Category = "Precision Sports", HasPositions = false },
        new() { Id = "climbing", Name = "Climbing", Category = "Precision Sports",
            HasPositions = true,
            Positions = ["Bouldering", "Lead", "Speed", "Combined"]
        },

        // ── Action Sports ───────────────────────────────────────────

        new() { Id = "skateboarding", Name = "Skateboarding", Category = "Action Sports",
            HasPositions = true,
            Positions = ["Street", "Park", "Vert"]
        },

        // ── General Fitness ─────────────────────────────────────────

        new() { Id = "general-fitness", Name = "General Fitness", Category = "General", HasPositions = false },
        new() { Id = "recreational", Name = "Recreational Athlete", Category = "General", HasPositions = false },
    ];
}
