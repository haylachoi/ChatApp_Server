using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class ChatAppContext : DbContext
{
    public ChatAppContext()
    {
    }

    public ChatAppContext(DbContextOptions<ChatAppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Blocklist> Blocklists { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<GroupInfo> GroupInfos { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageDetail> MessageDetails { get; set; }

    public virtual DbSet<Reaction> Reactions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomId> RoomIds { get; set; }

    public virtual DbSet<RoomMemberInfo> RoomMemberInfos { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Server=localhost;Database=ChatApp;Port=5432;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blocklist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("blocklist_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Blocked).WithMany(p => p.BlocklistBlockeds)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fs_user_blocked");

            entity.HasOne(d => d.Blocker).WithMany(p => p.BlocklistBlockers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fs_user_blocker");
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("friendships_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Receiver).WithMany(p => p.FriendshipReceivers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fs_user_receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.FriendshipSenders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fs_user_sender");
        });

        modelBuilder.Entity<GroupInfo>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("GroupInfo_pkey");

            entity.Property(e => e.GroupId).ValueGeneratedNever();

            entity.HasOne(d => d.Group).WithOne(p => p.GroupInfo).HasConstraintName("fk_gi_room");

            entity.HasOne(d => d.GroupOwner).WithMany(p => p.GroupInfos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gi_user");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("privatemessages_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);
            entity.Property(e => e.IsImage).HasDefaultValue(false);
            entity.Property(e => e.IsReaded).HasDefaultValue(false);

            entity.HasOne(d => d.Room).WithMany(p => p.Messages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_messages_room");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_messages_user_sender");
        });

        modelBuilder.Entity<MessageDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("MessageDetail_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.Reaction).WithMany(p => p.MessageDetails).HasConstraintName("fk_md_reactions");

            entity.HasOne(d => d.User).WithMany(p => p.MessageDetails).HasConstraintName("fk_md_users");

            entity.HasOne(d => d.Message).WithMany(p => p.MessageDetails)
                .HasPrincipalKey(p => new { p.Id, p.RoomId })
                .HasForeignKey(d => new { d.MessageId, d.RoomId })
                .HasConstraintName("fk_md_messages");
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Emotions_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refreshtoken_pkey");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.IsUsed).HasDefaultValue(true);
            entity.Property(e => e.IssuedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rt_user");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PrivateRoom_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.IsGroup).HasDefaultValue(false);

            entity.HasOne(d => d.FirstMessage).WithMany(p => p.RoomFirstMessages).HasConstraintName("fk_room_message_first_message");

            entity.HasOne(d => d.LastMessage).WithMany(p => p.RoomLastMessages).HasConstraintName("fk_room_message_last_message");
        });

        modelBuilder.Entity<RoomMemberInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PrivateRoomInfos_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CanDisplayRoom).HasDefaultValue(true);
            entity.Property(e => e.CanShowNofitication).HasDefaultValue(true);
            entity.Property(e => e.UnseenMessageCount).HasDefaultValue(0L);

            entity.HasOne(d => d.FirstUnseenMessage).WithMany(p => p.RoomMemberInfos).HasConstraintName("fk_prinfo_messages_first_unseen");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomMemberInfos).HasConstraintName("fk_prinfo_room");

            entity.HasOne(d => d.User).WithMany(p => p.RoomMemberInfos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_prinfo_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsOnline).HasDefaultValue(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
