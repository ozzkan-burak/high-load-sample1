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

// Veritabanını otomatik oluştur (Migration uygula)
// NOT: Production'da bu tehlikelidir ama geliştirme ortamı için hayat kurtarır.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Veritabanı yoksa oluştur, tabloları güncelle
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();