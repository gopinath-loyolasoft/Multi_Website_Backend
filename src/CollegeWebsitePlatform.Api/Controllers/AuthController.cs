using System.Security.Claims;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeWebsitePlatform.Api.Controllers.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<object>.Fail("Username and password are required."));

        var response = await _authService.LoginAsync(request.Username.Trim(), request.Password);
        if (response == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid credentials or account is inactive."));

        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(ApiResponse<object>.Fail("Refresh token is required."));

        var response = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (response == null)
            return Unauthorized(ApiResponse<object>.Fail("Refresh token is invalid, expired, or has been revoked."));

        return Ok(ApiResponse<RefreshTokenResponse>.Ok(response, "Access token refreshed"));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await _authService.LogoutAsync(request.RefreshToken);

        return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully"));
    }

    /// <summary>
    /// Returns the authenticated user's full profile including role, permissions list, and tenant memberships.
    /// Replaces the previous generic ObjectApiResponse with a typed CurrentUserDto.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid user token claims."));

        var profile = await _authService.GetCurrentUserProfileAsync(userId);
        if (profile == null)
            return NotFound(ApiResponse<object>.Fail("User profile not found."));

        return Ok(ApiResponse<CurrentUserDto>.Ok(profile));
    }
}
