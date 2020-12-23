using API.Interfaces;
using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using API.Helpers;
using API.Entities;
using AutoMapper;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext __context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext _context, IMapper mapper){
            _mapper = mapper;
            __context = _context;
        }

        public void AddMessage(Message message){
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message){
            _context.Message.Remove(message);
        }

        public async Task<Message> GetMessage(int id){
            return await _context.Messages
            .Include(u => u.Sender)
            .Include(u => u.Recipient)
            .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams){
            var query = _context.Messages
            .OrderByDescending(m => m.MessageSent)
            .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipientUserName == messageParams.Username && u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u.SenderUserName == messageParams.Username && u.SenderDeleted == false),
                _ => query.Where(u => u.RecipientUserName == messageParams.Username && u.RecipientDeleted == false && u.DateRead == null)                
            };            

            return await PagedList<Message>.CreateAsync(query, messageParams.PageNumber, messageParams.PageSize);
        }

        public Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername){
            var messages = await _context.Messages
            .Include(u => u.Sender).ThenInclude(p => p.Photos)
            .Include(u => u.Recipient).ThenInclude(p => p.Photos)
            .Where(m => m.Recipient.UserName == currentUsername && m.RecipientDeleted == false
            && m.Sender.UserName == recipientUsername
            || m.Recipient.UserName == recipientUsername
            && m.Sender.UserName == currentUsername && m.SenderDeleted == false )
            .OrderBy(m => m.MessageSent)
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.Recipient.UserName == currentUsername).ToList();

            if(unreadMessages.Any()){
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }

                
            }

            return messages;
        }

        public Task<Group> GetGroupForConnection(string connectionId){
            return await _context.Groups.Include(c => c.Connections)
            .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
            .FirstOrDefaultAsync();
        }

        public void AddGroup(Group group){
            _context.Groups.Add(message);
        }

        public void RemoveConnection(Connection connection){
            _context.Connections.Remove(connection);
        }

        public async Task<Connection> GetConnection(string connectionId){
            return await _context.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetMessageGroup(string groupName){
            return await _context.Groups
            .Include(x => x.Connections)
            .FirstOrDefaultAsync(x => x.Name == groupName);
        }
    }
}