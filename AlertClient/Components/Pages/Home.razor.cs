using AlertClient.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AlertClient.Components.Pages;

public partial class Home(IToastService toastService) : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private List<AlertData> _alertDatas = [];

    private async Task Connect()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:7147/api/")
            .WithAutomaticReconnect()
            .Build();
        _hubConnection.On<AlertData>("Alert", async x =>
        {
            _alertDatas.Add(x);
            await InvokeAsync(StateHasChanged);
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch
        {
            toastService.ShowError("監視サービスへの接続に失敗しました。");
            await DisconnectAsync();
        }
    }

    private async Task Disconnect()
    {
        await DisconnectAsync();
    }

    private void Clear() => _alertDatas.Clear();

    public async ValueTask DisposeAsync() => await DisconnectAsync();

    private async Task DisconnectAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
