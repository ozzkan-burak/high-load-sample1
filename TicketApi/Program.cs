using Microsoft.EntityFrameworkCore;
using TicketApi.Data;

var builder = WebApplication.CreateBuilder(args);

// --- MİMARİ AYAR BAŞLANGICI ---

// Docker-Compose dosyasındaki "ConnectionStrings__Postgres" ortam değişkenini otomatik okur.
// Eğer ortam değişkeni yoksa (Localde çalışıyorsan) appsettings.json'a bakar.
var connectionString = builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- MİMARİ AYAR BİTİŞİ ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Veritabanını otomatik oluştur (Migration yerine EnsureCreated kullanıyoruz)
// NOT: Production'da bu tehlikelidir ama geliştirme ortamı için hayat kurtarır.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();

        // Veritabanını oluştur (yoksa) - Migration history tutmaz
        dbContext.Database.EnsureCreated();

        logger.LogInformation("✅ Veritabanı başarıyla oluşturuldu/kontrol edildi.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Veritabanı oluşturulurken hata: {Message}", ex.Message);
    }
}

// Docker-Compose'dan gelen "ConnectionStrings__Redis" değerini okur.
var redisConnection = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "TarkanBilet_"; // Key'lerin başına eklenir (Örn: TarkanBilet_TicketList)
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();