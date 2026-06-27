using System;

namespace ServiceScheduler.Application.Features.Services;

public record ServiceDto(Guid Id, string Name, string Description, decimal Value);
