using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Exceptions;
using SadcOrders.Application.Mapping;
using SadcOrders.Domain.ValueObjects;

namespace SadcOrders.Application.Customers.Commands;

public record UpdateCustomerCommand(Guid Id, string Name, string Email, string CountryCode) : IRequest<CustomerDto>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}

public class UpdateCustomerCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Customer not found.");

        var countryCode = request.CountryCode.Trim().ToUpperInvariant();
        if (!SadcCountryCurrency.IsSupportedCountry(countryCode))
            throw new ValidationAppException($"Country code '{countryCode}' is not a supported SADC country.");

        var email = request.Email.Trim();
        if (await db.Customers.AnyAsync(c => c.Email == email && c.Id != request.Id, cancellationToken))
            throw new ConflictException("Email is already registered.");

        customer.Name = request.Name.Trim();
        customer.Email = email;
        customer.CountryCode = countryCode;

        await db.SaveChangesAsync(cancellationToken);
        return OrderMappings.ToDto(customer);
    }
}
