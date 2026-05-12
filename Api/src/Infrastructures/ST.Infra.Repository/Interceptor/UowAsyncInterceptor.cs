using Castle.DynamicProxy;
using ST.Infra.Repository.Interface;

namespace ST.Infra.Repository.Interceptor;

public class UowAsyncInterceptor : IAsyncInterceptor
{
	private readonly IUnitOfWork _unitOfWork;

	public UowAsyncInterceptor(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public void InterceptAsynchronous(IInvocation invocation)
	{
		throw new NotImplementedException();
	}

	public void InterceptAsynchronous<TResult>(IInvocation invocation)
	{
		throw new NotImplementedException();
	}

	public void InterceptSynchronous(IInvocation invocation)
	{
		throw new NotImplementedException();
	}
}
