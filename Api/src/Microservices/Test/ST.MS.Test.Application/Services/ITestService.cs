using ST.Infra.Core.Attributes;
using ST.MS.Test.Application.Dto;

namespace ST.MS.Test.Application.Services;

public interface ITestService
{
	[UnitOfWork]
	Task TestUow1111();

	Task<List<TestDto>> GetTests();
}
