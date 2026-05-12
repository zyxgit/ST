namespace ST.Infra.Repository.Interface;

public interface IUnitOfWork
{
	Task ExecuteAsync(Func<Task> action);
}
