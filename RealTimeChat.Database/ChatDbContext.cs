using Microsoft.EntityFrameworkCore;
using RealTimeChat.Database.Models;

namespace RealTimeChat.Database;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Timestamp).IsRequired();
            entity.Property(m => m.SenderName).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Text).IsRequired();
        });
    }
}
