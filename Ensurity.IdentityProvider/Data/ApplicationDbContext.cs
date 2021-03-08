using Ensurity.MultiTenancyServer;
using Ensurity.MultiTenancyServer.EntityFramework;
using Ensurity.MultiTenancyServer.Options;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ensurity.IdentityProvider.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser,IdentityRole,string>, IPersistedGrantDbContext, IConfigurationDbContext, ITenantDbContext<TenancyTenant, string>
    {

        private static TenancyModelState<string> _tenancyModelState;
        private readonly ITenancyContext<TenancyTenant> _tenancyContext;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenancyContext<TenancyTenant> tenancyContext)
            : base(options)
        {
            _tenancyContext = tenancyContext;
        }

        public DbSet<TenancyTenant> Organizations { get; set; }


        public DbSet<Client> Clients { get; set; }

        public DbSet<ClientCorsOrigin> ClientCorsOrigins { get; set; }

        public DbSet<IdentityResource> IdentityResources { get; set; }

        public DbSet<ApiResource> ApiResources { get; set; }

        public DbSet<ApiScope> ApiScopes { get; set; }



        public DbSet<PersistedGrant> PersistedGrants { get; set; }

        public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }




        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            // MultiTenancyServer configuration.
            var tenantStoreOptions = new TenantStoreOptions();
            builder.ConfigureTenantContext<TenancyTenant, string>(tenantStoreOptions);

            // Add multi-tenancy support to model.
            var tenantReferenceOptions = new TenantReferenceOptions();
            builder.HasTenancy<string>(tenantReferenceOptions, out _tenancyModelState);

            // Configure custom properties on ApplicationTenant.
            builder.Entity<TenancyTenant>(b =>
            {
                b.Property(t => t.CanonicalName).HasMaxLength(256);
            });


            // Configure properties on User (ASP.NET Core Identity).
            builder.Entity<IdentityUser>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                // Remove unique index on NormalizedUserName.
                b.HasIndex(u => u.NormalizedUserName).HasName("UserNameIndex").IsUnique(false);
                // Add unique index on TenantId and NormalizedUserName.
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(IdentityUser.NormalizedUserName))
                    .HasName("TenantUserNameIndex").IsUnique();
            });

            // Configure properties on Role (ASP.NET Core Identity).
            builder.Entity<IdentityRole>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                b.HasIndex(r => r.NormalizedName).HasName("RoleNameIndex").IsUnique(false);

                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(IdentityRole.NormalizedName))
                    .HasName("TenantRoleNameIndex").IsUnique();
            });


            builder.Entity<Client>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });

            builder.Entity<ClientCorsOrigin>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });
            builder.Entity<IdentityResource>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });
            builder.Entity<ApiResource>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });
            builder.Entity<ApiScope>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });

            builder.Entity<PersistedGrant>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasKey(e => e.Key);
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });

            builder.Entity<DeviceFlowCodes>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasNoKey();
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
            });

        }


        public Task<int> SaveChangesAsync()
        {
            this.EnsureTenancy(_tenancyContext?.Tenant?.Id, _tenancyModelState);
            return base.SaveChangesAsync();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            // Ensure multi-tenancy for all tenantable entities.
            this.EnsureTenancy(_tenancyContext?.Tenant?.Id, _tenancyModelState);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            // Ensure multi-tenancy for all tenantable entities.
            this.EnsureTenancy(_tenancyContext?.Tenant?.Id, _tenancyModelState);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}

