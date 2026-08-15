using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;
using System.Security.Claims;

namespace MilkshopSystem.Web.Controllers
{
    public class BillingController : BaseController
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPaymentModeRepository _paymentModeRepo;

        public BillingController(IInvoiceRepository invoiceRepo, ICustomerRepository customerRepo, IPaymentModeRepository paymentModeRepo)
        {
            _invoiceRepo = invoiceRepo;
            _customerRepo = customerRepo;
            _paymentModeRepo = paymentModeRepo;
        }

        // point 10: invoice list, search + pagination
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _invoiceRepo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id);
            if (invoice is null) return NotFound();
            ViewBag.PaymentModes = await _paymentModeRepo.GetAllActiveAsync();
            return View(invoice);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new BillingCreateViewModel { PaymentModes = await GetPaymentModeOptions() };
            return View(vm);
        }

        // when an existing customer is picked from the typeahead, prefill their outstanding balance
        [HttpGet]
        public async Task<IActionResult> GetCustomerBalance(int customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            return Json(new { balance = customer?.OutstandingBalance ?? 0, name = customer?.Name, phone = customer?.Phone });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BillingCreateViewModel vm)
        {
            vm.PaymentModes = await GetPaymentModeOptions();

            if (vm.Items is null || vm.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Kammiya oru product venum bill pananum.");
                return View(vm);
            }
            if (!ModelState.IsValid) return View(vm);

            // point 6: "customer name typing search, apadi illena new customer create pananum"
            int customerId;
            if (vm.CustomerId.HasValue)
            {
                customerId = vm.CustomerId.Value;
            }
            else
            {
                customerId = await _customerRepo.CreateAsync(new Customer
                {
                    Name = vm.CustomerName,
                    Phone = vm.CustomerPhone
                });
            }

            var customer = await _customerRepo.GetByIdAsync(customerId);
            var previousBalance = customer?.OutstandingBalance ?? 0;

            var subTotal = vm.Items.Sum(i => i.UnitPrice * i.Qty);
            var grandTotal = subTotal + previousBalance;
            var balanceAmount = grandTotal - vm.PaidAmount;

            var status = balanceAmount <= 0 ? "Paid" : (vm.PaidAmount > 0 ? "Partial" : "Unpaid");

            var invoice = new Invoice
            {
                InvoiceNo = await _invoiceRepo.GetNextInvoiceNoAsync(),
                CustomerId = customerId,
                SubTotal = subTotal,
                PreviousBalance = previousBalance,
                GrandTotal = grandTotal,
                PaidAmount = vm.PaidAmount,
                BalanceAmount = balanceAmount < 0 ? 0 : balanceAmount,
                PaymentStatus = status,
                PaymentModeId = vm.PaymentModeId,
                CreatedByUserId = GetCurrentUserId(),
                Items = vm.Items.Select(i => new InvoiceItem
                {
                    ProductId = i.ProductId,
                    PriceType = i.PriceType,
                    UnitPrice = i.UnitPrice,
                    Qty = i.Qty,
                    Amount = i.UnitPrice * i.Qty
                }).ToList()
            };

            try
            {
                // point 13: stock auto-reduces inside this transaction; throws if any item is short on stock
                var invoiceId = await _invoiceRepo.CreateInvoiceAsync(invoice);
                TempData["Success"] = $"Bill create ஆயிடுச்சு. Invoice No: {invoice.InvoiceNo}";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }
            catch (InvalidOperationException ex)
            {
                // e.g. "Not enough stock" from StockRepository.ReduceStockAsync
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        // customer comes back later and pays off part/all of a Partial/Unpaid invoice (point 11)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int invoiceId, int customerId, decimal amount, int paymentModeId)
        {
            if (amount <= 0)
            {
                TempData["Error"] = "Amount 0 kum jaasthi irukanum.";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }

            await _invoiceRepo.AddPaymentAsync(new InvoicePayment
            {
                InvoiceId = invoiceId,
                CustomerId = customerId,
                Amount = amount,
                PaymentModeId = paymentModeId
            });

            TempData["Success"] = "Payment record ஆயிடுச்சு.";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        private async Task<List<SelectListItem>> GetPaymentModeOptions()
        {
            var modes = await _paymentModeRepo.GetAllActiveAsync();
            return modes.Select(m => new SelectListItem(m.Name, m.Id.ToString())).ToList();
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
