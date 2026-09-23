using backend.Auth;
using backend.Common.Enums;
using backend.Data;
using backend.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Data.Seed;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // 1. Seed Attributes & Options (including CAP dropdown from course requirements)
        var attributes = await SeedAttributesAsync(db);

        // 2. Seed Users (including local admin, recruiters, and candidates)
        var users = await SeedUsersAsync(userManager);

        // 3. Seed Tags
        var tags = await SeedTagsAsync(db);

        // 4. Seed Positions, Projects, CVs, Likes, Discussions if positions are empty
        if (!await db.Positions.AnyAsync())
        {
            await SeedPositionsAndRelatedDataAsync(db, attributes, users, tags);
        }
    }

    private static async Task<Dictionary<string, AttributeDefinition>> SeedAttributesAsync(AppDbContext db)
    {
        var existingAttributes = await db.Attributes.Include(a => a.Options).ToDictionaryAsync(a => a.Name);

        async Task<AttributeDefinition> EnsureAttrAsync(
            string name,
            string category,
            AttributeType type,
            string description,
            string[]? options = null)
        {
            if (existingAttributes.TryGetValue(name, out var existing))
            {
                if (options != null && options.Length > 0 && !existing.Options.Any())
                {
                    for (int i = 0; i < options.Length; i++)
                    {
                        existing.Options.Add(new AttributeOption
                        {
                            Id = Guid.NewGuid(),
                            AttributeId = existing.Id,
                            Value = options[i],
                            SortOrder = i
                        });
                    }
                    await db.SaveChangesAsync();
                }
                return existing;
            }

            var attr = new AttributeDefinition
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = category,
                Type = type,
                Description = description,
                IsBuiltIn = false
            };

            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    attr.Options.Add(new AttributeOption
                    {
                        Id = Guid.NewGuid(),
                        AttributeId = attr.Id,
                        Value = options[i],
                        SortOrder = i
                    });
                }
            }

            db.Attributes.Add(attr);
            await db.SaveChangesAsync();
            existingAttributes[name] = attr;
            return attr;
        }

        // Requirements from course spec:
        // CAP dropdown in "Certificates" with None, Essentials, Pro, Expert
        await EnsureAttrAsync("CAP", "Certificates", AttributeType.Dropdown, "Certified Analytics Professional certification level", ["None", "Essentials", "Pro", "Expert"]);
        await EnsureAttrAsync("AWS Certified Solutions Architect", "Certificates", AttributeType.Dropdown, "AWS cloud architecture certification level", ["None", "Associate", "Professional"]);
        await EnsureAttrAsync("English Level", "Language", AttributeType.Dropdown, "CEFR English proficiency level", ["A1", "A2", "B1", "B2", "C1", "C2"]);
        await EnsureAttrAsync("German Level", "Language", AttributeType.Dropdown, "German language proficiency level", ["None", "B1", "B2", "C1"]);
        await EnsureAttrAsync("GPA", "Education", AttributeType.Numeric, "Grade Point Average on a 4.0 scale");
        await EnsureAttrAsync("Degree", "Education", AttributeType.Dropdown, "Highest completed educational degree", ["Bachelor's", "Master's", "PhD"]);
        await EnsureAttrAsync("Python", "Technology", AttributeType.Boolean, "Proficiency in Python programming language");
        await EnsureAttrAsync("Apache Hadoop", "Technology", AttributeType.Boolean, "Experience with Apache Hadoop and distributed big data processing");
        await EnsureAttrAsync("Docker", "Technology", AttributeType.Boolean, "Experience with Docker containerization");
        await EnsureAttrAsync("PostgreSQL", "Technology", AttributeType.Boolean, "Experience with relational database modeling in PostgreSQL");
        await EnsureAttrAsync("C# / .NET Core", "Technology", AttributeType.Boolean, "Experience with ASP.NET Core and C# backend development");
        await EnsureAttrAsync("React", "Technology", AttributeType.Boolean, "Experience with React and modern frontend web development");
        await EnsureAttrAsync("Years of Experience", "Experience", AttributeType.Numeric, "Total commercial software development experience in years");
        await EnsureAttrAsync("Remote Work", "Preferences", AttributeType.Boolean, "Available for remote work positions");
        await EnsureAttrAsync("Relocation Availability", "Preferences", AttributeType.Boolean, "Willingness to relocate for on-site positions");
        await EnsureAttrAsync("Presentation Skills", "Soft Skills", AttributeType.Dropdown, "Public speaking and technical presentation abilities", ["Beginner", "Intermediate", "Advanced"]);

        return existingAttributes;
    }

    private static async Task<Dictionary<string, AppUser>> SeedUsersAsync(UserManager<AppUser> userManager)
    {
        var users = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase);

        async Task<AppUser> EnsureUser(string email, string password, string first, string last, string[] roles, string? location = null)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    FirstName = first,
                    LastName = last,
                    Location = location,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    var errors = string.Join("; ", created.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Could not seed user {email}: {errors}");
                }
            }
            else
            {
                if (!await userManager.CheckPasswordAsync(user, password))
                {
                    user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
                    await userManager.UpdateAsync(user);
                }
            }

            foreach (var role in roles)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            users[email] = user;
            return user;
        }

        await EnsureUser("makej1318@gmail.com", "Password123!", "Jasur", "Karimov", [Roles.Administrator, Roles.Recruiter, Roles.Candidate], "Tashkent, Uzbekistan");
        await EnsureUser("sarah.recruiter@acmecorp.com", "Password123!", "Sarah", "Connor", [Roles.Recruiter], "New York, NY");
        await EnsureUser("alex.recruiter@talenthub.local", "Password123!", "Alex", "Morgan", [Roles.Recruiter], "San Francisco, CA");
        await EnsureUser("elena.rostova@example.com", "Password123!", "Elena", "Rostova", [Roles.Candidate], "Boston, MA (Remote)");
        await EnsureUser("john.doe@example.com", "Password123!", "John", "Doe", [Roles.Candidate], "Austin, TX");
        await EnsureUser("anna.smith@example.com", "Password123!", "Anna", "Smith", [Roles.Candidate], "London, UK");
        await EnsureUser("michael.brown@example.com", "Password123!", "Michael", "Brown", [Roles.Candidate], "Berlin, Germany");

        return users;
    }

    private static async Task<Dictionary<string, Tag>> SeedTagsAsync(AppDbContext db)
    {
        var tagNames = new[]
        {
            "SQL", "R", "Python", "Apache Hadoop", ".NET", "PostgreSQL",
            "Docker", "React", "TypeScript", "AWS", "Kubernetes", "Tailwind",
            "Pandas", "Spark", "CI/CD", "Redis", "Machine Learning", "Microservices"
        };

        var existingTags = await db.Tags.ToDictionaryAsync(t => t.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in tagNames)
        {
            if (!existingTags.ContainsKey(name))
            {
                var tag = new Tag { Id = Guid.NewGuid(), Name = name };
                db.Tags.Add(tag);
                existingTags[name] = tag;
            }
        }
        await db.SaveChangesAsync();
        return existingTags;
    }

    private static async Task SeedPositionsAndRelatedDataAsync(
        AppDbContext db,
        Dictionary<string, AttributeDefinition> attributes,
        Dictionary<string, AppUser> users,
        Dictionary<string, Tag> tags)
    {
        var elena = users["elena.rostova@example.com"];
        var john = users["john.doe@example.com"];
        var anna = users["anna.smith@example.com"];
        var michael = users["michael.brown@example.com"];
        var sarah = users["sarah.recruiter@acmecorp.com"];
        var alex = users["alex.recruiter@talenthub.local"];

        // Helper to get option ID
        Guid? GetOptionId(string attrName, string optValue)
        {
            if (attributes.TryGetValue(attrName, out var attr))
            {
                var opt = attr.Options.FirstOrDefault(o => string.Equals(o.Value, optValue, StringComparison.OrdinalIgnoreCase));
                return opt?.Id;
            }
            return null;
        }

        // 1. Candidate Attribute Values
        // Elena Rostova (Data Analyst / Junior Data Engineer candidate)
        db.UserAttributeValues.AddRange(
            new UserAttributeValue { UserId = elena.Id, AttributeId = attributes["English Level"].Id, SelectedOptionId = GetOptionId("English Level", "C1"), Version = 1 },
            new UserAttributeValue { UserId = elena.Id, AttributeId = attributes["GPA"].Id, NumberValue = 3.85m, Version = 1 },
            new UserAttributeValue { UserId = elena.Id, AttributeId = attributes["Python"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = elena.Id, AttributeId = attributes["Apache Hadoop"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = elena.Id, AttributeId = attributes["CAP"].Id, SelectedOptionId = GetOptionId("CAP", "Pro"), Version = 1 },
            new UserAttributeValue { UserId = elena.Id, AttributeId = attributes["Remote Work"].Id, BooleanValue = true, Version = 1 }
        );

        // John Doe (Senior Backend Developer candidate)
        db.UserAttributeValues.AddRange(
            new UserAttributeValue { UserId = john.Id, AttributeId = attributes["English Level"].Id, SelectedOptionId = GetOptionId("English Level", "B2"), Version = 1 },
            new UserAttributeValue { UserId = john.Id, AttributeId = attributes["C# / .NET Core"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = john.Id, AttributeId = attributes["PostgreSQL"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = john.Id, AttributeId = attributes["Docker"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = john.Id, AttributeId = attributes["Years of Experience"].Id, NumberValue = 5.0m, Version = 1 },
            new UserAttributeValue { UserId = john.Id, AttributeId = attributes["Remote Work"].Id, BooleanValue = true, Version = 1 }
        );

        // Anna Smith (Frontend candidate)
        db.UserAttributeValues.AddRange(
            new UserAttributeValue { UserId = anna.Id, AttributeId = attributes["English Level"].Id, SelectedOptionId = GetOptionId("English Level", "C1"), Version = 1 },
            new UserAttributeValue { UserId = anna.Id, AttributeId = attributes["React"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = anna.Id, AttributeId = attributes["Remote Work"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = anna.Id, AttributeId = attributes["Presentation Skills"].Id, SelectedOptionId = GetOptionId("Presentation Skills", "Advanced"), Version = 1 }
        );

        // Michael Brown (DevOps candidate)
        db.UserAttributeValues.AddRange(
            new UserAttributeValue { UserId = michael.Id, AttributeId = attributes["English Level"].Id, SelectedOptionId = GetOptionId("English Level", "B2"), Version = 1 },
            new UserAttributeValue { UserId = michael.Id, AttributeId = attributes["Docker"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = michael.Id, AttributeId = attributes["Remote Work"].Id, BooleanValue = true, Version = 1 },
            new UserAttributeValue { UserId = michael.Id, AttributeId = attributes["Years of Experience"].Id, NumberValue = 4.0m, Version = 1 }
        );

        await db.SaveChangesAsync();

        // 2. Candidate Projects
        void AddProject(Guid userId, string name, DateOnly start, DateOnly? end, string desc, string[] projectTags)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                StartedOn = start,
                EndedOn = end,
                Description = desc,
                Version = 1
            };
            foreach (var pt in projectTags)
            {
                if (tags.TryGetValue(pt, out var tagEntity))
                {
                    project.Tags.Add(new ProjectTag { ProjectId = project.Id, TagId = tagEntity.Id });
                }
            }
            db.Projects.Add(project);
        }

        AddProject(elena.Id, "Automated ETL Pipeline", new DateOnly(2023, 1, 15), new DateOnly(2023, 11, 20),
            "Architected end-to-end data ingestion pipeline processing 2M daily records into PostgreSQL data warehouse with Python and SQL.",
            ["SQL", "Python", "PostgreSQL", "Pandas"]);

        AddProject(elena.Id, "Predictive Customer Analytics Engine", new DateOnly(2024, 1, 10), null,
            "Trained predictive regression and statistical models in Python and R for customer churn and revenue forecasting.",
            ["Python", "R", "SQL"]);

        AddProject(elena.Id, "Distributed Big Data Log Analytics", new DateOnly(2022, 6, 1), new DateOnly(2022, 12, 30),
            "Aggregated multi-node web server telemetry using Apache Hadoop cluster with custom Python MapReduce transformations.",
            ["Apache Hadoop", "Python", "SQL"]);

        AddProject(elena.Id, "Financial Star-Schema Warehouse", new DateOnly(2021, 3, 1), new DateOnly(2022, 5, 15),
            "Modeled dimensional star-schema warehouse in PostgreSQL with automated nightly partition tables and analytics cubes.",
            ["SQL", "PostgreSQL"]);

        AddProject(john.Id, "TalentHub Recruitment Platform", new DateOnly(2023, 5, 1), null,
            "High-throughput enterprise recruitment portal with ASP.NET Core, EF Core, PostgreSQL full-text search, and Redis caching.",
            [".NET", "PostgreSQL", "Docker", "Redis"]);

        AddProject(john.Id, "Microservices Payment Gateway", new DateOnly(2022, 2, 1), new DateOnly(2023, 4, 15),
            "Distributed payment processing service with asynchronous event streaming and zero-downtime rolling deploys.",
            [".NET", "Docker", "PostgreSQL"]);

        AddProject(anna.Id, "Enterprise Analytics Dashboard", new DateOnly(2023, 8, 1), null,
            "Interactive executive dashboard built with React, TypeScript, and responsive Tailwind UI design system.",
            ["React", "TypeScript", "Tailwind"]);

        AddProject(michael.Id, "Multi-Region Cloud Infrastructure", new DateOnly(2023, 3, 1), null,
            "Automated multi-region AWS cloud infrastructure with Terraform, Kubernetes clusters, Docker containers, and CI/CD pipelines.",
            ["AWS", "Kubernetes", "Docker", "CI/CD"]);

        await db.SaveChangesAsync();

        // 3. Positions from Course Requirements
        // Position 1: Junior Data Engineer @ Acme Corp. (Exact match to course spec)
        var pos1 = new Position
        {
            Id = Guid.NewGuid(),
            Title = "Junior Data Engineer",
            Company = "Acme Corp.",
            Level = PositionLevel.Junior,
            ShortDescription = "Acme Corp is seeking a Junior Data Engineer to build ETL pipelines, maintain distributed data flows, and assist analytics teams.",
            IsPublic = true,
            MaxProjects = 4,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        pos1.Attributes.Add(new PositionAttribute { PositionId = pos1.Id, AttributeId = attributes["English Level"].Id, SortOrder = 1, IsRequired = true });
        pos1.Attributes.Add(new PositionAttribute { PositionId = pos1.Id, AttributeId = attributes["GPA"].Id, SortOrder = 2, IsRequired = true });
        pos1.Attributes.Add(new PositionAttribute { PositionId = pos1.Id, AttributeId = attributes["Python"].Id, SortOrder = 3, IsRequired = true });
        pos1.Attributes.Add(new PositionAttribute { PositionId = pos1.Id, AttributeId = attributes["Apache Hadoop"].Id, SortOrder = 4, IsRequired = true });
        pos1.Attributes.Add(new PositionAttribute { PositionId = pos1.Id, AttributeId = attributes["CAP"].Id, SortOrder = 5, IsRequired = true });

        pos1.ProjectTags.Add(new PositionProjectTag { PositionId = pos1.Id, TagId = tags["SQL"].Id });
        pos1.ProjectTags.Add(new PositionProjectTag { PositionId = pos1.Id, TagId = tags["R"].Id });
        pos1.ProjectTags.Add(new PositionProjectTag { PositionId = pos1.Id, TagId = tags["Python"].Id });
        db.Positions.Add(pos1);

        // Position 2: Senior Backend Developer @ TechCorp
        var pos2 = new Position
        {
            Id = Guid.NewGuid(),
            Title = "Senior Backend Developer",
            Company = "TechCorp",
            Level = PositionLevel.Senior,
            ShortDescription = "Lead backend developer responsible for high-performance .NET microservices, database architecture, and cloud scalability.",
            IsPublic = true,
            MaxProjects = 3,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddHours(-3)
        };
        pos2.Attributes.Add(new PositionAttribute { PositionId = pos2.Id, AttributeId = attributes["English Level"].Id, SortOrder = 1, IsRequired = true });
        pos2.Attributes.Add(new PositionAttribute { PositionId = pos2.Id, AttributeId = attributes["C# / .NET Core"].Id, SortOrder = 2, IsRequired = true });
        pos2.Attributes.Add(new PositionAttribute { PositionId = pos2.Id, AttributeId = attributes["PostgreSQL"].Id, SortOrder = 3, IsRequired = true });
        pos2.Attributes.Add(new PositionAttribute { PositionId = pos2.Id, AttributeId = attributes["Docker"].Id, SortOrder = 4, IsRequired = true });
        pos2.Attributes.Add(new PositionAttribute { PositionId = pos2.Id, AttributeId = attributes["Remote Work"].Id, SortOrder = 5, IsRequired = false });

        pos2.ProjectTags.Add(new PositionProjectTag { PositionId = pos2.Id, TagId = tags[".NET"].Id });
        pos2.ProjectTags.Add(new PositionProjectTag { PositionId = pos2.Id, TagId = tags["PostgreSQL"].Id });
        pos2.ProjectTags.Add(new PositionProjectTag { PositionId = pos2.Id, TagId = tags["Docker"].Id });
        pos2.ProjectTags.Add(new PositionProjectTag { PositionId = pos2.Id, TagId = tags["Redis"].Id });
        db.Positions.Add(pos2);

        // Position 3: DevOps Engineer @ CloudSystems
        var pos3 = new Position
        {
            Id = Guid.NewGuid(),
            Title = "DevOps Engineer",
            Company = "CloudSystems",
            Level = PositionLevel.Middle,
            ShortDescription = "Design, build, and automate continuous delivery infrastructure on AWS with Kubernetes and Docker containers.",
            IsPublic = true,
            MaxProjects = 3,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            UpdatedAt = DateTime.UtcNow.AddHours(-5)
        };
        pos3.Attributes.Add(new PositionAttribute { PositionId = pos3.Id, AttributeId = attributes["English Level"].Id, SortOrder = 1, IsRequired = true });
        pos3.Attributes.Add(new PositionAttribute { PositionId = pos3.Id, AttributeId = attributes["Docker"].Id, SortOrder = 2, IsRequired = true });
        pos3.Attributes.Add(new PositionAttribute { PositionId = pos3.Id, AttributeId = attributes["Remote Work"].Id, SortOrder = 3, IsRequired = false });

        pos3.ProjectTags.Add(new PositionProjectTag { PositionId = pos3.Id, TagId = tags["Docker"].Id });
        pos3.ProjectTags.Add(new PositionProjectTag { PositionId = pos3.Id, TagId = tags["AWS"].Id });
        pos3.ProjectTags.Add(new PositionProjectTag { PositionId = pos3.Id, TagId = tags["Kubernetes"].Id });
        pos3.ProjectTags.Add(new PositionProjectTag { PositionId = pos3.Id, TagId = tags["CI/CD"].Id });
        db.Positions.Add(pos3);

        // Position 4: Frontend Developer @ WebLabs
        var pos4 = new Position
        {
            Id = Guid.NewGuid(),
            Title = "Frontend Developer",
            Company = "WebLabs",
            Level = PositionLevel.Middle,
            ShortDescription = "Passionate Frontend Developer to craft responsive, accessible user interfaces with React, TypeScript, and modern CSS.",
            IsPublic = true,
            MaxProjects = 3,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-6)
        };
        pos4.Attributes.Add(new PositionAttribute { PositionId = pos4.Id, AttributeId = attributes["English Level"].Id, SortOrder = 1, IsRequired = true });
        pos4.Attributes.Add(new PositionAttribute { PositionId = pos4.Id, AttributeId = attributes["React"].Id, SortOrder = 2, IsRequired = true });
        pos4.Attributes.Add(new PositionAttribute { PositionId = pos4.Id, AttributeId = attributes["Remote Work"].Id, SortOrder = 3, IsRequired = false });

        pos4.ProjectTags.Add(new PositionProjectTag { PositionId = pos4.Id, TagId = tags["React"].Id });
        pos4.ProjectTags.Add(new PositionProjectTag { PositionId = pos4.Id, TagId = tags["TypeScript"].Id });
        pos4.ProjectTags.Add(new PositionProjectTag { PositionId = pos4.Id, TagId = tags["Tailwind"].Id });
        db.Positions.Add(pos4);

        // Position 5: Business Analyst @ GlobalTech
        var pos5 = new Position
        {
            Id = Guid.NewGuid(),
            Title = "Business Analyst",
            Company = "GlobalTech",
            Level = PositionLevel.Middle,
            ShortDescription = "Translate complex business requirements into analytical specifications, dashboards, and system documentation.",
            IsPublic = true,
            MaxProjects = 3,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-6),
            UpdatedAt = DateTime.UtcNow.AddHours(-8)
        };
        pos5.Attributes.Add(new PositionAttribute { PositionId = pos5.Id, AttributeId = attributes["English Level"].Id, SortOrder = 1, IsRequired = true });
        pos5.Attributes.Add(new PositionAttribute { PositionId = pos5.Id, AttributeId = attributes["Presentation Skills"].Id, SortOrder = 2, IsRequired = true });
        pos5.Attributes.Add(new PositionAttribute { PositionId = pos5.Id, AttributeId = attributes["Remote Work"].Id, SortOrder = 3, IsRequired = false });

        pos5.ProjectTags.Add(new PositionProjectTag { PositionId = pos5.Id, TagId = tags["SQL"].Id });
        pos5.ProjectTags.Add(new PositionProjectTag { PositionId = pos5.Id, TagId = tags["Python"].Id });
        db.Positions.Add(pos5);

        await db.SaveChangesAsync();

        // 4. Candidate CVs
        // Elena Rostova's CV for Junior Data Engineer @ Acme Corp.
        var cvElena = new Cv
        {
            Id = Guid.NewGuid(),
            CandidateId = elena.Id,
            PositionId = pos1.Id,
            Status = CvStatus.Published,
            CreatedAt = DateTime.UtcNow.AddHours(-18),
            UpdatedAt = DateTime.UtcNow.AddHours(-17),
            PublishedAt = DateTime.UtcNow.AddHours(-17)
        };
        cvElena.Likes.Add(new CvLike { CvId = cvElena.Id, RecruiterId = sarah.Id, CreatedAt = DateTime.UtcNow.AddHours(-16) });
        cvElena.Likes.Add(new CvLike { CvId = cvElena.Id, RecruiterId = alex.Id, CreatedAt = DateTime.UtcNow.AddHours(-15) });
        db.Cvs.Add(cvElena);

        // John Doe's CV for Senior Backend Developer @ TechCorp
        var cvJohn = new Cv
        {
            Id = Guid.NewGuid(),
            CandidateId = john.Id,
            PositionId = pos2.Id,
            Status = CvStatus.Published,
            CreatedAt = DateTime.UtcNow.AddHours(-14),
            UpdatedAt = DateTime.UtcNow.AddHours(-13),
            PublishedAt = DateTime.UtcNow.AddHours(-13)
        };
        cvJohn.Likes.Add(new CvLike { CvId = cvJohn.Id, RecruiterId = alex.Id, CreatedAt = DateTime.UtcNow.AddHours(-12) });
        db.Cvs.Add(cvJohn);

        // Anna Smith's CV for Frontend Developer @ WebLabs
        var cvAnna = new Cv
        {
            Id = Guid.NewGuid(),
            CandidateId = anna.Id,
            PositionId = pos4.Id,
            Status = CvStatus.Published,
            CreatedAt = DateTime.UtcNow.AddHours(-8),
            UpdatedAt = DateTime.UtcNow.AddHours(-7),
            PublishedAt = DateTime.UtcNow.AddHours(-7)
        };
        cvAnna.Likes.Add(new CvLike { CvId = cvAnna.Id, RecruiterId = sarah.Id, CreatedAt = DateTime.UtcNow.AddHours(-6) });
        db.Cvs.Add(cvAnna);

        // Michael Brown's CV for DevOps Engineer @ CloudSystems
        var cvMichael = new Cv
        {
            Id = Guid.NewGuid(),
            CandidateId = michael.Id,
            PositionId = pos3.Id,
            Status = CvStatus.Published,
            CreatedAt = DateTime.UtcNow.AddHours(-4),
            UpdatedAt = DateTime.UtcNow.AddHours(-3),
            PublishedAt = DateTime.UtcNow.AddHours(-3)
        };
        cvMichael.Likes.Add(new CvLike { CvId = cvMichael.Id, RecruiterId = alex.Id, CreatedAt = DateTime.UtcNow.AddHours(-2) });
        db.Cvs.Add(cvMichael);

        await db.SaveChangesAsync();

        // 5. Discussion Posts on Positions
        db.DiscussionPosts.AddRange(
            new DiscussionPost
            {
                Id = Guid.NewGuid(),
                PositionId = pos1.Id,
                AuthorId = sarah.Id,
                Content = "Welcome to Acme Corp! We are excited to find passionate data engineers with solid SQL and Python skills to join our team.",
                CreatedAt = DateTime.UtcNow.AddHours(-20)
            },
            new DiscussionPost
            {
                Id = Guid.NewGuid(),
                PositionId = pos1.Id,
                AuthorId = elena.Id,
                Content = "Hello! Does the team support hybrid work, or is the position open for full-time remote candidates as well?",
                CreatedAt = DateTime.UtcNow.AddHours(-19)
            },
            new DiscussionPost
            {
                Id = Guid.NewGuid(),
                PositionId = pos1.Id,
                AuthorId = sarah.Id,
                Content = "Hi Elena! Yes, we offer a flexible hybrid schedule with full remote options available for all data engineers.",
                CreatedAt = DateTime.UtcNow.AddHours(-18)
            },
            new DiscussionPost
            {
                Id = Guid.NewGuid(),
                PositionId = pos2.Id,
                AuthorId = alex.Id,
                Content = "Looking for candidates with strong .NET Core and relational database modeling experience.",
                CreatedAt = DateTime.UtcNow.AddHours(-15)
            },
            new DiscussionPost
            {
                Id = Guid.NewGuid(),
                PositionId = pos2.Id,
                AuthorId = john.Id,
                Content = "Looking forward to contributing to the distributed microservices architecture!",
                CreatedAt = DateTime.UtcNow.AddHours(-14)
            }
        );

        await db.SaveChangesAsync();
    }
}
