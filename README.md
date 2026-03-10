# 🍽️ RestAPI — Sistema de Fluxo de Restaurante

API REST para gerenciamento de fluxo de restaurante — controle de comandas, cardápio e notificações em tempo real para cozinha e caixa.

> Projeto em desenvolvimento ativo. Sprint atual: API completa → próxima etapa: Front-end React.

---

## Stack

| Tecnologia | Uso |
|---|---|
| .NET 8 | Web API |
| Entity Framework Core | ORM |
| SQL Server | Banco de dados |
| ASP.NET Core Identity | Gestão de usuários e roles |
| JWT Bearer | Autenticação stateless |
| SignalR | Notificações em tempo real |

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server local ou via Docker

  
---

## Configuração

### 1. Clonar o repositório

```bash
git clone https://github.com/Erasmojuniordev/RestAPI.git
cd RestAPI
```

### 2. Restaurar pacotes

```bash
dotnet restore
```

### 3. Configurar segredos locais

Dados sensíveis **nunca ficam no repositório**. Configure via User Secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "SUA_CHAVE_MINIMO_32_CARACTERES"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "SUA_CONNECTION_STRING"
```

> O `appsettings.json` contém apenas valores não sensíveis. Em produção, use variáveis de ambiente.

### 4. Aplicar migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Rodar

```bash
dotnet run
```

Acesse: `http://localhost:5000/swagger`

---

## Autenticação

A API usa **JWT Bearer**. Para autenticar no Swagger:

1. Faça login em `POST /api/auth/login`
2. Copie o `token` da resposta
3. Clique em **Authorize** no topo do Swagger
4. Digite: `Bearer SEU_TOKEN`

**Usuário admin padrão** (apenas em Development):

| Campo | Valor |
|---|---|
| Email | admin@restaurante.com |
| Senha | Admin@1234 |

---

## Roles e Permissões

| Role | Permissões |
|---|---|
| Admin | Acesso total |
| Garcom | Abrir comanda, adicionar/remover itens, marcar entregue |
| Cozinha | Visualizar painel, mudar status EmPreparo → Pronto |
| Caixa | Visualizar comandas, marcar como Paga |

**Criar usuário:**
```http
POST /api/auth/usuarios
Authorization: Bearer {token_admin}

{
  "email": "garcom@restaurante.com",
  "senha": "Senha@1234",
  "nomeCompleto": "João Silva",
  "role": "Garcom"
}
```

---

## Fluxo de Status da Comanda

```
Aberta ──→ Pendente ──→ EmPreparo ──→ Pronto ──→ Entregue ──→ Paga
  │            │              │
  └──────→ Cancelada ←────────┘
           (bloqueado a partir de Pronto)
```

Transições inválidas retornam `400 Bad Request` automaticamente.

---

## Endpoints

### Auth
| Método | Rota | Descrição | Role |
|---|---|---|---|
| POST | `/api/auth/login` | Login, retorna JWT | Público |
| POST | `/api/auth/usuarios` | Criar usuário | Admin |

### Comandas
| Método | Rota | Descrição | Role |
|---|---|---|---|
| GET | `/api/comandas` | Listar (filtro por status) | Todos |
| GET | `/api/comandas/{id}` | Detalhes da comanda | Todos |
| POST | `/api/comandas` | Abrir comanda | Admin, Garcom |
| POST | `/api/comandas/{id}/itens` | Adicionar item | Admin, Garcom |
| DELETE | `/api/comandas/{id}/itens/{itemId}` | Remover item | Admin, Garcom |
| PATCH | `/api/comandas/{id}/status` | Atualizar status | Conforme role |

### Cardápio
| Método | Rota | Descrição | Role |
|---|---|---|---|
| GET | `/api/itens` | Listar itens | Público |
| GET | `/api/itens/{id}` | Detalhes do item | Público |
| POST | `/api/itens` | Criar item | Admin |
| PUT | `/api/itens/{id}` | Atualizar item | Admin |
| PATCH | `/api/itens/{id}/disponibilidade` | Ativar/desativar | Admin |
| DELETE | `/api/itens/{id}` | Deletar item | Admin |

---

## SignalR — Notificações em Tempo Real

**Endpoint:** `ws://localhost:5000/hubs/cozinha`

O token JWT deve ser enviado via query string:
```
ws://localhost:5000/hubs/cozinha?access_token=SEU_TOKEN
```

**Eventos emitidos pelo servidor:**

| Evento | Destinatário | Quando |
|---|---|---|
| `NovoPedido` | Cozinha | Item adicionado à comanda |
| `StatusAtualizado` | Cozinha | Qualquer mudança de status |
| `PedidoPronto` | Garcom | Status muda para Pronto |
| `ComandaProntoParaPagar` | Caixa | Status muda para Entregue |

**Exemplo de conexão no React:**
```javascript
import { HubConnectionBuilder } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/cozinha", {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

connection.on("NovoPedido", (data) => {
  console.log(`Mesa ${data.mesa} pediu ${data.item}`);
});

await connection.start();
```

---

## Estrutura do Projeto

```
RestAPI/
├── Controllers/        # Entrada e saída HTTP
├── Data/               # DbContext e Migrations
├── DTOs/               # Contratos da API (Input/Output)
├── Enums/              # StatusComanda
├── Hubs/               # SignalR
├── Middleware/         # Tratamento global de erros
├── Models/             # Entidades do banco
└── Services/           # Regras de negócio
    └── Interfaces/
```

---

## Roadmap

- [x] Modelagem das entidades
- [x] Autenticação JWT + Identity
- [x] CRUD de Cardápio
- [x] Fluxo de Comandas com regras de status
- [x] Notificações em tempo real com SignalR
- [ ] Validação de inputs com FluentValidation
- [ ] Front-end React
- [ ] Painel da cozinha em tempo real
- [ ] Relatórios financeiros
