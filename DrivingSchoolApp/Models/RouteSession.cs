using System;
using System.Collections.Generic;

namespace DrivingSchoolApp.Models;

public sealed class RouteSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public List<TrackPoint> Points { get; set; } = new();
}