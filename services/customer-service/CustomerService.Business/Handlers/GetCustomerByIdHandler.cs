using CustomerService.Contract.Dtos;
using CustomerService.Contract.Requests;
using CustomerService.Contract.Responses;
using CustomerService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerService.Business.Handlers;

public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdRequest, GetCustomerByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCustomerByIdHandler> _logger;

    public GetCustomerByIdHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetCustomerByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetCustomerByIdResponse> Handle(GetCustomerByIdRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId);
            if (customer == null)
            {
                return new GetCustomerByIdResponse
                {
                    Success = false,
                    Message = $"Customer with ID {request.CustomerId} not found"
                };
            }

            var userDto = new CustomerDto
            {
                Id = customer.Id,
                Username = customer.Username,
                Email = customer.Email
            };

            return new GetCustomerByIdResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting customer with ID: {UserId}", request.CustomerId);
            return new GetCustomerByIdResponse
            {
                Success = false,
                Message = "Error occurred while getting user"
            };
        }
    }
}