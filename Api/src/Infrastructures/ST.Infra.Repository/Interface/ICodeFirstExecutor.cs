using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infra.Repository.Interface;

public interface ICodeFirstExecutor
{
	Task ExecuteAsync(IServiceProvider serviceProvider);
}
