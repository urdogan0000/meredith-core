﻿using System;
using WhyNotEarth.Meredith.Data.Entity.Models.Modules.Volkswagen;

namespace WhyNotEarth.Meredith.App.Results.Api.v0.Volkswagen
{
    public class ArticleResult
    {
        public int Id { get; }

        public DateTime Date { get; }

        public ArticleCategoryResult Category { get; }

        public string Headline { get; }

        public string Description { get; }

        public string? Image { get; }

        public DateTime? EventDate { get; }

        public decimal? Price { get; }

        public ArticleResult(Article article)
        {
            Id = article.Id;
            Date = article.Date;
            Category = new ArticleCategoryResult(article.Category);
            Headline = article.Headline;
            Description = article.Description;
            Image = article.Image?.Url;
            EventDate = article.EventDate;
            Price = article.Price;
        }
    }
}