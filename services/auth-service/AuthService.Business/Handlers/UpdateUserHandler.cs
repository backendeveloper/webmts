using AuthService.Common.Exceptions;
using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using AuthService.Data;
using AuthService.Data.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Handlers;

public class UpdateUserHandler : IRequestHandler<UpdateUserRequest, UpdateUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserHandler> _logger;

    public UpdateUserHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UpdateUserResponse> Handle(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating user with ID: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new UpdateUserResponse
                {
                    Success = false,
                    Message = $"User with ID {request.UserId} not found"
                };
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return new UpdateUserResponse
                    {
                        Success = false,
                        Message = "Passwords do not match"
                    };
                }
            }

            // Check if username is being changed and if it's already taken
            if (!string.IsNullOrEmpty(request.Username) && user.Username != request.Username &&
                await _unitOfWork.Users.AnyAsync(u => u.Username == request.Username))
            {
                return new UpdateUserResponse
                {
                    Success = false,
                    Message = "Username is already taken"
                };
            }

            // Check if email is being changed and if it's already taken
            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email &&
                await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new UpdateUserResponse
                {
                    Success = false,
                    Message = "Email is already registered"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Update user properties
                if (!string.IsNullOrEmpty(request.Username))
                    user.Username = request.Username;

                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;

                if (!string.IsNullOrEmpty(request.Password))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();

                // Update roles if provided
                if (request.Roles != null && request.Roles.Any())
                {
                    // Get current user roles
                    var currentUserRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(user.Id);
                    
                    // Remove all existing roles
                    foreach (var userRole in currentUserRoles)
                    {
                        _unitOfWork.UserRoles.Remove(userRole);
                    }
                    await _unitOfWork.CompleteAsync();

                    // Add new roles
                    foreach (var roleName in request.Roles)
                    {
                        var role = await _unitOfWork.Roles.GetByNameAsync(roleName);
                        if (role != null)
                        {
                            var userRole = new UserRole
                            {
                                UserId = user.Id,
                                RoleId = role.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.UserRoles.AddAsync(userRole);
                        }
                        else
                        {
                            _logger.LogWarning("Role {RoleName} not found when updating user", roleName);
                        }
                    }
                    await _unitOfWork.CompleteAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                var updatedUser = await _unitOfWork.Users.GetWithRolesAsync(user.Id);
                var userDto = new UserDto
                {
                    Id = updatedUser.Id,
                    Username = updatedUser.Username,
                    Email = updatedUser.Email,
                    Roles = updatedUser.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList() ?? []
                };

                return new UpdateUserResponse
                {
                    Success = true,
                    Message = "User updated successfully",
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
            _logger.LogWarning("Business validation failed when updating user: {Message}", ex.Message);
            return new UpdateUserResponse
            {
                Success = false,
                Message = string.Join(", ", ex.ValidationErrors)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user with ID: {UserId}", request.UserId);
            return new UpdateUserResponse
            {
                Success = false,
                Message = "Error occurred while updating user"
            };
        }
    }
}