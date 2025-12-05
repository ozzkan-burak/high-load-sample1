using MassTransit; // RabbitMQ için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed; // Redis için
using System.Text.Json; // JSON işlemleri için
using TicketApi.Data;
using TicketApi.Models;

namespace TicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly IDistributedCache _cache;
  private readonly IPublishEndpoint _publishEndpoint; // RabbitMQ mesajcısı

  // Constructor: Bağımlılıkları (Dependency Injection) alıyoruz
  public TicketController(AppDbContext context, IDistributedCache cache, IPublishEndpoint publishEndpoint)
  {
    _context = context;
    _cache = cache;
    _publishEndpoint = publishEndpoint;
  }

  // GET: Tüm biletleri listele (Redis Cache-Aside Desenli)
  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    string cacheKey = "ticket_list";

    // 1. ADIM: Cache'e bak (Redis)
    var cachedTickets = await _cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedTickets))
    {
      // Cache'te varsa veritabanına gitmeden dön
      var ticketsFromCache = JsonSerializer.Deserialize<List<Ticket>>(cachedTickets);
      return Ok(ticketsFromCache);
    }

    // 2. ADIM: Cache'te yoksa Veritabanına git
    // (Yük testi sırasında farkı görmek için yapay gecikme eklenebilir, şu an kapalı)
    // await Task.Delay(2000); 

    var ticketsFromDb = await _context.Tickets.ToListAsync();

    // 3. ADIM: Veriyi Cache'e yaz
    var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    string jsonString = JsonSerializer.Serialize(ticketsFromDb, jsonOptions);

    var cacheOptions = new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) // 1 dakika ömür biç
    };

    await _cache.SetStringAsync(cacheKey, jsonString, cacheOptions);

    return Ok(ticketsFromDb);
  }

  // POST: Bilet Satın Al (RabbitMQ - Asenkron Desenli)
  [HttpPost]
  public async Task<IActionResult> BuyTicket(Ticket ticket)
  {
    // 1. Veri paketini hazırla (Event Nesnesi)
    // Veritabanı nesnesini (Entity) doğrudan kuyruğa atmak yerine,
    // sadece gerekli verileri taşıyan hafif bir DTO (Data Transfer Object) oluşturuyoruz.
    var ticketEvent = new TicketCreatedEvent
    {
      OwnerName = ticket.CustomerName,
      EventName = ticket.EventName,
      Price = ticket.Price,
      CreatedAt = DateTime.UtcNow
    };

    // 2. Kuyruğa Mesaj Bırak (Fire and Forget)
    // Veritabanına yazma işlemini burada YAPMIYORUZ.
    // Cache silme işlemini burada YAPMIYORUZ.
    // Sadece mesajı RabbitMQ'ya teslim ediyoruz.
    await _publishEndpoint.Publish(ticketEvent);

    // 3. Kullanıcıya "Sıraya Alındı" (202 Accepted) dön
    // Artık veritabanını beklemediğimiz için bu cevap milisaniyeler içinde döner.
    return Accepted(new { status = "Sıraya alındı. İşleminiz arka planda yapılıyor.", ticketInfo = ticketEvent });
  }
}