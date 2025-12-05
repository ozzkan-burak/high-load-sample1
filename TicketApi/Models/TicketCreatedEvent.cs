namespace TicketApi.Models;

public class TicketCreatedEvent
{
  public int TicketId { get; set; }
  public string OwnerName { get; set; } = string.Empty;
  public string EventName { get; set; } = string.Empty;
  public string SeatNumber { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public DateTime CreatedAt { get; set; }
}