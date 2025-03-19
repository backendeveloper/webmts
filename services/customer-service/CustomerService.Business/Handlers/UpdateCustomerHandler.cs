using CustomerService.Business.Events;
using CustomerService.Common.Exceptions;
using CustomerService.Contract.Dtos;
using CustomerService.Contract.Requests;
using CustomerService.Contract.Responses;
using CustomerService.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerService.Business.Handlers;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerRequest, UpdateCustomerResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCustomerHandler> _logger;
    private readonly IEventBus _eventBus;

    public UpdateCustomerHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateCustomerHandler> logger,
        IEventBus eventBus)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<UpdateCustomerResponse> Handle(UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId);
            if (customer == null)
                return new UpdateCustomerResponse
                {
                    Success = false,
                    Message = $"Customer with ID {request.CustomerId} not found"
                };

            if (!string.IsNullOrEmpty(request.Username) && customer.Username != request.Username &&
                await _unitOfWork.Customers.AnyAsync(u => u.Username == request.Username))
                return new UpdateCustomerResponse
                {
                    Success = false,
                    Message = "Username is already taken"
                };

            if (!string.IsNullOrEmpty(request.Email) && customer.Email != request.Email &&
                await _unitOfWork.Customers.AnyAsync(u => u.Email == request.Email))
                return new UpdateCustomerResponse
                {
                    Success = false,
                    Message = "Email is already registered"
                };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var originalUsername = customer.Username;
                var originalEmail = customer.Email;

                if (!string.IsNullOrEmpty(request.Username))
                    customer.Username = request.Username;

                if (!string.IsNullOrEmpty(request.Email))
                    customer.Email = request.Email;

                customer.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Customers.Update(customer);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                if (originalUsername != customer.Username || originalEmail != customer.Email)
                {
                    var customerUpdatedEvent = new CustomerUpdatedEvent
                    {
                        CustomerId = customer.Id.ToString(),
                        CustomerName = customer.Username,
                        CustomerEmail = customer.Email
                    };

                    _eventBus.Publish(customerUpdatedEvent);
                    _logger.LogInformation("Published CustomerUpdatedEvent for customer {CustomerId}", customer.Id);
                }

                var updatedCustomer = await _unitOfWork.Customers.GetByIdAsync(customer.Id);
                var customerDto = new CustomerDto
                {
                    Id = updatedCustomer.Id,
                    Username = updatedCustomer.Username,
                    Email = updatedCustomer.Email
                };

                return new UpdateCustomerResponse
                {
                    Success = true,
                    Message = "Custumer updated successfully",
                    Customer = customerDto
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
            return new UpdateCustomerResponse
            {
                Success = false,
                Message = string.Join(", ", ex.ValidationErrors)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user with ID: {CustomerId}", request.CustomerId);
            return new UpdateCustomerResponse
            {
                Success = false,
                Message = "Error occurred while updating customer"
            };
        }
    }
}