using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RestauranteAPI.Hubs;

// Grupos por role:
// "Cozinha" → recebe notificações de novos pedidos e mudanças de status
// "Caixa"   → recebe notificações de comandas prontas para pagamento
public class CozinhaHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Adiciona o usuário no grupo correspondente ao seu role
        var user = Context.User;

        if (user?.IsInRole("Cozinha") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "Cozinha");

        if (user?.IsInRole("Caixa") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "Caixa");

        if (user?.IsInRole("Garcom") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "Garcom");

        await base.OnConnectedAsync();
    }
}
