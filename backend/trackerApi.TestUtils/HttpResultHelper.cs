using Microsoft.AspNetCore.Http;

namespace trackerApi.TestUtils;

public static class HttpResultHelper
{
    public static int GetStatusCode(IResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var statusCodeProperty = result.GetType().GetProperty("StatusCode")
            ?? throw new InvalidOperationException("Result does not contain a StatusCode property");

        return (int)(statusCodeProperty.GetValue(result)
            ?? throw new InvalidOperationException("StatusCode value is null"));
    }

    public static string GetTokenFromResult(IResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var valueProperty = result.GetType().GetProperty("Value")
            ?? throw new InvalidOperationException("Result does not contain a Value property");

        var value = valueProperty.GetValue(result)
            ?? throw new InvalidOperationException("Result Value property is null");

        var tokenProperty = value.GetType().GetProperty("Token")
            ?? throw new InvalidOperationException("Result Value does not contain a Token property");

        return tokenProperty.GetValue(value)?.ToString()
            ?? throw new InvalidOperationException("Token value is null");
    }
}
