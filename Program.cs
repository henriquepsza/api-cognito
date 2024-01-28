using System.Text;
using Amazon.CognitoIdentityProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAWSService<IAmazonCognitoIdentityProvider>();

// Adicione serviços de autenticação
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.Authority = builder.Configuration["AWS:Authority"];
        options.Audience = builder.Configuration["AWS:AppClientId"];
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes($"{builder.Configuration["AWS:CognitoSecret"]}")),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = $"https://cognito-idp.{builder.Configuration["AWS:Region"]}.amazonaws.com/{builder.Configuration["AWS:UserPoolId"]}",
            ValidAudience = builder.Configuration["AWS:AppClientId"]
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Configura o Swagger, que é uma ferramenta para documentar APIs
// Isso ajuda no desenvolvimento, testes e documentação de APIs
builder.Services.AddSwaggerGen(c =>
{
    // Define as informações básicas para a documentação do Swagger
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minha API", Version = "v1" });

    // Adiciona a definição de segurança para o método de autenticação Bearer (JWT)
    // Isso permite que o Swagger envie tokens JWT nas solicitações de API para endpoints protegidos
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", // Nome do cabeçalho usado para passar o token
        Type = SecuritySchemeType.Http, // Tipo de esquema de segurança
        Scheme = "Bearer", // Esquema de autorização, neste caso, Bearer
        BearerFormat = "JWT", // Formato do token, JWT neste caso
        In = ParameterLocation.Header, // O token deve ser enviado no cabeçalho da solicitação
        Description = "Autenticação JWT usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\""
    });

    // Adiciona o requisito de segurança que aplica o esquema de segurança definido acima
    // a todas as operações no Swagger. Isso exige que o token JWT seja fornecido para acessar os endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // Referencia a definição de segurança "Bearer" definida acima
                }
            },
            Array.Empty<string>() // Um array vazio indica que nenhum escopo é necessário
        }
    });
});

var app = builder.Build();

// Configura o middleware do Swagger apenas em ambientes de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Habilita o middleware para servir o JSON do Swagger
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minha API v1")); // Habilita a UI do Swagger
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();