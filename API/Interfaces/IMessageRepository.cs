using System.Threading.Tasks;
using API.Entities;
using API.Helpers;
using API.Dtos;

namespace API.Interfaces
{
    public class IMessageRepository
    {
        void AddMessage(Message message);

        void DeleteMessage(Message message);

        Task<Message> GetMessage(int id);

        Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams);

        Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername);

        Task<bool> SaveAllAsync();
    }
}