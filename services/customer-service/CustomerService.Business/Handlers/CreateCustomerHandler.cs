using CustomerService.Business.Events;
using CustomerService.Common.Exceptions;
using CustomerService.Contract.Requests;
using CustomerService.Contract.Responses;
using CustomerService.Data;
using CustomerService.Data.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerService.Business.Handlers;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerRequest, CreateCustomerResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCustomerHandler> _logger;
    private readonly IEventBus _eventBus;

    public CreateCustomerHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateCustomerHandler> logger,
        IEventBus eventBus)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<CreateCustomerResponse> Handle(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (await _unitOfWork.Customers.AnyAsync(u => u.Username == request.Username))
                return new CreateCustomerResponse
                {
                    Success = false,
                    Message = "Username is already taken"
                };

            if (await _unitOfWork.Customers.AnyAsync(u => u.Email == request.Email))
                return new CreateCustomerResponse
                {
                    Success = false,
                    Message = "Email is already registered"
                };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newUser = new Customer
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Customers.AddAsync(newUser);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                var customerCreatedEvent = new CustomerCreatedEvent
                {
                    CustomerId = newUser.Id.ToString(),
                    CustomerName = newUser.Username,
                    CustomerEmail = newUser.Email,
                    CustomerPhone = ""
                };

                _eventBus.Publish(customerCreatedEvent);
                _logger.LogInformation("Published CustomerCreatedEvent for customer {CustomerId}", newUser.Id);
                
                return new CreateCustomerResponse
                {
                    Success = true,
                    Message = "Customer created successfully"
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
            _logger.LogWarning("Business validation failed when creating customer: {Message}", ex.Message);
            return new CreateCustomerResponse
            {
                Success = false,
                Message = string.Join(", ", ex.ValidationErrors)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user with username: {Username}", request.Username);
            return new CreateCustomerResponse
            {
                Success = false,
                Message = "Error occurred while creating user"
            };
        }
    }
}