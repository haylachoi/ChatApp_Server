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

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<GroupMessage> GroupMessages { get; set; }

    public virtual DbSet<PrivateMessage> PrivateMessages { get; set; }

    public virtual DbSet<PrivateRoom> PrivateRooms { get; set; }

    public virtual DbSet<PrivateRoomInfo> PrivateRoomInfos { get; set; }

    public virtual DbSet<Reaction> Reactions { get; set; }

    public virtual DbSet<ReadedMessage> ReadedMessages { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

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

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.GroupOwner).WithMany(p => p.Groups)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_groups_users_ownerid");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groupmember_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers).HasConstraintName("fk_gm_group");

            entity.HasOne(d => d.Member).WithMany(p => p.GroupMembers).HasConstraintName("fk_gm_user");
        });

        modelBuilder.Entity<GroupMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groupmessages_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsImage).HasDefaultValue(false);

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gm_group");

            entity.HasOne(d => d.Sender).WithMany(p => p.GroupMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gm_user_sender");
        });

        modelBuilder.Entity<PrivateMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("privatemessages_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);
            entity.Property(e => e.IsImage).HasDefaultValue(false);
            entity.Property(e => e.IsReaded).HasDefaultValue(false);

            entity.HasOne(d => d.PrivateRoom).WithMany(p => p.PrivateMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pm_pr");

            entity.HasOne(d => d.Reaction).WithMany(p => p.PrivateMessages).HasConstraintName("fk_pm_emo");

            entity.HasOne(d => d.Receiver).WithMany(p => p.PrivateMessageReceivers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pm_user_receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.PrivateMessageSenders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pm_user_sender");
        });

        modelBuilder.Entity<PrivateRoom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PrivateRoom_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.BiggerUser).WithMany(p => p.PrivateRoomBiggerUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pr_user_1");

            entity.HasOne(d => d.FirstMessage).WithMany(p => p.PrivateRoomFirstMessages).HasConstraintName("fk_pr_pm_first_message");

            entity.HasOne(d => d.LastMessage).WithMany(p => p.PrivateRoomLastMessages).HasConstraintName("fk_pr_pm_last_message");

            entity.HasOne(d => d.SmallerUser).WithMany(p => p.PrivateRoomSmallerUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pr_user_2");
        });

        modelBuilder.Entity<PrivateRoomInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PrivateRoomInfos_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CanDisplayRoom).HasDefaultValue(true);
            entity.Property(e => e.CanShowNofitication).HasDefaultValue(true);
            entity.Property(e => e.UnseenMessageCount).HasDefaultValue(0L);

            entity.HasOne(d => d.FirstUnseenMessage).WithMany(p => p.PrivateRoomInfoFirstUnseenMessages).HasConstraintName("fk_prinfo_pm_first_unnseen");

            entity.HasOne(d => d.LastUnseenMessage).WithMany(p => p.PrivateRoomInfoLastUnseenMessages).HasConstraintName("fk_prinfo_pm_last_unnseen");

            entity.HasOne(d => d.PrivateRoom).WithMany(p => p.PrivateRoomInfos).HasConstraintName("fk_prinfo_pm");

            entity.HasOne(d => d.User).WithMany(p => p.PrivateRoomInfos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_prinfo_user");
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Emotions_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        });

        modelBuilder.Entity<ReadedMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ReadedMessages_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.IsReaded).HasDefaultValue(false);

            entity.HasOne(d => d.GroupMessage).WithMany(p => p.ReadedMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rm_gm");

            entity.HasOne(d => d.Member).WithMany(p => p.ReadedMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rm_user");
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
