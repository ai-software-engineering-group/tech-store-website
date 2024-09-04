﻿using Microsoft.EntityFrameworkCore;
using STech.Data.Models;
using STech.Services;
using STech.Services.Utils;

namespace STech.Services.Services
{
    public class ReviewService : IReviewService
    {
        private readonly StechDbContext _context;

        public ReviewService(StechDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetReviews(string productId, string? sort_by)
        {
            return await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Select(r => new Review
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Content = r.Content,
                    CreateAt = r.CreateAt,
                    ReviewerName = r.ReviewerName,
                    TotalLike = r.TotalLike,
                    TotalDislike = r.TotalDislike,
                    IsPurchased = r.IsPurchased,
                    User = r.User == null ? null : new User
                    {
                        UserId = r.User.UserId,
                        FullName = r.User.FullName,
                        Avatar = r.User.Avatar,
                        RoleId = r.User.RoleId,
                    },
                    ReviewImages = r.ReviewImages,
                    ReviewReplies = r.ReviewReplies.Select(rp => new ReviewReply
                    {
                        Id = rp.Id,
                        ReviewId = rp.ReviewId,
                        Content = rp.Content,
                        ReplyDate = rp.ReplyDate,
                        UserReply = new User
                        {
                            UserId = rp.UserReply.UserId,
                            FullName = rp.UserReply.FullName,
                            Avatar = rp.UserReply.Avatar,
                            RoleId = rp.UserReply.RoleId,
                        },
                    }).OrderByDescending(rp => rp.ReplyDate).ToList(),
                })
                .OrderByDescending(r => r.CreateAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Review>, int, int)> GetReviews(string productId, string? sort_by, int page = 1, int reviewsPerPage = 5)
        {
            IEnumerable<Review> reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Select(r => new Review
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Content = r.Content,
                    CreateAt = r.CreateAt,
                    ReviewerName = r.ReviewerName,
                    TotalLike = r.TotalLike,
                    TotalDislike = r.TotalDislike,
                    IsPurchased = r.IsPurchased,
                    User = r.User == null ? null : new User
                    {
                        UserId = r.User.UserId,
                        FullName = r.User.FullName,
                        Avatar = r.User.Avatar,
                        RoleId = r.User.RoleId,
                    },
                    ReviewImages = r.ReviewImages,
                    ReviewReplies = r.ReviewReplies.Select(rp => new ReviewReply
                    {
                        Id = rp.Id,
                        ReviewId = rp.ReviewId,
                        Content = rp.Content,
                        ReplyDate = rp.ReplyDate,
                        UserReply = new User
                        {
                            UserId = rp.UserReply.UserId,
                            FullName = rp.UserReply.FullName,
                            Avatar = rp.UserReply.Avatar,
                            RoleId = rp.UserReply.RoleId,
                        },
                    }).OrderByDescending(rp => rp.ReplyDate).ToList(),
                })
                .OrderByDescending(r => r.CreateAt)
                .ToListAsync();

            int totalReviews = reviews.Count();
            int totalPages = (int)Math.Ceiling((double)totalReviews / reviewsPerPage);

            return (reviews.Paginate(page, reviewsPerPage), totalPages, totalReviews);
        }

        public async Task<Review?> GetReview(int reviewId)
        {
            return await _context.Reviews
                .Where(r => r.Id == reviewId)
                .Select(r => new Review
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Content = r.Content,
                    CreateAt = r.CreateAt,
                    ReviewerName = r.ReviewerName,
                    TotalLike = r.TotalLike,
                    TotalDislike = r.TotalDislike,
                    IsPurchased = r.IsPurchased,
                    User = r.User == null ? null : new User
                    {
                        UserId = r.User.UserId,
                        FullName = r.User.FullName,
                        Avatar = r.User.Avatar,
                        RoleId = r.User.RoleId,
                    },
                    ReviewImages = r.ReviewImages,
                    ReviewReplies = r.ReviewReplies.Select(rp => new ReviewReply
                    {
                        Id = rp.Id,
                        ReviewId = rp.ReviewId,
                        Content = rp.Content,
                        ReplyDate = rp.ReplyDate,
                        UserReply = new User
                        {
                            UserId = rp.UserReply.UserId,
                            FullName = rp.UserReply.FullName,
                            Avatar = rp.UserReply.Avatar,
                            RoleId = rp.UserReply.RoleId,
                        },
                    }).OrderByDescending(rp => rp.ReplyDate).ToList(),
                })
                .FirstOrDefaultAsync();
        }


        public async Task<bool> CreateReview(Review review)
        {
            await _context.Reviews.AddAsync(review);
            return await _context.SaveChangesAsync() > 0;
        }


        public async Task<bool> UpdateReview(Review review)
        {
            Review? oldReview = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == review.Id);
            if (oldReview == null)
            {
                return false;
            }

            oldReview.TotalLike = review.TotalLike;
            oldReview.TotalDislike = review.TotalDislike;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteReview(int reviewId)
        {
            Review? review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null)
            {
                return false;
            }

            _context.Reviews.Remove(review);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> CreateReviewReply(ReviewReply reviewReply)
        {
            await _context.ReviewReplies.AddAsync(reviewReply);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}