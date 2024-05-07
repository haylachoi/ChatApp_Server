using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;
using System.Linq.Expressions;

namespace ChatApp_Server.Services
{
    public interface IMessageDetailService: IBaseService<IMessageDetailRepository, MessageDetail, MessageDetailParameter, long,  MessageDetailDto>
    {
       Task<Result<MessageDetailDto>> AddOrUpdateReaction(long mesageId, int userId, int? reactionId);
       Task<Result<MessageDetailDto>> AddOrUpdateIsReaded(long mesageId, int userId);
    }
    public class MessageDetailService : BaseService<IMessageDetailRepository, MessageDetail, MessageDetailParameter, long, MessageDetailDto>, IMessageDetailService
    {
        public MessageDetailService(IMessageDetailRepository repo) : base(repo)
        {
        }

        public async Task<Result<MessageDetailDto>> AddOrUpdateIsReaded(long mesageId, int userId)
        {
            return await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
            {
                var messageDetail = await _repo.AddOrUpdateIsReaded(mesageId, userId);
                return messageDetail.Adapt<MessageDetailDto>();
            });
        }

        public async Task<Result<MessageDetailDto>> AddOrUpdateReaction(long mesageId, int userId, int? reactionId)
        {
            return await ExceptionHandler.HandleLazy<MessageDetailDto>(async () =>
            {
                var messageDetail = await _repo.AddOrUpdateReaction(mesageId, userId, reactionId);
                return messageDetail.Adapt<MessageDetailDto>();
            });
        }

        public override async Task<IEnumerable<MessageDetailDto>> GetAllAsync(MessageDetailParameter parameter)
        {
            List<Expression<Func<MessageDetail, bool>>> filters = new List<Expression<Func<MessageDetail, bool>>>();
            var messageId = parameter.MessageId;
            var userId = parameter.UserId;

            if (messageId != null)
            {
                filters.Add(md => md.MessageId == messageId);
            }
            if (userId != null)
            {
                filters.Add(md => md.UserId ==  userId);
            }

            var mesageDetail = await _repo.GetAllAsync(filters);
            return mesageDetail.Adapt<IEnumerable<MessageDetailDto>>();
        }
    }
}
