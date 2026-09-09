using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;
using System.Security.Claims;

namespace MilkshopSystem.Web.Controllers
{
    public class NotesController : BaseController
    {
        private readonly INoteRepository _noteRepo;

        public NotesController(INoteRepository noteRepo)
        {
            _noteRepo = noteRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Json(new List<Note>());

            var notes = await _noteRepo.GetByUserAsync(userId.Value);
            return Json(notes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] NoteInput input)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(input.Content))
                return BadRequest(new { message = "Note cannot be empty" });

            var note = new Note
            {
                UserId = userId.Value,
                Content = input.Content.Trim(),
                Color = string.IsNullOrWhiteSpace(input.Color) ? "yellow" : input.Color
            };

            var id = await _noteRepo.CreateAsync(note);
            note.Id = id;
            note.CreatedDate = DateTime.Now;
            return Json(note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [FromBody] NoteInput input)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var existing = await _noteRepo.GetByIdAsync(id, userId.Value);
            if (existing is null) return NotFound();

            if (string.IsNullOrWhiteSpace(input.Content))
                return BadRequest(new { message = "Note cannot be empty" });

            existing.Content = input.Content.Trim();
            existing.Color = string.IsNullOrWhiteSpace(input.Color) ? existing.Color : input.Color;
            existing.IsPinned = input.IsPinned ?? existing.IsPinned;

            await _noteRepo.UpdateAsync(existing);
            return Json(existing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            await _noteRepo.DeleteAsync(id, userId.Value);
            return Json(new { success = true });
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public class NoteInput
    {
        public string Content { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool? IsPinned { get; set; }
    }
}