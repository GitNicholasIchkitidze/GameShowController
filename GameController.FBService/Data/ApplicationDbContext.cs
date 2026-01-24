using GameController.FBService.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<Vote> FaceBookVotes { get; set; }
    public DbSet<BanAccount> BannedAcount { get; set; }
}