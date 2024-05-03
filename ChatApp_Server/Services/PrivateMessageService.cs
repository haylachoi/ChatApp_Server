using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Google.Apis.Upload;
using Mapster;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IPrivateMessageService: IBaseService<IPrivateMessageRepository, PrivateMessage, PrivateMessageParameter,long, PrivateMessageDto>
    {
        Task<Result<PrivateMessageDto>> GetFirstMessage(int roomId);
        Task<Result<PrivateMessageDto>> UpdateIsSeen(long id);        
        Task<IEnumerable<PrivateMessageDto>> GetPreviousMessageAsync(int roomId, long messageId, int? numberMessages);
        Task<IEnumerable<PrivateMessageDto>> GetNextMessageAsync(int roomId, long messageId, int? numberMessages);
        Task<IEnumerable<PrivateMessageDto>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages = 10);
        Task<Result<PrivateMessageDto>> UpdateReactionMessage(long messageId, int receiverId, int? reactionId);


    }
    public class PrivateMessageService : BaseService<IPrivateMessageRepository, PrivateMessage, PrivateMessageParameter, long, PrivateMessageDto>, IPrivateMessageService
    {
        public PrivateMessageService(IPrivateMessageRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<PrivateMessageDto>> GetAllAsync(PrivateMessageParameter parameter)
        {
            List<Expression<Func<PrivateMessage, bool>>> filters = new List<Expression<Func<PrivateMessage, bool>>>();
            var senderId = parameter.SenderId;
            var receiverId = parameter.ReceiverId;
            if (parameter.IsTwoWay)
            {
                filters.Add(pm => (pm.SenderId == senderId && pm.ReceiverId == receiverId) || (pm.SenderId == receiverId && pm.ReceiverId == senderId));
            }
            else
            {
                filters.Add(pm => pm.SenderId == senderId && pm.ReceiverId == receiverId);
            }
            var pms =  await _repo.GetAllAsync(filters);       
            return pms.Adapt<IEnumerable<PrivateMessageDto>>();
        }

        public async Task<IEnumerable<PrivateMessageDto>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages = 10)
        {
            var seenPMs = await _repo.GetSeenAndUnseenAsync(roomId, firstUnseenMessageId, numberMessages);

            return seenPMs.Adapt<IEnumerable<PrivateMessageDto>>();
        }

        public async Task<IEnumerable<PrivateMessageDto>> GetNextMessageAsync(int roomId, long messageId, int? numberMessages = 10)
        {                 
            var pms = await _repo.GetAllAsync([pm => pm.PrivateRoomId == roomId && pm.Id > messageId], orderBy: query => query.OrderBy(pm => pm.CreatedAt), take: numberMessages);
            return pms.Adapt<IEnumerable<PrivateMessageDto>>();
        }

        public async Task<IEnumerable<PrivateMessageDto>> GetPreviousMessageAsync(int roomId, long messageId, int? numberMessages = 10)
        {
            var pms = await _repo.GetAllAsync([pm => pm.PrivateRoomId == roomId && pm.Id < messageId], orderBy: query => query.OrderByDescending(pm => pm.CreatedAt) , take: numberMessages);
            pms = pms.OrderBy(pm => pm.Id);
            return pms.Adapt<IEnumerable<PrivateMessageDto>>();
        }

        public async Task<Result<PrivateMessageDto>> UpdateIsSeen(long id)
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
                var dto = ms.Adapt<PrivateMessageDto>();
                return Result.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
        public async Task<Result<PrivateMessageDto>> GetFirstMessage(int roomId)
        {
            try
            {
                var messages = await _repo.GetAllAsync([pm => pm.PrivateRoomId == roomId], query => query.OrderBy(pm => pm.Id), take: 1);
                var fm = messages.FirstOrDefault();
                if (fm == null)
                {
                    return Result.Fail("Room không có tin nhắn");
                }
                return Result.Ok(fm.Adapt<PrivateMessageDto>());

            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

    

        public async Task<Result<PrivateMessageDto>> UpdateReactionMessage(long messageId, int receiverId, int? reactionId)
        {
            try
            {
                var pm = await _repo.UpdateReactionMessage(messageId, receiverId, reactionId);
                if (pm == null)
                {
                    return Result.Fail("Người dùng không thể gửi biểu cảm ở tín nhắn này");
                }
                return Result.Ok(pm.Adapt<PrivateMessageDto>());
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
    }
}
