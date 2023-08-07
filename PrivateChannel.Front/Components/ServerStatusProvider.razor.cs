using Microsoft.AspNetCore.Components;
using System.Threading;

namespace PrivateChannel.Front.Components;

public enum ServerStatus
{
    Unknown = 0,
    Up = 1,
    Down = 2,
    Ban = 3,
}

public sealed partial class ServerStatusProvider : IDisposable
{
    #region Fields

    private Task? _GetServerStatusTask;
    private CancellationTokenSource? _CancellationTokenSource;

    #endregion

    #region Parameters

    public ServerStatus ServerStatus { get; private set; }

    [Parameter]
    public EventCallback<(ServerStatus oldValue, ServerStatus newValue)> IsServerUpChanged { get; set; }

    #endregion

    #region Methods

    protected override void OnInitialized()
    {
        base.OnInitialized();

        string serverEndpoint = Configuration.GetValue<string>("ServerEndpoint") ?? throw new Exception("Server endpoint not provided in config file");
        
        _CancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _CancellationTokenSource.Token;

        _GetServerStatusTask = Task.Factory.StartNew(async () =>
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    using HttpClient client = ClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5);

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(serverEndpoint), "/status"));
                    HttpResponseMessage response = await client.SendAsync(request);

                    ServerStatus newStatus = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.OK => ServerStatus.Up,
                        System.Net.HttpStatusCode.Forbidden => ServerStatus.Ban,
                        _ => ServerStatus.Down
                    };

                    if (newStatus != ServerStatus)
                    {
                        ServerStatus oldStatus = ServerStatus;
                        ServerStatus = newStatus;
                        await IsServerUpChanged.InvokeAsync((oldStatus, ServerStatus));
                    }
                }
                catch (Exception ex)
                {
                    ServerStatus oldStatus = ServerStatus;
                    if (oldStatus != ServerStatus.Down)
                    {
                        ServerStatus = ServerStatus.Down;
                        await IsServerUpChanged.InvokeAsync((oldStatus, ServerStatus.Down));
                    }
                }

                if (ServerStatus == ServerStatus.Up)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                else if (ServerStatus == ServerStatus.Down)
                {
                    await Task.Delay(TimeSpan.FromSeconds(15));
                }
                else if (ServerStatus == ServerStatus.Ban)
                {
                    await Task.Delay(TimeSpan.FromHours(1));
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        if (_CancellationTokenSource?.IsCancellationRequested == false)
        {
            _CancellationTokenSource.Cancel();
            _CancellationTokenSource.Dispose();
            _CancellationTokenSource = null;
        }
        _GetServerStatusTask?.Dispose();
        _GetServerStatusTask = null;

        GC.SuppressFinalize(this);
    }

    #endregion
}
