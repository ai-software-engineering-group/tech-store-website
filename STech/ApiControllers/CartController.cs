﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STech.Data.Models;
using STech.Data.ViewModels;
using STech.Services;
using STech.Utils;
using System.Security.Claims;

namespace STech.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;

        public CartController(ICartService cartService, IProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartCount()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string? userId = User.FindFirstValue("Id");
                if (userId == null)
                {
                    return BadRequest();
                }

                IEnumerable<UserCart> cart = await _cartService.GetUserCart(userId);

                return Ok(new ApiResponse
                {
                    Status = true,
                    Data = cart.Count()
                });
            }
            else
            {
                IEnumerable<CartVM> cart = CartUtils.GetCartFromCookie(Request);
                return Ok(new ApiResponse
                {
                    Status = true,
                    Data = cart.Count()
                });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetUserCart()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string? userId = User.FindFirstValue("Id");
                if(userId == null)
                {
                    return BadRequest();
                }

                IEnumerable<UserCart> cart = await _cartService.GetUserCart(userId) ?? new List<UserCart>();
                return Ok(new ApiResponse
                {
                    Status = true,
                    Data = cart
                });
            }
            else
            {
                IEnumerable<CartVM> cartFromCookie = CartUtils.GetCartFromCookie(Request);
                List<UserCart> cart = new List<UserCart>();
                foreach(CartVM c in cartFromCookie)
                {
                    if(c.ProductId == null)
                    {
                        continue;
                    }

                    Product? product = await _productService.GetProductWithBasicInfo(c.ProductId);
                    if(product == null)
                    {
                        continue;
                    }

                    cart.Add(new UserCart
                    {
                        Product = product,
                        Quantity = c.Quantity
                    });
                }

                return Ok(new ApiResponse
                {
                    Status = true,
                    Data = cart
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string id)
        {
            if (id == null || await _productService.CheckOutOfStock(id))
            {
                return BadRequest();
            }


            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string? userId = User.FindFirstValue("Id");
                if (userId == null)
                {
                    return BadRequest();
                }

                UserCart? cart = await _cartService.GetUserCartItem(userId, id);

                if (cart == null)
                {
                    return Ok(new ApiResponse
                    {
                        Status = await _cartService.AddToCart(new UserCart
                        {
                            ProductId = id,
                            UserId = userId,
                            Quantity = 1
                        })
                    });
                }
                else
                {
                    int warehouseQty = await _productService.GetTotalQty(id);
                    int qty = cart.Quantity + 1;
                    if (qty > warehouseQty)
                    {
                        qty = warehouseQty;
                    }

                    return Ok(new ApiResponse
                    {
                        Status = await _cartService.UpdateQuantity(cart, qty)
                    });
                }
            }
            else
            {
                List<CartVM> cartFromCookie = CartUtils.GetCartFromCookie(Request);

                //----------
                CartVM? cart = cartFromCookie.FirstOrDefault(t => t.ProductId == id);

                if (cart == null)
                {
                    cartFromCookie.Add(new CartVM
                    {
                        ProductId = id,
                        Quantity = 1
                    });
                }
                else
                {
                    int warehouseQty = await _productService.GetTotalQty(id);
                    cart.Quantity += 1;
                    if (cart.Quantity >= warehouseQty)
                    {
                        cart.Quantity = warehouseQty;
                    }
                }

                CartUtils.SaveCartToCookie(Response, cartFromCookie);

                return Ok(new ApiResponse
                {
                    Status = true
                });
            }
        }


        [HttpPut]
        public async Task<IActionResult> UpdateQuantity(string id, string? type, int qty)
        {
            if (id == null)
            {
                return BadRequest();
            }

            Product product = await _productService.GetProductWithBasicInfo(id) ?? new Product();
            int warehouseQty = await _productService.GetTotalQty(id);
            decimal price = product.SaleProducts.FirstOrDefault()?.SalePrice ?? product.Price;

            string message = "";
            int updatedQty = qty;
            decimal totalPrice = 0;
            decimal productTotalPrice = 0;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string? userId = User.FindFirstValue("Id");
                if (userId == null)
                {
                    return BadRequest();
                }

                UserCart? cart = await _cartService.GetUserCartItem(userId, id);
                if (cart == null)
                {
                    return BadRequest();
                }


                if (warehouseQty <= 0)
                {
                    await _cartService.RemoveFromCart(cart);
                    return Ok(new ApiResponse
                    {
                        Status = false,
                        Message = $"Sản phẩm <strong class=\"text-danger\">{product.ProductName}</strong> đã hết hàng"
                    });
                }

                cart.Quantity = CartUtils.UpdateCartItemQuantity(type, cart.Quantity, qty);

                if (cart.Quantity > warehouseQty)
                {
                    cart.Quantity = warehouseQty;
                    message = $"<strong class=\"text-danger\">{product.ProductName}</strong> chỉ còn lại {warehouseQty} sản phẩm";
                }

                await _cartService.UpdateQuantity(cart, cart.Quantity);

                IEnumerable<UserCart> userCart = await _cartService.GetUserCart(userId);
                totalPrice = userCart.Sum(c =>
                {
                    decimal itemPrice = c.Product.SaleProducts.FirstOrDefault()?.SalePrice ?? c.Product.Price;
                    return itemPrice * c.Quantity;
                });
                updatedQty = cart.Quantity;
                productTotalPrice = updatedQty * price;
            }
            else
            {
                List<CartVM> cartFromCookie = CartUtils.GetCartFromCookie(Request);
                CartVM? cart = cartFromCookie.FirstOrDefault(c => c.ProductId == id);

                if (cart == null)
                {
                    return BadRequest();
                }

                if (warehouseQty <= 0)
                {
                    cartFromCookie.Remove(cart);
                    return Ok(new ApiResponse
                    {
                        Status = false,
                        Message = $"Sản phẩm <strong class=\"text-danger\">{product.ProductName}</strong> đã hết hàng"
                    });
                }

                cart.Quantity = CartUtils.UpdateCartItemQuantity(type, cart.Quantity, qty);

                if (cart.Quantity > warehouseQty)
                {
                    cart.Quantity = warehouseQty;
                    message = $"<strong class=\"text-danger\">{product.ProductName}</strong> chỉ còn lại {warehouseQty} sản phẩm";
                }

                foreach (CartVM c in cartFromCookie)
                {
                    if (c.ProductId == null)
                    {
                        cartFromCookie.Remove(c);
                        continue;
                    }

                    Product? p = await _productService.GetProductWithBasicInfo(c.ProductId);
                    totalPrice += c.Quantity * p.Price;
                }

                updatedQty = cart.Quantity;
                productTotalPrice = updatedQty * price;
                CartUtils.SaveCartToCookie(Response, cartFromCookie);
            }

            return Ok(new ApiResponse
            {
                Status = true,
                Message = message,
                Data = new { Quantity = updatedQty, TotalPrice = totalPrice, ProductTotalPrice = productTotalPrice }
            });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string? userId = User.FindFirstValue("Id");
                if (userId == null)
                {
                    return BadRequest();
                }

                UserCart? cart = await _cartService.GetUserCartItem(userId, id);
                if (cart == null)
                {
                    return BadRequest();
                }

                return Ok(new ApiResponse{
                    Status = await _cartService.RemoveFromCart(cart)
                });
            }
            else
            {
                List<CartVM> cartFromCookie = CartUtils.GetCartFromCookie(Request);
                CartVM? cart = cartFromCookie.FirstOrDefault(c => c.ProductId == id);

                if (cart == null)
                {
                    return BadRequest();
                }

                cartFromCookie.Remove(cart);
                CartUtils.SaveCartToCookie(Response, cartFromCookie);

                return Ok(new ApiResponse
                {
                    Status = true
                });
            }
        }
    }
}
