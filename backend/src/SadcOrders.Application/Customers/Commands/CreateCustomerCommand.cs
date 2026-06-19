using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Exceptions;
using SadcOrders.Application.Mapping;
using SadcOrders.Domain.Entities;
using SadcOrders.Domain.ValueObjects;

namespace SadcOrders.Application.Customers.Commands;

public record CreateCustomerCommand(string Name, string Email, string CountryCode) : IRequest<CustomerDto>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}

public class CreateCustomerCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var countryCode = request.CountryCode.Trim().ToUpperInvariant();
        if (!SadcCountryCurrency.IsSupportedCountry(countryCode))
            throw new ValidationAppException($"Country code '{countryCode}' is not a supported SADC country.");

        var email = request.Email.Trim();
        if (await db.Customers.AnyAsync(c => c.Email == email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            CountryCode = countryCode,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return OrderMappings.ToDto(customer);
    }
}
