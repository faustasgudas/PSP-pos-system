using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Services.Interfaces;
using PsP.Services.Implementations;
using PsP.Settings;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Stripe settings binding
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Service layer
builder.Services.AddScoped<IGiftCardService, GiftCardService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<StripePaymentService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();


// Stripe service – TIK VIENAS registravimas
builder.Services.AddScoped<StripePaymentService>();

// MVC / API
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");
app.MapControllers();

app.Run();