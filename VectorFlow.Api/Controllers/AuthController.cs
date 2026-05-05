using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VectorFlow.Api.Services.Interfaces;
using VectorFlow.Api.DTOs;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("/api/auth/")]
public class AuthController(IAuthService authService, IHostEnvironment env) : ControllerBase
{
   [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        if (!result.Succeeded && !result.EmailNotVerified)
            return Unauthorized(new { Message = "Invalid email or password." });

        // TODO: On client to check whether email is not verified, check for Forbidden HTTP status code
        if (result.EmailNotVerified)
            return StatusCode(403, new
            {
                Message = "Email not verified. A new verification link has been sent to your email.",
                EmailVerificationToken = result.EmailVerificationToken
            });

        AttachTokenCookies(result.AccessToken!, result.RefreshToken!);
        return Ok(result.User);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        if (!result.Succeeded) return BadRequest(result.Errors);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await authService.GetUserAsync(userId!);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken)) return Unauthorized();

        var result = await authService.RefreshAsync(refreshToken);
        if (result is null) return Unauthorized("Invalid or expired refresh token.");

        AttachTokenCookies(result.AccessToken, result.RefreshToken);
        return Ok();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
            await authService.RevokeRefreshTokenAsync(refreshToken);

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return Ok();
    }

    private void AttachTokenCookies(string accessToken, string refreshToken)
    {
        var secure = !HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("accessToken", accessToken, accessOptions);
        Response.Cookies.Append("refreshToken", refreshToken, refreshOptions);
    }
}

