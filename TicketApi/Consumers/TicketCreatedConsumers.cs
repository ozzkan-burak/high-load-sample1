using MassTransit;
using Microsoft.Extensions.Caching.Distributed; // Cache kütüphanesi
using TicketApi.Data;
using TicketApi.Models;

namespace TicketApi.Consumers;

public class TicketCreatedConsumer : IConsumer<TicketCreatedEvent>
{
  private readonly AppDbContext _dbContext;
  private readonly IDistributedCache _cache; // Cache servisi
  private readonly ILogger<TicketCreatedConsumer> _logger;

  public TicketCreatedConsumer(AppDbContext dbContext, IDistributedCache cache, ILogger<TicketCreatedConsumer> logger)
  {
    _dbContext = dbContext;
    _cache = cache;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<TicketCreatedEvent> context)
  {
    // 1. RabbitMQ'dan gelen mesajı alıyoruz
    var message = context.Message;

    _logger.LogInformation($"[RabbitMQ] Mesaj alındı: {message.OwnerName}");

    // 2. MAPPING (Dönüştürme) İŞLEMİ
    // RabbitMQ'dan gelen 'TicketCreatedEvent' nesnesini,
    // veritabanının anlayacağı 'Ticket' nesnesine çeviriyoruz.
    var ticket = new Ticket
    {
      // ID vermiyoruz, PostgreSQL otomatik verecek.
      CustomerName = message.OwnerName,
      SeatNumber = message.SeatNumber,
      EventName = message.EventName,
      Price = message.Price,
      CreatedAt = message.CreatedAt
    };

    // 3. Artık elimizde bir 'ticket' nesnesi var, veritabanına ekleyebiliriz
    _dbContext.Tickets.Add(ticket);
    await _dbContext.SaveChangesAsync(); // SQL Insert çalışır ve ID oluşur.

    // 4. Mimarın Dokunuşu: Veri DB'ye girdiğine göre Cache'i silebiliriz.
    await _cache.RemoveAsync("all_tickets");
    _logger.LogInformation("🗑️ Cache temizlendi (Worker tarafından)");
  }
}