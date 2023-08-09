using MudBlazor;
using PrivateChannel.Front.Models;
using System.Reflection;

namespace PrivateChannel.Front.Layouts;

public partial class MainLayout
{
    #region Fields
    
    private MudThemeProvider? _MudThemeProvider;
    private bool _IsDarkMode = true;
    private ServerStatus _ServerStatus = ServerStatus.Up;
    private readonly string? _Version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    #endregion

    #region Methods

    private void IsServerUpChanged((ServerStatus oldStatus, ServerStatus newStatus) isServerUp)
    {
        if (isServerUp.newStatus == ServerStatus.Down || isServerUp.newStatus == ServerStatus.Ban)
        {
            _ServerStatus = isServerUp.newStatus;
            Snackbar.Add(Localizer["SnackbarServerUnreachable"], Severity.Error);
        }
        else if (isServerUp.newStatus == ServerStatus.Up && _ServerStatus != isServerUp.newStatus)
        {
            _ServerStatus = isServerUp.newStatus;
            Snackbar.Add(Localizer["SnackbarServerReachable"], Severity.Success);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _MudThemeProvider != null)
        {
            _IsDarkMode = await _MudThemeProvider.GetSystemPreference();
            await _MudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            StateHasChanged();
        }
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        _IsDarkMode = newValue;
        StateHasChanged();

        return Task.CompletedTask;
    }

    private void GoHome()
    {
        if (NavigationManager.Uri != NavigationManager.BaseUri)
        {
            NavigationManager.NavigateTo("/");
        }
    }

    #endregion
}
