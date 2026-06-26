namespace SharedKernel.Abstractions.CQRS;
/// <summary>
/// Isso n é o UnitOfWork ta, isso é um NoMeaningfulResult, q ai simplifica a abstração n precisando de duplicidade nas queries.
/// Msm padrão q o MediatR usa, ai n precisa ter um IRequest e um IRequest[TResponse]
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}