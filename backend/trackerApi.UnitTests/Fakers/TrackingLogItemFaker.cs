using Bogus;

using trackerApi.Models;

namespace trackerApi.UnitTests.Fakers;

public static class TrackingLogItemFaker
{
    public static Faker<TrackingLogItem> Create(Guid? userId = null)
    {
        return new Faker<TrackingLogItem>()
            .RuleFor(t => t.Id, f => Guid.NewGuid())
            .RuleFor(t => t.UserId, f => userId ?? Guid.NewGuid())
            .RuleFor(t => t.EventDate, f => f.Date.Recent(7)) // Random date within last 7 days
            .RuleFor(t => t.Accident, f => f.Random.Bool(0.3f)) // 30% chance of being true
            .RuleFor(t => t.ChangePadOrUnderware, f => f.Random.Bool(0.4f)) // 40% chance of being true
            .RuleFor(t => t.LeakAmount, f => f.Random.Int(0, 5)) // Random value between 0-5
            .RuleFor(t => t.Urgency, f => f.Random.Int(0, 4)) // Random value between 0-4
            .RuleFor(t => t.AwokeFromSleep, f => f.Random.Bool(0.2f)) // 20% chance of being true
            .RuleFor(t => t.PainLevel, f => f.Random.Int(0, 10)) // Random value between 0-10
            .RuleFor(t => t.Notes, f => f.Random.Bool(0.7f) // 70% chance of having notes
                ? f.Lorem.Sentence()
                : null);
    }

    // Helper methods for common scenarios
    public static TrackingLogItem Generate(Guid? userId = null)
    {
        return Create(userId).Generate();
    }

    public static List<TrackingLogItem> Generate(int count, Guid? userId = null)
    {
        return Create(userId).Generate(count);
    }
}
