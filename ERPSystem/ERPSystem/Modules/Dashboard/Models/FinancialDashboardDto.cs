namespace ERPSystem.Modules.Dashboard.Models
{
    public class FinancialDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
        public decimal OverdueAmount { get; set; }
        public decimal PendingInstallmentsAmount { get; set; }

        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int OverdueInstallments { get; set; }

        public decimal TotalDiscounts { get; set; }
        public decimal TotalPriceAdjustments { get; set; }

        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
        public List<PaymentMethodDto> PaymentsByMethod { get; set; } = new();
        public List<InstallmentStatusDto> InstallmentsByStatus { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class PaymentMethodDto
    {
        public string Method { get; set; } = "";
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class InstallmentStatusDto
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
