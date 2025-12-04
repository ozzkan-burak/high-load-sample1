namespace TicketApi.Models;

public class Ticket
{
  public int Id { get; set; }
  public string CustomerName { get; set; } = string.Empty;
  public string EventName { get; set; } = string.Empty;
  public string SeatNumber { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}