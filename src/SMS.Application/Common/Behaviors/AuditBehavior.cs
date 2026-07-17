using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MediatR;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Common.Behaviors;

/// <summary>
/// Records an <see cref="AuditLog"/> entry for successful, mutating commands (types whose
/// name ends in "Command"). The request is serialized as the new values with password/token
/// fields redacted. Auditing never fails the underlying operation.
/// </summary>
public partial class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditBehavior(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Command", StringComparison.Ordinal) || _currentUser.UserId is not { } userId)
        {
            return response;
        }

        // Don't audit operations that reported failure (the Result wrapper exposes IsSuccess).
        if (response?.GetType().GetProperty("IsSuccess")?.GetValue(response) is false)
        {
            return response;
        }

        try
        {
            var (action, entityType) = SplitCommandName(requestName);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                NewValues = SerializeRedacted(request),
                IpAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Auditing is best-effort and must never break the command it observed.
        }

        return response;
    }

    private static (string Action, string EntityType) SplitCommandName(string requestName)
    {
        var name = requestName.EndsWith("Command", StringComparison.Ordinal)
            ? requestName[..^"Command".Length]
            : requestName;

        var words = CamelCaseBoundary().Split(name);
        return words.Length <= 1
            ? (name, string.Empty)
            : (words[0], string.Concat(words.Skip(1)));
    }

    private static string? SerializeRedacted(TRequest request)
    {
        try
        {
            var node = JsonSerializer.SerializeToNode(request, request.GetType());
            Redact(node);
            return node?.ToJsonString();
        }
        catch
        {
            return null;
        }
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kv => kv.Key).ToList())
                {
                    if (key.Contains("password", StringComparison.OrdinalIgnoreCase)
                        || key.Contains("token", StringComparison.OrdinalIgnoreCase))
                    {
                        obj[key] = "***";
                    }
                    else
                    {
                        Redact(obj[key]);
                    }
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    Redact(item);
                }
                break;
        }
    }

    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex CamelCaseBoundary();
}
