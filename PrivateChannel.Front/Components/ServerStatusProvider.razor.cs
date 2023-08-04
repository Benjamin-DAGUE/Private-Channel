using Microsoft.AspNetCore.Components;
using System.Threading;

namespace PrivateChannel.Front.Components;

public sealed partial class ServerStatusProvider : IDisposable
{
    #region Fields

    private Task? _GetServerStatusTask;
    private CancellationTokenSource? _CancellationTokenSource;

    #endregion

    #region Parameters

    public bool? IsServerUp { get; private set; }

    [Parameter]
    public EventCallback<(bool? oldValue, bool newValue)> IsServerUpChanged { get; set; }

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
                    client.Timeout = TimeSpan.FromSeconds(2);

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, serverEndpoint);
                    
                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK && IsServerUp != true)
                    {
                        bool? oldValue = IsServerUp;
                        IsServerUp = true;
                        await IsServerUpChanged.InvokeAsync((oldValue, true));
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.OK && IsServerUp != false)
                    {
                        bool? oldValue = IsServerUp;
                        IsServerUp = false;
                        await IsServerUpChanged.InvokeAsync((oldValue, false));
                    }
                }
                catch (Exception ex)
                {
                    bool? oldValue = IsServerUp;

                    if (oldValue != false)
                    {
                        IsServerUp = false;
                        await IsServerUpChanged.InvokeAsync((oldValue, false));
                    }
                }

                if (IsServerUp == true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(15));
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
