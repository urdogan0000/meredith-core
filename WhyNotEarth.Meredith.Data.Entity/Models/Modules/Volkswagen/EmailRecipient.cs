﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhyNotEarth.Meredith.Data.Entity.Models.Modules.Volkswagen
{
    public class EmailRecipient : IEntityTypeConfiguration<EmailRecipient>
    {
        public int Id { get; set; }

        public int? MemoId { get; set; }

        public Memo Memo { get; set; }

        public int? JumpStartId { get; set; }

        public JumpStart JumpStart { get; set; }

        public string Email { get; set; }

        public string DistributionGroup { get; set; }

        public EmailStatus Status { get; set; }

        public DateTime? DeliverDateTime { get; set; }

        public DateTime? OpenDateTime { get; set; }

        public void Configure(EntityTypeBuilder<EmailRecipient> builder)
        {
            builder.ToTable("EmailRecipients", "ModuleVolkswagen");
        }
    }

    public enum EmailStatus : byte
    {
        None = 0,
        ReadyToSend = 1,
        Sent = 2,
        Delivered = 3,
        Opened = 4,
        Clicked = 5
    }
}