using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoBank.API.Models;

public enum AccountType
{
    Checking = 1,   // Conta Corrente
    Savings = 2,    // Conta Poupança
    Investment = 3  // Conta Investimento
}

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(10)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(4)]
    public string Agency { get; set; } = "0001";

    public AccountType Type { get; set; } = AccountType.Checking;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Foreign Key
    public Guid UserId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Transaction> TransactionsFrom { get; set; } = new List<Transaction>();
    public ICollection<Transaction> TransactionsTo { get; set; } = new List<Transaction>();
}
