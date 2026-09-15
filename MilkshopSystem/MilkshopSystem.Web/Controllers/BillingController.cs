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
                ModelState.AddModelError(string.Empty, "");
                return View(vm);
            }
            if (!ModelState.IsValid) return View(vm);

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

            var status = balanceAmount < 0 ? "Advance" : (balanceAmount == 0 ? "Paid" : (vm.PaidAmount > 0 ? "Partial" : "Unpaid"));

            var invoice = new Invoice
            {
                InvoiceNo = await _invoiceRepo.GetNextInvoiceNoAsync(),
                CustomerId = customerId,
                SubTotal = subTotal,
                PreviousBalance = previousBalance,
                GrandTotal = grandTotal,
                PaidAmount = vm.PaidAmount,
                BalanceAmount = balanceAmount,
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
                var invoiceId = await _invoiceRepo.CreateInvoiceAsync(invoice);
                TempData["Success"] = $"Bill create sucessfully. Invoice No: {invoice.InvoiceNo}";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int invoiceId, int customerId, decimal amount, int paymentModeId)
        {
            if (amount <= 0)
            {
                TempData["Error"] = "Amount 0 is more then";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }

            await _invoiceRepo.AddPaymentAsync(new InvoicePayment
            {
                InvoiceId = invoiceId,
                CustomerId = customerId,
                Amount = amount,
                PaymentModeId = paymentModeId
            });

            TempData["Success"] = "Payment record successfully";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id);
            if (invoice is null) return NotFound();
            if (invoice.IsCancelled)
            {
                TempData["Error"] = "Cancelled invoices cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var vm = new BillingCreateViewModel
            {
                InvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                CustomerName = invoice.CustomerName ?? string.Empty,
                CustomerPhone = invoice.CustomerPhone ?? string.Empty,
                PreviousBalance = invoice.PreviousBalance,
                PaymentModeId = invoice.PaymentModeId ?? 0,
                PaidAmount = invoice.PaidAmount,
                PaymentModes = await GetPaymentModeOptions(),
                Items = invoice.Items.Select(i => new BillingItemInput
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Size = i.Size,
                    UnitSymbol = i.UnitSymbol,
                    PriceType = i.PriceType,
                    UnitPrice = i.UnitPrice,
                    Qty = i.Qty
                }).ToList()
            };

            ViewBag.InvoiceNo = invoice.InvoiceNo;
            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BillingCreateViewModel vm)
        {
            vm.InvoiceId = id;
            vm.PaymentModes = await GetPaymentModeOptions();

            if (vm.Items is null || vm.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "To bill a smaller quantity of a product, reduce the quantity before billing.");
                return View("Create", vm);
            }
            if (!ModelState.IsValid) return View("Create", vm);

            try
            {
                var items = vm.Items.Select(i => new InvoiceItem
                {
                    ProductId = i.ProductId,
                    PriceType = i.PriceType,
                    UnitPrice = i.UnitPrice,
                    Qty = i.Qty,
                    Amount = i.UnitPrice * i.Qty
                }).ToList();

                await _invoiceRepo.UpdateInvoiceAsync(id, items, vm.PaidAmount, vm.PaymentModeId);
                TempData["Success"] = "Invoice updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Create", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelInvoice(int id)
        {
            try
            {
                await _invoiceRepo.CancelInvoiceAsync(id);
                TempData["Success"] = "Invoice cancel successfully";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
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
