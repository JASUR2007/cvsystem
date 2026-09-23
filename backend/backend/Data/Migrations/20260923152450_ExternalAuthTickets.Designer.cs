using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;
using backend.Data;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260923152450_ExternalAuthTickets")]
    partial class ExternalAuthTickets
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("backend.Data.AppUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<int>("AuthVersion")
                        .IsConcurrencyToken()
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("IsBlocked")
                        .HasColumnType("boolean");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Location")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("PhotoObjectKey")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<NpgsqlTsVector>("SearchVector")
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("tsvector")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english")
                        .HasAnnotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName" });

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<string>("Theme")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<int>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .IsUnique()
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.HasIndex("SearchVector");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("SearchVector"), "GIN");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("backend.Data.AttributeDefinition", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("character varying(80)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<bool>("IsBuiltIn")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("character varying(160)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(24)
                        .HasColumnType("character varying(24)");

                    b.Property<int>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Attributes");
                });

            modelBuilder.Entity("backend.Data.AttributeOption", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("AttributeId")
                        .HasColumnType("uuid");

                    b.Property<int>("SortOrder")
                        .HasColumnType("integer");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.HasKey("Id");

                    b.HasIndex("AttributeId", "Value")
                        .IsUnique();

                    b.ToTable("AttributeOptions");
                });

            modelBuilder.Entity("backend.Data.Cv", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CandidateId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("PositionId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("PublishedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(24)
                        .HasColumnType("character varying(24)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("PositionId");

                    b.HasIndex("CandidateId", "PositionId")
                        .IsUnique();

                    b.ToTable("Cvs");
                });

            modelBuilder.Entity("backend.Data.CvLike", b =>
                {
                    b.Property<Guid>("CvId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RecruiterId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("CvId", "RecruiterId");

                    b.HasIndex("RecruiterId");

                    b.ToTable("CvLikes");
                });

            modelBuilder.Entity("backend.Data.DiscussionPost", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("PositionId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("PositionId", "CreatedAt");

                    b.ToTable("DiscussionPosts");
                });

            modelBuilder.Entity("backend.Data.ExternalAuthTicket", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("CodeHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CodeHash")
                        .IsUnique();

                    b.HasIndex("ExpiresAt");

                    b.HasIndex("UserId");

                    b.ToTable("ExternalAuthTickets");
                });

            modelBuilder.Entity("backend.Data.Position", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Company")
                        .HasMaxLength(160)
                        .HasColumnType("character varying(160)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsPublic")
                        .HasColumnType("boolean");

                    b.Property<string>("Level")
                        .HasMaxLength(24)
                        .HasColumnType("character varying(24)");

                    b.Property<int>("MaxProjects")
                        .HasColumnType("integer");

                    b.Property<NpgsqlTsVector>("SearchVector")
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("tsvector")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english")
                        .HasAnnotation("Npgsql:TsVectorProperties", new[] { "Title", "ShortDescription" });

                    b.Property<string>("ShortDescription")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("SearchVector");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("SearchVector"), "GIN");

                    b.HasIndex("UpdatedAt");

                    b.ToTable("Positions");
                });

            modelBuilder.Entity("backend.Data.PositionAccessRule", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("AttributeId")
                        .HasColumnType("uuid");

                    b.Property<bool?>("BooleanValue")
                        .HasColumnType("boolean");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<decimal?>("NumberValue")
                        .HasPrecision(18, 4)
                        .HasColumnType("numeric(18,4)");

                    b.Property<string>("Operator")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<Guid?>("OptionId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("PositionId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("AttributeId");

                    b.HasIndex("PositionId");

                    b.ToTable("PositionAccessRules");
                });

            modelBuilder.Entity("backend.Data.PositionAttribute", b =>
                {
                    b.Property<Guid>("PositionId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("AttributeId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsRequired")
                        .HasColumnType("boolean");

                    b.Property<int>("SortOrder")
                        .HasColumnType("integer");

                    b.HasKey("PositionId", "AttributeId");

                    b.HasIndex("AttributeId");

                    b.ToTable("PositionAttributes");
                });

            modelBuilder.Entity("backend.Data.PositionProjectTag", b =>
                {
                    b.Property<Guid>("PositionId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uuid");

                    b.HasKey("PositionId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("PositionProjectTags");
                });

            modelBuilder.Entity("backend.Data.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateOnly?>("EndedOn")
                        .HasColumnType("date");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateOnly>("StartedOn")
                        .HasColumnType("date");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<int>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("backend.Data.ProjectTag", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uuid");

                    b.HasKey("ProjectId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("ProjectTags");
                });

            modelBuilder.Entity("backend.Data.Tag", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("character varying(80)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("backend.Data.UserAttributeValue", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("AttributeId")
                        .HasColumnType("uuid");

                    b.Property<bool?>("BooleanValue")
                        .HasColumnType("boolean");

                    b.Property<DateOnly?>("DateValue")
                        .HasColumnType("date");

                    b.Property<string>("ImageObjectKey")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<decimal?>("NumberValue")
                        .HasPrecision(18, 4)
                        .HasColumnType("numeric(18,4)");

                    b.Property<DateOnly?>("PeriodEnd")
                        .HasColumnType("date");

                    b.Property<DateOnly?>("PeriodStart")
                        .HasColumnType("date");

                    b.Property<Guid?>("SelectedOptionId")
                        .HasColumnType("uuid");

                    b.Property<string>("TextValue")
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("integer");

                    b.HasKey("UserId", "AttributeId");

                    b.HasIndex("AttributeId");

                    b.HasIndex("SelectedOptionId");

                    b.ToTable("UserAttributeValues");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("backend.Data.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("backend.Data.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Data.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("backend.Data.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("backend.Data.AttributeOption", b =>
                {
                    b.HasOne("backend.Data.AttributeDefinition", "Attribute")
                        .WithMany("Options")
                        .HasForeignKey("AttributeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Attribute");
                });

            modelBuilder.Entity("backend.Data.Cv", b =>
                {
                    b.HasOne("backend.Data.AppUser", "Candidate")
                        .WithMany("Cvs")
                        .HasForeignKey("CandidateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Data.Position", "Position")
                        .WithMany("Cvs")
                        .HasForeignKey("PositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Candidate");

                    b.Navigation("Position");
                });

            modelBuilder.Entity("backend.Data.CvLike", b =>
                {
                    b.HasOne("backend.Data.Cv", "Cv")
                        .WithMany("Likes")
                        .HasForeignKey("CvId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Data.AppUser", "Recruiter")
                        .WithMany()
                        .HasForeignKey("RecruiterId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Cv");

                    b.Navigation("Recruiter");
                });

            modelBuilder.Entity("backend.Data.DiscussionPost", b =>
                {
                    b.HasOne("backend.Data.AppUser", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("backend.Data.Position", "Position")
                        .WithMany("DiscussionPosts")
                        .HasForeignKey("PositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");

                    b.Navigation("Position");
                });

            modelBuilder.Entity("backend.Data.ExternalAuthTicket", b =>
                {
                    b.HasOne("backend.Data.AppUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("backend.Data.PositionAccessRule", b =>
                {
                    b.HasOne("backend.Data.AttributeDefinition", "Attribute")
                        .WithMany()
                        .HasForeignKey("AttributeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("backend.Data.Position", "Position")
                        .WithMany("AccessRules")
                        .HasForeignKey("PositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Attribute");

                    b.Navigation("Position");
                });

            modelBuilder.Entity("backend.Data.PositionAttribute", b =>
                {
                    b.HasOne("backend.Data.AttributeDefinition", "Attribute")
                        .WithMany("PositionAttributes")
                        .HasForeignKey("AttributeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("backend.Data.Position", "Position")
                        .WithMany("Attributes")
                        .HasForeignKey("PositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Attribute");

                    b.Navigation("Position");
                });

            modelBuilder.Entity("backend.Data.PositionProjectTag", b =>
                {
                    b.HasOne("backend.Data.Position", "Position")
                        .WithMany("ProjectTags")
                        .HasForeignKey("PositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Data.Tag", "Tag")
                        .WithMany("Positions")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Position");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("backend.Data.Project", b =>
                {
                    b.HasOne("backend.Data.AppUser", "User")
                        .WithMany("Projects")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("backend.Data.ProjectTag", b =>
                {
                    b.HasOne("backend.Data.Project", "Project")
                        .WithMany("Tags")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Data.Tag", "Tag")
                        .WithMany("Projects")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("backend.Data.UserAttributeValue", b =>
                {
                    b.HasOne("backend.Data.AttributeDefinition", "Attribute")
                        .WithMany("UserValues")
                        .HasForeignKey("AttributeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("backend.Data.AttributeOption", "SelectedOption")
                        .WithMany()
                        .HasForeignKey("SelectedOptionId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("backend.Data.AppUser", "User")
                        .WithMany("AttributeValues")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Attribute");

                    b.Navigation("SelectedOption");

                    b.Navigation("User");
                });

            modelBuilder.Entity("backend.Data.AppUser", b =>
                {
                    b.Navigation("AttributeValues");

                    b.Navigation("Cvs");

                    b.Navigation("Projects");
                });

            modelBuilder.Entity("backend.Data.AttributeDefinition", b =>
                {
                    b.Navigation("Options");

                    b.Navigation("PositionAttributes");

                    b.Navigation("UserValues");
                });

            modelBuilder.Entity("backend.Data.Cv", b =>
                {
                    b.Navigation("Likes");
                });

            modelBuilder.Entity("backend.Data.Position", b =>
                {
                    b.Navigation("AccessRules");

                    b.Navigation("Attributes");

                    b.Navigation("Cvs");

                    b.Navigation("DiscussionPosts");

                    b.Navigation("ProjectTags");
                });

            modelBuilder.Entity("backend.Data.Project", b =>
                {
                    b.Navigation("Tags");
                });

            modelBuilder.Entity("backend.Data.Tag", b =>
                {
                    b.Navigation("Positions");

                    b.Navigation("Projects");
                });
#pragma warning restore 612, 618
        }
    }
}
