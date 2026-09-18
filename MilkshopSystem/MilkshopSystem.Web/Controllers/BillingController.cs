using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;
using System.Security.Claims;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10, int? year = null, int? month = null)
        {
            var result = await _invoiceRepo.GetPagedAsync(search, page, pageSize, year, month);

            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.AvailableYears = await _invoiceRepo.GetDistinctInvoiceYearsAsync();
            ViewBag.Months = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m), m.ToString()))
                .ToList();

            return View(result);
        }

        public async Task<IActionResult> ExportExcel(string? search, int? year, int? month)
        {
            var invoices = await _invoiceRepo.GetAllFilteredAsync(search, year, month);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Invoices");

            var headers = new[] { "Invoice No", "Date", "Customer", "Phone", "Sub Total", "Previous Balance",
                                   "Grand Total", "Paid Amount", "Payment Mode", "Balance", "Status" };
            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            var row = 2;
            foreach (var inv in invoices)
            {
                sheet.Cell(row, 1).Value = inv.InvoiceNo;
                sheet.Cell(row, 2).Value = inv.InvoiceDate;
                sheet.Cell(row, 2).Style.DateFormat.Format = "dd-MMM-yyyy hh:mm AM/PM";
                sheet.Cell(row, 3).Value = inv.CustomerName;
                sheet.Cell(row, 4).Value = inv.CustomerPhone;
                sheet.Cell(row, 5).Value = inv.SubTotal;
                sheet.Cell(row, 6).Value = inv.PreviousBalance;
                sheet.Cell(row, 7).Value = inv.GrandTotal;
                sheet.Cell(row, 8).Value = inv.PaidAmount;
                sheet.Cell(row, 9).Value = inv.PaymentModeName ?? "-";
                sheet.Cell(row, 10).Value = inv.BalanceAmount;
                sheet.Cell(row, 11).Value = inv.PaymentStatus;
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Invoices_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportPdf(string? search, int? year, int? month)
        {
            var invoices = await _invoiceRepo.GetAllFilteredAsync(search, year, month);
            var filterLabel = BuildFilterLabel(year, month);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text($"Invoice Report {filterLabel}").FontSize(16).Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.4f); // Invoice No
                            columns.RelativeColumn(1.4f); // Date
                            columns.RelativeColumn(1.8f); // Customer
                            columns.RelativeColumn(1.2f); // Phone
                            columns.RelativeColumn(1f);   // Grand Total
                            columns.RelativeColumn(1f);   // Paid
                            columns.RelativeColumn(1.2f); // Payment Mode
                            columns.RelativeColumn(1f);   // Balance
                            columns.RelativeColumn(1f);   // Status
                        });

                        table.Header(header =>
                        {
                            foreach (var text in new[] { "Invoice No", "Date", "Customer", "Phone", "Grand Total", "Paid", "Payment Mode", "Balance", "Status" })
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).Bold();
                            }
                        });

                        foreach (var inv in invoices)
                        {
                            table.Cell().Padding(4).Text(inv.InvoiceNo);
                            table.Cell().Padding(4).Text(inv.InvoiceDate.ToString("dd-MMM-yyyy"));
                            table.Cell().Padding(4).Text(inv.CustomerName);
                            table.Cell().Padding(4).Text(inv.CustomerPhone);
                            table.Cell().Padding(4).Text($"Rs.{inv.GrandTotal:N2}");
                            table.Cell().Padding(4).Text($"Rs.{inv.PaidAmount:N2}");
                            table.Cell().Padding(4).Text(inv.PaymentModeName ?? "-");
                            table.Cell().Padding(4).Text($"Rs.{inv.BalanceAmount:N2}");
                            table.Cell().Padding(4).Text(inv.PaymentStatus);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated on ").FontSize(8);
                        x.Span(DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt")).FontSize(8);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"Invoices_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> PrintView(string? search, int? year, int? month)
        {
            var invoices = await _invoiceRepo.GetAllFilteredAsync(search, year, month);
            ViewBag.FilterLabel = BuildFilterLabel(year, month);
            return View(invoices);
        }

        private static string BuildFilterLabel(int? year, int? month)
        {
            if (year.HasValue && month.HasValue)
                return $"— {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month.Value)} {year.Value}";
            if (year.HasValue)
                return $"— {year.Value}";
            return string.Empty;
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

            // BUG FIX: negative balance now means the customer paid MORE than the bill —
            // that extra amount is an advance/credit that should reduce their next bill,
            // not be silently discarded. Previously this was clamped to 0 and lost.
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
                TempData["Error"] = "Cannot edit a cancelled invoice.";
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
                ModelState.AddModelError(string.Empty, "Please add at least one product to the bill.");
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
                TempData["Success"] = "Invoice updated successfully";
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
                TempData["Success"] = "Invoice cancelled successfully";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetLastOrder(int customerId)
        {
            var lastInvoice = await _invoiceRepo.GetLastInvoiceForCustomerAsync(customerId);
            if (lastInvoice is null || lastInvoice.Items.Count == 0)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                invoiceDate = lastInvoice.InvoiceDate.ToString("dd-MMM-yyyy"),
                items = lastInvoice.Items.Select(i => new
                {
                    productId = i.ProductId,
                    name = i.ProductName,
                    size = i.Size,
                    unitSymbol = i.UnitSymbol,
                    priceType = i.PriceType,
                    unitPrice = i.UnitPrice,
                    qty = i.Qty
                })
            });
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