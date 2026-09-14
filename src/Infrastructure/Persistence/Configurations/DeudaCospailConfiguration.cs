using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="DeudaCospail"/> a la tabla <c>deudas_cospail</c>.
/// </summary>
public sealed class DeudaCospailConfiguration : IEntityTypeConfiguration<DeudaCospail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeudaCospail> builder)
    {
        builder.ToTable("deudas_cospail");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FixedCode).HasColumnName("fixed_code").IsRequired();
        builder.Property(x => x.DocumentId).HasColumnName("document_id").HasMaxLength(32).IsRequired();
        builder.Property(x => x.MemberName).HasColumnName("member_name").HasMaxLength(200);
        builder.Property(x => x.CreditNumber).HasColumnName("credit_number").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").IsRequired();
        builder.Property(x => x.NoticeNumber).HasColumnName("notice_number").IsRequired();
        builder.Property(x => x.Year).HasColumnName("year").IsRequired();
        builder.Property(x => x.Month).HasColumnName("month").IsRequired();
        builder.Property(x => x.Period).HasColumnName("period").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.PagoCospailId).HasColumnName("pago_cospail_id").IsRequired();

        builder
            .HasOne(x => x.PagoCospail)
            .WithMany(x => x.Deudas)
            .HasForeignKey(x => x.PagoCospailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PagoCospailId);
        builder.HasIndex(x => new { x.FixedCode, x.CreditNumber, x.Type, x.Status });
    }
}
