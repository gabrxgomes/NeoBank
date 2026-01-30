# NeoBank - Sistema Bancario Digital

Sistema Fintech completo desenvolvido em .NET 9 com frontend em HTML/CSS/JavaScript puro.

## Sobre o Projeto

O NeoBank e uma aplicacao de banco digital que simula operacoes bancarias tradicionais como:
- Cadastro e autenticacao de usuarios
- Criacao de contas bancarias (Corrente, Poupanca, Investimento)
- Operacoes financeiras: Deposito, Saque, Transferencia
- Consulta de extrato e historico de transacoes

## Tecnologias Utilizadas

### Backend
- **.NET 9** - Framework principal
- **ASP.NET Core Web API** - API RESTful
- **Entity Framework Core 9** - ORM para acesso a dados
- **SQLite** - Banco de dados (desenvolvimento)
- **JWT (JSON Web Tokens)** - Autenticacao
- **BCrypt** - Hash de senhas
- **Swagger/OpenAPI** - Documentacao da API

### Frontend
- **HTML5** - Estrutura
- **CSS3** - Estilizacao (Dark Theme)
- **JavaScript (ES6+)** - Logica e consumo da API

## Estrutura do Projeto

```
Backend_dot/
├── NeoBank.API/           # Backend .NET
│   ├── Controllers/       # Endpoints da API
│   ├── Data/             # DbContext
│   ├── DTOs/             # Data Transfer Objects
│   ├── Models/           # Entidades de dominio
│   ├── Services/         # Logica de negocios
│   ├── Program.cs        # Configuracao da aplicacao
│   └── appsettings.json  # Configuracoes
│
├── NeoBank.Frontend/      # Frontend
│   ├── index.html        # Pagina principal
│   ├── styles.css        # Estilos
│   └── app.js            # JavaScript
│
└── README.md             # Este arquivo
```

## Executando o Projeto

### Pre-requisitos
- .NET SDK 9.0 ou superior
- Navegador web moderno

### Backend

```bash
# Navegar para pasta do backend
cd NeoBank.API

# Restaurar pacotes (se necessario)
dotnet restore

# Executar a aplicacao
dotnet run
```

A API estara disponivel em: `http://localhost:5000`

Documentacao Swagger: `http://localhost:5000/swagger`

### Frontend

1. Abra o arquivo `NeoBank.Frontend/index.html` no navegador
2. Ou use uma extensao como "Live Server" no VS Code

**Importante:** O frontend esta configurado para consumir a API em `http://localhost:5000/api`

## Endpoints da API

### Autenticacao
| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | `/api/auth/register` | Cadastrar novo usuario |
| POST | `/api/auth/login` | Fazer login |

### Usuarios
| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | `/api/users/me` | Obter perfil do usuario |
| PUT | `/api/users/me` | Atualizar perfil |
| DELETE | `/api/users/me` | Desativar conta |

### Contas
| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | `/api/accounts` | Listar contas do usuario |
| GET | `/api/accounts/{id}` | Obter detalhes de uma conta |
| POST | `/api/accounts` | Criar nova conta |
| DELETE | `/api/accounts/{id}` | Encerrar conta |

### Transacoes
| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | `/api/transactions/deposit` | Realizar deposito |
| POST | `/api/transactions/withdraw` | Realizar saque |
| POST | `/api/transactions/transfer` | Realizar transferencia |
| GET | `/api/transactions/{id}` | Obter detalhes da transacao |
| GET | `/api/transactions/statement/{accountId}` | Consultar extrato |

## Autenticacao

A API utiliza JWT Bearer Token. Apos fazer login/registro, voce recebera um token que deve ser enviado no header:

```
Authorization: Bearer {seu_token_aqui}
```

No Swagger, clique em "Authorize" e insira: `Bearer {seu_token}`

## Exemplo de Uso via cURL

### Cadastrar Usuario
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Joao Silva",
    "cpf": "12345678901",
    "email": "joao@email.com",
    "password": "senha123",
    "phone": "11999999999"
  }'
```

### Criar Conta
```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "type": 1,
    "initialDeposit": 100.00
  }'
```

### Fazer Deposito
```bash
curl -X POST http://localhost:5000/api/transactions/deposit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "accountId": "{guid-da-conta}",
    "amount": 500.00,
    "description": "Deposito inicial"
  }'
```

## Funcionalidades

- **Autenticacao segura** com JWT e hash de senha com BCrypt
- **Validacao de dados** em todas as requisicoes
- **Transacoes atomicas** para operacoes financeiras
- **CORS habilitado** para desenvolvimento local
- **Swagger UI** para testar a API
- **Frontend responsivo** com tema escuro

## Melhorias Futuras

- [ ] Implementar PIX
- [ ] Adicionar investimentos
- [ ] Sistema de notificacoes
- [ ] Relatorios financeiros
- [ ] Autenticacao 2FA
- [ ] Deploy em nuvem

## Licenca

Este projeto foi criado para fins educacionais e de portfolio.

---

Desenvolvido com .NET 9 e muito cafe
