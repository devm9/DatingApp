using API.Interfaces;
using API.Helpers;
using API.DTOs;
using System.Threading.Tasks;
using API.Entities;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _context;
        public LikesRepository(DataContext context){
            _context = context;
        }

        public Task<UserLike> GetUserLike(int sourceUserId, int likedUserId){
            return await _context.Likes.FindAsync(sourceUserId, likedUserId);
        }

        public Task<AppUser> GetUserWithLikes(int userId){
            return await _context.Users
            .Include(x => x.LikedUsers)
            .SingleOrDefaultAsync(x => x.Id == userId);
        }

        public Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams){
            var users = _context.Users.OrderBy(u => u.UserName).AsQueryable();
            var likes = _context.Likes.AsQueryable();

            if(likesParams.predicate == "liked"){
                likes = likes.Where(like => like.sourceUserId == likesParams.UserId);
                users = likes.Select(like => like.LikedUser);
            }

            if(likesParams.predicate == "likedBy"){
                likes = likes.Where(like => like.sourceUserId == likesParams.userId);
                users = likes.Select(like => like.SourceUser);
            }

            var likedUsers = users.Select(user => new LikeDto
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Age = user.DateOfBirth.CalculateAge(),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = user.City,
                Id = user.Id
            });

            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
        }
    }
}