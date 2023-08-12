using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PrivateChannel.Front.Models;
using System.Text;

namespace PrivateChannel.Front.Pages;

public partial class Channel
{
    #region Properties

    [Parameter]
    public Guid? CurrentChannelId { get; set; }

    public List<Guid> Channels { get; set; } = new List<Guid>();

    #endregion

    #region Methods

    protected override Task OnParametersSetAsync()
    {
        if (CurrentChannelId != null)
        {
            if (Channels.Contains(CurrentChannelId.Value) == false)
            {
                Channels.Add(CurrentChannelId.Value);
            }
        }

        return base.OnParametersSetAsync();
    }

    private async Task AddChannel()
    {
        CreateChannelResponse reply = await Client.CreateChannelAsync(new CreateChannelRequest());
        NavigationManager.NavigateTo($"/channel/{Guid.Parse(reply.ChannelId)}");
    }

    #endregion
}
