using Microsoft.JSInterop;
using System.Globalization;

namespace PrivateChannel.Front.Components;

public partial class CultureSelector
{
    #region Fields

    private CultureInfo[] _SupportedCultures = new[]
{
        new CultureInfo("en-US"),
        new CultureInfo("fr-FR"),
    };

    #endregion

    private CultureInfo Culture
    {
        get => CultureInfo.CurrentCulture;
        set
        {
            if (CultureInfo.CurrentCulture != value)
            {
                IJSInProcessRuntime js = (IJSInProcessRuntime)JS;
                js.InvokeVoid("blazorCulture.set", value.Name);

                Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
            }
        }
    }

    private string CultureToNativeName(CultureInfo culture) => culture.Name switch
    {
        "en-US" => "English",
        "fr-FR" => "Français",
        _ => ""
    };
}