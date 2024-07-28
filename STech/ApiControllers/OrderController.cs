﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STech.Data.Models;
using STech.Data.ViewModels;
using STech.Services;
using System.Security.Claims;

namespace STech.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public OrderController(IProductService productService, ICartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
        }

        [Authorize, HttpGet("check")]
        public async Task<IActionResult> CheckQuantity([FromQuery] string? pId)
        {
            string? message = null;

            if(pId != null)
            {
                Product p = await _productService.GetProductWithBasicInfo(pId) ?? new Product();
                int qty = p.WarehouseProducts.Sum(wp => wp.Quantity);

                if(qty <= 0)
                {
                    message += $"<li>Sản phẩm <span class=\"fw-bold text-primary\">{p.ProductName}</span> đã hết hàng</li>";
                }
            }
            else 
            {
                string? userId = User.FindFirstValue("Id");
                if (userId == null)
                {
                    return BadRequest();
                }

                IEnumerable<UserCart> cart = await _cartService.GetUserCart(userId);

                foreach (UserCart c in cart)
                {
                    Product p = await _productService.GetProductWithBasicInfo(c.ProductId) ?? new Product();

                    int qty = p.WarehouseProducts.Sum(wp => wp.Quantity);

                    if (qty < c.Quantity)
                    {
                        message += $"<li>Số lượng sản phẩm <span class=\"fw-bold text-primary\">{p.ProductName}</span> chỉ còn lại <span class=\"fw-bold text-primary\">{qty}</span></li>";
                    }
                    else if (qty == 0)
                    {
                        message += $"<li>Sản phẩm <span class=\"fw-bold text-primary\">{p.ProductName}</span> đã hết hàng</li>";
                    }
                }

            }

            return Ok(new ApiResponse
            {
                Status = message == null ? true : false,
                Data = message
            });
        }
    }
}