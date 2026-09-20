using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TEContigo.Infrastructure.Database;
using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.Auth.Repositories;
using TEContigo.Modules.Auth.Services;
using TEContigo.Modules.Email.Services;
using TEContigo.Modules.FoundItems.Repositories;
using TEContigo.Modules.FoundItems.Services;
using TEContigo.Modules.LostItems.Repositories;
using TEContigo.Modules.LostItems.Services;
using TEContigo.Modules.Matches.Repositories;
using TEContigo.Modules.Matches.Services;
using TEContigo.Modules.Reports.Repositories;
using TEContigo.Modules.Reports.Services;
using TEContigo.Modules.Users.Repositories;
using TEContigo.Modules.Users.Services;
using TEContigo.Shared.Security;
using TEContigo.Shared.Security.CurrentUser;
using TEContigo.Shared.Security.Password;
using TEContigo.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IUsersService, UsersService>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<IReportsService, ReportsService>();

builder.Services.AddScoped<ILostItemsService,LostItemsService>();
builder.Services.AddScoped<ILostItemsRepository,LostItemsRepository>();

builder.Services.AddScoped<IFoundItemsRepository,FoundItemsRepository>();
builder.Services.AddScoped<IFoundItemsService,FoundItemsService>();

builder.Services.AddScoped<IMatchesRepository, MatchesRepository>();
builder.Services.AddScoped<IMatchesService, MatchesService>();

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var regionName =
        configuration["AWS:Region"]
        ?? throw new InvalidOperationException(
            "AWS:Region no está configurado.");

    var accessKey =
        configuration["AWS:AccessKeyId"]
        ?? throw new InvalidOperationException(
            "AWS:AccessKeyId no está configurado.");

    var secretKey =
        configuration["AWS:SecretAccessKey"]
        ?? throw new InvalidOperationException(
            "AWS:SecretAccessKey no está configurado.");

    var sessionToken =
        configuration["AWS:SessionToken"]
        ?? throw new InvalidOperationException(
            "AWS:SessionToken no está configurado.");

    var credentials = new SessionAWSCredentials(
        accessKey,
        secretKey,
        sessionToken);

    return new AmazonS3Client(
        credentials,
        RegionEndpoint.GetBySystemName(regionName));
});

builder.Services.AddScoped<IImageService, S3ImageService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "La clave JWT no está configurada.");

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
