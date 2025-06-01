
using ChatApp_Server.Criteria;
using ChatApp_Server.Models;
using ChatApp_Server.Repositories;
using Microsoft.EntityFrameworkCore;


namespace ChatApp_Server.Services
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken?> GetByTokenStringAsync(string token);
        Task InsertAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task DeleteAsync(RefreshToken refreshToken);
    }
    public class RefreshTokenService(IRefreshTokenRepository refreshTokenRepo) : IRefreshTokenService
    {

        public async Task<RefreshToken?> GetByTokenStringAsync(string token)
        {
            //var tokens = await refreshTokenRepository.GetAllAsync([tk => tk.Token.Equals(token)]);
            var rf = await refreshTokenRepo.GetAsync(new RefreshTokenCriteria { Token = token });
            return rf;
        }

        public async Task InsertAsync(RefreshToken refreshToken)
        {
            refreshTokenRepo.Insert(refreshToken);
            await refreshTokenRepo.SaveAsync();
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            refreshTokenRepo.Update(refreshToken);
            await refreshTokenRepo.SaveAsync();
        }
        public async Task DeleteAsync(RefreshToken refreshToken)
        {
            refreshTokenRepo.Delete(refreshToken);
            await refreshTokenRepo.SaveAsync();
        }
    }
}
