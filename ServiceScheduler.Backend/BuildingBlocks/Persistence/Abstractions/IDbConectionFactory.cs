using System.Data;

namespace BuildingBlocks.Persistence.Abstractions;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
