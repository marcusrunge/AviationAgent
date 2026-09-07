using System.Net;

namespace MarcusRunge.AviationAgent.AviationWeather;

/// <summary>
/// Represents a failed request to the deterministic aviation weather source.
/// </summary>
public sealed class AviationWeatherClientException : Exception
{
    public AviationWeatherClientException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException) => StatusCode = statusCode;

    public HttpStatusCode? StatusCode { get; }
}
