using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TicketApi.Data;
using TicketApi.Models;

namespace TicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
  private readonly AppDbContext _dbContext;
  private readonly IDistributedCache _cache;
  private readonly ILogger<TicketController> _logger;

  private const string TICKETS_CACHE_KEY = "all_tickets";

  public TicketController(
      AppDbContext dbContext,
      IDistributedCache cache,
      ILogger<TicketController> logger)
  {
    _dbContext = dbContext;
    _cache = cache;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetTickets()
  {
    // 1. Önce Redis'ten kontrol et
    var cachedTickets = await _cache.GetStringAsync(TICKETS_CACHE_KEY);

    if (!string.IsNullOrEmpty(cachedTickets))
    {
      _logger.LogInformation("✅ Cache'den veri döndürüldü");
      var tickets = JsonSerializer.Deserialize<List<Ticket>>(cachedTickets);
      return Ok(new { source = "cache", data = tickets });
    }

    // 2. Cache'de yoksa veritabanından çek
    _logger.LogInformation("⚠️ Cache'de veri yok, veritabanından çekiliyor...");
    await Task.Delay(2000); // Veritabanı gecikmesi simülasyonu

    var ticketsFromDb = await _dbContext.Tickets.ToListAsync();

    // 3. Redis'e kaydet (5 dakika süreyle)
    var cacheOptions = new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    var serializedTickets = JsonSerializer.Serialize(ticketsFromDb);
    await _cache.SetStringAsync(TICKETS_CACHE_KEY, serializedTickets, cacheOptions);

    _logger.LogInformation("💾 Veri Redis'e kaydedildi");

    return Ok(new { source = "database", data = ticketsFromDb });
  }

  [HttpPost]
  public async Task<IActionResult> BuyTicket(Ticket ticket)
  {
    await _dbContext.Tickets.AddAsync(ticket);
    await _dbContext.SaveChangesAsync();

    // ⚠️ Yeni ticket eklendiğinde cache'i temizle
    await _cache.RemoveAsync(TICKETS_CACHE_KEY);
    _logger.LogInformation("🗑️ Cache temizlendi (yeni ticket eklendi)");

    return CreatedAtAction(nameof(GetTickets), new { id = ticket.Id }, ticket);
  }

  // 🆕 Cache'i manuel temizlemek için endpoint
  [HttpDelete("cache")]
  public async Task<IActionResult> ClearCache()
  {
    await _cache.RemoveAsync(TICKETS_CACHE_KEY);
    _logger.LogInformation("🗑️ Cache manuel olarak temizlendi");
    return Ok(new { message = "Cache başarıyla temizlendi" });
  }

  // 🆕 Cache durumunu kontrol et
  [HttpGet("cache/status")]
  public async Task<IActionResult> GetCacheStatus()
  {
    var cachedData = await _cache.GetStringAsync(TICKETS_CACHE_KEY);
    var isCached = !string.IsNullOrEmpty(cachedData);

    return Ok(new
    {
      isCached = isCached,
      message = isCached ? "Cache'de veri var" : "Cache boş"
    });
  }
}