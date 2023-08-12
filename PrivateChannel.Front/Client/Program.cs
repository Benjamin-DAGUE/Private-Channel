using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using PrivateChannel.Front;
using PrivateChannel.Front.Services;
using System.Globalization;

namespace PrivateChannel.Front;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        builder.Services.AddTransient<IdentIconService>();
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;

            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        string endpoint = builder.Configuration.GetValue<string>("ServerEndpoint") ?? throw new Exception("Server endpoint not provided in config file");

        builder.Services.AddGrpcClient<PrivateChannelSvc.PrivateChannelSvcClient>(o =>
        {
            o.Address = new Uri(endpoint);
        }).ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));

        builder.Services.AddGrpcClient<PrivateNoteSvc.PrivateNoteSvcClient>(o =>
        {
            o.Address = new Uri(endpoint);
        }).ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));


        WebAssemblyHost host = builder.Build();

        CultureInfo culture;
        IJSRuntime js = host.Services.GetRequiredService<IJSRuntime>();
        string result = await js.InvokeAsync<string>("blazorCulture.get");

        if (result != null)
        {
            culture = new CultureInfo(result);
        }
        else
        {
            culture = new CultureInfo("fr-FR");
            await js.InvokeVoidAsync("blazorCulture.set", "fr-FR");
        }

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        await host.RunAsync();
    }
}
