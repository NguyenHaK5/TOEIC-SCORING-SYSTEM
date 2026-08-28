using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Auth;
using Toeic.Application.Auth.Dtos;
using Toeic.Application.Common.Exceptions;
using Toeic.Domain.Enums;

namespace Toeic.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng ký công khai — LUÔN tạo tài khoản Role=CANDIDATE, bất kể client gửi gì khác.
    /// Muốn tạo tài khoản ADMIN/RATER phải dùng /register-staff (chỉ ADMIN gọi được).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var profile = await _authService.RegisterAsync(request, UserRole.CANDIDATE, ct);
        return CreatedAtAction(nameof(Me), null, profile);
    }

    /// <summary>Chỉ ADMIN được tạo tài khoản ADMIN/RATER. Role=CANDIDATE bị từ chối ở đây (dùng /register).</summary>
    [HttpPost("register-staff")]
    [Authorize(Roles = nameof(UserRole.ADMIN))]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterStaffRequest request, CancellationToken ct)
    {
        if (request.Role == UserRole.CANDIDATE)
        {
            throw new ValidationAppException("Dùng endpoint /api/v1/auth/register để tạo tài khoản CANDIDATE.");
        }

        var profile = await _authService.RegisterAsync(request, request.Role, ct);
        return CreatedAtAction(nameof(Me), null, profile);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshAsync(request, ip, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous] // Sở hữu refresh token hợp lệ đã là bằng chứng đủ để revoke chính nó.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var profile = await _authService.GetProfileAsync(CurrentUserId, ct);
        return Ok(profile);
    }
}
