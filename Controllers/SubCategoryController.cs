using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubCategoryController : Controller
    {
        private readonly ISubCategoryService _service;

        public SubCategoryController(
            ISubCategoryService service)
        {
            _service = service;
        }

        // GET: api/SubCategories
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        // GET: api/SubCategories/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new
                {
                    message = "Subcategory not found."
                });

            return Ok(result);
        }

        // GET: api/SubCategories/category/2
        [HttpGet("category/{categoryId:int}")]
        public async Task<IActionResult> GetByCategory(
            int categoryId)
        {
            var result =
                await _service.GetByCategoryIdAsync(categoryId);

            return Ok(result);
        }

        // POST: api/SubCategories
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CrudSubCategoryDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/SubCategories/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CrudSubCategoryDto dto)
        {
            try
            {
                var result =
                    await _service.UpdateAsync(id, dto);

                if (!result)
                    return NotFound(new
                    {
                        message = "Subcategory not found."
                    });

                return Ok(new
                {
                    message = "Subcategory updated successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/SubCategories/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new
                {
                    message = "Subcategory not found."
                });

            return Ok(new
            {
                message = "Subcategory deleted successfully."
            });
        }

        // POST: api/SubCategories/5/products/10
        [HttpPost("{subCategoryId:int}/products/{productId:int}")]
        public async Task<IActionResult> AddProduct(
            int subCategoryId,
            int productId)
        {
            try
            {
                var result = await _service.AddProductAsync(
                    subCategoryId,
                    productId);

                if (!result)
                    return NotFound(new
                    {
                        message = "Subcategory not found."
                    });

                return Ok(new
                {
                    message = "Product added to subcategory."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/SubCategories/5/products/10
        [HttpDelete("{subCategoryId:int}/products/{productId:int}")]
        public async Task<IActionResult> RemoveProduct(
            int subCategoryId,
            int productId)
        {
            var result = await _service.RemoveProductAsync(
                subCategoryId,
                productId);

            if (!result)
                return NotFound(new
                {
                    message = "Subcategory or product relationship not found."
                });

            return Ok(new
            {
                message = "Product removed from subcategory."
            });
        }

        // PUT: api/SubCategories/5/products
        [HttpPut("{subCategoryId:int}/products")]
        public async Task<IActionResult> SetProducts(
            int subCategoryId,
            [FromBody] List<int> productIds)
        {
            try
            {
                var result = await _service.SetProductsAsync(
                    subCategoryId,
                    productIds);

                if (!result)
                    return NotFound(new
                    {
                        message = "Subcategory not found."
                    });

                return Ok(new
                {
                    message = "Subcategory products updated successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
