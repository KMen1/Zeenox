using System.Net;
using System.Text.Json;

namespace Zeenox;

public class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            var response = context.Response;
            if (response.StatusCode == (int)HttpStatusCode.SwitchingProtocols)
            {
                return;
            }
            
            var result = JsonSerializer.Serialize(new { message = error.Message });
            response.ContentType = "application/json";
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await response.WriteAsync(result).ConfigureAwait(false);
            
        }
    }
}