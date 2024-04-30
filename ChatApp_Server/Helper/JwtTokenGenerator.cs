using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp_Server.Helper
{
    public class JwtTokenGenerator
    {
       
        public static (SecurityToken token, string tokenString) GenerateAccessToken(string name, string username, string email, string role, string id, DateTime exprire, byte[] secretByte)
        {
            var jwthandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, name),
                    new Claim(JwtRegisteredClaimNames.Email, email ),
                    new Claim(JwtRegisteredClaimNames.Sub, email ),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() ),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("username", username),
                    new Claim(ClaimTypes.NameIdentifier, id)
                }),
                Expires = exprire,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretByte), SecurityAlgorithms.HmacSha512Signature),
            };

            var token = jwthandler.CreateToken(tokenDescriptor);
            return (token, jwthandler.WriteToken(token));
        }

        public static (SecurityToken token, string tokenString) GenerateAccessToken(ClaimsIdentity identity, DateTime expire, byte[] secretByte)
        {
            var jwthandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = expire,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretByte), SecurityAlgorithms.HmacSha512Signature),
            };

            var token = jwthandler.CreateToken(tokenDescriptor);
            return (token, jwthandler.WriteToken(token));
        }

        public static string GenerateRefreshoken()
        {
            var random = new byte[32];
            using (var rng = RandomNumberGenerator.Create())    
            rng.GetBytes(random);

            return Convert.ToBase64String(random);     
        }

      
    }
}
