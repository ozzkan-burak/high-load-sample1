using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApi.Data;
using TicketApi.Models;

namespace TicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
  private readonly AppDbContext _dbContext;
  public TicketController(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }
  [HttpGet]
  public async Task<IActionResult> GetTickets()
  {
    var tickets = await _dbContext.Tickets.ToListAsync();
    return Ok(tickets);
  }
  [HttpPost]
  public async Task<IActionResult> BuyTicket(Ticket ticket)
  {
    await _dbContext.Tickets.AddAsync(ticket);
    await _dbContext.SaveChangesAsync();
    return CreatedAtAction(nameof(GetTickets), new { id = ticket.Id }, ticket);
  }

}