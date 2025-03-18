using CustomerService.Contract.Dtos;
using CustomerService.Contract.Requests;
using CustomerService.Contract.Responses;
using CustomerService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerService.Business.Handlers;

public class GetCustomersHandler : IRequestHandler<GetCustomersRequest, GetCustomersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCustomersHandler> _logger;

    public GetCustomersHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetCustomersHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetCustomersResponse> Handle(GetCustomersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _unitOfWork.Customers.GetAllPaginatedAsync(
                request.Page,
                request.PageSize,
                includeRoles: true);

            var totalCount = await _unitOfWork.Customers.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var userDtos = users.Select(user => new CustomerDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            }).ToList();

            return new GetCustomersResponse
            {
                Success = true,
                TotalCount = totalCount,
                PageSize = request.PageSize,
                CurrentPage = request.Page,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting customers");
            return new GetCustomersResponse
            {
                Success = false,
                Message = "Error occurred while getting customers"
            };
        }
    }
}