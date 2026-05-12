using System;
using System.Collections.Generic;
using System.Text;
using Castle.DynamicProxy;

namespace ST.Infra.EntityFramework.Interceptors;

public class UowAsyncInterceptor : AsyncInterceptorBase
{
	protected override Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
	{
		throw new NotImplementedException();
	}

	protected override Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
	{
		throw new NotImplementedException();
	}
}
