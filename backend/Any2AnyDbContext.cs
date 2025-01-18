/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-01-26
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using Any2Any.Prototype.Model;
using Microsoft.EntityFrameworkCore;

namespace Any2Any.Prototype;

/// <summary>
///     The database context for Any2Any models.
/// </summary>
public class Any2AnyDbContext(DbContextOptions<Any2AnyDbContext> options)
    : DbContext(options)
{
    public DbSet<Entity> Entities { get; set; }
    public DbSet<EntityProperty> EntityProperties { get; set; }
    public DbSet<Record> Records { get; set; }
    public DbSet<Value> Values { get; set; }
    public DbSet<RecordLink> RecordLinks { get; set; }
    public DbSet<RecordGroup> RecordGroups { get; set; }
    public DbSet<RecordGroupLink> RecordGroupLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecordLink>()
            .HasOne(rl => rl.Record1)
            .WithMany()
            .HasForeignKey(rl => rl.Record1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecordLink>()
            .HasOne(rl => rl.Record2)
            .WithMany()
            .HasForeignKey(rl => rl.Record2Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecordGroupLink>()
            .HasOne(rgl => rgl.RecordGroup)
            .WithMany(rg => rg.RecordGroupLinks)
            .HasForeignKey(rgl => rgl.RecordGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}