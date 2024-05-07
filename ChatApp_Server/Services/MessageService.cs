using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Google.Apis.Upload;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IMessageService: IBaseService<IMessageRepository, Message, MessageParameter,long, MessageDto>
    {
        Task<Result<MessageDto>> GetOneAsync(long messageId);
        Task<Result<MessageDto>> GetOneIncludeRoomInfoAsync(long messageId, int userId);
        Task<Result<MessageDto>> GetFirstMessageAsync(int roomId);
        Task<Result<MessageDto>> UpdateIsSeenAsync(long id);        
        Task<IEnumerable<MessageDto>> GetPreviousMessageAsync(int roomId, long messageId, int? numberMessages);
        Task<IEnumerable<MessageDto>> GetNextMessageAsync(int roomId, long messageId, int? numberMessages);
        Task<IEnumerable<MessageDto>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages = 10);

        //Task<Result<MessageDto>> UpdateReactionMessage(long messageId, int receiverId, int? reactionId);


    }
    public class MessageService : BaseService<IMessageRepository, Message, MessageParameter, long, MessageDto>, IMessageService
    {
        public MessageService(IMessageRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<MessageDto>> GetAllAsync(MessageParameter parameter)
        {
            List<Expression<Func<Message, bool>>> filters = new List<Expression<Func<Message, bool>>>();
            var senderId = parameter.SenderId;                  
            filters.Add(pm => pm.SenderId == senderId);            
            var pms =  await _repo.GetAllAsync(filters);       
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages = 10)
        {
            var seenPMs = await _repo.GetSeenAndUnseenAsync(roomId, firstUnseenMessageId, numberMessages);

            return seenPMs.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetNextMessageAsync(int roomId, long messageId, int? numberMessages = 10)
        {                 
            var pms = await _repo.GetAllAsync([pm => pm.RoomId == roomId && pm.Id > messageId], orderBy: query => query.OrderBy(pm => pm.CreatedAt), take: numberMessages);
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetPreviousMessageAsync(int roomId, long messageId, int? numberMessages = 10)
        {
            var pms = await _repo.GetAllAsync([pm => pm.RoomId == roomId && pm.Id < messageId], orderBy: query => query.OrderByDescending(pm => pm.CreatedAt) , take: numberMessages);
            pms = pms.OrderBy(pm => pm.Id);
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<Result<MessageDto>> UpdateIsSeenAsync(long id)
        {
            var ms = await _repo.GetByIdAsync(id);
            if (ms == null)
            {
                return Result.Fail("Message không tồn tại");
            }

            ms.IsReaded = true;
            try
            {
                await _repo.SaveAsync();
                var dto = ms.Adapt<MessageDto>();
                return Result.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
        public async Task<Result<MessageDto>> GetFirstMessageAsync(int roomId)
        {
            try
            {
                var messages = await _repo.GetAllAsync([pm => pm.RoomId == roomId], query => query.OrderBy(pm => pm.Id), take: 1);
                var fm = messages.FirstOrDefault();
                if (fm == null)
                {
                    return Result.Fail("Room không có tin nhắn");
                }
                return Result.Ok(fm.Adapt<MessageDto>());

            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public async Task<Result<MessageDto>> GetOneIncludeRoomInfoAsync(long messageId, int userId)
        {
           return await ExceptionHandler.HandleLazy<MessageDto>(async () =>
            {
                var message = await _repo.GetOne([m => m.Id == messageId && m.Room.RoomMemberInfos.Any(info => info.UserId == userId)]);
                if (message == null)
                {
                    return Result.Fail("User không ở trong room có tin nhắn này");
                }
                return message.Adapt<MessageDto>();

            });
        }

        public Task<Result<MessageDto>> GetOneAsync(long messageId)
        {
            throw new NotImplementedException();
        }



        //public async Task<Result<MessageDto>> UpdateReactionMessage(long messageId, int receiverId, int? reactionId)
        //{
        //    try
        //    {
        //        var pm = await _repo.UpdateReactionMessage(messageId, receiverId, reactionId);
        //        if (pm == null)
        //        {
        //            return Result.Fail("Người dùng không thể gửi biểu cảm ở tín nhắn này");
        //        }
        //        return Result.Ok(pm.Adapt<MessageDto>());
        //    }
        //    catch (Exception ex)
        //    {
        //        return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
        //    }
        //}
    }
}
