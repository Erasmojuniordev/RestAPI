# 🍽️ Restaurante API

API REST para sistema de fluxo de restaurante com controle de comandas, cardápio e notificações em tempo real.

## Stack
- **.NET 8** — Web API
- **Entity Framework Core** — ORM
- **SQL Server** — banco de dados
- **ASP.NET Core Identity** — autenticação e gestão de usuários
- **JWT Bearer** — autorização stateless
- **SignalR** — notificações em tempo real

---

## Setup

### 1. Pré-requisitos
- .NET 8 SDK
- SQL Server (local ou Docker)

### 2. Criar o projeto e instalar pacotes

```bash
dotnet new webapi -n RestauranteAPI
cd RestauranteAPI

dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Swashbuckle.AspNetCore
```

### 3. Configurar appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RestauranteDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "SUA_CHAVE_SECRETA_MIN_32_CARACTERES_AQUI",
    "Issuer": "RestauranteAPI",
    "Audience": "RestauranteFront",
    "ExpiresInHours": 8
  },
  "Cors": {
    "AllowedOrigin": "http://localhost:5173"
  }
}
```

> ⚠️ **Nunca commite a chave JWT.** Em produção, use variável de ambiente ou Azure Key Vault.

### 4. Migrations e banco

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Rodar

```bash
dotnet run
```

Acesse: `https://localhost:5001/swagger`

---

## Roles e permissões

| Role    | Permissões |
|---------|-----------|
| Admin   | Tudo |
| Garcom  | Abrir comanda, adicionar/remover itens, marcar entregue |
| Cozinha | Ver painel, mudar EmPreparo → Pronto |
| Caixa   | Ver comandas, marcar Paga |

### Usuário admin padrão (apenas dev)
- Email: `admin@restaurante.com`
- Senha: `Admin@1234`

---

## Fluxo de Status

```
Aberta → Pendente → EmPreparo → Pronto → Entregue → Paga
           ↘           ↘          ↘
         Cancelada  Cancelada  (bloqueado)
```

---

## Endpoints principais

### Auth
- `POST /api/auth/login` — login, retorna JWT
- `POST /api/auth/usuarios` — criar usuário (Admin)

### Comandas
- `GET  /api/comandas?status=Pendente` — listar com filtro
- `GET  /api/comandas/{id}` — detalhes
- `POST /api/comandas` — abrir comanda
- `POST /api/comandas/{id}/itens` — adicionar item
- `DELETE /api/comandas/{id}/itens/{itemId}` — remover item
- `PATCH /api/comandas/{id}/status` — mudar status

### Itens (Cardápio)
- `GET  /api/itens` — listar (público)
- `POST /api/itens` — criar (Admin)
- `PUT  /api/itens/{id}` — atualizar (Admin)
- `PATCH /api/itens/{id}/disponibilidade` — ativar/desativar (Admin)
- `DELETE /api/itens/{id}` — deletar (Admin)

---

## SignalR — Hub da Cozinha

Endpoint: `wss://localhost:5001/hubs/cozinha`

O token JWT deve ser enviado via query string: `?access_token=SEU_TOKEN`

### Eventos emitidos pelo servidor

| Evento | Destinatário | Payload |
|--------|-------------|---------|
| `NovoPedido` | Cozinha | `{comandaId, mesa, item, quantidade, observacao}` |
| `StatusAtualizado` | Cozinha | `{comandaId, mesa, statusAnterior, novoStatus}` |
| `PedidoPronto` | Garcom | `{comandaId, mesa, ...}` |
| `ComandaProntoParaPagar` | Caixa | `{comandaId, mesa, ...}` |
