using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Auth;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/profilephoto")]
[Authorize]
public class ProfilePhotoController : ControllerBase
{
    private const int MaxPhotoBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly AppDbContext _db;

    public ProfilePhotoController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<UploadProfilePhotoResponse>> UploadPhoto(IFormFile? file)
    {
        var userId = User.GetUserId();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Dosya boş veya eksik." });

        if (file.Length > MaxPhotoBytes)
            return BadRequest(new { message = "Fotoğraf çok büyük. En fazla 2 MB olmalı." });

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedContentTypes.Contains(contentType))
            return BadRequest(new { message = "Desteklenmeyen fotoğraf türü." });

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        user.ProfilePhotoBytes = ms.ToArray();
        user.ProfilePhotoContentType = contentType;
        user.ProfilePhotoUpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new UploadProfilePhotoResponse
        {
            ProfilePhotoUpdatedAtUtc = user.ProfilePhotoUpdatedAtUtc.Value
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPhoto()
    {
        var userId = User.GetUserId();

        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.ProfilePhotoBytes, u.ProfilePhotoContentType })
            .FirstOrDefaultAsync();

        if (user is null) return NotFound();
        if (user.ProfilePhotoBytes is null || user.ProfilePhotoBytes.Length == 0) return NotFound();

        return File(user.ProfilePhotoBytes, user.ProfilePhotoContentType ?? "image/jpeg",
            enableRangeProcessing: false);
    }

    [HttpDelete]
    public async Task<IActionResult> DeletePhoto()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.ProfilePhotoBytes = null;
        user.ProfilePhotoContentType = null;
        user.ProfilePhotoUpdatedAtUtc = null;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
