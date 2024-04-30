using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Google.Apis.Upload;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IPrivateRoomService: IBaseService<IPrivateRoomRepository, PrivateRoom, PrivateRoomParameter, int, PrivateRoomDto>
    {
        Task<PrivateRoomDto?> GetOneByRoomAsync(int id, int? userId);
        Task<PrivateRoomDto?> GetOneByUserAsync(int userId1, int userId2);
        Task<PrivateMessageDto> GetLastUnseenMessage(int roomId, int userId);
        //Task<PrivateRoomDto?> GetOneAsync(int biggerUserId, int smallerUserId);

    }
    public class PrivateRoomService : BaseService<IPrivateRoomRepository, PrivateRoom, PrivateRoomParameter, int, PrivateRoomDto>, IPrivateRoomService
    {
        public PrivateRoomService(IPrivateRoomRepository repo) : base(repo)
        {         
        }

        public override async Task<IEnumerable<PrivateRoomDto>> GetAllAsync(PrivateRoomParameter parameter)
        {
            var biggerUserId = parameter.BiggerUserId;
            var smallerUserId = parameter.SmallerUserId;
            var userId = parameter.UserId;
            var numberMessage = parameter.NumberMessage;
            List<Expression<Func<PrivateRoom, bool>>> filters = new List<Expression<Func<PrivateRoom, bool>>>();
            Func<IQueryable<PrivateRoom>, IIncludableQueryable<PrivateRoom, object>>? includes = query => query
                //.Include(pr => pr.BiggerUser)
                //.Include(pr => pr.SmallerUser)
                .Include(pm => pm.PrivateRoomInfos).ThenInclude(pri => pri.User);
            //.Include(pr => pr.PrivateMessages.OrderByDescending(pm => pm.CreatedAt).Take(numberMessage))
            ;
                
            
            if (userId != null)
            {
                filters.Add(pm => pm.SmallerUserId == userId || pm.BiggerUserId == userId);
            } 
            else
            {
                if (biggerUserId != null)
                {
                    filters.Add(pm => pm.BiggerUserId == biggerUserId);                
                }
                if (smallerUserId != null)
                {
                    filters.Add(pm => pm.SmallerUserId == smallerUserId);
                }
            }
            
            var prs = await _repo.GetAllAsync(filters, includes: includes);
            var prDtos = prs.Adapt<IEnumerable<PrivateRoomDto>>().ToArray();
          
            foreach (var prDto in prDtos)
            {
                //if (prDto.SmallerUser != null && prDto.SmallerUser.Id != userId)
                //{
                //    prDto.Friend = prDto.SmallerUser;
                //    prDto.FirstUnseenMessageId = prDto.FirstUnseenBiggerUserMessageId;
                //}
                //else
                //{
                //    prDto.Friend = prDto.BiggerUser;
                //    prDto.FirstUnseenMessageId = prDto.FirstUnseenSmallerUserMessageId;
                //}

                //prDto.SmallerUser = null;
                //prDto.BiggerUser = null;

                prDto.Friend = prDto.PrivateRoomInfos?.Single(pri => pri.UserId != userId).User;
                var user_pr_info = prDto.PrivateRoomInfos?.Single(pri => pri.UserId == userId);
                prDto.FirstUnseenMessageId = user_pr_info?.FirstUnseenMessageId;
                prDto.LastUnseenMessageId = user_pr_info?.LastUnseenMessageId;
                prDto.UnseenMessageCount = user_pr_info?.UnseenMessageCount ?? 0;

                prDto.PrivateMessages = prDto?.PrivateMessages?.OrderBy(pm => pm.CreatedAt).ToArray();
            }           
            return prDtos;
        }

        public async Task<PrivateMessageDto> GetLastUnseenMessage(int roomId, int userId)
        {
            var message = await _repo.GetLastUnseenMessage(roomId, userId);
            var dto = message.Adapt<PrivateMessageDto>();
            return dto;
        }

        public async Task<PrivateRoomDto?> GetOneByRoomAsync(int roomId, int? userId = null)
        {
            Func<IQueryable<PrivateRoom>, IIncludableQueryable<PrivateRoom, object>> includes = query => query
                //.Include(pm => pm.BiggerUser)
                //.Include(pm => pm.SmallerUser)
                .Include(pm => pm.PrivateRoomInfos).ThenInclude(pri => pri.User);
              
                //.Include(pm => pm.PrivateMessages);
            var prs = await _repo.GetAllAsync([pr => pr.Id == roomId], includes: includes);
            var pr = prs.FirstOrDefault();
            var prDto = pr.Adapt<PrivateRoomDto>();
            //if (prDto.SmallerUser != null && prDto.SmallerUser.Id != roomId)
            //{
            //    prDto.Friend = prDto.SmallerUser;
            //} else
            //{
            //    prDto.Friend = prDto.BiggerUser;
            //}

            //prDto.SmallerUser = null;
            //prDto.BiggerUser = null;
            
            if (userId != null && prDto != null)
            {
                prDto.Friend = prDto.PrivateRoomInfos?.Single(pri => pri.UserId != userId).User;
                var user_pr_info = prDto.PrivateRoomInfos?.Single(pri => pri.UserId == userId);
                prDto.FirstUnseenMessageId = user_pr_info?.FirstUnseenMessageId;
                prDto.LastUnseenMessageId = user_pr_info?.LastUnseenMessageId;
                prDto.UnseenMessageCount = user_pr_info?.UnseenMessageCount ?? 0;

            }
            return prDto;
        }

        public async Task<PrivateRoomDto?> GetOneByUserAsync(int userId1, int userId2)
        {
            var biggerUserId = int.Max(userId1, userId2);
            var smallerUserId = int.Min(userId1, userId2);

            var prs = await _repo.GetAllAsync([pr => pr.BiggerUserId == biggerUserId && pr.SmallerUserId == smallerUserId]);
            var pr = prs.FirstOrDefault();
            return pr.Adapt<PrivateRoomDto>();
        }

    }
}
