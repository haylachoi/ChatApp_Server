using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Mapster;
using FluentResults;

using ChatApp_Server.Helper;
using ChatApp_Server.Services;
using ChatApp_Server.Repositories;
using ChatApp_Server.Models;
using ChatApp_Server.DTOs;
using System.Data;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using ChatApp_Server.Settings;
using System.Text;

namespace ChatApp_Server.Services
{
    public interface IAuthService
    {
        Task<Result<UserDto>> ValidateCredential(string email, string password);
        Task<Result<int>> Register(string email, string password, string fullName, string? imgUrl);
        Task<Result<JwtTokenDto>> CreateToken(UserDto user);
        Task<Result<JwtTokenDto>> RenewToken(JwtTokenDto jwtToken);
        Task<UserDto?> GetProfile(int id);

        //public Task<Result> ChangePassword(User user, string newPassword);
        //public Task<Result<string>> SendResetPasswordEmail(string email);
        //public Task<Result> ResetPassword(Guid id);


    }
    public class AuthService(
        IRefreshTokenService refreshTokenService,
        //IUserService userService,
        IUserRepository userRepository,
        IOptions<AppSettings> settingsOptions
        ): IAuthService
    {
        private readonly JwtSecurityTokenHandler _jwtHandler = new JwtSecurityTokenHandler();
        public async Task<Result<int>> Register(string email, string password, string fullName, string? imgUrl)
        {
            try
            {
                var salt = HashedPassword.GenerateRandomKey();
                var user = new User
                {
                    Email = email,
                    Fullname = fullName,
                    Avatar = imgUrl,
                    Salt = salt,
                    Password = password.ToSHA512Hash(salt)
                };
                userRepository.Insert(user);
                await userRepository.SaveAsync();
                return user.Id;
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
        public async Task<Result<UserDto>> ValidateCredential(string email, string password)
        {
            try
            {
                var users = await userRepository.GetAllAsync([u => u.Email == email]);
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return Result.Fail("Tài khoản hoặc password không chính xác");
                }
                if (user.Password == password.ToSHA512Hash(user.Salt))
                {
                    return user.Adapt<UserDto>();
                }
                return Result.Fail("Tài khoản hoặc password không chính xác");
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
        public async Task<Result<JwtTokenDto>> CreateToken(UserDto user)
        {
            if (user.Id == null)
            {
                return Result.Fail("User has not id");
            }
            
            var identity = new ClaimsIdentity(new[] {                
                new Claim(JwtRegisteredClaimNames.Email, user.Email ),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() ),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()!)
            });

            var setting = settingsOptions.Value;
            var secret = setting.SecretKey;
            var (token, tokenString) = JwtTokenGenerator.GenerateAccessToken(identity, DateTime.UtcNow.AddHours(1), Encoding.ASCII.GetBytes(secret)); 
        
            var refreshTokenString = JwtTokenGenerator.GenerateRefreshoken();
            await InsertRefreshTokenAsync(user.Id.Value, token, refreshTokenString, TimeSpan.FromDays(1));
            return new JwtTokenDto { AccessToken = tokenString, RefreshToken = refreshTokenString };
        }
        public async Task<Result<JwtTokenDto>> RenewToken(JwtTokenDto jwtToken)
        {
            var storedTokenResult = await ValidateToken(jwtToken.AccessToken, jwtToken.RefreshToken);
            if (storedTokenResult.IsFailed)
            {
                return storedTokenResult.ToResult<JwtTokenDto>();
            }
            var storedToken = storedTokenResult.Value;

            //Update token is used
            //storedToken.IsRevoked = true;
            //storedToken.IsUsed = true;
            //await refreshTokenService.UpdateAsync(storedToken);

            // warning: delete token
            await refreshTokenService.DeleteAsync(storedToken);

            var user = await userRepository.GetByIdAsync(storedToken.UserId);
            if (user is null)
            {
                return Result.Fail("Token không hợp lệ");
            }
            return await CreateToken(user.Adapt<UserDto>());
        }



        //public async Task<Result> ChangePassword(User user, string newPassword)
        //{
        //    try
        //    {
        //        var salt = HashedPassword.GenerateRandomKey();              
        //        user.Salt = salt;
        //        user.Password = newPassword.ToSHA512Hash(salt);
        //        userRepository.Update(user);
        //        await userRepository.SaveAsync();
        //        return Result.Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
        //    }
        //}
    
        private async Task<Result<RefreshToken>> ValidateToken(string accessTokenString, string refreshTokenString)
        {
            try
            {
                var principal = _jwtHandler.ValidateToken(accessTokenString, Constants.TOKEN_VALIDATION_PARAM, out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);
                    if (!result)//false
                    {
                        return Result.Fail("Token không hợp lệ");
                    }
                }

                //check 3: Check accessToken expire?

                var utcExpireDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value ?? throw new NullReferenceException());

                var expireDate = Utils.ConvertUnixTimeToDateTime(utcExpireDate);
                if (expireDate > DateTime.UtcNow)
                {
                    return Result.Fail("Token đã hết hạn");
                }

                //check 4: Check refreshtoken exist in DB
                var storedToken = await refreshTokenService.GetByTokenStringAsync(refreshTokenString);
                if (storedToken == null)
                {
                    return Result.Fail("Token không hợp lệ");
                }

                //check 5: check refreshToken is used/revoked?
                if (storedToken.IsUsed)
                {
                    return Result.Fail("Token không hợp lệ");
                }
                if (storedToken.IsRevoked)
                {
                    return Result.Fail("Token không hợp lệ");
                }
                //check 6: AccessToken id == JwtId in RefreshToken
                var jti = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (storedToken.JwtId != jti)
                {
                    return Result.Fail("Token không hợp lệ");
                }

                return storedToken;
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
        private async Task InsertRefreshTokenAsync(int userId, SecurityToken token, string tokenString, TimeSpan expiresIn)
        {
            var refreshToken = (new RefreshToken
            {
                Id = Guid.NewGuid(),
                JwtId = token.Id,
                Token = tokenString,
                UserId = userId,
                IsRevoked = false,
                IsUsed = false,
                IssuedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.Add(expiresIn),
            });
            await refreshTokenService.InsertAsync(refreshToken);
        }

        public async Task<UserDto?> GetProfile(int id)
        {
            var user = await userRepository.GetByIdAsync(id);
            return user.Adapt<UserDto>();
        }
    }
}
