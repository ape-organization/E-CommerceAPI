using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BrandsController : ControllerBase
    {
        private readonly IBrandService _brandService;


        public BrandsController(
            IBrandService brandService)
        {
            _brandService = brandService;
        }


        // =====================================================
        // GET ALL
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Brand>>> GetBrands()
        {
            var brands =
                await _brandService.GetBrands();

            return Ok(brands);
        }


        // =====================================================
        // GET BY ID
        // =====================================================

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Brand>> GetBrand(
            int id)
        {
            var brand =
                await _brandService.GetBrand(id);


            if (brand == null)
            {
                return NotFound();
            }


            return Ok(brand);
        }


        // =====================================================
        // CREATE
        // =====================================================

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Brand>> CreateBrand(
            [FromForm] BrandRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(
                    "Brand name is required."
                );
            }


            var brand =
                await _brandService.CreateBrand(
                    request
                );


            return CreatedAtAction(
                nameof(GetBrand),

                new
                {
                    id = brand!.Id
                },

                brand
            );
        }


        // =====================================================
        // UPDATE
        // =====================================================

        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBrand(
            int id,
            [FromForm] BrandRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(
                    "Brand name is required."
                );
            }


            var result =
                await _brandService.UpdateBrand(
                    id,
                    request
                );


            if (!result)
            {
                return NotFound();
            }


            return Ok(
                new
                {
                    message =
                        "Brand updated successfully."
                }
            );
        }


        // =====================================================
        // DELETE
        // =====================================================

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteBrand(
            int id)
        {
            var result =
                await _brandService.DeleteBrand(
                    id
                );


            if (!result)
            {
                return NotFound();
            }


            return NoContent();
        }
    }
}