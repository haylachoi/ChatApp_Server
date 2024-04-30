using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp_Server.Helper
{
    public static class HashedPassword
    {
        private static readonly string PATTERN = "qetuoeiwoklsdfanxvcbnmkhu53928761QOISDFNZXMJHGUYWA";
        public static string GenerateRandomKey(int length= 8)
        {
            var sb = new StringBuilder();
            var rd = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(PATTERN[rd.Next(0, PATTERN.Length)]);
            }
            return sb.ToString();
        }

        public static string ToSHA512Hash(this string password, string? saltKey)
        {
          
            using (var alg = SHA512.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(string.Concat(password, saltKey));
                byte[] hash = alg.ComputeHash(bytes);           
                return Convert.ToBase64String(hash);
            }
        }

       
    }
}
