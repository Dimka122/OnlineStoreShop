using ECommerceShop.Data;
using ECommerceShop.DTOs;
using ECommerceShop.Models;
using ECommerceShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _userManager = userManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid registration data", Errors = GetModelStateErrors() });

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                    return BadRequest(new { Message = "User with this email already exists" });

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    City = model.City,
                    PostalCode = model.PostalCode,
                    Country = model.Country,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                    return BadRequest(new { Message = "Failed to create user", Errors = GetIdentityErrors(result) });

                // Assign User role
                await _userManager.AddToRoleAsync(user, "User");

                // Create shopping cart for the user
                var shoppingCart = new ShoppingCart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ShoppingCarts.Add(shoppingCart);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var authResponse = await _jwtService.GenerateTokenAsync(user);

                return Ok(new
                {
                    Message = "User registered successfully",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid login data", Errors = GetModelStateErrors() });

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return BadRequest(new { Message = "Invalid email or password" });

                var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!isPasswordValid)
                    return BadRequest(new { Message = "Invalid email or password" });

                if (!user.EmailConfirmed)
                    return BadRequest(new { Message = "Please confirm your email first" });

                // Generate JWT token
                var authResponse = await _jwtService.GenerateTokenAsync(user);

                return Ok(new
                {
                    Message = "Login successful",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Unauthorized(new { Message = "User not found" });

                // Generate new JWT token
                var authResponse = await _jwtService.GenerateTokenAsync(user);

                return Ok(new
                {
                    Message = "Token refreshed successfully",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                var roles = await _userManager.GetRolesAsync(user);

                var userDto = new UserDTO
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    City = user.City,
                    PostalCode = user.PostalCode,
                    Country = user.Country,
                    Roles = roles.ToList()
                };

                return Ok(new
                {
                    Message = "User retrieved successfully",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] RegisterRequestDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid profile data", Errors = GetModelStateErrors() });

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                // Update user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.City = model.City;
                user.PostalCode = model.PostalCode;
                user.Country = model.Country;

                // Update password if provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordResult.Succeeded)
                        return BadRequest(new { Message = "Failed to update password", Errors = GetIdentityErrors(passwordResult) });
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(new { Message = "Failed to update profile", Errors = GetIdentityErrors(result) });

                // Generate new JWT token
                var authResponse = await _jwtService.GenerateTokenAsync(user);

                return Ok(new
                {
                    Message = "Profile updated successfully",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // For JWT, logout is handled on the client side by removing the token
            return Ok(new { Message = "Logout successful" });
        }

        private List<string> GetModelStateErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
        }

        private List<string> GetIdentityErrors(IdentityResult result)
        {
            return result.Errors
                .Select(e => e.Description)
                .ToList();
        }
    }
}