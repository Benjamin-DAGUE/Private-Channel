using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace PrivateChannel.DataModel;

public class PrivateChannelContext : DbContext
{
    #region Properties

    /// <summary>
    ///     Get or set <see cref="Note"/> database set.
    /// </summary>
    public required DbSet<Note> Notes { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    ///     Initialise a new instance off the <see cref="PrivateChannelContext"/> class.
    /// </summary>
    public PrivateChannelContext()
    {

    }

    /// <summary>
    ///     Initialise a new instance off the <see cref="PrivateChannelContext"/> class.
    /// </summary>
    /// <param name="options"></param>
    public PrivateChannelContext(DbContextOptions<PrivateChannelContext> options)
    : base(options)
    {

    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured == false)
        {
            optionsBuilder.UseSqlServer(@"Server=localhost;Database=PrivateChannel;Trusted_Connection=True;Encrypted=false");
        }
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>(e =>
        {
            e.HasKey(e => e.Id);

            e.Property(e => e.RemainingUnlockAttempts)
            .HasDefaultValue(5);
        });
    }

    #endregion
}
