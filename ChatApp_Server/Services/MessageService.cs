using ChatApp_Server.Criteria;
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
        Task<MessageDto?> GetFirstMessageAsync(int roomId);    
        Task<IEnumerable<MessageDto>> GetPreviousMessagesAsync(int roomId, long messageId, int numberMessages);
        Task<IEnumerable<MessageDto>> GetNextMessagesAsync(int roomId, long messageId, int numberMessages);
        Task<IEnumerable<MessageDto>> GetSomeMessagesAsync(int roomId, int userId, int numberMessages = 10);
        Task<Result<MessageDetailDto>> AddOrUpdateReaction(long mesageId, int userId, int? reactionId);
  
    }
    public class MessageService(IMessageRepository _messageRepo, IMessageDetailRepository _messageDetailRepo, IRoomRepository _roomRepo): IMessageService
    {     
        public async Task<IEnumerable<MessageDto>> GetSomeMessagesAsync(int roomId, int userId, int numberMessages = 10)
        {
            var seenPMs = await _messageRepo.GetSeenAndUnseenAsync(roomId, userId, numberMessages);
            return seenPMs.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetNextMessagesAsync(int roomId, long messageId, int numberMessages = 10)
        {
            var pms = await _messageRepo.GetNextMessagesAsync(roomId, messageId, numberMessages);
            return pms.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<IEnumerable<MessageDto>> GetPreviousMessagesAsync(int roomId, long messageId, int numberMessages = 10)
        {
            var pms = await _messageRepo.GetPreviousMessagesAsync(roomId, messageId, numberMessages);        
            return pms.Adapt<IEnumerable<MessageDto>>();
        }
   
        public async Task<MessageDto?> GetFirstMessageAsync(int roomId)
        {
            var message = await _roomRepo.GetFirstMessage(roomId);
            return message.Adapt<MessageDto?>();
        }

        public async Task<Result<MessageDto>> CreateMessageAsync(MessageParam param)
        => await ExceptionHandler.HandleLazy<MessageDto>(async () =>
        {
            var room = await _roomRepo.GetAsync(new RoomCriteria { Id = param.RoomId, MemberId = param.SenderId });
            if (room == null)
                return Result.Fail("Người dùng không phải là thành viên của nhóm");

            var entity = param.Adapt<Message>();
            _messageRepo.Insert(entity);
            await _messageRepo.SaveAsync();

            return entity.Adapt<MessageDto>();         
        });

        public async Task<Result<MessageDetailDto>> AddOrUpdateReaction(long messageId, int userId, int? reactionId)     
        => await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
        {
            var message = await _messageRepo.GetOneAsync([m => m.Id == messageId && m.Room.RoomMemberInfos.Any(info => info.UserId == userId)]);
            if (message == null)
            {
                return Result.Fail("User không ở trong room có tin nhắn này");
            }
            var messageDetail = await _messageDetailRepo.AddOrUpdateReaction(messageId, message.RoomId, userId, reactionId);

            return messageDetail.Adapt<MessageDetailDto>();
        });
    }
}
