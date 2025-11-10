using CafApi.Data;
using CafApi.Models.Sample;
using CafApi.Models.SampleState;
using CafApi.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();


builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;      // respuestas camelCase
        o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;                     // acepta camelCase al recibir
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

const string CorsPolicy = "FrontendPolicy";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, p =>
    {
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod();
    });
});

// 🔹 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese: Bearer {token}"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Db y repos
builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<UsersRepository>();
builder.Services.AddScoped<ClientsRepository>();
builder.Services.AddScoped<ProcessRepository>();
builder.Services.AddScoped<PrendaRepository>();
builder.Services.AddScoped<ISampleStatusRepository, SampleStatusRepository>();
builder.Services.AddScoped<ISampleRepository, SampleRepository>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<RolesRepository>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key (env var JWT__KEY).");

var issuer = builder.Configuration["Jwt:Issuer"] ?? "CafApi";
var audience = builder.Configuration["Jwt:Audience"] ?? "CafApiUsers";
// 🔧 FIX: tu config usa Jwt:ExpireMinutes (no ExpMinutes)
var expMinutes = int.TryParse(builder.Configuration["Jwt:ExpireMinutes"], out var m) ? m : 300;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        opt.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { ok = false, message = "Acceso denegado." });
                await ctx.Response.WriteAsync(payload);
            }
        };
    });

static AuthorizationPolicy BuildPermPolicy(string codigoPermiso)
{
    return new AuthorizationPolicyBuilder()
        .RequireAssertion(ctx =>
            ctx.User.IsInRole("Administrador") ||
            ctx.User.HasClaim(c => c.Type == "perm" && c.Value == codigoPermiso))
        .Build();
}

builder.Services.AddAuthorization(opt =>
{
    // Prenda
    opt.AddPolicy("Prenda.Crear", BuildPermPolicy("Prenda.Crear"));
    opt.AddPolicy("Prenda.Editar", BuildPermPolicy("Prenda.Editar"));
    opt.AddPolicy("Prenda.Eliminar", BuildPermPolicy("Prenda.Eliminar"));

    // Muestra
    opt.AddPolicy("Muestra.Crear", BuildPermPolicy("Muestra.Crear"));
    opt.AddPolicy("Muestra.Editar", BuildPermPolicy("Muestra.Editar"));
    opt.AddPolicy("Muestra.Eliminar", BuildPermPolicy("Muestra.Eliminar"));

    // Usuarios
    opt.AddPolicy("Usuarios.Listado", BuildPermPolicy("Usuarios.Listado"));
    opt.AddPolicy("Usuarios.Crear", BuildPermPolicy("Usuarios.Crear"));
    opt.AddPolicy("Usuarios.Editar", BuildPermPolicy("Usuarios.Editar"));
    opt.AddPolicy("Usuarios.Eliminar", BuildPermPolicy("Usuarios.Eliminar"));

    // Clientes
    opt.AddPolicy("Clientes.Crear", BuildPermPolicy("Clientes.Crear"));
    opt.AddPolicy("Clientes.Editar", BuildPermPolicy("Clientes.Editar"));
    opt.AddPolicy("Clientes.Eliminar", BuildPermPolicy("Clientes.Eliminar"));
});


var app = builder.Build();

// 🔹 Habilitar Swagger en runtime
app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        ctx.RequestServices.GetRequiredService<ILoggerFactory>()
           .CreateLogger("Global").LogError(ex, "Unhandled");
        throw; // deja que Railway registre el 500
    }
});

//app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
