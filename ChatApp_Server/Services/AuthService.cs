﻿using Microsoft.EntityFrameworkCore;
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
using Google.Api.Gax;
using ChatApp_Server.Params;
using ChatApp_Server.Criteria;

namespace ChatApp_Server.Services
{
    public interface IAuthService
    {
        Task<Result<UserDto>> ValidateCredential(string email, string password);
        Task<Result<ProfileDto>> Register(UserParam param);
        Task<Result<AuthToken>> CreateToken(UserDto user);
        Task<Result<AuthToken>> RenewToken(AuthToken jwtToken);
      
        //public Task<Result> ChangePassword(User user, string newPassword);
        //public Task<Result<string>> SendResetPasswordEmail(string email);
        //public Task<Result> ResetPassword(Guid id);


    }
    public class AuthService(
        IRefreshTokenService _refreshTokenService,
        IUserService _userService,
        IUserRepository _userRepo,
        IOptions<AppSettings> _settingsOptions
        ): IAuthService
    {
        private readonly JwtSecurityTokenHandler _jwtHandler = new JwtSecurityTokenHandler();
        public async Task<Result<ProfileDto>> Register(UserParam param)
        {
            return await _userService.CreateUser(param);
        }
        public async Task<Result<UserDto>> ValidateCredential(string email, string password)
        => await ExceptionHandler.HandleLazy<UserDto>(async () =>
        {
            //var users = await _userRepo.GetAllAsync([u => u.Email == email]);
            //var user = users.FirstOrDefault();
            var user = await _userRepo.GetAsync(new UserCriteria { Email = email });
            if (user == null)
            {
                return Result.Fail("Tài khoản hoặc password không chính xác");
            }
            if (user.Password == password.ToSHA512Hash(user.Salt))
            {
                return user.Adapt<UserDto>();
            }
            return Result.Fail("Tài khoản hoặc password không chính xác");
        });
        public async Task<Result<AuthToken>> CreateToken(UserDto user)
        => await ExceptionHandler.HandleLazy<AuthToken>(async () =>
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

            var setting = _settingsOptions.Value;
            var secret = setting.SecretKey;
            var (token, tokenString) = JwtTokenGenerator.GenerateAccessToken(identity, DateTime.UtcNow.AddMinutes(600), Encoding.ASCII.GetBytes(secret));

            var refreshTokenString = JwtTokenGenerator.GenerateRefreshoken();
            await InsertRefreshTokenAsync(user.Id.Value, token, refreshTokenString, TimeSpan.FromDays(1));
            return new AuthToken { AccessToken = tokenString, RefreshToken = refreshTokenString };
        });

        public async Task<Result<AuthToken>> RenewToken(AuthToken jwtToken)
        => await ExceptionHandler.HandleLazy(async () =>
        {
            var storedTokenResult = await ValidateToken(jwtToken.AccessToken, jwtToken.RefreshToken);
            if (storedTokenResult.IsFailed)
            {
                return storedTokenResult.ToResult<AuthToken>();
            }
            var storedToken = storedTokenResult.Value;

            //Update token is used
            //storedToken.IsRevoked = true;
            //storedToken.IsUsed = true;
            //await refreshTokenService.UpdateAsync(storedToken);

            // warning: delete token
            await _refreshTokenService.DeleteAsync(storedToken);

            var user = await _userRepo.GetByIdAsync(storedToken.UserId);
            if (user is null)
            {
                return Result.Fail("Token không hợp lệ");
            }
            return await CreateToken(user.Adapt<UserDto>());
        });

        private async Task<Result<RefreshToken>> ValidateToken(string accessTokenString, string refreshTokenString)
        => await ExceptionHandler.HandleLazy<RefreshToken>(async () =>
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

            var expireDate = DateTimeOffset.FromUnixTimeSeconds(utcExpireDate).UtcDateTime;
            if (expireDate > DateTime.UtcNow)
            {
                return Result.Fail("Token đã hết hạn");
            }

            //check 4: Check refreshtoken exist in DB
            var storedToken = await _refreshTokenService.GetByTokenStringAsync(refreshTokenString);
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
        });
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
            await _refreshTokenService.InsertAsync(refreshToken);
        }
    }
}
