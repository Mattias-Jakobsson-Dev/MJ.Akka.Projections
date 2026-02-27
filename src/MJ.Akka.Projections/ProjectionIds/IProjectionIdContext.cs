using System.Security.Cryptography;
using System.Text;

namespace MJ.Akka.Projections.ProjectionIds;

public interface IProjectionIdContext : IEquatable<IProjectionIdContext>
{
    string GetStringRepresentation();

    string GetProjectorId()
    {
        var message = Encoding.ASCII.GetBytes(GetStringRepresentation());
        var hashValue = MD5.HashData(message);

        return hashValue.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
    }
}