using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Components.StateMachines.Payment;

public class PaymentStateInstanceMap :
    SagaClassMap<PaymentState>
{
    protected override void Configure(EntityTypeBuilder<PaymentState> entity, ModelBuilder model)
    {
        entity.Property(x => x.Reference);
        entity.Property(x => x.DocumentNumber);
        entity.Property(x => x.Amount).HasPrecision(12,2);
        entity.Property(x => x.DocEntryOv);
        entity.Property(x => x.DocEntryInvoice);
        entity.Property(x => x.DocEntryPayment);
        entity.Property(x => x.RegistrationId);
        entity.Property(x => x.DocEntryOvExpirationDate);
        entity.Property(x => x.Reason);
        entity.Property(x => x.RetryAttempt);
        entity.Property(x => x.ScheduleRetryToken);
    }
}