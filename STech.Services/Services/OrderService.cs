﻿using Microsoft.EntityFrameworkCore;
using STech.Data.Models;

namespace STech.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly StechDbContext _context;

        public OrderService(StechDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateInvoice(Invoice invoice)
        {
            IEnumerable<InvoiceDetail> invoiceDetails = invoice.InvoiceDetails;
            IEnumerable<InvoiceStatus> invoiceStatuses = invoice.InvoiceStatuses;
            PackingSlip? packingSlip = invoice.PackingSlip;

            invoice.InvoiceDetails = new List<InvoiceDetail>();
            invoice.InvoiceStatuses = new List<InvoiceStatus>();
            invoice.PackingSlip = null;
            invoice.WarehouseExports = new List<WarehouseExport>();

            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();

            await _context.InvoiceStatuses.AddRangeAsync(invoiceStatuses);
            await _context.InvoiceDetails.AddRangeAsync(invoiceDetails);
            if (packingSlip != null)
            {
                await _context.PackingSlips.AddAsync(packingSlip);
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Invoice?> GetInvoice(string invoiceId)
        {
            return await _context.Invoices.Where(i => i.InvoiceId == invoiceId).FirstOrDefaultAsync();
        }

        public async Task<Invoice?> GetInvoice(string invoiceId, string phone)
        {
            Invoice? invoice = await _context.Invoices
                .Where(i => i.InvoiceId == invoiceId && i.RecipientPhone == phone)
                .Include(i => i.PackingSlip)
                .Include(i => i.InvoiceStatuses)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Product)
                .ThenInclude(d => d.ProductImages)
                .FirstOrDefaultAsync();

            if(invoice != null)
            {
                invoice.InvoiceDetails = invoice.InvoiceDetails.Select(d => new InvoiceDetail
                {
                    InvoiceId = d.InvoiceId,
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    Cost = d.Cost,
                    Product = new Product
                    {
                        ProductId = d.Product.ProductId,
                        ProductName = d.Product.ProductName,
                        Price = d.Product.Price,
                        ProductImages = d.Product.ProductImages.OrderBy(t => t.Id).Take(1).ToList(),
                    }
                }).ToList();
            }

            if (invoice?.PackingSlip != null)
            {
                invoice.PackingSlip.PackingSlipStatuses = await _context.PackingSlipStatuses
                    .Where(p => p.Psid == invoice.PackingSlip.Psid)
                    .ToListAsync();
            }

            return invoice;
        }

        public async Task<bool> UpdateInvoice(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
