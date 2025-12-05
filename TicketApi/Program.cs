using Microsoft.EntityFrameworkCore;
using TicketApi.Data;
using MassTransit;
using TicketApi.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Redis Cache - ✅ app.Build() ÖNCE ekle!
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "TicketCache_";
});

// --- RABBITMQ (MassTransit) AYARLARI ---
builder.Services.AddMassTransit(x =>
{
    // Tüketiciyi (Consumer) tanıtıyoruz
    x.AddConsumer<TicketCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Docker-Compose'dan gelen RabbitMQ adresini al
        // ConnectionStrings__RabbitMQ=host=rabbitmq_broker
        var rabbitConn = builder.Configuration.GetConnectionString("RabbitMQ");

        // Eğer bağlantı stringi "host=..." formatındaysa parse etmemiz gerekebilir
        // Basitlik olsun diye direkt host adını veriyoruz:
        cfg.Host("rabbitmq_broker", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Kuyruk ayarları
        cfg.ReceiveEndpoint("ticket-queue", e =>
        {
            e.ConfigureConsumer<TicketCreatedConsumer>(context);
        });
    });
});

var app = builder.Build();

// Veritabanını otomatik oluştur
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        logger.LogInformation("✅ Veritabanı başarıyla oluşturuldu/kontrol edildi.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Veritabanı oluşturulurken hata: {Message}", ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();