using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ST.Infra.Repository.Entities;

namespace ST.Shared.Domain.Entites;

public abstract class Entity : IEntity<Guid>
{
	[Key]
	[Column(Order = 0)]
	public virtual Guid Id { get; set; }
}
