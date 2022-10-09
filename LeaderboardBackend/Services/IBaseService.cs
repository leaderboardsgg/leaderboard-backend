namespace LeaderboardBackend.Services;

public interface IBaseService<Entity, ID> where Entity : BaseEntity
{
	Task<Entity> Get(ID id);
	Task Create(Entity entity);
	Task Delete(Entity entity);
}
