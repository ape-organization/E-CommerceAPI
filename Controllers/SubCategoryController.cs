using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubCategoryController : ControllerBase
    {
        private readonly ISubCategoryService _service;


        public SubCategoryController(
            ISubCategoryService service)
        {
            _service = service;
        }


        // =====================================================
        // GET ALL
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _service
                .GetAllAsync(cancellationToken);

            return Ok(result);
        }


        // =====================================================
        // GET BY ID
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _service
                .GetByIdAsync(
                    id,
                    cancellationToken);


            if (result is null)
            {
                return NotFound(new
                {
                    message = "الفئات الفرعيه غير موجوده"
                });
            }


            return Ok(result);
        }


        // =====================================================
        // GET BY CATEGORY
        // =====================================================

        [HttpGet("category/{categoryId:int}")]
        public async Task<IActionResult> GetByCategory(
            int categoryId,
            CancellationToken cancellationToken)
        {
            var result = await _service
                .GetByCategoryIdAsync(
                    categoryId,
                    cancellationToken);

            return Ok(result);
        }


        // =====================================================
        // CREATE
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CrudSubCategoryDto dto,
            CancellationToken cancellationToken)
        {
            if (dto is null)
            {
                return BadRequest(new
                {
                    message = "بيانات الفئات الفرعيه مطلوبه"
                });
            }


            if (string.IsNullOrWhiteSpace(dto.NameEn) &&
                string.IsNullOrWhiteSpace(dto.NameAr))
            {
                return BadRequest(new
                {
                    message = "الاسم مطلوب"
                });
            }


            try
            {
                var result = await _service
                    .CreateAsync(
                        dto,
                        cancellationToken);


                return CreatedAtAction(
                    nameof(GetById),
                    new
                    {
                        id = result.Id
                    },
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


        // =====================================================
        // UPDATE
        // =====================================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CrudSubCategoryDto dto,
            CancellationToken cancellationToken)
        {
            if (dto is null)
            {
                return BadRequest(new
                {
                    message = "بيانات الفئات الفرعيه غير موجوده"
                });
            }


            if (string.IsNullOrWhiteSpace(dto.NameEn) &&
                string.IsNullOrWhiteSpace(dto.NameAr))
            {
                return BadRequest(new
                {
                    message = "الاسم مطلوب"
                });
            }


            try
            {
                var result = await _service
                    .UpdateAsync(
                        id,
                        dto,
                        cancellationToken);


                if (!result)
                {
                    return NotFound(new
                    {
                        message = "الفئه الفرعيه غير موجوده."
                    });
                }


                return NoContent();
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


        // =====================================================
        // DELETE
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _service
                .DeleteAsync(
                    id,
                    cancellationToken);


            if (!result)
            {
                return NotFound(new
                {
                    message = "الفئه الفرعيه غير موجوده"
                });
            }


            return NoContent();
        }


        // =====================================================
        // ADD PRODUCT
        // =====================================================

        [HttpPost("{subCategoryId:int}/products/{productId:int}")]
        public async Task<IActionResult> AddProduct(
            int subCategoryId,
            int productId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service
                    .AddProductAsync(
                        subCategoryId,
                        productId,
                        cancellationToken);


                if (!result)
                {
                    return NotFound(new
                    {
                        message = "الفئه الفرعيه غير موجوده"
                    });
                }


                return Ok(new
                {
                    message = "تم اضافه المنتج "
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


        // =====================================================
        // REMOVE PRODUCT
        // =====================================================

        [HttpDelete("{subCategoryId:int}/products/{productId:int}")]
        public async Task<IActionResult> RemoveProduct(
            int subCategoryId,
            int productId,
            CancellationToken cancellationToken)
        {
            var result = await _service
                .RemoveProductAsync(
                    subCategoryId,
                    productId,
                    cancellationToken);


            if (!result)
            {
                return NotFound(new
                {
                    message =
                        "حدث خطا"
                });
            }


            return NoContent();
        }


        // =====================================================
        // SET PRODUCTS
        // =====================================================

        [HttpPut("{subCategoryId:int}/products")]
        public async Task<IActionResult> SetProducts(
            int subCategoryId,
            [FromBody] List<int> productIds,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service
                    .SetProductsAsync(
                        subCategoryId,
                        productIds,
                        cancellationToken);


                if (!result)
                {
                    return NotFound(new
                    {
                        message = "الفئه الفرعيه غير موجوده"
                    });
                }


                return NoContent();
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