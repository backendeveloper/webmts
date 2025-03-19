using CustomerService.Business.Events;
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
    private readonly IEventBus _eventBus;

    public DeleteCustomerHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteCustomerHandler> logger,
        IEventBus eventBus)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<DeleteCustomerResponse> Handle(DeleteCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId);
            if (customer == null)
                return new DeleteCustomerResponse
                {
                    Success = false,
                    Message = $"Customer with ID {request.CustomerId} not found"
                };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                _unitOfWork.Customers.Remove(customer);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
                
                var customerDeletedEvent = new CustomerDeletedEvent
                {
                    CustomerId = customer.Id.ToString()
                };

                _eventBus.Publish(customerDeletedEvent);
                _logger.LogInformation("Published CustomerDeletedEvent for customer {CustomerId}", customer.Id);

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