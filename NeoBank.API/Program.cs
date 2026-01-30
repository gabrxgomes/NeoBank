using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoBank.API.Data;
using NeoBank.API.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// CONFIGURAÇÃO DO BANCO DE DADOS
// =============================================
builder.Services.AddDbContext<NeoBankDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=neobank.db"));

// =============================================
// CONFIGURAÇÃO DE AUTENTICAÇÃO JWT
// =============================================
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "NeoBank-Super-Secret-Key-2024-Fintech-Application";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NeoBank",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NeoBank.Users",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// =============================================
// REGISTRO DE SERVIÇOS
// =============================================
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

// =============================================
// CONFIGURAÇÃO DO SWAGGER
// =============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NeoBank API",
        Version = "v1",
        Description = "API de banco digital - Sistema Fintech para gestão de contas e transações",
        Contact = new OpenApiContact
        {
            Name = "NeoBank Team",
            Email = "contato@neobank.com"
        }
    });

    // Configuração de autenticação no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =============================================
// CONFIGURAÇÃO DO CORS
// =============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// =============================================
// PIPELINE DE MIDDLEWARE
// =============================================

// Criar banco de dados automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NeoBankDbContext>();
    db.Database.EnsureCreated();
}

// Swagger sempre habilitado para desenvolvimento
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NeoBank API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Rota raiz para verificar se a API está funcionando
app.MapGet("/", () => Results.Ok(new
{
    message = "NeoBank API is running!",
    version = "1.0.0",
    documentation = "/swagger"
}));

app.Run();
