using ChatApp_Server.DTOs;
using ChatApp_Server.Helper;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Services;
using Mapster;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp_Server.Hubs
{
    public class PrivateRoomHub: Hub
    {
        private readonly IPrivateRoomService privateRoomService;
        private readonly IPrivateMessageService privateMessageService;
        private readonly IPrivateRoomInfoService privateRoomInfoService;

        public PrivateRoomHub(IPrivateRoomService privateRoomService, IPrivateMessageService privateMessageService, IPrivateRoomInfoService privateRoomInfoService)
        {
            this.privateRoomService = privateRoomService;
            this.privateMessageService = privateMessageService;
            this.privateRoomInfoService = privateRoomInfoService;
        }
        public async Task<HubResponse> GetLastMessageUnseen(int roomId)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var message = await privateRoomService.GetLastUnseenMessage(roomId, userId);
            return HubResponse.Ok(message);
        }
        public async Task<HubResponse> CreatePrivateRoom(int receiverId)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }
            //var bigger = receiverId;
            //var smaller = userId;
            //if (smaller > bigger)
            //{
            //    bigger = userId;
            //    smaller = receiverId;
            //}
            var result = await privateRoomService.InsertAsync(new PrivateRoomDto
            {
                //BiggerUserId = bigger,
                //SmallerUserId = smaller,
                PrivateRoomInfos = new List<PrivateRoomInfoDto>()
                {
                    new PrivateRoomInfoDto { UserId = userId, CanDisplayRoom = true},
                    new PrivateRoomInfoDto { UserId = receiverId, CanDisplayRoom = false},
                }
            });

            if (result.IsFailed)
            {
                return HubResponse.Fail(new { error = result.Errors[0].Message });
            }
            if (!result.Value.Id.HasValue) {
                return HubResponse.Fail("Không lấy được Id");
            }
            var pr = await privateRoomService.GetOneByRoomAsync(result.Value.Id.Value, userId);
            if (pr == null)
            {
                return HubResponse.Fail("Có lỗi xảy ra!");
            }
            //pr.Friend = pr.PrivateRoomInfos?.Single(pri => pri.Id == receiverId).User;
            //var user_pr_info = pr.PrivateRoomInfos?.Single(pri => pri.Id == userId);
            //pr.FirstUnseenMessageId = user_pr_info?.FirstUnseenMessageId;

            var room = pr.Adapt<RoomDto>();

            await Clients.Caller.SendAsync("CreatePrivateRoom", room);
            await Clients.User(receiverId.ToString()).SendAsync("CreatePrivateRoom", room);
            return HubResponse.Ok(pr);
        }

        public async Task<HubResponse> GetPrivateRooms()
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }
            
            var privateRooms = await privateRoomService.GetAllAsync(new PrivateRoomParameter
            {
                UserId = userId,
            });

            //foreach (var rooms in privateRooms)
            //{
            //    if (rooms.LastUnseenMessageId != null)
            //    {
            //        rooms.LastUnseenMessage = await privateMessageService.GetByIdAsync(rooms.LastUnseenMessageId.Value);
            //    }          
            //}
            var rooms = privateRooms.Adapt<RoomDto>();
            return HubResponse.Ok(rooms);
        }
        public async Task<HubResponse> UpdateCanRoomDisplay(int roomId, bool canDisplay)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var result = await privateRoomInfoService.UpdateCanRoomDisplay(roomId, userId, canDisplay);
            if (result.IsFailed)
            {
                return HubResponse.Fail(result.Errors);
            }
            return HubResponse.Ok(result.Value);
        }
        public async Task<HubResponse> GetSomePrivateMessages(int privateRoomId)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var room = await privateRoomService.GetOneByRoomAsync(privateRoomId, userId);
            if (room == null || room.Id == null || room.PrivateRoomInfos == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
         
            //if (room.BiggerUserId != userId && room.SmallerUserId != userId)
            //{
            //    return HubResponse.Fail("Không có quyền truy cập room này");
            //}

            //var firstUnseenMessageId = room.BiggerUserId == userId ? room.FirstUnseenBiggerUserMessageId : room.FirstUnseenSmallerUserMessageId;
            var roomInfoOfUser = room.PrivateRoomInfos.FirstOrDefault(info => info.Id == userId);
            if (roomInfoOfUser == null)
            {
                return HubResponse.Fail("User Ko có trong room này");
            }
            var pms = await privateMessageService.GetSeenAndUnseenAsync(privateRoomId, roomInfoOfUser?.FirstUnseenMessageId);
            
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetFirstMessage(int privateRoomId)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var room = await privateRoomService.GetOneByRoomAsync(privateRoomId, userId);
            if (room == null || room.Id == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
            //if (room.BiggerUserId != userId && room.SmallerUserId != userId)
            //{
            //    return HubResponse.Fail("Không có quyền truy cập room này");
            //}
            var fMessageResult = await privateMessageService.GetFirstMessage(privateRoomId);
            if (fMessageResult.IsFailed)
            {
                return HubResponse.Fail(fMessageResult.Errors[0].Message);
            }
            return HubResponse.Ok(fMessageResult.Value);
        }

        public async Task<HubResponse> GetPreviousPrivateMessages(int privateRoomId, long messageId, int numberMessages)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var room = await privateRoomService.GetByIdAsync(privateRoomId);
            if (room == null || room.Id == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
           
            var pms = await privateMessageService.GetPreviousMessageAsync(room.Id.Value, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }
        public async Task<HubResponse> GetNextPrivateMessages(int privateRoomId, long messageId, int? numberMessages)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId))
            {
                return HubResponse.Fail("Mã định danh người dùng ko hợp lệ");
            }

            var room = await privateRoomService.GetOneByRoomAsync(privateRoomId, userId);
            if (room == null || room.Id == null)
            {
                return HubResponse.Fail("Room không tồn tại");
            }
            //if (room.BiggerUserId != userId && room.SmallerUserId != userId)
            //{
            //    return HubResponse.Fail("Không có quyền truy cập room này");
            //}
            var pms = await privateMessageService.GetNextMessageAsync(room.Id.Value, messageId, numberMessages);
            return HubResponse.Ok(pms);
        }
    }
}
