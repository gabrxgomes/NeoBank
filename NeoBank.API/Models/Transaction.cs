using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoBank.API.Models;

public enum TransactionType
{
    Deposit = 1,        // Depósito
    Withdrawal = 2,     // Saque
    Transfer = 3,       // Transferência
    Payment = 4,        // Pagamento
    PixIn = 5,          // PIX Recebido
    PixOut = 6          // PIX Enviado
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public class Transaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public TransactionType Type { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    // Conta de origem (quem envia)
    public Guid? FromAccountId { get; set; }
    public Account? FromAccount { get; set; }

    // Conta de destino (quem recebe)
    public Guid? ToAccountId { get; set; }
    public Account? ToAccount { get; set; }

    // Referência externa (para PIX, boletos, etc.)
    [MaxLength(100)]
    public string? ExternalReference { get; set; }
}
