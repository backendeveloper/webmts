using CustomerService.Contract.Requests;
using CustomerService.Contract.Responses;
using CustomerService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerService.Business.Handlers;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerRequest, DeleteCustomerResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCustomerHandler> _logger;

    public DeleteCustomerHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DeleteCustomerResponse> Handle(DeleteCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId);
            if (user == null)
            {
                return new DeleteCustomerResponse
                {
                    Success = false,
                    Message = $"Customer with ID {request.CustomerId} not found"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                _unitOfWork.Customers.Remove(user);
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new DeleteCustomerResponse
                {
                    Success = true,
                    Message = "Customer deleted successfully"
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
            _logger.LogError(ex, "Error occurred while deleting customer with ID: {UserId}", request.CustomerId);
            return new DeleteCustomerResponse
            {
                Success = false,
                Message = "Error occurred while deleting user"
            };
        }
    }
}