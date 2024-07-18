﻿// <auto-generated />
using System;
using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    [Migration("20240718210249_AddRunInfo")]
    partial class AddRunInfo
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "user_role", new[] { "registered", "confirmed", "administrator", "banned" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.AccountConfirmation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Instant>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<Instant?>("UsedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("used_at");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_account_confirmations");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_account_confirmations_user_id");

                    b.ToTable("account_confirmations", (string)null);
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.AccountRecovery", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Instant>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<Instant?>("UsedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("used_at");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_account_recoveries");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_account_recoveries_user_id");

                    b.ToTable("account_recoveries", (string)null);
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.Category", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("LeaderboardId")
                        .HasColumnType("bigint")
                        .HasColumnName("leaderboard_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int>("PlayersMax")
                        .HasColumnType("integer")
                        .HasColumnName("players_max");

                    b.Property<int>("PlayersMin")
                        .HasColumnType("integer")
                        .HasColumnName("players_min");

                    b.Property<string>("Rules")
                        .HasColumnType("text")
                        .HasColumnName("rules");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("slug");

                    b.HasKey("Id")
                        .HasName("pk_categories");

                    b.HasIndex("LeaderboardId")
                        .HasDatabaseName("ix_categories_leaderboard_id");

                    b.ToTable("categories", (string)null);
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.Leaderboard", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Rules")
                        .HasColumnType("text")
                        .HasColumnName("rules");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("slug");

                    b.HasKey("Id")
                        .HasName("pk_leaderboards");

                    b.ToTable("leaderboards", (string)null);
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.Run", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<long>("CategoryId")
                        .HasColumnType("bigint")
                        .HasColumnName("category_id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Instant?>("DeletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("deleted_at");

                    b.Property<string>("Info")
                        .HasColumnType("text")
                        .HasColumnName("info");

                    b.Property<LocalDate>("PlayedOn")
                        .HasColumnType("date")
                        .HasColumnName("played_on");

                    b.Property<Instant?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_runs");

                    b.HasIndex("CategoryId")
                        .HasDatabaseName("ix_runs_category_id");

                    b.ToTable("runs", (string)null);
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("email")
                        .UseCollation("case_insensitive");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password");

                    b.Property<UserRole>("Role")
                        .HasColumnType("user_role")
                        .HasColumnName("role");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username")
                        .UseCollation("case_insensitive");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("ix_users_email");

                    b.HasIndex("Username")
                        .IsUnique()
                        .HasDatabaseName("ix_users_username");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.AccountConfirmation", b =>
                {
                    b.HasOne("LeaderboardBackend.Models.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_account_confirmations_users_user_id");

                    b.Navigation("User");
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.AccountRecovery", b =>
                {
                    b.HasOne("LeaderboardBackend.Models.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_account_recoveries_users_user_id");

                    b.Navigation("User");
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.Category", b =>
                {
                    b.HasOne("LeaderboardBackend.Models.Entities.Leaderboard", "Leaderboard")
                        .WithMany("Categories")
                        .HasForeignKey("LeaderboardId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_categories_leaderboards_leaderboard_id");

                    b.Navigation("Leaderboard");
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.Run", b =>
                {
                    b.HasOne("LeaderboardBackend.Models.Entities.Category", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_runs_categories_category_id");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("LeaderboardBackend.Models.Entities.Leaderboard", b =>
                {
                    b.Navigation("Categories");
                });
#pragma warning restore 612, 618
        }
    }
}