using ST.Infra.Core.Attributes;
using ST.MS.Test.Application.Dto;
using ST.Shared.Application;

namespace ST.MS.Test.Application.IServices;

public interface ITestService : IAppService
{
	[UnitOfWork]
	Task TestUow1111();

	Task<List<TestDto>> GetTests();
}
