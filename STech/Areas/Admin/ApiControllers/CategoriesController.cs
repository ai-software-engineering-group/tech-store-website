﻿using Microsoft.AspNetCore.Mvc;
using STech.Contants;
using STech.Data.Models;
using STech.Data.ViewModels;
using STech.Filters;
using STech.Services;
using STech.Utils;

namespace STech.Areas.Admin.ApiControllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly string IMAGE_PATH = "categories/";

        private readonly ICategoryService _categoryService;
        private readonly IAzureStorageService _azureService;
        
        private readonly int _itemsPerPage = 30;

        public CategoriesController(ICategoryService categoryService, IAzureStorageService azureService)
        {
            _categoryService = categoryService;
            _azureService = azureService;
        }

        [HttpGet]
        [AdminAuthorize(Code = Functions.ViewCategories)]
        public async Task<IActionResult> GetCategories(string? sort_by, int page = 1)
        {
            if (page <= 0)
            {
                page = 1;
            }

            PagedList<Category> data = await _categoryService.GetAllWithProducts(sort_by, page, _itemsPerPage);

            return Ok(new ApiResponse
            {
                Status = true,
                Data = new
                {
                    categories = data.Items,
                    currentPage = data.CurrentPage,
                    totalPages = data.TotalPages
                }
            });
        }

        [HttpGet("{id}")]
        [AdminAuthorize(Code = Functions.ViewCategories)]
        public async Task<IActionResult> GetCategory(string id)
        {
            Category? category = await _categoryService.GetOne(id);

            return Ok(new ApiResponse
            {
                Status = category != null,
                StatusCode = category == null ? 404 : 200,
                Message = category == null ? "Không tìm thấy danh mục này" : "",
                Data = category,
            });
        }

        [HttpPost]
        [AdminAuthorize(Code = Functions.CreateCategory)]
        public async Task<IActionResult> CreateCategory([FromForm]CategoryVM category, IFormFile? image)
        {
            if(ModelState.IsValid)
            {
                if(image != null)
                {
                    string imageExtension = Path.GetExtension(image.FileName).ToLower();
                    if(!ImageUtils.CheckImageExtension(imageExtension))
                    {
                        return Ok(new ApiResponse { 
                            Status = false,
                            Message = "Hình ảnh danh mục không hợp lệ"
                        });
                    }

                    byte[] imageBytes = ConvertFile.ConvertIFormFileToByteArray(image);
                    string path = $"{IMAGE_PATH}{category.CategoryId}{imageExtension}";

                    category.ImageSrc = await _azureService.UploadImage(path, imageBytes);
                }

                bool result = await _categoryService.Create(new Category { 
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    ImageSrc = category.ImageSrc,
                });
              
                return Ok(new ApiResponse
                {
                    Status = result,
                    Message = result ? "Tạo danh mục thành công" : "Tạo danh mục thất bại"
                });
            }

            return BadRequest();
        }

        [HttpPut]
        [AdminAuthorize(Code = Functions.EditCategory)]
        public async Task<IActionResult> UpdateCategory([FromForm] CategoryVM category, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                Category updateCategory = new Category
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                };

                if (image != null)
                {
                    string imageExtension = Path.GetExtension(image.FileName).ToLower();
                    if (!ImageUtils.CheckImageExtension(imageExtension))
                    {
                        return Ok(new ApiResponse
                        {
                            Status = false,
                            Message = "Hình ảnh danh mục không hợp lệ"
                        });
                    }

                    byte[] imageBytes = ConvertFile.ConvertIFormFileToByteArray(image);
                    string path = $"{IMAGE_PATH}{category.CategoryId}{imageExtension}";

                    updateCategory.ImageSrc = await _azureService.UploadImage(path, imageBytes);
                }

                bool result = await _categoryService.Update(updateCategory);

                return Ok(new ApiResponse
                {
                    Status = result,
                    Message = result ? "Cập nhật danh mục thành công" : "Cập nhật danh mục thất bại"
                });
            }

            return BadRequest();
        }

        [HttpDelete("{id}")]
        [AdminAuthorize(Code = Functions.DeleteCategory)]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            Category? category = await _categoryService.GetOne(id);
            if (category == null)
            {
                return Ok(new ApiResponse
                {
                    Status = false,
                    StatusCode = 404,
                    Message = "Danh mục không tồn tại"
                });
            }

            bool result = await _categoryService.Delete(id);
            if(result && category.ImageSrc != null)
            {
                await _azureService.DeleteImage(category.ImageSrc);
            }

            return Ok(new ApiResponse
            {
                Status = result,
                Message = result ? "Xóa danh mục thành công" : "Xóa danh mục thất bại"
            });
        }
    }
}
