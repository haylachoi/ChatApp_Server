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
        Task<MessageDto?> GetAsync(long id);
        Task<IEnumerable<MessageDto>> GetFromTo(int roomId, long from, long to);
        Task<Result<MessageDto>> CreateMessageAsync(MessageParam param);    
        Task<MessageDto?> GetFirstMessageAsync(int roomId);    
        Task<IEnumerable<MessageDto>> GetPreviousMessagesAsync(int roomId, long messageId, int numberMessages);
        Task<IEnumerable<MessageDto>> GetNextMessagesAsync(int roomId, long messageId, int numberMessages);
        Task<IEnumerable<MessageDto>> GetSomeMessagesAsync(int roomId, int userId, int numberMessages = 10);
        Task<Result<MessageDetailDto>> AddOrUpdateReactionMessage(long mesageId, int userId, int reactionId);
        Task<Result<MessageDetailDto>> DeleteMessageDetail(long mesageId, int userId);

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

        public async Task<Result<MessageDetailDto>> AddOrUpdateReactionMessage(long messageId, int userId, int reactionId)     
        => await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
        {         
            var message = await _messageRepo.GetAsync(new MessageCriteria { Id = messageId });
            if (message == null)
            {
                return Result.Fail("User không ở trong room có tin nhắn này");
            }
            var messageDetail = await _messageDetailRepo.AddOrUpdateReaction(messageId, message.RoomId, userId, reactionId);

            return messageDetail.Adapt<MessageDetailDto>();
        });

        public async Task<MessageDto?> GetAsync(long id)
        {
            var message = await _messageRepo.GetAsync(new MessageCriteria { Id=id });
            return message.Adapt<MessageDto>();
        }

        public async Task<IEnumerable<MessageDto>> GetFromTo(int roomId, long from, long to)
        {
            var messages = await _messageRepo.GetAllAsync(new MessagesCriteria { From = from, To = to, RoomId = roomId });
            return messages.Adapt<IEnumerable<MessageDto>>();
        }

        public async Task<Result<MessageDetailDto>> DeleteMessageDetail(long mesageId, int userId)
        => await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
        {
            var messageDetail = await _messageDetailRepo.GetAsync(new MessageDetailCriteria { MessageId = mesageId, UserId = userId });
            if (messageDetail == null)
                return Result.Fail("Chi tiết tin nhắn không tồn tại");

            _messageDetailRepo.Delete(messageDetail);
            await _messageDetailRepo.SaveAsync();
            return messageDetail.Adapt<MessageDetailDto>();
        });
    }
}
