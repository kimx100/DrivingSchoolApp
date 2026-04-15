using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class LessonCatalogService
{
    private static readonly IReadOnlyList<LessonItemDefinition> Items =
        new List<LessonItemDefinition>
        {
            new(LessonItemType.LeftTurn, "left_turn", "Left turn", "Venstresving",
                "Turning left safely with mirror, signal, and positioning."),
            new(LessonItemType.RightTurn, "right_turn", "Right turn", "Højresving",
                "Controlled right turns with speed and lane discipline."),
            new(LessonItemType.Parking, "parking", "Parking", "Parkering",
                "Standard parking practice in marked spaces."),
            new(LessonItemType.Reversing, "reversing", "Reversing", "Bakkemanøvre",
                "Reversing with observation and speed control."),
            new(LessonItemType.CorenerReversing, "corner_reversing", "Corner reversing", "Bakke om hjørne",
                "Reversing with observation and speed control through a corner."),
            new(LessonItemType.HighwayDriving, "motorway_driving", "Motorway driving", "Motorvejskørsel",
                "Merging, lane keeping, and safe motorway speed."),
            new(LessonItemType.NightDriving, "night_driving", "Night driving", "Natkørsel",
                "Driving with limited visibility and proper light use."),
            new(LessonItemType.Roundabout, "roundabout", "Roundabout", "Rundkørsel",
                "Lane choice, signaling, and smooth exits in roundabouts."),
            new(LessonItemType.LaneChange, "lane_change", "Lane change", "Vognbaneskift",
                "Mirror checks, blind-spot checks, and smooth repositioning."),
            new(LessonItemType.ParallelParking, "parallel_parking", "Parallel parking", "Parallelparkering",
                "Accurate parking close to the curb."),
            new(LessonItemType.EmergencyBraking, "emergency_braking", "Emergency braking", "Nødbremsning",
                "Controlled hard stop with safe reaction."),
            new(LessonItemType.SlipperyTrack, "slippery_track", "Slippery track", "Glatbane", 
                "Slippery track driving takes place on a specially designed track where the road surface simulates slippery conditions such as ice, rain or snow.")
        };

    public static IReadOnlyList<LessonItemDefinition> GetAll() => Items;
}

public sealed record LessonItemDefinition(
    LessonItemType ItemType,
    string Key,
    string DisplayName,
    string? DisplayNameDa,
    string? Description);
