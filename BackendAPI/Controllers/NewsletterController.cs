using API.Services;
using BackendAPI.Models.Newsletter;
using BackendAPI.Services.Newsletter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/newsletter")]
    public class NewsletterController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        public NewsletterController(ApplicationDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        // POST api/newsletter/subscribe
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _db.NewsletterSubscribers
                .AnyAsync(s => s.Email == request.Email.ToLower());

            if (exists)
                return Conflict(new { message = "Dit e-mailadres is al aangemeld." });

            _db.NewsletterSubscribers.Add(new NewsletterSubscriberModel
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower(),
                SubscribedAtUtc = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(new { message = "Bedankt voor je aanmelding!" });
        }

        // GET api/newsletter/subscribers  (Manager only)
        [HttpGet("subscribers")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetSubscribers()
        {
            var list = await _db.NewsletterSubscribers
                .OrderBy(s => s.SubscribedAtUtc)
                .Select(s => new { s.Id, s.Email, s.SubscribedAtUtc })
                .ToListAsync();

            return Ok(list);
        }

        // DELETE api/newsletter/subscribers/{id}  (Manager only)
        [HttpDelete("subscribers/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteSubscriber(Guid id)
        {
            var sub = await _db.NewsletterSubscribers.FindAsync(id);
            if (sub is null) return NotFound();

            _db.NewsletterSubscribers.Remove(sub);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/newsletter/send  (Manager only)
        [HttpPost("send")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Send([FromBody] SendMailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emails = await _db.NewsletterSubscribers
                .Select(s => s.Email)
                .ToListAsync();

            if (emails.Count == 0)
                return BadRequest(new { message = "Er zijn geen aangemelde abonnees." });

            await _email.SendAsync(emails, request.Subject, request.HtmlBody);

            return Ok(new { message = $"Mail verstuurd naar {emails.Count} abonnee(s)." });
        }
    }

    public class SubscribeRequest
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = "";
    }

    public class SendMailRequest
    {
        [Required, MaxLength(200)]
        public string Subject { get; set; } = "";

        [Required]
        public string HtmlBody { get; set; } = "";
    }
}
