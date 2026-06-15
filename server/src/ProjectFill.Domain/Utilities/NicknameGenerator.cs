namespace ProjectFill.Domain.Utilities;

public sealed class NicknameGenerator
{
    private static readonly string[] Adjectives =
    [
        "Bright", "Calm", "Swift", "Lucky", "Brave", "Fresh", "Bold", "Cool", "Dark", "Deep",
        "Eager", "Fast", "Glad", "Grand", "High", "Keen", "Kind", "Light", "Long", "Mild",
        "Neat", "Noble", "Pale", "Pure", "Rare", "Rich", "Safe", "Sharp", "Slim", "Smart",
        "Still", "Strong", "True", "Vast", "Warm", "Wide", "Wild", "Wise", "Young", "Agile",
        "Azure", "Blaze", "Blunt", "Clear", "Crisp", "Daring", "Fancy", "Fiery", "Firm", "Fleet",
        "Flint", "Fluid", "Frosty", "Gentle", "Golden", "Hardy", "Hasty", "Icy", "Iron", "Jade",
        "Jolly", "Lively", "Lunar", "Mellow", "Mighty", "Misty", "Mystic", "Onyx", "Opal", "Polar",
        "Prime", "Proud", "Quick", "Radiant", "Rapid", "Rocky", "Royal", "Rustic", "Savage", "Silent",
        "Silver", "Sleek", "Solar", "Solid", "Sonic", "Starry", "Steel", "Stormy", "Sunny", "Tidal",
        "Tiny", "Turbo", "Vibrant", "Violet", "Vivid", "Witty", "Zesty", "Cosmic", "Frozen", "Amber",
    ];

    private static readonly string[] Nouns =
    [
        "Wave", "Drop", "Bloom", "Spark", "Stone", "Leaf", "Arch", "Beam", "Bird", "Bolt",
        "Bud", "Cave", "Claw", "Clay", "Cliff", "Cloud", "Coal", "Core", "Cove", "Crow",
        "Dawn", "Deer", "Dew", "Dome", "Dove", "Dust", "Edge", "Ember", "Fang", "Fern",
        "Fire", "Fish", "Flag", "Flame", "Flare", "Flash", "Flow", "Fog", "Fork", "Frost",
        "Gate", "Glow", "Gust", "Hawk", "Haze", "Hill", "Horn", "Hunt", "Ice", "Isle",
        "Kite", "Lake", "Lance", "Lark", "Lava", "Link", "Lion", "Mist", "Moon", "Moss",
        "Nova", "Oak", "Path", "Peak", "Pine", "Pond", "Pool", "Port", "Rain", "Reed",
        "Ridge", "River", "Rock", "Root", "Rose", "Sand", "Sea", "Seed", "Shell", "Shore",
        "Sky", "Snow", "Soul", "Star", "Steam", "Storm", "Stream", "Sun", "Surge", "Tide",
        "Thorn", "Tree", "Veil", "Vine", "Wind", "Wing", "Wood", "Ash", "Crest", "Flux",
    ];

    public string Generate()
    {
        var rng = Random.Shared;
        return Adjectives[rng.Next(Adjectives.Length)]
             + Nouns[rng.Next(Nouns.Length)]
             + rng.Next(100, 1000);
    }
}
