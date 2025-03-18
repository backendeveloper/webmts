using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using AuthService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Handlers;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdRequest, GetUserByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserByIdHandler> _logger;

    public GetUserByIdHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetUserByIdResponse> Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting user with ID: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetWithRolesAsync(request.UserId);
            if (user == null)
            {
                return new GetUserByIdResponse
                {
                    Success = false,
                    Message = $"User with ID {request.UserId} not found"
                };
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList() ?? []
            };

            return new GetUserByIdResponse
            {
                Success = true,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting user with ID: {UserId}", request.UserId);
            return new GetUserByIdResponse
            {
                Success = false,
                Message = "Error occurred while getting user"
            };
        }
    }
}