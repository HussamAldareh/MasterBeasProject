using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MasterBeasProject.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinBookingChat(int bookingId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.EngineerProfile)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return;

            bool isParticipant = booking.ClientId == userId ||
                                 booking.EngineerProfile.UserId == userId;
            if (!isParticipant) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"booking_{bookingId}");
        }

        public async Task SendMessage(int bookingId, string message)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(message) || message.Length > 1000) return;

            var booking = await _context.Bookings
                .Include(b => b.EngineerProfile)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return;

            bool isParticipant = booking.ClientId == userId ||
                                 booking.EngineerProfile.UserId == userId;
            if (!isParticipant) return;

            var chatMessage = new ChatMessage
            {
                BookingId = bookingId,
                SenderId = userId!,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            var sender = await _context.Users.FindAsync(userId);

            await Clients.Group($"booking_{bookingId}").SendAsync("ReceiveMessage", new
            {
                id = chatMessage.Id,
                senderName = sender?.UserName ?? "مجهول",
                senderId = userId,
                message = chatMessage.Message,
                sentAt = chatMessage.SentAt.ToString("hh:mm tt")
            });
        }
    }
}