using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Params;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IMessageService
    {
        Task<Result<MessageDto>> CreateMessageAsync(MessageParam param);
        Task<Result<MessageDto>> GetOneAsync(long messageId);
        Task<Result<MessageDto>> GetOneIncludeRoomInfoAsync(long messageId, int userId);
        Task<Result<MessageDto>> GetFirstMessageAsync(int roomId);
        Task<Result<MessageDto>> UpdateIsSeenAsync(long id);        
        Task<IEnumerable<MessageDto>> GetPreviousMessageAsync(int roomId, long messageId, int? numberMessages);
        Task<IEnumerable<MessageDto>> GetNextMessageAsync(int roomId, long messageId, int? numberMessages);
        Task<IEnumerable<MessageDto>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages = 10);
        Task<Result<MessageDetailDto>> AddOrUpdateReaction(long mesageId, int userId, int? reactionId);
        Task<Result<MessageDetailDto>> AddOrUpdateIsReaded(long mesageId, int userId);
    


    }
    public class MessageService(IMessageRepository messageRepository, IMessageDetailRepository messageDetailRepository): IMessageService
    {     
        public async Task<IEnumerable<MessageDto>> GetAllAsync(int senderId)
        {
            List<Expression<Func<Message, bool>>> filters = new List<Expression<Func<Message, bool>>>();        
            filters.Add(pm => pm.SenderId == senderId);            
            var pms =  await messageRepository.GetAllAsync(filters);       
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages = 10)
        {
            var seenPMs = await messageRepository.GetSeenAndUnseenAsync(roomId, firstUnseenMessageId, numberMessages);

            return seenPMs.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetNextMessageAsync(int roomId, long messageId, int? numberMessages = 10)
        {                 
            var pms = await messageRepository.GetAllAsync([pm => pm.RoomId == roomId && pm.Id > messageId], orderBy: query => query.OrderBy(pm => pm.CreatedAt), take: numberMessages);
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetPreviousMessageAsync(int roomId, long messageId, int? numberMessages = 10)
        {
            var pms = await messageRepository.GetAllAsync([pm => pm.RoomId == roomId && pm.Id < messageId], orderBy: query => query.OrderByDescending(pm => pm.CreatedAt) , take: numberMessages);
            pms = pms.OrderBy(pm => pm.Id);
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<Result<MessageDto>> UpdateIsSeenAsync(long id)
        {
            var ms = await messageRepository.GetByIdAsync(id);
            if (ms == null)
            {
                return Result.Fail("Message không tồn tại");
            }

            ms.IsReaded = true;
            try
            {
                await messageRepository.SaveAsync();
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
                var messages = await messageRepository.GetAllAsync([pm => pm.RoomId == roomId], query => query.OrderBy(pm => pm.Id), take: 1);
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
                var message = await messageRepository.GetOne([m => m.Id == messageId && m.Room.RoomMemberInfos.Any(info => info.UserId == userId)]);
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

        public async Task<Result<MessageDto>> CreateMessageAsync(MessageParam param)
        => await ExceptionHandler.HandleLazy<MessageDto>(async () =>
        {
            var entity = param.Adapt<Message>();
            messageRepository.Insert(entity);
            await messageRepository.SaveAsync();
            return entity.Adapt<MessageDto>();         

        });

        public async Task<Result<MessageDetailDto>> AddOrUpdateIsReaded(long mesageId, int userId)
        {
            return await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
            {
                var messageDetail = await messageDetailRepository.AddOrUpdateIsReaded(mesageId, userId);
                return messageDetail.Adapt<MessageDetailDto>();
            });
        }

        public async Task<Result<MessageDetailDto>> AddOrUpdateReaction(long mesageId, int userId, int? reactionId)
        {
            return await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
            {
                var messageDetail = await messageDetailRepository.AddOrUpdateReaction(mesageId, userId, reactionId);
                return messageDetail.Adapt<MessageDetailDto>();
            });
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
