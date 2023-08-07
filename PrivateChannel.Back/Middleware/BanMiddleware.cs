using PrivateChannel.Back.Services;

namespace PrivateChannel.Back.Middleware;

public class BanMiddleware
{
    #region Fields

    private readonly RequestDelegate _Next;
    private readonly ILogger<BanMiddleware> _Logger;
    private readonly BanService _BanService;

    #endregion

    #region Constructors

    public BanMiddleware(RequestDelegate next, ILogger<BanMiddleware> logger, BanService banService)
    {
        _Next = next;
        _Logger = logger;
        _BanService = banService;
    }

    #endregion

    #region Methods

    public async Task Invoke(HttpContext context)
    {
        string? ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (ipAddress != null && _BanService.IsBanned(ipAddress))
        {
            _Logger.LogInformation("Banned IP {ipAddress} tried to access {requestPath}", ipAddress, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        try
        {
            if (ipAddress != null && context.Request.Path != "/status")
            {
                _BanService.AddUsage(ipAddress);
            }
            
            await _Next(context);

            if (context.Response.StatusCode == 404 && ipAddress != null)
            {
                _BanService.AddStrike(ipAddress);
            }
        }
        catch (Exception ex)
        {
            //this catch is not working for grpc calls because they do not rethrow internal exceptions.
            //this code may be used in future usages.
            if (ipAddress != null && context.Request.Path != "/status")
            {
                _Logger.LogError(ex, "An error occurred, banning IP {ipAddress}", ipAddress);
                _BanService.AddStrike(ipAddress);
            }
            throw;
        }
    }

    #endregion
}

