using SecureChat.BLL;

var builder = WebApplication.CreateBuilder(args);

// Dependency injection

builder.Services.AddBllServices(builder.Configuration);

// Server settings and CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed((host) => true)
              .AllowCredentials(); // required for SignalR
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// (Pipline)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.MapHub<SecureChat.API.Hubs.ChatHub>("/chatHub");

app.Run();