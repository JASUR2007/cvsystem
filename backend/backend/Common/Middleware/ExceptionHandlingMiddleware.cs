using System.Net;
using System.Text.Json;
using backend.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Common.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        try {
            await next(context);
        }
        catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception) {
        var statusCode = HttpStatusCode.InternalServerError;
        string message;

        switch (exception) {
            case NotFoundException notFound:
                statusCode = HttpStatusCode.NotFound;
                message = notFound.Message;
                break;

            case ForbiddenException forbidden:
                statusCode = HttpStatusCode.Forbidden;
                message = forbidden.Message;
                break;

            case ConflictException conflict:
                statusCode = HttpStatusCode.Conflict;
                message = conflict.Message;
                break;

            case ValidationException validation:
                statusCode = HttpStatusCode.BadRequest;
                message = validation.Message;
                break;

            case DbUpdateConcurrencyException:
                statusCode = HttpStatusCode.Conflict;
                message = "The record was changed in another session. Please reload to continue.";
                break;

            case DbUpdateException dbEx when dbEx.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }:
                statusCode = HttpStatusCode.Conflict;
                message = "A record with this unique value already exists.";
                break;

            default:
                logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                message = "An unexpected error occurred. Please try again later.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { message };
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
