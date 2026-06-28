using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.ValueObjects;

namespace ServiceScheduler.Backend.UnitTests;

public class WorkerAvailabilityTests
{
    private static Service CreateDummyService()
    {
        return Service.Create("Corte", "Corte de cabelo", 50m);
    }

    [Fact]
    public void GetAvailablePeriods_ShouldReturnCandidateIntervals_WhenNoBlockersExist()
    {
        // Arrange
        var availablePeriods = new List<AvailablePeriod>
        {
            new AvailablePeriod(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18))
        };

        var worker = Worker.Create("Leila", "11999999999", "leila@cabeleleila.com", "12345678901", availablePeriods);

        var start = new DateTime(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc); // A Monday
        var end = new DateTime(2026, 6, 29, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = worker.GetAvailablePeriods(start, end, Enumerable.Empty<Schedule>()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc), result[0].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc), result[0].End);
    }

    [Fact]
    public void GetAvailablePeriods_ShouldClipCandidateIntervals_ToQueryBounds()
    {
        // Arrange
        var availablePeriods = new List<AvailablePeriod>
        {
            new AvailablePeriod(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18))
        };

        var worker = Worker.Create("Leila", "11999999999", "leila@cabeleleila.com", "12345678901", availablePeriods);

        var start = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc); // A Monday, inside shift
        var end = new DateTime(2026, 6, 29, 15, 0, 0, DateTimeKind.Utc); // Inside shift

        // Act
        var result = worker.GetAvailablePeriods(start, end, Enumerable.Empty<Schedule>()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc), result[0].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 15, 0, 0, DateTimeKind.Utc), result[0].End);
    }

    [Fact]
    public void GetAvailablePeriods_ShouldSubtractUnavailablePeriods()
    {
        // Arrange
        var availablePeriods = new List<AvailablePeriod>
        {
            new AvailablePeriod(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18))
        };

        var unavailablePeriods = new List<UnavailablePeriod>
        {
            new UnavailablePeriod(
                new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 29, 13, 0, 0, DateTimeKind.Utc),
                "Lunch")
        };

        var worker = Worker.Create("Leila", "11999999999", "leila@cabeleleila.com", "12345678901", availablePeriods, unavailablePeriods);

        var start = new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc);

        // Act
        var result = worker.GetAvailablePeriods(start, end, Enumerable.Empty<Schedule>()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc), result[0].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc), result[0].End);

        Assert.Equal(new DateTime(2026, 6, 29, 13, 0, 0, DateTimeKind.Utc), result[1].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc), result[1].End);
    }

    [Fact]
    public void GetAvailablePeriods_ShouldSubtractActiveSchedules()
    {
        // Arrange
        var availablePeriods = new List<AvailablePeriod>
        {
            new AvailablePeriod(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18))
        };

        var worker = Worker.Create("Leila", "11999999999", "leila@cabeleleila.com", "12345678901", availablePeriods);

        var services = new List<Service> { CreateDummyService() };
        var activeSchedule = Schedule.Create(
            Guid.NewGuid(),
            worker.Id,
            services,
            new DateTime(2026, 6, 29, 14, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1)
        );
        activeSchedule.Confirm();

        var start = new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc);

        // Act
        var result = worker.GetAvailablePeriods(start, end, new List<Schedule> { activeSchedule }).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc), result[0].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 14, 0, 0, DateTimeKind.Utc), result[0].End);

        Assert.Equal(new DateTime(2026, 6, 29, 15, 0, 0, DateTimeKind.Utc), result[1].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc), result[1].End);
    }

    [Fact]
    public void GetAvailablePeriods_ShouldNotSubtractCanceledSchedules()
    {
        // Arrange
        var availablePeriods = new List<AvailablePeriod>
        {
            new AvailablePeriod(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18))
        };

        var worker = Worker.Create("Leila", "11999999999", "leila@cabeleleila.com", "12345678901", availablePeriods);

        var services = new List<Service> { CreateDummyService() };
        var canceledSchedule = Schedule.Create(
            Guid.NewGuid(),
            worker.Id,
            services,
            new DateTime(2026, 6, 29, 14, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1)
        );
        canceledSchedule.Cancel();

        var start = new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc);

        // Act
        var result = worker.GetAvailablePeriods(start, end, new List<Schedule> { canceledSchedule }).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc), result[0].Start);
        Assert.Equal(new DateTime(2026, 6, 29, 18, 0, 0, DateTimeKind.Utc), result[0].End);
    }
}
