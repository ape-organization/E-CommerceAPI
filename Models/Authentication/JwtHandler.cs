using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PharmacyAPI.Models.Authentication
{
    public class JwtHandler
    {

        
            private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> userManager;
        public JwtHandler(IConfiguration configuration,
            UserManager<ApplicationUser> _userManager
            )
            {
            userManager = _userManager;
            _configuration = configuration;
            }

            public SigningCredentials GetSigningCredentials()
            {
                var key = Encoding.UTF8.GetBytes(_configuration["JWTSettings:securityKey"]);
                var secret = new SymmetricSecurityKey(key);
                return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
            }

        public async Task<List<Claim>> GetClaims(ApplicationUser user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var roles = await userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }

        public JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
            {
                var tokenOptions = new JwtSecurityToken(
                    issuer: _configuration["JWTSettings:validIssuer"],
                    audience: _configuration["JWTSettings:validAudience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWTSettings:expires"])),
                    signingCredentials: signingCredentials
                );

                return tokenOptions;
            }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(
   string token)
        {
            var tokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,

                    ValidateIssuerSigningKey = true,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                _configuration[
                                    "JWTSettings:securityKey"
                                ]!
                            )
                        ),

                    ValidateLifetime = false,

                    ValidIssuer =
                        _configuration[
                            "JWTSettings:validIssuer"
                        ],

                    ValidAudience =
                        _configuration[
                            "JWTSettings:validAudience"
                        ]
                };


            var tokenHandler =
                new JwtSecurityTokenHandler();


            var principal =
                tokenHandler.ValidateToken(
                    token,
                    tokenValidationParameters,
                    out SecurityToken securityToken
                );


            if (
                securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                throw new SecurityTokenException(
                    "Invalid token"
                );
            }


            return principal;
        }
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            // Get signing credentials
            var signingCredentials = GetSigningCredentials();

            // Create token options
            var tokenOptions = new JwtSecurityToken(
                issuer: _configuration["JWTSettings:validIssuer"],
                audience: _configuration["JWTSettings:validAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWTSettings:expires"])),
                signingCredentials: signingCredentials
            );

            // Convert to string token
            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }

    }

    }

