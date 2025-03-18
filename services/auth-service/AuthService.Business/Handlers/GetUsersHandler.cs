using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using AuthService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Handlers;

public class GetUsersHandler : IRequestHandler<GetUsersRequest, GetUsersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUsersHandler> _logger;

    public GetUsersHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUsersHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetUsersResponse> Handle(GetUsersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting users list. Page: {Page}, PageSize: {PageSize}",
                request.Page, request.PageSize);

            var users = await _unitOfWork.Users.GetAllPaginatedAsync(
                request.Page,
                request.PageSize,
                includeRoles: true);

            var totalCount = await _unitOfWork.Users.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList() ?? []
            }).ToList();

            return new GetUsersResponse
            {
                Success = true,
                Users = userDtos,
                TotalCount = totalCount,
                PageSize = request.PageSize,
                CurrentPage = request.Page,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting users");
            return new GetUsersResponse
            {
                Success = false,
                Message = "Error occurred while getting users"
            };
        }
    }
}