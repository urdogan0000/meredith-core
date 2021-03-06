﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WhyNotEarth.Meredith.Data.Entity;
using WhyNotEarth.Meredith.Data.Entity.Models.Modules.Volkswagen;
using WhyNotEarth.Meredith.Exceptions;

namespace WhyNotEarth.Meredith.Volkswagen
{
    public class ArticleService
    {
        private readonly MeredithDbContext _dbContext;

        public ArticleService(MeredithDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateAsync(string categorySlug, DateTime date, string headline, string description,
            decimal? price, DateTime? eventDate, string? imageUrl)
        {
            var category = await ValidateAsync(categorySlug, date);

            var article = new Article
            {
                CategoryId = category.Id,
                Date = date,
                Headline = headline,
                Description = description,
                Price = price,
                EventDate = eventDate
            };

            if (!string.IsNullOrEmpty(imageUrl))
            {
                article.Image = new ArticleImage
                {
                    Url = imageUrl
                };
            }

            await _dbContext.Articles.AddAsync(article);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Article> EditAsync(int articleId, string categorySlug, DateTime date, string headline,
            string description, decimal? price, DateTime? eventDate)
        {
            var category = await ValidateAsync(categorySlug, date);

            var article = await GetAsync(articleId);

            article.CategoryId = category.Id;
            article.Date = date;
            article.Headline = headline;
            article.Description = description;
            article.Price = price;
            article.EventDate = eventDate;

            _dbContext.Articles.Update(article);
            await _dbContext.SaveChangesAsync();

            return article;
        }

        public async Task DeleteAsync(int articleId)
        {
            var article = await GetAsync(articleId);

            if (article.Image != null)
            {
                // I'm not sure why but cascade doesn't work on this
                var isUsedInAnyOtherArticle =
                    _dbContext.Articles.Any(item => item.ImageId == article.ImageId && item.Id != article.Id);

                if (!isUsedInAnyOtherArticle)
                {
                    _dbContext.Images.Remove(article.Image);
                }
            }

            _dbContext.Articles.Remove(article);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<Article> GetAsync(int articleId)
        {
            var article = await _dbContext.Articles
                .FirstOrDefaultAsync(item => item.Id == articleId);

            if (article is null)
            {
                throw new RecordNotFoundException($"Article {articleId} not found");
            }

            var jumpStart = await _dbContext.JumpStarts.FirstOrDefaultAsync(item => item.DateTime.Date == article.Date);
            if (jumpStart != null && jumpStart.Status != JumpStartStatus.Preview)
            {
                throw new InvalidActionException($"The email of {article.Date.ToShortDateString()} had already sent");
            }

            return article;
        }

        private async Task<ArticleCategory> ValidateAsync(string categorySlug, DateTime date)
        {
            var category = await _dbContext.Categories.OfType<ArticleCategory>()
                .FirstOrDefaultAsync(item => item.Slug == categorySlug.ToLower());

            if (category is null)
            {
                throw new RecordNotFoundException($"Category {categorySlug} not found");
            }

            var jumpStart = await _dbContext.JumpStarts.FirstOrDefaultAsync(item => item.DateTime.Date == date.Date);
            if (jumpStart != null && jumpStart.Status != JumpStartStatus.Preview)
            {
                throw new InvalidActionException($"The email of {date.ToShortDateString()} had already sent");
            }

            return category;
        }
    }
}