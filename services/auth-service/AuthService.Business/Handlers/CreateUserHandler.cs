using AuthService.Common.Exceptions;
using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using AuthService.Data;
using AuthService.Data.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Handlers;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating new user with username: {Username}", request.Username);

            if (request.Password != request.ConfirmPassword)
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = "Passwords do not match"
                };
            }

            if (await _unitOfWork.Users.AnyAsync(u => u.Username == request.Username))
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = "Username is already taken"
                };
            }

            if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = "Email is already registered"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(newUser);
                await _unitOfWork.CompleteAsync();

                // Add user roles
                foreach (var roleName in request.Roles)
                {
                    var role = await _unitOfWork.Roles.GetByNameAsync(roleName);
                    if (role != null)
                    {
                        var userRole = new UserRole
                        {
                            UserId = newUser.Id,
                            RoleId = role.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.UserRoles.AddAsync(userRole);
                    }
                    else
                    {
                        _logger.LogWarning("Role {RoleName} not found when creating user", roleName);
                    }
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(newUser.Id);
                var userDto = new UserDto
                {
                    Id = userWithRoles.Id,
                    Username = userWithRoles.Username,
                    Email = userWithRoles.Email,
                    Roles = userWithRoles.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList() ?? []
                };

                return new CreateUserResponse
                {
                    Success = true,
                    Message = "User created successfully",
                    User = userDto
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (BusinessValidationException ex)
        {
            _logger.LogWarning("Business validation failed when creating user: {Message}", ex.Message);
            return new CreateUserResponse
            {
                Success = false,
                Message = string.Join(", ", ex.ValidationErrors)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user with username: {Username}", request.Username);
            return new CreateUserResponse
            {
                Success = false,
                Message = "Error occurred while creating user"
            };
        }
    }
}