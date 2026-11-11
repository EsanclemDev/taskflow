
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.DTOs;
using TaskManager.Api.Filters;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AccessTokenService _accessTokenService;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccessTokenService accessTokenService,
        RefreshTokenService refreshTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto request)
    {
        var existingUser =
            await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                Message = "No se ha podido completar el registro."
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password
        );

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        return StatusCode(201, new
        {
            Message = "Usuario registrado correctamente.",
            UserId = user.Id
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto request)
    {
        var user =
            await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                Message = "Credenciales incorrectas."
            });
        }

        var result =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true
            );

        if (result.IsLockedOut)
        {
            return StatusCode(423, new
            {
                Message = "Cuenta bloqueada temporalmente."
            });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                Message = "Credenciales incorrectas."
            });
        }

        var accessToken =
            await _accessTokenService.GenerateAsync(user);

        var refreshToken =
            await _refreshTokenService.CreateSessionAsync(user);

        SetRefreshTokenCookie(refreshToken);

        return Ok(new
        {
            TokenType = "Bearer",
            AccessToken = accessToken,
            ExpiresIn = 3600
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            Id = user.Id,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        });
    }

    [ValidateAuthOrigin]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken =
            Request.Cookies["taskflow_refresh"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new
            {
                Message = "No existe una sesión válida."
            });
        }

        // Comprobamos el usuario antes de rotar el token.
        var existingSession =
            await _refreshTokenService.FindValidSessionAsync(
                refreshToken
            );

        if (existingSession != null)
        {
            var existingUser =
                await _userManager.FindByIdAsync(
                    existingSession.UserId
                );

            if (existingUser == null ||
                !await _signInManager.CanSignInAsync(existingUser) ||
                await _userManager.IsLockedOutAsync(existingUser))
            {
                await _refreshTokenService.RevokeSessionAsync(
                    refreshToken
                );

                DeleteRefreshTokenCookie();

                return Unauthorized(new
                {
                    Message = "La sesión ya no es válida."
                });
            }
        }

        var result =
            await _refreshTokenService.RotateSessionAsync(
                refreshToken
            );

        if (result.Status != RefreshRotationStatus.Success)
        {
            DeleteRefreshTokenCookie();

            return Unauthorized(new
            {
                Message = result.Status switch
                {
                    RefreshRotationStatus.ExpiredToken =>
                        "Refresh token caducado.",

                    RefreshRotationStatus.AlreadyRotated =>
                        "El token ya ha sido renovado.",

                    RefreshRotationStatus.ReuseDetected =>
                        "Se ha detectado una reutilización de sesión.",

                    _ => "Sesión no válida."
                }
            });
        }

        var user =
            await _userManager.FindByIdAsync(result.UserId!);

        if (user == null ||
            !await _signInManager.CanSignInAsync(user) ||
            await _userManager.IsLockedOutAsync(user))
        {
            // Si el usuario deja de ser válido durante la
            // operación, revocamos la familia recién creada.
            if (result.FamilyId.HasValue)
            {
                await _refreshTokenService.RevokeFamilyAsync(
                    result.FamilyId.Value
                );
            }

            DeleteRefreshTokenCookie();

            return Unauthorized(new
            {
                Message = "La sesión ya no es válida."
            });
        }

        var accessToken =
            await _accessTokenService.GenerateAsync(user);

        SetRefreshTokenCookie(result.RefreshToken!);

        return Ok(new
        {
            TokenType = "Bearer",
            AccessToken = accessToken,
            ExpiresIn = 3600
        });
    }


    [ValidateAuthOrigin]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken =
            Request.Cookies["taskflow_refresh"];

        await _refreshTokenService.LogoutFamilyAsync(
            refreshToken
        );

        DeleteRefreshTokenCookie();

        return NoContent();
    }


    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(
            "taskflow_refresh",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/api/auth",
                MaxAge = TimeSpan.FromDays(7),
                IsEssential = true
            }
        );
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            "taskflow_refresh",
            new CookieOptions
            {
                Path = "/api/auth",
                Secure = true,
                SameSite = SameSiteMode.None
            }
        );
    }
}
