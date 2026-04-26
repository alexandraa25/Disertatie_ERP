using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities
{
    public class MarketingCampaign
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DiscountScope DiscountScope { get; set; }

        public ICollection<MarketingCampaignCourseSessions> CourseSessions { get; set; }
            = new List<MarketingCampaignCourseSessions>();

        public ICollection<ContractDiscount> ContractDiscounts { get; set; }
            = new List<ContractDiscount>();


    }
}
