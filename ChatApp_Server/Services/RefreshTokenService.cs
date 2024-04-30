
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
    public class RefreshTokenService(IRefreshTokenRepository refreshTokenRepository) : IRefreshTokenService
    {

        public async Task<RefreshToken?> GetByTokenStringAsync(string token)
        {
            var tokens = await refreshTokenRepository.GetAllAsync([tk => tk.Token.Equals(token)]);
            return tokens.FirstOrDefault();
        }

        public async Task InsertAsync(RefreshToken refreshToken)
        {
            refreshTokenRepository.Insert(refreshToken);
            await refreshTokenRepository.SaveAsync();
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            refreshTokenRepository.Update(refreshToken);
            await refreshTokenRepository.SaveAsync();
        }
        public async Task DeleteAsync(RefreshToken refreshToken)
        {
            refreshTokenRepository.Delete(refreshToken);
            await refreshTokenRepository.SaveAsync();
        }
    }
}
