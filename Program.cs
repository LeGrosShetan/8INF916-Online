using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis services
var redisConfiguration = builder.Configuration.GetConnectionString("RedisConnection");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfiguration));
builder.Services.AddScoped<IDatabase>(sp =>
{
    var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
    return connectionMultiplexer.GetDatabase();
});

builder.Services.AddScoped<GameServerService>();

// Configure PostgreSQL database
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TODO : Add global variable for SecretKey
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("6a29ce19b1c552a59bc3759e9dfd7c40f764e87cd74aa0a6adeabf180fb7f236c4dcf4f8eb97a806605d2d5cff1f6ca93be91f9cc9e28b7a815d25b58b35cd556ca66f559a3db228f1987f3f545bdfefab754c8d56d38b945385a4225367ed93e4b2384c5c0172486b75598da708907d47b282dcde0c532e3a55cb84e175191465129e86164785bf0f47c2e95979ef5a84ffe2176d9e0d7e7cdfef8a87be14d56759ddd8fffeb5f0528180a45cbc726010fddb8b9cf67e6c52aca36f058b897f2717deeb2806dfdb6a3479c7aca27180cb3a8bcfbf034a3d50d10510c6cc5fba997dc7fc776bc4dd7e5a4fc55db7fb9ceb26ac1c3d09a440309e2a50868f65c4c619ebfdee124a174e86129f0e46ee72903526447cdfe06358bffabe23683370de319203e807b3f2496532ad8525e4c22e2ee2f001140acadbd2ece7abd959ef"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireLoggedIn", policy => policy.RequireAuthenticatedUser());
});

builder.WebHost.UseUrls("http://0.0.0.0:8000");

// anything past this line trying to add things to builder will result in an error
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Enable authentication middleware
app.UseAuthorization();

app.MapControllers();

app.Run();
