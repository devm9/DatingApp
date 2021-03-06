using Microsoft.AspNetCore.SignalR;
using AutoMapper;
using API.Interfaces;
using System.Threading.Tasks;
using System;
using API.DTOs;
using API.Entities;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;
        private readonly IUnitOfWork _unitOfWork;
        public MessageHub(IMapper mapper, IUnitOfWork unitOfWork,            
        IHubContext<PresenceHub> presenceHub, PresenceTracker tracker)
        {
            _unitOfWork = unitOfWork;
            _tracker = tracker;
            _presenceHub = presenceHub;
            _userRepository = userRepository;
            _mapper = mapper;
            _messageRepository = messageRepository;
        }

        public override async Task OnConnectedAsync(){
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

            if(_unitOfWork.HasChanges()) await _unitOfWork.Complete();
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception){
            await RemoveFromMessageGroup(Context.ConnectionId);
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto) {
            var username = Context.User.GetUsername();

            if(username == createMessageDto.RecipientUsername.ToLower())
                return new HubException("You cannot send message to yourself");

            var sender = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if(recipient == null) return new HubException("Not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.username,
                RecipientUsername = recipient.username,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);

            if(group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else {
                var connections = await _tracker.GetConnectionsForUser(recipient.UserName);
                if(connections != null)
                {   
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", 
                    new {username = sender.UserName, knownAs = sender.knownAs});
                }
            }

            _unitOfWork.MessageRepository.AddMessage(message);

            if(await _unitOfWork.Complete()){                 
                await Clients.Group(group).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
            }
            
        }

        private async Task<bool> AddToGroup(string groupName) {
            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(context.connectionId, Context.User.GetUsername());

            if(group == null)
            {
                group = new Group(groupName);
                _unitOfWork.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if(await _unitOfWork.Complete()) return group;

            throw new HubException("Failed to join group");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _unitOfWork.MessageRepository.GetGroupForConnection(Context.connectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _messageRepository.RemoveConnection(connection);
            if(await _unitOfWork.Complete()) return group;

            throw new HubException("Failed to remove from group");
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}