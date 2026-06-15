using ProjectFill.Domain.Interfaces;

namespace ProjectFill.Infrastructure.Data;

public partial class StaticDataService : IStaticDataService
{
    public StaticDataService()
    {
        InitGeneratedData(Path.Combine(AppContext.BaseDirectory, "generated", "data"));
    }
}
