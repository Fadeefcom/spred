using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Exception;
using TrackService.Models.DTOs;

namespace TrackService.Middleware;

/// <summary>
/// Endpoint filter that parses and validates track creation requests from base64-encoded JSON data.
/// </summary>
/// <remarks>
/// This filter processes track creation requests where the track metadata is provided as a base64-encoded JSON string
/// in the 'X-JSON-Data' header and the audio file is provided in the request body. It handles deserialization,
/// validation, and makes the parsed data available to subsequent pipeline components.
/// </remarks>
public class TrackCreateParser : IEndpointFilter
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        { IgnoreReadOnlyFields = true, IgnoreReadOnlyProperties = true };

    /// <summary>
    /// Processes the incoming HTTP request to parse and validate track creation data.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var base64Dto = context.HttpContext.Request.Headers["X-JSON-Data"];
        var file = context.HttpContext.Request.Body;

        if (string.IsNullOrWhiteSpace(base64Dto))
            return Results.BadRequest("Provide valid track data");

        var dto = Encoding.UTF8.GetString(Convert.FromBase64String(base64Dto!));
        var trackCreate = JsonSerializer.Deserialize<TrackCreate>(dto, _jsonSerializerOptions);

        trackCreate.ThrowBaseExceptionIfNull("Create command is null",
            status: (int)ErrorCode.UnprocessableEntity, "Missing valid json.");
        file.ThrowBaseExceptionIfNull("Missing audio file", status: (int)ErrorCode.UnprocessableEntity,
            "Missing audio file.");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);

        if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
            return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));

        context.HttpContext.Items.Add("Track", trackCreate);

        return await next(context);
    }
}