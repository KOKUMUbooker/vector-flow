using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VectorFlow.Api.Services.Interfaces;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Api.Controllers;

[ApiController]
[Route("/api/auth/")]
public class AuthController(
    IAuthService authService, 
    IHostEnvironment env,
    IConfiguration configuration) : ControllerBase
{
   [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await authService.LoginAsync(request,baseUrl);
        if (!result.Succeeded && !result.EmailNotVerified)
            return Unauthorized(new { Message = "Invalid email or password." });

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
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await authService.RegisterAsync(request,baseUrl);
        if (!result.Succeeded) return BadRequest(result.Errors);
        return Ok();
    }

    // ── Email verification ────────────────────────────────────────────────────
 
    /// <summary>
    /// Linked from the verification email. Validates the token and
    /// redirects the user to the client app with a success or error state.
    /// </summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { Message = "Verification token is required." });
 
        var result = await authService.VerifyEmailAsync(token);
 
        // Redirect to the Blazor client with the outcome as a query param
        // so the UI can show the appropriate message without an extra API call
        var clientBase = GetClientBaseUrl();
 
        if (result.Succeeded)
            return Redirect($"{clientBase}/email-verified?email={Uri.EscapeDataString(result.RedirectEmail!)}");
 
        if (result.AlreadyVerified)
            return Redirect($"{clientBase}/email-verified?already=true");
 
        if (result.TokenExpired)
            return Redirect($"{clientBase}/verify-email?error=expired");
 
        // Invalid token
        return Redirect($"{clientBase}/verify-email?error=invalid");
    }

    // ── Resend verification ───────────────────────────────────────────────────
 
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await authService.ResendVerificationAsync(request.Email, baseUrl);
 
        // Always return 200 — don't reveal whether the email is registered
        return Ok(new { Message = "If your email is registered and unverified, a new link has been sent." });
    }

    // ── Forgot password ───────────────────────────────────────────────────────
 
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await authService.ForgotPasswordAsync(request.Email, baseUrl);
 
        // Always return 200 — don't reveal whether the email is registered
        return Ok(new { Message = "If an account with that email exists, a reset link has been sent." });
    }

    // ── Reset password ────────────────────────────────────────────────────────
 
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto request)
    {
        var result = await authService.ResetPasswordAsync(request);
 
        if (!result.Succeeded)
            return BadRequest(new { Message = result.Error });
 
        // Clear cookies — all sessions were revoked server-side
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
 
        return Ok(new { Message = "Password reset successfully. Please log in with your new password." });
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
        if (result is null || result.AccessToken is null || result.RefreshToken is null) 
            return Unauthorized("Invalid or expired refresh token.");

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

    // For this to work with blazor wasm site ensure your request that requires server to set
    // cookies in res, makes the request with this 
    //      request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
    // That is if server & client are running separately
    private void AttachTokenCookies(string accessToken, string refreshToken)
    {
        var accessTknExpMins = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes", 15);
        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsDevelopment() ? false : true, // Ensure cookie only works under https connection
            SameSite = env.IsDevelopment() ? SameSiteMode.None :  SameSiteMode.Strict, 
            Expires = DateTime.UtcNow.AddMinutes(accessTknExpMins),
            IsEssential = true
        };

        var refreshTknExpDays = configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);
        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsDevelopment() ? false : true, // Ensure cookie only works under https connection
            SameSite = env.IsDevelopment() ? SameSiteMode.None :  SameSiteMode.Strict, 
            Expires = DateTime.UtcNow.AddDays(refreshTknExpDays),
            IsEssential = true
        };

        Response.Cookies.Append("accessToken", accessToken, accessOptions);
        Response.Cookies.Append("refreshToken", refreshToken, refreshOptions);
    }

    /// <summary>
    /// Returns the Blazor client base URL.
    /// In development the client runs on a different port to the API.
    /// In production they can be on the same domain or configured separately.
    /// </summary>
    private string GetClientBaseUrl()
    {
        // Prefer an explicit config value — falls back to same origin
        return HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["ClientBaseUrl"]
            ?? $"{Request.Scheme}://{Request.Host}";
    }
}

