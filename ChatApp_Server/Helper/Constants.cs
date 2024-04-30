using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ChatApp_Server.Helper
{
    public static class Constants
    {
       
        public static void Initialize(IConfiguration config)
        {         
            SECRET_KEY_BYTE = Encoding.ASCII.GetBytes(config["AppSettings:SecretKey"] ?? throw new NullReferenceException());
            TOKEN_VALIDATION_PARAM = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Constants.SECRET_KEY_BYTE),

                ClockSkew = TimeSpan.Zero,

                ValidateLifetime = false
            };
        }
        public static byte[] SECRET_KEY_BYTE { get; private set; } = null!;

        public static TokenValidationParameters TOKEN_VALIDATION_PARAM { get; private set; } = null!;

    }
}
