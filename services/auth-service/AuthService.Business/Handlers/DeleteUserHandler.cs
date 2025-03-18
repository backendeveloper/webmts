using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using AuthService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Handlers;

public class DeleteUserHandler : IRequestHandler<DeleteUserRequest, DeleteUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ILogger<DeleteUserHandler> _logger;

    public DeleteUserHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ILogger<DeleteUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<DeleteUserResponse> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting user with ID: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new DeleteUserResponse
                {
                    Success = false,
                    Message = $"User with ID {request.UserId} not found"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Get and delete all refresh tokens for this user
                await _tokenService.RevokeAllUserTokensAsync(request.UserId);

                // Get and delete all user roles
                var userRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(request.UserId);
                foreach (var userRole in userRoles)
                {
                    _unitOfWork.UserRoles.Remove(userRole);
                }
                await _unitOfWork.CompleteAsync();

                // Delete the user
                _unitOfWork.Users.Remove(user);
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new DeleteUserResponse
                {
                    Success = true,
                    Message = "User deleted successfully"
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting user with ID: {UserId}", request.UserId);
            return new DeleteUserResponse
            {
                Success = false,
                Message = "Error occurred while deleting user"
            };
        }
    }
}