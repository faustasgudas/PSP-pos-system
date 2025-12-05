using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PsP.Data;
using PsP.Services.Implementations;
using PsP.Services.Interfaces;
using PsP.Settings;

var builder = WebApplication.CreateBuilder(args);

// ========== DB ==========
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========== STRIPE ==========
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ========== JWT SETTINGS + AUTH ==========
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Key)
            ),

            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// ========== SERVICE LAYER ==========
builder.Services.AddScoped<IGiftCardService, GiftCardService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
builder.Services.AddScoped<ICatalogItemsService, CatalogItemsService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IDiscountsService, DiscountsService>();

// Stripe service – TIK VIENAS registravimas
builder.Services.AddScoped<StripePaymentService>();

// ========== MVC / API ==========
builder.Services.AddControllers();

// ========== SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PsP API",
        Version = "v1"
    });

    // JWT schema – Http type, ne ApiKey
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,   // svarbu
        Scheme = "Bearer",                // svarbu (mažosiom)
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste JWT token only. The 'Bearer ' prefix will be added automatically."
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

// ========== CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// ========== PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
