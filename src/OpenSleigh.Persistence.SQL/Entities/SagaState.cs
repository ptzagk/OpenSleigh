﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenSleigh.Persistence.SQL.Entities
{
    public class SagaState
    {
        private SagaState() { }
        
        public SagaState(Guid correlationId, string type, byte[] data, Guid? lockId = null, DateTime? lockTime = null)
        {
            CorrelationId = correlationId;
            Type = type;
            Data = data;
            LockId = lockId;
            LockTime = lockTime;
        }

        public Guid CorrelationId { get; }
        public string Type { get; }
        public byte[] Data { get; }
        public Guid? LockId { get; }
        public DateTime? LockTime { get; }
    }

    internal class SagaStateEntityTypeConfiguration : IEntityTypeConfiguration<SagaState>
    {
        public void Configure(EntityTypeBuilder<SagaState> builder)
        {
            builder.ToTable("SagaStates", "dbo");

            builder.HasKey(e => new {e.CorrelationId, e.Type});

            builder.Property(e => e.Data);

            builder.Property(e => e.LockId);
            builder.Property(e => e.LockTime);
        }
    }
}