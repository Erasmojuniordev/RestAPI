# 🚀 Como rodar o projeto

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server (local, ou via Docker abaixo)
- Visual Studio 2022 / VS Code / Rider

---

## Opção A — SQL Server via Docker (mais fácil)

Se não tiver SQL Server instalado, sobe em 1 comando:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Senha@1234" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

String de conexão para o `appsettings.json`:
```
Server=localhost,1433;Database=RestauranteDB;User Id=sa;Password=Senha@1234;TrustServerCertificate=True;
```

---

## Opção B — SQL Server local (Windows)

String de conexão padrão (autenticação Windows):
```
Server=localhost;Database=RestauranteDB;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## Passo a passo

### 1. Abrir o terminal na pasta do projeto

```bash
cd RestauranteAPI
```

### 2. Restaurar pacotes NuGet

```bash
dotnet restore
```

### 3. Configurar a connection string

Edite o arquivo `appsettings.json` e ajuste a `DefaultConnection` conforme sua configuração de banco.

> ⚠️ **Troque também a chave JWT:**
> ```json
> "Jwt": {
>   "Key": "COLOQUE_UMA_CHAVE_SECRETA_AQUI_MIN_32_CHARS"
> }
> ```

### 4. Criar o banco e rodar as migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Rodar a API

```bash
dotnet run
```

Acesse: **http://localhost:5000/swagger**

---

## Usuário admin padrão (apenas dev)

| Campo | Valor |
|-------|-------|
| Email | admin@restaurante.com |
| Senha | Admin@1234 |

Use o endpoint `POST /api/auth/login` no Swagger para obter o token JWT.

---

## Testando no Swagger

1. Faça login em `POST /api/auth/login`
2. Copie o token retornado
3. Clique em **Authorize** (cadeado no topo do Swagger)
4. Digite: `Bearer SEU_TOKEN_AQUI`
5. Pronto — todas as rotas autenticadas estarão liberadas

---

## Criando outros usuários

```http
POST /api/auth/usuarios
Authorization: Bearer {token_admin}

{
  "email": "garcom1@restaurante.com",
  "senha": "Senha@1234",
  "nomeCompleto": "João Garçom",
  "role": "Garcom"
}
```

Roles disponíveis: `Admin`, `Garcom`, `Cozinha`, `Caixa`
