using ERPSystem.Utils.Enums;
namespace ERPSystem.Data.Entities
{
    public class ContractDiscount
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; } = null!;

        public DiscountType Type { get; set; }

        public decimal Value { get; set; }

        public string Reason { get; set; } = default!;
    }
}
